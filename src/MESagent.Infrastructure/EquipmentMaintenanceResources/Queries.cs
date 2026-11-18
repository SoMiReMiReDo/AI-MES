using MESagent.Application;
using MESagent.Domain.Entities;
using MESagent.Infrastructure.Identity;
using MESagent.Infrastructure.MaterialConsumptionInventory;
using Microsoft.EntityFrameworkCore;

namespace MESagent.Infrastructure.EquipmentMaintenanceResources;

public sealed partial class EquipmentBackend
{
    private IQueryable<Equipment> Scope => EquipmentResources.EquipmentScope(db, fabId);
    private IQueryable<Tool> Tools => db.Set<Tool>().Where(x => x.FabId == fabId);
    private IQueryable<Reticle> Reticles => db.Set<Reticle>().Where(x => x.FabId == fabId);
    private IQueryable<MaintenanceSchedule> Plans => db.Set<MaintenanceSchedule>().Where(x =>
        Scope.Any(e => e.Id == x.EquipmentId) || Tools.Any(t => t.Id == x.ToolId));
    private IQueryable<MaintenanceEvent> MaintenanceWorks => db.Set<MaintenanceEvent>().Where(x =>
        Scope.Any(e => e.Id == x.EquipmentId) || Tools.Any(t => t.Id == x.ToolId));
    private IQueryable<Material> ConsumableMaterials => db.Set<Material>().Where(x =>
        !db.Set<MaterialType>().Any(t => t.Id == x.MaterialTypeId && t.Code == WaferOrigins.TypeCode));

    private IQueryable<MaterialLot> ConsumableLots => db.Set<MaterialLot>().Where(x =>
        db.Set<Warehouse>().Any(w => w.Id == x.WarehouseId && w.FabId == fabId)
        && ConsumableMaterials.Any(m => m.Id == x.MaterialId));

    private static int Number(OrganizationIdentityAccessControlRequest r, string key, int fallback, int min, int max)
    {
        if (!r.Query.TryGetValue(key, out var text) || text == "")
            return fallback;
        if (!int.TryParse(text, out var value) || value < min || value > max)
            throw Error("InvalidQuery", $"{key} 应在 {min}～{max} 范围内", 400);
        return value;
    }

