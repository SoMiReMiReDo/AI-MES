using MESagent.Application;
using MESagent.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MESagent.Infrastructure.QualityExceptionDisposition;

public sealed partial class QualityBackend
{
    private async Task<object> Page<T>(IQueryable<T> query, OrganizationIdentityAccessControlRequest r)
        where T : class
    {
        var page = r.Query.TryGetValue("page", out var p) && int.TryParse(p, out var n) ? n : 1;
        var size = r.Query.TryGetValue("pageSize", out var s) && int.TryParse(s, out n) ? n : 20;
        if (page is < 1 or > 1000000 || size is < 1 or > 100)
            throw Error("InvalidRequest", "分页参数无效", 422);
        var total = await query.CountAsync();
        var rows = await query.OrderByDescending(x => EF.Property<DateTime>(x, "CreatedAt"))
            .ThenBy(x => EF.Property<Guid>(x, "Id")).Skip((page - 1) * size).Take(size).ToListAsync();
        return new { items = rows.Select(View), total, page, pageSize = size };
    }

    private static string Filter(OrganizationIdentityAccessControlRequest r, string key) =>
        r.Query.TryGetValue(key, out var value) ? value.Trim() : "";

    private static Guid? LotFilter(OrganizationIdentityAccessControlRequest r)
    {
        var value = Filter(r, "lotId");
        return value.Length == 0 ? null : Guid.TryParse(value, out var id) ? id
            : throw Error("InvalidRequest", "Lot 筛选无效", 422);
    }

    private async Task<object> Query(OrganizationIdentityAccessControlRequest r, string[] p)
    {
        var q = Filter(r, "q");
        var status = Filter(r, "status");
        var lotId = LotFilter(r);
        if (p.Length == 2)
        {
            switch (p[1])
            {
                case "lots":
                    var lots = db.Set<Lot>().Where(x => x.FabId == fabId && x.SnapshotVersion != null
                        && x.WorkOrderId != null);
                    if (q.Length > 0)
                        lots = lots.Where(x => x.LotNumber.Contains(q));
                    if (status.Length > 0)
                        lots = lots.Where(x => x.Status == status);
                    return await Page(lots, r);
                case "issues":
                    var issues = VisibleIssues();
                    if (lotId != null)
                        issues = issues.Where(x => x.LotId == lotId);
                    if (status.Length > 0)
                        issues = issues.Where(x => x.Status == status);
                    if (q.Length > 0)
                        issues = issues.Where(x => x.Title.Contains(q));
                    return await Page(issues, r);
                case "holds":
                    var holds = db.Set<HoldRecord>().Where(x => db.Set<Lot>()
                        .Any(l => l.Id == x.LotId && l.FabId == fabId));
                    if (lotId != null)
                        holds = holds.Where(x => x.LotId == lotId);
                    if (status == "Active")
                        holds = holds.Where(x => x.ReleasedAt == null);
                    if (status == "Released")
                        holds = holds.Where(x => x.ReleasedAt != null);
                    return await Page(holds, r);
                case "checks":
                    var checks = db.Set<QualityCheck>().Where(x => db.Set<Lot>()
                        .Any(l => l.Id == x.LotId && l.FabId == fabId));
                    if (lotId != null)
                        checks = checks.Where(x => x.LotId == lotId);
                    if (status == "Failed")
                        checks = checks.Where(x => x.AllPassed == false);
                    if (status == "Passed")
                        checks = checks.Where(x => x.AllPassed == true);
                    return await Page(checks, r);
                case "scraps":
                    var scraps = db.Set<ScrapEntry>().Where(x => db.Set<WorkOrder>()
                        .Any(o => o.Id == x.WorkOrderId && o.FabId == fabId));
                    if (lotId != null)
                        scraps = scraps.Where(x => db.Set<WaferDisposition>()
                            .Any(d => d.ScrapEntryId == x.Id && d.LotIdAtEvent == lotId));
                    return await Page(scraps, r);
                case "reason-codes":
                    var reasons = db.Set<ReasonCode>().AsQueryable();
                    var category = Filter(r, "category");
                    if (category.Length > 0)
                        reasons = reasons.Where(x => x.Category == category);
                    if (q.Length > 0)
                        reasons = reasons.Where(x => x.Code.Contains(q) || x.Name.Contains(q));
                    return await Page(reasons, r);
                case "issue-types":
                    return await Page(db.Set<IssueType>().Where(x => q == "" || x.Code.Contains(q)
                        || x.Name.Contains(q)), r);
                case "specifications":
                    return await Page(db.Set<SpecificationVersion>().Where(x =>
                        (status == "" || x.LifecycleStatus == status) && (q == "" || x.ParameterCode.Contains(q))), r);
                case "templates":
                    return await Page(db.Set<QualityCheckTemplate>().Where(x =>
                        (status == "" || x.LifecycleStatus == status) && (q == "" || x.Name.Contains(q))), r);
                case "assignees":
                    return new
                    {
                        items = await db.Users.Where(x => x.IsActive && x.AccountType == "Human"
                        && db.Set<UserFabScope>().Any(s => s.UserId == x.Id && s.FabId == fabId))
                        .OrderBy(x => x.UserName).Select(x => new { x.Id, x.Name, x.UserName }).Take(100).ToListAsync()
                    };
                case "work-orders":
                    return await Page(db.Set<WorkOrder>().Where(x => x.FabId == fabId
                        && (q == "" || x.OrderNo.Contains(q))), r);
                case "routes":
                    return new
                    {
                        items = await (from version in db.Set<RouteVersion>()
                                       join route in db.Set<Route>() on version.RouteId equals route.Id
                                       orderby route.Name, version.Version
                                       select new
                                       {
                                           version.Id,
                                           route.Name,
                                           version.Version,
                                           version.LifecycleStatus,
                                           steps = db.Set<RouteStep>().Where(s => s.RouteVersionId == version.Id)
                                               .OrderBy(s => s.SequenceNo).Select(s => new { s.Id, s.Name, s.SequenceNo }).ToArray()
                                       })
                        .Take(100).ToListAsync()
                    };
                case "equipment":
                    return new
                    {
                        items = await (from tool in db.Set<Equipment>()
                                       join area in db.Set<Area>() on tool.AreaId equals area.Id
                                       where area.FabId == fabId && tool.IsActive
                                       orderby tool.Code
                                       select new { tool.Id, tool.Code, tool.Name }).Take(100).ToListAsync()
                    };
            }
        }
        if (p.Length != 3)
            throw Error("ResourceNotFound", "接口不存在", 404);
        var id = Id(p[2]);
        switch (p[1])
        {
            case "lots":
                return await LotView(await Lot(id));
            case "issues":
                var issue = await Issue(id);
                var issueView = View(issue);
                issueView["assignedToName"] = await db.Users.Where(x => x.Id == issue.AssignedToId)
                    .Select(x => x.Name).FirstOrDefaultAsync();
                issueView["reportedByName"] = await db.Users.Where(x => x.Id == issue.ReportedById)
                    .Select(x => x.Name).SingleAsync();
                issueView["actions"] = (await db.Set<IssueAction>().Where(x => x.IssueId == id)
                    .OrderBy(x => x.CreatedAt).ToListAsync()).Select(View);
                issueView["holds"] = await HoldViews(db.Set<HoldRecord>().Where(x => x.IssueId == id));
                return issueView;
            case "holds":
                var hold = await Find<HoldRecord>(id);
                await Lot(hold.LotId);
                return View(hold);
            case "actions":
                var action = await Find<IssueAction>(id);
                await Issue(action.IssueId);
                return View(action);
            case "checks":
                var check = await Find<QualityCheck>(id);
                await Lot(check.LotId);
                return await CheckView(check);
            case "scraps":
                var scrap = await db.Set<ScrapEntry>().SingleOrDefaultAsync(x => x.Id == id
                    && db.Set<WorkOrder>().Any(o => o.Id == x.WorkOrderId && o.FabId == fabId))
                    ?? throw Error("ResourceNotFound", "报废记录不存在", 404);
                var scrapView = View(scrap);
                var dispositions = await (from disposition in db.Set<WaferDisposition>()
                                          join wafer in db.Set<Wafer>() on disposition.WaferId equals wafer.Id
                                          where disposition.ScrapEntryId == id
                                          orderby wafer.WaferCode
                                          select new { disposition, wafer.WaferCode }).ToListAsync();
                scrapView["dispositions"] = dispositions.Select(x =>
                {
                    var view = View(x.disposition);
                    view["waferCode"] = x.WaferCode;
                    return view;
                });
                return scrapView;
            case "reason-codes": return View(await Find<ReasonCode>(id));
            case "issue-types": return View(await Find<IssueType>(id));
            case "specifications": return View(await Find<SpecificationVersion>(id));
            case "templates": return View(await Find<QualityCheckTemplate>(id));
            default: throw Error("ResourceNotFound", "接口不存在", 404);
        }
    }

