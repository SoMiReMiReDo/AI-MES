using System.Globalization;
using System.Text.Json;
using MESagent.Application;
using MESagent.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MESagent.Infrastructure.ApplicationAuditReliability;

internal sealed partial class ReliabilityBackend
{
    private async Task<PageView<OperationAuditView>> AuditLogs(OrganizationIdentityAccessControlRequest request)
    {
        var to = Date(request, "to") ?? Now;
        var from = Date(request, "from") ?? to.AddDays(-7);
        if (to <= from || to - from > TimeSpan.FromDays(90))
            throw Invalid("查询时间范围须大于零且不超过 90 天");

        var modules = new List<string>();
        foreach (var (module, permission) in new (string, string)[]
        {
            ("engineering", "engineering.read"),
            ("planning", "planning.work-orders.read"),
            ("analytics", "planning.work-orders.read"),
            ("lots", WorkbenchAccess.Lots),
            ("carriers", "production.carriers.read"),
            ("execution", WorkbenchAccess.Execution),
            ("quality", WorkbenchAccess.Quality),
            ("equipment", WorkbenchAccess.Equipment),
            ("materials", "materials.wafer-sources.read"),
            ("settings", WebsiteSettingsAccess.Read),
            ("reliability", ReliabilityAccess.OutboxRead)
        })
            if (can(permission))
                modules.Add(module);
        if (can(WorkbenchAccess.Execution) && can(WorkbenchAccess.Equipment))
            modules.Add("workbench");
        if (can(IntegrationAccess.Simulator) && can(WorkbenchAccess.Equipment))
            modules.Add("simulator");
        if (can(IntegrationAccess.WorkOrderImport) && can("planning.work-orders.manage"))
            modules.Add("imports");

        var (page, size) = Paging(request);
        var logs = db.Set<AuditLog>().AsNoTracking()
            .Where(x => x.FabId == fabId && x.OccurredAt >= from && x.OccurredAt < to)
            .Concat(LegacyAudit(from, to));
        logs = logs.Where(x => modules.Contains(x.Module!) && (x.Module != "imports" || x.UserId == actor.UserId));
        if (FilterId(request, "actorId") is Guid actorId)
            logs = logs.Where(x => x.UserId == actorId);
        if (FilterId(request, "entityId") is Guid entityId)
            logs = logs.Where(x => x.EntityId == entityId);
        if (FilterId(request, "correlationId") is Guid correlationId)
            logs = logs.Where(x => x.CorrelationId == correlationId);
        if (Filter(request, "entityType", 64) is { } type)
            logs = logs.Where(x => x.EntityType == type);
        if (Filter(request, "action", 64) is { } action)
            logs = logs.Where(x => x.Action == action);
        if (Filter(request, "sourceChannel", 32) is { } source)
            logs = logs.Where(x => x.SourceChannel == source);

        var total = await logs.CountAsync();
        var selected = logs.OrderByDescending(x => x.OccurredAt).ThenByDescending(x => x.Id)
            .Skip((page - 1) * size).Take(size);
        var rows = await (from log in selected
                          join user in db.Users on log.UserId equals user.Id into users
                          from user in users.DefaultIfEmpty()
                          orderby log.OccurredAt descending, log.Id descending
                          select new { Log = log, Actor = user.Name }).ToArrayAsync();
        return new(rows.Select(row => new OperationAuditView(
            row.Log.Id, row.Log.UserId, row.Actor, row.Log.Module!, row.Log.EntityType, row.Log.EntityId,
            row.Log.Action, row.Log.SourceChannel, row.Log.OccurredAt, row.Log.CorrelationId,
            ReadState(row.Log.BeforeState), ReadState(row.Log.AfterState))).ToArray(), total, page, size);
    }

