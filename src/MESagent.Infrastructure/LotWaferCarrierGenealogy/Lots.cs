using System.Text.Json;
using MESagent.Application;
using MESagent.Domain;
using MESagent.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MESagent.Infrastructure.LotWaferCarrierGenealogy;

public sealed partial class ProductionBackend
{
    private async Task<(ProductRevision Revision, RouteVersion Route)> Baseline(WorkOrder order)
    {
        var snapshot = await db.Set<WorkOrderSnapshot>().SingleOrDefaultAsync(x =>
            x.WorkOrderId == order.Id && x.Version == order.SnapshotVersion)
            ?? throw Error("InvalidBaseline", "工单下达基线不存在");
        using var json = JsonDocument.Parse(snapshot.Snapshot);
        var revisionId = json.RootElement.GetProperty("revision").GetProperty("id").GetGuid();
        var routeId = json.RootElement.GetProperty("route").GetProperty("id").GetGuid();
        var revision = await db.Set<ProductRevision>().SingleAsync(x => x.Id == revisionId);
        var route = await db.Set<RouteVersion>().SingleAsync(x => x.Id == routeId);
        if (revision.Id != order.ProductRevisionId || revision.ProductId != order.ProductId
            || revision.RouteVersionId != route.Id)
            throw Error("InvalidBaseline", "工单、产品及路线基线不一致");
        if (!ManufacturingRules.IsEffective(revision.LifecycleStatus, revision.EffectiveFrom, revision.EffectiveTo, Now)
            || !ManufacturingRules.IsEffective(route.LifecycleStatus, route.EffectiveFrom, route.EffectiveTo, Now)
            || !await db.Set<Product>().AnyAsync(x => x.Id == order.ProductId && x.IsActive)
            || !await db.Set<Route>().AnyAsync(x => x.Id == route.RouteId && x.IsActive))
            throw Error("InvalidBaseline", "产品或路线已失效，不允许新投放");
        return (revision, route);
    }

    private async Task<string> Number(string? supplied, Guid productId)
    {
        if (!string.IsNullOrWhiteSpace(supplied))
            return Text(supplied, 64);
        var candidates = await db.Set<NumberSequence>().Where(x => x.Purpose == "Lot"
            && (x.ProductId == productId || x.ProductId == null)).ToListAsync();
        var preferred = candidates.Where(x => x.ProductId == productId).ToList();
        if (preferred.Count == 0)
            preferred = candidates.Where(x => x.ProductId == null).ToList();
        if (preferred.Count != 1)
            throw Error("InvalidRequest", "请输入批号，或配置唯一的 Lot 编号规则");
        var sequence = preferred[0];
        var number = sequence.Prefix + sequence.NextNumber.ToString().PadLeft(sequence.PadSize, '0') + sequence.Suffix;
        sequence.NextNumber = checked(sequence.NextNumber + 1);
        Record(sequence, "NumberAllocated", new { number });
        return Text(number, 64);
    }

    private GenealogyEvent Genealogy(string type, string reason, DateTime time)
    {
        var evt = new GenealogyEvent
        {
            EventType = type, Reason = reason, OccurredAt = time, PerformedById = actor.UserId,
            IdempotencyKey = commandId.ToString(), CorrelationId = actor.CorrelationId
        };
        db.Add(evt);
        return evt;
    }

    private void Membership(GenealogyEvent evt, Wafer wafer, Lot target, Guid? source = null)
    {
        db.Add(new GenealogyDetail
        {
            EventId = evt.Id, WaferId = wafer.Id, FromLotId = source, ToLotId = target.Id
        });
        db.Add(new LotMembership
        {
            WaferId = wafer.Id, LotId = target.Id, ValidFrom = evt.OccurredAt, ChangeEventId = evt.Id
        });
    }

