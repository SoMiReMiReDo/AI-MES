using System.Security.Cryptography;
using MESagent.Application;
using MESagent.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MESagent.Infrastructure.DispatchAndProductionExecution;

public sealed partial class ExecutionBackend
{
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
        var page = Number(r, "page", 1, 1, 1000000);
        var size = Number(r, "pageSize", 20, 1, 100);
        var total = await query.CountAsync();
        var items = await query.Skip((page - 1) * size).Take(size).ToListAsync();
        return new { items = items.Select(View), total, page, pageSize = size };
    }

    private async Task<object> Query(OrganizationIdentityAccessControlRequest r, string[] p)
    {
        if (p[0] == "equipment")
        {
            if (p.Length == 2 && p[1] == "options")
                return new
                {
                    areas = await db.Set<Area>().Where(x => x.FabId == fabId && x.IsActive)
                        .OrderBy(x => x.Code).Select(x => new { x.Id, x.Code, x.Name }).ToListAsync(),
                    groups = await db.Set<EquipmentGroup>().Where(x => x.FabId == fabId && x.IsActive)
                        .OrderBy(x => x.Code).Select(x => new { x.Id, x.Code, x.Name }).ToListAsync(),
                    types = await db.Set<EquipmentType>().Where(x => x.IsActive)
                        .OrderBy(x => x.Code).Select(x => new { x.Id, x.Code, x.Name }).ToListAsync()
                };
            if (p.Length == 2 && p[1] == "types")
                return await Page(db.Set<EquipmentType>().OrderBy(x => x.Code).ThenBy(x => x.Id), r);
            if (p.Length == 2 && p[1] == "groups")
                return await Page(db.Set<EquipmentGroup>().Where(x => x.FabId == fabId)
                    .OrderBy(x => x.Code).ThenBy(x => x.Id), r);
            if (p.Length == 2 && p[1] == "tools")
            {
                var query = from tool in db.Set<Equipment>()
                            join area in db.Set<Area>() on tool.AreaId equals area.Id
                            join equipmentGroup in db.Set<EquipmentGroup>()
                                on tool.EquipmentGroupId equals equipmentGroup.Id
                            where area.FabId == fabId && equipmentGroup.FabId == fabId
                            select tool;
                return await Page(query.OrderBy(x => x.Code).ThenBy(x => x.Id), r);
            }
            if (p.Length == 3 && p[1] == "tools")
            {
                var tool = await Equipment(Id(p[2]));
                return new
                {
                    equipment = View(tool),
                    currentRun = await db.Set<ProcessRun>()
                        .Where(x => x.EquipmentId == tool.Id && x.Status == "Running")
                        .Select(x => new { x.Id, x.StepExecutionId, x.TrackInAt }).SingleOrDefaultAsync(),
                    states = await db.Set<EquipmentState>().Where(x => x.EquipmentId == tool.Id)
                        .OrderByDescending(x => x.StartedAt).ThenBy(x => x.Id).Take(100).ToListAsync()
                };
            }
        }
        if (p[0] != "execution")
            throw Error("ResourceNotFound", "接口不存在", 404);
        if (p.Length == 2 && p[1] == "dispatch")
            return await Dispatch(r);
        if (p.Length == 2 && p[1] == "options")
            return new
            {
                groups = await db.Set<EquipmentGroup>().Where(x => x.FabId == fabId && x.IsActive)
                    .OrderBy(x => x.Code).Select(x => new { x.Id, x.Code, x.Name }).ToListAsync(),
                steps = await db.Set<RouteStep>().Where(x => db.Set<Lot>().Any(l => l.FabId == fabId
                        && l.CurrentRouteStepId == x.Id)).OrderBy(x => x.Name)
                    .Select(x => new { x.Id, x.Name, x.SequenceNo }).ToListAsync()
            };
        if (p.Length == 3 && p[1] == "lots")
        {
            var lot = await Lot(Id(p[2]));
            var step = await db.Set<StepExecution>().SingleOrDefaultAsync(x => x.LotId == lot.Id && x.ExitedAt == null);
            var runs = await (from run in db.Set<ProcessRun>()
                              join execution in db.Set<StepExecution>() on run.StepExecutionId equals execution.Id
                              where execution.LotId == lot.Id
                              orderby run.TrackInAt descending, run.Id
                              select run).Take(20).ToListAsync();
            return new
            {
                lot = View(lot), step = step == null ? null : View(step),
                wafers = (await Members(lot)).Select(View), runs = runs.Select(View),
                work = step == null ? null : await Work(step),
                candidates = step is { Status: "Waiting" } ? await Candidates(lot, step) : [],
                blocks = await Blocks(lot, step is { Status: "Waiting" }),
                missingDependencies = new[] { "F6 质量处置/返工", "完整 F7 资格与维护" }
            };
        }
        if (p.Length == 6 && p[1] == "lots" && p[3] == "documents" && p[5] == "content")
            return await File(await Lot(Id(p[2])), Id(p[4]));
        if (p.Length == 2 && p[1] == "runs")
        {
            var query = from run in db.Set<ProcessRun>()
                        join step in db.Set<StepExecution>() on run.StepExecutionId equals step.Id
                        join lot in db.Set<Lot>() on step.LotId equals lot.Id
                        where lot.FabId == fabId && lot.SnapshotVersion != null
                        select run;
            if (r.Query.TryGetValue("status", out var status) && status != "")
                query = query.Where(x => x.Status == status);
            if (r.Query.TryGetValue("lotId", out var lotText) && lotText != "")
            {
                var id = Id(lotText);
                query = query.Where(x => db.Set<StepExecution>().Any(s => s.Id == x.StepExecutionId && s.LotId == id));
            }
            return await Page(query.OrderByDescending(x => x.TrackInAt).ThenBy(x => x.Id), r);
        }
        if (p.Length == 3 && p[1] == "runs")
        {
            var (run, step, lot) = await Run(Id(p[2]));
            var equipment = await Equipment(run.EquipmentId);
            var recipe = await db.Set<RecipeVersion>().SingleAsync(x => x.Id == run.RecipeVersionId);
            var recipeName = await db.Set<Recipe>().Where(x => x.Id == recipe.RecipeId)
                .Select(x => x.Code).SingleAsync();
            var participants = await (from member in db.Set<ProcessRunWafer>()
                                      join wafer in db.Set<Wafer>() on member.WaferId equals wafer.Id
                                      where member.ProcessRunId == run.Id
                                      orderby wafer.WaferCode
                                      select new { wafer.Id, wafer.WaferCode, member.Result, member.Notes,
                                          member.ProcessedAt }).ToListAsync();
            return new
            {
                run = View(run), step = View(step), lot = View(lot), equipment = View(equipment),
                recipe = new { recipe.Id, recipeName, recipe.Version }, wafers = participants,
                work = await Work(step), blocks = await Blocks(lot, false),
                tracks = await db.Set<TrackEvent>().Where(x => x.ProcessRunId == run.Id)
                    .OrderBy(x => x.OccurredAt).ThenBy(x => x.Id).ToListAsync()
            };
        }
        throw Error("ResourceNotFound", "接口不存在", 404);
    }

    private async Task<object> Dispatch(OrganizationIdentityAccessControlRequest r)
    {
        var query = from lot in db.Set<Lot>()
                    join step in db.Set<StepExecution>() on lot.Id equals step.LotId
                    where lot.FabId == fabId && lot.Status == "Ready" && lot.SnapshotVersion != null
                        && step.Status == "Waiting" && step.ExitedAt == null
                        && step.RouteStepId == lot.CurrentRouteStepId
                    select new { lot, step };
        if (r.Query.TryGetValue("routeStepId", out var stepText) && stepText != "")
        {
            var id = Id(stepText);
            query = query.Where(x => x.step.RouteStepId == id);
        }
        if (r.Query.TryGetValue("equipmentGroupId", out var groupText) && groupText != "")
        {
            var id = Id(groupText);
            query = query.Where(x => (from app in db.Set<RecipeApplicability>()
                                     join tool in db.Set<Equipment>() on app.EquipmentId equals tool.Id
                                     where app.RouteStepId == x.step.RouteStepId && tool.EquipmentGroupId == id
                                     select app.Id).Any());
        }
        if (r.Query.TryGetValue("lotNumber", out var code) && code != "")
            query = query.Where(x => x.lot.LotNumber.Contains(code));
        var priority = Number(r, "minPriority", 0, 0, 100);
        var minutes = Number(r, "minWaitingMinutes", 0, 0, 5256000);
        var before = Now.AddMinutes(-minutes);
        query = query.Where(x => x.lot.Priority >= priority && x.step.EnteredAt <= before);
        var page = Number(r, "page", 1, 1, 1000000);
        var size = Number(r, "pageSize", 20, 1, 100);
        var total = await query.CountAsync();
        var rows = await query.OrderByDescending(x => x.lot.Priority).ThenBy(x => x.step.EnteredAt)
            .ThenBy(x => x.lot.Id).Skip((page - 1) * size).Take(size).ToListAsync();
        var items = new List<object>();
        foreach (var row in rows)
        {
            var blocks = await Blocks(row.lot, true);
            var candidates = await Candidates(row.lot, row.step);
            if (candidates.Count == 0)
                blocks.Add("暂无符合绑定基线的可用设备/配方");
            items.Add(new
            {
                lot = View(row.lot), step = View(row.step),
                waitingMinutes = Math.Max(0, (Now - row.step.EnteredAt).TotalMinutes),
                executable = blocks.Count == 0, blocks, candidateCount = candidates.Count
            });
        }
        return new { items, total, page, pageSize = size };
    }

    private async Task<object> Work(StepExecution step) => new
    {
        checklist = await db.Set<ChecklistItem>().Where(x => x.RouteStepId == step.RouteStepId)
            .OrderBy(x => x.SortOrder).ThenBy(x => x.Id).ToListAsync(),
        outputs = await db.Set<OutputDefinition>().Where(x => x.RouteStepId == step.RouteStepId)
            .OrderBy(x => x.SortOrder).ThenBy(x => x.Id).ToListAsync(),
        completions = await db.Set<ChecklistCompletion>().Where(x => x.StepExecutionId == step.Id).ToListAsync(),
        values = await db.Set<OutputValue>().Where(x => x.StepExecutionId == step.Id).ToListAsync(),
        documents = await db.Set<StepDocument>().Where(x => x.StepExecutionId == step.Id)
            .OrderBy(x => x.Id).ToListAsync(),
        confirmations = await db.Set<ProcessConfirmation>().Where(x => x.StepExecutionId == step.Id).ToListAsync()
    };

    private async Task<EngineeringFile> File(Lot lot, Guid documentId)
    {
        var document = await (from stepDocument in db.Set<StepDocument>()
                              join step in db.Set<StepExecution>() on stepDocument.StepExecutionId equals step.Id
                              join doc in db.Set<EngineeringDocument>()
                                  on stepDocument.EngineeringDocumentId equals doc.Id
                              where step.LotId == lot.Id && stepDocument.Id == documentId
                              select doc).SingleOrDefaultAsync()
            ?? throw Error("ResourceNotFound", "指导书不属于本批执行记录", 404);
        if (document.Checksum.Length != 64 || document.Checksum.Any(c => !Uri.IsHexDigit(c)))
            throw Error("DocumentIntegrityConflict", "指导书摘要无效");
        var root = Environment.GetEnvironmentVariable("MES_DOCUMENT_ROOT")
            ?? Path.Combine(AppContext.BaseDirectory, ".local", "engineering-documents");
        var path = Path.Combine(root, document.Checksum + ".pdf");
        if (!System.IO.File.Exists(path))
            throw Error("DocumentMissing", "指导书文件缺失");
        var bytes = await System.IO.File.ReadAllBytesAsync(path);
        if (Convert.ToHexString(SHA256.HashData(bytes)) != document.Checksum)
            throw Error("DocumentIntegrityConflict", "指导书完整性校验失败");
        return new(bytes, document.OriginalFilename);
    }
}
