using System.Text.RegularExpressions;
using MESagent.Application;
using MESagent.Domain;
using MESagent.Domain.Entities;
using MESagent.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace MESagent.Infrastructure.OrganizationIdentityAccessControl;

public sealed partial class OrganizationIdentityAccessControlBackend
{
    private async Task<object> UserDto(MesUser u) => new
    {
        id = u.Id,
        username = u.UserName,
        displayName = u.Name,
        u.WorkerId,
        u.IsActive,
        u.LockoutEnd,
        u.ForcePasswordChange,
        u.LastLoginAt,
        etag = Tag(u.ConcurrencyStamp),
        roleIds = await db.UserRoles.Where(x => x.UserId == u.Id).Select(x => x.RoleId).ToArrayAsync(),
        fabIds = await db.Set<UserFabScope>().Where(x => x.UserId == u.Id).Select(x => x.FabId).ToArrayAsync()
    };

    private async Task<object> RoleDto(MesRole r) => new
    {
        id = r.Id,
        name = r.Name,
        r.IsActive,
        r.IsSystem,
        etag = Tag(r.ConcurrencyStamp),
        permissionCodes = await (from rp in db.Set<RolePermission>()
                                 join p in db.Set<Permission>() on rp.PermissionId equals p.Id
                                 where rp.RoleId == r.Id
                                 select p.Code).OrderBy(x => x).ToArrayAsync(),
        memberCount = await db.UserRoles.CountAsync(x => x.RoleId == r.Id)
    };

    private async Task<OrganizationIdentityAccessControlResult> Security(OrganizationIdentityAccessControlRequest r)
    {
        var parts = r.Path.Split('/');
        if (parts[1] == "users")
            return await UserManagement(r, parts);
        if (parts[1] == "roles")
            return await RoleManagement(r, parts);
        if (parts[1] == "permissions" && parts.Length == 2 && r.Method == "GET")
        {
            Require("security.roles.read");
            return new(await db.Set<Permission>()
                .Where(x => x.IsActive && PermissionCatalog.All.Contains(x.Code))
                .OrderBy(x => x.Code)
                .Select(x => new { x.Code, x.Name, x.Description })
                .ToListAsync());
        }
        if (parts.Length == 2 && r.Method == "GET" && parts[1] is "audit-logs" or "events")
        {
            Require("security.audit.read");
            var (page, size, _) = Page(r);
            var from = r.Query.TryGetValue("from", out var f) ? DateTimeOffset.Parse(f).UtcDateTime : Now.AddDays(-7);
            var to = r.Query.TryGetValue("to", out var t) ? DateTimeOffset.Parse(t).UtcDateTime : Now;
            if (to <= from || to - from > TimeSpan.FromDays(90))
                Fail(400, "ValidationFailed", "查询时间范围须大于零且不超过 90 天");
            if (parts[1] == "events")
            {
                var q = db.Set<SecurityEvent>().Where(x => x.OccurredAt >= from && x.OccurredAt < to);
                if (r.Query.TryGetValue("eventType", out var ev))
                    q = q.Where(x => x.EventType == ev);
                if (r.Query.TryGetValue("outcome", out var oc))
                    q = q.Where(x => x.Outcome == oc);
                if (r.Query.TryGetValue("userId", out var ui) && Guid.TryParse(ui, out var user))
                    q = q.Where(x => x.UserId == user);
                return new(new
                {
                    items = await q
                        .OrderByDescending(x => x.OccurredAt)
                        .ThenBy(x => x.Id)
                        .Skip((page - 1) * size)
                        .Take(size)
                        .ToListAsync(),
                    total = await q.CountAsync(),
                    page,
                    pageSize = size
                });
            }

            string[] types = ["Fab", "Area", "Line", "Location", "Worker", "Workstation", "Terminal", "User", "Role"];
            var logs = db.Set<AuditLog>()
                .Where(x => types.Contains(x.EntityType) && x.OccurredAt >= from && x.OccurredAt < to);
            // Organization audit rows inherit the source object's Fab scope before count/pagination.
            logs = logs.Where(x => x.EntityType == "User" || x.EntityType == "Role"
                || x.EntityType == "Fab" && x.EntityId == options.DeploymentFabId
                || x.EntityType == "Area"
                    && db.Set<Area>().Any(a => a.Id == x.EntityId && a.FabId == options.DeploymentFabId)
                || x.EntityType == "Worker"
                    && db.Set<Worker>().Any(w => w.Id == x.EntityId && w.FabId == options.DeploymentFabId)
                || x.EntityType == "Terminal"
                    && db.Set<Terminal>().Any(t => t.Id == x.EntityId && t.FabId == options.DeploymentFabId)
                || x.EntityType == "Line"
                    && db.Set<Line>().Any(l => l.Id == x.EntityId
                        && db.Set<Area>().Any(a => a.Id == l.AreaId && a.FabId == options.DeploymentFabId))
                || x.EntityType == "Location"
                    && db.Set<Location>().Any(l => l.Id == x.EntityId
                        && db.Set<Area>().Any(a => a.Id == l.AreaId && a.FabId == options.DeploymentFabId))
                || x.EntityType == "Workstation"
                    && db.Set<Workstation>().Any(w => w.Id == x.EntityId
                        && db.Set<Area>().Any(a => a.Id == w.AreaId && a.FabId == options.DeploymentFabId)));

            if (r.Query.TryGetValue("actorId", out var au) && Guid.TryParse(au, out var actorId))
                logs = logs.Where(x => x.UserId == actorId);
            if (r.Query.TryGetValue("entityId", out var ei) && Guid.TryParse(ei, out var entityId))
                logs = logs.Where(x => x.EntityId == entityId);
            if (r.Query.TryGetValue("entityType", out var et))
                logs = logs.Where(x => x.EntityType == et);
            if (r.Query.TryGetValue("action", out var action))
                logs = logs.Where(x => x.Action == action);
            return new(new
            {
                items = await logs
                    .OrderByDescending(x => x.OccurredAt)
                    .ThenBy(x => x.Id)
                    .Skip((page - 1) * size)
                    .Take(size)
                    .Select(x => new
                    {
                        x.Id,
                        x.UserId,
                        x.EntityType,
                        x.EntityId,
                        x.Action,
                        x.BeforeState,
                        x.AfterState,
                        x.OccurredAt,
                        x.CorrelationId
                    })
                    .ToListAsync(),
                total = await logs.CountAsync(),
                page,
                pageSize = size
            });
        }
        throw new AppException(404, "ResourceNotFound", "接口不存在");
    }

