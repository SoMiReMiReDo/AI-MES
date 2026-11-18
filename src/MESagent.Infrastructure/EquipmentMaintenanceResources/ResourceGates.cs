using MESagent.Domain;
using MESagent.Domain.Entities;
using MESagent.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MESagent.Infrastructure.EquipmentMaintenanceResources;

public static partial class EquipmentResources
{
    public static async Task<bool> MaintenanceBlocked(MesDbContext db, Guid equipmentId, DateTime now)
    {
        var tools = db.Set<Tool>().Where(x => x.EquipmentId == equipmentId).Select(x => x.Id);
        return await db.Set<Tool>().AnyAsync(x => x.EquipmentId == equipmentId
                && (!x.IsActive || x.Status != "Available"))
            || await db.Set<MaintenanceSchedule>().AnyAsync(x => x.IsActive && x.NextDueAt <= now
                && (x.EquipmentId == equipmentId || x.ToolId != null && tools.Contains(x.ToolId.Value)))
            || await db.Set<MaintenanceEvent>().AnyAsync(x =>
                (x.EquipmentId == equipmentId || x.ToolId != null && tools.Contains(x.ToolId.Value))
                && (x.Status == "InProgress" || x.Status == "Completed"
                    || x.Status == "Planned" && x.ScheduledAt <= now));
    }

    public static async Task<DateTime?> MaterialExpiry(MesDbContext db, MaterialLot lot, Guid fabId)
    {
        var expiry = lot.ResourceExpiryAt;
        if (lot.ExpiryDate != null)
        {
            var timezone = await db.Set<Fab>().Where(x => x.Id == fabId).Select(x => x.Timezone).SingleAsync();
            var dateEnd = lot.ExpiryDate.Value.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
            var limit = TimeZoneInfo.ConvertTimeToUtc(dateEnd, TimeZoneInfo.FindSystemTimeZoneById(timezone));
            if (expiry == null || limit < expiry)
                expiry = limit;
        }
        return expiry;
    }

    public static async Task<string?> MaterialProblem(MesDbContext db, MaterialLot lot, Guid fabId, DateTime now)
    {
        var material = await db.Set<Material>().SingleAsync(x => x.Id == lot.MaterialId);
        if (!material.IsActive || material.UnitOfMeasure != lot.UnitOfMeasure
            || lot.Status is not ("Available" or "Released")
            || !await db.Set<MaterialType>().AnyAsync(x => x.Id == material.MaterialTypeId && x.IsActive)
            || !await db.Set<Warehouse>().AnyAsync(x => x.Id == lot.WarehouseId && x.FabId == fabId && x.IsActive)
            || !string.IsNullOrWhiteSpace(lot.HoldReason)
            || lot.HeldAt != null && (lot.ReleasedAt == null || lot.HeldAt > lot.ReleasedAt)
            || await db.Set<MaterialHold>().AnyAsync(x => x.MaterialLotId == lot.Id && x.ReleasedAt == null))
            return "耗材停用、被冻结、厂区/单位不符或未放行";
        var expiry = await MaterialExpiry(db, lot, fabId);
        return expiry != null && expiry <= now ? "耗材批次已过期" : null;
    }

