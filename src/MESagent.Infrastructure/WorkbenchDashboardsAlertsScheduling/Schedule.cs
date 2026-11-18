using System.Security.Cryptography;
using System.Text.Json;
using MESagent.Application;
using MESagent.Domain.Entities;
using MESagent.Infrastructure.DispatchAndProductionExecution;
using Microsoft.EntityFrameworkCore;

namespace MESagent.Infrastructure.WorkbenchDashboardsAlertsScheduling;

internal sealed partial class WorkbenchBackend
{
    private IQueryable<ProductionScheduleBlock> Plans => db.Set<ProductionScheduleBlock>()
        .Where(x => x.FabId == fabId && Equipment.Any(e => e.Id == x.EquipmentId)
            && (from step in db.Set<StepExecution>()
                join lot in Lots on step.LotId equals lot.Id
                where step.Id == x.StepExecutionId
                select step.Id).Any());

    private IQueryable<CurrentLot> WaitingQueue => Wip.Where(x => x.Lot.Status == "Ready"
        && x.Step.Status == "Waiting"
        && !Plans.Any(p => p.StepExecutionId == x.Step.Id && p.CancelledAt == null));

    private sealed record CalendarLane(Equipment Tool, ShiftWindow[] Windows, DateTime? BlockedAt);

    private sealed record PlanRow(ProductionScheduleBlock Plan, Lot Lot, StepExecution Step);

    private void RequireSchedule(bool write = false)
    {
        require(WorkbenchAccess.Execution);
        require(WorkbenchAccess.Equipment);
        if (write)
            require(WorkbenchAccess.Schedule);
    }

    private async Task<CalendarLane[]> Calendar(
        DateOnly[] days, TimeZoneInfo zone, IReadOnlyDictionary<string, string> q)
    {
        var equipment = await FilterEquipment(q).OrderBy(x => x.Code)
            .ThenBy(x => x.Id).ToArrayAsync();
        var ids = equipment.Select(x => x.Id).ToArray();
        var running = await db.Set<ProcessRun>().Where(x => ids.Contains(x.EquipmentId) && x.Status == "Running")
            .Select(x => x.EquipmentId).Distinct().ToArrayAsync();
        var schedules = await (
            from schedule in db.Set<MaintenanceSchedule>()
            join tool in db.Set<Tool>().Where(x => x.FabId == fabId) on schedule.ToolId equals tool.Id into tools
            from tool in tools.DefaultIfEmpty()
            let equipmentId = schedule.EquipmentId ?? tool.EquipmentId
            where schedule.IsActive && equipmentId != null && ids.Contains(equipmentId.Value)
            select new { Id = equipmentId!.Value, At = schedule.NextDueAt }).ToArrayAsync();
        var maintenance = await (
            from work in db.Set<MaintenanceEvent>()
            join tool in db.Set<Tool>().Where(x => x.FabId == fabId) on work.ToolId equals tool.Id into tools
            from tool in tools.DefaultIfEmpty()
            let equipmentId = work.EquipmentId ?? tool.EquipmentId
            where work.Status != "Released" && work.Status != "Cancelled"
                && equipmentId != null && ids.Contains(equipmentId.Value)
            select new { Id = equipmentId!.Value, At = work.StartedAt ?? work.ScheduledAt }).ToArrayAsync();
        var shifts = await ShiftDefinitions();
        return equipment.Select(tool =>
        {
            var blocked = !tool.IsActive || tool.OperationalState != "Idle" || running.Contains(tool.Id) ? Now
                : schedules.Concat(maintenance).Where(x => x.Id == tool.Id).Select(x => (DateTime?)x.At).Min();
            return new CalendarLane(tool, Windows(tool, shifts, days.Prepend(days[0].AddDays(-1)), zone), blocked);
        }).ToArray();
    }

