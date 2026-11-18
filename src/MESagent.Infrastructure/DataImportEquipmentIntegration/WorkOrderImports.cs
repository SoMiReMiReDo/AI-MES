using System.Text;
using System.Text.Json;
using MESagent.Application;
using MESagent.Domain.Entities;
using MESagent.Infrastructure.ProductProcessVersionManagement;
using Microsoft.EntityFrameworkCore;

namespace MESagent.Infrastructure.DataImportEquipmentIntegration;

internal sealed partial class IntegrationBackend
{
    private const string WorkOrderEntity = "WorkOrder";
    private sealed record ImportData(Dictionary<string, string> Mapping, WorkOrderImportRow[] Rows);

    private async Task<OrganizationIdentityAccessControlResult> Imports(
        OrganizationIdentityAccessControlRequest r, string[] path)
    {
        var scoped = db.Set<ImportJob>().Where(x => x.FabId == fabId && x.UserId == actor.UserId
            && x.Entity == WorkOrderEntity && x.InputData != null);
        if (path.Length == 2 && r.Method == "GET")
        {
            var rows = scoped.OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id)
                .Select(x => new WorkOrderImportSummary(x.Id, x.OriginalFilename, x.Status, x.CreatedAt, x.TotalRows,
                    x.SuccessfulRows, x.FailedRows));
            return new(await Page(rows, r));
        }
        if (path.Length == 2 && r.Method == "POST")
        {
            var input = Body<PreviewWorkOrderCsv>(r);
            var filename = Text(input.Filename, 255, "文件名");
            if (filename.Contains('/') || filename.Contains('\\')
                || !filename.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
                throw Error(422, "InvalidCsvFile", "请选择 .csv 文件");
            if (input.Csv == null || input.Csv.Contains('\0') || input.Mapping == null || input.Mapping.Count > 9
                || input.Mapping.Any(x => x.Key.Length > 40 || x.Value == null || x.Value.Length > 255
                    || x.Value.Any(char.IsControl)))
                throw Error(422, "InvalidCsvInput", "CSV 内容或表头映射无效");
            if (Encoding.UTF8.GetByteCount(input.Csv) > WorkOrderCsv.MaxBytes)
                throw Error(413, "CsvFileTooLarge", "CSV 文件超过 256 KiB");

            var mapping = input.Mapping.OrderBy(x => x.Key, StringComparer.Ordinal)
                .ToDictionary(x => x.Key, x => x.Value, StringComparer.Ordinal);
            return await WithReceipt(r, new { filename, input.Csv, mapping },
                () => PreviewImport(filename, input.Csv, mapping));
        }
        if (path.Length is not (3 or 4))
            throw NotFound();

        var job = await scoped.SingleOrDefaultAsync(x => x.Id == Id(path[2])) ?? throw NotFound();
        var data = JsonSerializer.Deserialize<ImportData>(job.InputData!, Json)!;
        if (path.Length == 3 && r.Method == "GET")
            return new(ImportView(job, data), Tag(job.RowVersion));
        if (path.Length == 4 && path[3] == "confirm" && r.Method == "POST")
        {
            if (r.Body.ValueKind != JsonValueKind.Object || r.Body.EnumerateObject().Any())
                throw Error(400, "InvalidRequest", "确认仅使用已保存的预检结果");
            return await WithReceipt(r, r.Body, () => ConfirmImport(job, data, r.Version));
        }
        throw NotFound();
    }

    private async Task<OrganizationIdentityAccessControlResult> PreviewImport(
        string filename, string csv, Dictionary<string, string> mapping)
    {
        var preview = WorkOrderCsv.Read(csv, mapping);
        var rows = await CheckOrders(preview.Rows, preview.Errors, false);
        var input = new ImportData(preview.Mapping, rows);
        var job = new ImportJob
        {
            UserId = actor.UserId,
            FabId = fabId,
            Entity = WorkOrderEntity,
            OriginalFilename = filename,
            Status = preview.Errors.Count == 0 ? "Ready" : "Rejected",
            DryRun = true,
            TotalRows = preview.TotalRows,
            InputData = JsonSerializer.Serialize(input, Json),
            StartedAt = Now,
            CompletedAt = Now
        };
        StoreIssues(job, preview.Errors);
        db.Add(job);
        AuditImport(job, "WorkOrderCsvPreviewed");
        await db.SaveChangesAsync();
        return new(ImportView(job, input), Tag(job.RowVersion), 201);
    }

