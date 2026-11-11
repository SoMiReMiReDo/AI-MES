using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using MESagent.Application;
using MESagent.Database.Tests;
using MESagent.Domain.Entities;
using MESagent.Infrastructure.Identity;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace MESagent.Api.Tests;

public sealed class OrganizationIdentityAccessControlFixture : IAsyncLifetime
{
    public DatabaseFixture Database { get; } = new();
    public Guid Fab { get; } = Guid.NewGuid();
    public string Password { get; } = Guid.NewGuid() + "Aa!";
    public string FinalPassword { get; } = Guid.NewGuid() + "Bb!";
    public WebApplicationFactory<Program> Host { get; private set; } = null!;
    public HttpClient Admin { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await Database.InitializeAsync();

        Host = new WebApplicationFactory<Program>().WithWebHostBuilder(b =>
        {
            b.UseEnvironment("Testing");
            b.UseSetting("Security:LoginPermitLimit", "1000");
            b.UseSetting("ConnectionStrings:Mes", Database.ConnectionString);
            b.UseSetting("Mes:DeploymentFabId", Fab.ToString());
        });
        using (var scope = Host.Services.CreateScope())
            await scope.ServiceProvider.GetRequiredService<IOrganizationIdentityAccessControlBackend>().Bootstrap(
                "admin",
                Password,
                "TESTFAB",
                "Asia/Shanghai");

        Admin = Client();
        await Login(Admin, "admin", Password);
        var changed = await Send(
            Admin,
            "account/change-password",
            new { currentPassword = Password, newPassword = FinalPassword });
        Assert.True(changed.StatusCode == HttpStatusCode.NoContent, await changed.Content.ReadAsStringAsync());
        await Login(Admin, "admin", FinalPassword);
    }

    public HttpClient Client() => Host.CreateClient(new WebApplicationFactoryClientOptions
    {
        BaseAddress = new Uri("https://localhost"),
        AllowAutoRedirect = false,
        HandleCookies = true
    });

    public static async Task Csrf(HttpClient client)
    {
        var data = await client.GetFromJsonAsync<JsonElement>("/api/v1/auth/csrf");
        client.DefaultRequestHeaders.Remove("X-CSRF-TOKEN");
        client.DefaultRequestHeaders.Add("X-CSRF-TOKEN", data.GetProperty("requestToken").GetString());
    }

    public static async Task<HttpResponseMessage> Send(
        HttpClient client,
        string path,
        object? body = null,
        string method = "POST",
        string? etag = null)
    {
        var req = new HttpRequestMessage(new HttpMethod(method), "/api/v1/" + path)
        {
            Content = JsonContent.Create(body ?? new { })
        };
        if (etag is not null)
            req.Headers.TryAddWithoutValidation("If-Match", etag);
        return await client.SendAsync(req);
    }

    public static async Task Login(HttpClient client, string name, string pwd)
    {
        await Csrf(client);
        var response = await Send(client, "auth/login", new { username = name, password = pwd });
        Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());
        await Csrf(client);
    }

    public async Task<(HttpClient Client, JsonElement User, string Password)> NewUser(
        string role = "Viewer",
        bool scope = true)
    {
        var roles = await Admin.GetFromJsonAsync<JsonElement>("/api/v1/security/roles?pageSize=100");
        var id = roles.GetProperty("items")
            .EnumerateArray()
            .Single(x => x.GetProperty("name").GetString() == role)
            .GetProperty("id")
            .GetGuid();

        var name = "u" + Guid.NewGuid().ToString("N");
        var pwd = Guid.NewGuid() + "tmp!";
        var final = Guid.NewGuid() + "done!";
        var response = await Send(Admin, "security/users", new
        {
            username = name,
            displayName = "测试用户",
            temporaryPassword = pwd,
            roleIds = new[] { id },
            fabIds = scope ? new[] { Fab } : Array.Empty<Guid>()
        });
        Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());
        var user = await response.Content.ReadFromJsonAsync<JsonElement>();

        var client = Client();
        await Login(client, name, pwd);
        var changed = await Send(
            client,
            "account/change-password",
            new { currentPassword = pwd, newPassword = final });
        Assert.True(changed.StatusCode == HttpStatusCode.NoContent, await changed.Content.ReadAsStringAsync());
        await Login(client, name, final);
        user = await Admin.GetFromJsonAsync<JsonElement>("/api/v1/security/users/" + user.GetProperty("id").GetGuid());
        return (client, user, final);
    }

    public async Task DisposeAsync()
    {
        Admin.Dispose();
        await Host.DisposeAsync();
        await Database.DisposeAsync();
    }
}

