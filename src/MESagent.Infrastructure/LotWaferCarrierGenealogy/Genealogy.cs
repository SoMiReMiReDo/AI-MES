using MESagent.Application;
using MESagent.Domain;
using MESagent.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MESagent.Infrastructure.LotWaferCarrierGenealogy;

public sealed partial class ProductionBackend
{
    private async Task<StepExecution> Waiting(Lot lot)
    {
        await Order(lot.WorkOrderId!.Value);
        await Unblocked(lot);
        await SupportedGates(lot);
        if (lot.Status != "Ready" || lot.CurrentRouteStepId == null)
            throw Error("LotStateConflict", "仅已投放且等待加工的批次可以拆合批", 409);
        if (await db.Set<QTimeWindow>().AnyAsync(x => x.LotId == lot.Id && x.EndedAt == null)
            || await db.Set<ReworkRecord>().AnyAsync(x => x.LotId == lot.Id))
            throw Error("DependencyNotReady", "活动 Q-time 或返工上下文尚不支持拆合批", 409);
        var step = await db.Set<StepExecution>().SingleOrDefaultAsync(x => x.LotId == lot.Id && x.ExitedAt == null);
        if (step is null || step.Status != "Waiting" || step.RouteStepId != lot.CurrentRouteStepId
            || step.RouteVersionId != lot.RouteVersionId
            || await db.Set<ProcessRun>().AnyAsync(x => x.StepExecutionId == step.Id))
            throw Error("LotStateConflict", "当前步骤不是未加工的等待位置", 409);
        var wafers = await MemberWafers(lot);
        if (wafers.Any(x => x.LifecycleStatus != "Active"))
            throw Error("InvalidWaferSet", "片清单包含非在制晶圆");
        var ids = wafers.Select(x => x.Id).ToArray();
        if (await db.Set<WaferRelease>().CountAsync(x => ids.Contains(x.WaferId)
            && x.WorkOrderId == lot.WorkOrderId) != wafers.Count)
            throw Error("InvalidBaseline", "缺少可信的逐片首次投放事实");
        return step;
    }

    private async Task ValidateSplit(Lot lot, SplitProductionLot input)
    {
        Text(input.Reason, 1000);
        if (input.NewLotNumber != null)
            Text(input.NewLotNumber, 64);
        await Waiting(lot);
        if (input.WaferIds is null || input.WaferIds.Length > 100
            || !LotRules.IsProperSubset((await Members(lot.Id)).Select(x => x.WaferId), input.WaferIds))
            throw Error("InvalidWaferSet", "拆批须选择源批的部分晶圆，至少留一片");
    }

    private async Task<List<Lot>> ValidateMerge(MergeProductionLots input)
    {
        Text(input.Reason, 1000);
        if (input.NewLotNumber != null)
            Text(input.NewLotNumber, 64);
        if (input.Sources is null || input.Sources.Length is < 2 or > 20
            || input.Sources.Any(x => x is null || x.WaferIds is null)
            || input.Sources.Select(x => x.LotId).Distinct().Count() != input.Sources.Length
            || input.Sources.Sum(x => x.WaferIds.Length) > 100)
            throw Error("IncompatibleLots", "请选择 2～20 个不同源批，总片数最多 100");
        var lots = new List<Lot>();
        var all = new HashSet<Guid>();
        decimal? size = null;
        foreach (var source in input.Sources.OrderBy(x => x.LotId))
        {
            var lot = await Lot(source.LotId);
            Match(source.Etag, lot.RowVersion);
            await Waiting(lot);
            var wafers = await MemberWafers(lot);
            if (source.WaferIds.Distinct().Count() != source.WaferIds.Length
                || !wafers.Select(x => x.Id).ToHashSet().SetEquals(source.WaferIds))
                throw Error("MembershipConflict", "源批完整片清单已经变化", 409);
            foreach (var wafer in wafers)
            {
                size ??= wafer.WaferSizeMm;
                if (!all.Add(wafer.Id) || size != wafer.WaferSizeMm)
                    throw Error("IncompatibleLots", "源片重复或尺寸不一致");
            }
            if (lots.FirstOrDefault() is Lot first && (lot.WorkOrderId != first.WorkOrderId
                || lot.ProductRevisionId != first.ProductRevisionId || lot.RouteVersionId != first.RouteVersionId
                || lot.SnapshotVersion != first.SnapshotVersion || lot.CurrentRouteStepId != first.CurrentRouteStepId))
                throw Error("IncompatibleLots", "工单、产品、路线、基线或工艺位置不一致");
            lots.Add(lot);
        }
        return lots;
    }

    private async Task<object> Preview(OrganizationIdentityAccessControlRequest r, string[] p)
    {
        if (p.Length == 4 && p[1] == "lots" && p[3] == "split-preview")
        {
            var lot = await Lot(Id(p[2]));
            var input = Body<SplitProductionLot>(r);
            await ValidateSplit(lot, input);
            return new
            {
                source = View(lot), selectedWaferIds = input.WaferIds,
                remainingCount = (await Members(lot.Id)).Count - input.WaferIds.Length,
                newCount = input.WaferIds.Length, lot.CurrentRouteStepId, lot.Priority,
                coverage = "F4Validated", missingDependencies = new[] { "F5 设备与实际 Recipe 门禁" }
            };
        }
        if (p.Length == 3 && p[1] == "lot-merges" && p[2] == "preview")
        {
            var input = Body<MergeProductionLots>(r);
            var lots = await ValidateMerge(input);
            return new
            {
                sources = lots.Select(View), newCount = input.Sources.Sum(x => x.WaferIds.Length),
                lots[0].CurrentRouteStepId, priority = lots.Max(x => x.Priority),
                coverage = "F4Validated", missingDependencies = new[] { "F5 设备与实际 Recipe 门禁" }
            };
        }
        throw Error("ResourceNotFound", "接口不存在", 404);
    }

