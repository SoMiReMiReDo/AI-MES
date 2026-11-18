using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Text.Json;
using MESagent.Application;
using MESagent.Domain.Entities;
using MESagent.Infrastructure.Persistence;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace MESagent.Api;

internal sealed class ReliabilityHttp(RequestDelegate next, ILogger<ReliabilityHttp> logger)
{
    private static readonly Meter Meter = new("MESagent.Api");
    private static readonly Histogram<double> Duration =
        Meter.CreateHistogram<double>("mes.api.request.duration", "ms");

    public async Task InvokeAsync(HttpContext ctx)
    {
        var trace = Guid.NewGuid();
        var started = Stopwatch.GetTimestamp();
        ctx.Items["correlation"] = trace;
        ctx.Response.Headers["X-Trace-Id"] = trace.ToString();
        ctx.Response.Headers["X-Content-Type-Options"] = "nosniff";
        using var scope = logger.BeginScope(new Dictionary<string, object> { ["TraceId"] = trace });
        try
        {
            await next(ctx);
        }
        catch (Exception ex) when (!ctx.Response.HasStarted && !ctx.RequestAborted.IsCancellationRequested)
        {
            var (status, code, title) = ex switch
            {
                AppException a => (a.Status, a.Code, a.Message),
                AntiforgeryValidationException => (400, "CsrfInvalid", "防伪令牌无效，请刷新页面"),
                DbUpdateConcurrencyException => (412, "VersionConflict", "内容已发生变化，请刷新后重试"),
                DbUpdateException { InnerException: PostgresException { SqlState: "23505" or "23P01" } } =>
                    (409, "CodeConflict", "编号或关联已存在"),
                DbUpdateException { InnerException: PostgresException { SqlState: "23503" or "23514" } } =>
                    (409, "ValidationFailed", "关联或业务约束不满足"),
                ArgumentException or FormatException or JsonException =>
                    (400, "ValidationFailed", "请求字段或格式无效"),
                BadHttpRequestException { StatusCode: 413 } => (413, "RequestTooLarge", "请求超过允许大小"),
                BadHttpRequestException => (400, "ValidationFailed", "请求格式无效"),
                _ when DatabaseUnavailable(ex) =>
                    (503, "DependencyUnavailable", "数据库暂不可用"),
                _ => (500, "InternalError", "请求处理失败，请联系管理员并提供追踪 ID")
            };
            if (status >= 500)
                logger.LogError("Request failed with {Code} ({ErrorType}), trace {TraceId}",
                    code, ex.GetType().Name, trace);
            await Problem(ctx, status, code, title);
        }
        finally
        {
            var route = (ctx.GetEndpoint() as RouteEndpoint)?.RoutePattern.RawText ?? "(unmatched)";
            var method = ctx.Request.Method;
            var status = ctx.RequestAborted.IsCancellationRequested ? 499 : ctx.Response.StatusCode;
            var ms = Stopwatch.GetElapsedTime(started).TotalMilliseconds;
            Duration.Record(ms, new KeyValuePair<string, object?>("http.request.method", method),
                new KeyValuePair<string, object?>("http.route", route),
                new KeyValuePair<string, object?>("http.response.status_code", status));
            logger.LogInformation("HTTP {Method} {Route} {StatusCode} in {ElapsedMs:F1} ms, trace {TraceId}",
                method, route, status, ms, trace);
        }
    }

    public static Task Problem(HttpContext ctx, int status, string code, string title)
    {
        ctx.Response.StatusCode = status;
        return ctx.Response.WriteAsJsonAsync(
            new MesProblem("about:blank", title, status, code, (Guid)ctx.Items["correlation"]!, new()),
            options: null, contentType: "application/problem+json", cancellationToken: ctx.RequestAborted);
    }

    public static async Task<IResult> Readiness(MesDbContext db, CancellationToken cancellationToken)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(2));
        try
        {
            await db.Set<OutboxMessage>().AsNoTracking().Select(x => x.DeadLetteredAt).Take(1)
                .ToArrayAsync(timeout.Token);
            return Results.Ok(new { status = "ready" });
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested
            && (ex is OperationCanceledException || DatabaseUnavailable(ex)))
        {
            return Results.Json(new { status = "unavailable" }, statusCode: 503);
        }
    }

    private static bool DatabaseUnavailable(Exception ex) => ex is NpgsqlException or TimeoutException
        or InvalidOperationException { InnerException: NpgsqlException or TimeoutException }
        or DbUpdateException { InnerException: NpgsqlException };
}

public sealed record MesProblem(
    string Type, string Title, int Status, string Code, Guid TraceId, Dictionary<string, string[]> Errors);