    private async Task<object> LotView(Lot lot)
    {
        var view = View(lot);
        view["wafers"] = (await Members(lot)).Select(View);
        view["holds"] = await HoldViews(db.Set<HoldRecord>().Where(x => x.LotId == lot.Id));
        view["releases"] = (await db.Set<LotRelease>().Where(x => x.LotId == lot.Id)
            .OrderByDescending(x => x.ReleasedAt).ToListAsync()).Select(View);
        var step = await Step(lot);
        var run = await LatestRun(step);
        view["step"] = step == null ? null : View(step);
        view["run"] = run == null ? null : View(run);
        view["workOrderNo"] = await db.Set<WorkOrder>().Where(x => x.Id == lot.WorkOrderId)
            .Select(x => x.OrderNo).SingleAsync();
        view["permissions"] = new[] { "hold", "release", "inspect", "scrap" }
            .Where(p => can("quality." + p)).ToArray();
        return view;
    }

    private async Task<object> CheckView(QualityCheck check)
    {
        var view = View(check);
        var measurements = await db.Set<Measurement>().Where(x => x.QualityCheckId == check.Id)
            .OrderBy(x => x.WaferId).ThenBy(x => x.SampleNumber).ThenBy(x => x.ParameterCode).ToListAsync();
        var waferIds = measurements.Select(x => x.WaferId).ToArray();
        var wafers = await db.Set<Wafer>().Where(x => waferIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => x.WaferCode);
        var specIds = measurements.Select(x => x.SpecificationVersionId).ToArray();
        var specs = await db.Set<SpecificationVersion>().Where(x => specIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id);
        view["measurements"] = measurements.Select(row =>
        {
            var result = View(row);
            result["waferCode"] = row.WaferId == null ? null : wafers.GetValueOrDefault(row.WaferId.Value);
            result["expectedBoolean"] = specs[row.SpecificationVersionId].ExpectedBoolean;
            result["expectedText"] = specs[row.SpecificationVersionId].ExpectedText;
            return result;
        });
        return view;
    }

    private async Task<object> HoldViews(IQueryable<HoldRecord> query)
    {
        var rows = await (from hold in query
                          join reason in db.Set<ReasonCode>() on hold.ReasonCodeId equals reason.Id
                          orderby hold.HeldAt descending
                          select new { hold, reason.Name }).ToListAsync();
        return rows.Select(x =>
        {
            var view = View(x.hold);
            view["reasonName"] = x.Name;
            return view;
        }).ToArray();
    }
}
