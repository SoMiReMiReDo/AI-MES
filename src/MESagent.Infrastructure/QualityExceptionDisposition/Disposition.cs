using MESagent.Application;
using MESagent.Domain;
using MESagent.Domain.Entities;
using MESagent.Infrastructure.DispatchAndProductionExecution;
using MESagent.Infrastructure.LotWaferCarrierGenealogy;
using Microsoft.EntityFrameworkCore;

namespace MESagent.Infrastructure.QualityExceptionDisposition;

public sealed partial class QualityBackend
{
    private void AddHold(Lot lot, Guid reasonId, Guid? issueId)
    {
        if (lot.Status is not ("Draft" or "Ready" or "Running" or "AwaitingDisposition"))
            throw Error("LotStateConflict", "终结批次不能新增 Hold");
        var hold = new HoldRecord
        {
            LotId = lot.Id,
            ReasonCodeId = reasonId,
            IssueId = issueId,
            HoldScope = "Lot",
            HeldAt = Now,
            HeldById = actor.UserId
        };
        db.Add(hold);
        Touch(lot);
        Record(hold, "LotHeld", new { lot.Id, reasonId, issueId });
    }

    private async Task ValidateIssueContext(Guid? issueId, Lot lot)
    {
        if (issueId != null)
        {
            var issue = await Issue(issueId.Value);
            if (issue.LotId != lot.Id)
                throw Error("InvalidContext", "异常不属于本批次", 422);
        }
    }

    private async Task<OrganizationIdentityAccessControlResult> Hold(Lot lot, HoldQualityLot input)
    {
        await Reason(input.ReasonCodeId, "Hold");
        await ValidateIssueContext(input.IssueId, lot);
        AddHold(lot, input.ReasonCodeId, input.IssueId);
        await db.SaveChangesAsync();
        return new(await LotView(lot), Tag(lot.RowVersion), 201);
    }

    private async Task<OrganizationIdentityAccessControlResult> ReleaseHold(
        OrganizationIdentityAccessControlRequest r, Guid id, ReasonRequest input)
    {
        var hold = await Find<HoldRecord>(id);
        var lot = await Lot(hold.LotId);
        Match(r.Version, hold.RowVersion);
        if (hold.ReleasedAt != null)
            throw Error("HoldAlreadyReleased", "该 Hold 已解除");
        if (hold.IssueId != null && (await Issue(hold.IssueId.Value)).Status is not ("Resolved" or "Closed"))
            throw Error("IssueUnresolved", "先完成关联异常处置");
        hold.ReleasedAt = LotRules.NextEventTime(Now, [hold.HeldAt]);
        hold.ReleasedById = actor.UserId;
        hold.ReleaseReason = Text(input.Reason, 2000);
        Touch(lot);
        return await Saved(hold, "HoldReleased", new { input.Reason, lot.Id });
    }

    private async Task<OrganizationIdentityAccessControlResult> Release(Lot lot, ReleaseQualityLot input)
    {
        var reason = Text(input.Reason, 2000);
        await ValidateIssueContext(input.IssueId, lot);
        if (input.Action is not ("None" or "Continue" or "Retry")
            || lot.Status is not ("Draft" or "Ready" or "AwaitingDisposition"))
            throw Error("LotStateConflict", "本批次状态或质量放行动作无效");
        if (!await db.Set<WorkOrder>().AnyAsync(x => x.Id == lot.WorkOrderId && x.Status == "Released")
            || await db.Set<HoldRecord>().AnyAsync(x => x.LotId == lot.Id && x.ReleasedAt == null)
            || await db.Set<WorkOrderStop>().AnyAsync(x => x.WorkOrderId == lot.WorkOrderId
                && (x.LotId == null || x.LotId == lot.Id) && x.ResumedAt == null)
            || await db.Set<QualityTask>().AnyAsync(x => x.LotId == lot.Id && x.CompletedAt == null)
            || await VisibleIssues().AnyAsync(x => x.LotId == lot.Id && x.IsBlocking
                && x.Status != "Resolved" && x.Status != "Closed"))
            throw Error("LotBlocked", "工单、Hold、质量任务或阻塞异常尚未解除");
        var step = await Step(lot);
        var run = await LatestRun(step);
        if (input.Action == "None" && lot.Status == "AwaitingDisposition"
            || input.Action != "None" && (lot.Status != "AwaitingDisposition" || step == null || run == null))
            throw Error("RecoveryRequired", "等待处置须明确继续或重试；其他状态仅能登记质量放行");
        var release = new LotRelease
        {
            LotId = lot.Id,
            ReleaseType = "Quality",
            ReleasedAt = LotRules.NextEventTime(Now, await db.Set<HoldRecord>()
                .Where(x => x.LotId == lot.Id && x.ReleasedAt != null).Select(x => x.ReleasedAt!.Value).ToListAsync()),
            ReleasedById = actor.UserId,
            Reason = reason,
            IssueId = input.IssueId,
            RecoveryAction = input.Action,
            StepExecutionId = step?.Id,
            ProcessRunId = run?.Id,
            CommandId = commandId
        };
        db.Add(release);
        await db.SaveChangesAsync();
        if (input.Action != "None")
            await new ExecutionBackend(db, fabId, actor, require, can, clock)
                .RecoverAfterQualityRelease(release.Id, commandId);
        Touch(lot);
        Record(release, "LotQualityReleased", input);
        await db.SaveChangesAsync();
        return new(new { release = View(release), lot = await LotView(lot) }, Tag(lot.RowVersion));
    }

