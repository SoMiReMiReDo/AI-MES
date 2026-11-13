using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using MESagent.Application;
using MESagent.Domain.Entities;
using MESagent.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MESagent.Infrastructure.ProductProcessVersionManagement;

// Uses the existing authenticated application pipeline and its transaction/security lock.
public sealed partial class ManufacturingBackend(
    MesDbContext db, Guid fabId, RequestActor actor, Action<string> require, TimeProvider clock)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private DateTime Now => clock.GetUtcNow().UtcDateTime;
    private static AppException Error(string message, int status = 422) => new(status, "ManufacturingRule", message);
    private static string Tag(uint version) => "\"" + Convert.ToBase64String(Encoding.UTF8.GetBytes(version.ToString())) + "\"";
    private static void Match(string? expected, uint version)
    {
        if (expected is null)
            throw Error("请先读取版本", 428);
        if (expected != Tag(version))
            throw Error("版本已变化，请刷新", 412);
    }
    private static string Text(JsonElement body, string field) =>
        body.TryGetProperty(field, out var value) ? value.GetString() ?? "" : "";
    private static void Fields(JsonElement body, IEnumerable<string> allowed)
    {
        if (body.ValueKind != JsonValueKind.Object)
            throw Error("请求必须为对象");
        foreach (var field in body.EnumerateObject())
            if (!allowed.Contains(field.Name))
                throw Error("不允许写入字段：" + field.Name);
    }
    private object View(object entity)
    {
        var result = JsonSerializer.SerializeToElement(entity, Json).Deserialize<Dictionary<string,
            JsonElement>>(Json)!;
        var version = (uint)entity.GetType().GetProperty("RowVersion")!.GetValue(entity)!;
        result["etag"] = JsonSerializer.SerializeToElement(Tag(version));
        return result;
    }
    private void Audit(object entity, string action, object? before = null)
    {
        db.Add(new AuditLog
        {
            UserId = actor.UserId, EntityType = entity.GetType().Name,
            EntityId = (Guid)entity.GetType().GetProperty("Id")!.GetValue(entity)!,
            Action = action, BeforeState = before is null ? null : JsonSerializer.Serialize(before, Json),
            AfterState = JsonSerializer.Serialize(entity, Json), OccurredAt = Now,
            CorrelationId = actor.CorrelationId, SourceChannel = "Web"
        });
    }
    public async Task<OrganizationIdentityAccessControlResult> Execute(OrganizationIdentityAccessControlRequest r)
    {
        var parts = r.Path.Split('/');
        var action = parts.Last();
        var permission = parts[0] == "planning"
            ? "planning.work-orders." + (r.Method == "GET" ? "read" : action is "release" or "cancel" or "close" or "recalculate" ? action : "manage")
            : r.Method == "GET" ? "engineering.read"
            : parts[1] == "documents" ? "engineering.documents.manage"
            : action is "release" or "obsolete" ? "engineering.release"
            : action == "allocate" ? "engineering.numbers.allocate" : "engineering.manage";
        require(permission);
        var idempotent = r.Method == "POST" && action is "release" or "obsolete" or "cancel" or "close" or "allocate" or "recalculate";
        CommandReceipt? receipt = null;
        if (idempotent)
        {
            if (string.IsNullOrWhiteSpace(r.IdempotencyKey) || r.IdempotencyKey.Length > 128)
                throw Error("需要有效的 Idempotency-Key");
            var scope = actor.UserId + ":" + r.Path;
            var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(r.Body.GetRawText())));
            receipt = await db.Set<CommandReceipt>().SingleOrDefaultAsync(x => x.Scope == scope
                && x.IdempotencyKey == r.IdempotencyKey);
            if (receipt is not null)
            {
                if (receipt.RequestHash != hash)
                    throw Error("幂等键对应不同请求", 409);
                return JsonSerializer.Deserialize<OrganizationIdentityAccessControlResult>(receipt.Result!, Json)!;
            }
            receipt = new CommandReceipt
            {
                Scope = scope, IdempotencyKey = r.IdempotencyKey!, RequestHash = hash,
                ActorUserId = actor.UserId, CorrelationId = actor.CorrelationId, Status = "Succeeded"
            };
        }
        var result = parts[0] == "planning" ? await Planning(r, parts) : await Engineering(r, parts);
        if (receipt is not null)
        {
            receipt.Result = JsonSerializer.Serialize(result, Json);
            receipt.CompletedAt = Now;
            db.Add(receipt);
        }
        return result;
    }

    private async Task<OrganizationIdentityAccessControlResult> Resource<T>(
        OrganizationIdentityAccessControlRequest r, string[] parts, string fields,
        Func<T, Task> validate, Action<T>? initialize = null) where T : class, new()
    {
        var query = db.Set<T>().AsQueryable();
        if (typeof(T) == typeof(WorkOrder)) query = query.Where(x => EF.Property<Guid>(x, "FabId") == fabId);
        if (typeof(T) == typeof(WorkOrder))
        {
            if (r.Query.TryGetValue("status", out var status)) query = query.Where(x => EF.Property<string>(x,
                "Status") == status);
            if (r.Query.TryGetValue("orderNo", out var number)) query = query.Where(x => EF.Property<string>(x,
                "OrderNo").Contains(number));
            if (r.Query.TryGetValue("productId", out var productText) && Guid.TryParse(productText,
                out var product)) query = query.Where(x => EF.Property<Guid>(x, "ProductId") == product);
        }
        foreach (var name in new[] { "RouteVersionId", "RouteStepId", "RecipeId", "RouteId", "ProductId" })
        {
            var property = typeof(T).GetProperty(name);
            if (property is null || !r.Query.TryGetValue(JsonNamingPolicy.CamelCase.ConvertName(name),
                out var value)) continue;
            if (!Guid.TryParse(value, out var parent)) throw Error("关联筛选 ID 无效");
            query = property.PropertyType == typeof(Guid?)
                ? query.Where(x => EF.Property<Guid?>(x, name) == parent)
                : query.Where(x => EF.Property<Guid>(x, name) == parent);
        }
        if (parts.Length == 2 && r.Method == "GET")
        {
            var page = r.Query.TryGetValue("page", out var pv) && int.TryParse(pv, out var n) ? Math.Clamp(n, 1,
                1000000) : 1;
            var size = r.Query.TryGetValue("pageSize", out var sv) && int.TryParse(sv, out n) ? Math.Clamp(n, 1,
                100) : 20;
            var rows = await query.OrderBy(x => EF.Property<Guid>(x,
                "Id")).Skip((page - 1) * size).Take(size).ToListAsync();
            return new(new { items = rows.Select(View), total = await query.CountAsync(), page, pageSize = size });
        }
        T entity;
        var create = parts.Length == 2 && r.Method == "POST";
        if (create) entity = new T();
        else
        {
            if (parts.Length != 3 || !Guid.TryParse(parts[2], out var id))
                throw Error("接口不存在", 404);
            entity = await query.SingleOrDefaultAsync(x => EF.Property<Guid>(x, "Id") == id)
                ?? throw Error("对象不存在", 404);
            if (r.Method == "GET")
                return new(View(entity), Tag((uint)typeof(T).GetProperty("RowVersion")!.GetValue(entity)!));
            if (r.Method != "PUT")
                throw Error("不支持的动作", 404);
            Match(r.Version, (uint)typeof(T).GetProperty("RowVersion")!.GetValue(entity)!);
            var state = typeof(T).GetProperty("LifecycleStatus") ?? typeof(T).GetProperty("Status");
            if (state is not null && (string?)state.GetValue(entity) != "Draft")
                throw Error("仅草稿允许编辑", 409);
        }
        object? before = create ? null : JsonSerializer.SerializeToElement(entity, Json);
        Fields(r.Body, fields.Split(' '));
        foreach (var item in r.Body.EnumerateObject())
        {
            var property = typeof(T).GetProperties().Single(x => JsonNamingPolicy.CamelCase.ConvertName(x.Name) == item.Name);
            if (!create && item.Name is "code" or "orderNo" or "fabId" or "routeId" or "recipeId" or "routeVersionId" or "routeStepId")
            {
                if (JsonSerializer.SerializeToElement(property.GetValue(entity),
                    Json).ToString() != item.Value.ToString())
                    throw Error("编码和厂区创建后不可变更");
            }
            var value = item.Value.Deserialize(property.PropertyType, Json);
            if (value is DateTime timestamp)
            {
                if (timestamp.Kind == DateTimeKind.Unspecified) throw Error("时间必须包含 UTC 或时区偏移");
                value = timestamp.ToUniversalTime();
            }
            property.SetValue(entity, value);
        }
        if (create) initialize?.Invoke(entity);
        await validate(entity);
        foreach (var prop in db.Entry(entity).Metadata.GetProperties())
        {
            var value = prop.PropertyInfo?.GetValue(entity);
            if (!prop.IsNullable && prop.ClrType == typeof(string) && string.IsNullOrWhiteSpace(value as string))
                throw Error(prop.Name + " 必填");
            if (value is string str && prop.GetMaxLength() is int max && str.Length > max)
                throw Error(prop.Name + " 超过长度限制");
        }
        typeof(T).GetProperty("UpdatedAt")!.SetValue(entity, Now);
        if (create) db.Add(entity);
        Audit(entity, create ? "Created" : "Updated", before);
        await db.SaveChangesAsync();
        return new(View(entity), Tag((uint)typeof(T).GetProperty("RowVersion")!.GetValue(entity)!), create ? 201 : 200);
    }
    private async Task<object> Page<T>(IQueryable<T> query, OrganizationIdentityAccessControlRequest request,
        Func<T, object>? project = null) where T : class
    {
        var page = request.Query.TryGetValue("page", out var text) && int.TryParse(text, out var n) ? Math.Clamp(n,
            1, 1000000) : 1;
        var size = request.Query.TryGetValue("pageSize", out text) && int.TryParse(text, out n) ? Math.Clamp(n, 1,
            100) : 20;
        var total = await query.CountAsync();
        var rows = await query.OrderBy(x => EF.Property<Guid>(x,
            "Id")).Skip((page - 1) * size).Take(size).ToListAsync();
        return new { items = rows.Select(x => project is null ? View(x) : project(x)), total, page, pageSize = size };
    }

    private static Task Valid() => Task.CompletedTask;
    private async Task<T> Get<T>(Guid id) where T : class =>
        await db.Set<T>().FindAsync(id) ?? throw Error("关联对象不存在", 404);
}
