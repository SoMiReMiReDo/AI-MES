using System.Globalization;
using System.Text;
using System.Text.Json;
using MESagent.Application;
using MESagent.Domain.Entities;
using MESagent.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MESagent.Infrastructure.ApplicationAuditReliability;

internal sealed partial class ReliabilityBackend(
    MesDbContext db,
    Guid fabId,
    RequestActor actor,
    Action<string> require,
    Func<string, bool> can,
    TimeProvider clock)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        UnmappedMemberHandling = System.Text.Json.Serialization.JsonUnmappedMemberHandling.Disallow
    };

    private DateTime Now => clock.GetUtcNow().UtcDateTime;

    public async Task<OrganizationIdentityAccessControlResult> Execute(OrganizationIdentityAccessControlRequest request)
    {
        if (request is { Method: "GET", Path: "reliability/audit-logs" })
        {
            require(ReliabilityAccess.Audit);
            return new(await AuditLogs(request));
        }

        require(ReliabilityAccess.OutboxRead);
        var messages = OutboxDelivery.Scope(db, fabId);
        if (request is { Method: "GET", Path: "reliability/status" })
        {
            var status = await messages.GroupBy(_ => 1).Select(g => new OutboxStatus(
                g.Count(x => x.PublishedAt == null && x.DeadLetteredAt == null),
                g.Count(x => x.PublishedAt != null),
                g.Count(x => x.DeadLetteredAt != null),
                g.Min(x => x.PublishedAt == null && x.DeadLetteredAt == null ? (DateTime?)x.OccurredAt : null)))
                .SingleOrDefaultAsync();
            return new(status ?? new OutboxStatus(0, 0, 0, null));
        }
        if (request is { Method: "GET", Path: "reliability/outbox" })
        {
            var status = Filter(request, "status", 24);
            messages = status switch
            {
                "Pending" => messages.Where(x => x.PublishedAt == null && x.DeadLetteredAt == null),
                "Published" => messages.Where(x => x.PublishedAt != null),
                "DeadLettered" => messages.Where(x => x.DeadLetteredAt != null),
                null => messages,
                _ => throw Invalid("投递状态无效")
            };
            if (FilterId(request, "correlationId") is Guid correlationId)
                messages = messages.Where(x => x.CorrelationId == correlationId);
            var (page, size) = Paging(request);
            var rows = await messages.AsNoTracking().OrderByDescending(x => x.OccurredAt)
                .ThenByDescending(x => x.Id).Skip((page - 1) * size).Take(size).ToArrayAsync();
            return new(new PageView<OutboxView>(rows.Select(View).ToArray(), await messages.CountAsync(), page, size));
        }

        var parts = request.Path.Split('/');
        if (request.Method != "POST" || parts.Length != 4 || parts[1] != "outbox" || parts[3] != "replay"
            || !Guid.TryParse(parts[2], out var id))
            throw Missing();
        require(ReliabilityAccess.OutboxReplay);
        var replay = request.Body.Deserialize<ReplayOutboxInput>(Json);
        var reason = replay?.Reason?.Trim();
        if (string.IsNullOrEmpty(reason) || reason.Length > 500 || reason.Any(char.IsControl))
            throw Invalid("重放原因应为 1～500 个可显示字符");

        var message = await messages.SingleOrDefaultAsync(x => x.Id == id) ?? throw Missing();
        if (string.IsNullOrWhiteSpace(request.Version))
            throw new AppException(428, "VersionRequired", "请先读取待发事件版本");
        if (request.Version != Tag(message.RowVersion))
            throw new AppException(412, "VersionConflict", "投递状态已变化，请刷新");
        if (message.DeadLetteredAt == null)
            throw new AppException(409, "OutboxStateConflict", "仅死信可以重放");
        var before = View(message);
        var now = Now;
        message.Attempts = 0;
        message.DeadLetteredAt = null;
        message.LastError = null;
        message.NextAttemptAt = now;
        db.Add(new AuditLog
        {
            FabId = fabId,
            Module = "reliability",
            UserId = actor.UserId,
            EntityType = nameof(OutboxMessage),
            EntityId = message.Id,
            Action = "OutboxReplayQueued",
            OccurredAt = now,
            CorrelationId = actor.CorrelationId,
            SourceChannel = "Web",
            BeforeState = JsonSerializer.Serialize(new { before.Status, before.Attempts, before.ErrorCode }, Json),
            AfterState = JsonSerializer.Serialize(new
            {
                status = "Pending", reason, originalCorrelationId = message.CorrelationId, message.BusinessEventId
            }, Json)
        });
        await db.SaveChangesAsync();
        return new(View(message), Tag(message.RowVersion));
    }

    private static OutboxView View(OutboxMessage x) => new(x.Id, x.BusinessEventId, x.EventType,
        x.PublishedAt != null ? "Published" : x.DeadLetteredAt != null ? "DeadLettered" : "Pending",
        x.OccurredAt, x.CorrelationId, x.Attempts, x.NextAttemptAt, x.PublishedAt, x.DeadLetteredAt,
        x.LastError is null or "InvalidPayload" or "DeliveryFailed" ? x.LastError : "DeliveryFailed",
        Tag(x.RowVersion));

    private static string Tag(uint version) =>
        "\"" + Convert.ToBase64String(Encoding.UTF8.GetBytes(version.ToString(CultureInfo.InvariantCulture))) + "\"";

    private static AppException Invalid(string message) => new(400, "ValidationFailed", message);

    private static AppException Missing() => new(404, "ResourceNotFound", "投递记录不存在");

    private static (int Page, int Size) Paging(OrganizationIdentityAccessControlRequest request) =>
        (PageNumber(request, "page", 1, 1_000_000), PageNumber(request, "pageSize", 20, 100));

    private static int PageNumber(OrganizationIdentityAccessControlRequest request, string key, int fallback, int max) =>
        !request.Query.TryGetValue(key, out var value) ? fallback
        : int.TryParse(value, out var n) && n >= 1 && n <= max ? n
        : throw Invalid(key + " 超出允许范围");

    private static string? Filter(OrganizationIdentityAccessControlRequest request, string key, int max)
    {
        if (!request.Query.TryGetValue(key, out var value) || string.IsNullOrWhiteSpace(value))
            return null;
        if (value.Length > max || value.Any(char.IsControl))
            throw Invalid(key + " 格式无效");
        return value.Trim();
    }

    private static Guid? FilterId(OrganizationIdentityAccessControlRequest request, string key) =>
        Filter(request, key, 36) is not { } value ? null
        : Guid.TryParse(value, out var id) ? id : throw Invalid(key + " 必须为有效 UUID");
}
