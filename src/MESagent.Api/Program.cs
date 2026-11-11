using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using MESagent.Application;
using MESagent.Infrastructure.OrganizationIdentityAccessControl;
using MESagent.Infrastructure.Identity;
using MESagent.Infrastructure.Persistence;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.ConfigureKestrel(o => o.Limits.MaxRequestBodySize = 65536);
var connection = builder.Configuration.GetConnectionString("Mes")
    ?? throw new InvalidOperationException("缺少 ConnectionStrings__Mes");
var options = builder.Configuration.GetSection("Mes").Get<OrganizationIdentityAccessControlOptions>() ?? new();
if (options.DeploymentFabId == Guid.Empty)
    throw new InvalidOperationException("缺少 Mes__DeploymentFabId");

builder.Services.AddSingleton(options);
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddDbContext<MesDbContext>(o => o.UseNpgsql(connection, n =>
{
    n.SetPostgresVersion(18, 0);
    n.MigrationsHistoryTable("__ef_migrations_history", "public");
}));
builder.Services
    .AddIdentityCore<MesUser>(o =>
    {
        o.Password.RequiredLength = 12;
        o.Password.RequireDigit = false;
        o.Password.RequireUppercase = false;
        o.Password.RequireLowercase = false;
        o.Password.RequireNonAlphanumeric = false;
        o.Lockout.MaxFailedAccessAttempts = 5;
        o.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        o.Lockout.AllowedForNewUsers = true;
    })
    .AddRoles<MesRole>()
    .AddEntityFrameworkStores<MesDbContext>()
    .AddDefaultTokenProviders();
builder.Services.AddScoped<IOrganizationIdentityAccessControlBackend, OrganizationIdentityAccessControlBackend>();
builder.Services.AddScoped<OrganizationIdentityAccessControlService>();

// Preserve the established key purpose so existing authentication cookies remain valid.
var dataProtection = builder.Services.AddDataProtection().SetApplicationName("MESagent.F01");
var keys = builder.Configuration["DataProtection:Path"];
if (!string.IsNullOrEmpty(keys))
{
    Directory.CreateDirectory(keys);
    dataProtection.PersistKeysToFileSystem(new DirectoryInfo(keys));
}
else if (!builder.Environment.IsDevelopment() && !builder.Environment.IsEnvironment("Testing"))
    throw new InvalidOperationException("生产环境必须配置 DataProtection__Path");

builder.Services
    .AddAuthentication("mes")
    .AddCookie("mes", o =>
    {
        o.Cookie.Name = "mes.session";
        o.Cookie.HttpOnly = true;
        o.Cookie.SameSite = SameSiteMode.Lax;
        o.Cookie.Path = "/";
        o.Cookie.SecurePolicy = builder.Environment.IsDevelopment() || builder.Environment.IsEnvironment("Testing")
            ? CookieSecurePolicy.SameAsRequest
            : CookieSecurePolicy.Always;
        o.SlidingExpiration = false;
        o.Events.OnRedirectToLogin = c =>
        {
            c.Response.StatusCode = 401;
            return Task.CompletedTask;
        };
        o.Events.OnRedirectToAccessDenied = c =>
        {
            c.Response.StatusCode = 403;
            return Task.CompletedTask;
        };
        o.Events.OnValidatePrincipal = async c =>
        {
            if (!Guid.TryParse(c.Principal?.FindFirstValue(ClaimTypes.NameIdentifier), out var user)
                || !Guid.TryParse(c.Principal?.FindFirstValue("sid"), out var session)
                || !await c.HttpContext.RequestServices.GetRequiredService<OrganizationIdentityAccessControlService>()
                    .ValidateSession(user, session))
                c.RejectPrincipal();
        };
    });
builder.Services.AddAuthorization();
builder.Services.AddAntiforgery(o =>
{
    o.HeaderName = "X-CSRF-TOKEN";
    o.Cookie.Name = "mes.csrf";
    o.Cookie.HttpOnly = true;
    o.Cookie.SameSite = SameSiteMode.Strict;
    o.Cookie.SecurePolicy = builder.Environment.IsDevelopment() || builder.Environment.IsEnvironment("Testing")
        ? CookieSecurePolicy.SameAsRequest
        : CookieSecurePolicy.Always;
});
builder.Services.ConfigureHttpJsonOptions(
    o => o.SerializerOptions.UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow);
builder.Services.AddOpenApi();
builder.Services.AddSingleton(PartitionedRateLimiter.Create<string, string>(key =>
    RateLimitPartition.GetFixedWindowLimiter(key, _ => new FixedWindowRateLimiterOptions
    {
        PermitLimit = builder.Configuration.GetValue<int?>("Security:LoginPermitLimit") ?? 10,
        Window = TimeSpan.FromMinutes(1),
        QueueLimit = 0
    })));
builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = 429;
    o.AddPolicy("login", ctx =>
        RateLimitPartition.GetFixedWindowLimiter(
            ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = builder.Configuration.GetValue<int?>("Security:LoginPermitLimit") ?? 20,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
    o.OnRejected = async (c, ct) =>
    {
        await c.HttpContext.RequestServices.GetRequiredService<IOrganizationIdentityAccessControlBackend>()
            .RecordSecurityEvent("LoginRateLimited", "Denied", (Guid)c.HttpContext.Items["correlation"]!);
        c.HttpContext.Response.Headers.RetryAfter = "60";
        await c.HttpContext.Response.WriteAsJsonAsync(
            new { status = 429, code = "RateLimited", title = "请求过于频繁，请稍后重试" }, ct);
    };
});

var app = builder.Build();
if (args.Contains("bootstrap") || args.Contains("recover-admin"))
{
    using var scope = app.Services.CreateScope();
    var name = Environment.GetEnvironmentVariable("MES_ADMIN_USER")
        ?? throw new InvalidOperationException("缺少 MES_ADMIN_USER");
    var pwd = Environment.GetEnvironmentVariable("MES_ADMIN_PASSWORD")
        ?? throw new InvalidOperationException("缺少 MES_ADMIN_PASSWORD");
    if (args.Contains("recover-admin"))
    {
        await scope.ServiceProvider.GetRequiredService<IOrganizationIdentityAccessControlBackend>().RecoverAdmin(
            name,
            pwd,
            Environment.GetEnvironmentVariable("MES_RECOVERY_REASON") ?? "Host administrator recovery");
        Console.WriteLine("管理员已恢复，全部旧会话已撤销，首次登录须改密。");
        return;
    }
    await scope.ServiceProvider.GetRequiredService<IOrganizationIdentityAccessControlBackend>()
        .Bootstrap(
            name,
            pwd,
            Environment.GetEnvironmentVariable("MES_FAB_CODE") ?? "FAB01",
            Environment.GetEnvironmentVariable("MES_FAB_TIMEZONE") ?? "Asia/Shanghai");
    Console.WriteLine("初始化完成。首次登录须修改密码。");
    return;
}

app.Use(async (ctx, next) =>
{
    ctx.Items["correlation"] = Guid.NewGuid();
    ctx.Response.Headers["X-Trace-Id"] = ctx.Items["correlation"]!.ToString();
    ctx.Response.Headers["X-Content-Type-Options"] = "nosniff";
    try
    {
        await next();
    }
    catch (Exception ex)
    {
        if (ctx.Response.HasStarted)
            throw;
        var (status, code, message) = ex switch
        {
            AppException a => (a.Status, a.Code, a.Message),
            AntiforgeryValidationException => (400, "CsrfInvalid", "防伪令牌无效，请刷新页面"),
            DbUpdateConcurrencyException => (412, "VersionConflict", "内容已发生变化，请刷新后重试"),
            DbUpdateException { InnerException: PostgresException { SqlState: "23505" } } =>
                (409, "CodeConflict", "编号或关联已存在"),
            DbUpdateException { InnerException: PostgresException { SqlState: "23503" or "23514" } } =>
                (409, "ValidationFailed", "关联或业务约束不满足"),
            ArgumentException or FormatException or JsonException =>
                (400, "ValidationFailed", "请求字段或格式无效"),
            BadHttpRequestException => (400, "ValidationFailed", "请求格式无效"),
            NpgsqlException => (503, "DependencyUnavailable", "数据库暂不可用"),
            _ => (500, "InternalError", "请求处理失败，请联系管理员并提供追踪 ID")
        };
        if (status == 500)
            app.Logger.LogError(ex, "OrganizationIdentityAccessControl request failed {TraceId}", ctx.Items["correlation"]);
        ctx.Response.StatusCode = status;
        await ctx.Response.WriteAsJsonAsync(
            new { type = "about:blank", title = message, status, code, traceId = ctx.Items["correlation"] });
    }
});

app.UseRouting();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.Use(async (ctx, next) =>
{
    if (ctx.Request.Path.StartsWithSegments("/api")
        && ctx.Request.Method is not ("GET" or "HEAD" or "OPTIONS"))
        await ctx.RequestServices.GetRequiredService<IAntiforgery>().ValidateRequestAsync(ctx);
    if (ctx.Request.Path.StartsWithSegments("/api"))
        ctx.Response.Headers.CacheControl = "no-store";
    await next();
});
app.MapGet("/health/live", () => Results.Ok(new { status = "live" }));
app.MapGet("/health/ready", async (MesDbContext db) => await db.Database.CanConnectAsync()
    ? Results.Ok(new { status = "ready" })
    : Results.StatusCode(503));
if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Testing"))
    app.MapOpenApi();

var api = app.MapGroup("/api/v1");
api.MapGet("/auth/csrf", (HttpContext ctx, IAntiforgery antiforgery) =>
    Results.Ok(new { requestToken = antiforgery.GetAndStoreTokens(ctx).RequestToken }));
