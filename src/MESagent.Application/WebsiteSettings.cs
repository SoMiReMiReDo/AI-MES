namespace MESagent.Application;

public static class WebsiteSettingsAccess
{
    public const string Read = "security.settings.read";
    public const string ManageAppearance = "security.settings.appearance.manage";
    public const string ManageSchedule = "security.settings.schedule.manage";
}

public sealed record WebsiteAppearance(string Name, string Subtitle, string DefaultTheme);

public sealed record WebsiteAppearanceSettings(WebsiteAppearance Appearance, string Etag);

public sealed record FabScheduleSettings(string DefaultView, string Etag);

public sealed record FabScheduleSettingsInput(string DefaultView);

public sealed record WebsiteSystemInfo(
    Guid FabId, string FabCode, string FabName, string Timezone, int IdleMinutes, int AbsoluteHours,
    bool CanManageAppearance, bool CanManageSchedule);
