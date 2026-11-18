using MESagent.Application;
using MESagent.Domain.Entities;
using MESagent.Infrastructure.Identity;
using MESagent.Infrastructure.QualityExceptionDisposition;
using Microsoft.EntityFrameworkCore;

namespace MESagent.Infrastructure.WorkbenchDashboardsAlertsScheduling;

internal sealed partial class WorkbenchBackend
{
    private static readonly string[] AlertKinds = ["issue", "hold", "disposition", "downtime"];

    private static string AlertPermission(string kind) => kind switch
    {
        "issue" or "hold" => WorkbenchAccess.Quality,
        "disposition" => WorkbenchAccess.Execution,
        "downtime" => WorkbenchAccess.Equipment,
        _ => throw Invalid("无效告警来源")
    };

    private string[] AllowedAlerts() => AlertKinds.Where(x => can(AlertPermission(x))).ToArray();

    private IQueryable<WorkbenchRecord> AlertSources(string kind)
    {
        if (kind == "issue")
            return from issue in QualityBackend.VisibleIssues(db, fabId).AsNoTracking()
                   where issue.Status == "Open" || issue.Status == "Acknowledged"
                   select new WorkbenchRecord
                   {
                       Id = issue.Id, Kind = kind, Label = issue.Title, Status = issue.Status,
                       OccurredAt = issue.ReportedAt, LotId = issue.LotId, EquipmentId = issue.EquipmentId,
                       Quantity = 1, Detail = issue.Description
                   };
        if (kind == "hold")
            return from hold in db.Set<HoldRecord>()
                   join lot in Lots on hold.LotId equals lot.Id
                   join reason in db.Set<ReasonCode>() on hold.ReasonCodeId equals reason.Id
                   where hold.ReleasedAt == null
                   select new WorkbenchRecord
                   {
                       Id = hold.Id, Kind = kind, Label = lot.LotNumber, Status = "Active",
                       OccurredAt = hold.HeldAt, LotId = lot.Id, EquipmentId = null, Quantity = 1, Detail = reason.Name
                   };
        if (kind == "disposition")
            return from lot in Lots
                   join step in db.Set<StepExecution>() on lot.Id equals step.LotId
                   join run in db.Set<ProcessRun>() on step.Id equals run.StepExecutionId
                   join tool in Equipment on run.EquipmentId equals tool.Id
                   where lot.Status == "AwaitingDisposition" && lot.CurrentRouteStepId == step.RouteStepId
                       && step.ExitedAt == null && run.TrackOutAt != null
                       && run.Id == db.Set<ProcessRun>().Where(x => x.StepExecutionId == step.Id)
                           .OrderByDescending(x => x.TrackInAt).ThenBy(x => x.Id).Select(x => x.Id).First()
                   select new WorkbenchRecord
                   {
                       Id = run.Id, Kind = kind, Label = lot.LotNumber, Status = lot.Status,
                       OccurredAt = run.TrackOutAt!.Value, LotId = lot.Id, EquipmentId = tool.Id, Quantity = 1,
                       Detail = step.Name
                   };
        return from downtime in db.Set<Downtime>()
               join tool in Equipment on downtime.EquipmentId equals tool.Id
               where downtime.EndedAt == null
               select new WorkbenchRecord
               {
                   Id = downtime.Id, Kind = kind, Label = tool.Code, Status = "Down", OccurredAt = downtime.StartedAt,
                   LotId = null, EquipmentId = tool.Id, Quantity = 1, Detail = downtime.Notes
               };
    }

    private IQueryable<MesUser> AlertRecipients(string kind)
    {
        var permission = AlertPermission(kind);
        return db.Users.AsNoTracking().Where(user => user.IsActive && !user.ForcePasswordChange
            && user.AccountType == "Human"
            && db.Set<UserFabScope>().Any(scope => scope.UserId == user.Id && scope.FabId == fabId)
            && (from membership in db.UserRoles
                join role in db.Roles on membership.RoleId equals role.Id
                join grant in db.Set<RolePermission>() on role.Id equals grant.RoleId
                join p in db.Set<Permission>() on grant.PermissionId equals p.Id
                where membership.UserId == user.Id && role.IsActive && p.IsActive && p.Code == permission
                select p.Id).Any());
    }

    private IQueryable<WorkbenchSubscription> PersonalSubscriptions =>
        db.Set<WorkbenchSubscription>().Where(x => x.FabId == fabId);

    private IQueryable<WorkbenchAlertReceipt> ReceiptRows =>
        db.Set<WorkbenchAlertReceipt>().Where(x => x.FabId == fabId);

    private sealed class AlertProjection
    {
        public WorkbenchRecord Source { get; init; } = null!;
        public DateTime? AcknowledgedAt { get; init; }
        public DateTime? SilencedUntil { get; init; }
        public bool Escalated { get; init; }
    }