    private async Task<OrganizationIdentityAccessControlResult> UserManagement(OrganizationIdentityAccessControlRequest r, string[] parts)
    {
        if (parts.Length == 2 && r.Method == "GET")
        {
            Require("security.users.read");
            var (page, size, key) = Page(r);
            var q = db.Users.AsQueryable();
            if (key != "")
                q = q.Where(x => x.UserName!.Contains(key) || x.Name.Contains(key));
            if (r.Query.TryGetValue("roleId", out var raw) && Guid.TryParse(raw, out var role))
                q = q.Where(x => db.UserRoles.Any(ur => ur.UserId == x.Id && ur.RoleId == role));
            if (r.Query.TryGetValue("status", out var state))
                q = state switch
                {
                    "inactive" => q.Where(x => !x.IsActive),
                    "active" => q.Where(x => x.IsActive),
                    "locked" => q.Where(x => x.LockoutEnd > clock.GetUtcNow()),
                    "password-change" => q.Where(x => x.ForcePasswordChange),
                    _ => q
                };
            var list = await q
                .OrderBy(x => x.UserName)
                .ThenBy(x => x.Id)
                .Skip((page - 1) * size)
                .Take(size)
                .ToListAsync();
            var items = new List<object>();
            foreach (var u in list)
                items.Add(await UserDto(u));
            return new(new { items, total = await q.CountAsync(), page, pageSize = size });
        }

        if (parts.Length == 2 && r.Method == "POST")
        {
            Require("security.users.manage");
            Require("security.assignments.manage");
            Fields(r.Body, "username", "displayName", "workerId", "temporaryPassword", "roleIds", "fabIds");
            var name = S(r.Body, "username");
            Username(name);
            Password(S(r.Body, "temporaryPassword"));
            var u = new MesUser
            {
                Id = Guid.CreateVersion7(),
                UserName = name,
                Name = OrganizationRules.Text(S(r.Body, "displayName")),
                WorkerId = G(r.Body, "workerId"),
                ForcePasswordChange = true,
                LockoutEnabled = true
            };
            var fabs = Ids(r.Body, "fabIds");
            await ValidateWorker(u, fabs);
            IdentityResult(await users.CreateAsync(u, S(r.Body, "temporaryPassword")));
            await Assign(u, Ids(r.Body, "roleIds"), fabs);
            await db.SaveChangesAsync();
            var dto = await UserDto(u);
            Audit("User", u.Id, "Created", after: dto);
            return new(dto, Tag(u.ConcurrencyStamp), 201);
        }

        if (parts.Length < 3 || !Guid.TryParse(parts[2], out var id))
            throw new AppException(404, "ResourceNotFound", "账号不存在");
        var permission = parts.Length > 3
            ? parts[3] switch
            {
                "reset-password" => "security.users.reset-password",
                "sessions" => "security.sessions.revoke",
                "access" => "security.assignments.manage",
                _ => "security.users.manage"
            }
            : r.Method == "GET"
                ? "security.users.read"
                : "security.users.manage";
        Require(permission);
        var target = await db.Users.SingleOrDefaultAsync(x => x.Id == id)
            ?? throw new AppException(404, "ResourceNotFound", "账号不存在");
        if (parts.Length == 3 && r.Method == "GET")
            return new(await UserDto(target), Tag(target.ConcurrencyStamp));
        if (parts.Length == 4 && parts[3] == "sessions" && r.Method == "GET")
            return new(await Sessions(id));
        if (parts.Length == 5 && parts[3] == "sessions" && parts[4] == "revoke-all" && r.Method == "POST")
        {
            Fields(r.Body, "reason");
            await Revoke(target, Reason(r.Body));
            Audit("User", id, "SessionsRevoked", after: new { reason = Reason(r.Body) });
            return new(Status: 204);
        }
        Version(r.Version, target.ConcurrencyStamp);
        var before = await UserDto(target);
        string action;
        if (parts.Length == 3 && r.Method == "PATCH")
        {
            Fields(r.Body, "displayName", "workerId");
            if (r.Body.TryGetProperty("displayName", out _))
                target.Name = OrganizationRules.Text(S(r.Body, "displayName"));
            if (r.Body.TryGetProperty("workerId", out _))
            {
                target.WorkerId = G(r.Body, "workerId");
                await ValidateWorker(target, await db.Set<UserFabScope>()
                    .Where(x => x.UserId == id)
                    .Select(x => x.FabId)
                    .ToArrayAsync());
                await Revoke(target, "WorkerBindingChanged");
            }
            action = "Updated";
        }
        else if (parts.Length == 4 && parts[3] == "access" && r.Method == "PUT")
        {
            Fields(r.Body, "roleIds", "fabIds", "reason");
            Reason(r.Body);
            var roles = Ids(r.Body, "roleIds");
            var fabs = Ids(r.Body, "fabIds");
            var systemRole = await db.Roles.SingleAsync(x => x.IsSystem && x.Name == "SystemAdmin");
            if (!roles.Contains(systemRole.Id) || !fabs.Contains(options.DeploymentFabId))
                await ProtectAdmin(target);
            await ValidateWorker(target, fabs);
            await Assign(target, roles, fabs);
            await Revoke(target, "AccessChanged");
            action = "AccessChanged";
        }
        else if (parts.Length == 4 && r.Method == "POST")
        {
            Fields(r.Body, parts[3] == "reset-password"
                ? ["temporaryPassword", "reason"]
                : parts[3] == "lock"
                    ? ["until", "reason"]
                    : ["reason"]);
            Reason(r.Body);
            switch (parts[3])
            {
                case "activate":
                    await ValidateWorker(target, await db.Set<UserFabScope>()
                        .Where(x => x.UserId == id)
                        .Select(x => x.FabId)
                        .ToArrayAsync());
                    target.IsActive = true;
                    action = "Activated";
                    break;
                case "deactivate":
                    await ProtectAdmin(target);
                    if (target.Id == current.Id)
                        Fail(409, "LastAdministrator", "不能停用自己");
                    target.IsActive = false;
                    await Revoke(target, "Deactivated");
                    action = "Deactivated";
                    break;
                case "lock":
                    await ProtectAdmin(target);
                    if (target.Id == current.Id)
                        Fail(409, "LastAdministrator", "不能锁定自己");
                    var until = DateTimeOffset.Parse(S(r.Body, "until"));
                    if (until <= clock.GetUtcNow() || until > clock.GetUtcNow().AddDays(30))
                        Fail(400, "ValidationFailed", "锁定到期须在未来 30 天内");
                    target.LockoutEnd = until;
                    await Revoke(target, "Locked");
                    action = "Locked";
                    break;
                case "unlock":
                    target.LockoutEnd = null;
                    target.AccessFailedCount = 0;
                    action = "Unlocked";
                    break;
                case "reset-password":
                    if (id == current.Id)
                        Fail(403, "PermissionDenied", "本人请使用修改密码入口");
                    var pwd = S(r.Body, "temporaryPassword");
                    Password(pwd);
                    var token = await users.GeneratePasswordResetTokenAsync(target);
                    IdentityResult(await users.ResetPasswordAsync(target, token, pwd));
                    target.ForcePasswordChange = true;
                    await Revoke(target, "PasswordReset");
                    action = "PasswordReset";
                    break;
                default:
                    throw new AppException(404, "ResourceNotFound", "接口不存在");
            }
        }
        else
            throw new AppException(404, "ResourceNotFound", "接口不存在");
        target.ConcurrencyStamp = Guid.NewGuid().ToString();
        await db.SaveChangesAsync();
        var after = await UserDto(target);
        Audit("User", id, action, before, new { data = after, reason = S(r.Body, "reason") });
        return action == "PasswordReset" ? new(Status: 204) : new(after, Tag(target.ConcurrencyStamp));
    }

