using MESagent.Application;
using MESagent.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MESagent.Infrastructure.TraceabilityImpactAnalysis;

internal sealed partial class TraceabilityBackend
{
    private IQueryable<ProcessRunWafer> Participants => db.Set<ProcessRunWafer>().Where(x =>
        Wafers.Any(w => w.Id == x.WaferId));

    private IQueryable<CarrierLoad> SubjectLoads(Guid carrierId) =>
        from load in Loads
        join slot in Slots on load.CarrierSlotId equals slot.Id
        where slot.CarrierId == carrierId
        select load;

    private sealed class RunContext
    {
        public required ProcessRun Run { get; init; }
        public required StepExecution Step { get; init; }
        public required Equipment Tool { get; init; }
    }

    private IQueryable<RunContext> RunContexts(TraceObject subject)
    {
        var lots = SubjectLots(subject);
        var loads = SubjectLoads(subject.Id);
        return from run in db.Set<ProcessRun>()
               join step in db.Set<StepExecution>() on run.StepExecutionId equals step.Id
               join tool in Equipment on run.EquipmentId equals tool.Id
               where lots.Any(l => l.Id == step.LotId)
                   && (subject.Kind != "equipment" || run.EquipmentId == subject.Id)
                   && (subject.Kind != "wafer" || Participants.Any(w => w.ProcessRunId == run.Id
                       && w.WaferId == subject.Id))
                   && (subject.Kind != "carrier" || Participants.Any(w => w.ProcessRunId == run.Id
                       && loads.Any(load => load.WaferId == w.WaferId && load.LoadedAt <= run.TrackInAt
                           && (load.UnloadedAt == null || run.TrackInAt < load.UnloadedAt))))
               select new RunContext { Run = run, Step = step, Tool = tool };
    }

    private IQueryable<Fact> ExecutionEvents(TraceObject subject)
    {
        var runs = RunContexts(subject);
        var details = from row in runs
                      join version in db.Set<RecipeVersion>() on row.Run.RecipeVersionId equals version.Id
                      join recipe in db.Set<Recipe>() on version.RecipeId equals recipe.Id
                      select new { row.Run, row.Step, row.Tool, version.Version, RecipeCode = recipe.Code };
        var started = details.Select(row => new Fact
        {
            Id = row.Run.Id, Type = "run-started", OccurredAt = row.Run.TrackInAt, EndedAt = null,
            LotId = row.Step.LotId, WaferId = null, ReferenceId = row.Tool.Id, ActorId = row.Run.TrackInById,
            Summary = row.Step.Name + " · 到访 " + row.Step.VisitNo.ToString() + " · " + row.Tool.Code
                + " · " + row.RecipeCode + " v" + row.Version,
            Result = null
        });
        var ended = details.Where(row => row.Run.TrackOutAt != null).Select(row => new Fact
        {
            Id = row.Run.Id, Type = "run-ended", OccurredAt = row.Run.TrackOutAt!.Value, EndedAt = null,
            LotId = row.Step.LotId, WaferId = null, ReferenceId = row.Tool.Id, ActorId = row.Run.TrackOutById,
            Summary = row.Step.Name + " · 到访 " + row.Step.VisitNo.ToString() + " · " + row.Tool.Code
                + " · " + row.RecipeCode + " v" + row.Version,
            Result = row.Run.Status
        });
        var events = started.Concat(ended);
        if (can(WaferRead))
        {
            var loads = SubjectLoads(subject.Id);
            var results = from row in runs
                          join participant in Participants on row.Run.Id equals participant.ProcessRunId
                          where (subject.Kind != "wafer" || participant.WaferId == subject.Id)
                              && (subject.Kind != "carrier" || loads.Any(load => load.WaferId == participant.WaferId
                                  && load.LoadedAt <= row.Run.TrackInAt
                                  && (load.UnloadedAt == null || row.Run.TrackInAt < load.UnloadedAt)))
                          select new Fact
                          {
                              Id = participant.Id, Type = "run-wafer", OccurredAt = row.Run.TrackInAt,
                              EndedAt = participant.ProcessedAt, LotId = row.Step.LotId, WaferId = participant.WaferId,
                              ReferenceId = row.Run.Id, ActorId = participant.OperatorId,
                              Summary = row.Step.Name + " · 到访 " + row.Step.VisitNo.ToString(),
                              Result = participant.Result
                          };
            events = events.Concat(results);
        }
        if (subject.Kind is "equipment" or "carrier")
            return events;

        var lots = SubjectLots(subject);
        var visits = db.Set<StepExecution>().Where(x => lots.Any(l => l.Id == x.LotId)
                && (subject.Kind != "wafer" || Memberships.Any(m => m.WaferId == subject.Id && m.LotId == x.LotId
                    && m.ValidFrom <= x.EnteredAt && (m.ValidTo == null || x.EnteredAt < m.ValidTo))))
            .Select(x => new Fact
            {
                Id = x.Id, Type = "step-visit", OccurredAt = x.EnteredAt, EndedAt = x.ExitedAt,
                LotId = x.LotId, WaferId = null, ReferenceId = null, ActorId = null,
                Summary = x.Name + " · 到访 " + x.VisitNo.ToString(), Result = x.Status
            });
        return events.Concat(visits);
    }

    private IQueryable<Fact> EquipmentEvents(TraceObject subject)
    {
        var states = db.Set<EquipmentState>().Where(x => x.EquipmentId == subject.Id).Select(x => new Fact
        {
            Id = x.Id, Type = "equipment-state", OccurredAt = x.StartedAt, EndedAt = x.EndedAt,
            LotId = null, WaferId = null, ReferenceId = subject.Id, ActorId = null, Summary = x.Source, Result = x.State
        });
        var events = db.Set<EquipmentEvent>().Where(x => x.EquipmentId == subject.Id).Select(x => new Fact
        {
            Id = x.Id, Type = "equipment-event", OccurredAt = x.EventTimestamp, EndedAt = null,
            LotId = null, WaferId = null, ReferenceId = subject.Id, ActorId = null,
            Summary = x.EventType, Result = x.ProcessingStatus
        });
        return states.Concat(events);
    }
}
