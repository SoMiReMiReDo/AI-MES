using MESagent.Application;
using MESagent.Domain;
using MESagent.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MESagent.Infrastructure.DispatchAndProductionExecution;

public sealed partial class ExecutionBackend
{
    private static void Unique(Guid[]? values)
    {
        if (values == null || values.Length > 100 || values.Distinct().Count() != values.Length)
            throw Error("InvalidRequest", "记录清单不能为空、重复或超过 100 项", 422);
    }

    private async Task<OrganizationIdentityAccessControlResult> Records(
        ProcessRun run, StepExecution step, Lot lot, RecordProductionWork input)
    {
        Unique(input.ChecklistItemIds);
        Unique(input.DocumentIds);
        if (input.Outputs == null || input.Outputs.Length > 100 || input.Outputs.Any(x => x == null))
            throw Error("InvalidRequest", "输出清单无效", 422);
        Unique(input.Outputs.Select(x => x.OutputId).ToArray());
        var notes = Text(input.Notes, 2000, false);
        foreach (var id in input.ChecklistItemIds)
        {
            if (!await db.Set<ChecklistItem>().AnyAsync(x => x.Id == id && x.RouteStepId == step.RouteStepId))
                throw Error("InvalidWorkDefinition", "点检不属于绑定步骤", 422);
            if (await db.Set<ChecklistCompletion>().AnyAsync(x => x.StepExecutionId == step.Id
                && x.ChecklistItemId == id))
                throw Error("WorkAlreadyRecorded", "点检已记录，不能覆盖历史");
            db.Add(new ChecklistCompletion
            {
                StepExecutionId = step.Id, ChecklistItemId = id, CheckedById = actor.UserId, CheckedAt = Now
            });
        }
        foreach (var value in input.Outputs)
        {
            var definition = await db.Set<OutputDefinition>().SingleOrDefaultAsync(x => x.Id == value.OutputId
                && x.RouteStepId == step.RouteStepId)
                ?? throw Error("InvalidWorkDefinition", "输出不属于绑定步骤", 422);
            if ((value.Unit ?? "") != (definition.Unit ?? "")
                || !ExecutionRules.ValidOutput(definition.ValueType, definition.Options, value.ValueText,
                    value.ValueNumber, value.ValueBoolean, value.ValueDate))
                throw Error("InvalidOutputValue", "输出类型、精度、选项或单位不符合定义", 422);
            if (await db.Set<OutputValue>().AnyAsync(x => x.StepExecutionId == step.Id && x.OutputId == definition.Id))
                throw Error("WorkAlreadyRecorded", "输出已记录，不能覆盖历史");
            db.Add(new OutputValue
            {
                StepExecutionId = step.Id, OutputId = definition.Id,
                ValueText = value.ValueText, ValueNumber = value.ValueNumber,
                ValueBoolean = value.ValueBoolean, ValueDate = value.ValueDate,
                RecordedById = actor.UserId, RecordedAt = Now
            });
        }
        foreach (var id in input.DocumentIds)
        {
            var document = await db.Set<StepDocument>().SingleOrDefaultAsync(x => x.Id == id
                && x.StepExecutionId == step.Id)
                ?? throw Error("InvalidWorkDefinition", "指导书不属于当前步骤", 422);
            if (document.ValidatedAt != null)
                throw Error("WorkAlreadyRecorded", "指导书已确认");
            document.ValidatedAt = Now;
            document.ValidatedById = actor.UserId;
        }
        if (input.Confirm)
        {
            if (step.ConfirmedAt != null)
                throw Error("WorkAlreadyRecorded", "关键操作已确认");
            step.ConfirmedAt = Now;
            step.ConfirmedById = actor.UserId;
            db.Add(new ProcessConfirmation
            {
                LotId = lot.Id, StepExecutionId = step.Id, ConfirmedById = actor.UserId,
                ConfirmedAt = Now, ConfirmationType = "Operation", Notes = notes
            });
        }
        if (input.ChecklistItemIds.Length + input.DocumentIds.Length + input.Outputs.Length == 0 && !input.Confirm)
            throw Error("InvalidRequest", "至少提交一项作业记录", 422);
        Touch(run);
        Touch(step);
        Touch(lot);
        Record(run, "WorkRecorded", new { step.Id, input });
        await db.SaveChangesAsync();
        return new(View(run), Tag(run.RowVersion));
    }

    private async Task RequiredWork(StepExecution step)
    {
        if (step.RequiresConfirmation && step.ConfirmedAt == null
            || await db.Set<ChecklistItem>().AnyAsync(x => x.RouteStepId == step.RouteStepId && x.IsRequired
                && !db.Set<ChecklistCompletion>().Any(c => c.StepExecutionId == step.Id && c.ChecklistItemId == x.Id))
            || await db.Set<OutputDefinition>().AnyAsync(x => x.RouteStepId == step.RouteStepId && x.IsRequired
                && !db.Set<OutputValue>().Any(v => v.StepExecutionId == step.Id && v.OutputId == x.Id))
            || await db.Set<StepDocument>().AnyAsync(x => x.StepExecutionId == step.Id
                && x.RequiresValidation && x.ValidatedAt == null))
            throw Error("WorkRecordsRequired", "请完成必填点检、输出、指导书和关键操作确认");
    }
}
