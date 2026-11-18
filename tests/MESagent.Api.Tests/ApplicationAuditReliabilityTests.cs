using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using MESagent.Application;
using MESagent.Domain.Entities;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Xunit;
using static MESagent.Api.Tests.OrganizationIdentityAccessControlFixture;

namespace MESagent.Api.Tests;

public sealed class ApplicationAuditReliabilityTests(OrganizationIdentityAccessControlFixture f)
    : IClassFixture<OrganizationIdentityAccessControlFixture>
{
    [Fact]
    public async Task Outbox_write_is_atomic_and_manufacturing_replay_enqueues_only_once()
    {
        var token = Guid.NewGuid().ToString("N");
        using var created = await Send(f.Admin, "engineering/number-sequences", new
        {
            name = token, purpose = "Lot", prefix = token, padSize = 4, nextNumber = 1
        });
        var sequence = await Json(created);
        var id = sequence.GetProperty("id").GetGuid();
        var version = sequence.GetProperty("etag").GetString()!;
        var path = $"engineering/number-sequences/{id}/allocate";
        var key = Guid.NewGuid().ToString();

        await using var db = f.Database.CreateContext();
        await db.Database.ExecuteSqlRawAsync(
            "ALTER TABLE audit.outbox_messages ADD CONSTRAINT test_reject_enqueue CHECK (false) NOT VALID");
        try
        {
            using var failed = await Allocate(path, version, key);
            Assert.Equal(HttpStatusCode.Conflict, failed.StatusCode);
            Assert.Equal("application/problem+json", failed.Content.Headers.ContentType?.MediaType);
            var failedTrace = Trace(failed);
            var problem = await failed.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(failedTrace, problem.GetProperty("traceId").GetGuid());
            Assert.False(await db.Set<AuditLog>().AnyAsync(x => x.CorrelationId == failedTrace));
            Assert.False(await db.Set<BusinessEvent>().AnyAsync(x => x.CorrelationId == failedTrace));
            Assert.False(await db.Set<CommandReceipt>().AnyAsync(x => x.IdempotencyKey == key));
            Assert.Equal(1L, await db.Set<NumberSequence>().Where(x => x.Id == id).Select(x => x.NextNumber).SingleAsync());
        }
        finally
        {
            await db.Database.ExecuteSqlRawAsync("ALTER TABLE audit.outbox_messages DROP CONSTRAINT test_reject_enqueue");
        }

        using var success = await Allocate(path, version, key);
        var data = await Json(success);
        var trace = Trace(success);
        using var duplicate = await Allocate(path, version, key, "{ }");
        Assert.True(JsonElement.DeepEquals(data, await Json(duplicate)));
        Assert.Equal("true", duplicate.Headers.GetValues("Idempotency-Replayed").Single());
        using var different = await Allocate(path, data.GetProperty("etag").GetString()!, key);
        Assert.Equal(HttpStatusCode.Conflict, different.StatusCode);
        Assert.False(await db.Set<OutboxMessage>().AnyAsync(x => x.CorrelationId == Trace(duplicate)));

        var evt = await db.Set<BusinessEvent>().SingleAsync(x => x.CorrelationId == trace);
        var queued = await db.Set<OutboxMessage>().SingleAsync(x => x.CorrelationId == trace);
        Assert.Equal(evt.Id, queued.BusinessEventId);
        Assert.Equal(WorkbenchRefreshHint.EventType, evt.EventType);
        Assert.Equal(f.Fab, queued.AggregateId);
        Assert.Null(queued.PublishedAt);
        Assert.True(await db.Set<CommandReceipt>().AnyAsync(x => x.CorrelationId == trace && x.IdempotencyKey == key));

        using var audit = await f.Admin.GetAsync($"/api/v1/reliability/audit-logs?correlationId={trace}&entityId={id}");
        var row = Assert.Single((await Json(audit)).GetProperty("items").EnumerateArray());
        Assert.Equal("Allocated", row.GetProperty("action").GetString());
        Assert.Equal("engineering", row.GetProperty("module").GetString());
    }

    [Fact]
    public async Task Audit_filters_legacy_and_current_rows_by_fab_origin_role_and_actor()
    {
        var readerRole = "AuditReader" + Guid.NewGuid().ToString("N")[..8];
        using var created = await Send(f.Admin, "security/roles", new
        {
            name = readerRole, permissionCodes = new[] { ReliabilityAccess.Audit, "engineering.read" }
        });
        var role = await Json(created);
        using var reader = (await f.NewUser(readerRole)).Client;
        using var noScope = (await f.NewUser(readerRole, scope: false)).Client;
        using var noPermission = (await f.NewUser()).Client;
        var trace = Guid.NewGuid();
        var id = Guid.NewGuid();
        var me = await f.Admin.GetFromJsonAsync<JsonElement>("/api/v1/auth/me");
        var actor = me.GetProperty("userId").GetGuid();
        await using var db = f.Database.CreateContext();
        var foreign = new Fab { Code = "F12OTHER", Name = "外厂", Timezone = "Asia/Shanghai" };
        db.Add(foreign);
        db.AddRange(
            Log(f.Fab, "engineering", "Product", "{\"name\":\"可查\",\"nested\":{\"api_key\":\"hidden\"}}"),
            Log(null, null, "Product", "{\"name\":\"旧工艺\",\"storagePath\":\"hidden\"}"),
            Log(f.Fab, "quality", "Issue", "{\"name\":\"没有质量权限\"}"),
            Log(foreign.Id, "engineering", "Product", "{\"name\":\"外厂\"}"),
            Log(null, null, "WorkOrder", JsonSerializer.Serialize(new { fabId = foreign.Id })));
        await db.SaveChangesAsync();

        var query = $"/api/v1/reliability/audit-logs?correlationId={trace}&entityId={id}&actorId={actor}"
            + "&sourceChannel=Web&action=F12Test&entityType=Product&pageSize=1";
        using var allowed = await reader.GetAsync(query);
        var payload = await Json(allowed);
        Assert.Equal(2, payload.GetProperty("total").GetInt32());
        var row = Assert.Single(payload.GetProperty("items").EnumerateArray());
        Assert.Equal("可查", row.GetProperty("afterState").GetProperty("name").GetString());
        Assert.Empty(row.GetProperty("afterState").GetProperty("nested").EnumerateObject());
        using var legacy = await reader.GetAsync(query + "&page=2");
        var older = Assert.Single((await Json(legacy)).GetProperty("items").EnumerateArray());
        Assert.False(older.GetProperty("afterState").TryGetProperty("storagePath", out _));
        Assert.Equal("旧工艺", older.GetProperty("afterState").GetProperty("name").GetString());
        using var broad = await reader.GetAsync($"/api/v1/reliability/audit-logs?correlationId={trace}");
        Assert.Equal(2, (await Json(broad)).GetProperty("total").GetInt32());
        Assert.Equal(HttpStatusCode.BadRequest,
            (await reader.GetAsync("/api/v1/reliability/audit-logs?actorId=invalid")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await noPermission.GetAsync(query)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await noScope.GetAsync(query)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await reader.GetAsync("/api/v1/reliability/outbox")).StatusCode);

        using var changed = await Send(f.Admin, $"security/roles/{role.GetProperty("id").GetGuid()}/permissions",
            new { permissionCodes = new[] { "engineering.read" }, reason = "验证审计授权撤销" },
            "PUT", role.GetProperty("etag").GetString());
        Assert.True(changed.IsSuccessStatusCode, await changed.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.Unauthorized, (await reader.GetAsync(query)).StatusCode);

        AuditLog Log(Guid? fab, string? module, string type, string after) => new()
        {
            FabId = fab, Module = module, UserId = actor, EntityType = type, EntityId = id,
            OccurredAt = DateTime.UtcNow.AddMinutes(module == null ? -2 : -1),
            AfterState = after, CorrelationId = trace, SourceChannel = "Web", Action = "F12Test"
        };
    }

    [Fact]
    public async Task Unavailable_database_keeps_process_live_and_returns_sanitized_readiness()
    {
        using var host = f.Host.WithWebHostBuilder(b => b.UseSetting("ConnectionStrings:Mes",
            "Host=127.0.0.1;Port=1;Database=unavailable;Username=unavailable;Timeout=1"));
        using var client = host.CreateClient();
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health/live")).StatusCode);
        using var readiness = await client.GetAsync("/health/ready");
        Assert.Equal(HttpStatusCode.ServiceUnavailable, readiness.StatusCode);
        var data = await readiness.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Single(data.EnumerateObject());
        Assert.Equal("unavailable", data.GetProperty("status").GetString());
    }

    private Task<HttpResponseMessage> Allocate(string path, string version, string key, string body = "{}")
    {
        var req = new HttpRequestMessage(HttpMethod.Post, "/api/v1/" + path)
        {
            Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json")
        };
        req.Headers.TryAddWithoutValidation("If-Match", version);
        req.Headers.Add("Idempotency-Key", key);
        return f.Admin.SendAsync(req);
    }

    internal static async Task<JsonElement> Json(HttpResponseMessage response)
    {
        Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    internal static Guid Trace(HttpResponseMessage response) =>
        Guid.Parse(response.Headers.GetValues("X-Trace-Id").Single());
}
