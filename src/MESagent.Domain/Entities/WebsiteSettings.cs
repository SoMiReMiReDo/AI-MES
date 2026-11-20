namespace MESagent.Domain.Entities;

public sealed class SiteAppearance
{
    public static readonly Guid SingletonId = new("67c6f063-676d-452b-bdd1-3a4556b9a35c");
    public const string DefaultName = "MESagent";
    public const string DefaultSubtitle = "MANUFACTURING SYSTEM";
    public const string InitialTheme = "light";

    public Guid Id { get; set; } = SingletonId;
    public string Name { get; set; } = DefaultName;
    public string Subtitle { get; set; } = DefaultSubtitle;
    public string DefaultTheme { get; set; } = InitialTheme;
    public Guid Version { get; set; }
}

public sealed class FabSchedulePreference
{
    public const string InitialView = "week";

    public Guid FabId { get; set; }
    public string DefaultView { get; set; } = InitialView;
    public Guid Version { get; set; }
}
