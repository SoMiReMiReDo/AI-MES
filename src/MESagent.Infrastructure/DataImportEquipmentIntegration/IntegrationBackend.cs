using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using MESagent.Application;
using MESagent.Domain.Entities;
using MESagent.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MESagent.Infrastructure.DataImportEquipmentIntegration;

internal sealed partial class IntegrationBackend(
    MesDbContext db,
    Guid fabId,
    RequestActor actor,
    Action<string> require,
    TimeProvider clock)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        RespectRequiredConstructorParameters = true
    };

    private DateTime Now
    {
        get
        {
            var now = clock.GetUtcNow().UtcDateTime;
            return now.AddTicks(-(now.Ticks % 10));
        }
    }

    private static AppException Error(int status, string code, string message) => new(status, code, message);

    private static AppException NotFound() => Error(404, "ResourceNotFound", "集成对象不存在");

    private static Guid Id(string value) => Guid.TryParse(value, out var id) ? id : throw NotFound();

    private static T Body<T>(OrganizationIdentityAccessControlRequest r) =>
        r.Body.Deserialize<T>(Json) ?? throw Error(400, "InvalidRequest", "请求不能为空");

    private static string Text(string? input, int max, string field)
    {
        var value = input?.Trim() ?? "";
        if (value.Length == 0 || value.Length > max || value.Any(char.IsControl))
            throw Error(422, "InvalidRequest", $"{field}应为 1～{max} 个可显示字符");
        return value;
    }

    private static string Tag(uint version) =>
        "\"" + Convert.ToBase64String(Encoding.UTF8.GetBytes(version.ToString(CultureInfo.InvariantCulture))) + "\"";

    public Task<OrganizationIdentityAccessControlResult> Execute(OrganizationIdentityAccessControlRequest request)
    {
        var path = request.Path.Split('/');
        switch (path[1])
        {
            case "connections":
                require(IntegrationAccess.Simulator);
                require("equipment.read");
                if (request.Method == "POST")
                    require("equipment.simulate");
                return Connections(request, path);
            case "work-order-imports":
                require(IntegrationAccess.WorkOrderImport);
                require("planning.work-orders.manage");
                return Imports(request, path);
            default:
                throw NotFound();
        }
    }

    private static async Task<PageView<T>> Page<T>(IQueryable<T> query, OrganizationIdentityAccessControlRequest r)
    {
        var page = r.Query.TryGetValue("page", out var p) && int.TryParse(p, out var n) ? Math.Clamp(n, 1, 1000000) : 1;
        var size = r.Query.TryGetValue("pageSize", out var s) && int.TryParse(s, out n) ? Math.Clamp(n, 1, 100) : 20;
        return new(await query.Skip((page - 1) * size).Take(size).ToArrayAsync(), await query.CountAsync(), page, size);
    }

    private void Audit(string type, Guid id, string action, object facts)
    {
        db.Add(new AuditLog
        {
            UserId = actor.UserId,
            FabId = fabId,
            Module = type == nameof(ImportJob) ? "imports" : "simulator",
            EntityType = type,
            EntityId = id,
            Action = action,
            AfterState = JsonSerializer.Serialize(new { fabId, facts }, Json),
            OccurredAt = Now,
            CorrelationId = actor.CorrelationId,
            SourceChannel = type == nameof(MachineMessage) ? "Simulator" : "Web"
        });
    }

    private async Task<OrganizationIdentityAccessControlResult> WithReceipt(
        OrganizationIdentityAccessControlRequest r,
        object input,
        Func<Task<OrganizationIdentityAccessControlResult>> action)
    {
        var key = Text(r.IdempotencyKey, 128, "幂等键");
        var path = string.Join('/', r.Path.Split('/').Select(x => Guid.TryParse(x, out var id) ? id.ToString() : x));
        var scope = "integration:" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(
            $"{actor.UserId}:{fabId}:{path}")));
        var hash = Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(new
        {
            input,
            r.Version
        }, Json)));
        var receipt = await db.Set<CommandReceipt>().SingleOrDefaultAsync(x => x.Scope == scope
            && x.IdempotencyKey == key);
        if (receipt != null)
        {
            if (receipt.RequestHash != hash)
                throw Error(409, "IdempotencyConflict", "同一幂等键对应不同参数或版本");
            return JsonSerializer.Deserialize<OrganizationIdentityAccessControlResult>(receipt.Result!, Json)!
                with { Replayed = true };
        }

        var result = await action();
        db.Add(new CommandReceipt
        {
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
}