    private async Task<OrganizationIdentityAccessControlResult> ConfirmImport(
        ImportJob job, ImportData input, string? expected)
    {
        if (string.IsNullOrEmpty(expected))
            throw Error(428, "VersionRequired", "请先读取导入任务版本");
        if (expected != Tag(job.RowVersion))
            throw Error(412, "VersionConflict", "导入任务已变化，请刷新后比较");
        if (job.Status != "Ready")
            throw Error(409, "ImportNotReady", "只有通过预检的任务可以确认");

        var errors = new List<WorkOrderImportIssue>();
        var rows = await CheckOrders(input.Rows, errors, true);
        if (errors.Count == 0)
        {
            var orders = new List<ImportedWorkOrder>(rows.Length);
            var planning = new ManufacturingBackend(db, fabId, actor, require, clock);
            foreach (var row in rows)
            {
                var result = await planning.Execute(new("POST", "planning/work-orders",
                    JsonSerializer.SerializeToElement(new
                    {
                        row.OrderNo,
                        row.ProductId,
                        row.ProductRevisionId,
                        row.PlannedQty,
                        row.Priority,
                        row.DueDate,
                        row.PlannedStartAt,
                        row.PlannedEndAt,
                        row.Description
                    }, Json), new Dictionary<string, string>(), null));
                var created = JsonSerializer.SerializeToElement(result.Data, Json).GetProperty("id").GetGuid();
                orders.Add(new(row.Row, created, row.OrderNo));
            }
            job.Status = "Imported";
            job.DryRun = false;
            job.SuccessfulRows = orders.Count;
            job.ResultData = JsonSerializer.Serialize(orders, Json);
        }
        else
        {
            job.Status = "Rejected";
        }

        job.CompletedAt = Now;
        job.UpdatedAt = Now;
        StoreIssues(job, errors);
        AuditImport(job, "WorkOrderCsvConfirmed");
        await db.SaveChangesAsync();
        return new(ImportView(job, input), Tag(job.RowVersion));
    }

    private async Task<WorkOrderImportRow[]> CheckOrders(
        IReadOnlyCollection<WorkOrderImportRow> input, List<WorkOrderImportIssue> errors, bool confirming)
    {
        if (input.Count == 0)
            return [];

        var numbers = input.Select(x => x.OrderNo).ToArray();
        var existing = (await db.Set<WorkOrder>().Where(x => numbers.Contains(x.OrderNo))
            .Select(x => x.OrderNo).ToListAsync()).ToHashSet(StringComparer.Ordinal);
        var duplicate = numbers.GroupBy(x => x, StringComparer.Ordinal).Where(x => x.Count() > 1)
            .Select(x => x.Key).ToHashSet(StringComparer.Ordinal);
        var codes = input.Select(x => x.ProductCode).Distinct().ToArray();
        var revisions = input.Select(x => x.RevisionCode).Distinct().ToArray();
        var matches = await (from p in db.Set<Product>()
                             join revision in db.Set<ProductRevision>() on p.Id equals revision.ProductId
                             where codes.Contains(p.Code) && revisions.Contains(revision.RevisionCode)
                             select new { p.Code, ProductId = p.Id, revision.RevisionCode, RevisionId = revision.Id })
            .ToListAsync();
        var byCode = matches.ToDictionary(x => (x.Code, x.RevisionCode));
        var rows = new List<WorkOrderImportRow>(input.Count);
        foreach (var row in input)
        {
            if (duplicate.Contains(row.OrderNo))
                errors.Add(new(row.Row, "orderNo", "文件内工单号重复"));
            else if (existing.Contains(row.OrderNo))
                errors.Add(new(row.Row, "orderNo", "工单号不可用，可能已存在"));

            if (!byCode.TryGetValue((row.ProductCode, row.RevisionCode), out var match)
                || confirming && (match.ProductId != row.ProductId || match.RevisionId != row.ProductRevisionId))
            {
                errors.Add(new(row.Row, "revisionCode", "产品与修订不存在或已变化"));
                rows.Add(row);
            }
            else
            {
                rows.Add(row with { ProductId = match.ProductId, ProductRevisionId = match.RevisionId });
            }
        }
        return [.. rows];
    }

    private static void StoreIssues(ImportJob job, List<WorkOrderImportIssue> errors)
    {
        job.FailedRows = errors.Where(x => x.Row > 1 && x.Row <= job.TotalRows + 1).Select(x => x.Row)
            .Distinct().Count();
        job.ErrorLog = JsonSerializer.Serialize(errors.OrderBy(x => x.Row), Json);
    }

    private void AuditImport(ImportJob job, string action) => Audit(nameof(ImportJob), job.Id, action,
        new { job.Status, job.TotalRows, job.SuccessfulRows, job.FailedRows });

    private static WorkOrderImportView ImportView(ImportJob job, ImportData input) => new(
        job.Id, job.OriginalFilename, job.Status, job.CreatedAt, job.TotalRows, job.SuccessfulRows, job.FailedRows,
        Tag(job.RowVersion), input.Mapping, input.Rows,
        job.ErrorLog == null ? [] : JsonSerializer.Deserialize<WorkOrderImportIssue[]>(job.ErrorLog, Json)!,
        job.ResultData == null ? [] : JsonSerializer.Deserialize<ImportedWorkOrder[]>(job.ResultData, Json)!);
}
