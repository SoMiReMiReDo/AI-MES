using MESagent.Application;
using MESagent.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MESagent.Infrastructure.EquipmentMaintenanceResources;

public sealed partial class EquipmentBackend
{
    private async Task OperationRecipe(Guid operationId, Guid? recipeId)
    {
        if (!await db.Set<Operation>().AnyAsync(x => x.Id == operationId && x.IsActive)
            || recipeId != null && !await db.Set<RecipeVersion>().AnyAsync(x => x.Id == recipeId
                && x.LifecycleStatus == "Released"))
            throw Error("InvalidQualification", "请选择启用工序及已发布配方", 422);
    }

    private static void ValidPeriod(DateTime from, DateTime? to)
    {
        if (from.Kind != DateTimeKind.Utc || to != null && (to.Value.Kind != DateTimeKind.Utc || to <= from))
            throw Error("InvalidPeriod", "有效期须为 UTC，截止必须晚于生效时间", 422);
    }

    private async Task<OrganizationIdentityAccessControlResult> Qualifications(
        OrganizationIdentityAccessControlRequest r, string[] p)
    {
        if (p[1] == "policies" && (p.Length == 2 && r.Method == "POST" || p.Length == 3 && r.Method == "PUT"))
        {
            var input = Body<SaveEquipmentOperationPolicy>(r);
            await OperationRecipe(input.OperationId, null);
            if (input.RequireEquipmentQualification != input.RequireWorkerQualification)
                throw Error("InvalidQualificationPolicy", "资格门禁须同时启用或关闭设备与人员资格", 422);
            if (input.RequiredMaterialIds == null || input.RequiredMaterialIds.Length > 30
                || input.RequiredMaterialIds.Distinct().Count() != input.RequiredMaterialIds.Length
                || await ConsumableMaterials.CountAsync(x => input.RequiredMaterialIds.Contains(x.Id)
                    && x.IsActive) != input.RequiredMaterialIds.Length)
                throw Error("InvalidMaterials", "所需耗材须唯一、启用，最多30项", 422);
            var created = p.Length == 2;
            var row = created ? new EquipmentOperationPolicy { OperationId = input.OperationId }
                : await db.Set<EquipmentOperationPolicy>().SingleOrDefaultAsync(x => x.Id == Id(p[2]))
                    ?? throw Error("ResourceNotFound", "门禁策略不存在", 404);
            if (!created)
            {
                Match(r.Version, row.RowVersion);
                if (row.OperationId != input.OperationId)
                    throw Error("PolicyIdentityConflict", "门禁策略不能更换工序");
            }
            row.RequireEquipmentQualification = input.RequireEquipmentQualification;
            row.RequireWorkerQualification = input.RequireWorkerQualification;
            row.RequiredReticleLayer = Text(input.RequiredReticleLayer, 64, false);
            if (created)
                db.Add(row);
            var prior = await db.Set<EquipmentMaterialRequirement>().Where(x => x.OperationId == row.OperationId)
                .ToListAsync();
            db.RemoveRange(prior.Where(x => !input.RequiredMaterialIds.Contains(x.MaterialId)));
            foreach (var id in input.RequiredMaterialIds.Where(id => !prior.Any(x => x.MaterialId == id)))
                db.Add(new EquipmentMaterialRequirement { OperationId = row.OperationId, MaterialId = id });
            return await Saved(row, "OperationPolicySaved", input, created);
        }
        if (p.Length == 2 && r.Method == "POST")
        {
            if (p[1] == "equipment-qualifications")
            {
                var input = Body<CreateEquipmentQualification>(r);
                await Equipment(input.EquipmentId);
                await OperationRecipe(input.OperationId, input.RecipeVersionId);
                ValidPeriod(input.ValidFrom, input.ValidTo);
                if (await db.Set<EquipmentQualification>().AnyAsync(x => x.EquipmentId == input.EquipmentId
                    && x.OperationId == input.OperationId && x.RecipeVersionId == input.RecipeVersionId
                    && x.QualificationStatus == "Qualified" && (x.ValidTo == null || x.ValidTo > input.ValidFrom)
                    && (input.ValidTo == null || x.ValidFrom < input.ValidTo)))
                    throw Error("QualificationOverlap", "同范围内已有重叠资格，请先撤销旧资格");
                var row = new EquipmentQualification
                {
                    EquipmentId = input.EquipmentId,
                    OperationId = input.OperationId,
                    RecipeVersionId = input.RecipeVersionId,
                    ValidFrom = input.ValidFrom,
                    ValidTo = input.ValidTo,
                    QualifiedById = actor.UserId,
                    QualificationStatus = "Qualified"
                };
                db.Add(row);
                return await Saved(row, "EquipmentQualified", input, true);
            }
            if (p[1] == "worker-qualifications")
            {
                var input = Body<CreateWorkerQualification>(r);
                if (!await db.Set<Worker>().AnyAsync(x => x.Id == input.WorkerId && x.FabId == fabId && x.IsActive))
                    throw Error("InvalidWorker", "人员不存在、停用或不属于本厂", 422);
                await OperationRecipe(input.OperationId, input.RecipeVersionId);
                ValidPeriod(input.ValidFrom, input.ValidTo);
                if (await db.Set<WorkerQualification>().AnyAsync(x => x.WorkerId == input.WorkerId
                    && x.OperationId == input.OperationId && x.RecipeVersionId == input.RecipeVersionId
                    && x.QualificationStatus == "Qualified" && (x.ValidTo == null || x.ValidTo > input.ValidFrom)
                    && (input.ValidTo == null || x.ValidFrom < input.ValidTo)))
                    throw Error("QualificationOverlap", "同范围内已有重叠资格，请先撤销旧资格");
                var row = new WorkerQualification
                {
                    WorkerId = input.WorkerId,
                    OperationId = input.OperationId,
                    RecipeVersionId = input.RecipeVersionId,
                    ValidFrom = input.ValidFrom,
                    ValidTo = input.ValidTo,
                    QualifiedById = actor.UserId,
                    QualificationStatus = "Qualified"
                };
                db.Add(row);
                return await Saved(row, "WorkerQualified", input, true);
            }
        }
        if (p.Length == 4 && p[3] == "revoke" && r.Method == "POST")
        {
            var reason = Text(Body<ReasonRequest>(r).Reason, 500);
            if (p[1] == "equipment-qualifications")
            {
                var row = await db.Set<EquipmentQualification>().SingleOrDefaultAsync(x => x.Id == Id(p[2]))
                    ?? throw Error("ResourceNotFound", "资格不存在", 404);
                await Equipment(row.EquipmentId);
                Match(r.Version, row.RowVersion);
                if (row.QualificationStatus != "Qualified")
                    throw Error("QualificationConflict", "该资格已撤销");
                row.QualificationStatus = "Revoked";
                return await Saved(row, "EquipmentQualificationRevoked", new { reason });
            }
            if (p[1] == "worker-qualifications")
            {
                var row = await db.Set<WorkerQualification>().SingleOrDefaultAsync(x => x.Id == Id(p[2])
                    && db.Set<Worker>().Any(w => w.Id == x.WorkerId && w.FabId == fabId))
                    ?? throw Error("ResourceNotFound", "资格不存在", 404);
                Match(r.Version, row.RowVersion);
                if (row.QualificationStatus != "Qualified")
                    throw Error("QualificationConflict", "该资格已撤销");
                row.QualificationStatus = "Revoked";
                return await Saved(row, "WorkerQualificationRevoked", new { reason });
            }
        }
        throw Error("ResourceNotFound", "接口不存在", 404);
    }
}
