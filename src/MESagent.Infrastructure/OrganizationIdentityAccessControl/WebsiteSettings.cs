using System.Text.Json;
using MESagent.Application;
using MESagent.Domain.Entities;
using MESagent.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MESagent.Infrastructure.OrganizationIdentityAccessControl;

public sealed partial class OrganizationIdentityAccessControlBackend
{
    private async Task<OrganizationIdentityAccessControlResult> WebsiteSettings(
        OrganizationIdentityAccessControlRequest request)
    {
        Require(WebsiteSettingsAccess.Read);
        return request.Path switch
        {
            "settings/system" when request.Method == "GET" => new(await WebsiteSystemInformation()),
            "settings/appearance" => await ManageSiteAppearance(request),
            "settings/schedule" => await ManageSchedulePreference(request),
            _ => throw new AppException(404, "ResourceNotFound", "设置不存在")
        };
    }

    private async Task<bool> HasAllFabScopes() =>
        !await db.Set<Fab>().AnyAsync(fab => !db.Set<UserFabScope>()
            .Any(scope => scope.UserId == actor.UserId && scope.FabId == fab.Id));

    private async Task<WebsiteSystemInfo> WebsiteSystemInformation()
    {
        var fab = await db.Set<Fab>().AsNoTracking().SingleAsync(x => x.Id == options.DeploymentFabId);
        return new(fab.Id, fab.Code, fab.Name, fab.Timezone, options.IdleMinutes, options.AbsoluteHours,
            permissions.Contains(WebsiteSettingsAccess.ManageAppearance) && await HasAllFabScopes(),
            permissions.Contains(WebsiteSettingsAccess.ManageSchedule));
    }

    private async Task<OrganizationIdentityAccessControlResult> ManageSiteAppearance(
        OrganizationIdentityAccessControlRequest request)
    {
        if (request.Method == "PUT")
        {
            Require(WebsiteSettingsAccess.ManageAppearance);
            if (!await HasAllFabScopes())
                Fail(403, "AllFabScopesRequired", "站点外观需要全厂区授权");
        }

        var site = await db.Set<SiteAppearance>().SingleAsync();
        var before = WebsiteBranding.View(site);
        if (request.Method == "PUT")
        {
            Version(request.Version, site.Version);
            Fields(request.Body, "name", "subtitle", "defaultTheme");
            var name = SettingsText(S(request.Body, "name"), 60, "站点名称", required: true);
            if (!request.Body.TryGetProperty("subtitle", out var subtitleField)
                || subtitleField.ValueKind != JsonValueKind.String)
                Fail(400, "ValidationFailed", "副标题必须为文本");
            var subtitle = SettingsText(S(request.Body, "subtitle"), 100, "副标题");
            var theme = S(request.Body, "defaultTheme");
            if (theme is not ("light" or "dark"))
                Fail(400, "ValidationFailed", "默认主题必须为浅色或深色");
            site.Name = name;
            site.Subtitle = subtitle;
            site.DefaultTheme = theme;
            site.Version = Guid.NewGuid();
            AuditWebsiteSetting(nameof(SiteAppearance), site.Id, "SiteAppearanceUpdated", before,
                WebsiteBranding.View(site));
        }

        var tag = Tag(site.Version);
        return new(new WebsiteAppearanceSettings(WebsiteBranding.View(site), tag), tag);
    }

    private async Task<OrganizationIdentityAccessControlResult> ManageSchedulePreference(
        OrganizationIdentityAccessControlRequest request)
    {
        if (request.Method == "PUT")
            Require(WebsiteSettingsAccess.ManageSchedule);

        var fabId = options.DeploymentFabId;
        var preference = await db.Set<FabSchedulePreference>().SingleOrDefaultAsync(x => x.FabId == fabId);
        var defaultView = preference?.DefaultView ?? FabSchedulePreference.InitialView;
        var version = preference?.Version ?? Guid.Empty;
        if (request.Method == "PUT")
        {
            Version(request.Version, version);
            Fields(request.Body, "defaultView");
            var value = S(request.Body, "defaultView");
            if (value is not ("day" or "week"))
                Fail(400, "ValidationFailed", "排程默认视图必须为日或周");
            if (preference is null)
            {
                preference = new FabSchedulePreference { FabId = fabId };
                db.Add(preference);
            }

            version = Guid.NewGuid();
            preference.Version = version;
            preference.DefaultView = value;
            AuditWebsiteSetting(nameof(FabSchedulePreference), fabId, "FabSchedulePreferenceUpdated",
                new { DefaultView = defaultView }, new { DefaultView = value });
            defaultView = value;
        }

        var tag = Tag(version);
        return new(new FabScheduleSettings(defaultView, tag), tag);
    }

    private static string SettingsText(string value, int maxLength, string field, bool required = false)
    {
        var trimmed = value.Trim();
        if (value.Any(char.IsControl) || trimmed.Length > maxLength || (required && trimmed.Length == 0))
            Fail(
                400,
                "ValidationFailed",
                $"{field}须为{(required ? " 1～" : "不超过 ")}{maxLength} 个可显示字符");
        return trimmed;
    }

    private void AuditWebsiteSetting(string type, Guid id, string action, object before, object after) =>
        db.Add(new AuditLog
        {
            FabId = options.DeploymentFabId,
            Module = "settings",
            UserId = actor.UserId,
            EntityType = type,
            EntityId = id,
            Action = action,
            BeforeState = JsonSerializer.Serialize(before, Json),
            AfterState = JsonSerializer.Serialize(after, Json),
            OccurredAt = Now,
            CorrelationId = actor.CorrelationId,
            SourceChannel = sourceChannel
        });
}

public static class WebsiteBranding
{
    public static async Task<WebsiteAppearance> Read(MesDbContext db, CancellationToken cancellationToken)
    {
        var site = await db.Set<SiteAppearance>().AsNoTracking().SingleOrDefaultAsync(cancellationToken);
        return View(site ?? new SiteAppearance());
    }

    internal static WebsiteAppearance View(SiteAppearance site) =>
        new(site.Name, site.Subtitle, site.DefaultTheme);
}
