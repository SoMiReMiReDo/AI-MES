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
        var incomplete = await db.Set<Lot>().AnyAsync(x => x.WorkOrderId == order.Id && x.SnapshotVersion == null)
            || await wafers.AnyAsync(x => x.LifecycleStatus == "Completed" && !completedIds.Contains(x.Id)
                || x.LifecycleStatus != "Completed" && completedIds.Contains(x.Id))
            || await wafers.AnyAsync(x => x.LifecycleStatus == "Active"
                && !db.Set<LotMembership>().Any(m => m.WaferId == x.Id && m.ValidTo == null))
            || await wafers.AnyAsync(x => x.LifecycleStatus != "Registered"
            && x.LifecycleStatus != "Cancelled" && !releasedIds.Contains(x.Id))
            || await (from m in db.Set<LotMembership>()
                      join lot in db.Set<Lot>() on m.LotId equals lot.Id
                      join wafer in db.Set<Wafer>() on m.WaferId equals wafer.Id
                      where lot.WorkOrderId == order.Id && (wafer.WorkOrderId != order.Id
                          || wafer.FabId != order.FabId || lot.SnapshotVersion == null)
                      select m.Id).AnyAsync();
        if (incomplete)
            return new("IncompleteHistory", now, ["历史身份/投放事实核验", "F6 完整质量处置"],
                order.PlannedQty, null, null, null, null, null);
        var active = wafers.Where(x => releasedIds.Contains(x.Id) && !completedIds.Contains(x.Id)
            && x.LifecycleStatus != "Scrapped");
        var blocked = from wafer in active
                      join member in db.Set<LotMembership>() on wafer.Id equals member.WaferId
                      join lot in db.Set<Lot>() on member.LotId equals lot.Id
                      where member.ValidTo == null && (lot.Status == "AwaitingDisposition"
                          || db.Set<HoldRecord>().Any(x => x.LotId == lot.Id && x.ReleasedAt == null)
                          || db.Set<WorkOrderStop>().Any(x => x.WorkOrderId == order.Id
                              && (x.LotId == null || x.LotId == lot.Id) && x.ResumedAt == null)
                          || db.Set<QualityTask>().Any(x => x.LotId == lot.Id && x.CompletedAt == null))
                      select wafer.Id;
        return new("ProductionFacts", now, ["F6 完整质量处置"], order.PlannedQty,
            await releases.CountAsync(), await active.CountAsync(), await blocked.Distinct().CountAsync(),
            await completions.CountAsync(), await wafers.CountAsync(x => releasedIds.Contains(x.Id)
                && x.LifecycleStatus == "Scrapped"));
    }
}
