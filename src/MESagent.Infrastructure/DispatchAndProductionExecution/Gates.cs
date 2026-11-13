using System.Text.Json;
using MESagent.Domain;
using MESagent.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MESagent.Infrastructure.DispatchAndProductionExecution;

public sealed partial class ExecutionBackend
{
    private sealed record Candidate(
        Guid EquipmentId,
        string EquipmentCode,
        string EquipmentName,
        string EquipmentEtag,
        Guid EquipmentGroupId,
        Guid RecipeVersionId,
        string RecipeCode,
        string RecipeVersion,
        Guid ApplicabilityId,
        DateTime EffectiveFrom,
        DateTime? EffectiveTo,
        string ApplicabilityEtag);

    private async Task<Equipment> Equipment(Guid id) => await (
        from equipment in db.Set<Equipment>()
        join area in db.Set<Area>() on equipment.AreaId equals area.Id
        join equipmentGroup in db.Set<EquipmentGroup>() on equipment.EquipmentGroupId equals equipmentGroup.Id
        where equipment.Id == id && area.FabId == fabId && equipmentGroup.FabId == fabId
        select equipment).SingleOrDefaultAsync()
        ?? throw Error("ResourceNotFound", "设备不存在", 404);

    private async Task<List<Wafer>> Members(Lot lot)
    {
        return await (from member in db.Set<LotMembership>()
                      join wafer in db.Set<Wafer>() on member.WaferId equals wafer.Id
                      where member.LotId == lot.Id && member.ValidTo == null
                      orderby wafer.Id
                      select wafer).ToListAsync();
    }

    private async Task<List<string>> Blocks(Lot lot, bool starting)
    {
        var result = new List<string>();
        if (await db.Set<HoldRecord>().AnyAsync(x => x.LotId == lot.Id && x.ReleasedAt == null))
            result.Add("存在有效 Hold");
        if (await db.Set<WorkOrderStop>().AnyAsync(x => x.WorkOrderId == lot.WorkOrderId
            && (x.LotId == null || x.LotId == lot.Id) && x.ResumedAt == null))
            result.Add("工单或批次停工");
        if (await db.Set<QualityTask>().AnyAsync(x => x.LotId == lot.Id && x.CompletedAt == null))
            result.Add("存在未完成质量任务");
        if (!await db.Set<WorkOrder>().AnyAsync(x => x.Id == lot.WorkOrderId && x.FabId == fabId
            && x.Status == "Released"))
            result.Add("工单未下达或已终结");
        var ids = db.Set<RouteStep>().Where(x => x.RouteVersionId == lot.RouteVersionId).Select(x => x.Id);
        var product = await db.Set<ProductRevision>().Where(x => x.Id == lot.ProductRevisionId)
            .Select(x => x.ProductId).SingleAsync();
        if (await db.Set<QTimeRule>().AnyAsync(x => x.IsActive
            && (ids.Contains(x.StartRouteStepId) || ids.Contains(x.EndRouteStepId)))
            || await db.Set<QualityTrigger>().AnyAsync(x => x.IsActive && x.IsBlocking
                && (x.ProductId == null || x.ProductId == product)))
            result.Add("DependencyNotReady：Q-time 或自动质量门禁尚未接通");
        if (starting)
        {
            var step = await db.Set<RouteStep>().SingleAsync(x => x.Id == lot.CurrentRouteStepId);
            var operation = await db.Set<Operation>().SingleAsync(x => x.Id == step.OperationId);
            if (!operation.IsActive)
                result.Add("工序已停用");
            if (ExecutionRules.HasUnimplementedPolicy(step.Parameters)
                || ExecutionRules.HasUnimplementedPolicy(operation.Parameters))
                result.Add("DependencyNotReady：步骤工艺参数策略尚未接通");
            if (lot.Status != "Ready")
                result.Add("批次不是等待加工状态");
            var wafers = await Members(lot);
            var waferIds = wafers.Select(x => x.Id).ToArray();
            if (wafers.Count == 0 || wafers.Any(x => x.LifecycleStatus != "Active"
                    || x.FabId != fabId || x.WorkOrderId != lot.WorkOrderId)
                || await db.Set<WaferRelease>().CountAsync(x => waferIds.Contains(x.WaferId)
                    && x.WorkOrderId == lot.WorkOrderId && x.RouteVersionId == lot.RouteVersionId) != wafers.Count)
                result.Add("参与晶圆身份、生命周期或投放事实不完整");
        }
        return result;
    }

