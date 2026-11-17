using MESagent.Application;
using MESagent.Domain;
using MESagent.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MESagent.Infrastructure.QualityExceptionDisposition;

public sealed partial class QualityBackend
{
    private async Task<OrganizationIdentityAccessControlResult> Inspect(RecordQualityCheck input)
    {
        var lot = await Lot(input.LotId);
        Match(input.LotEtag, lot.RowVersion);
        if (lot.Status is not ("Ready" or "Running" or "AwaitingDisposition"))
            throw Error("LotStateConflict", "仅已投放且未终结批次可登记检验");
        var template = await Find<QualityCheckTemplate>(input.QualityCheckTemplateId);
        if (template.LifecycleStatus != "Released" || template.RouteVersionId != lot.RouteVersionId)
            throw Error("InvalidTemplate", "模板尚未发布或不适用本批路线", 422);
        var specs = await TemplateSpecifications(template, true);
        var step = await Step(lot);
        ProcessRun? run = null;
        if (input.ProcessRunId != null)
        {
            run = await Find<ProcessRun>(input.ProcessRunId.Value);
            var execution = await Find<StepExecution>(run.StepExecutionId);
            if (execution.LotId != lot.Id)
                throw Error("InvalidContext", "加工记录不属于本批次", 422);
            if (template.RouteStepId != null && template.RouteStepId != execution.RouteStepId)
                throw Error("InvalidTemplate", "模板与加工步骤不一致", 422);
        }
        else if (template.RouteStepId != null && template.RouteStepId != lot.CurrentRouteStepId)
            throw Error("InvalidTemplate", "模板与当前步骤不一致", 422);
        var rows = input.Measurements;
        if (rows == null || rows.Length is < 1 or > 5000 || rows.Any(x => x == null))
            throw Error("InvalidMeasurements", "量测清单须为1～5000项", 422);
        var members = await Members(lot);
        if (members.Count == 0 || members.Any(x => x.LifecycleStatus != "Active"))
            throw Error("InvalidWaferSet", "批次有效晶圆身份不完整", 422);
        var waferMode = rows[0].WaferId != null;
        if (rows.Any(x => (x.WaferId != null) != waferMode))
            throw Error("InvalidMeasurements", "一次检验不能混合 Lot 与逐片模式", 422);
        var waferIds = rows.Where(x => x.WaferId != null).Select(x => x.WaferId!.Value).Distinct().ToArray();
        if (waferIds.Any(id => members.All(x => x.Id != id))
            || run != null && waferIds.Length > 0 && await db.Set<ProcessRunWafer>()
                .CountAsync(x => x.ProcessRunId == run.Id && waferIds.Contains(x.WaferId)) != waferIds.Length)
            throw Error("InvalidWaferSet", "检验片不属于当前批次或指定加工", 422);
        foreach (var group in rows.GroupBy(x => x.WaferId))
        {
            if (group.Count() != template.SamplesPerCheck * specs.Count)
                throw Error("InvalidMeasurements", "每个检验对象须完整提交全部样本及规格", 422);
            for (var sample = 1; sample <= template.SamplesPerCheck; sample++)
            {
                var values = group.Where(x => x.SampleNumber == sample).ToArray();
                if (values.Length != specs.Count || values.Select(x => x.SpecificationVersionId)
                    .Distinct().Count() != specs.Count
                    || !values.Select(x => x.SpecificationVersionId).ToHashSet().SetEquals(specs.Select(x => x.Id))
                    || values.Select(x => x.SamplePoint?.Trim() ?? "").Distinct().Count() != 1)
                    throw Error("InvalidMeasurements", "样本号、样点或参数不完整/重复", 422);
            }
        }
        foreach (var id in rows.Where(x => x.MeasurementEquipmentId != null)
            .Select(x => x.MeasurementEquipmentId!.Value).Distinct())
            await Equipment(id);
        var check = new QualityCheck
        {
            LotId = lot.Id,
            ProcessRunId = run?.Id,
            QualityCheckTemplateId = template.Id,
            CheckedById = actor.UserId,
            CheckedAt = Now,
            Notes = Text(input.Notes, 2000, false),
            ProductionQuantity = waferMode ? waferIds.Length : members.Count
        };
        var measurements = new List<Measurement>();
        foreach (var row in rows)
        {
            var spec = specs.Single(x => x.Id == row.SpecificationVersionId);
            if (row.Unit != spec.Unit || !QualityRules.ValidValue(spec.ValueType,
                row.ValueNumeric, row.ValueBoolean, row.ValueText))
                throw Error("InvalidMeasurement", "量测类型、精度或单位与绑定规格不一致", 422);
            measurements.Add(new Measurement
            {
                QualityCheckId = check.Id,
                ProcessRunId = run?.Id,
                LotId = lot.Id,
                WaferId = row.WaferId,
                SampleNumber = row.SampleNumber,
                SamplePoint = string.IsNullOrWhiteSpace(row.SamplePoint) ? null : Text(row.SamplePoint, 200),
                SpecificationVersionId = spec.Id,
                ParameterCode = spec.ParameterCode,
                ParameterType = spec.ValueType,
                Unit = spec.Unit,
                SpecMin = spec.SpecMin,
                SpecMax = spec.SpecMax,
                ValueNumeric = row.ValueNumeric,
                ValueBoolean = row.ValueBoolean,
                ValueText = row.ValueText,
                IsPassed = QualityRules.Passed(spec.ValueType, spec.SpecMin, spec.SpecMax,
                    spec.ExpectedBoolean, spec.ExpectedText, row.ValueNumeric, row.ValueBoolean, row.ValueText),
                MeasuredAt = Now,
                MeasurementEquipmentId = row.MeasurementEquipmentId,
                RecordedById = actor.UserId
            });
        }
        check.AllPassed = measurements.All(x => x.IsPassed);
        if (check.AllPassed == false)
        {
            var issueTitle = "检验不合格：" + template.Name;
            var type = await db.Set<IssueType>().SingleAsync(x => x.Code == AutomaticCode);
            var reason = await db.Set<ReasonCode>().SingleAsync(x => x.Code == AutomaticCode);
            if (!type.IsActive || !type.IsBlocking || !reason.IsActive || reason.Category != "Hold")
                throw Error("DependencyNotReady", "系统检验冻结配置无效，拒绝保存未受控结果");
            var issue = new Issue
            {
                LotId = lot.Id,
                WorkOrderId = lot.WorkOrderId,
                StepExecutionId = step?.Id,
                IssueTypeId = type.Id,
                IsBlocking = true,
                Title = issueTitle.Length > 200 ? issueTitle[..200] : issueTitle,
                Description = $"质量检验 {check.Id} 存在不合格量测，须人工处置并逐条解除 Hold。",
                Status = "Open",
                ReportedById = actor.UserId,
                ReportedAt = Now,
                NonConformingQty = waferMode ? measurements.Where(x => !x.IsPassed)
                    .Select(x => x.WaferId).Distinct().Count() : members.Count
            };
            db.Add(issue);
            check.IssueId = issue.Id;
            AddHold(lot, reason.Id, issue.Id);
            Record(issue, "InspectionIssueCreated", new { check.Id });
        }
        db.Add(check);
        db.AddRange(measurements);
        Touch(lot);
        Record(check, "QualityChecked", new { check.AllPassed, check.IssueId, measurementCount = rows.Length });
        await db.SaveChangesAsync();
        return new(await CheckView(check), Tag(check.RowVersion), 201);
    }
}