    private IQueryable<AlertProjection> AlertRows(string kind, bool notifications)
    {
        var subscriptions = PersonalSubscriptions.Where(x => x.Kind == kind && x.Enabled);
        var receipts = ReceiptRows.Where(x => x.Kind == kind);
        var recipients = AlertRecipients(kind);
        return from source in AlertSources(kind)
               join own in receipts.Where(x => x.UserId == actor.UserId)
                   on source.Id equals own.SourceId into personal
               from own in personal.DefaultIfEmpty()
               let subscribed = subscriptions.Any(x => x.UserId == actor.UserId && x.EnabledAt <= source.OccurredAt)
               let escalated = notifications && subscriptions.Any(x => x.EscalateToId == actor.UserId
                   && x.EnabledAt <= source.OccurredAt && x.EscalationMinutes != null
                   && source.OccurredAt.AddMinutes(x.EscalationMinutes.Value) <= Now
                   && recipients.Any(u => u.Id == x.UserId)
                   && !receipts.Any(r => r.UserId == x.UserId && r.SourceId == source.Id
                       && (r.AcknowledgedAt != null || r.SilencedUntil > Now)))
               where !notifications || subscribed || escalated
               select new AlertProjection
               {
                   Source = new WorkbenchRecord
                   {
                       Id = source.Id, Kind = source.Kind, Label = source.Label, Status = source.Status,
                       OccurredAt = source.OccurredAt, LotId = source.LotId, EquipmentId = source.EquipmentId,
                       Quantity = source.Quantity, Detail = source.Detail
                   },
                   AcknowledgedAt = own.AcknowledgedAt, SilencedUntil = own.SilencedUntil,
                   Escalated = escalated
               };
    }

    private async Task<WorkbenchAlerts> Alerts(IReadOnlyDictionary<string, string> q)
    {
        var kind = q.GetValueOrDefault("kind", "");
        var mode = q.GetValueOrDefault("mode", "tasks");
        if (mode is not ("tasks" or "notifications"))
            throw Invalid("无效列表类别");
        var allowed = AllowedAlerts();
        if (kind != "")
        {
            require(AlertPermission(kind));
            allowed = [kind];
        }
        var page = Number(q, "page", 1, 1000000);
        var size = Number(q, "pageSize", 20, 100);
        if (allowed.Length == 0)
            return new(new([], 0, page, size), 0);

        var notifications = mode == "notifications";
        var query = allowed.Select(k => AlertRows(k, notifications)).Aggregate((left, right) => left.Concat(right));
        var unread = notifications
            ? await query.CountAsync(x => x.AcknowledgedAt == null
                && (x.SilencedUntil == null || x.SilencedUntil <= Now))
            : 0;
        if (notifications && q.GetValueOrDefault("includeSilenced") != "true")
            query = query.Where(x => x.SilencedUntil == null || x.SilencedUntil <= Now);
        var rows = query.OrderByDescending(x => x.Source.OccurredAt).ThenBy(x => x.Source.Kind)
            .ThenBy(x => x.Source.Id);
        var result = await Page(rows, q);
        return new(new(result.Items.Select(x => new WorkbenchAlert(
            x.Source, x.AcknowledgedAt, x.SilencedUntil, x.Escalated)).ToArray(),
            result.Total, result.Page, result.PageSize), unread);
    }

    private async Task<WorkbenchSubscriptionView[]> Subscriptions()
    {
        var own = await PersonalSubscriptions.Where(x => x.UserId == actor.UserId).ToArrayAsync();
        var views = new List<WorkbenchSubscriptionView>();
        foreach (var kind in AllowedAlerts())
        {
            var row = own.SingleOrDefault(x => x.Kind == kind);
            var people = await AlertRecipients(kind).Where(x => x.Id != actor.UserId)
                .OrderBy(x => x.Name).ThenBy(x => x.Id).Take(100)
                .Select(x => new WorkbenchOption(x.Id, "", x.Name)).ToArrayAsync();
            views.Add(new(kind, row?.Enabled ?? false, row?.EscalationMinutes, row?.EscalateToId, people));
        }
        return views.ToArray();
    }

    private async Task<OrganizationIdentityAccessControlResult> ChangeSubscription(
        string kind, WorkbenchSubscriptionInput input)
    {
        require(AlertPermission(kind));
        if ((input.EscalationMinutes == null) != (input.EscalateToId == null)
            || input.EscalationMinutes is < 1 or > 10080 || input.EscalateToId == actor.UserId)
            throw Invalid("升级需选择其他收件人并填写 1～10080 分钟");
        if (input.EscalateToId != null && !await AlertRecipients(kind).AnyAsync(x => x.Id == input.EscalateToId))
            throw Invalid("升级收件人须获本厂来源权限");
        var own = await PersonalSubscriptions.SingleOrDefaultAsync(x => x.UserId == actor.UserId && x.Kind == kind);
        if (own == null)
        {
            own = new() { FabId = fabId, UserId = actor.UserId, Kind = kind };
            db.Add(own);
        }
        if (input.Enabled && !own.Enabled)
            own.EnabledAt = Now;
        own.Enabled = input.Enabled;
        own.EscalationMinutes = input.EscalationMinutes;
        own.EscalateToId = input.EscalateToId;
        return new(Status: 204);
    }

    private async Task<OrganizationIdentityAccessControlResult> ChangeAlert(
        string kind, Guid id, WorkbenchAlertInput input)
    {
        require(AlertPermission(kind));
        if (!await AlertRows(kind, true).AnyAsync(x => x.Source.Id == id))
            throw Missing();
        if (input.Action is not ("acknowledge" or "silence" or "unsilence")
            || input.Action == "silence" && (input.Until is not { Kind: DateTimeKind.Utc }
                || input.Until <= Now || input.Until > Now.AddDays(7))
            || input.Action != "silence" && input.Until != null)
            throw Invalid("选择确认、取消静默或未来七天内的静默截止时间");
        var own = await ReceiptRows.SingleOrDefaultAsync(x => x.UserId == actor.UserId
            && x.Kind == kind && x.SourceId == id);
        if (own == null)
        {
            own = new() { FabId = fabId, UserId = actor.UserId, Kind = kind, SourceId = id };
            db.Add(own);
        }
        if (input.Action == "acknowledge")
            own.AcknowledgedAt ??= Now;
        else
            own.SilencedUntil = input.Until;
        return new(Status: 204);
    }
}