    private async Task<WorkbenchSchedule> Schedule(IReadOnlyDictionary<string, string> q)
    {
        RequireSchedule();
        var zone = await Zone();
        var today = Today(zone);
        var days = Days(Date(q, "from", today), Date(q, "to", today), 14);
        var lanes = await Calendar(days, zone, q);
        var ids = lanes.Select(x => x.Tool.Id).ToArray();
        var lower = Utc(days[0], TimeOnly.MinValue, zone);
        var to = Utc(days[^1].AddDays(1), TimeOnly.MinValue, zone);
        var plans = await (from plan in Plans.AsNoTracking()
                           join step in db.Set<StepExecution>().AsNoTracking() on plan.StepExecutionId equals step.Id
                           join lot in Lots on step.LotId equals lot.Id
                           where ids.Contains(plan.EquipmentId) && plan.StartsAt < to && plan.EndsAt > lower
                           orderby plan.StartsAt, plan.Id
                           select new PlanRow(plan, lot, step)).ToArrayAsync();
        var queue = await Page(WaitingQueue.OrderByDescending(x => x.Lot.Priority).ThenBy(x => x.Step.EnteredAt)
            .ThenBy(x => x.Step.Id).Select(x => new WorkbenchQueueItem(
                x.Step.Id, x.Lot.Id, x.Lot.LotNumber, x.Step.Name, x.Lot.Priority, x.Step.EnteredAt)), q);
        return new(lower, to, lanes.Select(x => new WorkbenchOption(x.Tool.Id, x.Tool.Code, x.Tool.Name)).ToArray(),
            plans.Select(x => PlanView(x.Plan, x.Lot, x.Step, lanes.Single(l => l.Tool.Id == x.Plan.EquipmentId)))
                .ToArray(),
            lanes.SelectMany(lane => lane.Windows.Where(x => x.From < to && x.To > lower)
                .Select(x => new WorkbenchCapacity(lane.Tool.Id, x.Label,
                    Max(x.From, lower), Min(x.To, to), lane.BlockedAt))).ToArray(), queue);
    }

    private async Task<WorkbenchPlanChange[]> PlanHistory(Guid id)
    {
        RequireSchedule();
        if (!await Plans.AnyAsync(x => x.Id == id))
            throw Missing();
        var rows = await (from log in db.Set<AuditLog>()
                          join user in db.Users on log.UserId equals user.Id into users
                          from user in users.DefaultIfEmpty()
                          where log.EntityId == id && log.EntityType == nameof(ProductionScheduleBlock)
                          orderby log.OccurredAt descending, log.Id
                          select new { log.OccurredAt, log.Action, Actor = user.Name, log.BeforeState, log.AfterState })
            .Take(100).ToArrayAsync();
        return rows.Select(x => new WorkbenchPlanChange(x.OccurredAt, x.Action, x.Actor,
            x.BeforeState == null ? null : JsonSerializer.Deserialize<WorkbenchPlan>(x.BeforeState, Json),
            x.AfterState == null ? null : JsonSerializer.Deserialize<WorkbenchPlan>(x.AfterState, Json))).ToArray();
    }

    private WorkbenchPlan PlanView(ProductionScheduleBlock plan, Lot lot, StepExecution step, CalendarLane? lane = null)
    {
        var status = "Planned";
        if (plan.CancelledAt != null)
            status = "Cancelled";
        else if (lot.Status != "Ready" || step.Status != "Waiting" || step.ExitedAt != null
            || lot.CurrentRouteStepId != step.RouteStepId)
            status = "Inactive";
        else if (plan.EndsAt <= Now)
            status = "Expired";
        else if (lane != null && (!lane.Windows.Any(x => plan.StartsAt >= x.From && plan.EndsAt <= x.To)
            || plan.EndsAt > lane.BlockedAt || step.AssignedEquipmentId != null
                && step.AssignedEquipmentId != plan.EquipmentId))
            status = "Conflict";
        return new(plan.Id, step.Id, lot.Id, lot.LotNumber, step.Name, plan.EquipmentId,
            plan.StartsAt, plan.EndsAt, status, Tag(plan.RowVersion));
    }

    private ExecutionBackend Execution() => new(db, fabId, actor, require, can, clock);