    private async Task<object> Page<T>(IQueryable<T> query, OrganizationIdentityAccessControlRequest r)
        where T : class
    {
        if (r.Query.TryGetValue("q", out var q) && !string.IsNullOrWhiteSpace(q))
        {
            var field = typeof(T).GetProperty("Code") != null ? "Code"
                : typeof(T).GetProperty("Name") != null ? "Name"
                : typeof(T).GetProperty("Title") != null ? "Title"
                : typeof(T).GetProperty("LotNumber") != null ? "LotNumber" : null;
            if (field != null)
                query = query.Where(x => EF.Property<string>(x, field).Contains(q.Trim()));
        }
        if (typeof(T) == typeof(Equipment) && r.Query.TryGetValue("equipmentId", out var selected) && selected != "")
        {
            var selectedId = Id(selected);
            query = query.Where(x => EF.Property<Guid>(x, "Id") == selectedId);
        }
        if (r.Query.TryGetValue("equipmentId", out var eid) && eid != ""
            && typeof(T).GetProperty("EquipmentId") != null)
        {
            var equipmentId = Id(eid);
            if (typeof(T) == typeof(MaintenanceSchedule) || typeof(T) == typeof(MaintenanceEvent))
            {
                var tools = Tools.Where(x => x.EquipmentId == equipmentId).Select(x => x.Id);
                query = query.Where(x => EF.Property<Guid?>(x, "EquipmentId") == equipmentId
                    || EF.Property<Guid?>(x, "ToolId") != null
                    && tools.Contains(EF.Property<Guid?>(x, "ToolId")!.Value));
            }
            else
                query = query.Where(x => EF.Property<Guid?>(x, "EquipmentId") == equipmentId);
        }
        if (r.Query.TryGetValue("status", out var status) && status != ""
            && typeof(T).GetProperty("Status") != null)
            query = query.Where(x => EF.Property<string>(x, "Status") == status);
        if (r.Query.TryGetValue("needsReason", out var needs) && needs == "true" && typeof(T) == typeof(Downtime))
            query = query.Where(x => EF.Property<bool>(x, "NeedsReason"));
        if (r.Query.TryGetValue("id", out var id) && id != "")
        {
            var value = Id(id);
            query = query.Where(x => EF.Property<Guid>(x, "Id") == value);
        }
        var page = Number(r, "page", 1, 1, 1000000);
        var size = Number(r, "pageSize", 20, 1, 100);
        var total = await query.CountAsync();
        var rows = await query.OrderByDescending(x => EF.Property<DateTime>(x, "CreatedAt"))
            .ThenBy(x => EF.Property<Guid>(x, "Id")).Skip((page - 1) * size).Take(size).ToListAsync();
        var views = rows.Select(View).ToList();
        if (typeof(T) == typeof(EquipmentState))
        {
            var stateIds = rows.Cast<EquipmentState>().Where(x => x.State == "Down").Select(x => x.Id).ToArray();
            var downtimes = await db.Set<Downtime>().Where(x => x.EquipmentStateId != null
                && stateIds.Contains(x.EquipmentStateId.Value))
                .Select(x => new { x.Id, x.EquipmentStateId }).ToListAsync();
            foreach (var view in views)
            {
                var stateId = ((System.Text.Json.JsonElement)view["id"]!).GetGuid();
                view["downtimeId"] = downtimes.SingleOrDefault(x => x.EquipmentStateId == stateId)?.Id;
            }
        }
        if (typeof(T) == typeof(EquipmentOperationPolicy))
        {
            var operationIds = rows.Cast<EquipmentOperationPolicy>().Select(x => x.OperationId).ToArray();
            var requirements = await db.Set<EquipmentMaterialRequirement>()
                .Where(x => operationIds.Contains(x.OperationId)).ToListAsync();
            foreach (var view in views)
            {
                var operationId = ((System.Text.Json.JsonElement)view["operationId"]!).GetGuid();
                view["requiredMaterialIds"] = requirements.Where(x => x.OperationId == operationId)
                    .Select(x => x.MaterialId).ToArray();
            }
        }
        return new { items = views, total, page, pageSize = size };
    }

