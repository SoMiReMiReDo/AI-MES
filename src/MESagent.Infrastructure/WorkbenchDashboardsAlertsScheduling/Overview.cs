using MESagent.Application;
using MESagent.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MESagent.Infrastructure.WorkbenchDashboardsAlertsScheduling;

internal sealed partial class WorkbenchBackend
{
    private sealed class CurrentLot
    {
        public Lot Lot { get; init; } = null!;
        public StepExecution Step { get; init; } = null!;
        public RouteStep Route { get; init; } = null!;
        public Guid? EquipmentId { get; init; }
    }

    private IQueryable<CurrentLot> Wip =>
        from lot in Lots
        join step in db.Set<StepExecution>() on lot.Id equals step.LotId
        join route in db.Set<RouteStep>() on step.RouteStepId equals route.Id
        where lot.SnapshotVersion != null && step.ExitedAt == null
            && step.RouteStepId == lot.CurrentRouteStepId
            && (lot.Status == "Ready" || lot.Status == "Running" || lot.Status == "AwaitingDisposition")
        select new CurrentLot
        {
            Lot = lot,
            Step = step,
            Route = route,
            EquipmentId = step.Status == "Waiting" ? step.AssignedEquipmentId
                : db.Set<ProcessRun>().Where(x => x.StepExecutionId == step.Id)
                    .OrderByDescending(x => x.TrackInAt).ThenBy(x => x.Id)
                    .Select(x => (Guid?)x.EquipmentId).FirstOrDefault()
        };

    private IQueryable<Equipment> FilterEquipment(IReadOnlyDictionary<string, string> q)
    {
        var area = FilterId(q, "areaId");
        var group = FilterId(q, "equipmentGroupId");
        var tool = FilterId(q, "equipmentId");
        var line = FilterId(q, "lineId");
        if ((area != null || group != null || tool != null || line != null) && !can(WorkbenchAccess.Execution))
            require(WorkbenchAccess.Equipment);
        return Equipment.Where(x => (area == null || x.AreaId == area)
            && (group == null || x.EquipmentGroupId == group)
            && (tool == null || x.Id == tool) && (line == null || x.LineId == line));
    }

    private static bool EquipmentFilter(IReadOnlyDictionary<string, string> q) =>
        FilterId(q, "areaId") != null || FilterId(q, "equipmentGroupId") != null
        || FilterId(q, "equipmentId") != null || FilterId(q, "lineId") != null;

    private IQueryable<CurrentLot> FilterWip(IReadOnlyDictionary<string, string> q)
    {
        var operation = FilterId(q, "operationId");
        var equipment = FilterEquipment(q);
        var rows = Wip.Where(x => operation == null || x.Route.OperationId == operation);
        return EquipmentFilter(q)
            ? rows.Where(x => equipment.Any(tool => tool.Id == x.EquipmentId)) : rows;
    }

    private IQueryable<Equipment> DashboardEquipment(IReadOnlyDictionary<string, string> q)
    {
        var operation = FilterId(q, "operationId");
        return FilterEquipment(q).Where(x => x.IsActive && (operation == null ||
            (from app in db.Set<RecipeApplicability>()
             join step in db.Set<RouteStep>() on app.RouteStepId equals step.Id
             join version in db.Set<RecipeVersion>() on app.RecipeVersionId equals version.Id
             where app.EquipmentId == x.Id && step.OperationId == operation
                 && version.LifecycleStatus == "Released" && app.EffectiveFrom <= Now
                 && (app.EffectiveTo == null || app.EffectiveTo > Now)
             select app.Id).Any()));
    }