api.MapPost("/auth/login", async (LoginRequest body, HttpContext ctx, OrganizationIdentityAccessControlService service) =>
{
    if (string.IsNullOrEmpty(body.Username) || body.Username.Length > 64)
        throw new AppException(401, "LoginFailed", "无法登录，请检查凭据或联系管理员");
    using var lease = await ctx.RequestServices.GetRequiredService<PartitionedRateLimiter<string>>()
        .AcquireAsync(body.Username.Trim().ToUpperInvariant());
    if (!lease.IsAcquired)
    {
        ctx.Response.Headers.RetryAfter = "60";
        await ctx.RequestServices.GetRequiredService<IOrganizationIdentityAccessControlBackend>()
            .RecordSecurityEvent("LoginRateLimited", "Denied", (Guid)ctx.Items["correlation"]!);
        throw new AppException(429, "RateLimited", "请求过于频繁，请稍后重试");
    }
    var ticket = await service.Login(body, (Guid)ctx.Items["correlation"]!);
    var principal = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, ticket.UserId.ToString()),
            new Claim("sid", ticket.SessionId.ToString())
        ], "mes"));
    await ctx.SignInAsync(
        "mes",
        principal,
        new AuthenticationProperties
        {
            IsPersistent = false,
            ExpiresUtc = new DateTimeOffset(ticket.ExpiresAt, TimeSpan.Zero)
        });
    return Results.Ok(new { ticket.PasswordChangeRequired });
}).RequireRateLimiting("login");

async Task<IResult> Dispatch(HttpContext ctx, OrganizationIdentityAccessControlService service)
{
    var path = ctx.Request.Path.Value!["/api/v1/".Length..];
    if (ctx.User.Identity?.IsAuthenticated != true)
    {
        if (path == "auth/logout")
        {
            await ctx.SignOutAsync("mes");
            return Results.NoContent();
        }
        throw new AppException(401, "SessionInvalid", "请先登录");
    }
    using var body = ctx.Request.HasJsonContentType()
        ? await JsonDocument.ParseAsync(ctx.Request.Body)
        : JsonDocument.Parse("{}");
    var actor = new RequestActor(
        Guid.Parse(ctx.User.FindFirstValue(ClaimTypes.NameIdentifier)!),
        Guid.Parse(ctx.User.FindFirstValue("sid")!),
        (Guid)ctx.Items["correlation"]!);
    var result = await service.Execute(
        actor,
        new(
            ctx.Request.Method,
            path,
            body.RootElement,
            ctx.Request.Query.ToDictionary(x => x.Key, x => x.Value.ToString()),
            ctx.Request.Headers.IfMatch.FirstOrDefault()));
    if (result.Version is not null)
        ctx.Response.Headers.ETag = result.Version;
    if (path is "auth/logout" or "account/change-password" or "account/sessions/revoke-all")
        await ctx.SignOutAsync("mes");
    if (result.Status == 201 && result.Data is not null)
    {
        var json = JsonSerializer.SerializeToElement(
            result.Data,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));
        if (json.TryGetProperty("id", out var id))
            ctx.Response.Headers.Location = ctx.Request.Path + "/" + id.GetString();
    }
    return result.Status == 204 ? Results.NoContent() : Results.Json(result.Data, statusCode: result.Status);
}

api.MapGet("/auth/me", Dispatch).Produces<CurrentUserView>();
foreach (var path in new[] { "account/sessions", "security/permissions", "security/audit-logs", "security/events" })
    api.MapGet("/" + path, Dispatch);
foreach (var path in new[] { "auth/logout", "account/sessions/revoke-all", "account/sessions/{id:guid}/revoke" })
    api.MapPost("/" + path, Dispatch);
api.MapPost("/account/change-password", Dispatch).Accepts<ChangePasswordRequest>("application/json");
api.MapPatch("/account/profile", Dispatch).Accepts<ProfileRequest>("application/json").Produces<CurrentUserView>();
foreach (var root in new[] { "organization/{resource}", "security/users", "security/roles" })
{
    var list = api.MapGet("/" + root, Dispatch);
    var create = api.MapPost("/" + root, Dispatch);
    var detail = api.MapGet("/" + root + "/{id:guid}", Dispatch);
    api.MapPatch("/" + root + "/{id:guid}", Dispatch);
    if (root.StartsWith("organization"))
    {
        list.Produces<PageView<OrgRow>>();
        detail.Produces<OrgRow>();
        create.Accepts<OrganizationCreateRequest>("application/json").Produces<OrgRow>(201);
    }
    else if (root.EndsWith("users"))
    {
        list.Produces<PageView<UserView>>();
        detail.Produces<UserView>();
        create.Accepts<CreateUserRequest>("application/json").Produces<UserView>(201);
    }
    else
    {
        list.Produces<PageView<RoleView>>();
        detail.Produces<RoleView>();
        create.Accepts<CreateRoleRequest>("application/json").Produces<RoleView>(201);
    }
    api.MapMethods("/" + root + "/{id:guid}/{action}", ["GET", "POST", "PUT"], Dispatch);
}
api.MapPost("/security/users/{id:guid}/sessions/revoke-all", Dispatch);

app.Run();

public partial class Program;
