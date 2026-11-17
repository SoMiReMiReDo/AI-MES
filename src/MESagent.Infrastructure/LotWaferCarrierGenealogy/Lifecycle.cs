using MESagent.Application;
using MESagent.Domain;
using MESagent.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MESagent.Infrastructure.LotWaferCarrierGenealogy;

public sealed partial class ProductionBackend
{
    public async Task<Guid> EnterWaitingStep(Guid lotId, Guid routeStepId, DateTime time, Guid callerCommandId,
        Guid? qualityReleaseId = null)
    {
        require(qualityReleaseId == null ? "execution.track-out" : "quality.release");
        RequireTransaction(callerCommandId);
        if (qualityReleaseId != null)
            await VerifyQualityRelease(qualityReleaseId.Value, lotId, callerCommandId);
        return (await EnterStep(await Lot(lotId), routeStepId, time)).Id;
    }

    // Internal collaboration capabilities. No HTTP route exposes these methods. The future F5/F6 caller
    // must use the authenticated transaction and its own permission; facts are independently revalidated here.
    public async Task CompleteAtRouteEnd(Guid runId, Guid callerCommandId, Guid? qualityReleaseId = null)
    {
        require(qualityReleaseId == null ? "execution.track-out" : "quality.release");
        RequireTransaction(callerCommandId);
        var run = await db.Set<ProcessRun>().SingleOrDefaultAsync(x => x.Id == runId)
            ?? throw Error("ResourceNotFound", "加工记录不存在", 404);
        var step = await db.Set<StepExecution>().SingleAsync(x => x.Id == run.StepExecutionId);
        var lot = await Lot(step.LotId);
        var release = qualityReleaseId == null ? null
            : await VerifyQualityRelease(qualityReleaseId.Value, lot.Id, callerCommandId);
        if (release != null && (release.ProcessRunId != run.Id || release.StepExecutionId != step.Id
            || release.RecoveryAction != "Continue"))
            throw Error("InvalidQualityRelease", "质量凭据与终点加工不一致", 409);
        var order = await Order(lot.WorkOrderId!.Value);
        await SupportedGates(lot);
        var existing = await db.Set<WaferCompletion>().Where(x => x.ProcessRunId == runId).ToListAsync();
        if (existing.Count > 0)
        {
            if (existing.Any(x => x.CommandId != callerCommandId) || lot.Status != "Completed")
                throw Error("LotStateConflict", "完成事实不属于同一命令或批次状态不一致", 409);
            return;
        }
        var final = await db.Set<RouteStep>().Where(x => x.RouteVersionId == lot.RouteVersionId)
            .OrderByDescending(x => x.SequenceNo).FirstAsync();
        if (run.Status != "Completed" || run.TrackOutAt == null || release == null && run.TrackOutById != actor.UserId
            || step.Status != "Completed" || step.ExitedAt == null || step.RouteStepId != final.Id
            || step.RouteVersionId != lot.RouteVersionId || lot.CurrentRouteStepId != final.Id
            || lot.Status is not ("Running" or "Ready"))
            throw Error("LotStateConflict", "缺少本批路线终点的可信完成加工", 409);
        if (await db.Set<HoldRecord>().AnyAsync(x => x.LotId == lot.Id && x.ReleasedAt == null)
            || await db.Set<WorkOrderStop>().AnyAsync(x => x.WorkOrderId == order.Id
                && (x.LotId == null || x.LotId == lot.Id) && x.ResumedAt == null)
            || await db.Set<QualityTask>().AnyAsync(x => x.LotId == lot.Id && x.CompletedAt == null))
            throw Error("LotBlocked", "质量或停工门禁未解除", 409);
        if (step.RequiresConfirmation && step.ConfirmedAt == null
            || await db.Set<ChecklistItem>().AnyAsync(x => x.RouteStepId == step.RouteStepId && x.IsRequired
                && !db.Set<ChecklistCompletion>().Any(c => c.StepExecutionId == step.Id && c.ChecklistItemId == x.Id))
            || await db.Set<OutputDefinition>().AnyAsync(x => x.RouteStepId == step.RouteStepId && x.IsRequired
                && !db.Set<OutputValue>().Any(v => v.StepExecutionId == step.Id && v.OutputId == x.Id
                    && (v.ValueText != null || v.ValueNumber != null || v.ValueBoolean != null || v.ValueDate != null)))
            || await db.Set<StepDocument>().AnyAsync(x => x.StepExecutionId == step.Id
                && x.RequiresValidation && x.ValidatedAt == null))
            throw Error("LotBlocked", "必填作业记录尚未完成", 409);
        var wafers = await MemberWafers(lot);
        var ids = wafers.Select(x => x.Id).ToArray();
        var participants = await db.Set<ProcessRunWafer>().Where(x => x.ProcessRunId == runId).ToListAsync();
        var activeParticipants = participants.Where(x => ids.Contains(x.WaferId)).ToList();
        var removedIds = participants.Where(x => !ids.Contains(x.WaferId)).Select(x => x.WaferId).ToArray();
        var removedAreScrapped = release != null && await db.Set<WaferDisposition>().CountAsync(x =>
            removedIds.Contains(x.WaferId) && x.DispositionType == "Scrap" && x.LotIdAtEvent == lot.Id
                && x.ProcessRunId == runId) == removedIds.Length
            && await db.Set<Wafer>().CountAsync(x => removedIds.Contains(x.Id)
                && x.LifecycleStatus == "Scrapped") == removedIds.Length;
        if (wafers.Any(x => x.LifecycleStatus != "Active")
            || !activeParticipants.Select(x => x.WaferId).ToHashSet().SetEquals(ids)
            || removedIds.Length > 0 && !removedAreScrapped
            || activeParticipants.Any(x => x.Result != "Passed" || x.ProcessedAt == null)
            || await db.Set<WaferRelease>().CountAsync(x => ids.Contains(x.WaferId)
                && x.WorkOrderId == order.Id) != ids.Length)
            throw Error("InvalidWaferSet", "终点参与片、逐片结果或投放事实不完整");
        var completionTime = release?.ReleasedAt ?? run.TrackOutAt.Value;
        var requiredSteps = await db.Set<RouteStep>().Where(x => x.RouteVersionId == lot.RouteVersionId)
            .Select(x => x.Id).ToListAsync();
        foreach (var wafer in wafers)
        {
            var passed = await (from participant in db.Set<ProcessRunWafer>()
                                join processing in db.Set<ProcessRun>() on participant.ProcessRunId equals processing.Id
                                join execution in db.Set<StepExecution>() on processing.StepExecutionId equals execution.Id
                                where participant.WaferId == wafer.Id && participant.Result == "Passed"
                                    && participant.ProcessedAt != null && processing.Status == "Completed"
                                    && execution.RouteVersionId == lot.RouteVersionId
                                select execution.RouteStepId).Distinct().ToListAsync();
            if (!requiredSteps.All(passed.Contains))
                throw Error("InvalidBaseline", "晶圆缺少绑定路线的必要加工记录");
            db.Add(new WaferCompletion
            {
                WaferId = wafer.Id,
                WorkOrderId = order.Id,
                LotIdAtCompletion = lot.Id,
                ProcessRunId = runId,
                RouteVersionId = lot.RouteVersionId,
                RouteStepId = final.Id,
                CompletedAt = completionTime,
                CompletedById = actor.UserId,
                CommandId = commandId
            });
            wafer.LifecycleStatus = "Completed";
            wafer.ProducedAt = completionTime;
        }
        lot.Status = "Completed";
        lot.CompletedAt = completionTime;
        lot.CurrentRouteStepId = null;
        lot.ProducedQty = wafers.Count;
        await TouchCarriers(ids);
        Record(lot, "LotCompleted", new { runId, waferIds = ids });
        await db.SaveChangesAsync();
        order.ProducedQty = await db.Set<WaferCompletion>().CountAsync(x => x.WorkOrderId == order.Id);
        if (order.ProducedQty == order.PlannedQty)
            order.CompletedAt = completionTime;
        await db.SaveChangesAsync();
    }

