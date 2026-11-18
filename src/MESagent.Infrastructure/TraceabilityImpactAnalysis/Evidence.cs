using System.Globalization;
using MESagent.Application;
using MESagent.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MESagent.Infrastructure.TraceabilityImpactAnalysis;

internal sealed partial class TraceabilityBackend
{
    private static string? EventGroup(string type) => type switch
    {
        "order-created" or "order-released" or "order-completed" => "orders",
        "lot-created" or "lot-closed" => "lots",
        "membership" or "wafer-released" or "wafer-completed" => "wafers",
        "genealogy" => "genealogy",
        "carrier-loaded" or "carrier-unloaded" or "carrier-moved" => "carriers",
        "run-started" or "run-ended" or "run-wafer" or "step-visit" => "execution",
        "inspection" or "measurement" or "hold" or "hold-released" or "quality-release" or "disposition" => "quality",
        "equipment-state" or "equipment-event" => "equipment",
        _ => null
    };

    private static string? ReferenceKind(string type) => type switch
    {
        "genealogy" => "lot",
        "carrier-loaded" or "carrier-unloaded" or "carrier-moved" => "carrier",
        "run-started" or "run-ended" or "equipment-state" or "equipment-event" or "measurement" => "equipment",
        _ => null
    };

    private async Task<TraceEvent[]> Render(Fact[] facts)
    {
        if (facts.Length == 0)
            return [];

        var references = facts.SelectMany(x => new (string? Kind, Guid? Id)[]
            { ("lot", x.LotId), ("wafer", x.WaferId), (ReferenceKind(x.Type), x.ReferenceId) })
            .Where(x => x.Id != null && x.Kind != null).Distinct().GroupBy(x => x.Kind!);
        var objects = new Dictionary<(string, Guid), TraceObject>();
        foreach (var group in references.Where(g => can(Permission(g.Key))))
        {
            var ids = group.Select(x => x.Id!.Value).ToArray();
            foreach (var subject in await Subjects(group.Key).Where(x => ids.Contains(x.Id)).ToArrayAsync())
                objects[(subject.Kind, subject.Id)] = subject;
        }
        var actorIds = facts.Where(x => x.ActorId != null).Select(x => x.ActorId!.Value).Distinct().ToArray();
        var actors = await db.Users.Where(x => actorIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id, x => x.Name);
        var events = new List<TraceEvent>(facts.Length);
        foreach (var fact in facts)
        {
            var links = new List<TraceLink>();
            var kind = ReferenceKind(fact.Type);
            if (kind != null)
            {
                var relation = fact.Type == "genealogy" ? "来源批" : kind == "carrier" ? "载具" : "设备";
                Add(kind, fact.ReferenceId, relation);
            }
            Add("lot", fact.LotId, fact.Type == "genealogy" ? "目标批" : "当时批次");
            Add("wafer", fact.WaferId, "晶圆");
            events.Add(new(fact.Id, fact.Type, fact.OccurredAt, fact.EndedAt, fact.Summary, fact.Result,
                fact.ActorId is Guid actorId ? actors.GetValueOrDefault(actorId) : null, links.ToArray()));

            void Add(string kind, Guid? id, string relation)
            {
                if (id is Guid value && objects.TryGetValue((kind, value), out var match))
                    links.Add(new(value, kind, match.Code, relation));
            }
        }
        return events.ToArray();
    }

    private async Task<TraceEvidence> Evidence(TraceObject subject, string[] groups, string type, Guid id)
    {
        var group = EventGroup(type);
        if (group == null || !groups.Contains(group))
            throw NotFound();

        var fact = await Events(subject, [group]).FirstOrDefaultAsync(x => x.Type == type && x.Id == id)
            ?? throw NotFound();
        var rendered = await Render([fact]);
        var fields = await SourceFields(subject, fact);
        return new(rendered[0], fields);
    }

