using MESagent.Domain;
using MESagent.Domain.Entities;
using MESagent.Infrastructure.LotWaferCarrierGenealogy;
using Microsoft.EntityFrameworkCore;

namespace MESagent.Infrastructure.DispatchAndProductionExecution;

public sealed partial class ExecutionBackend
{
    public async Task RecoverAfterQualityRelease(Guid releaseId, Guid callerCommandId)
    {
        require("quality.release");
        if (db.Database.CurrentTransaction == null || callerCommandId == Guid.Empty)
            throw Error("TransactionRequired", "恢复必须处于质量命令事务中");
        commandId = callerCommandId;
        var release = await db.Set<LotRelease>().SingleOrDefaultAsync(x => x.Id == releaseId
            && x.ReleaseType == "Quality" && x.CommandId == commandId && x.ReleasedById == actor.UserId)
            ?? throw Error("InvalidQualityRelease", "缺少本次授权质量恢复凭据");
        var lot = await Lot(release.LotId);
        var step = await CurrentStep(lot);
        var run = await db.Set<ProcessRun>().SingleOrDefaultAsync(x => x.Id == release.ProcessRunId
            && x.StepExecutionId == step.Id && x.TrackOutAt != null && x.Status != "Running")
            ?? throw Error("ExecutionStateConflict", "当前加工尚未收尾");
        if (release.StepExecutionId != step.Id || lot.Status != "AwaitingDisposition"
            || step.Status != "AwaitingDisposition")
            throw Error("ExecutionStateConflict", "批次、到访或质量凭据不一致");
        var blocks = await Blocks(lot, false);
        if (blocks.Count > 0 || await db.Set<Issue>().AnyAsync(x => x.LotId == lot.Id && x.IsBlocking
            && x.Status != "Resolved" && x.Status != "Closed"))
            throw Error("LotBlocked", "恢复门禁未解除：" + string.Join("；", blocks));
        var members = await Members(lot);
        if (members.Count == 0 || members.Any(x => x.LifecycleStatus != "Active"
            || x.FabId != fabId || x.WorkOrderId != lot.WorkOrderId))
            throw Error("MembershipConflict", "当前参与片身份不完整");
        var participants = await db.Set<ProcessRunWafer>().Where(x => x.ProcessRunId == run.Id).ToListAsync();
        var ids = members.Select(x => x.Id).ToArray();
        var remaining = participants.Where(x => ids.Contains(x.WaferId)).ToArray();
        var removed = participants.Where(x => !ids.Contains(x.WaferId)).Select(x => x.WaferId).ToArray();
        if (remaining.Length != ids.Length || remaining.Any(x => x.ProcessedAt == null)
            || await db.Set<WaferDisposition>().CountAsync(x => removed.Contains(x.WaferId)
                && x.DispositionType == "Scrap" && x.LotIdAtEvent == lot.Id
                && x.ProcessRunId == run.Id) != removed.Length
            || await db.Set<Wafer>().CountAsync(x => removed.Contains(x.Id)
                && x.LifecycleStatus == "Scrapped") != removed.Length)
            throw Error("InvalidWaferSet", "原加工参与片与剩余/报废片不守恒");
        var time = LotRules.NextEventTime(Now, [run.TrackOutAt!.Value, step.EnteredAt]);
        var production = new ProductionBackend(db, fabId, actor, require, can, clock);
        if (release.RecoveryAction == "Continue")
        {
            if (run.Status != "Completed" || remaining.Any(x => x.Result != "Passed"))
                throw Error("FailedWafersRequireRetry", "仍有 Failed/中止结果，须报废或授权当前站重试");
            await RequiredWork(step);
            step.Status = "Completed";
            step.ExitedAt = time;
            lot.Status = "Running";
            Touch(step);
            Touch(lot);
            await db.SaveChangesAsync();
            var current = await db.Set<RouteStep>().SingleAsync(x => x.Id == step.RouteStepId);
            var next = await db.Set<RouteStep>().Where(x => x.RouteVersionId == lot.RouteVersionId
                && x.SequenceNo > current.SequenceNo).OrderBy(x => x.SequenceNo).FirstOrDefaultAsync();
            if (next == null)
                await production.CompleteAtRouteEnd(run.Id, commandId, releaseId);
            else
            {
                lot.Status = "Ready";
                lot.CurrentRouteStepId = next.Id;
                await production.EnterWaitingStep(lot.Id, next.Id, time, commandId, releaseId);
            }
        }
        else if (release.RecoveryAction == "Retry")
        {
            if (run.Status != "Aborted" && remaining.All(x => x.Result == "Passed"))
                throw Error("RetryNotRequired", "当前有效片均已通过，应审核继续");
            step.Status = "RetryAuthorized";
            step.ExitedAt = time;
            lot.Status = "Ready";
            Touch(step);
            Touch(lot);
            await db.SaveChangesAsync();
            await production.EnterWaitingStep(lot.Id, step.RouteStepId, time, commandId, releaseId);
        }
        else
            throw Error("InvalidQualityRelease", "质量凭据未指定恢复动作");
        Record(lot, "QualityRecovered", new { releaseId, run.Id, release.RecoveryAction, waferIds = ids });
        await db.SaveChangesAsync();
    }
}
