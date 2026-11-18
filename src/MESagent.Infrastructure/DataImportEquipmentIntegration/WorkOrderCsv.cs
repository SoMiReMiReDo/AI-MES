using System.Globalization;
using System.Text.RegularExpressions;
using MESagent.Application;
using MESagent.Domain;
using Microsoft.VisualBasic.FileIO;

namespace MESagent.Infrastructure.DataImportEquipmentIntegration;

internal static class WorkOrderCsv
{
    internal const int MaxRows = 100;
    internal const int MaxBytes = 256 * 1024;
    private static readonly string[] Required = ["orderNo", "productCode", "revisionCode", "plannedQty"];
    private static readonly string[] Fields =
    [
        .. Required, "priority", "dueDate", "plannedStartAt", "plannedEndAt", "description"
    ];
    private static readonly Regex Timestamp = new(
        @"\A[0-9]{4}-[0-9]{2}-[0-9]{2}T[0-9]{2}:[0-9]{2}:[0-9]{2}(\.[0-9]{1,7})?(Z|[+-][0-9]{2}:[0-9]{2})\z",
        RegexOptions.CultureInvariant);

    internal sealed record Preview(
        int TotalRows,
        Dictionary<string, string> Mapping,
        List<WorkOrderImportRow> Rows,
        List<WorkOrderImportIssue> Errors);

    internal static Preview Read(string csv, Dictionary<string, string> requested)
    {
        var mapping = new Dictionary<string, string>(StringComparer.Ordinal);
        var rows = new List<WorkOrderImportRow>();
        var errors = new List<WorkOrderImportIssue>();
        using var parser = new TextFieldParser(new StringReader(csv.TrimStart('\uFEFF')))
        {
            TextFieldType = FieldType.Delimited,
            HasFieldsEnclosedInQuotes = true,
            TrimWhiteSpace = false
        };
        parser.SetDelimiters(",");

        string[]? header;
        try
        {
            header = parser.ReadFields()?.Select(x => x.Trim()).ToArray();
        }
        catch (MalformedLineException)
        {
            header = null;
        }
        if (header == null || header.Any(string.IsNullOrEmpty) || header.Distinct(StringComparer.Ordinal).Count()
            != header.Length)
        {
            errors.Add(new(1, "header", "CSV 表头必须有效，列名不能为空或重复"));
            return new(0, mapping, rows, errors);
        }

        foreach (var field in requested.Keys.Where(x => !Fields.Contains(x, StringComparer.Ordinal)))
            errors.Add(new(1, "mapping", "包含不受支持的目标字段"));
        var indexes = new Dictionary<string, int>(StringComparer.Ordinal);
        var used = new HashSet<int>();
        foreach (var field in Fields)
        {
            var explicitMapping = requested.TryGetValue(field, out var configured);
            var name = explicitMapping ? configured?.Trim() : field;
            var index = Array.IndexOf(header, name);
            if (index < 0)
            {
                if (explicitMapping || Required.Contains(field, StringComparer.Ordinal))
                    errors.Add(new(1, field, "映射的源列不存在"));
                continue;
            }
            if (!used.Add(index))
            {
                errors.Add(new(1, field, "同一源列只能映射一次"));
                continue;
            }
            mapping.Add(field, header[index]);
            indexes.Add(field, index);
        }
        if (errors.Count > 0)
            return new(0, mapping, rows, errors);

        var total = 0;
        while (!parser.EndOfData)
        {
            if (total == MaxRows)
            {
                errors.Add(new(total + 2, "row", $"CSV 最多包含 {MaxRows} 条数据记录"));
                break;
            }

            var row = ++total + 1;
            try
            {
                var cells = parser.ReadFields();
                if (cells == null || cells.Length != header.Length)
                {
                    errors.Add(new(row, "row", "数据列数与表头不一致"));
                    continue;
                }
                var draft = Draft(row, cells, indexes, errors);
                if (draft != null)
                    rows.Add(draft);
            }
            catch (MalformedLineException)
            {
                errors.Add(new(row, "row", $"引号格式无效，CSV 物理行 {parser.ErrorLineNumber}"));
            }
        }
        if (total == 0)
            errors.Add(new(1, "row", "CSV 至少应含一条数据记录"));

        return new(total, mapping, rows, errors);
    }

    private static WorkOrderImportRow? Draft(
        int row, string[] cells, Dictionary<string, int> indexes, List<WorkOrderImportIssue> errors)
    {
        var initialErrors = errors.Count;
        string Cell(string field) => indexes.TryGetValue(field, out var index) ? cells[index] : "";
        void Reject(string field, string message) => errors.Add(new(row, field, message));

        string RequiredText(string field, int max)
        {
            var value = Cell(field).Trim();
            if (value.Length == 0 || value.Length > max || value.Any(char.IsControl))
                Reject(field, $"必填，最多 {max} 个可显示字符");
            return value;
        }

        DateTime? PlannedAt(string field)
        {
            var value = Cell(field).Trim();
            if (value.Length == 0)
                return null;
            if (Timestamp.IsMatch(value) && DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var parsed))
                return parsed.UtcDateTime;
            Reject(field, "填写带 Z 或时区偏移的 ISO 8601 时间");
            return null;
        }

        var orderNo = RequiredText("orderNo", 64);
        var productCode = RequiredText("productCode", 64);
        var revisionCode = RequiredText("revisionCode", 32);
        var validQty = int.TryParse(Cell("plannedQty").Trim(), NumberStyles.AllowLeadingSign,
            CultureInfo.InvariantCulture, out var qty) && qty > 0;
        if (!validQty)
            Reject("plannedQty", "计划数量必须为正整数");
        var priorityText = Cell("priority").Trim();
        var priority = 0;
        var validPriority = priorityText.Length == 0 || int.TryParse(priorityText, NumberStyles.AllowLeadingSign,
            CultureInfo.InvariantCulture, out priority) && priority is >= 0 and <= 100;
        if (!validPriority)
            Reject("priority", "优先级必须为 0～100 的整数");
        var dueText = Cell("dueDate").Trim();
        DateOnly? dueDate = null;
        if (dueText.Length > 0)
        {
            if (DateOnly.TryParseExact(dueText, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None,
                out var value))
                dueDate = value;
            else
                Reject("dueDate", "交期格式必须为 yyyy-MM-dd");
        }
        var start = PlannedAt("plannedStartAt");
        var end = PlannedAt("plannedEndAt");
        if (!ManufacturingRules.ValidWorkOrderPlan(1, 0, start, end))
            Reject("plannedEndAt", "结束时间不能早于开始时间");
        var description = Cell("description");

        return errors.Count == initialErrors
            ? new(row, orderNo, productCode, revisionCode, Guid.Empty, Guid.Empty, qty, priority, dueDate, start, end,
                description.Length == 0 ? null : description)
            : null;
    }
}
