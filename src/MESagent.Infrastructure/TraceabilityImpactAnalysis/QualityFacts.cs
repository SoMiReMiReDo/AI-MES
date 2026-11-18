using MESagent.Application;
using MESagent.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MESagent.Infrastructure.TraceabilityImpactAnalysis;

internal sealed partial class TraceabilityBackend
{
    private IQueryable<Fact> QualityEvents(TraceObject subject)
    {
        var lots = SubjectLots(subject);
        var memberships = Memberships.Where(m => m.WaferId == subject.Id);
        var runs = RunContexts(subject);
        var checks = db.Set<QualityCheck>().Where(x => lots.Any(l => l.Id == x.LotId));
        if (subject.Kind == "equipment")
            checks = checks.Where(x => runs.Any(r => r.Run.Id == x.ProcessRunId && r.Step.LotId == x.LotId));
        if (subject.Kind == "wafer")
            checks = checks.Where(x => memberships.Any(m => m.LotId == x.LotId && m.ValidFrom <= x.CheckedAt
                && (m.ValidTo == null || x.CheckedAt < m.ValidTo)));
        var inspection = checks.Select(x => new Fact
        {
            Id = x.Id, Type = "inspection", OccurredAt = x.CheckedAt, EndedAt = null,
            LotId = x.LotId, WaferId = null, ReferenceId = null, ActorId = x.CheckedById,
            Summary = "批级检验", Result = x.AllPassed == null ? null : x.AllPassed == true ? "Passed" : "Failed"
        });

        var measurements = db.Set<Measurement>().Where(x => lots.Any(l => l.Id == x.LotId)
            && db.Set<QualityCheck>().Any(c => c.Id == x.QualityCheckId && c.LotId == x.LotId)
            && (x.WaferId == null || Wafers.Any(w => w.Id == x.WaferId))
            && (x.MeasurementEquipmentId == null || Equipment.Any(e => e.Id == x.MeasurementEquipmentId)));
        if (subject.Kind == "equipment")
            measurements = measurements.Where(x => x.MeasurementEquipmentId == subject.Id
                || runs.Any(r => r.Run.Id == x.ProcessRunId && r.Step.LotId == x.LotId));
        if (subject.Kind == "wafer")
            measurements = measurements.Where(x => x.WaferId == subject.Id || (x.WaferId == null
                && memberships.Any(m => m.LotId == x.LotId && m.ValidFrom <= x.MeasuredAt
                    && (m.ValidTo == null || x.MeasuredAt < m.ValidTo))));
        if (!can(WaferRead))
            measurements = measurements.Where(x => x.WaferId == null);
        var measured = measurements.Select(x => new Fact
        {
            Id = x.Id, Type = "measurement", OccurredAt = x.MeasuredAt, EndedAt = null,
            LotId = x.LotId, WaferId = x.WaferId, ReferenceId = x.MeasurementEquipmentId, ActorId = x.RecordedById,
            Summary = (x.WaferId == null ? "批级 · " : "逐片 · ") + x.ParameterCode,
            Result = x.IsPassed ? "Passed" : "Failed"
        });
        var events = inspection.Concat(measured);
        if (subject.Kind == "equipment")
            return events;

        var holds = db.Set<HoldRecord>().Where(x => lots.Any(l => l.Id == x.LotId)
            && (subject.Kind != "wafer" || memberships.Any(m => m.LotId == x.LotId && m.ValidFrom <= x.HeldAt
                && (m.ValidTo == null || x.HeldAt < m.ValidTo))));
        var held = from hold in holds
                   join reason in db.Set<ReasonCode>() on hold.ReasonCodeId equals reason.Id
                   select new Fact
                   {
                       Id = hold.Id, Type = "hold", OccurredAt = hold.HeldAt, EndedAt = hold.ReleasedAt,
                       LotId = hold.LotId, WaferId = null, ReferenceId = null, ActorId = hold.HeldById,
                       Summary = reason.Name, Result = null
                   };
        var unheld = holds.Where(x => x.ReleasedAt != null).Select(x => new Fact
        {
            Id = x.Id, Type = "hold-released", OccurredAt = x.ReleasedAt!.Value, EndedAt = null,
            LotId = x.LotId, WaferId = null, ReferenceId = null, ActorId = x.ReleasedById,
            Summary = x.ReleaseReason, Result = null
        });
        var releases = db.Set<LotRelease>().Where(x => lots.Any(l => l.Id == x.LotId)
                && (subject.Kind != "wafer" || memberships.Any(m => m.LotId == x.LotId
                    && m.ValidFrom <= x.ReleasedAt && (m.ValidTo == null || x.ReleasedAt < m.ValidTo))))
            .Select(x => new Fact
            {
                Id = x.Id, Type = "quality-release", OccurredAt = x.ReleasedAt, EndedAt = null,
                LotId = x.LotId, WaferId = null, ReferenceId = null, ActorId = x.ReleasedById,
                Summary = x.Reason, Result = x.RecoveryAction
            });
        events = events.Concat(held).Concat(unheld).Concat(releases);
        if (!can(WaferRead))
            return events;

        var dispositions = from disposition in db.Set<WaferDisposition>()
                           join reason in db.Set<ReasonCode>() on disposition.ReasonCodeId equals reason.Id
                           where lots.Any(l => l.Id == disposition.LotIdAtEvent)
                               && Wafers.Any(w => w.Id == disposition.WaferId)
                               && (subject.Kind != "wafer" || disposition.WaferId == subject.Id)
                           select new Fact
                           {
                               Id = disposition.Id, Type = "disposition", OccurredAt = disposition.OccurredAt,
                               EndedAt = null, LotId = disposition.LotIdAtEvent, WaferId = disposition.WaferId,
                               ReferenceId = null, ActorId = disposition.PerformedById, Summary = reason.Name,
                               Result = disposition.DispositionType
                           };
        return events.Concat(dispositions);
    }
}
