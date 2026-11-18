using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using MESagent.Application;
using MESagent.Domain.Entities;
using MESagent.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MESagent.Infrastructure.EquipmentMaintenanceResources;

// The shared authenticated pipeline owns the transaction, access reload and global write lock.
public sealed partial class EquipmentBackend(
    MesDbContext db,
    Guid fabId,
    RequestActor actor,
    Action<string> require,
    TimeProvider clock) : IEquipmentMaintenanceResourcesBackend
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        RespectRequiredConstructorParameters = true
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
            EventType = "Equipment." + action,
            Payload = payload,
            OccurredAt = Now,
            CorrelationId = actor.CorrelationId
        });
    }

    private static readonly HashSet<string> GuidFields = new(StringComparer.OrdinalIgnoreCase)
    {
        "equipmentId", "equipmentGroupId", "equipmentTypeId", "areaId", "lineId", "reasonCodeId", "shiftId",
        "toolId", "scheduleId", "assignedToId", "workerId", "operationId", "recipeVersionId",
        "requiredMaterialIds", "reticleId", "materialId", "materialLotId", "equipmentStateId"
    };

    private static string Canonical(JsonElement value, string field = "") => value.ValueKind switch
    {
        JsonValueKind.Object => "{" + string.Join(",", value.EnumerateObject()
            .OrderBy(x => x.Name, StringComparer.Ordinal)
            .Select(x => JsonSerializer.Serialize(x.Name) + ":" + Canonical(x.Value, x.Name))) + "}",
        JsonValueKind.Array => "[" + string.Join(",", value.EnumerateArray().Select(x => Canonical(x, field))
            .OrderBy(x => x, StringComparer.Ordinal)) + "]",
        JsonValueKind.String => JsonSerializer.Serialize(GuidFields.Contains(field)
            && Guid.TryParse(value.GetString(), out var id) ? id.ToString() : value.GetString()),
        JsonValueKind.Number => value.GetDecimal().ToString("G29", CultureInfo.InvariantCulture),
        _ => value.GetRawText()
    };

    public async Task<OrganizationIdentityAccessControlResult> Execute(OrganizationIdentityAccessControlRequest r)
    {
        var p = r.Path.Split('/');
        if (p.Length < 2 || p[0] != "equipment")
            throw Error("ResourceNotFound", "接口不存在", 404);
        require(r.Method == "GET" ? "equipment.read" : Permission(p));
        if (r.Method == "GET")
        {
            var data = await Query(r, p);
            var version = data is Dictionary<string, object?> view && view.TryGetValue("etag", out var tag)
                ? tag as string : null;
            return new(data, version);
        }
        if (r.Method is not ("POST" or "PUT"))
            throw Error("ResourceNotFound", "接口不存在", 404);
        var key = Text(r.IdempotencyKey, 128);
        var path = string.Join('/', p.Select(x => Guid.TryParse(x, out var id) ? id.ToString() : x));
        var scope = "equipment:" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(
            $"{actor.UserId}:{fabId}:{r.Method}:{path}")));
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(Canonical(r.Body) + "|" + r.Version)));
        var receipt = await db.Set<CommandReceipt>().SingleOrDefaultAsync(x => x.Scope == scope
            && x.IdempotencyKey == key);
        if (receipt != null)
        {
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

    private static string Permission(string[] p) => p[1] switch
    {
        "events" => "equipment.simulate",
        "downtimes" or "reasons" or "shifts" => "equipment.downtime",
        "maintenance-plans" or "maintenance" or "resources" => p[^1] == "release"
            ? "equipment.release" : "equipment.maintenance",
        "equipment-qualifications" or "worker-qualifications" or "policies" => "equipment.qualify",
        "reticles" or "reticle-mounts" => "equipment.reticles",
        "consumables" or "consumable-lots" or "material-mounts" => "equipment.materials",
        _ => p[^1] == "state" ? "equipment.state" : "equipment.manage"
    };

    private async Task<Equipment> Equipment(Guid id) => await EquipmentResources.ScopedEquipment(db, fabId, id)
        ?? throw Error("ResourceNotFound", "设备不存在", 404);

    private async Task<OrganizationIdentityAccessControlResult> Saved(object entity, string action,
        object facts, bool created = false)
    {
        if (!created)
            Touch(entity);
        Record(entity, action, facts);
        await db.SaveChangesAsync();
        var view = View(entity);
        return new(view, (string?)view["etag"], created ? 201 : 200);
    }

    private async Task<OrganizationIdentityAccessControlResult> Command(
        OrganizationIdentityAccessControlRequest r, string[] p)
    {
        return p[1] switch
        {
            "types" or "groups" or "tools" or "chambers" => await Registry(r, p),
            "events" or "downtimes" or "reasons" or "shifts" => await Events(r, p),
            "maintenance-plans" or "maintenance" or "resources" => await Maintenance(r, p),
            "equipment-qualifications" or "worker-qualifications" or "policies" => await Qualifications(r, p),
            "reticles" or "reticle-mounts" or "consumables" or "consumable-lots" or "material-mounts"
                => await Consumables(r, p),
            _ => throw Error("ResourceNotFound", "接口不存在", 404)
        };
    }
}
