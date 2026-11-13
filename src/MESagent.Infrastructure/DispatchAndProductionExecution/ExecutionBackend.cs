using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using MESagent.Application;
using MESagent.Domain.Entities;
using MESagent.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MESagent.Infrastructure.DispatchAndProductionExecution;

// The shared authenticated pipeline owns the transaction, access reload and global write lock.
public sealed partial class ExecutionBackend(
    MesDbContext db,
    Guid fabId,
    RequestActor actor,
    Action<string> require,
    Func<string, bool> can,
    TimeProvider clock) : IDispatchAndProductionExecutionBackend
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
            UserId = actor.UserId, EntityType = entity.GetType().Name, EntityId = id,
            Action = action, AfterState = payload, OccurredAt = Now, CorrelationId = actor.CorrelationId,
            SourceChannel = "Web"
        });
        db.Add(new BusinessEvent
        {
            UserId = actor.UserId, EntityType = entity.GetType().Name, EntityId = id,
            EventType = "Execution." + action, Payload = payload, OccurredAt = Now,
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
        JsonValueKind.String => JsonSerializer.Serialize(Guid.TryParse(value.GetString(), out var id)
            ? id.ToString() : value.GetString()),
        JsonValueKind.Number => value.GetDecimal().ToString("G29", CultureInfo.InvariantCulture),
        _ => value.GetRawText()
    };

    public async Task<OrganizationIdentityAccessControlResult> Execute(OrganizationIdentityAccessControlRequest r)
    {
        var p = r.Path.Split('/');
        if (p.Length < 2)
            throw Error("ResourceNotFound", "接口不存在", 404);
        var permission = p[0] == "equipment"
            ? r.Method == "GET" ? "equipment.read" : p[^1] == "state" ? "equipment.state" : "equipment.manage"
            : r.Method == "GET" ? "execution.read" : "execution." + (p[^1] switch
            {
                "assign" => "dispatch", "records" => "record", "track-in" => "track-in",
                "track-out" => "track-out", "abort" => "abort",
                _ => throw Error("ResourceNotFound", "接口不存在", 404)
            });
        require(permission);
        if (r.Method == "GET")
        {
            var data = await Query(r, p);
            var version = data is Dictionary<string, object?> view && view.TryGetValue("etag", out var tag)
                ? tag as string : null;
            return new(data, version);
        }
        if (p[0] == "execution" && r.Method != "POST")
            throw Error("ResourceNotFound", "接口不存在", 404);
        if (r.Method is not ("POST" or "PUT"))
            throw Error("ResourceNotFound", "接口不存在", 404);
        var key = Text(r.IdempotencyKey, 128);
        var path = string.Join('/', p.Select(x => Guid.TryParse(x, out var id) ? id.ToString() : x));
        var scope = "execution:" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(
            $"{actor.UserId}:{fabId}:{r.Method}:{path}")));
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(Canonical(r.Body) + "|" + r.Version)));
        var receipt = await db.Set<CommandReceipt>().SingleOrDefaultAsync(x => x.Scope == scope
            && x.IdempotencyKey == key);
        if (receipt != null)
        {
            if (receipt.RequestHash != hash)
                throw Error("IdempotencyConflict", "同一幂等键对应不同参数或版本");
            return JsonSerializer.Deserialize<OrganizationIdentityAccessControlResult>(receipt.Result!, Json)!
                with { Replayed = true };
        }
        commandId = Guid.CreateVersion7();
        var result = p[0] == "equipment" ? await EquipmentCommand(r, p) : await Command(r, p);
        db.Add(new CommandReceipt
        {
            Id = commandId, Scope = scope, IdempotencyKey = key, RequestHash = hash,
            ActorUserId = actor.UserId, CorrelationId = actor.CorrelationId,
            Status = "Succeeded", CompletedAt = Now, Result = JsonSerializer.Serialize(result, Json)
        });
        return result;
    }

    private async Task<Lot> Lot(Guid id) => await db.Set<Lot>().SingleOrDefaultAsync(x => x.Id == id
        && x.FabId == fabId && x.WorkOrderId != null && x.SnapshotVersion != null)
        ?? throw Error("ResourceNotFound", "批次不存在", 404);

    private async Task<StepExecution> CurrentStep(Lot lot) => await db.Set<StepExecution>()
        .SingleOrDefaultAsync(x => x.LotId == lot.Id && x.ExitedAt == null
            && x.RouteStepId == lot.CurrentRouteStepId && x.RouteVersionId == lot.RouteVersionId)
        ?? throw Error("ExecutionStateConflict", "批次没有可执行的当前步骤");

    private async Task<(ProcessRun Run, StepExecution Step, Lot Lot)> Run(Guid id)
    {
        var run = await db.Set<ProcessRun>().SingleOrDefaultAsync(x => x.Id == id)
            ?? throw Error("ResourceNotFound", "加工记录不存在", 404);
        var step = await db.Set<StepExecution>().SingleAsync(x => x.Id == run.StepExecutionId);
        return (run, step, await Lot(step.LotId));
    }

    private async Task<OrganizationIdentityAccessControlResult> Command(
        OrganizationIdentityAccessControlRequest r, string[] p)
    {
        if (p.Length != 4)
            throw Error("ResourceNotFound", "接口不存在", 404);
        if (p[1] == "lots")
        {
            var lot = await Lot(Id(p[2]));
            Match(r.Version, lot.RowVersion);
            var step = await CurrentStep(lot);
            if (p[3] == "assign")
                return await Assign(lot, step, Body<AssignProductionLot>(r));
            if (p[3] == "track-in")
                return await TrackIn(lot, step, Body<TrackInProductionLot>(r));
        }
        if (p[1] == "runs")
        {
            var (run, step, lot) = await Run(Id(p[2]));
            Match(r.Version, run.RowVersion);
            if (run.Status != "Running" || step.Status != "Running" || lot.Status != "Running")
                throw Error("ExecutionStateConflict", "加工已结束或批次状态不匹配");
            if (p[3] == "records")
                return await Records(run, step, lot, Body<RecordProductionWork>(r));
            if (p[3] == "track-out")
                return await TrackOut(run, step, lot, Body<TrackOutProductionRun>(r));
            if (p[3] == "abort")
                return await Abort(run, step, lot, Body<ReasonRequest>(r));
        }
        throw Error("ResourceNotFound", "接口不存在", 404);
    }
}