public sealed class OrganizationIdentityAccessControlTests(OrganizationIdentityAccessControlFixture f) : IClassFixture<OrganizationIdentityAccessControlFixture>
{
    private static string E(JsonElement e) => e.GetProperty("etag").GetString()!;

    private async Task<JsonElement> Create(string resource, object body)
    {
        var r = await OrganizationIdentityAccessControlFixture.Send(f.Admin, "organization/" + resource, body);
        Assert.True(r.IsSuccessStatusCode, await r.Content.ReadAsStringAsync());
        return await r.Content.ReadFromJsonAsync<JsonElement>();
    }

    [Fact]
    public async Task Organization_scope_versions_parent_state_and_history()
    {
        var code = "A" + Guid.NewGuid().ToString("N");
        var a = await Create("areas", new { code, name = "区域", fabId = f.Fab });
        var id = a.GetProperty("id").GetGuid();
        Assert.Equal(
            HttpStatusCode.Conflict,
            (await OrganizationIdentityAccessControlFixture.Send(
                f.Admin,
                "organization/areas",
                new { code = code.ToLowerInvariant(), name = "重复", fabId = f.Fab })).StatusCode);
        var updated = await OrganizationIdentityAccessControlFixture.Send(
            f.Admin,
            $"organization/areas/{id}",
            new { name = "更名" },
            "PATCH",
            E(a));
        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
        Assert.Equal(
            HttpStatusCode.PreconditionFailed,
            (await OrganizationIdentityAccessControlFixture.Send(
                f.Admin,
                $"organization/areas/{id}",
                new { name = "旧版本" },
                "PATCH",
                E(a))).StatusCode);
        var current = await updated.Content.ReadFromJsonAsync<JsonElement>();
        var line = await Create("lines", new { code = "L1", name = "产线", areaId = id });
        Assert.Equal(
            HttpStatusCode.Conflict,
            (await OrganizationIdentityAccessControlFixture.Send(
                f.Admin,
                $"organization/areas/{id}/deactivate",
                new { reason = "测试" },
                etag: E(current))).StatusCode);
        Assert.Equal(
            HttpStatusCode.OK,
            (await OrganizationIdentityAccessControlFixture.Send(
                f.Admin,
                $"organization/lines/{line.GetProperty("id").GetGuid()}/deactivate",
                new { reason = "测试" },
                etag: E(line))).StatusCode);
        Assert.Equal(
            HttpStatusCode.OK,
            (await OrganizationIdentityAccessControlFixture.Send(
                f.Admin,
                $"organization/areas/{id}/deactivate",
                new { reason = "测试" },
                etag: E(current))).StatusCode);
        Assert.Equal(
            HttpStatusCode.OK,
            (await f.Admin.GetAsync($"/api/v1/organization/areas/{id}")).StatusCode);
        Assert.Equal(
            HttpStatusCode.Conflict,
            (await OrganizationIdentityAccessControlFixture.Send(
                f.Admin,
                "organization/lines",
                new { code = "L2", name = "新产线", areaId = id })).StatusCode);
    }