    private async Task<WorkbenchOptions> Options()
    {
        var production = WorkbenchAccess.Sources.Where(x => x != WorkbenchAccess.Equipment).Any(can);
        var tools = Equipment.OrderBy(x => x.Code).ThenBy(x => x.Id);
        var operations = from lot in Lots
                         join step in db.Set<StepExecution>() on lot.Id equals step.LotId
                         join route in db.Set<RouteStep>() on step.RouteStepId equals route.Id
                         select route.OperationId;
        var products = from lot in Lots
                       join revision in db.Set<ProductRevision>() on lot.ProductRevisionId equals revision.Id
                       select revision.ProductId;
        var equipmentFilters = can(WorkbenchAccess.Equipment) || can(WorkbenchAccess.Execution);
        return new(
            equipmentFilters ? await db.Set<Area>().Where(x => x.FabId == fabId).OrderBy(x => x.Code)
                .Select(x => new WorkbenchOption(x.Id, x.Code, x.Name)).ToArrayAsync() : [],
            production ? await db.Set<Operation>().Where(x => operations.Contains(x.Id))
                .OrderBy(x => x.Code).Select(x => new WorkbenchOption(x.Id, x.Code, x.Name)).ToArrayAsync() : [],
            equipmentFilters ? await db.Set<EquipmentGroup>().Where(x => x.FabId == fabId).OrderBy(x => x.Code)
                .Select(x => new WorkbenchOption(x.Id, x.Code, x.Name)).ToArrayAsync() : [],
            production ? await db.Set<Product>().Where(x => products.Contains(x.Id))
                .OrderBy(x => x.Code).Select(x => new WorkbenchOption(x.Id, x.Code, x.Name)).ToArrayAsync() : [],
            can(WorkbenchAccess.Equipment) ? await tools
                .Select(x => new WorkbenchOption(x.Id, x.Code, x.Name)).ToArrayAsync() : [],
            equipmentFilters ? (await ShiftDefinitions()).OrderBy(x => x.Code).ThenBy(x => x.Id)
                .Select(x => new WorkbenchOption(x.Id, x.Code, x.Name)).ToArray() : []);
    }

    private IQueryable<WorkbenchRecord> WipRecords(string kind, IReadOnlyDictionary<string, string> q)
    {
        var rows = FilterWip(q);
        if (kind == "waiting")
            rows = rows.Where(x => x.Step.Status == "Waiting");
        return rows.Select(x => new WorkbenchRecord
        {
            Id = x.Lot.Id, Kind = kind, Label = x.Lot.LotNumber, Status = x.Step.Status,
            OccurredAt = x.Step.EnteredAt, LotId = x.Lot.Id, Detail = x.Step.Name,
            Quantity = (from member in db.Set<LotMembership>()
                        join wafer in db.Set<Wafer>() on member.WaferId equals wafer.Id
                        where member.LotId == x.Lot.Id && member.ValidTo == null && wafer.FabId == fabId
                            && wafer.WorkOrderId == x.Lot.WorkOrderId && wafer.LifecycleStatus == "Active"
                        select wafer.Id).Count()
        });
    }

    private IQueryable<WorkbenchRecord> HeldRecords(IReadOnlyDictionary<string, string> q)
    {
        var operation = FilterId(q, "operationId");
        var equipmentFilter = EquipmentFilter(q);
        var scopedWip = FilterWip(q);
        return from lot in Lots
               let hold = db.Set<HoldRecord>().Where(x => x.LotId == lot.Id && x.ReleasedAt == null)
                   .OrderBy(x => x.HeldAt).ThenBy(x => x.Id).FirstOrDefault()
               where hold != null && (!equipmentFilter || scopedWip.Any(x => x.Lot.Id == lot.Id))
                   && (operation == null || db.Set<RouteStep>().Any(s =>
                       s.Id == lot.CurrentRouteStepId && s.OperationId == operation))
               select new WorkbenchRecord
               {
                   Id = hold.Id, Kind = "hold", Label = lot.LotNumber, Status = "Active",
                   OccurredAt = hold.HeldAt, LotId = lot.Id, Quantity = 1
               };
    }

    private IQueryable<WorkbenchRecord> RunningRecords(IReadOnlyDictionary<string, string> q)
    {
        var operation = FilterId(q, "operationId");
        return from run in db.Set<ProcessRun>()
               join step in db.Set<StepExecution>() on run.StepExecutionId equals step.Id
               join route in db.Set<RouteStep>() on step.RouteStepId equals route.Id
               join lot in Lots on step.LotId equals lot.Id
               join tool in FilterEquipment(q) on run.EquipmentId equals tool.Id
               where run.TrackOutAt == null && run.Status == "Running"
                   && (operation == null || route.OperationId == operation)
               select new WorkbenchRecord
               {
                   Id = run.Id, Kind = "running", Label = lot.LotNumber, Status = run.Status,
                   OccurredAt = run.TrackInAt, LotId = lot.Id, EquipmentId = tool.Id, Detail = step.Name,
                   Quantity = db.Set<ProcessRunWafer>().Count(x => x.ProcessRunId == run.Id)
               };
    }