    private async Task<OrganizationIdentityAccessControlResult> ChangeSchedule(
        OrganizationIdentityAccessControlRequest r, string[] parts)
    {
        RequireSchedule(write: true);
        await RequireActiveFab();
        if (parts.Length == 3 && parts[2] == "auto" && r.Method == "POST")
            return await AutoSchedule(Body<WorkbenchAutoPlanInput>(r), r.IdempotencyKey);

        var create = parts.Length == 2 && r.Method == "POST";
        var move = parts.Length == 3 && r.Method == "PUT";
        var cancel = parts.Length == 4 && parts[3] == "cancel" && r.Method == "POST";
        if (!create && !move && !cancel)
            throw Missing();
        var plan = create ? null : await Plans.AsTracking().SingleOrDefaultAsync(x => x.Id == Id(parts[2]))
            ?? throw Missing();
        if (plan != null)
            Match(r.Version, plan.RowVersion);
        if (plan?.CancelledAt != null)
            throw Conflict("计划已取消");
        var input = cancel ? null : Body<WorkbenchPlanInput>(r);
        if (plan != null && input != null && plan.StepExecutionId != input.StepExecutionId)
            throw Conflict("现有计划须保留原到访");
        var stepId = input?.StepExecutionId ?? plan!.StepExecutionId;
        var entry = await (from step in db.Set<StepExecution>()
                           join lot in Lots on step.LotId equals lot.Id
                           where step.Id == stepId
                           select new { Lot = lot, Step = step }).SingleOrDefaultAsync() ?? throw Missing();
        var before = plan == null ? null : PlanView(plan, entry.Lot, entry.Step);
        if (cancel)
        {
            plan!.CancelledAt = Now;
        }
        else
        {
            var sameVisit = create ? await Plans.SingleOrDefaultAsync(x => x.StepExecutionId == stepId
                && x.CancelledAt == null) : null;
            if (sameVisit != null)
            {
                if (sameVisit.EquipmentId == input!.EquipmentId && sameVisit.StartsAt == input.StartsAt
                    && sameVisit.EndsAt == input.EndsAt)
                    return new(PlanView(sameVisit, entry.Lot, entry.Step), Tag(sameVisit.RowVersion), Replayed: true);
                throw Conflict("该到访已有有效计划");
            }
            var execution = Execution();
            var blocks = await PlanBlocks(execution, entry.Lot, entry.Step);
            if (blocks.Count > 0)
                throw Conflict(string.Join("；", blocks));
            await ValidateWindow(execution, entry.Lot, entry.Step, input!, plan?.Id);
            if (plan == null)
            {
                plan = new() { FabId = fabId, StepExecutionId = stepId };
                db.Add(plan);
            }
            plan.EquipmentId = input!.EquipmentId;
            plan.StartsAt = input.StartsAt;
            plan.EndsAt = input.EndsAt;
        }
        await db.SaveChangesAsync();
        var after = PlanView(plan!, entry.Lot, entry.Step);
        Audit(plan!.Id, nameof(ProductionScheduleBlock),
            cancel ? "ScheduleCancelled" : create ? "ScheduleCreated" : "ScheduleMoved", before, after);
        return new(after, after.Etag, create ? 201 : 200);
    }

    private static Task<List<string>> PlanBlocks(ExecutionBackend execution, Lot lot, StepExecution step) =>
        lot.Status == "Ready" && lot.SnapshotVersion != null && step.Status == "Waiting"
            && step.ExitedAt == null && step.RouteStepId == lot.CurrentRouteStepId
            ? execution.PlanningBlocks(lot) : Task.FromResult<List<string>>(["批次已离开当前待加工到访"]);

    private IQueryable<ProductionScheduleBlock> Occupied => Plans.Where(plan => plan.CancelledAt == null
        && Wip.Any(x => x.Step.Id == plan.StepExecutionId && x.Step.Status == "Waiting" && x.Lot.Status == "Ready"));