    private static void Username(string value)
    {
        if (!Regex.IsMatch(value, "^[a-zA-Z0-9_.@-]{3,64}$"))
            Fail(400, "ValidationFailed", "用户名须为 3～64 位字母、数字或 _.-@");
    }

    private async Task ValidateWorker(MesUser u, Guid[] fabs)
    {
        if (fabs.Any(x => x != options.DeploymentFabId))
            Fail(400, "ValidationFailed", "仅支持部署厂区");
        if (u.WorkerId is not Guid id)
            return;
        var w = await db.Set<Worker>().SingleOrDefaultAsync(x => x.Id == id && x.FabId == options.DeploymentFabId)
            ?? throw new AppException(404, "ResourceNotFound", "员工不存在或无权访问");
        if (!w.IsActive || !fabs.Contains(w.FabId))
            Fail(409, "ParentInactive", "员工必须有效且在账号厂区范围内");
        if (await db.Users.AnyAsync(x => x.WorkerId == id && x.Id != u.Id))
            Fail(409, "WorkerAlreadyLinked", "员工已绑定其它账号");
    }

    private async Task Assign(MesUser u, Guid[] roleIds, Guid[] fabs)
    {
        if (fabs.Any(x => x != options.DeploymentFabId)
            || await db.Roles.CountAsync(x => roleIds.Contains(x.Id) && x.IsActive) != roleIds.Length)
            Fail(400, "ValidationFailed", "角色或厂区范围无效");
        var existing = await db.UserRoles.Where(x => x.UserId == u.Id).ToListAsync();
        db.RemoveRange(existing.Where(x => !roleIds.Contains(x.RoleId)));
        foreach (var id in roleIds.Where(x => !existing.Any(e => e.RoleId == x)))
            db.Add(new IdentityUserRole<Guid> { UserId = u.Id, RoleId = id });
        var scopes = await db.Set<UserFabScope>().Where(x => x.UserId == u.Id).ToListAsync();
        db.RemoveRange(scopes.Where(x => !fabs.Contains(x.FabId)));
        foreach (var id in fabs.Where(x => !scopes.Any(e => e.FabId == x)))
            db.Add(new UserFabScope { UserId = u.Id, FabId = id });
        u.ConcurrencyStamp = Guid.NewGuid().ToString();
    }