    private async Task<Lot> DerivedLot(Lot source, string? number, int count, int priority, DateTime time)
    {
        var order = await Order(source.WorkOrderId!.Value);
        var target = new Lot
        {
            FabId = fabId, WorkOrderId = source.WorkOrderId, ProductRevisionId = source.ProductRevisionId,
            RouteVersionId = source.RouteVersionId, SnapshotVersion = source.SnapshotVersion,
            CurrentRouteStepId = source.CurrentRouteStepId, Status = "Ready", LotType = "Production",
            LotNumber = await Number(number, order.ProductId), LotAssignedAt = time, TargetQty = count,
            Priority = priority, ReleasedToRouteAt = time, ReleasedToRouteById = actor.UserId
        };
        db.Add(target);
        Touch(order);
        return target;
    }

    private async Task<OrganizationIdentityAccessControlResult> Split(Lot lot, SplitProductionLot input)
    {
        await ValidateSplit(lot, input);
        var oldStep = await Waiting(lot);
        var members = (await Members(lot.Id)).Where(x => input.WaferIds.Contains(x.WaferId)).ToList();
        var wafers = (await MemberWafers(lot)).Where(x => input.WaferIds.Contains(x.Id)).ToList();
        var time = LotRules.NextEventTime(Now, members.Select(x => x.ValidFrom));
        var target = await DerivedLot(lot, input.NewLotNumber, wafers.Count, lot.Priority, time);
        var evt = Genealogy("Split", Text(input.Reason, 1000), time);
        foreach (var member in members)
            member.ValidTo = time;
        // Flush closed intervals first: immediate unique/exclusion constraints must see the old membership closed.
        await db.SaveChangesAsync();
        foreach (var wafer in wafers)
        {
            Membership(evt, wafer, target, lot.Id);
            Touch(wafer);
        }
        var step = await EnterStep(target, target.CurrentRouteStepId!.Value, time);
        db.Add(new StepExecutionInheritance
        {
            GenealogyEventId = evt.Id, SourceStepExecutionId = oldStep.Id, TargetStepExecutionId = step.Id
        });
        Touch(lot);
        await TouchCarriers(wafers.Select(x => x.Id));
        Record(lot, "LotSplit", new { target.Id, waferIds = input.WaferIds, genealogyEventId = evt.Id });
        Record(target, "LotSplitReceived", new { sourceLotId = lot.Id, genealogyEventId = evt.Id });
        await db.SaveChangesAsync();
        return new(new
        {
            commandId, genealogyEventId = evt.Id, newLot = View(target),
            sourceLots = new[] { await LotView(lot) }
        });
    }

    private async Task<OrganizationIdentityAccessControlResult> Merge(MergeProductionLots input)
    {
        var lots = await ValidateMerge(input);
        var members = new List<LotMembership>();
        var steps = new List<StepExecution>();
        foreach (var lot in lots)
        {
            members.AddRange(await Members(lot.Id));
            steps.Add(await Waiting(lot));
        }
        var ids = members.Select(x => x.WaferId).ToArray();
        var wafers = await db.Set<Wafer>().Where(x => ids.Contains(x.Id)).ToListAsync();
        var time = LotRules.NextEventTime(Now, members.Select(x => x.ValidFrom)
            .Concat(steps.Select(x => x.EnteredAt)));
        var target = await DerivedLot(lots[0], input.NewLotNumber, ids.Length, lots.Max(x => x.Priority), time);
        var evt = Genealogy("Merge", Text(input.Reason, 1000), time);
        foreach (var member in members)
            member.ValidTo = time;
        foreach (var step in steps)
        {
            step.ExitedAt = time;
            step.Status = "Transferred";
        }
        foreach (var lot in lots)
        {
            lot.Status = "Merged";
            lot.ClosedAt = time;
            lot.CloseReason = "Merged";
            lot.CurrentRouteStepId = null;
        }
        await db.SaveChangesAsync();
        foreach (var wafer in wafers)
        {
            Membership(evt, wafer, target, members.Single(x => x.WaferId == wafer.Id).LotId);
            Touch(wafer);
        }
        var newStep = await EnterStep(target, target.CurrentRouteStepId!.Value, time);
        foreach (var source in steps)
            db.Add(new StepExecutionInheritance
            {
                GenealogyEventId = evt.Id, SourceStepExecutionId = source.Id, TargetStepExecutionId = newStep.Id
            });
        await TouchCarriers(ids);
        foreach (var lot in lots)
            Record(lot, "LotMerged", new { target.Id, genealogyEventId = evt.Id });
        Record(target, "LotMergeReceived", new { sourceLotIds = lots.Select(x => x.Id), genealogyEventId = evt.Id });
        await db.SaveChangesAsync();
        var sources = new List<object>();
        foreach (var lot in lots)
            sources.Add(await LotView(lot));
        return new(new { commandId, genealogyEventId = evt.Id, newLot = View(target), sourceLots = sources });
    }
}
