using System.Text.Json;
using MESagent.Application;
using MESagent.Domain;
using MESagent.Domain.Entities;
using MESagent.Infrastructure.LotWaferCarrierGenealogy;
using Microsoft.EntityFrameworkCore;

namespace MESagent.Infrastructure.DispatchAndProductionExecution;

public sealed partial class ExecutionBackend
{
    private async Task<OrganizationIdentityAccessControlResult> Assign(
        Lot lot, StepExecution step, AssignProductionLot input)
    {
        await StartGate(lot, step, input.EquipmentId, input.RecipeVersionId);
        step.AssignedEquipmentId = input.EquipmentId;
        step.AssignedRecipeVersionId = input.RecipeVersionId;
        step.AssignedAt = Now;
        step.AssignedById = actor.UserId;
        Touch(step);
        Touch(lot);
        Record(lot, "Assigned", input);
        await db.SaveChangesAsync();
        return new(View(lot), Tag(lot.RowVersion));
    }

    private void Track(ProcessRun run, string type, object facts)
    {
        db.Add(new TrackEvent
        {
            ProcessRunId = run.Id, EventType = type, OccurredAt = Now, RecordedAt = Now,
            ActorId = actor.UserId, Source = "Web", CorrelationId = actor.CorrelationId,
            IdempotencyKey = commandId.ToString(), Payload = JsonSerializer.Serialize(facts, Json)
        });
    }

    private async Task<OrganizationIdentityAccessControlResult> TrackIn(
        Lot lot, StepExecution step, TrackInProductionLot input)
    {
        var equipment = await Equipment(input.EquipmentId);
        Match(input.EquipmentEtag, equipment.RowVersion);
        var match = await StartGate(lot, step, equipment.Id, input.RecipeVersionId);
        var wafers = await Members(lot);
        // F4 waiting steps created before F5 already have the bound document references.
        // Their acknowledgements become enforceable when actual processing starts.
        foreach (var document in await db.Set<StepDocument>().Where(x => x.StepExecutionId == step.Id).ToListAsync())
        {
            document.IsMandatory = true;
            document.RequiresValidation = true;
        }
        var run = new ProcessRun
        {
            StepExecutionId = step.Id, EquipmentId = equipment.Id, RecipeVersionId = input.RecipeVersionId,
            RecipeApplicabilityId = match.ApplicabilityId, TrackInAt = Now, TrackInById = actor.UserId,
            Status = "Running"
        };
        db.Add(run);
        foreach (var wafer in wafers)
            db.Add(new ProcessRunWafer { ProcessRunId = run.Id, WaferId = wafer.Id });
        lot.Status = "Running";
        lot.StartedAt ??= run.TrackInAt;
        step.Status = "Running";
        Touch(lot);
        Touch(step);
        Touch(equipment);
        await TouchResources(wafers.Select(x => x.Id).ToArray());
        Track(run, "TrackIn", new { match, waferIds = wafers.Select(x => x.Id) });
        Record(run, "TrackedIn", new { lot.Id, step.RouteStepId, match, waferIds = wafers.Select(x => x.Id) });
        await db.SaveChangesAsync();
        return new(View(run), Tag(run.RowVersion), 201);
    }

    private async Task TouchResources(Guid[] waferIds)
    {
        foreach (var wafer in await db.Set<Wafer>().Where(x => waferIds.Contains(x.Id)).ToListAsync())
            Touch(wafer);
        var carrierIds = await (from load in db.Set<CarrierLoad>()
                                join slot in db.Set<CarrierSlot>() on load.CarrierSlotId equals slot.Id
                                where waferIds.Contains(load.WaferId) && load.UnloadedAt == null
                                select slot.CarrierId).Distinct().ToArrayAsync();
        foreach (var carrier in await db.Set<Carrier>().Where(x => carrierIds.Contains(x.Id)).ToListAsync())
            Touch(carrier);
    }

