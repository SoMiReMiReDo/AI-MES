using MESagent.Application;
using MESagent.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MESagent.Infrastructure.QualityExceptionDisposition;

public sealed partial class QualityBackend
{
    private async Task<OrganizationIdentityAccessControlResult> IssueCommand(
        OrganizationIdentityAccessControlRequest r, string[] p)
    {
        if (p.Length == 2 && r.Method == "POST")
            return await Report(Body<ReportQualityIssue>(r));
        if (p.Length != 4 || r.Method != "POST")
            throw Error("ResourceNotFound", "接口不存在", 404);
        var issue = await Issue(Id(p[2]));
        Match(r.Version, issue.RowVersion);
        if (issue.Status == "Closed")
            throw Error("IssueStateConflict", "异常已关闭");
        switch (p[3])
        {
            case "acknowledge":
                Text(Body<ReasonRequest>(r).Reason, 2000);
                if (issue.Status != "Open")
                    throw Error("IssueStateConflict", "仅新异常可确认");
                issue.Status = "Acknowledged";
                issue.AcknowledgedAt = Now;
                break;
            case "assign":
                if (issue.Status != "Acknowledged")
                    throw Error("IssueStateConflict", "确认异常后才能指派");
                var assigned = Body<AssignQualityIssue>(r);
                await Assignee(assigned.AssignedToId);
                issue.AssignedToId = assigned.AssignedToId;
                break;
            case "resolve":
                if (issue.Status != "Acknowledged")
                    throw Error("IssueStateConflict", "确认异常后才能处置");
                var resolution = Body<ResolveQualityIssue>(r);
                if (resolution.Disposition is not ("Continue" or "Retry" or "Scrap"))
                    throw Error("InvalidDisposition", "处置结论无效", 422);
                issue.Disposition = resolution.Disposition;
                issue.RootCause = Text(resolution.RootCause, 2000);
                issue.ContainmentAction = Text(resolution.ContainmentAction, 2000);
                issue.ResolutionNotes = Text(resolution.Notes, 2000);
                issue.DispositionById = actor.UserId;
                issue.DispositionAt = Now;
                issue.ResolvedAt = Now;
                issue.Status = "Resolved";
                break;
            case "close":
                Text(Body<ReasonRequest>(r).Reason, 2000);
                if (issue.Status != "Resolved"
                    || await db.Set<IssueAction>().AnyAsync(x => x.IssueId == issue.Id && x.Status != "Verified")
                    || await db.Set<HoldRecord>().AnyAsync(x => x.IssueId == issue.Id && x.ReleasedAt == null))
                    throw Error("IssueStateConflict", "须先处置、复核全部整改并解除关联 Hold");
                issue.Status = "Closed";
                issue.ClosedAt = Now;
                break;
            case "actions":
                if (issue.Status != "Acknowledged")
                    throw Error("IssueStateConflict", "仅已确认异常可新增整改");
                var input = Body<CreateQualityAction>(r);
                await Assignee(input.AssignedToId);
                if (input.ActionType is not ("Containment" or "Corrective" or "Preventive"))
                    throw Error("InvalidRequest", "整改类别无效", 422);
                var action = new IssueAction
                {
                    IssueId = issue.Id,
                    Title = Text(input.Title, 200),
                    ActionType = input.ActionType,
                    AssignedToId = input.AssignedToId,
                    Description = Text(input.Description, 2000, false),
                    DueDate = input.DueDate,
                    Status = "Open"
                };
                Touch(issue);
                db.Add(action);
                return await Saved(action, "ActionCreated", input, 201);
            default:
                throw Error("ResourceNotFound", "接口不存在", 404);
        }
        return await Saved(issue, "Issue" + p[3], r.Body);
    }