    private async Task SetPermissions(MesRole role, string[] codes)
    {
        if (codes.Any(x => !PermissionCatalog.All.Contains(x) || x.StartsWith("security.") && !role.IsSystem))
            Fail(400, "ValidationFailed", "权限不存在或不能授予普通角色");
        var defs = await db.Set<Permission>().Where(x => codes.Contains(x.Code) && x.IsActive).ToListAsync();
        if (defs.Count != codes.Length)
            Fail(400, "ValidationFailed", "权限已停用");
        var existing = await db.Set<RolePermission>().Where(x => x.RoleId == role.Id).ToListAsync();
        var ids = defs.Select(x => x.Id).ToArray();
        db.RemoveRange(existing.Where(x => !ids.Contains(x.PermissionId)));
        foreach (var id in ids.Where(x => !existing.Any(e => e.PermissionId == x)))
            db.Add(new RolePermission { RoleId = role.Id, PermissionId = id });
    }

    private async Task<OrganizationIdentityAccessControlResult> RoleManagement(OrganizationIdentityAccessControlRequest r, string[] parts)
    {
        Require(r.Method == "GET" ? "security.roles.read" : "security.roles.manage");
        if (parts.Length == 2 && r.Method == "GET")
        {
            var (page, size, key) = Page(r);
            var q = db.Roles.Where(x => x.Name!.Contains(key));
            var rows = await q.OrderBy(x => x.Name).Skip((page - 1) * size).Take(size).ToListAsync();
            var items = new List<object>();
            foreach (var row in rows)
                items.Add(await RoleDto(row));
            return new(new { items, total = await q.CountAsync(), page, pageSize = size });
        }

        if (parts.Length == 2 && r.Method == "POST")
        {
            Fields(r.Body, "name", "permissionCodes");
            var name = OrganizationRules.Text(S(r.Body, "name"), 64);
            if (PermissionCatalog.Roles.Contains(name, StringComparer.OrdinalIgnoreCase))
                Fail(409, "CodeConflict", "角色名已保留");
            var role = new MesRole
            {
                Id = Guid.CreateVersion7(),
                Name = name,
                NormalizedName = name.ToUpperInvariant(),
                ConcurrencyStamp = Guid.NewGuid().ToString()
            };
            db.Add(role);
            await SetPermissions(role, Strings(r.Body, "permissionCodes"));
            await db.SaveChangesAsync();
            var dto = await RoleDto(role);
            Audit("Role", role.Id, "Created", after: dto);
            return new(dto, Tag(role.ConcurrencyStamp), 201);
        }

        if (parts.Length < 3 || !Guid.TryParse(parts[2], out var id))
            throw new AppException(404, "ResourceNotFound", "角色不存在");
        var target = await db.Roles.SingleOrDefaultAsync(x => x.Id == id)
            ?? throw new AppException(404, "ResourceNotFound", "角色不存在");
        if (parts.Length == 3 && r.Method == "GET")
            return new(await RoleDto(target), Tag(target.ConcurrencyStamp));
        if (target.IsSystem)
            Fail(403, "PermissionDenied", "受保护系统角色不可修改");
        Version(r.Version, target.ConcurrencyStamp);
        var before = await RoleDto(target);
        string action;
        if (parts.Length == 3 && r.Method == "PATCH")
        {
            Fields(r.Body, "name");
            var name = OrganizationRules.Text(S(r.Body, "name"), 64);
            if (PermissionCatalog.Roles.Contains(name, StringComparer.OrdinalIgnoreCase))
                Fail(409, "CodeConflict", "角色名已保留");
            target.Name = name;
            target.NormalizedName = name.ToUpperInvariant();
            action = "Renamed";
        }
        else if (parts.Length == 4 && parts[3] == "permissions" && r.Method == "PUT")
        {
            Fields(r.Body, "permissionCodes", "reason");
            Reason(r.Body);
            await SetPermissions(target, Strings(r.Body, "permissionCodes"));
            action = "PermissionsChanged";
        }
        else if (parts.Length == 4 && r.Method == "POST" && parts[3] is "activate" or "deactivate")
        {
            Fields(r.Body, "reason");
            Reason(r.Body);
            target.IsActive = parts[3] == "activate";
            action = target.IsActive ? "Activated" : "Deactivated";
        }
        else
            throw new AppException(404, "ResourceNotFound", "接口不存在");
        foreach (var user in await db.Users
            .Where(x => db.UserRoles.Any(ur => ur.UserId == x.Id && ur.RoleId == id))
            .ToListAsync())
            await Revoke(user, "RoleChanged");
        target.ConcurrencyStamp = Guid.NewGuid().ToString();
        await db.SaveChangesAsync();
        var after = await RoleDto(target);
        Audit("Role", id, action, before, new { data = after, reason = S(r.Body, "reason") });
        return new(after, Tag(target.ConcurrencyStamp));
    }