    // Older audit rows are immutable. Recover scope only from an explicit server-written Fab or an owned source.
    private IQueryable<AuditLog> LegacyAudit(DateTime from, DateTime to) =>
        db.Set<AuditLog>().FromSqlInterpolated($"""
            SELECT l.id, l.created_at, l.updated_at, l.xmin, l.user_id, {fabId} AS fab_id,
                   scope.module, l.entity_type, l.entity_id, l.action, l.before_state, l.after_state,
                   l.ip_address, l.user_agent, l.occurred_at, l.correlation_id, l.source_channel, l.reason_code_id
            FROM audit.audit_logs l
            CROSS JOIN LATERAL (
                SELECT CASE
                    WHEN l.entity_type IN ('Product','ProductRevision','Operation','Route','RouteVersion','RouteStep',
                        'Recipe','RecipeVersion','ChecklistItem','OutputDefinition','EngineeringDocument')
                        OR l.entity_type = 'NumberSequence'
                            AND l.action NOT IN ('AllocatedForWorkOrder','NumberAllocated')
                        THEN 'engineering'
                    WHEN l.entity_type = 'ProductionScheduleBlock' AND EXISTS (
                        SELECT 1 FROM workbench.production_schedule_blocks p
                        WHERE p.id = l.entity_id AND p.fab_id = {fabId}
                    ) THEN 'workbench'
                    WHEN l.after_state->>'fabId' = {fabId.ToString()} THEN CASE
                        WHEN l.entity_type = 'WorkOrder' THEN 'planning'
                        WHEN l.entity_type IN ('MachineConnection','MachineMessage') THEN 'simulator'
                        WHEN l.entity_type = 'ImportJob' THEN 'imports'
                        WHEN l.entity_type = 'EquipmentIdealRate' THEN 'workbench'
                        ELSE (
                            SELECT CASE WHEN count(DISTINCT split_part(e.event_type, '.', 1)) = 1 THEN
                                CASE min(split_part(e.event_type, '.', 1))
                                    WHEN 'Production' THEN CASE WHEN l.entity_type IN ('Carrier','Wafer')
                                        THEN 'carriers' ELSE 'lots' END
                                    WHEN 'Execution' THEN 'execution'
                                    WHEN 'Quality' THEN 'quality'
                                    WHEN 'Equipment' THEN 'equipment'
                                    WHEN 'Materials' THEN 'materials'
                                END
                            END
                            FROM audit.business_events e
                            WHERE e.correlation_id = l.correlation_id AND e.entity_id = l.entity_id
                                AND e.entity_type = l.entity_type
                                AND split_part(e.event_type, '.', 2) = l.action
                        )
                    END
                END AS module
            ) scope
            WHERE l.fab_id IS NULL AND l.module IS NULL AND l.occurred_at >= {from} AND l.occurred_at < {to}
                AND scope.module IS NOT NULL
            """).AsNoTracking();

    private static DateTime? Date(OrganizationIdentityAccessControlRequest request, string key)
    {
        var value = Filter(request, key, 48);
        if (value is null)
            return null;
        return DateTimeOffset.TryParse(
            value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var date)
            && date.Year >= 1900 && date.Year <= 2200 ? date.UtcDateTime : throw Invalid(key + " 时间格式无效");
    }

    private static JsonElement? ReadState(string? value) => value is null
        ? null : AuditState.Redact(JsonSerializer.Deserialize<JsonElement>(value));
}

internal static class AuditState
{
    private static readonly HashSet<string> Omitted =
    [
        "password", "passwordhash", "currentpassword", "newpassword", "temporarypassword", "securitystamp",
        "secret", "clientsecret", "apikey", "token", "accesstoken", "refreshtoken", "csrftoken", "requesttoken",
        "authorization", "cookie", "connectionstring", "storagepath", "filepath", "rawpayload", "content",
        "csv", "inputdata", "resultdata", "processsnapshot", "parameters"
    ];

    public static JsonElement Redact(JsonElement value) => JsonSerializer.SerializeToElement(Filter(value));

    private static object Filter(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.Object => value.EnumerateObject()
            .Where(x => !Omitted.Contains(string.Concat(x.Name.Where(char.IsLetterOrDigit)).ToLowerInvariant()))
            .ToDictionary(x => x.Name, x => Filter(x.Value)),
        JsonValueKind.Array => value.EnumerateArray().Select(Filter).ToArray(),
        _ => value
    };
}