    [Fact]
    public async Task Cross_area_and_cross_fab_terminal_context_rejected()
    {
        var a = await Create("areas", new { code = "A" + Guid.NewGuid().ToString("N"), name = "区域A", fabId = f.Fab });
        var b = await Create("areas", new { code = "B" + Guid.NewGuid().ToString("N"), name = "区域B", fabId = f.Fab });
        var line = await Create(
            "lines",
            new { code = "LINE", name = "产线", areaId = a.GetProperty("id").GetGuid() });
        Assert.Equal(
            HttpStatusCode.BadRequest,
            (await OrganizationIdentityAccessControlFixture.Send(
                f.Admin,
                "organization/workstations",
                new
                {
                    code = "W",
                    name = "工位",
                    areaId = b.GetProperty("id").GetGuid(),
                    lineId = line.GetProperty("id").GetGuid()
                })).StatusCode);
        var term = await Create(
            "terminals",
            new
            {
                code = "T" + Guid.NewGuid().ToString("N"),
                name = "未绑定",
                fabId = f.Fab,
                deviceUuid = Guid.NewGuid()
            });
        Assert.Equal(f.Fab, term.GetProperty("fabId").GetGuid());
        await using var db = f.Database.CreateContext();
        var other = new Fab
        {
            Code = "X" + Guid.NewGuid().ToString("N"),
            Name = "其它厂",
            Timezone = "Asia/Shanghai"
        };
        db.Add(other);
        await db.SaveChangesAsync();
        Assert.Equal(
            HttpStatusCode.NotFound,
            (await f.Admin.GetAsync("/api/v1/organization/fabs/" + other.Id)).StatusCode);
        Assert.Equal(
            HttpStatusCode.NotFound,
            (await OrganizationIdentityAccessControlFixture.Send(
                f.Admin,
                "organization/areas",
                new { code = "X", name = "越权", fabId = other.Id })).StatusCode);
        var list = await f.Admin.GetFromJsonAsync<JsonElement>(
            "/api/v1/organization/fabs?fabId=" + other.Id);
        Assert.Equal(0, list.GetProperty("total").GetInt32());
    }