    private async Task<List<Candidate>> Candidates(Lot lot, StepExecution step)
    {
        var snapshot = await db.Set<WorkOrderSnapshot>().SingleAsync(x => x.WorkOrderId == lot.WorkOrderId
            && x.Version == lot.SnapshotVersion);
        using var baseline = JsonDocument.Parse(snapshot.Snapshot);
        var applications = baseline.RootElement.GetProperty("recipeApplicabilities").EnumerateArray()
            .Where(x => x.GetProperty("routeStepId").GetGuid() == step.RouteStepId)
            .Select(x => new
            {
                Id = x.GetProperty("id").GetGuid(),
                EquipmentId = x.GetProperty("equipmentId").GetGuid(),
                RecipeVersionId = x.GetProperty("recipeVersionId").GetGuid()
            }).ToArray();
        var ids = applications.Select(x => x.Id).ToArray();
        var routeStep = await db.Set<RouteStep>().SingleAsync(x => x.Id == step.RouteStepId);
        var operation = await db.Set<Operation>().SingleAsync(x => x.Id == routeStep.OperationId);
        var typeId = routeStep.EquipmentTypeId ?? operation.EquipmentTypeId;
        var now = Now;
        var rows = await (from app in db.Set<RecipeApplicability>()
                          join recipeVersion in db.Set<RecipeVersion>() on app.RecipeVersionId equals recipeVersion.Id
                          join recipe in db.Set<Recipe>() on recipeVersion.RecipeId equals recipe.Id
                          join equipment in db.Set<Equipment>() on app.EquipmentId equals equipment.Id
                          join area in db.Set<Area>() on equipment.AreaId equals area.Id
                          join equipmentGroup in db.Set<EquipmentGroup>()
                              on equipment.EquipmentGroupId equals equipmentGroup.Id
                          join type in db.Set<EquipmentType>() on equipment.EquipmentTypeId equals type.Id
                          where ids.Contains(app.Id) && app.RouteStepId == step.RouteStepId
                              && app.EffectiveFrom <= now && (app.EffectiveTo == null || app.EffectiveTo > now)
                              && recipeVersion.LifecycleStatus == "Released" && recipe.IsActive
                              && area.FabId == fabId && equipmentGroup.FabId == fabId && area.IsActive
                              && equipmentGroup.IsActive && type.IsActive && equipment.IsActive
                              && (typeId == null || equipment.EquipmentTypeId == typeId)
                              && equipment.OperationalState == "Idle"
                              && (equipment.LineId == null || db.Set<Line>().Any(x => x.Id == equipment.LineId
                                  && x.AreaId == area.Id && x.IsActive))
                              && !db.Set<ProcessRun>().Any(x => x.EquipmentId == equipment.Id && x.Status == "Running")
                          orderby equipment.Code, recipe.Code, recipeVersion.Version, app.Id
                          select new { app, equipment, recipe, recipeVersion }).ToListAsync();
        return rows.Where(x => applications.Any(a => a.Id == x.app.Id && a.EquipmentId == x.equipment.Id
                && a.RecipeVersionId == x.recipeVersion.Id))
            .Select(x => new Candidate(x.equipment.Id, x.equipment.Code, x.equipment.Name, Tag(x.equipment.RowVersion),
                x.equipment.EquipmentGroupId, x.recipeVersion.Id, x.recipe.Code, x.recipeVersion.Version, x.app.Id,
                x.app.EffectiveFrom, x.app.EffectiveTo, Tag(x.app.RowVersion)))
            .ToList();
    }

    private async Task<Candidate> StartGate(Lot lot, StepExecution step, Guid equipmentId, Guid recipeVersionId)
    {
        if (lot.Status != "Ready" || step.Status != "Waiting" || step.ExitedAt != null)
            throw Error("ExecutionStateConflict", "仅等待加工的当前步骤可启动或指派");
        var blocks = await Blocks(lot, true);
        if (blocks.Count > 0)
            throw Error(blocks.Any(x => x.StartsWith("DependencyNotReady")) ? "DependencyNotReady" : "LotBlocked",
                string.Join("；", blocks));
        var equipment = await Equipment(equipmentId);
        if (!equipment.IsActive)
            throw Error("EquipmentUnavailable", $"设备 {equipment.Code} 已停用");
        if (equipment.OperationalState != "Idle")
            throw Error("EquipmentUnavailable", $"设备 {equipment.Code} 当前人工状态为 {equipment.OperationalState}");
        if (await db.Set<ProcessRun>().AnyAsync(x => x.EquipmentId == equipmentId && x.Status == "Running"))
            throw Error("EquipmentOccupied", $"设备 {equipment.Code} 已被运行中的加工占用");
        return (await Candidates(lot, step)).FirstOrDefault(x => x.EquipmentId == equipmentId
            && x.RecipeVersionId == recipeVersionId)
            ?? throw Error("EquipmentRecipeUnavailable", "设备不可用、已占用，或配方不符合绑定基线/当前适用条件");
    }
}
