using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using MESagent.Application;
using MESagent.Domain.Entities;
using MESagent.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MESagent.Infrastructure.QualityExceptionDisposition;

// The shared authenticated pipeline owns the transaction, access reload and global write lock.
public sealed partial class QualityBackend(
    MesDbContext db,
    Guid fabId,
    RequestActor actor,
    Action<string> require,
    Func<string, bool> can,
    TimeProvider clock) : IQualityExceptionDispositionBackend
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
    };
    private Guid commandId;
    private DateTime Now => clock.GetUtcNow().UtcDateTime;
    private static AppException Error(string code, string message, int status = 409) => new(status, code, message);
    private static string Tag(uint value) => "\"" + Convert.ToBase64String(
        Encoding.UTF8.GetBytes(value.ToString(CultureInfo.InvariantCulture))) + "\"";

    private static void Match(string? expected, uint version)
    {
        if (string.IsNullOrEmpty(expected))
            throw Error("VersionRequired", "请先读取对象版本", 428);
        if (expected != Tag(version))
            throw Error("VersionConflict", "对象已变化，请刷新并重新确认", 412);
    }

    private static T Body<T>(OrganizationIdentityAccessControlRequest r) =>
        r.Body.Deserialize<T>(Json) ?? throw Error("InvalidRequest", "请求不能为空", 400);

    private static string Text(string? text, int max, bool required = true)
    {
        var value = text?.Trim() ?? "";
        if (value.Length > max || required && value.Length == 0)
            throw Error("InvalidRequest", $"文本长度应为 {(required ? 1 : 0)}～{max}", 422);
        return value;
    }

    private static Guid Id(string value) => Guid.TryParse(value, out var id)
        ? id : throw Error("ResourceNotFound", "对象不存在", 404);

    private void Touch(object entity)
    {
        db.Entry(entity).Property("UpdatedAt").CurrentValue = Now;
        db.Entry(entity).Property("UpdatedAt").IsModified = true;
    }

    private static Dictionary<string, object?> View(object entity)
    {
        var result = JsonSerializer.SerializeToElement(entity, Json).EnumerateObject()
            .Where(x => x.Name != "rowVersion").ToDictionary(x => x.Name, x => (object?)x.Value.Clone());
        if (entity.GetType().GetProperty("RowVersion")?.GetValue(entity) is uint version)
            result["etag"] = Tag(version);
        return result;
    }

    private void Record(object entity, string action, object facts)
    {
        var id = (Guid)entity.GetType().GetProperty("Id")!.GetValue(entity)!;
        var payload = JsonSerializer.Serialize(new { schemaVersion = 1, commandId, fabId, facts }, Json);
        db.Add(new AuditLog
        {
            UserId = actor.UserId,
            FabId = fabId,
            Module = "quality",
            EntityType = entity.GetType().Name,
            EntityId = id,
            Action = action,
            AfterState = payload,
            OccurredAt = Now,
            CorrelationId = actor.CorrelationId,
            SourceChannel = "Web"
        });
        db.Add(new BusinessEvent
        {
            UserId = actor.UserId,
            EntityType = entity.GetType().Name,
            EntityId = id,
            EventType = "Quality." + action,
            Payload = payload,
            OccurredAt = Now,
            CorrelationId = actor.CorrelationId
        });
    }

    private static string Canonical(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.Object => "{" + string.Join(",", value.EnumerateObject()
            .OrderBy(x => x.Name, StringComparer.Ordinal)
            .Select(x => JsonSerializer.Serialize(x.Name) + ":" + Canonical(x.Value))) + "}",
        JsonValueKind.Array => "[" + string.Join(",", value.EnumerateArray().Select(Canonical)
            .OrderBy(x => x, StringComparer.Ordinal)) + "]",
        JsonValueKind.String => JsonSerializer.Serialize(value.GetString()),
        JsonValueKind.Number => value.TryGetDecimal(out var number)
            ? number.ToString("G29", CultureInfo.InvariantCulture)
            : throw Error("InvalidRequest", "数值超出允许范围", 422),
        _ => value.GetRawText()
    };

    public async Task<OrganizationIdentityAccessControlResult> Execute(OrganizationIdentityAccessControlRequest r)
    {
        var p = r.Path.Split('/');
        if (p.Length < 2 || p[0] != "quality")
            throw Error("ResourceNotFound", "接口不存在", 404);
        var permission = r.Method == "GET" ? "read" : p[1] switch
        {
            "reason-codes" or "issue-types" or "specifications" or "templates" => "configuration",
            "issues" or "actions" => "issues",
            "holds" => "release",
            "lots" => p[^1] switch
            {
                "holds" => "hold",
                "release" => "release",
                "scrap" => "scrap",
                _ => throw Error("ResourceNotFound", "接口不存在", 404)
            },
            "checks" => "inspect",
            _ => throw Error("ResourceNotFound", "接口不存在", 404)
        };
        require("quality." + permission);
        if (r.Method == "GET")
        {
            var data = await Query(r, p);
            return new(data, data is Dictionary<string, object?> v ? v.GetValueOrDefault("etag") as string : null);
        }
        if (r.Method is not ("POST" or "PUT"))
            throw Error("ResourceNotFound", "接口不存在", 404);
        if (string.IsNullOrWhiteSpace(r.IdempotencyKey))
            throw Error("IdempotencyKeyRequired", "请提供幂等键", 400);
        var key = Text(r.IdempotencyKey, 128);
        var path = string.Join('/', p.Select(x => Guid.TryParse(x, out var id) ? id.ToString() : x));
        var scope = "quality:" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(
            $"{actor.UserId}:{fabId}:{r.Method}:{path}")));
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(Canonical(r.Body) + "|" + r.Version)));
        var receipt = await db.Set<CommandReceipt>().SingleOrDefaultAsync(x => x.Scope == scope
            && x.IdempotencyKey == key);
        if (receipt != null)
        {
            require("quality.read");
            if (receipt.RequestHash != hash)
                throw Error("IdempotencyConflict", "同一幂等键对应不同参数或版本");
            return JsonSerializer.Deserialize<OrganizationIdentityAccessControlResult>(receipt.Result!, Json)!
                with
            { Replayed = true };
        }
        commandId = Guid.CreateVersion7();
        var result = await Command(r, p);
        db.Add(new CommandReceipt
        {
            Id = commandId,
            Scope = scope,
            IdempotencyKey = key,
            RequestHash = hash,
            ActorUserId = actor.UserId,
            CorrelationId = actor.CorrelationId,
            Status = "Succeeded",
            CompletedAt = Now,
            Result = JsonSerializer.Serialize(result, Json)
        });
        return result;
    }

    private async Task<Lot> Lot(Guid id) => await db.Set<Lot>().SingleOrDefaultAsync(x => x.Id == id
        && x.FabId == fabId && x.WorkOrderId != null && x.SnapshotVersion != null)
        ?? throw Error("ResourceNotFound", "批次不存在", 404);

    private IQueryable<Issue> VisibleIssues() => VisibleIssues(db, fabId);

    internal static IQueryable<Issue> VisibleIssues(MesDbContext db, Guid fabId) => db.Set<Issue>().Where(x =>
        (x.LotId != null || x.WorkOrderId != null || x.WaferId != null || x.EquipmentId != null)
        && (x.LotId == null || db.Set<Lot>().Any(l => l.Id == x.LotId && l.FabId == fabId))
        && (x.WorkOrderId == null || db.Set<WorkOrder>().Any(o => o.Id == x.WorkOrderId && o.FabId == fabId))
        && (x.WaferId == null || db.Set<Wafer>().Any(w => w.Id == x.WaferId && w.FabId == fabId))
        && (x.EquipmentId == null || db.Set<Equipment>().Any(e => e.Id == x.EquipmentId
            && db.Set<Area>().Any(a => a.Id == e.AreaId && a.FabId == fabId))));

    private async Task<Issue> Issue(Guid id) => await VisibleIssues().SingleOrDefaultAsync(x => x.Id == id)
        ?? throw Error("ResourceNotFound", "异常不存在", 404);

    private async Task Equipment(Guid id)
    {
        if (!await db.Set<Equipment>().AnyAsync(x => x.Id == id && x.IsActive
            && db.Set<Area>().Any(a => a.Id == x.AreaId && a.FabId == fabId && a.IsActive)))
            throw Error("ResourceNotFound", "设备不存在或不可用", 404);
    }

    private async Task Assignee(Guid id)
    {
        if (!await db.Users.AnyAsync(x => x.Id == id && x.IsActive && x.AccountType == "Human"
            && db.Set<UserFabScope>().Any(s => s.UserId == x.Id && s.FabId == fabId)))
            throw Error("InvalidAssignee", "责任人须为本厂启用的人类账号", 422);
    }

    private async Task<ReasonCode> Reason(Guid id, string category) =>
        await db.Set<ReasonCode>().SingleOrDefaultAsync(x => x.Id == id && x.IsActive && x.Category == category)
        ?? throw Error("InvalidReason", "原因码不存在、已停用或类别不匹配", 422);

    private async Task<List<Wafer>> Members(Lot lot) => await (from m in db.Set<LotMembership>()
                                                               join w in db.Set<Wafer>() on m.WaferId equals w.Id
                                                               where m.LotId == lot.Id && m.ValidTo == null && w.FabId == fabId && w.WorkOrderId == lot.WorkOrderId
                                                               orderby w.Id
                                                               select w).ToListAsync();

    private async Task<StepExecution?> Step(Lot lot) => await db.Set<StepExecution>()
        .SingleOrDefaultAsync(x => x.LotId == lot.Id && x.ExitedAt == null
            && x.RouteStepId == lot.CurrentRouteStepId);

    private async Task<ProcessRun?> LatestRun(StepExecution? step) => step == null ? null
        : await db.Set<ProcessRun>().Where(x => x.StepExecutionId == step.Id)
            .OrderByDescending(x => x.TrackInAt).ThenByDescending(x => x.Id).FirstOrDefaultAsync();

    private async Task<OrganizationIdentityAccessControlResult> Saved(object row, string action,
        object facts, int status = 200)
    {
        Touch(row);
        Record(row, action, facts);
        await db.SaveChangesAsync();
        var view = View(row);
        return new(view, view["etag"] as string, status);
    }

    private async Task<OrganizationIdentityAccessControlResult> Command(
        OrganizationIdentityAccessControlRequest r, string[] p)
    {
        if (p[1] is "reason-codes" or "issue-types" or "specifications" or "templates")
            return await Configuration(r, p);
        if (p[1] == "checks" && p.Length == 2 && r.Method == "POST")
            return await Inspect(Body<RecordQualityCheck>(r));
        if (p[1] == "issues")
            return await IssueCommand(r, p);
        if (p[1] == "actions" && p.Length == 4 && r.Method == "POST")
            return await ActionCommand(r, p);
        if (p[1] == "holds" && p.Length == 4 && p[3] == "release" && r.Method == "POST")
            return await ReleaseHold(r, Id(p[2]), Body<ReasonRequest>(r));
        if (p[1] == "lots" && p.Length == 4 && r.Method == "POST")
        {
            var lot = await Lot(Id(p[2]));
            Match(r.Version, lot.RowVersion);
            return p[3] switch
            {
                "holds" => await Hold(lot, Body<HoldQualityLot>(r)),
                "release" => await Release(lot, Body<ReleaseQualityLot>(r)),
                "scrap" => await Scrap(lot, Body<ScrapQualityWafers>(r)),
                _ => throw Error("ResourceNotFound", "接口不存在", 404)
            };
        }
        throw Error("ResourceNotFound", "接口不存在", 404);
    }
}
