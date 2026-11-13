using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using MESagent.Application;
using MESagent.Domain;
using MESagent.Domain.Entities;
using MESagent.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MESagent.Infrastructure.LotWaferCarrierGenealogy;

// Called only from the authenticated application pipeline, inside its transaction and shared write lock.
public sealed partial class ProductionBackend(
    MesDbContext db,
    Guid fabId,
    RequestActor actor,
    Action<string> require,
    Func<string, bool> can,
    TimeProvider clock) : ILotWaferCarrierGenealogyBackend, IProductionLifecycle
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
    };
    private DateTime Now => clock.GetUtcNow().UtcDateTime;
    private Guid commandId;
    private static AppException Error(string code, string message, int status = 422) => new(status, code, message);
    private static string Tag(uint version) => "\"" + Convert.ToBase64String(Encoding.UTF8.GetBytes(
        version.ToString(CultureInfo.InvariantCulture))) + "\"";

    private static void Match(string? expected, uint version)
    {
        if (string.IsNullOrEmpty(expected))
            throw Error("VersionRequired", "请读取并提交所有对象的版本", 428);
        if (expected != Tag(version))
            throw Error("VersionConflict", "对象已变化，请刷新后重新确认", 412);
    }

    private static T Body<T>(OrganizationIdentityAccessControlRequest r) =>
        r.Body.Deserialize<T>(Json) ?? throw Error("InvalidRequest", "请求不能为空");

    private static string Text(string? value, int max, bool required = true)
    {
        value = value?.Trim() ?? "";
        if (value.Length > max || required && value.Length == 0)
            throw Error("InvalidRequest", $"文本长度应为 {(required ? 1 : 0)}～{max}");
        return value;
    }

    private static Guid Id(string value) => Guid.TryParse(value, out var id)
        ? id : throw Error("ResourceNotFound", "对象不存在", 404);

    private async Task<Lot> Lot(Guid id) => await db.Set<Lot>().SingleOrDefaultAsync(x =>
        x.Id == id && x.FabId == fabId && x.WorkOrderId != null && x.SnapshotVersion != null)
        ?? throw Error("ResourceNotFound", "批次不存在或尚未完成历史归属核验", 404);

    private async Task<Wafer> Wafer(Guid id) => await db.Set<Wafer>().SingleOrDefaultAsync(x =>
        x.Id == id && x.FabId == fabId && x.WorkOrderId != null)
        ?? throw Error("ResourceNotFound", "晶圆不存在或尚未完成历史归属核验", 404);

    private async Task<Carrier> Carrier(Guid id) => await db.Set<Carrier>().SingleOrDefaultAsync(x =>
        x.Id == id && x.FabId == fabId) ?? throw Error("ResourceNotFound", "载具不存在", 404);

    private async Task<WorkOrder> Order(Guid id, bool released = true)
    {
        var order = await db.Set<WorkOrder>().SingleOrDefaultAsync(x => x.Id == id && x.FabId == fabId)
            ?? throw Error("ResourceNotFound", "工单不存在", 404);
        if (released && order.Status != "Released")
            throw Error("LotStateConflict", "必须关联已下达且未关闭的工单", 409);
        return order;
    }

    private async Task<List<LotMembership>> Members(Guid id) => await db.Set<LotMembership>()
        .Where(x => x.LotId == id && x.ValidTo == null).OrderBy(x => x.WaferId).ToListAsync();

    private async Task<List<Wafer>> MemberWafers(Lot lot)
    {
        var ids = (await Members(lot.Id)).Select(x => x.WaferId).ToArray();
        var wafers = await db.Set<Wafer>().Where(x => ids.Contains(x.Id)).OrderBy(x => x.Id).ToListAsync();
        if (wafers.Count == 0 || wafers.Any(x => x.FabId != fabId || x.WorkOrderId != lot.WorkOrderId))
            throw Error("MembershipConflict", "片清单为空或稳定归属不一致", 409);
        return wafers;
    }

    private async Task<string[]> Blocks(Lot lot)
    {
        var reasons = new List<string>();
        if (await db.Set<HoldRecord>().AnyAsync(x => x.LotId == lot.Id && x.ReleasedAt == null))
            reasons.Add("有效 Hold");
        if (await db.Set<WorkOrderStop>().AnyAsync(x => x.WorkOrderId == lot.WorkOrderId
            && (x.LotId == null || x.LotId == lot.Id) && x.ResumedAt == null))
            reasons.Add("工单停工");
        if (await (from run in db.Set<ProcessRun>() join step in db.Set<StepExecution>()
                   on run.StepExecutionId equals step.Id
                   where step.LotId == lot.Id && (run.Status == "Running" || run.TrackOutAt == null)
                   select run.Id).AnyAsync())
            reasons.Add("存在未结束加工");
        if (await db.Set<QualityTask>().AnyAsync(x => x.LotId == lot.Id && x.CompletedAt == null))
            reasons.Add("尚未完成的质量任务");
        if (lot.Status is "Running" or "AwaitingDisposition")
            reasons.Add("加工中或等待处置");
        return reasons.ToArray();
    }

    private async Task Unblocked(Lot lot)
    {
        var blocks = await Blocks(lot);
        if (blocks.Length > 0)
            throw Error("LotBlocked", string.Join("；", blocks), 409);
    }

    private void Touch(object entity)
    {
        db.Entry(entity).Property("UpdatedAt").CurrentValue = Now;
        db.Entry(entity).Property("UpdatedAt").IsModified = true;
    }

    private Dictionary<string, object?> View(object entity)
    {
        var values = JsonSerializer.SerializeToElement(entity, Json).EnumerateObject()
            .Where(x => x.Name != "rowVersion").ToDictionary(x => x.Name, x => (object?)x.Value.Clone());
        if (entity.GetType().GetProperty("RowVersion")?.GetValue(entity) is uint version)
            values["etag"] = Tag(version);
        return values;
    }

    private void Record(object entity, string action, object facts)
    {
        var id = (Guid)entity.GetType().GetProperty("Id")!.GetValue(entity)!;
        var payload = JsonSerializer.Serialize(new { schemaVersion = 1, commandId, fabId, facts }, Json);
        db.Add(new AuditLog
        {
            UserId = actor.UserId, EntityType = entity.GetType().Name, EntityId = id,
            Action = action, AfterState = payload, OccurredAt = Now,
            CorrelationId = actor.CorrelationId, SourceChannel = "Web"
        });
        db.Add(new BusinessEvent
        {
            UserId = actor.UserId, EntityType = entity.GetType().Name, EntityId = id,
            EventType = "Production." + action, Payload = payload, OccurredAt = Now,
            CorrelationId = actor.CorrelationId
        });
    }

    private static string Canonical(JsonElement value, string? field = null) => value.ValueKind switch
    {
        JsonValueKind.Object => "{" + string.Join(",", value.EnumerateObject()
            .OrderBy(x => x.Name, StringComparer.Ordinal)
            .Select(x => JsonSerializer.Serialize(x.Name) + ":" + Canonical(x.Value, x.Name))) + "}",
        JsonValueKind.Array => "[" + string.Join(",", value.EnumerateArray().Select(x => Canonical(x, field))
            .OrderBy(x => x, StringComparer.Ordinal)) + "]",
        JsonValueKind.String => JsonSerializer.Serialize(field != null && (field == "id" || field.EndsWith("Id") || field.EndsWith("Ids"))
            && Guid.TryParse(value.GetString(), out var id) ? id.ToString() : value.GetString()!.Trim()),
        JsonValueKind.Number => value.GetDecimal().ToString("G29", CultureInfo.InvariantCulture),
        _ => value.GetRawText()
    };

    public async Task<OrganizationIdentityAccessControlResult> Execute(OrganizationIdentityAccessControlRequest r)
    {
        var p = r.Path.Split('/');
        if (p.Length < 2 || p[0] != "production")
            throw Error("ResourceNotFound", "接口不存在", 404);
        var preview = r.Method == "POST" && p[^1] is "split-preview" or "preview";
        var permission = p[1] switch
        {
            "lots" => r.Method == "GET" ? (p[^1] == "genealogy" ? "genealogy.read" : "lots.read")
                : p.Length == 2 ? "lots.create" : r.Method == "PUT" ? "lots.manage"
                : p[^1] is "split" or "split-preview" ? "lots.split"
                : p[^1] is "release" or "cancel" ? "lots." + p[^1] : "lots.manage",
            "lot-merges" => "lots.merge",
            "wafers" => "wafers.read",
            "carriers" => r.Method == "GET" ? "carriers.read"
                : p[^1] == "move" ? "carriers.move" : "carriers.manage",
            "carrier-load-commands" => "carriers.load",
            "genealogy-events" => "genealogy.read",
            _ => throw Error("ResourceNotFound", "接口不存在", 404)
        };
        require("production." + permission);
        if (r.Method == "GET")
        {
            var data = await Query(r, p);
            var version = data is Dictionary<string, object?> view && view.TryGetValue("etag", out var tag)
                ? tag as string : null;
            return new(data, version);
        }
        if (r.Method is not ("POST" or "PUT"))
            throw Error("ResourceNotFound", "接口不存在", 404);
        if (preview)
            return new(await Preview(r, p));
        var key = Text(r.IdempotencyKey, 128);
        var canonicalPath = string.Join("/", p.Select(x => Guid.TryParse(x, out var id) ? id.ToString() : x));
        var scope = "production:" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(
            $"{actor.UserId}:{fabId}:{r.Method}:{canonicalPath}")));
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(
            Canonical(r.Body) + "|" + r.Version)));
        var receipt = await db.Set<CommandReceipt>().SingleOrDefaultAsync(x => x.Scope == scope
            && x.IdempotencyKey == key);
        if (receipt is not null)
        {
            if (receipt.RequestHash != hash)
                throw Error("IdempotencyConflict", "同一幂等键对应不同参数或版本", 409);
            // Scope includes the current authorized deployment Fab. Managed objects cannot change Fab.
            var replay = JsonSerializer.Deserialize<OrganizationIdentityAccessControlResult>(receipt.Result!, Json)!;
            return replay with { Replayed = true };
        }
        commandId = Guid.CreateVersion7();
        var result = await Mutate(r, p);
        await db.SaveChangesAsync();
        db.Add(new CommandReceipt
        {
            Id = commandId, Scope = scope, IdempotencyKey = key, RequestHash = hash,
            ActorUserId = actor.UserId, CorrelationId = actor.CorrelationId,
            Status = "Succeeded", CompletedAt = Now, Result = JsonSerializer.Serialize(result, Json)
        });
        return result;
    }

    private async Task<OrganizationIdentityAccessControlResult> Mutate(
        OrganizationIdentityAccessControlRequest r, string[] p)
    {
        if (p[1] == "lots")
        {
            if (p.Length == 2 && r.Method == "POST")
                return await CreateLot(Body<CreateProductionLot>(r));
            if (p.Length is 3 or 4)
            {
                var lot = await Lot(Id(p[2]));
                Match(r.Version, lot.RowVersion);
                if (p.Length == 3 && r.Method == "PUT")
                    return await UpdateLot(lot, Body<UpdateProductionLot>(r));
                if (p.Length == 4 && r.Method == "POST")
                {
                    if (p[3] == "release")
                    {
                        if (r.Body.EnumerateObject().Any())
                            throw Error("InvalidRequest", "投放无需客户端指定步骤或片集");
                        return await Release(lot);
                    }
                    if (p[3] == "cancel")
                        return await Cancel(lot, Body<ReasonRequest>(r));
                    if (p[3] == "split")
                        return await Split(lot, Body<SplitProductionLot>(r));
                }
            }
        }
        if (p[1] == "lot-merges" && p.Length == 2 && r.Method == "POST")
            return await Merge(Body<MergeProductionLots>(r));
        if (p[1] == "carrier-load-commands" && p.Length == 2 && r.Method == "POST")
            return await Loads(Body<ChangeCarrierLoads>(r));
        if (p[1] == "carriers")
        {
            if (p.Length == 2 && r.Method == "POST")
                return await CreateCarrier(Body<CreateProductionCarrier>(r));
            if (p.Length is 3 or 4)
            {
                var carrier = await Carrier(Id(p[2]));
                Match(r.Version, carrier.RowVersion);
                if (p.Length == 3 && r.Method == "PUT")
                    return await UpdateCarrier(carrier, Body<UpdateProductionCarrier>(r));
                if (p.Length == 4 && p[3] == "move" && r.Method == "POST")
                    return await MoveCarrier(carrier, Body<MoveProductionCarrier>(r));
            }
        }
        throw Error("ResourceNotFound", "接口不存在", 404);
    }
}
