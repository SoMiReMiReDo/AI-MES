using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using MESagent.Application;
using MESagent.Domain;
using MESagent.Domain.Entities;
using MESagent.Infrastructure.Identity;
using MESagent.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace MESagent.Infrastructure.OrganizationIdentityAccessControl;

public sealed partial class OrganizationIdentityAccessControlBackend(
    MesDbContext db,
    UserManager<MesUser> users,
    OrganizationIdentityAccessControlOptions options,
    TimeProvider clock) : IOrganizationIdentityAccessControlBackend
{
    private DateTime Now => clock.GetUtcNow().UtcDateTime;
    private string sourceChannel = "Web";
    private RequestActor actor = null!;
    private MesUser current = null!;
    private HashSet<string> permissions = [];
    private bool admin;
    private bool hasScope;
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private static void Fail(int status, string code, string message) => throw new AppException(status, code, message);

    private static string Tag(object? value) =>
        "\"" + Convert.ToBase64String(Encoding.UTF8.GetBytes(value?.ToString() ?? "")) + "\"";

    private static void Version(string? expected, object? actual)
    {
        if (expected is null)
            Fail(428, "VersionRequired", "请先读取对象版本");
        if (expected != Tag(actual))
            Fail(412, "VersionConflict", "内容已被其他操作修改，请刷新后比较");
    }

    private static void IdentityResult(IdentityResult result)
    {
        if (!result.Succeeded)
            Fail(result.Errors.Any(e => e.Code == "ConcurrencyFailure") ? 412 : 400,
                "ValidationFailed",
                string.Join("；", result.Errors.Select(e => e.Description)));
    }

    private static void Fields(JsonElement b, params string[] allowed)
    {
        if (b.ValueKind != JsonValueKind.Object)
            Fail(400, "ValidationFailed", "请求必须为 JSON 对象");
        foreach (var p in b.EnumerateObject())
            if (!allowed.Contains(p.Name))
                Fail(400, "ValidationFailed", "不允许的字段：" + p.Name);
    }

    private static string S(JsonElement b, string key, string fallback = "")
    {
        if (!b.TryGetProperty(key, out var v) || v.ValueKind == JsonValueKind.Null)
            return fallback;
        if (v.ValueKind != JsonValueKind.String)
            throw new AppException(400, "ValidationFailed", key + " 必须为文本");
        return v.GetString()!;
    }

    private static Guid? G(JsonElement b, string key)
    {
        if (!b.TryGetProperty(key, out var v) || v.ValueKind == JsonValueKind.Null)
            return null;
        if (v.ValueKind != JsonValueKind.String || !v.TryGetGuid(out var id))
            throw new AppException(400, "ValidationFailed", key + " 必须为有效 UUID");
        return id;
    }

    private static Guid[] Ids(JsonElement b, string key)
    {
        if (!b.TryGetProperty(key, out var v))
            return [];
        if (v.ValueKind != JsonValueKind.Array || v.GetArrayLength() > 100)
            throw new AppException(400, "ValidationFailed", key + " 必须为不超过 100 项的数组");
        return v.EnumerateArray()
            .Select(x => x.ValueKind == JsonValueKind.String && x.TryGetGuid(out var id)
                ? id
                : throw new AppException(400, "ValidationFailed", "无效 UUID"))
            .Distinct()
            .ToArray();
    }

    private static string[] Strings(JsonElement b, string key)
    {
        if (!b.TryGetProperty(key, out var v))
            return [];
        if (v.ValueKind != JsonValueKind.Array || v.GetArrayLength() > 100)
            throw new AppException(400, "ValidationFailed", key + " 必须为不超过 100 项的数组");
        return v.EnumerateArray()
            .Select(x => x.ValueKind == JsonValueKind.String
                ? x.GetString()!
                : throw new AppException(400, "ValidationFailed", "数组项目必须为文本"))
            .Distinct()
            .ToArray();
    }

    private static string Reason(JsonElement b) => OrganizationRules.Text(S(b, "reason"), 500);

    private async Task Lock() => await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(78001001)");

    private void Require(string permission)
    {
        if (!hasScope)
            Fail(403, "FabScopeRequired", "尚未配置部署厂区范围");
        if (!permissions.Contains(permission) || permission.StartsWith("security.") && !admin)
            Fail(403, "PermissionDenied", "没有执行该操作的权限");
    }

    private async Task LoadAccess()
    {
        hasScope = await db.Set<UserFabScope>()
            .AnyAsync(x => x.UserId == current.Id && x.FabId == options.DeploymentFabId);
        var roles = await (from ur in db.UserRoles
                           join r in db.Roles on ur.RoleId equals r.Id
                           where ur.UserId == current.Id && r.IsActive
                           select r).ToListAsync();
        admin = roles.Any(x => x.IsSystem && x.Name == "SystemAdmin");
        var roleIds = roles.Select(x => x.Id).ToArray();
        permissions = (await (from rp in db.Set<RolePermission>()
                              join p in db.Set<Permission>() on rp.PermissionId equals p.Id
                              where roleIds.Contains(rp.RoleId) && p.IsActive
                              select p.Code).ToListAsync()).ToHashSet();
    }

    private bool Valid(MesUser? user, UserSession? s) =>
        user is { IsActive: true, AccountType: "Human" }
        && s is not null
        && s.UserId == user.Id
        && s.RevokedAt is null
        && s.IdleExpiresAt > Now
        && s.AbsoluteExpiresAt > Now
        && s.SecurityStamp == user.SecurityStamp;

    public async Task<bool> ValidateSession(Guid userId, Guid sessionId)
    {
        var u = await db.Users.AsNoTracking().SingleOrDefaultAsync(x => x.Id == userId);
        var s = await db.Set<UserSession>().AsNoTracking().SingleOrDefaultAsync(x => x.Id == sessionId);
        return Valid(u, s);
    }

    public async Task RecordSecurityEvent(string type, string outcome, Guid correlation)
    {
        Event(type, outcome, correlation);
        await db.SaveChangesAsync();
    }

    private void Event(string type, string outcome, Guid correlation, Guid? user = null, Guid? session = null) =>
        db.Add(new SecurityEvent
        {
            OccurredAt = Now,
            EventType = type,
            Outcome = outcome,
            CorrelationId = correlation,
            UserId = user,
            SessionId = session
        });

    private void Audit(string type, Guid id, string action, object? before = null, object? after = null) =>
        db.Add(new AuditLog
        {
            UserId = actor.UserId,
            EntityType = type,
            EntityId = id,
            Action = action,
            BeforeState = before is null ? null : JsonSerializer.Serialize(before, Json),
            AfterState = after is null ? null : JsonSerializer.Serialize(after, Json),
            OccurredAt = Now,
            CorrelationId = actor.CorrelationId,
            SourceChannel = sourceChannel
        });

    private async Task Revoke(MesUser user, string reason)
    {
        foreach (var s in await db.Set<UserSession>()
            .Where(x => x.UserId == user.Id && x.RevokedAt == null)
            .ToListAsync())
        {
            s.RevokedAt = Now;
            s.RevokeReason = reason;
        }
        user.SecurityStamp = Guid.NewGuid().ToString();
        user.ConcurrencyStamp = Guid.NewGuid().ToString();
    }

    private async Task ProtectAdmin(MesUser target)
    {
        var isAdmin = await (from ur in db.UserRoles
                             join r in db.Roles on ur.RoleId equals r.Id
                             where ur.UserId == target.Id && r.IsSystem && r.Name == "SystemAdmin" && r.IsActive
                             select r.Id).AnyAsync();
        if (!isAdmin)
            return;
        if (target.Id == current.Id)
            Fail(409, "LastAdministrator",
                "不能通过管理入口锁定、停用自己或移除自己的管理权限");
        var others = await (from ur in db.UserRoles
                            join r in db.Roles on ur.RoleId equals r.Id
                            join u in db.Users on ur.UserId equals u.Id
                            join scope in db.Set<UserFabScope>() on u.Id equals scope.UserId
                            where r.IsSystem && r.Name == "SystemAdmin" && r.IsActive
                                && u.Id != target.Id
                                && u.IsActive
                                && (u.LockoutEnd == null || u.LockoutEnd <= clock.GetUtcNow())
                                && scope.FabId == options.DeploymentFabId
                            select u.Id).AnyAsync();
        if (!others)
            Fail(409, "LastAdministrator", "必须保留至少一名可用管理员");
    }

    public async Task<LoginTicket> Login(LoginRequest request, Guid correlationId)
    {
        await using var tx = await db.Database.BeginTransactionAsync();
        await Lock();
        var user = await users.FindByNameAsync(request.Username);
        var ok = user is { IsActive: true, AccountType: "Human" }
            && !await users.IsLockedOutAsync(user)
            && await users.CheckPasswordAsync(user, request.Password);
        if (!ok)
        {
            if (user is { IsActive: true } && !await users.IsLockedOutAsync(user))
                IdentityResult(await users.AccessFailedAsync(user));
            Event("Login", "Denied", correlationId, user?.Id);
            await db.SaveChangesAsync();
            await tx.CommitAsync();
            Fail(401, "LoginFailed", "无法登录，请检查凭据或联系管理员");
        }
        current = user!;
        await LoadAccess();
        if (request.TerminalId is Guid terminal)
        {
            if (!hasScope)
                Fail(403, "FabScopeRequired", "无终端厂区权限");
            var t = await db.Set<Terminal>().SingleOrDefaultAsync(
                x => x.Id == terminal && x.FabId == options.DeploymentFabId && x.IsActive);
            if (t is null)
                Fail(404, "ResourceNotFound", "终端不存在或不可用");
            await ValidateOrgParents(t!, true);
            t!.LastSeenAt = Now;
        }
        IdentityResult(await users.ResetAccessFailedCountAsync(user!));
        user!.LastLoginAt = Now;
        if (string.IsNullOrEmpty(user.SecurityStamp))
            user.SecurityStamp = Guid.NewGuid().ToString();
        var expiry = Now.AddMinutes(user.ForcePasswordChange ? options.RestrictedMinutes : options.AbsoluteHours * 60);
        var session = new UserSession
        {
            UserId = user.Id,
            CreatedAt = Now,
            LastActivityAt = Now,
            IdleExpiresAt = user.ForcePasswordChange ? expiry : Now.AddMinutes(options.IdleMinutes),
            AbsoluteExpiresAt = expiry,
            Restricted = user.ForcePasswordChange,
            TerminalId = request.TerminalId,
            SecurityStamp = user.SecurityStamp
        };
        db.Add(session);
        Event("Login", "Succeeded", correlationId, user.Id, session.Id);
        await db.SaveChangesAsync();
        await tx.CommitAsync();
        return new(user.Id, session.Id, user.ForcePasswordChange, expiry);
    }

    public async Task<OrganizationIdentityAccessControlResult> Execute(RequestActor a, OrganizationIdentityAccessControlRequest r)
    {
        actor = a;
        var isolation = r.Method == "GET" && r.Path != "auth/me"
            ? System.Data.IsolationLevel.RepeatableRead : System.Data.IsolationLevel.ReadCommitted;
        await using var tx = await db.Database.BeginTransactionAsync(isolation);
        // Serializes the small OrganizationIdentityAccessControl administrative workload and its shared parent/security invariants.
        // All mutations, including login and bootstrap, use this same database lock.
        if (r.Method != "GET" || r.Path == "auth/me")
            await Lock();
        db.ChangeTracker.Clear();
        current = await db.Users.SingleOrDefaultAsync(x => x.Id == a.UserId)
            ?? throw new AppException(401, "SessionInvalid", "会话已失效");
        var session = await db.Set<UserSession>().SingleOrDefaultAsync(x => x.Id == a.SessionId);
        if (!Valid(current, session))
            Fail(401, "SessionInvalid", "会话已失效，请重新登录");
        var restricted = session!.Restricted || current.ForcePasswordChange;
        if (restricted && r.Path is not ("auth/me" or "auth/logout" or "account/change-password"))
            Fail(403, "PasswordChangeRequired", "请先修改初始密码");
        await LoadAccess();
        OrganizationIdentityAccessControlResult result;
        if (r.Path.StartsWith("execution/") || r.Path.StartsWith("equipment/"))
            result = await new MESagent.Infrastructure.DispatchAndProductionExecution.ExecutionBackend(
                db, options.DeploymentFabId, actor, Require, p => hasScope && permissions.Contains(p), clock).Execute(r);
        else if (r.Path.StartsWith("production/"))
            result = await new MESagent.Infrastructure.LotWaferCarrierGenealogy.ProductionBackend(
                db, options.DeploymentFabId, actor, Require, p => hasScope && permissions.Contains(p), clock).Execute(r);
        else if (r.Path.StartsWith("engineering/") || r.Path.StartsWith("planning/"))
            result = await new MESagent.Infrastructure.ProductProcessVersionManagement.ManufacturingBackend(
                db, options.DeploymentFabId, actor, Require, clock).Execute(r);
        else if (r.Path.StartsWith("organization/"))
            result = await Organization(r);
        else if (r.Path.StartsWith("security/"))
            result = await Security(r);
        else
            result = await Account(r, session);
        if (!restricted && session.RevokedAt is null && (r.Method != "GET" || r.Path == "auth/me"))
        {
            session.LastActivityAt = Now;
            session.IdleExpiresAt = new[] { Now.AddMinutes(options.IdleMinutes), session.AbsoluteExpiresAt }.Min();
        }
        await db.SaveChangesAsync();
        await tx.CommitAsync();
        return result;
    }

    private async Task<object> Me()
    {
        if (current.ForcePasswordChange)
            return new { userId = current.Id, displayName = current.Name, passwordChangeRequired = true };
        var fab = hasScope
            ? await db.Set<Fab>().AsNoTracking().SingleOrDefaultAsync(x => x.Id == options.DeploymentFabId)
            : null;
        var roles = await users.GetRolesAsync(current);
        return new
        {
            userId = current.Id,
            username = current.UserName,
            displayName = current.Name,
            workerId = current.WorkerId,
            passwordChangeRequired = false,
            permissions = hasScope ? permissions.Order().ToArray() : [],
            roles,
            fab = fab is null ? null : new { fab.Id, fab.Name, fab.Code, fab.Timezone, fab.IsActive },
            etag = Tag(current.ConcurrencyStamp)
        };
    }

    private async Task<OrganizationIdentityAccessControlResult> Account(OrganizationIdentityAccessControlRequest r, UserSession session)
    {
        if (r.Path == "auth/me" && r.Method == "GET")
            return new(await Me(), Tag(current.ConcurrencyStamp));

        if (r.Path == "auth/logout" && r.Method == "POST")
        {
            session.RevokedAt = Now;
            session.RevokeReason = "Logout";
            Event("Logout", "Succeeded", actor.CorrelationId, current.Id, session.Id);
            return new(Status: 204);
        }

        if (r.Path == "account/profile" && r.Method == "PATCH")
        {
            Fields(r.Body, "displayName");
            Version(r.Version, current.ConcurrencyStamp);
            var before = current.Name;
            current.Name = OrganizationRules.Text(S(r.Body, "displayName"));
            current.ConcurrencyStamp = Guid.NewGuid().ToString();
            Audit("User", current.Id, "ProfileChanged", new { displayName = before },
                new { displayName = current.Name });
            return new(await Me(), Tag(current.ConcurrencyStamp));
        }

        if (r.Path == "account/change-password" && r.Method == "POST")
        {
            Fields(r.Body, "currentPassword", "newPassword");
            var pwd = S(r.Body, "newPassword");
            Password(pwd);
            if (await users.CheckPasswordAsync(current, pwd))
                Fail(400, "ValidationFailed", "新密码不能与旧密码相同");
            IdentityResult(await users.ChangePasswordAsync(current, S(r.Body, "currentPassword"), pwd));
            current.ForcePasswordChange = false;
            await Revoke(current, "PasswordChanged");
            Audit("User", current.Id, "PasswordChanged");
            return new(Status: 204);
        }

        if (r.Path == "account/sessions" && r.Method == "GET")
            return new(await Sessions(current.Id));

        if (r.Path == "account/sessions/revoke-all" && r.Method == "POST")
        {
            await Revoke(current, "UserRevokedAll");
            Audit("User", current.Id, "SessionsRevoked");
            return new(Status: 204);
        }

        var parts = r.Path.Split('/');
        if (parts.Length == 4 && parts[0] == "account" && parts[1] == "sessions"
            && parts[3] == "revoke" && r.Method == "POST" && Guid.TryParse(parts[2], out var id))
        {
            var s = await db.Set<UserSession>().SingleOrDefaultAsync(x => x.Id == id && x.UserId == current.Id)
                ?? throw new AppException(404, "ResourceNotFound", "会话不存在");
            s.RevokedAt ??= Now;
            s.RevokeReason = "UserRevoked";
            Audit("User", current.Id, "SessionRevoked", after: new { sessionId = id });
            return new(Status: 204);
        }

        throw new AppException(404, "ResourceNotFound", "接口不存在");
    }

    private static void Password(string pwd)
    {
        if (pwd.Length < 12 || pwd.Length > 128)
            Fail(400, "ValidationFailed", "密码长度须为 12～128 字符");
    }

    private async Task<object> Sessions(Guid id) =>
        await db.Set<UserSession>()
            .Where(x => x.UserId == id)
            .OrderByDescending(x => x.CreatedAt)
            .Take(100)
            .Select(x => new
            {
                x.Id,
                x.CreatedAt,
                x.LastActivityAt,
                x.IdleExpiresAt,
                x.AbsoluteExpiresAt,
                x.RevokedAt,
                x.RevokeReason,
                x.TerminalId,
                current = x.Id == actor.SessionId
            })
            .ToListAsync();
}