    public async Task RecoverAdmin(string username, string password, string reason)
    {
        Password(password);
        OrganizationRules.Text(reason, 500);
        await using var tx = await db.Database.BeginTransactionAsync();
        await Lock();
        var u = await users.FindByNameAsync(username)
            ?? throw new AppException(404, "ResourceNotFound", "管理员不存在");
        if (!await (from ur in db.UserRoles
                    join r in db.Roles on ur.RoleId equals r.Id
                    where ur.UserId == u.Id && r.IsSystem && r.Name == "SystemAdmin"
                    select r.Id).AnyAsync())
            Fail(403, "PermissionDenied", "仅可恢复既有系统管理员");
        var token = await users.GeneratePasswordResetTokenAsync(u);
        IdentityResult(await users.ResetPasswordAsync(u, token, password));
        u.IsActive = true;
        u.LockoutEnd = null;
        u.AccessFailedCount = 0;
        u.ForcePasswordChange = true;
        if (!await db.Set<UserFabScope>()
            .AnyAsync(x => x.UserId == u.Id && x.FabId == options.DeploymentFabId))
            db.Add(new UserFabScope { UserId = u.Id, FabId = options.DeploymentFabId });
        await Revoke(u, "LocalAdminRecovery");
        actor = new(u.Id, Guid.Empty, Guid.NewGuid());
        sourceChannel = "LocalRecovery";
        Audit("User", u.Id, "Recovered", after: new { reason });
        await db.SaveChangesAsync();
        await tx.CommitAsync();
    }