    private async Task<TraceField[]> SourceFields(TraceObject subject, Fact fact)
    {
        var fields = new List<TraceField>();
        Add("记录 ID", fact.Id.ToString());
        Add("操作人 ID", fact.ActorId?.ToString());
        switch (fact.Type)
        {
            case "genealogy":
                var detail = await db.Set<GenealogyDetail>().SingleAsync(x => x.Id == fact.Id);
                Add("拆合批事件 ID", detail.EventId.ToString());
                break;
            case "membership":
                var membership = await db.Set<LotMembership>().SingleAsync(x => x.Id == fact.Id);
                if (can(GenealogyRead))
                    Add("归属变更事件 ID", membership.ChangeEventId.ToString());
                break;
            case "carrier-moved":
                var movement = await db.Set<CarrierMovement>().SingleAsync(x => x.Id == fact.Id);
                Add("来源位置", await Locations.Where(x => x.Id == movement.FromLocationId)
                    .Select(x => x.Name).SingleOrDefaultAsync());
                Add("目标位置", fact.Summary);
                Add("移位备注", movement.Notes);
                break;
            case "run-started":
            case "run-ended":
            case "run-wafer":
                await RunFields(subject, fact, fields);
                break;
            case "step-visit":
                var step = await db.Set<StepExecution>().SingleAsync(x => x.Id == fact.Id);
                Add("路线版本 ID", step.RouteVersionId.ToString());
                Add("工序 ID", step.RouteStepId.ToString());
                Add("到访次数", step.VisitNo.ToString(CultureInfo.InvariantCulture));
                break;
            case "measurement":
                var measurement = await db.Set<Measurement>().SingleAsync(x => x.Id == fact.Id);
                Add("检验记录 ID", measurement.QualityCheckId.ToString());
                Add("参数", measurement.ParameterCode);
                Add("样本号", measurement.SampleNumber.ToString(CultureInfo.InvariantCulture));
                Add("采样点", measurement.SamplePoint);
                Add("测量值", measurement.ValueNumeric?.ToString(CultureInfo.InvariantCulture)
                    ?? measurement.ValueBoolean?.ToString() ?? measurement.ValueText);
                Add("单位", measurement.Unit);
                Add("下限", measurement.SpecMin?.ToString(CultureInfo.InvariantCulture));
                Add("上限", measurement.SpecMax?.ToString(CultureInfo.InvariantCulture));
                Add("规格版本 ID", measurement.SpecificationVersionId.ToString());
                break;
            case "inspection":
                var check = await db.Set<QualityCheck>().SingleAsync(x => x.Id == fact.Id);
                Add("检验备注", check.Notes);
                break;
            case "hold":
            case "hold-released":
                var hold = await db.Set<HoldRecord>().SingleAsync(x => x.Id == fact.Id);
                Add("冻结范围", hold.HoldScope);
                Add("解除原因", hold.ReleaseReason);
                break;
            case "disposition":
                var disposition = await db.Set<WaferDisposition>().SingleAsync(x => x.Id == fact.Id);
                Add("报废记录 ID", disposition.ScrapEntryId?.ToString());
                Add("审核人 ID", disposition.ApprovedById.ToString());
                break;
            case "equipment-event":
                var evt = await db.Set<EquipmentEvent>().SingleAsync(x => x.Id == fact.Id);
                Add("来源", evt.Source);
                Add("原状态", evt.StateFrom);
                Add("目标状态", evt.StateTo);
                Add("拒绝原因", evt.RejectionReason);
                break;
        }
        return fields.ToArray();

        void Add(string label, string? value)
        {
            if (!string.IsNullOrEmpty(value))
                fields.Add(new(label, value));
        }
    }

    private async Task RunFields(TraceObject subject, Fact fact, List<TraceField> fields)
    {
        var runId = fact.WaferId != null ? fact.ReferenceId!.Value : fact.Id;
        var run = await (from row in RunContexts(subject)
                         join version in db.Set<RecipeVersion>() on row.Run.RecipeVersionId equals version.Id
                         join recipe in db.Set<Recipe>() on version.RecipeId equals recipe.Id
                         where row.Run.Id == runId
                         select new { row.Run, row.Step, row.Tool.Code, version.Version, Recipe = recipe.Code })
            .SingleAsync();
        fields.Add(new("Run ID", runId.ToString()));
        fields.Add(new("到访记录 ID", run.Step.Id.ToString()));
        fields.Add(new("实际设备", run.Code));
        fields.Add(new("实际配方", $"{run.Recipe} v{run.Version}"));
        fields.Add(new("实际配方版本 ID", run.Run.RecipeVersionId.ToString()));
        fields.Add(new("Run 状态", run.Run.Status));
        if (subject.Kind == "carrier")
            fields.Add(new("载具关联", "实际参与片在 Track-in 时装载于此载具"));
        if (subject.Kind == "wafer" || fact.WaferId != null)
        {
            var waferId = fact.WaferId ?? subject.Id;
            var result = await Participants.Where(x => x.ProcessRunId == runId && x.WaferId == waferId)
                .Select(x => new { x.Result, x.Notes }).SingleAsync();
            fields.Add(new("逐片结果", result.Result ?? "未记录"));
            if (!string.IsNullOrEmpty(result.Notes))
                fields.Add(new("逐片备注", result.Notes));
        }
        else if (can(WaferRead) && !await Participants.AnyAsync(x => x.ProcessRunId == runId))
        {
            fields.Add(new("参与片记录", "此 Run 暂无已登记的参与片"));
        }
        var tracks = await db.Set<TrackEvent>().Where(x => x.ProcessRunId == runId)
            .OrderBy(x => x.OccurredAt).ThenBy(x => x.Id).Select(x => new { x.Id, x.EventType })
            .ToArrayAsync();
        foreach (var track in tracks)
            fields.Add(new($"{track.EventType} 过站记录 ID", track.Id.ToString()));
    }
}