    private async Task<object> Query(OrganizationIdentityAccessControlRequest r, string[] p)
    {
        if (p.Length == 2 && p[1] == "options")
            return await Options();
        if (p.Length == 2 && p[1] == "statistics")
            return await Statistics(r);
        if (p.Length == 3 && p[1] == "tools")
        {
            var equipment = await Equipment(Id(p[2]));
            var current = await (from run in db.Set<ProcessRun>()
                                 join step in db.Set<StepExecution>() on run.StepExecutionId equals step.Id
                                 join lot in db.Set<Lot>() on step.LotId equals lot.Id
                                 where run.EquipmentId == equipment.Id && run.Status == "Running" && lot.FabId == fabId
                                 select new
                                 {
                                     run.Id,
                                     run.StepExecutionId,
                                     run.TrackInAt,
                                     lotId = lot.Id,
                                     lot.LotNumber
                                 })
                .SingleOrDefaultAsync();
            return new
            {
                equipment = View(equipment),
                currentRun = current,
                equipmentAvailable = equipment.IsActive && equipment.OperationalState == "Idle" && current == null
                    && !await EquipmentResources.MaintenanceBlocked(db, equipment.Id, Now),
                states = await db.Set<EquipmentState>().Where(x => x.EquipmentId == equipment.Id)
                    .OrderByDescending(x => x.StartedAt).ThenBy(x => x.Id).Take(100).ToListAsync(),
                capabilities = await (from a in db.Set<RecipeApplicability>()
                                      join step in db.Set<RouteStep>() on a.RouteStepId equals step.Id
                                      join recipe in db.Set<RecipeVersion>() on a.RecipeVersionId equals recipe.Id
                                      where a.EquipmentId == equipment.Id
                                      select new
                                      {
                                          a.Id,
                                          a.RouteStepId,
                                          step.Name,
                                          a.RecipeVersionId,
                                          recipe.Version,
                                          a.EffectiveFrom,
                                          a.EffectiveTo
                                      }).Take(100).ToListAsync(),
                maintenance = (await MaintenanceWorks.Where(x => x.EquipmentId == equipment.Id
                        && x.Status != "Released" && x.Status != "Cancelled").Take(100).ToListAsync()).Select(View),
                currentReticle = await db.Set<ReticleMount>().Where(x => x.EquipmentId == equipment.Id
                    && x.UnloadedAt == null).SingleOrDefaultAsync(),
                materials = (await db.Set<MaterialMount>().Where(x => x.EquipmentId == equipment.Id
                    && x.UnmountedAt == null).ToListAsync()).Select(View)
            };
        }
        if (p.Length == 4 && p[3] == "impact" && p[1] is "reticles" or "material-mounts")
            return await Impact(r, p);
        if (p.Length != 2)
            throw Error("ResourceNotFound", "接口不存在", 404);
        return p[1] switch
        {
            "types" => await Page(db.Set<EquipmentType>(), r),
            "groups" => await Page(db.Set<EquipmentGroup>().Where(x => x.FabId == fabId), r),
            "tools" => await Page(Scope, r),
            "chambers" => await Page(db.Set<Chamber>().Where(x => Scope.Any(e => e.Id == x.EquipmentId)), r),
            "states" => await Page(db.Set<EquipmentState>().Where(x => Scope.Any(e => e.Id == x.EquipmentId)), r),
            "events" => await Page(db.Set<EquipmentEvent>().Where(x => Scope.Any(e => e.Id == x.EquipmentId)), r),
            "downtimes" => await Page(db.Set<Downtime>().Where(x => Scope.Any(e => e.Id == x.EquipmentId)), r),
            "reasons" => await Page(db.Set<ReasonCode>().Where(x => x.Category == "Downtime"), r),
            "shifts" => await Page(db.Set<Shift>().Where(x => x.FabId == fabId), r),
            "resources" => await Page(Tools, r),
            "maintenance-plans" => await Page(Plans, r),
            "maintenance" => await Page(MaintenanceWorks, r),
            "equipment-qualifications" => await Page(db.Set<EquipmentQualification>().Where(x =>
                Scope.Any(e => e.Id == x.EquipmentId)), r),
            "worker-qualifications" => await Page(db.Set<WorkerQualification>().Where(x =>
                db.Set<Worker>().Any(w => w.Id == x.WorkerId && w.FabId == fabId)), r),
            "policies" => await Page(db.Set<EquipmentOperationPolicy>(), r),
            "reticles" => await Page(Reticles, r),
            "reticle-mounts" => await Page(db.Set<ReticleMount>().Where(x => Scope.Any(e => e.Id == x.EquipmentId)), r),
            "consumables" => await Page(ConsumableMaterials, r),
            "consumable-lots" => await Page(ConsumableLots, r),
            "material-mounts" => await Page(db.Set<MaterialMount>().Where(x => Scope.Any(e => e.Id == x.EquipmentId)), r),
            _ => throw Error("ResourceNotFound", "接口不存在", 404)
        };
    }