    private async Task<OrganizationIdentityAccessControlResult> TrackOut(
        ProcessRun run, StepExecution step, Lot lot, TrackOutProductionRun input)
    {
        var participants = await db.Set<ProcessRunWafer>().Where(x => x.ProcessRunId == run.Id).ToListAsync();
        if (input.Wafers == null || input.Wafers.Length > 100 || input.Wafers.Any(x => x == null)
            || !ExecutionRules.ValidResults(participants.Select(x => x.WaferId),
                input.Wafers.Select(x => (x.WaferId, x.Result))))
            throw Error("InvalidWaferSet", "必须为完整参与片清单提供唯一的 Passed/Failed 结果", 422);
        var wafers = await Members(lot);
        if (!participants.Select(x => x.WaferId).ToHashSet().SetEquals(wafers.Select(x => x.Id))
            || wafers.Any(x => x.LifecycleStatus != "Active" || x.FabId != fabId
                || x.WorkOrderId != lot.WorkOrderId))
            throw Error("MembershipConflict", "参与片身份或当前归属不一致");
        await RequiredWork(step);
        var blocks = await Blocks(lot, false);
        var time = LotRules.NextEventTime(Now, [run.TrackInAt, step.EnteredAt]);
        foreach (var participant in participants)
        {
            var value = input.Wafers.Single(x => x.WaferId == participant.WaferId);
            participant.Result = value.Result;
            participant.Notes = Text(value.Notes, 2000, false);
            participant.OperatorId = actor.UserId;
            participant.ProcessedAt = time;
        }
        run.Status = "Completed";
        run.TrackOutAt = time;
        run.TrackOutById = actor.UserId;
        step.PassedQty = participants.Count(x => x.Result == "Passed");
        var advance = step.PassedQty == participants.Count && blocks.Count == 0;
        step.Status = advance ? "Completed" : "AwaitingDisposition";
        lot.Status = advance ? "Running" : "AwaitingDisposition";
        if (advance)
            step.ExitedAt = time;
        Touch(run);
        Touch(step);
        Touch(lot);
        Touch(await Equipment(run.EquipmentId));
        await TouchResources(wafers.Select(x => x.Id).ToArray());
        Track(run, "TrackOut", new { input.Wafers, advance, blocks });
        Record(run, "TrackedOut", new { lot.Id, input.Wafers, advance, blocks });
        await db.SaveChangesAsync();
        if (advance)
        {
            var current = await db.Set<RouteStep>().SingleAsync(x => x.Id == step.RouteStepId);
            var next = await db.Set<RouteStep>().Where(x => x.RouteVersionId == lot.RouteVersionId
                && x.SequenceNo > current.SequenceNo).OrderBy(x => x.SequenceNo).FirstOrDefaultAsync();
            var lifecycle = new ProductionBackend(db, fabId, actor, require, can, clock);
            if (next == null)
                await lifecycle.CompleteAtRouteEnd(run.Id, commandId);
            else
            {
                lot.Status = "Ready";
                lot.CurrentRouteStepId = next.Id;
                await lifecycle.EnterWaitingStep(lot.Id, next.Id, time, commandId);
                await db.SaveChangesAsync();
            }
        }
        return new(new { run = View(run), lot = View(lot), advanced = advance, blocks }, Tag(run.RowVersion));
    }

    private async Task<OrganizationIdentityAccessControlResult> Abort(
        ProcessRun run, StepExecution step, Lot lot, ReasonRequest input)
    {
        var reason = Text(input.Reason, 500);
        var time = LotRules.NextEventTime(Now, [run.TrackInAt]);
        var participants = await db.Set<ProcessRunWafer>().Where(x => x.ProcessRunId == run.Id).ToListAsync();
        foreach (var participant in participants)
        {
            participant.Result = "Aborted";
            participant.ProcessedAt = time;
            participant.OperatorId = actor.UserId;
        }
        run.Status = "Aborted";
        run.TrackOutAt = time;
        run.TrackOutById = actor.UserId;
        step.Status = "AwaitingDisposition";
        lot.Status = "AwaitingDisposition";
        Touch(run);
        Touch(step);
        Touch(lot);
        Touch(await Equipment(run.EquipmentId));
        await TouchResources(participants.Select(x => x.WaferId).ToArray());
        Track(run, "Abort", new { reason });
        Record(run, "Aborted", new { lot.Id, reason });
        await db.SaveChangesAsync();
        return new(new { run = View(run), lot = View(lot) }, Tag(run.RowVersion));
    }
}