    public static async Task<List<string>> Blocks(MesDbContext db, Guid fabId, Guid actorId, Guid equipmentId,
        Guid operationId, Guid recipeId, DateTime now)
    {
        var result = new List<string>();
        if (await MaintenanceBlocked(db, equipmentId, now))
            result.Add("设备/工具维护到期、维护未复机或工具不可用");
        var policy = await db.Set<EquipmentOperationPolicy>().SingleOrDefaultAsync(x => x.OperationId == operationId);
        var requireQualifications = policy?.RequireEquipmentQualification == true
            || policy?.RequireWorkerQualification == true;
        if (requireQualifications)
        {
            var rows = await db.Set<EquipmentQualification>().Where(x => x.EquipmentId == equipmentId
                && x.OperationId == operationId && (x.RecipeVersionId == null || x.RecipeVersionId == recipeId)
                && x.ValidFrom <= now).OrderByDescending(x => x.RecipeVersionId != null)
                .ThenByDescending(x => x.ValidFrom).ThenByDescending(x => x.CreatedAt).ToListAsync();
            var row = rows.FirstOrDefault();
            if (row == null || !EquipmentRules.Qualified(row.QualificationStatus, row.ValidFrom, row.ValidTo, now))
                result.Add("设备工艺资格缺失、过期或撤销");
        }
        if (requireQualifications)
        {
            var workerId = await db.Users.Where(x => x.Id == actorId && x.IsActive && x.AccountType == "Human")
                .Select(x => x.WorkerId).SingleOrDefaultAsync();
            if (workerId == null || !await db.Set<Worker>().AnyAsync(x => x.Id == workerId && x.FabId == fabId
                && x.IsActive))
                result.Add("当前账号未绑定本厂启用人员");
            else
            {
                var rows = await db.Set<WorkerQualification>().Where(x => x.WorkerId == workerId
                    && x.OperationId == operationId && (x.RecipeVersionId == null || x.RecipeVersionId == recipeId)
                    && x.ValidFrom <= now).OrderByDescending(x => x.RecipeVersionId != null)
                    .ThenByDescending(x => x.ValidFrom).ThenByDescending(x => x.CreatedAt).ToListAsync();
                var row = rows.FirstOrDefault();
                if (row == null || !EquipmentRules.Qualified(row.QualificationStatus, row.ValidFrom, row.ValidTo, now))
                    result.Add("操作员工艺资格缺失、过期或撤销");
            }
        }
        var mount = await db.Set<ReticleMount>().SingleOrDefaultAsync(x => x.EquipmentId == equipmentId
            && x.UnloadedAt == null);
        if (mount == null)
        {
            if (!string.IsNullOrEmpty(policy?.RequiredReticleLayer))
                result.Add("未装载工序要求的光罩");
        }
        else
        {
            var reticle = await db.Set<Reticle>().SingleAsync(x => x.Id == mount.ReticleId);
            if (reticle.FabId != fabId || !reticle.IsActive || reticle.Status != "Available"
                || !EquipmentRules.WithinLife(reticle.UsageCount, reticle.MaxUsageCount, reticle.UsableUntil, now)
                || !string.IsNullOrEmpty(policy?.RequiredReticleLayer) && reticle.LayerCode != policy.RequiredReticleLayer)
                result.Add("已装载光罩层次不符、不可用或寿命耗尽");
        }
        var materials = await (from m in db.Set<MaterialMount>()
                               join l in db.Set<MaterialLot>()
                               on m.MaterialLotId equals l.Id
                               where m.EquipmentId == equipmentId && m.UnmountedAt == null
                               select new { m, l }).ToListAsync();
        foreach (var item in materials)
        {
            var problem = await MaterialProblem(db, item.l, fabId, now);
            if (problem != null || item.m.UsableUntil == null || item.m.UsableUntil <= now
                || item.m.Unit != item.l.UnitOfMeasure)
                result.Add(problem ?? "已上机耗材寿命到期或单位不符");
        }
        var required = await db.Set<EquipmentMaterialRequirement>().Where(x => x.OperationId == operationId)
            .Select(x => x.MaterialId).ToListAsync();
        if (required.Any(id => !materials.Any(x => x.l.MaterialId == id)))
            result.Add("工序所需耗材尚未上机");
        return result.Distinct().ToList();
    }

    public static async Task<object> BeginRun(MesDbContext db, Guid fabId, ProcessRun run, DateTime now)
    {
        var mount = await db.Set<ReticleMount>().SingleOrDefaultAsync(x => x.EquipmentId == run.EquipmentId
            && x.UnloadedAt == null);
        if (mount != null)
        {
            var reticle = await db.Set<Reticle>().SingleAsync(x => x.Id == mount.ReticleId && x.FabId == fabId);
            reticle.UsageCount++;
            reticle.UpdatedAt = now;
            db.Add(new ReticleUsage
            {
                ReticleId = reticle.Id,
                ReticleMountId = mount.Id,
                ProcessRunId = run.Id,
                LoadedAt = run.TrackInAt
            });
        }
        var materials = await db.Set<MaterialMount>().Where(x => x.EquipmentId == run.EquipmentId
            && x.UnmountedAt == null).ToListAsync();
        foreach (var item in materials)
            db.Add(new MaterialRunUsage { MaterialMountId = item.Id, ProcessRunId = run.Id });
        return new { reticleMountId = mount?.Id, materialMountIds = materials.Select(x => x.Id).ToArray() };
    }

    public static async Task FinishRun(MesDbContext db, ProcessRun run, DateTime time)
    {
        foreach (var usage in await db.Set<ReticleUsage>().Where(x => x.ProcessRunId == run.Id
            && x.UnloadedAt == null).ToListAsync())
            usage.UnloadedAt = time;
    }
}