    private async Task<OrganizationIdentityAccessControlResult> Scrap(Lot lot, ScrapQualityWafers input)
    {
        await Reason(input.ReasonCodeId, "Scrap");
        await ValidateIssueContext(input.IssueId, lot);
        if (lot.Status is not ("Ready" or "AwaitingDisposition") || lot.ReleasedToRouteAt == null)
            throw Error("LotStateConflict", "仅非加工中的已投放批次可报废");
        if (input.WaferIds == null || input.WaferIds.Length is < 1 or > 100
            || input.WaferIds.Distinct().Count() != input.WaferIds.Length
            || input.Quantity != input.WaferIds.Length)
            throw Error("InvalidWaferSet", "报废数量必须等于非空且唯一的逐片清单", 422);
        var members = await Members(lot);
        var wafers = members.Where(x => input.WaferIds.Contains(x.Id)).ToArray();
        if (wafers.Length != input.Quantity || wafers.Any(x => x.LifecycleStatus != "Active")
            || await db.Set<WaferRelease>().CountAsync(x => input.WaferIds.Contains(x.WaferId)
                && x.WorkOrderId == lot.WorkOrderId) != input.Quantity)
            throw Error("InvalidWaferSet", "报废片必须是本批已投放的有效晶圆", 422);
        var step = await Step(lot);
        var run = await LatestRun(step);
        if (await (from processing in db.Set<ProcessRun>()
                   join execution in db.Set<StepExecution>() on processing.StepExecutionId equals execution.Id
                   where execution.LotId == lot.Id && processing.TrackOutAt == null
                   select processing.Id).AnyAsync())
            throw Error("ResourceInUse", "先由执行模块收尾当前加工");
        if (input.ProcessRunId != null && input.ProcessRunId != run?.Id)
            throw Error("InvalidContext", "报废加工上下文必须是当前到访的最近 Run", 422);
        var entry = new ScrapEntry
        {
            WorkOrderId = lot.WorkOrderId,
            StepExecutionId = step?.Id,
            ReasonCodeId = input.ReasonCodeId,
            Quantity = input.Quantity,
            Notes = Text(input.Notes, 2000),
            ReportedById = actor.UserId,
            ReportedAt = Now
        };
        db.Add(entry);
        var dispositions = wafers.Select(w => new WaferDisposition
        {
            WaferId = w.Id,
            LotIdAtEvent = lot.Id,
            ProcessRunId = run?.Id,
            ScrapEntryId = entry.Id,
            DispositionType = "Scrap",
            ReasonCodeId = input.ReasonCodeId,
            IssueId = input.IssueId,
            ApprovedById = actor.UserId,
            PerformedById = actor.UserId,
            OccurredAt = Now
        }).ToArray();
        db.AddRange(dispositions);
        await db.SaveChangesAsync();
        var production = new ProductionBackend(db, fabId, actor, require, can, clock);
        foreach (var disposition in dispositions)
            await production.CloseMembershipForScrap(disposition.Id, commandId);
        if (step != null)
            step.ScrapQty += wafers.Length;
        Record(entry, "WafersScrapped", new { input, lot.Id, waferIds = input.WaferIds });
        await db.SaveChangesAsync();
        return new(View(entry), Tag(entry.RowVersion), 201);
    }
}
