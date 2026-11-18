using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using MESagent.Application;
using MESagent.Domain.Entities;
using MESagent.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MESagent.Infrastructure.MaterialConsumptionInventory;

// The authenticated pipeline supplies authorization and owns the transaction and shared write lock.
internal sealed partial class MaterialBackend(
    MesDbContext db,
    Guid fabId,
    RequestActor actor,
    Action<string> require,
    Func<string, bool> can,
    TimeProvider clock)
{
    internal const string ReadPermission = "materials.wafer-sources.read";
    private const string ManagePermission = "materials.wafer-sources.manage";
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        RespectRequiredConstructorParameters = true,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
    };

    private DateTime Now => clock.GetUtcNow().UtcDateTime;
    private static AppException NotFound() => new(404, "ResourceNotFound", "硅片来料对象不存在");
    private static AppException Conflict(string message) => new(409, "RegistrationConflict", message);
    private static AppException Invalid(string message) => new(422, "InvalidRequest", message);

    private static string Text(string? text, int max)
    {
        var value = text?.Trim() ?? "";
        if (value.Length == 0 || value.Length > max)
            throw Invalid($"文本长度应为 1～{max}");
        return value;
    }

    private static Guid Id(string value) => Guid.TryParse(value, out var id) ? id : throw NotFound();

    private static T Body<T>(OrganizationIdentityAccessControlRequest r) =>
        r.Body.Deserialize<T>(Json) ?? throw Invalid("请求不能为空");

    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

    public async Task<OrganizationIdentityAccessControlResult> Execute(OrganizationIdentityAccessControlRequest r)
    {
        var path = r.Path.Split('/');
        if (r.Method == "GET")
        {
            require(path[1] == "wafer-origin-options" ? "production.lots.create" : ReadPermission);
            return new(await Query(r, path));
        }

        require(ManagePermission);
        object input = path[1] switch
        {
            "wafer-materials" => Body<RegisterWaferMaterial>(r),
            "wafer-sources" => Body<RegisterWaferMaterialSource>(r),
            "wafer-lots" => Body<RegisterWaferMaterialLot>(r),
            _ => throw NotFound()
        };
        var key = Text(r.IdempotencyKey, 128);
        var scope = "materials:" + Hash($"{actor.UserId}:{fabId}:{r.Method}:{r.Path}");
        var hash = Hash(JsonSerializer.Serialize(input, Json));
        var receipt = await db.Set<CommandReceipt>().SingleOrDefaultAsync(x =>
            x.Scope == scope && x.IdempotencyKey == key);
        if (receipt != null)
        {
            if (receipt.RequestHash != hash)
                throw new AppException(409, "IdempotencyConflict", "同一幂等键对应不同参数");
            return JsonSerializer.Deserialize<OrganizationIdentityAccessControlResult>(receipt.Result!, Json)!
                with { Replayed = true };
        }

        var time = Now;
        var commandId = Guid.CreateVersion7();
        var (entity, id, data) = input switch
        {
            RegisterWaferMaterial material => await RegisterMaterial(material),
            RegisterWaferMaterialSource source => await RegisterSource(source),
            RegisterWaferMaterialLot lot => await RegisterLot(lot, time),
            _ => throw NotFound()
        };
        var action = entity + "Registered";
        var payload = JsonSerializer.Serialize(new { schemaVersion = 1, commandId, fabId, facts = data }, Json);
        db.Add(new AuditLog
        {
            FabId = fabId,
            Module = "materials",
            UserId = actor.UserId,
            EntityType = entity,
            EntityId = id,
            Action = action,
            AfterState = payload,
            OccurredAt = time,
            CorrelationId = actor.CorrelationId,
            SourceChannel = "Web"
        });
        db.Add(new BusinessEvent
        {
            UserId = actor.UserId,
            EntityType = entity,
            EntityId = id,
            EventType = "Materials." + action,
            Payload = payload,
            OccurredAt = time,
            CorrelationId = actor.CorrelationId
        });
        var result = new OrganizationIdentityAccessControlResult(data, Status: 201,
            Location: input is RegisterWaferMaterialLot ? $"/api/v1/materials/wafer-lots/{id}" : null);
        db.Add(new CommandReceipt
        {
            Id = commandId,
            Scope = scope,
            IdempotencyKey = key,
            RequestHash = hash,
            ActorUserId = actor.UserId,
            CorrelationId = actor.CorrelationId,
            Status = "Succeeded",
            CompletedAt = time,
            Result = JsonSerializer.Serialize(result, Json)
        });
        return result;
    }
}