    public async Task Bootstrap(string username, string password, string fabCode, string timezone)
    {
        Username(username);
        Password(password);
        OrganizationRules.Timezone(timezone);
        if (options.DeploymentFabId == Guid.Empty)
            throw new ArgumentException("必须配置 Mes:DeploymentFabId");
        await using var tx = await db.Database.BeginTransactionAsync();
        await Lock();
        if (await db.Roles.AnyAsync(x => x.IsSystem)
            || await db.Users.AnyAsync(x => x.NormalizedUserName == username.ToUpperInvariant()))
            Fail(409, "AlreadyInitialized", "已有管理员或同名账号，拒绝重复初始化");
        var fab = await db.Set<Fab>().SingleOrDefaultAsync(x => x.Id == options.DeploymentFabId);
        if (fab is null)
            db.Add(new Fab
            {
                Id = options.DeploymentFabId,
                Code = OrganizationRules.Code(fabCode),
                Name = "MES 试点厂区",
                Timezone = timezone
            });
        var defs = new List<Permission>();
        foreach (var code in PermissionCatalog.All)
        {
            var permission = await db.Set<Permission>().SingleOrDefaultAsync(x => x.Code == code);
            if (permission is null)
            {
                permission = new Permission { Code = code, Name = code };
                db.Add(permission);
            }
            defs.Add(permission);
        }
        var roles = new List<MesRole>();
        foreach (var name in PermissionCatalog.Roles)
        {
            if (await db.Roles.AnyAsync(x => x.NormalizedName == name.ToUpperInvariant()))
                Fail(409, "CodeConflict", "存在同名角色，请先核对现有角色");
            var role = new MesRole
            {
                Id = Guid.CreateVersion7(),
                Name = name,
                NormalizedName = name.ToUpperInvariant(),
                IsSystem = name == "SystemAdmin",
                ConcurrencyStamp = Guid.NewGuid().ToString()
            };
            roles.Add(role);
            db.Add(role);
            foreach (var p in defs.Where(x => role.IsSystem || x.Code == "org.structure.read"))
                db.Add(new RolePermission { RoleId = role.Id, PermissionId = p.Id });
        }
        var u = new MesUser
        {
            Id = Guid.CreateVersion7(),
            UserName = username,
            Name = "系统管理员",
            ForcePasswordChange = true,
            LockoutEnabled = true
        };
        IdentityResult(await users.CreateAsync(u, password));
        db.Add(new IdentityUserRole<Guid> { UserId = u.Id, RoleId = roles.Single(x => x.IsSystem).Id });
        db.Add(new UserFabScope { UserId = u.Id, FabId = options.DeploymentFabId });
        actor = new(u.Id, Guid.Empty, Guid.NewGuid());
        sourceChannel = "Bootstrap";
        Audit("User", u.Id, "Bootstrapped", after: new { fabId = options.DeploymentFabId });
        await db.SaveChangesAsync();
        await tx.CommitAsync();
    }
}