    private async Task ValidateWindow(
        ExecutionBackend execution, Lot lot, StepExecution step, WorkbenchPlanInput input, Guid? ignore)
    {
        var zone = await Zone();
        var today = Today(zone);
        if (input.StartsAt.Kind != DateTimeKind.Utc || input.EndsAt.Kind != DateTimeKind.Utc
            || input.StartsAt < Now || input.EndsAt <= input.StartsAt
            || input.EndsAt > Utc(today.AddDays(14), TimeOnly.MinValue, zone))
            throw Invalid("计划时间需采用 UTC，且为未来 14 个厂区自然日内的有效时段");
        var day = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(input.StartsAt, zone));
        var lanes = await Calendar([day], zone,
            new Dictionary<string, string> { ["equipmentId"] = input.EquipmentId.ToString() });
        if (lanes.Length == 0)
            throw Missing();
        var lane = lanes[0];
        if (!lane.Windows.Any(x => input.StartsAt >= x.From && input.EndsAt <= x.To))
            throw Conflict("计划必须完整落在设备适用的班次内");
        if (input.EndsAt > lane.BlockedAt)
            throw Conflict("设备正忙、不可用或与维护计划冲突");
        if ((step.AssignedEquipmentId != null && step.AssignedEquipmentId != input.EquipmentId)
            || !(await execution.PlanningEquipment(lot, step, input.StartsAt, input.EndsAt))
                .Contains(input.EquipmentId))
            throw Conflict("设备和计划时段不符合工单冻结的工艺或当前指派");
        if (await Occupied.AnyAsync(x => x.Id != ignore && x.EquipmentId == input.EquipmentId
            && x.StartsAt < input.EndsAt && x.EndsAt > input.StartsAt))
            throw Conflict("与设备上的现有计划重叠");
    }

    private async Task<OrganizationIdentityAccessControlResult> AutoSchedule(WorkbenchAutoPlanInput input, string? key)
    {
        var zone = await Zone();
        var today = Today(zone);
        if (!Guid.TryParse(key, out var parsedKey) || parsedKey == Guid.Empty)
            throw Invalid("自动排程需要 UUID 格式的 Idempotency-Key");
        var scope = $"workbench/schedule/auto/{fabId}/{actor.UserId}";
        var hash = Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(input, Json)));
        var normalizedKey = parsedKey.ToString();
        var receipt = await db.Set<CommandReceipt>().SingleOrDefaultAsync(x => x.Scope == scope
            && x.IdempotencyKey == normalizedKey);
        if (receipt != null)
        {
            if (receipt.RequestHash != hash)
                throw Conflict("相同幂等键已用于其他排程参数");
            return new(JsonSerializer.Deserialize<WorkbenchAutoResult>(receipt.Result!, Json), Replayed: true);
        }

        var days = Days(input.From, input.To, 14);
        if (input.From < today || input.To >= today.AddDays(14) || input.MaxLots is < 1 or > 50)
            throw Invalid("自动排程范围为未来 14 个厂区自然日，每次 1～50 个批次");
        var lanes = await Calendar(days, zone, new Dictionary<string, string>());
        var from = Max(Now, Utc(days[0], TimeOnly.MinValue, zone));
        var to = Utc(days[^1].AddDays(1), TimeOnly.MinValue, zone);
        var ids = lanes.Select(x => x.Tool.Id).ToArray();
        var plans = await Occupied.Where(x => ids.Contains(x.EquipmentId) && x.StartsAt < to && x.EndsAt > from)
            .ToListAsync();
        var rates = await Rates.Where(x => ids.Contains(x.EquipmentId) && x.EffectiveOn <= input.To).ToArrayAsync();
        var ready = await WaitingQueue.OrderByDescending(x => x.Lot.Priority).ThenBy(x => x.Step.EnteredAt)
            .ThenBy(x => x.Step.Id).Take(input.MaxLots).ToArrayAsync();
        var execution = Execution();
        var created = new List<PlanRow>();
        var unplanned = new List<WorkbenchUnplanned>();
        foreach (var entry in ready)
        {
            var reasons = await PlanBlocks(execution, entry.Lot, entry.Step);
            ProductionScheduleBlock? plan = null;
            if (reasons.Count == 0)
            {
                var count = await db.Set<LotMembership>().CountAsync(x => x.LotId == entry.Lot.Id && x.ValidTo == null);
                plan = await Earliest(execution, entry, count, lanes, plans, rates, from, to);
            }
            if (plan == null)
            {
                unplanned.Add(new(entry.Lot.Id, entry.Lot.LotNumber,
                    reasons.Count > 0
                        ? string.Join("；", reasons)
                        : "无满足产速、班次和设备限制的空闲时段"));
                continue;
            }
            db.Add(plan);
            plans.Add(plan);
            created.Add(new(plan, entry.Lot, entry.Step));
        }
        await db.SaveChangesAsync();
        var views = created.Select(x => PlanView(x.Plan, x.Lot, x.Step)).ToArray();
        foreach (var row in views)
            Audit(row.Id, nameof(ProductionScheduleBlock), "ScheduleAutoCreated", null, row);
        var result = new WorkbenchAutoResult(views, unplanned.ToArray());
        db.Add(new CommandReceipt
        {
            Scope = scope, IdempotencyKey = normalizedKey, RequestHash = hash,
            ActorUserId = actor.UserId, CorrelationId = actor.CorrelationId, Status = "Completed",
            CompletedAt = Now, Result = JsonSerializer.Serialize(result, Json)
        });
        return new(result);
    }

    private async Task<ProductionScheduleBlock?> Earliest(
        ExecutionBackend execution, CurrentLot entry, int count, CalendarLane[] lanes,
        List<ProductionScheduleBlock> occupied, EquipmentIdealRate[] rates, DateTime from, DateTime to)
    {
        var options = new List<(Guid Tool, DateTime Starts, DateTime Ends)>();
        var toolIds = lanes.Select(x => x.Tool.Id).ToArray();
        var effective = (await db.Set<RecipeApplicability>().AsNoTracking()
            .Where(x => x.RouteStepId == entry.Step.RouteStepId && toolIds.Contains(x.EquipmentId)
                && x.EffectiveFrom > from && x.EffectiveFrom < to)
            .Select(x => new { x.EquipmentId, x.EffectiveFrom }).ToArrayAsync())
            .ToLookup(x => x.EquipmentId, x => x.EffectiveFrom);
        void AddOptions(Guid tool, DateTime start, DateTime end, TimeSpan duration)
        {
            if (end - start < duration)
                return;
            options.Add((tool, start, start + duration));
            foreach (var later in effective[tool].Where(x => x > start && end - x >= duration))
                options.Add((tool, later, later + duration));
        }

        foreach (var lane in lanes.Where(x => entry.Step.AssignedEquipmentId == null
                     || entry.Step.AssignedEquipmentId == x.Tool.Id))
        {
            var blocks = occupied.Where(x => x.EquipmentId == lane.Tool.Id).OrderBy(x => x.StartsAt).ToArray();
            foreach (var window in lane.Windows)
            {
                if (EffectiveRate(rates, lane.Tool.Id, window.Day) is not decimal ideal)
                    continue;
                var duration = TimeSpan.FromMinutes((double)Math.Max(1, decimal.Ceiling(ideal * count / 60)));
                var start = Max(window.From, from);
                var end = Min(window.To, Min(lane.BlockedAt ?? to, to));
                foreach (var block in blocks)
                {
                    if (block.EndsAt <= start || block.StartsAt >= end)
                        continue;
                    AddOptions(lane.Tool.Id, start, block.StartsAt, duration);
                    start = Max(start, block.EndsAt);
                }
                AddOptions(lane.Tool.Id, start, end, duration);
            }
        }
        foreach (var candidate in options.Distinct().OrderBy(x => x.Starts).ThenBy(x => x.Ends).ThenBy(x => x.Tool))
        {
            var tools = await execution.PlanningEquipment(entry.Lot, entry.Step, candidate.Starts, candidate.Ends);
            if (tools.Contains(candidate.Tool))
                return new()
                {
                    FabId = fabId, StepExecutionId = entry.Step.Id, EquipmentId = candidate.Tool,
                    StartsAt = candidate.Starts, EndsAt = candidate.Ends
                };
        }
        return null;
    }
}