    private async Task<object> Options() => new
    {
        areas = await db.Set<Area>().Where(x => x.FabId == fabId && x.IsActive)
            .OrderBy(x => x.Code).Select(x => new { x.Id, x.Code, x.Name }).Take(1000).ToListAsync(),
        lines = await db.Set<Line>().Where(x => db.Set<Area>().Any(a => a.Id == x.AreaId && a.FabId == fabId)
            && x.IsActive).OrderBy(x => x.Code).Select(x => new
            {
                x.Id,
                x.Code,
                x.Name,
                x.AreaId
            }).Take(1000).ToListAsync(),
        groups = await db.Set<EquipmentGroup>().Where(x => x.FabId == fabId && x.IsActive)
            .OrderBy(x => x.Code).Select(x => new { x.Id, x.Code, x.Name }).Take(1000).ToListAsync(),
        types = await db.Set<EquipmentType>().Where(x => x.IsActive)
            .OrderBy(x => x.Code).Select(x => new { x.Id, x.Code, x.Name }).Take(1000).ToListAsync(),
        equipment = await Scope.OrderBy(x => x.Code).Select(x => new { x.Id, x.Code, x.Name }).Take(1000).ToListAsync(),
        operations = await db.Set<Operation>().Where(x => x.IsActive)
            .OrderBy(x => x.Code).Select(x => new { x.Id, x.Code, x.Name }).Take(1000).ToListAsync(),
        recipes = await db.Set<RecipeVersion>().Where(x => x.LifecycleStatus == "Released")
            .Join(db.Set<Recipe>(), v => v.RecipeId, x => x.Id, (v, x) => new { v.Id, x.Code, name = v.Version })
            .OrderBy(x => x.Code).Take(1000).ToListAsync(),
        workers = await db.Set<Worker>().Where(x => x.FabId == fabId && x.IsActive)
            .OrderBy(x => x.Code).Select(x => new { x.Id, x.Code, x.Name }).Take(1000).ToListAsync(),
        reasons = await db.Set<ReasonCode>().Where(x => x.Category == "Downtime" && x.IsActive)
            .OrderBy(x => x.Code).Select(x => new { x.Id, x.Code, x.Name }).Take(1000).ToListAsync(),
        shifts = await db.Set<Shift>().Where(x => x.FabId == fabId && x.IsActive)
            .OrderBy(x => x.Code).Select(x => new { x.Id, x.Code, x.Name }).Take(1000).ToListAsync(),
        resources = await Tools.Where(x => x.IsActive).OrderBy(x => x.Code)
            .Select(x => new { x.Id, x.Code, x.Name }).Take(1000).ToListAsync(),
        materials = await ConsumableMaterials.Where(x => x.IsActive).OrderBy(x => x.Code)
            .Select(x => new { x.Id, x.Code, x.Name }).Take(1000).ToListAsync(),
        reticles = await Reticles.Where(x => x.IsActive).OrderBy(x => x.Code)
            .Select(x => new { x.Id, x.Code, name = x.Name + " " + x.Revision }).Take(1000).ToListAsync(),
        materialLots = await ConsumableLots.OrderBy(x => x.LotNumber)
            .Select(x => new { x.Id, code = x.LotNumber }).Take(1000).ToListAsync(),
        plans = await Plans.Where(x => x.IsActive).OrderBy(x => x.Name)
            .Select(x => new { x.Id, x.Name, x.EquipmentId, x.ToolId }).Take(1000).ToListAsync(),
        users = await db.Users.Where(x => x.IsActive && x.AccountType == "Human"
                && db.Set<UserFabScope>().Any(s => s.UserId == x.Id && s.FabId == fabId))
            .OrderBy(x => x.UserName).Select(x => new { x.Id, code = x.UserName, name = x.Name })
            .Take(1000).ToListAsync()
    };

    private async Task<object> Statistics(OrganizationIdentityAccessControlRequest r)
    {
        var asOf = Now;
        var until = QueryDate(r, "until", asOf);
        if (until > asOf.AddSeconds(5))
            throw Error("InvalidQuery", "不能统计未来时段", 400);
        if (until > asOf)
            until = asOf;
        var from = QueryDate(r, "from", until.AddDays(-30));
        if (from >= until || until - from > TimeSpan.FromDays(366))
            throw Error("InvalidQuery", "统计区间应为不超过 366 天的有效历史时间", 400);
        var query = db.Set<EquipmentState>().Where(x => Scope.Any(e => e.Id == x.EquipmentId)
            && x.StartedAt < until && (x.EndedAt == null || x.EndedAt > from));
        if (r.Query.TryGetValue("equipmentId", out var id) && id != "")
        {
            var equipmentId = Id(id);
            await Equipment(equipmentId);
            query = query.Where(x => x.EquipmentId == equipmentId);
        }
        var rows = await query.ToListAsync();
        var ids = rows.Select(x => x.Id).ToArray();
        var downtimes = await db.Set<Downtime>().Where(x => x.EquipmentStateId != null
            && ids.Contains(x.EquipmentStateId.Value)).ToListAsync();
        double Seconds(EquipmentState x) => Math.Max(0,
            ((x.EndedAt == null || x.EndedAt > until ? until : x.EndedAt.Value)
                - (x.StartedAt < from ? from : x.StartedAt)).TotalSeconds);
        return new
        {
            from,
            until,
            asOf = Now,
            states = rows.GroupBy(x => new { x.EquipmentId, x.State })
                .Select(g => new { g.Key.EquipmentId, g.Key.State, seconds = g.Sum(Seconds) }),
            downtimes = rows.Where(x => x.State == "Down").Select(x => new
            {
                x.EquipmentId,
                seconds = Seconds(x),
                reasonCodeId = downtimes.SingleOrDefault(d => d.EquipmentStateId == x.Id)?.ReasonCodeId,
                shiftId = downtimes.SingleOrDefault(d => d.EquipmentStateId == x.Id)?.ShiftId
            }).GroupBy(x => new { x.EquipmentId, x.reasonCodeId, x.shiftId })
                .Select(g => new
                {
                    g.Key.EquipmentId,
                    g.Key.reasonCodeId,
                    g.Key.shiftId,
                    seconds = g.Sum(x => x.seconds)
                }),
            note = "按状态区间裁剪汇总；仅故障停机计入停机时长，维护/离线单列；未指定班次归入未分类，不推断排班。早期区间沿用人工状态，未重建运行时长。"
        };
    }