    [Fact]
    public async Task Viewer_direct_api_and_scope_denial_ignore_direct_grants()
    {
        var (client, user, _) = await f.NewUser();
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/v1/organization/areas")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/v1/security/users")).StatusCode);
        Assert.Equal(
            HttpStatusCode.Forbidden,
            (await OrganizationIdentityAccessControlFixture.Send(
                client,
                "organization/areas",
                new { code = "NO", name = "越权", fabId = f.Fab })).StatusCode);
        await using (var db = f.Database.CreateContext())
        {
            db.Add(new UserPermission
            {
                UserId = user.GetProperty("id").GetGuid(),
                PermissionId = await db.Set<Permission>()
                    .Where(x => x.Code == "security.users.read")
                    .Select(x => x.Id)
                    .SingleAsync()
            });
            await db.SaveChangesAsync();
        }
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/v1/security/users")).StatusCode);
        var (noScope, _, _) = await f.NewUser(scope: false);
        Assert.Equal(HttpStatusCode.Forbidden, (await noScope.GetAsync("/api/v1/organization/areas")).StatusCode);
        client.Dispose();
        noScope.Dispose();
    }

    [Fact]
    public async Task First_login_restricted_and_password_invalidates_all_sessions()
    {
        var name = "first" + Guid.NewGuid().ToString("N");
        var pwd = Guid.NewGuid().ToString();
        var r = await OrganizationIdentityAccessControlFixture.Send(
            f.Admin,
            "security/users",
            new
            {
                username = name,
                displayName = "首登",
                temporaryPassword = pwd,
                roleIds = Array.Empty<Guid>(),
                fabIds = new[] { f.Fab }
            });
        Assert.Equal(HttpStatusCode.Created, r.StatusCode);
        using var client = f.Client();
        await OrganizationIdentityAccessControlFixture.Login(client, name, pwd);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/v1/account/sessions")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/v1/organization/areas")).StatusCode);
        var next = Guid.NewGuid().ToString();
        Assert.Equal(
            HttpStatusCode.NoContent,
            (await OrganizationIdentityAccessControlFixture.Send(
                client,
                "account/change-password",
                new { currentPassword = pwd, newPassword = next })).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/v1/auth/me")).StatusCode);
        await OrganizationIdentityAccessControlFixture.Login(client, name, next);
        await OrganizationIdentityAccessControlFixture.Csrf(client);
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            (await OrganizationIdentityAccessControlFixture.Send(client, "auth/login", new { username = name, password = pwd })).StatusCode);
    }

    [Fact]
    public async Task Reset_deactivate_and_access_changes_revoke_sessions()
    {
        var (client, user, pwd) = await f.NewUser();
        var id = user.GetProperty("id").GetGuid();
        Assert.Equal(
            HttpStatusCode.OK,
            (await OrganizationIdentityAccessControlFixture.Send(
                f.Admin,
                $"security/users/{id}/access",
                new { roleIds = Array.Empty<Guid>(), fabIds = new[] { f.Fab }, reason = "撤权" },
                "PUT",
                E(user))).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/v1/auth/me")).StatusCode);
        await OrganizationIdentityAccessControlFixture.Login(client, user.GetProperty("username").GetString()!, pwd);
        user = await f.Admin.GetFromJsonAsync<JsonElement>($"/api/v1/security/users/{id}");
        var temp = Guid.NewGuid().ToString();
        Assert.Equal(
            HttpStatusCode.NoContent,
            (await OrganizationIdentityAccessControlFixture.Send(
                f.Admin,
                $"security/users/{id}/reset-password",
                new { temporaryPassword = temp, reason = "重置" },
                etag: E(user))).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/v1/auth/me")).StatusCode);
        await OrganizationIdentityAccessControlFixture.Login(client, user.GetProperty("username").GetString()!, temp);
        var me = await client.GetFromJsonAsync<JsonElement>("/api/v1/auth/me");
        Assert.True(me.GetProperty("passwordChangeRequired").GetBoolean());
        client.Dispose();
    }

    [Fact]
    public async Task Logout_revokes_copied_cookie_but_not_another_session()
    {
        var (client, user, pwd) = await f.NewUser();
        using var other = f.Client();
        await OrganizationIdentityAccessControlFixture.Login(other, user.GetProperty("username").GetString()!, pwd);
        var sessions = await client.GetFromJsonAsync<JsonElement>("/api/v1/account/sessions");
        var sid = sessions.EnumerateArray()
            .Single(x => x.GetProperty("current").GetBoolean())
            .GetProperty("id")
            .GetGuid();
        Assert.Equal(
            HttpStatusCode.NoContent,
            (await OrganizationIdentityAccessControlFixture.Send(client, "account/sessions/" + sid + "/revoke")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/v1/auth/me")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await other.GetAsync("/api/v1/auth/me")).StatusCode);
        client.Dispose();
    }

    [Fact]
    public async Task Csrf_lockout_and_manual_lock_have_distinct_behavior()
    {
        using var anonymous = f.Client();
        Assert.Equal(
            HttpStatusCode.BadRequest,
            (await OrganizationIdentityAccessControlFixture.Send(
                anonymous,
                "auth/login",
                new { username = "admin", password = f.FinalPassword })).StatusCode);
        var (client, user, pwd) = await f.NewUser();
        using var attacker = f.Client();
        await OrganizationIdentityAccessControlFixture.Csrf(attacker);
        var name = user.GetProperty("username").GetString();
        for (int i = 0; i < 5; i++)
            Assert.Equal(
                HttpStatusCode.Unauthorized,
                (await OrganizationIdentityAccessControlFixture.Send(
                    attacker,
                    "auth/login",
                    new { username = name, password = "wrong-password-long" })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/v1/auth/me")).StatusCode);
        user = await f.Admin.GetFromJsonAsync<JsonElement>(
            "/api/v1/security/users/" + user.GetProperty("id").GetGuid());
        Assert.Equal(
            HttpStatusCode.OK,
            (await OrganizationIdentityAccessControlFixture.Send(
                f.Admin,
                "security/users/" + user.GetProperty("id").GetGuid() + "/lock",
                new { until = DateTimeOffset.UtcNow.AddMinutes(15), reason = "手动锁定" },
                etag: E(user))).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/v1/auth/me")).StatusCode);
        client.Dispose();
    }

    [Fact]
    public async Task Protected_admin_and_role_escalation_are_denied()
    {
        var me = await f.Admin.GetFromJsonAsync<JsonElement>("/api/v1/auth/me");
        var id = me.GetProperty("userId").GetGuid();
        var user = await f.Admin.GetFromJsonAsync<JsonElement>("/api/v1/security/users/" + id);
        Assert.Equal(
            HttpStatusCode.Conflict,
            (await OrganizationIdentityAccessControlFixture.Send(
                f.Admin,
                $"security/users/{id}/deactivate",
                new { reason = "不能停用自己" },
                etag: E(user))).StatusCode);
        Assert.Equal(
            HttpStatusCode.BadRequest,
            (await OrganizationIdentityAccessControlFixture.Send(
                f.Admin,
                "security/roles",
                new
                {
                    name = "Custom" + Guid.NewGuid(),
                    permissionCodes = new[] { "security.users.manage" }
                })).StatusCode);
        Assert.Equal(
            HttpStatusCode.BadRequest,
            (await OrganizationIdentityAccessControlFixture.Send(
                f.Admin,
                "security/roles",
                new
                {
                    name = "Custom" + Guid.NewGuid(),
                    permissionCodes = new[] { "*" }
                })).StatusCode);
        using var scope = f.Host.Services.CreateScope();
        await Assert.ThrowsAsync<AppException>(
            () => scope.ServiceProvider.GetRequiredService<IOrganizationIdentityAccessControlBackend>().Bootstrap(
                "another",
                Guid.NewGuid().ToString(),
                "X",
                "Asia/Shanghai"));
    }

    [Fact]
    public async Task Session_expiration_and_audit_redaction()
    {
        var (client, user, pwd) = await f.NewUser();
        await using (var db = f.Database.CreateContext())
        {
            var id = user.GetProperty("id").GetGuid();
            var session = await db.Set<UserSession>().SingleAsync(x => x.UserId == id && x.RevokedAt == null);
            session.IdleExpiresAt = DateTime.UtcNow.AddSeconds(-1);
            await db.SaveChangesAsync();
        }

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/v1/auth/me")).StatusCode);
        var text = await f.Admin.GetStringAsync("/api/v1/security/audit-logs?pageSize=100");
        Assert.DoesNotContain(pwd, text);
        Assert.DoesNotContain("passwordHash", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("securityStamp", text, StringComparison.OrdinalIgnoreCase);
        client.Dispose();
    }

    [Fact]
    public async Task Concurrent_child_create_and_parent_deactivation_preserve_invariant()
    {
        var a = await Create(
            "areas",
            new { code = "C" + Guid.NewGuid().ToString("N"), name = "并发", fabId = f.Fab });
        var id = a.GetProperty("id").GetGuid();
        var results = await Task.WhenAll(
            OrganizationIdentityAccessControlFixture.Send(
                f.Admin,
                $"organization/areas/{id}/deactivate",
                new { reason = "竞争" },
                etag: E(a)),
            OrganizationIdentityAccessControlFixture.Send(
                f.Admin,
                "organization/lines",
                new { code = "L", name = "产线", areaId = id }));
        Assert.Single(results, x => x.IsSuccessStatusCode);
        Assert.Single(results, x => x.StatusCode == HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Worker_binding_is_unique_and_deactivation_revokes_login()
    {
        var worker = await Create(
            "workers",
            new { code = "W" + Guid.NewGuid().ToString("N"), name = "责任员工", fabId = f.Fab });
        var wid = worker.GetProperty("id").GetGuid();
        var (client, user, _) = await f.NewUser();
        var id = user.GetProperty("id").GetGuid();
        var bound = await OrganizationIdentityAccessControlFixture.Send(
            f.Admin,
            $"security/users/{id}",
            new { workerId = wid },
            "PATCH",
            E(user));
        Assert.Equal(HttpStatusCode.OK, bound.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/v1/auth/me")).StatusCode);
        var (second, user2, _) = await f.NewUser();
        Assert.Equal(
            HttpStatusCode.Conflict,
            (await OrganizationIdentityAccessControlFixture.Send(
                f.Admin,
                $"security/users/{user2.GetProperty("id").GetGuid()}",
                new { workerId = wid },
                "PATCH",
                E(user2))).StatusCode);
        Assert.Equal(
            HttpStatusCode.OK,
            (await OrganizationIdentityAccessControlFixture.Send(
                f.Admin,
                $"organization/workers/{wid}/deactivate",
                new { reason = "离岗" },
                etag: E(worker))).StatusCode);
        var disabled = await f.Admin.GetFromJsonAsync<JsonElement>($"/api/v1/security/users/{id}");
        Assert.False(disabled.GetProperty("isActive").GetBoolean());
        client.Dispose();
        second.Dispose();
    }

    [Fact]
    public async Task Terminal_binding_change_revokes_its_session_and_database_rejects_cross_area()
    {
        var a = await Create(
            "areas",
            new { code = "T" + Guid.NewGuid().ToString("N"), name = "终端区域", fabId = f.Fab });
        var aid = a.GetProperty("id").GetGuid();
        var station = await Create(
            "workstations",
            new { code = "S", name = "工位", areaId = aid });
        var sid = station.GetProperty("id").GetGuid();
        var t = await Create(
            "terminals",
            new
            {
                code = "T" + Guid.NewGuid().ToString("N"),
                name = "终端",
                fabId = f.Fab,
                deviceUuid = Guid.NewGuid(),
                workstationId = sid
            });
        var tid = t.GetProperty("id").GetGuid();

        var (client, user, pwd) = await f.NewUser();
        using var terminalClient = f.Client();
        await OrganizationIdentityAccessControlFixture.Csrf(terminalClient);
        Assert.Equal(
            HttpStatusCode.OK,
            (await OrganizationIdentityAccessControlFixture.Send(
                terminalClient,
                "auth/login",
                new
                {
                    username = user.GetProperty("username").GetString(),
                    password = pwd,
                    terminalId = tid
                })).StatusCode);
        t = await f.Admin.GetFromJsonAsync<JsonElement>($"/api/v1/organization/terminals/{tid}");
        Assert.Equal(
            HttpStatusCode.OK,
            (await OrganizationIdentityAccessControlFixture.Send(
                f.Admin,
                $"organization/terminals/{tid}/bind-workstation",
                new { workstationId = (Guid?)null, reason = "解除绑定" },
                etag: E(t))).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await terminalClient.GetAsync("/api/v1/auth/me")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/v1/auth/me")).StatusCode);

        await using var db = f.Database.CreateContext();
        var area2 = new Area
        {
            FabId = f.Fab,
            Code = "Z" + Guid.NewGuid().ToString("N"),
            Name = "其它区域"
        };
        var line = new Line { AreaId = area2.Id, Code = "L", Name = "产线" };
        db.AddRange(area2, line);
        await db.SaveChangesAsync();
        db.Add(new Workstation { AreaId = aid, LineId = line.Id, Code = "INVALID", Name = "非法跨区域" });
        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        client.Dispose();
    }

    [Fact]
    public async Task Role_permission_audit_failure_rolls_back_permissions_and_revocations()
    {
        var roleName = "R" + Guid.NewGuid().ToString("N");
        var created = await OrganizationIdentityAccessControlFixture.Send(
            f.Admin,
            "security/roles",
            new { name = roleName, permissionCodes = new[] { "org.structure.read" } });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var role = await created.Content.ReadFromJsonAsync<JsonElement>();
        var (client, _, _) = await f.NewUser(roleName);
        await using var db = f.Database.CreateContext();
        await db.Database.ExecuteSqlRawAsync(
            "CREATE FUNCTION audit.fail_access_control_test() RETURNS trigger LANGUAGE plpgsql AS $$ BEGIN IF NEW.action = 'PermissionsChanged' THEN RAISE EXCEPTION 'test audit failure' USING ERRCODE = '23514'; END IF; RETURN NEW; END $$; CREATE TRIGGER test_access_control_failure BEFORE INSERT ON audit.audit_logs FOR EACH ROW EXECUTE FUNCTION audit.fail_access_control_test();");

        try
        {
            var response = await OrganizationIdentityAccessControlFixture.Send(
                f.Admin,
                $"security/roles/{role.GetProperty("id").GetGuid()}/permissions",
                new { permissionCodes = new[] { "org.workers.read" }, reason = "故障回滚测试" },
                "PUT",
                E(role));
            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/v1/organization/areas")).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/v1/organization/workers")).StatusCode);
        }
        finally
        {
            await db.Database.ExecuteSqlRawAsync(
                "DROP TRIGGER test_access_control_failure ON audit.audit_logs; DROP FUNCTION audit.fail_access_control_test();");
            client.Dispose();
        }
    }

    [Fact]
    public async Task Api_contract_rejects_immutable_fields_missing_versions_and_unknown_resources()
    {
        var a = await Create(
            "areas",
            new { code = "I" + Guid.NewGuid().ToString("N"), name = "不可变字段", fabId = f.Fab });
        var id = a.GetProperty("id").GetGuid();
        Assert.Equal(
            (HttpStatusCode)428,
            (await OrganizationIdentityAccessControlFixture.Send(
                f.Admin,
                $"organization/areas/{id}",
                new { name = "没有版本" },
                "PATCH")).StatusCode);
        Assert.Equal(
            HttpStatusCode.BadRequest,
            (await OrganizationIdentityAccessControlFixture.Send(
                f.Admin,
                $"organization/areas/{id}",
                new { fabId = Guid.NewGuid() },
                "PATCH",
                E(a))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await f.Admin.GetAsync("/api/v1/organization/passwords")).StatusCode);
        Assert.Equal(
            HttpStatusCode.BadRequest,
            (await f.Admin.GetAsync("/api/v1/organization/areas?pageSize=9999")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await f.Admin.GetAsync("/openapi/v1.json")).StatusCode);
    }

    [Fact]
    public async Task Login_rate_limit_returns_429_and_security_events_are_append_only()
    {
        await using var host = f.Host.WithWebHostBuilder(b => b.UseSetting("Security:LoginPermitLimit", "1"));
        using var client = host.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });
        await OrganizationIdentityAccessControlFixture.Csrf(client);

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            (await OrganizationIdentityAccessControlFixture.Send(
                client,
                "auth/login",
                new { username = "unknown_user", password = "invalid-password" })).StatusCode);
        var response = await OrganizationIdentityAccessControlFixture.Send(
            client,
            "auth/login",
            new { username = "unknown_user", password = "invalid-password" });
        Assert.Equal((HttpStatusCode)429, response.StatusCode);
        Assert.NotNull(response.Headers.RetryAfter);

        await using var db = f.Database.CreateContext();
        var item = await db.Set<SecurityEvent>().FirstAsync();
        item.Outcome = "Tampered";
        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task Local_admin_recovery_revokes_sessions_and_requires_password_change()
    {
        var (client, user, _) = await f.NewUser("SystemAdmin");
        var password = Guid.NewGuid().ToString();
        using var scope = f.Host.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<IOrganizationIdentityAccessControlBackend>().RecoverAdmin(
            user.GetProperty("username").GetString()!,
            password,
            "Recovery test");
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/v1/auth/me")).StatusCode);
        await OrganizationIdentityAccessControlFixture.Login(client, user.GetProperty("username").GetString()!, password);
        var me = await client.GetFromJsonAsync<JsonElement>("/api/v1/auth/me");
        Assert.True(me.GetProperty("passwordChangeRequired").GetBoolean());
        client.Dispose();
    }

    [Fact]
    public async Task Audit_query_filters_foreign_fab_before_count()
    {
        await using var db = f.Database.CreateContext();
        var fab = new Fab
        {
            Code = "AUDIT" + Guid.NewGuid().ToString("N"),
            Name = "Foreign",
            Timezone = "Asia/Shanghai"
        };
        var area = new Area { Code = "AREA", Name = "Hidden", FabId = fab.Id };
        db.AddRange(fab, area);
        await db.SaveChangesAsync();
        db.Add(new AuditLog
        {
            EntityType = "Area",
            EntityId = area.Id,
            Action = "ForeignOnly",
            OccurredAt = DateTime.UtcNow,
            CorrelationId = Guid.NewGuid(),
            SourceChannel = "Test"
        });
        await db.SaveChangesAsync();
        var result = await f.Admin.GetFromJsonAsync<JsonElement>("/api/v1/security/audit-logs?action=ForeignOnly");
        Assert.Equal(0, result.GetProperty("total").GetInt32());
        Assert.Empty(result.GetProperty("items").EnumerateArray());
    }
}
