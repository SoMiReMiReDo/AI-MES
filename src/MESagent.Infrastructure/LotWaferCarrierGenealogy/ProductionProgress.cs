using MESagent.Application;
using MESagent.Domain.Entities;
using MESagent.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MESagent.Infrastructure.LotWaferCarrierGenealogy;

public static class ProductionProgress
{
    public static async Task<ProductionProgressView> Read(MesDbContext db, WorkOrder order, DateTime now)
    {
        var releases = db.Set<WaferRelease>().Where(x => x.WorkOrderId == order.Id);
        var releasedIds = releases.Select(x => x.WaferId);
        var completions = db.Set<WaferCompletion>().Where(x => x.WorkOrderId == order.Id);
        var completedIds = completions.Select(x => x.WaferId);
        var wafers = db.Set<Wafer>().Where(x => x.WorkOrderId == order.Id && x.FabId == order.FabId);
        if (!await Verified(db, db.Set<WorkOrder>().Where(x => x.Id == order.Id)).AnyAsync())
            return new("IncompleteHistory", now, ["历史身份/投放事实核验"],
                order.PlannedQty, null, null, null, null, null);
        var active = wafers.Where(x => releasedIds.Contains(x.Id) && !completedIds.Contains(x.Id)
            && x.LifecycleStatus != "Scrapped");
        var pendingRelease = MESagent.Infrastructure.QualityExceptionDisposition.QualityGates.PendingReleaseLots(db);
        var blocked = from wafer in active
                      join member in db.Set<LotMembership>() on wafer.Id equals member.WaferId
                      join lot in db.Set<Lot>() on member.LotId equals lot.Id
                      where member.ValidTo == null && (lot.Status == "AwaitingDisposition" || pendingRelease.Contains(lot.Id)
                          || db.Set<HoldRecord>().Any(x => x.LotId == lot.Id && x.ReleasedAt == null)
                          || db.Set<WorkOrderStop>().Any(x => x.WorkOrderId == order.Id
                              && (x.LotId == null || x.LotId == lot.Id) && x.ResumedAt == null)
                          || db.Set<QualityTask>().Any(x => x.LotId == lot.Id && x.CompletedAt == null))
                      select wafer.Id;
        return new("ProductionFacts", now, [], order.PlannedQty,
            await releases.CountAsync(), await active.CountAsync(), await blocked.Distinct().CountAsync(),
            await completions.CountAsync(), await wafers.CountAsync(x => releasedIds.Contains(x.Id)
                && x.LifecycleStatus == "Scrapped"));
    }

    internal static IQueryable<WorkOrder> Verified(MesDbContext db, IQueryable<WorkOrder> orders)
    {
        var releases = db.Set<WaferRelease>();
        var completions = db.Set<WaferCompletion>();
        var members = db.Set<LotMembership>();
        var lots = db.Set<Lot>();
        var wafers = db.Set<Wafer>();
        var scraps = db.Set<WaferDisposition>().Where(x => x.DispositionType == "Scrap");
        return orders.Where(order =>
            !lots.Any(lot => lot.WorkOrderId == order.Id && lot.SnapshotVersion == null)
            && !wafers.Any(wafer => wafer.WorkOrderId == order.Id && wafer.FabId == order.FabId
                && ((wafer.LifecycleStatus == "Completed")
                    != completions.Any(c => c.WorkOrderId == order.Id && c.WaferId == wafer.Id)
                    || wafer.LifecycleStatus == "Active"
                        && !members.Any(m => m.WaferId == wafer.Id && m.ValidTo == null)
                    || wafer.LifecycleStatus == "Scrapped"
                        && (!scraps.Any(d => d.WaferId == wafer.Id && d.ScrapEntryId != null)
                            || members.Any(m => m.WaferId == wafer.Id && m.ValidTo == null))
                    || wafer.LifecycleStatus != "Scrapped" && scraps.Any(d => d.WaferId == wafer.Id)
                    || wafer.LifecycleStatus != "Registered" && wafer.LifecycleStatus != "Cancelled"
                        && !releases.Any(r => r.WorkOrderId == order.Id && r.WaferId == wafer.Id)))
            && !(from m in members
                 join lot in lots on m.LotId equals lot.Id
                 join wafer in wafers on m.WaferId equals wafer.Id
                 where lot.WorkOrderId == order.Id && (wafer.WorkOrderId != order.Id
                     || wafer.FabId != order.FabId || lot.SnapshotVersion == null)
                 select m.Id).Any());
    }
}