    private static DateTime QueryDate(OrganizationIdentityAccessControlRequest r, string key, DateTime fallback)
    {
        if (!r.Query.TryGetValue(key, out var text) || text == "")
            return fallback;
        if (!DateTime.TryParse(text, System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.AdjustToUniversal | System.Globalization.DateTimeStyles.AssumeUniversal,
            out var date))
            throw Error("InvalidQuery", "时间格式错误", 400);
        return DateTime.SpecifyKind(date, DateTimeKind.Utc);
    }

    private async Task<object> Impact(OrganizationIdentityAccessControlRequest r, string[] p)
    {
        var id = Id(p[2]);
        IQueryable<ProcessRun> runs;
        if (p[1] == "reticles")
        {
            if (!await Reticles.AnyAsync(x => x.Id == id))
                throw Error("ResourceNotFound", "光罩不存在", 404);
            runs = db.Set<ProcessRun>().Where(x => db.Set<ReticleUsage>().Any(u => u.ReticleId == id
                && u.ProcessRunId == x.Id));
        }
        else
        {
            var mount = await db.Set<MaterialMount>().SingleOrDefaultAsync(x => x.Id == id)
                ?? throw Error("ResourceNotFound", "上机记录不存在", 404);
            await Equipment(mount.EquipmentId);
            runs = db.Set<ProcessRun>().Where(x => db.Set<MaterialRunUsage>().Any(u => u.MaterialMountId == id
                && u.ProcessRunId == x.Id));
        }
        runs = runs.Where(x => Scope.Any(e => e.Id == x.EquipmentId)
            && db.Set<StepExecution>().Any(s => s.Id == x.StepExecutionId
                && db.Set<Lot>().Any(l => l.Id == s.LotId && l.FabId == fabId)));
        var page = Number(r, "page", 1, 1, 1000000);
        var size = Number(r, "pageSize", 20, 1, 100);
        var total = await runs.CountAsync();
        var rows = await runs.OrderByDescending(x => x.TrackInAt).ThenBy(x => x.Id)
            .Skip((page - 1) * size).Take(size).ToListAsync();
        var result = new List<object>();
        foreach (var run in rows)
        {
            var lot = await (from s in db.Set<StepExecution>()
                             join l in db.Set<Lot>() on s.LotId equals l.Id
                             where s.Id == run.StepExecutionId
                             select new { l.Id, l.LotNumber }).SingleAsync();
            result.Add(new
            {
                run = View(run),
                lot,
                wafers = await (from u in db.Set<ProcessRunWafer>()
                                join w in db.Set<Wafer>() on u.WaferId equals w.Id
                                where u.ProcessRunId == run.Id
                                select new { w.Id, w.WaferCode, u.Result }).ToListAsync()
            });
        }
        return new { items = result, total, page, pageSize = size, coverage = "RecordedRunAssociations" };
    }
}