    private async Task<OrganizationIdentityAccessControlResult> Report(ReportQualityIssue input)
    {
        var type = await db.Set<IssueType>().SingleOrDefaultAsync(x => x.Id == input.IssueTypeId && x.IsActive)
            ?? throw Error("InvalidIssueType", "异常类型不存在或已停用", 422);
        if (type.Code == AutomaticCode)
            throw Error("ReservedConfiguration", "系统类型仅用于检验自动异常", 422);
        Lot? lot = input.LotId == null ? null : await Lot(input.LotId.Value);
        var workOrderId = input.WorkOrderId ?? lot?.WorkOrderId;
        if (workOrderId != null && !await db.Set<WorkOrder>().AnyAsync(x => x.Id == workOrderId && x.FabId == fabId))
            throw Error("ResourceNotFound", "工单不存在", 404);
        if (lot != null && lot.WorkOrderId != workOrderId)
            throw Error("InvalidContext", "工单与批次不一致", 422);
        if (input.WaferId != null)
        {
            var wafer = await db.Set<Wafer>().SingleOrDefaultAsync(x => x.Id == input.WaferId && x.FabId == fabId)
                ?? throw Error("ResourceNotFound", "晶圆不存在", 404);
            if (workOrderId != null && wafer.WorkOrderId != workOrderId
                || lot != null && !await db.Set<LotMembership>().AnyAsync(x => x.WaferId == wafer.Id
                    && x.LotId == lot.Id && x.ValidTo == null))
                throw Error("InvalidContext", "晶圆与工单/批次不一致", 422);
            workOrderId ??= wafer.WorkOrderId;
        }
        if (input.StepExecutionId != null && (lot == null || !await db.Set<StepExecution>()
            .AnyAsync(x => x.Id == input.StepExecutionId && x.LotId == lot.Id)))
            throw Error("InvalidContext", "步骤必须属于所选批次", 422);
        if (input.EquipmentId != null)
            await Equipment(input.EquipmentId.Value);
        if (lot == null && workOrderId == null && input.WaferId == null && input.EquipmentId == null)
            throw Error("InvalidContext", "至少关联一个工单、批次、晶圆或设备", 422);
        if (type.IsBlocking && (lot == null || input.HoldReasonCodeId == null))
            throw Error("InvalidContext", "阻塞异常必须关联 Lot 并指定 Hold 原因码", 422);
        var issue = new Issue
        {
            WorkOrderId = workOrderId,
            LotId = lot?.Id,
            WaferId = input.WaferId,
            StepExecutionId = input.StepExecutionId,
            EquipmentId = input.EquipmentId,
            IssueTypeId = type.Id,
            IsBlocking = type.IsBlocking,
            Title = Text(input.Title, 200),
            Description = Text(input.Description, 4000),
            Status = "Open",
            ReportedById = actor.UserId,
            ReportedAt = Now
        };
        db.Add(issue);
        if (type.IsBlocking)
        {
            await Reason(input.HoldReasonCodeId!.Value, "Hold");
            AddHold(lot!, input.HoldReasonCodeId.Value, issue.Id);
        }
        else if (input.HoldReasonCodeId != null)
            throw Error("InvalidRequest", "非阻塞异常请通过独立 Hold 操作冻结", 422);
        return await Saved(issue, "IssueReported", input, 201);
    }

    private async Task<OrganizationIdentityAccessControlResult> ActionCommand(
        OrganizationIdentityAccessControlRequest r, string[] p)
    {
        var action = await Find<IssueAction>(Id(p[2]));
        var issue = await Issue(action.IssueId);
        Match(r.Version, action.RowVersion);
        if (issue.Status == "Closed")
            throw Error("IssueStateConflict", "异常已关闭");
        var reason = Text(Body<ReasonRequest>(r).Reason, 2000);
        if (p[3] == "complete" && action.Status == "Open")
        {
            action.Status = "Completed";
            action.CompletedAt = Now;
            action.CompletedById = actor.UserId;
            action.Notes = reason;
        }
        else if (p[3] == "verify" && action.Status == "Completed")
        {
            action.Status = "Verified";
            action.VerifiedAt = Now;
            action.VerifiedById = actor.UserId;
            action.Notes += "\n复核：" + reason;
        }
        else
            throw Error("ActionStateConflict", "整改须按完成、复核顺序执行");
        Touch(issue);
        return await Saved(action, "Action" + p[3], new { reason });
    }
}
