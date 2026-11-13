using System.Text.Json;
using MESagent.Application;
using MESagent.Domain;
using MESagent.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MESagent.Infrastructure.ProductProcessVersionManagement;

public sealed partial class ManufacturingBackend
{
    private async Task<OrganizationIdentityAccessControlResult> Engineering(OrganizationIdentityAccessControlRequest r, string[] p)
    {
        if (p.Length == 3 && p[1] == "route-versions" && r.Method == "PUT" && r.Body.TryGetProperty("steps", out _))
            return await SaveRouteDraft(r, Guid.Parse(p[2]));
        if (p.Length >= 2 && p[1] == "documents")
            return await Documents(r, p);
        var extra = await EngineeringQuery(r, p);
        if (extra is not null)
            return extra;
        if (p.Length == 4 && Guid.TryParse(p[2], out var parent) && p[3] is "versions" or "revisions")
        {
            var kind = p[1] switch { "products" => "product-revisions", "routes" => "route-versions",
                "recipes" => "recipe-versions", _ => "" };
            var field = p[1] switch { "products" => "productId", "routes" => "routeId", _ => "recipeId" };
            if (kind == "")
                throw Error("接口不存在", 404);
            if (r.Method == "POST")
            {
                var values = r.Body.Deserialize<Dictionary<string, JsonElement>>(Json)!;
                values[field] = JsonSerializer.SerializeToElement(parent);
                return await Engineering(r with { Body = JsonSerializer.SerializeToElement(values),
                    Path = "engineering/" + kind }, ["engineering", kind]);
            }
            if (r.Method == "GET")
            {
                object rows = kind switch
                {
                    "product-revisions" => (await db.Set<ProductRevision>().Where(x => x.ProductId == parent).ToListAsync()).Select(View),
                    "route-versions" => (await db.Set<RouteVersion>().Where(x => x.RouteId == parent).ToListAsync()).Select(View),
                    _ => (await db.Set<RecipeVersion>().Where(x => x.RecipeId == parent).ToListAsync()).Select(View)
                };
                return new(rows);
            }
        }
        if (r.Method == "POST" && p.Length == 4 && Guid.TryParse(p[2], out var id) && p[3] is "release" or "obsolete")
            return await Publish(r, p[1], id, p[3]);
        if (p.Length == 4 && p[1] == "number-sequences" && Guid.TryParse(p[2], out var sequenceId))
        {
            var seq = await Get<NumberSequence>(sequenceId);
            var number = seq.Prefix + seq.NextNumber.ToString().PadLeft(seq.PadSize, '0') + seq.Suffix;
            if (r.Method != "POST" || p[3] is not ("preview" or "allocate"))
                throw Error("接口不存在", 404);
            if (p[3] == "allocate")
            {
                Match(r.Version, seq.RowVersion);
                if (await db.Set<CommandReceipt>().FromSqlInterpolated($"SELECT * FROM audit.command_receipts WHERE result->'data'->>'number' = {number}").AnyAsync())
                    throw Error("编号已分配，请调整规则或下一序号", 409);
                seq.NextNumber = checked(seq.NextNumber + 1);
                Audit(seq, "Allocated");
                await db.SaveChangesAsync();
            }
            return new(new { number, etag = Tag(seq.RowVersion) });
        }
        return p[1] switch
        {
            "products" => await Resource<Product>(r, p, "code name description unitOfMeasure category isActive",
                x => Valid()),
            "operations" => await Resource<Operation>(r, p,
                "code name description segmentType equipmentTypeId standardInstruction isActive", x => Valid()),
            "routes" => await Resource<Route>(r, p, "code name description productId isActive",
                async x => { if (x.ProductId is Guid product) await Get<Product>(product);
 }),
            "recipes" => await Resource<Recipe>(r, p, "code name description isActive", x => Valid()),
            "route-versions" => await Resource<RouteVersion>(r, p,
                "routeId version effectiveFrom effectiveTo changeReason", async x =>
            {
                await Get<Route>(x.RouteId);
 Period(x.EffectiveFrom, x.EffectiveTo);
            }, x => x.LifecycleStatus = "Draft"),
            "product-revisions" => await Resource<ProductRevision>(r, p,
                "productId revisionCode routeVersionId description effectiveFrom effectiveTo changeReason", async x =>
            {
                await Get<Product>(x.ProductId);
 Period(x.EffectiveFrom, x.EffectiveTo);
                if (x.RouteVersionId is Guid route) await Get<RouteVersion>(route);
            }, x => x.LifecycleStatus = "Draft"),
            "recipe-versions" => await Resource<RecipeVersion>(r, p, "recipeId version parameters checksum",
                async x => { await Get<Recipe>(x.RecipeId);
 }, x => x.LifecycleStatus = "Draft"),
            "route-steps" => await Resource<RouteStep>(r, p,
                "routeVersionId operationId sequenceNo name instruction equipmentTypeId requiresConfirmation parameters", async x =>
            {
                if ((await Get<RouteVersion>(x.RouteVersionId)).LifecycleStatus != "Draft")
                    throw Error("路线已冻结", 409);
                var operation = await Get<Operation>(x.OperationId);
                if (!operation.IsActive || x.SequenceNo <= 0)
                    throw Error("工序无效或顺序非正数");
                x.Instruction ??= operation.StandardInstruction;
            }),
            "checklist-items" => await Resource<ChecklistItem>(r, p, "routeStepId label isRequired sortOrder",
                async x => await DraftStep(x.RouteStepId)),
            "output-definitions" => await Resource<OutputDefinition>(r, p,
                "routeStepId key label valueType unit options isRequired sortOrder", async x =>
            {
                await DraftStep(x.RouteStepId);
                if (!new[] { "string", "decimal", "integer", "boolean", "datetime", "enum" }.Contains(x.ValueType))
                    throw Error("输出类型无效");
                if (x.ValueType == "enum" && string.IsNullOrWhiteSpace(x.Options))
                    throw Error("枚举必须提供选项");
                if (x.ValueType == "enum")
                {
                    using var choices = JsonDocument.Parse(x.Options!);
                    if (choices.RootElement.ValueKind != JsonValueKind.Array
                        || choices.RootElement.GetArrayLength() == 0
                        || choices.RootElement.EnumerateArray().Any(y => y.ValueKind != JsonValueKind.String
                            || string.IsNullOrWhiteSpace(y.GetString())))
                        throw Error("枚举选项必须为非空字符串数组");
                }
            }),
            "number-sequences" => await Resource<NumberSequence>(r, p,
                "name purpose productId prefix suffix padSize nextNumber", x =>
            {
                if (x.NextNumber < 1 || x.PadSize is < 1 or > 18)
                    throw Error("编号参数无效");
                var original = db.Entry(x).Property(y => y.NextNumber).OriginalValue;
                if (x.NextNumber < original)
                    throw Error("禁止回拨编号");
                return Valid();
            }, x => x.ResetPeriod = "None"),
            "recipe-applicabilities" => await Resource<RecipeApplicability>(r, p,
                "recipeVersionId routeStepId equipmentId effectiveFrom effectiveTo", async x =>
            {
                if ((await Get<RecipeVersion>(x.RecipeVersionId)).LifecycleStatus != "Released")
                    throw Error("配方未发布");
                var step = await Get<RouteStep>(x.RouteStepId);
                var equipment = await Get<Equipment>(x.EquipmentId);
                if ((await Get<Area>(equipment.AreaId)).FabId != fabId)
                    throw Error("设备不在当前厂区", 404);
                if (step.EquipmentTypeId is Guid requiredType && requiredType != equipment.EquipmentTypeId)
                    throw Error("设备类型不匹配");
                Period(x.EffectiveFrom, x.EffectiveTo);
                if (await db.Set<RecipeApplicability>().AnyAsync(y => y.Id != x.Id
                    && y.RecipeVersionId == x.RecipeVersionId
                    && y.RouteStepId == x.RouteStepId && y.EquipmentId == x.EquipmentId
                    && (y.EffectiveTo == null || y.EffectiveTo > x.EffectiveFrom)
                    && (x.EffectiveTo == null || y.EffectiveFrom < x.EffectiveTo))) throw Error("适用区间重叠", 409);
            }),
            _ => throw Error("接口不存在", 404)
        };
    }
    private static void Period(DateTime? start, DateTime? end)
    {
        if (start is not null && end <= start)
            throw Error("有效期结束必须晚于开始");
    }
    private async Task DraftStep(Guid id)
    {
        var step = await Get<RouteStep>(id);
        if ((await Get<RouteVersion>(step.RouteVersionId)).LifecycleStatus != "Draft")
            throw Error("步骤定义已冻结", 409);
    }
    private async Task ReleasedProduct(ProductRevision revision)
    {
        if (!(await Get<Product>(revision.ProductId)).IsActive)
            throw Error("产品已停用");
        var route = await Get<RouteVersion>(revision.RouteVersionId ?? throw Error("缺少路线版本"));
        var master = await Get<Route>(route.RouteId);
        if (!master.IsActive || master.ProductId is Guid product && product != revision.ProductId)
            throw Error("路线不适用于产品");
        if (!ManufacturingRules.IsEffective(route.LifecycleStatus, route.EffectiveFrom, route.EffectiveTo, Now))
            throw Error("路线版本无效");
    }
    private async Task<OrganizationIdentityAccessControlResult> Publish(OrganizationIdentityAccessControlRequest r,
        string kind, Guid id, string action)
    {
        Fields(r.Body, ["reason"]);
        if (string.IsNullOrWhiteSpace(Text(r.Body, "reason")))
            throw Error("必须填写原因");
        object entity = kind switch
        {
            "product-revisions" => await Get<ProductRevision>(id),
            "route-versions" => await Get<RouteVersion>(id),
            "recipe-versions" => await Get<RecipeVersion>(id),
            _ => throw Error("接口不存在", 404)
        };
        var type = entity.GetType();
        Match(r.Version, (uint)type.GetProperty("RowVersion")!.GetValue(entity)!);
        var state = type.GetProperty("LifecycleStatus")!;
        if ((string?)state.GetValue(entity) != (action == "release" ? "Draft" : "Released"))
            throw Error("状态不允许该动作", 409);
        if (action == "release")
        {
            if (entity is ProductRevision revision) await ReleasedProduct(revision);
            if (entity is RecipeVersion recipe && !(await Get<Recipe>(recipe.RecipeId)).IsActive)
                throw Error("Recipe 已停用");
            if (entity is RouteVersion route)
            {
                if (!(await Get<Route>(route.RouteId)).IsActive)
                    throw Error("路线已停用");
                var steps = await db.Set<RouteStep>().Where(x => x.RouteVersionId == id).ToListAsync();
                var stepIds = steps.Select(x => x.Id).ToArray();
                if (await db.Set<EngineeringDocument>().AnyAsync(x => x.EntityType == "RouteStep"
                    && stepIds.Contains(x.EntityId) && x.LifecycleStatus != "Released"))
                    throw Error("关联指导书尚未发布");
                if (steps.Count == 0)
                    throw Error("路线至少需要一个步骤");
                foreach (var step in steps)
                    if (!(await Get<Operation>(step.OperationId)).IsActive)
                        throw Error("存在停用工序");
            }
            type.GetProperty("ReleasedAt")!.SetValue(entity, Now);
            type.GetProperty("ReleasedById")!.SetValue(entity, actor.UserId);
        }
        state.SetValue(entity, action == "release" ? "Released" : "Obsolete");
        Audit(entity, action, new { reason = Text(r.Body, "reason") });
        await db.SaveChangesAsync();
        return new(View(entity), Tag((uint)type.GetProperty("RowVersion")!.GetValue(entity)!));
    }
}