    private async Task<OrganizationIdentityAccessControlResult> CreateLot(CreateProductionLot input)
    {
        if (input.Wafers is null || input.Wafers.Length is < 1 or > 100 || input.Wafers.Any(x => x is null)
            || input.Priority is < 0 or > 100)
            throw Error("InvalidWaferSet", "需要 1～100 片且优先级为 0～100");
        var codes = input.Wafers.Select(x => Text(x.WaferCode, 64)).ToArray();
        if (codes.Distinct().Count() != codes.Length || input.Wafers.Any(x => x.WaferSizeMm <= 0
            || x.WaferSizeMm != decimal.Round(x.WaferSizeMm, 6))
            || input.Wafers.Select(x => x.WaferSizeMm).Distinct().Count() != 1)
            throw Error("InvalidWaferSet", "片号不得重复，尺寸须为相同正数且最多六位小数");
        if (await db.Set<Wafer>().AnyAsync(x => codes.Contains(x.WaferCode)))
            throw Error("CodeConflict", "片号已登记，不能覆盖永久身份", 409);
        var order = await Order(input.WorkOrderId);
        var baseline = await Baseline(order);
        var lot = new Lot
        {
            FabId = fabId, WorkOrderId = order.Id, SnapshotVersion = order.SnapshotVersion,
            ProductRevisionId = baseline.Revision.Id, RouteVersionId = baseline.Route.Id,
            LotNumber = await Number(input.LotNumber, order.ProductId), LotAssignedAt = Now,
            Priority = input.Priority, Notes = Text(input.Notes, 2000, false), LotType = "Production",
            Status = "Draft", TargetQty = input.Wafers.Length
        };
        db.Add(lot);
        var evt = Genealogy("Create", "工单建批", Now);
        var wafers = new List<Wafer>();
        for (var i = 0; i < input.Wafers.Length; i++)
        {
            var entry = input.Wafers[i];
            if (entry.OriginMaterialLotId is Guid source)
                await ValidateMaterial(source);
            var wafer = new Wafer
            {
                FabId = fabId, WorkOrderId = order.Id, WaferCode = codes[i], WaferSizeMm = entry.WaferSizeMm,
                OriginMaterialLotId = entry.OriginMaterialLotId,
                SupplierWaferCode = Text(entry.SupplierWaferCode, 128, false), LifecycleStatus = "Registered"
            };
            db.Add(wafer);
            wafers.Add(wafer);
            Membership(evt, wafer, lot);
        }
        Touch(order);
        Record(lot, "LotCreated", new { waferIds = wafers.Select(x => x.Id), evt.Id, order.SnapshotVersion });
        await db.SaveChangesAsync();
        var result = View(lot);
        result["commandId"] = commandId;
        result["waferIds"] = wafers.Select(x => x.Id);
        result["genealogyEventId"] = evt.Id;
        return new(result, Tag(lot.RowVersion), 201, Location: $"/api/v1/production/lots/{lot.Id}");
    }

    private async Task ValidateMaterial(Guid id)
    {
        var material = await db.Set<MaterialLot>().SingleOrDefaultAsync(x => x.Id == id)
            ?? throw Error("ResourceNotFound", "来源批次不可访问", 404);
        if (material.WarehouseId is Guid warehouse && !await db.Set<Warehouse>()
            .AnyAsync(x => x.Id == warehouse && x.FabId == fabId))
            throw Error("ResourceNotFound", "来源批次不可访问", 404);
        if (material.HeldAt != null && material.ReleasedAt == null
            || material.Status is "Held" or "Rejected" or "Expired" or "Cancelled"
            || material.ExpiryDate < DateOnly.FromDateTime(Now)
            || await db.Set<MaterialHold>().AnyAsync(x => x.MaterialLotId == id && x.ReleasedAt == null)
            || !await db.Set<Material>().AnyAsync(x => x.Id == material.MaterialId && x.IsActive))
            throw Error("LotBlocked", "来源物料已冻结、失效或停用", 409);
    }

    private async Task<OrganizationIdentityAccessControlResult> UpdateLot(Lot lot, UpdateProductionLot input)
    {
        if (lot.Status != "Draft" || input.Priority is < 0 or > 100)
            throw Error("LotStateConflict", "仅草稿允许修改备注及优先级", 409);
        var before = new { lot.Priority, lot.Notes };
        lot.Priority = input.Priority;
        lot.Notes = Text(input.Notes, 2000, false);
        Touch(lot);
        Record(lot, "LotUpdated", new { before, lot.Priority, lot.Notes });
        await db.SaveChangesAsync();
        return new(View(lot), Tag(lot.RowVersion));
    }

    private async Task<StepExecution> EnterStep(Lot lot, Guid stepId, DateTime time)
    {
        var step = await db.Set<RouteStep>().SingleOrDefaultAsync(x => x.Id == stepId
            && x.RouteVersionId == lot.RouteVersionId) ?? throw Error("InvalidBaseline", "路线步骤不匹配");
        var execution = new StepExecution
        {
            LotId = lot.Id, RouteVersionId = lot.RouteVersionId, RouteStepId = step.Id,
            VisitNo = 1, Status = "Waiting", Name = step.Name, Instruction = step.Instruction,
            RequiresConfirmation = step.RequiresConfirmation, EnteredAt = time
        };
        db.Add(execution);
        var snapshot = await db.Set<WorkOrderSnapshot>().SingleAsync(x => x.WorkOrderId == lot.WorkOrderId
            && x.Version == lot.SnapshotVersion);
        using var baseline = JsonDocument.Parse(snapshot.Snapshot);
        var documentIds = baseline.RootElement.GetProperty("documents").EnumerateArray()
            .Where(x => x.GetProperty("entityId").GetGuid() == stepId)
            .Select(x => x.GetProperty("id").GetGuid()).ToArray();
        var documents = await db.Set<EngineeringDocument>().Where(x => documentIds.Contains(x.Id)).ToListAsync();
        foreach (var document in documents)
            db.Add(new StepDocument
            {
                StepExecutionId = execution.Id, EngineeringDocumentId = document.Id,
                Name = document.OriginalFilename, IsMandatory = true, RequiresValidation = true
            });
        return execution;
    }