    public async Task CloseMembershipForScrap(Guid dispositionId, Guid callerCommandId)
    {
        require("quality.scrap");
        RequireTransaction(callerCommandId);
        var disposition = await db.Set<WaferDisposition>().SingleOrDefaultAsync(x => x.Id == dispositionId
            && x.DispositionType == "Scrap") ?? throw Error("ResourceNotFound", "报废处置不存在", 404);
        var lot = await Lot(disposition.LotIdAtEvent);
        var wafer = await Wafer(disposition.WaferId);
        if (wafer.WorkOrderId != lot.WorkOrderId || disposition.PerformedById != actor.UserId
            || disposition.ScrapEntryId == null)
            throw Error("InvalidBaseline", "缺少同工单授权报废凭据");
        var scrap = await db.Set<ScrapEntry>().SingleAsync(x => x.Id == disposition.ScrapEntryId);
        if (scrap.WorkOrderId != lot.WorkOrderId || scrap.Quantity != await db.Set<WaferDisposition>()
            .CountAsync(x => x.ScrapEntryId == scrap.Id && x.DispositionType == "Scrap"))
            throw Error("InvalidWaferSet", "报废数量与逐片处置不一致");
        if (await db.Set<WaferCompletion>().AnyAsync(x => x.WaferId == wafer.Id)
            || wafer.LifecycleStatus is "Completed" or "Cancelled")
            throw Error("LotStateConflict", "终结晶圆不能通过普通报废重写", 409);
        if (await (from run in db.Set<ProcessRun>()
                   join step in db.Set<StepExecution>()
                   on run.StepExecutionId equals step.Id
                   where step.LotId == lot.Id && run.Status == "Running"
                   select run.Id).AnyAsync())
            throw Error("ResourceInUse", "先由执行模块收尾当前加工", 409);
        var member = await db.Set<LotMembership>()
            .SingleOrDefaultAsync(x => x.WaferId == wafer.Id && x.ValidTo == null);
        if (member == null && wafer.LifecycleStatus == "Scrapped")
            return;
        if (member == null || member.LotId != lot.Id)
            throw Error("MembershipConflict", "报废时归属已变化", 409);
        var loads = await db.Set<CarrierLoad>().Where(x => x.WaferId == wafer.Id && x.UnloadedAt == null).ToListAsync();
        var time = LotRules.NextEventTime(Now, loads.Select(x => x.LoadedAt).Append(member.ValidFrom));
        await TouchCarriers([wafer.Id]);
        member.ValidTo = time;
        foreach (var load in loads)
            load.UnloadedAt = time;
        wafer.LifecycleStatus = "Scrapped";
        Touch(lot);
        Record(lot, "WaferScrapped", new { dispositionId, wafer.Id, time });
        await db.SaveChangesAsync();
        if (!await db.Set<LotMembership>().AnyAsync(x => x.LotId == lot.Id && x.ValidTo == null))
        {
            lot.Status = "Closed";
            lot.ClosedAt = time;
            lot.CloseReason = "AllScrapped";
            lot.CurrentRouteStepId = null;
            foreach (var step in await db.Set<StepExecution>()
                .Where(x => x.LotId == lot.Id && x.ExitedAt == null).ToListAsync())
            {
                step.ExitedAt = time;
                step.Status = "Disposed";
            }
        }
        Touch(await Order(lot.WorkOrderId!.Value, false));
        await db.SaveChangesAsync();
    }

    private async Task<LotRelease> VerifyQualityRelease(Guid id, Guid lotId, Guid callerCommandId) =>
        await db.Set<LotRelease>().SingleOrDefaultAsync(x => x.Id == id && x.LotId == lotId
            && x.ReleaseType == "Quality" && x.CommandId == callerCommandId && x.ReleasedById == actor.UserId
            && (x.RecoveryAction == "Continue" || x.RecoveryAction == "Retry"))
        ?? throw Error("InvalidQualityRelease", "缺少本次授权质量恢复凭据", 409);

    private void RequireTransaction(Guid id)
    {
        if (db.Database.CurrentTransaction == null || id == Guid.Empty)
            throw Error("TransactionRequired", "生产协作能力必须在调用用例的事务中执行", 409);
        commandId = id;
    }
}
