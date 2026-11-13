namespace MESagent.Application;

public sealed record PageView<T>(T[] Items, int Total, int Page, int PageSize);

public sealed record FabView(Guid Id, string Name, string Code, string Timezone, bool IsActive);

public sealed record CurrentUserView(
    Guid UserId,
    string DisplayName,
    bool PasswordChangeRequired,
    string? Username = null,
    Guid? WorkerId = null,
    string[]? Permissions = null,
    string[]? Roles = null,
    FabView? Fab = null,
    string? Etag = null);

public sealed record UserView(
    Guid Id,
    string Username,
    string DisplayName,
    Guid? WorkerId,
    bool IsActive,
    DateTimeOffset? LockoutEnd,
    bool ForcePasswordChange,
    DateTime? LastLoginAt,
    string Etag,
    Guid[] RoleIds,
    Guid[] FabIds);

public sealed record RoleView(
    Guid Id,
    string Name,
    bool IsActive,
    bool IsSystem,
    string Etag,
    string[] PermissionCodes,
    int MemberCount);

public sealed record CreateUserRequest(
    string Username,
    string DisplayName,
    string TemporaryPassword,
    Guid[] RoleIds,
    Guid[] FabIds,
    Guid? WorkerId = null);

public sealed record AccessRequest(Guid[] RoleIds, Guid[] FabIds, string Reason);

public sealed record CreateRoleRequest(string Name, string[] PermissionCodes);

public sealed record RolePermissionsRequest(string[] PermissionCodes, string Reason);

public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);

public sealed record ResetPasswordRequest(string TemporaryPassword, string Reason);

public sealed record ProfileRequest(string DisplayName);

public sealed record ReasonRequest(string Reason);

public sealed record BindWorkstationRequest(Guid? WorkstationId, string Reason);

public sealed record OrganizationCreateRequest(
    string Code,
    string Name,
    string? Description = null,
    Guid? FabId = null,
    Guid? AreaId = null,
    Guid? LineId = null,
    Guid? WorkstationId = null,
    Guid? DeviceUuid = null,
    string? LocationType = null);

public sealed class OrgRow
{
    public Guid Id { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public Guid FabId { get; set; }
    public Guid? AreaId { get; set; }
    public Guid? LineId { get; set; }
    public Guid? WorkstationId { get; set; }
    public Guid? DeviceUuid { get; set; }
    public string? LocationType { get; set; }
    public string? Timezone { get; set; }
    public DateTime? LastSeenAt { get; set; }
    [System.Text.Json.Serialization.JsonIgnore]
    public uint Version { get; set; }
    public string Etag =>
        "\"" + Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(Version.ToString())) + "\"";
}

public sealed record EngineeringFile(byte[] Content, string Filename);