    private async Task SupportedGates(Lot lot)
    {
        var steps = db.Set<RouteStep>().Where(x => x.RouteVersionId == lot.RouteVersionId).Select(x => x.Id);
        var product = await db.Set<ProductRevision>().Where(x => x.Id == lot.ProductRevisionId)
            .Select(x => x.ProductId).SingleAsync();
        if (await db.Set<QTimeRule>().AnyAsync(x => x.IsActive
                && (steps.Contains(x.StartRouteStepId) || steps.Contains(x.EndRouteStepId)))
            || await db.Set<QualityTrigger>().AnyAsync(x => x.IsActive && x.IsBlocking
                && (x.ProductId == null || x.ProductId == product)))
            throw Error("DependencyNotReady", "本路线启用了尚未接通的 Q-time 或自动质量门禁", 409);
    }

    private async Task<OrganizationIdentityAccessControlResult> Release(Lot lot)
    {
        if (lot.Status != "Draft")
            throw Error("LotStateConflict", "仅草稿可以投放", 409);
        var order = await Order(lot.WorkOrderId!.Value);
        var baseline = await Baseline(order);
        if (lot.SnapshotVersion != order.SnapshotVersion || lot.RouteVersionId != baseline.Route.Id
            || lot.ProductRevisionId != baseline.Revision.Id)
            throw Error("InvalidBaseline", "批次基线不匹配");
        await Unblocked(lot);
        await SupportedGates(lot);
        var wafers = await MemberWafers(lot);
        if (wafers.Count != lot.TargetQty || wafers.Any(x => x.LifecycleStatus != "Registered"))
            throw Error("InvalidWaferSet", "当前片清单不符合登记基线");
        foreach (var source in wafers.Where(x => x.OriginMaterialLotId != null)
            .Select(x => x.OriginMaterialLotId!.Value).Distinct())
            await ValidateMaterial(source);
        var released = await db.Set<WaferRelease>().CountAsync(x => x.WorkOrderId == order.Id);
        if (released + wafers.Count > order.PlannedQty)
            throw Error("WorkOrderQuantityExceeded", $"计划 {order.PlannedQty}，已投放 {released}，本次 {wafers.Count}", 409);
        var first = await db.Set<RouteStep>().Where(x => x.RouteVersionId == lot.RouteVersionId)
            .OrderBy(x => x.SequenceNo).FirstOrDefaultAsync() ?? throw Error("InvalidBaseline", "路线没有步骤");
        var time = Now;
        lot.Status = "Ready";
        lot.CurrentRouteStepId = first.Id;
        lot.ReleasedToRouteAt = time;
        lot.ReleasedToRouteById = actor.UserId;
        await EnterStep(lot, first.Id, time);
        foreach (var wafer in wafers)
        {
            wafer.LifecycleStatus = "Active";
            db.Add(new WaferRelease
            {
                WaferId = wafer.Id, WorkOrderId = order.Id, InitialLotId = lot.Id,
                SnapshotVersion = lot.SnapshotVersion!.Value, RouteVersionId = lot.RouteVersionId,
                ReleasedAt = time, ReleasedById = actor.UserId, CommandId = commandId
            });
        }
        Touch(order);
        await TouchCarriers(wafers.Select(x => x.Id));
        Record(lot, "LotReleased", new { waferIds = wafers.Select(x => x.Id), first.Id });
        await db.SaveChangesAsync();
        return new(View(lot), Tag(lot.RowVersion));
    }

    private async Task<OrganizationIdentityAccessControlResult> Cancel(Lot lot, ReasonRequest input)
    {
        var reason = Text(input.Reason, 1000);
        if (lot.Status != "Draft" || lot.ReleasedToRouteAt != null
            || await db.Set<StepExecution>().AnyAsync(x => x.LotId == lot.Id))
            throw Error("LotStateConflict", "仅未投放且无加工事实的草稿可以取消", 409);
        await Unblocked(lot);
        var wafers = await MemberWafers(lot);
        if (wafers.Any(x => x.LifecycleStatus != "Registered"))
            throw Error("MembershipConflict", "晶圆状态不匹配", 409);
        var members = await Members(lot.Id);
        var ids = wafers.Select(x => x.Id).ToArray();
        var loads = await db.Set<CarrierLoad>()
            .Where(x => ids.Contains(x.WaferId) && x.UnloadedAt == null).ToListAsync();
        var time = LotRules.NextEventTime(Now, members.Select(x => x.ValidFrom).Concat(loads.Select(x => x.LoadedAt)));
        await TouchCarriers(ids);
        foreach (var member in members)
            member.ValidTo = time;
        foreach (var load in loads)
            load.UnloadedAt = time;
        foreach (var wafer in wafers)
            wafer.LifecycleStatus = "Cancelled";
        lot.Status = "Cancelled";
        lot.ClosedAt = time;
        lot.CloseReason = reason;
        Touch(await Order(lot.WorkOrderId!.Value, false));
        Record(lot, "LotCancelled", new { reason, waferIds = ids, loadIds = loads.Select(x => x.Id), time });
        await db.SaveChangesAsync();
        return new(View(lot), Tag(lot.RowVersion));
    }
}
