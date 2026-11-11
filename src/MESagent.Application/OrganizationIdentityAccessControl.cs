using System.Text.Json;

namespace MESagent.Application;

public sealed class AppException(int status, string code, string message) : Exception(message)
{
    public int Status { get; } = status;
    public string Code { get; } = code;
}

public sealed class OrganizationIdentityAccessControlOptions
{
    public Guid DeploymentFabId { get; set; }
    public int IdleMinutes { get; set; } = 30;
    public int AbsoluteHours { get; set; } = 12;
    public int RestrictedMinutes { get; set; } = 10;
}

public sealed record RequestActor(Guid UserId, Guid SessionId, Guid CorrelationId);

public sealed record OrganizationIdentityAccessControlRequest(
    string Method,
    string Path,
    JsonElement Body,
    IReadOnlyDictionary<string, string> Query,
    string? Version);

public sealed record OrganizationIdentityAccessControlResult(object? Data = null, string? Version = null, int Status = 200);

public sealed record LoginRequest(string Username, string Password, Guid? TerminalId = null);

public sealed record LoginTicket(Guid UserId, Guid SessionId, bool PasswordChangeRequired, DateTime ExpiresAt);

public interface IOrganizationIdentityAccessControlBackend
{
    Task<LoginTicket> Login(LoginRequest request, Guid correlationId);

    Task<bool> ValidateSession(Guid userId, Guid sessionId);

    Task<OrganizationIdentityAccessControlResult> Execute(
        RequestActor actor,
        OrganizationIdentityAccessControlRequest request);

    Task RecordSecurityEvent(string type, string outcome, Guid correlation);

    Task RecoverAdmin(string username, string password, string reason);

    Task Bootstrap(string username, string password, string fabCode, string timezone);
}

// Every adapter calls the same application entry; no controller owns business rules.
public sealed class OrganizationIdentityAccessControlService(IOrganizationIdentityAccessControlBackend backend)
{
    public Task<LoginTicket> Login(LoginRequest request, Guid correlationId)
    {
        if (string.IsNullOrEmpty(request.Username)
            || request.Username.Length > 64
            || string.IsNullOrEmpty(request.Password)
            || request.Password.Length > 128)
            throw new AppException(401, "LoginFailed", "无法登录，请检查凭据或联系管理员");

        return backend.Login(request, correlationId);
    }

    public Task<bool> ValidateSession(Guid user, Guid session) => backend.ValidateSession(user, session);

    public Task<OrganizationIdentityAccessControlResult> Execute(
        RequestActor actor,
        OrganizationIdentityAccessControlRequest request) => backend.Execute(actor, request);
}

public static class PermissionCatalog
{
    public static readonly string[] All =
    [
        "org.structure.read",
        "org.structure.manage",
        "org.workers.read",
        "org.workers.manage",
        "org.workstations.read",
        "org.workstations.manage",
        "org.terminals.read",
        "org.terminals.manage",
        "security.users.read",
        "security.users.manage",
        "security.users.reset-password",
        "security.sessions.revoke",
        "security.roles.read",
        "security.roles.manage",
        "security.assignments.manage",
        "security.audit.read"
    ];

    public static readonly string[] Roles =
    [
        "SystemAdmin",
        "Operator",
        "ProcessEngineer",
        "QualityEngineer",
        "Planner",
        "EquipmentEngineer",
        "Viewer"
    ];
}