    private IQueryable<WorkbenchRecord> EquipmentRecords(string kind, IReadOnlyDictionary<string, string> q)
    {
        var rows = from tool in DashboardEquipment(q)
                   let state = db.Set<EquipmentState>().Where(x => x.EquipmentId == tool.Id && x.EndedAt == null)
                       .OrderByDescending(x => x.StartedAt).ThenBy(x => x.Id).FirstOrDefault()
                   where kind != "down" || state != null && state.State == "Down"
                   select new WorkbenchRecord
                   {
                       Id = tool.Id, Kind = kind, Label = tool.Code, Status = state == null ? "Unknown" : state.State,
                       OccurredAt = state == null ? tool.CreatedAt : state.StartedAt, EquipmentId = tool.Id,
                       Quantity = 1, Detail = tool.Name
                   };
        return rows;
    }

    private async Task<WorkbenchOverview> Overview(IReadOnlyDictionary<string, string> q)
    {
        var wip = can(WorkbenchAccess.Lots) ? WipRecords("wip", q) : null;
        var zone = await Zone();
        var today = Today(zone);
        var completed = can(WorkbenchAccess.Lots)
            ? CompletedRecords(q, Utc(today, TimeOnly.MinValue, zone), Utc(today.AddDays(1), TimeOnly.MinValue, zone))
            : null;
        return new(Now,
            wip == null ? null : await wip.CountAsync(),
            wip == null ? null : await wip.SumAsync(x => x.Quantity),
            can(WorkbenchAccess.Execution) ? await WipRecords("waiting", q).CountAsync() : null,
            can(WorkbenchAccess.Execution) ? await RunningRecords(q).CountAsync() : null,
            can(WorkbenchAccess.Quality) ? await HeldRecords(q).CountAsync() : null,
            can(WorkbenchAccess.Equipment) ? await EquipmentRecords("equipment", q).CountAsync() : null,
            can(WorkbenchAccess.Equipment) ? await EquipmentRecords("down", q).CountAsync() : null,
            completed == null ? null : await completed.CountAsync());
    }

    private async Task<PageView<WorkbenchRecord>> Records(IReadOnlyDictionary<string, string> q)
    {
        var kind = q.GetValueOrDefault("kind", "");
        var permission = kind switch
        {
            "wip" or "completed" or "cycle" => WorkbenchAccess.Lots,
            "waiting" or "running" or "passed" or "failed" or "wait" => WorkbenchAccess.Execution,
            "held" or "scrap" or "failed-checks" => WorkbenchAccess.Quality,
            "equipment" or "down" => WorkbenchAccess.Equipment,
            _ => throw Invalid("无效指标类别")
        };
        require(permission);
        IQueryable<WorkbenchRecord> rows;
        if (kind is "completed" or "cycle" or "passed" or "failed" or "scrap" or "failed-checks" or "wait")
        {
            var zone = await Zone();
            var date = Date(q, "date", Today(zone));
            var shift = await HistoryShift(q);
            var overnight = shift != null && shift.EndTime < shift.StartTime;
            var from = Utc(date, shift?.StartTime ?? TimeOnly.MinValue, zone);
            var to = Utc(shift == null || overnight ? date.AddDays(1) : date,
                shift?.EndTime ?? TimeOnly.MinValue, zone);
            rows = HistoryRecords(kind, ShiftFilters(q, shift), from, to);
            if (shift != null && !ShiftDays(shift).Contains(Weekday(date)))
                rows = rows.Where(_ => false);
        }
        else
        {
            rows = kind switch
            {
                "running" => RunningRecords(q),
                "held" => HeldRecords(q),
                "equipment" or "down" => EquipmentRecords(kind, q),
                _ => WipRecords(kind, q)
            };
        }
        return await Page(rows.OrderByDescending(x => x.OccurredAt).ThenBy(x => x.Id), q);
    }
}
