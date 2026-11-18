using System.Net;
using MESagent.Application;
using MESagent.Domain.Entities;
using MESagent.Infrastructure.ApplicationAuditReliability;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Xunit;
using static MESagent.Api.Tests.ApplicationAuditReliabilityTests;
using static MESagent.Api.Tests.OrganizationIdentityAccessControlFixture;

namespace MESagent.Api.Tests;

public sealed class OutboxDeliveryTests(OrganizationIdentityAccessControlFixture f)
    : IClassFixture<OrganizationIdentityAccessControlFixture>
{
    [Fact]
    public async Task Retries_dead_letters_and_replays_with_durable_claim_and_hosted_recovery()
    {
        await using var db = f.Database.CreateContext();
        var other = new Fab { Code = "OTHER", Name = "其他厂区", Timezone = "Asia/Shanghai" };
        db.Add(other);
        var payload = """{"schemaVersion":1,"sources":["execution.read"]}""";
        var queued = AddMessage(f.Fab);
        var foreign = AddMessage(other.Id, deadLettered: true);
        await db.SaveChangesAsync();
        var incomingTrace = queued.CorrelationId;
        var options = new OrganizationIdentityAccessControlOptions { DeploymentFabId = f.Fab };

        try
        {
            var original = f.Clock.Offset;
            for (var attempt = 1; attempt <= 5; attempt++)
            {
                await using var workerDb = f.Database.CreateContext();
                var worker = new OutboxDelivery(workerDb, options, f.Clock);
                var result = await worker.DeliverNext((_, _) => throw new InvalidOperationException("secret"), default);
                Assert.NotNull(result);
                Assert.Equal(queued.Id, result.Id);
                Assert.Equal("DeliveryFailed", result.ErrorCode);
                Assert.Equal(attempt, result.Attempts);
                if (attempt < 5)
                {
                    Assert.Equal("Retry", result.Outcome);
                    Assert.Null(await worker.DeliverNext((_, _) => throw new Exception("Too early"), default));
                    f.Clock.Offset += TimeSpan.FromSeconds(5 * (1 << (attempt - 1)) + 1);
                }
                else
                    Assert.Equal("DeadLettered", result.Outcome);
            }
            f.Clock.Offset = original;

            using var viewer = (await f.NewUser()).Client;
            using var summary = await f.Admin.GetAsync("/api/v1/reliability/status");
            Assert.Equal(1, (await Json(summary)).GetProperty("deadLettered").GetInt32());
            using var dead = await f.Admin.GetAsync("/api/v1/reliability/outbox?status=DeadLettered");
            var row = Assert.Single((await Json(dead)).GetProperty("items").EnumerateArray());
            Assert.Equal(queued.Id, row.GetProperty("id").GetGuid());
            Assert.Equal("DeliveryFailed", row.GetProperty("errorCode").GetString());
            var etag = row.GetProperty("etag").GetString()!;
            var path = $"reliability/outbox/{queued.Id}/replay";
            var reason = new { reason = "已核对并恢复事件投递" };
            Assert.Equal(HttpStatusCode.Forbidden, (await Send(viewer, path, reason, etag: etag)).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound,
                (await Send(f.Admin, $"reliability/outbox/{foreign.Id}/replay", reason, etag: etag)).StatusCode);
            Assert.Equal((HttpStatusCode)428, (await Send(f.Admin, path, reason)).StatusCode);
            Assert.Equal(HttpStatusCode.PreconditionFailed,
                (await Send(f.Admin, path, reason, etag: "\"stale\"")).StatusCode);

            using var replay = await Send(f.Admin, path, reason, etag: etag);
            var replayed = await Json(replay);
            Assert.Equal("Pending", replayed.GetProperty("status").GetString());
            Assert.Equal(0, replayed.GetProperty("attempts").GetInt32());
            Assert.Equal(queued.BusinessEventId, replayed.GetProperty("businessEventId").GetGuid());
            Assert.True(await db.Set<AuditLog>().AnyAsync(x => x.EntityId == queued.Id && x.CorrelationId == Trace(replay)
                && x.Action == "OutboxReplayQueued"));
            Assert.Equal(HttpStatusCode.Conflict,
                (await Send(f.Admin, path, reason, etag: replayed.GetProperty("etag").GetString())).StatusCode);

            var dispatchCount = 0;
            using var started = new ManualResetEventSlim();
            using var release = new ManualResetEventSlim();
            await using var firstDb = f.Database.CreateContext();
            await using var secondDb = f.Database.CreateContext();
            var firstWorker = new OutboxDelivery(firstDb, options, f.Clock);
            var first = Task.Run(() => firstWorker.DeliverNext((id, sources) =>
            {
                Assert.Equal(queued.BusinessEventId, id);
                Assert.Equal(new[] { WorkbenchAccess.Execution }, sources);
                Interlocked.Increment(ref dispatchCount);
                started.Set();
                if (!release.Wait(TimeSpan.FromSeconds(5)))
                    throw new TimeoutException();
            }, default));
            try
            {
                Assert.True(started.Wait(TimeSpan.FromSeconds(5)));
                Assert.Null(await new OutboxDelivery(secondDb, options, f.Clock)
                    .DeliverNext((_, _) => Interlocked.Increment(ref dispatchCount), default));
            }
            finally
            {
                release.Set();
            }
            Assert.Equal("Published", (await first)?.Outcome);
            Assert.Equal(1, dispatchCount);
            Assert.Null(await new OutboxDelivery(secondDb, options, f.Clock).DeliverNext((_, _) => { }, default));
            var published = await db.Set<OutboxMessage>().AsNoTracking().SingleAsync(x => x.CorrelationId == incomingTrace);
            Assert.Equal(1, published.Attempts);
            Assert.NotNull(published.PublishedAt);

            var pending = AddMessage(f.Fab);
            await db.SaveChangesAsync();
            Assert.Null(pending.PublishedAt);
            using var host = f.Host.WithWebHostBuilder(b => b.UseSetting("Reliability:OutboxEnabled", "true"));
            using var client = host.CreateClient();
            using var health = await client.GetAsync("/health/ready");
            Assert.Equal(HttpStatusCode.OK, health.StatusCode);
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            while (!await db.Set<OutboxMessage>().AnyAsync(x => x.Id == pending.Id && x.PublishedAt != null, timeout.Token))
                await Task.Delay(75, timeout.Token);
            Assert.True(await db.Set<OutboxMessage>().AnyAsync(x => x.Id == foreign.Id && x.DeadLetteredAt != null));
        }
        finally
        {
            f.Clock.Offset = TimeSpan.Zero;
        }

        OutboxMessage AddMessage(Guid fabId, bool deadLettered = false)
        {
            var evt = new BusinessEvent
            {
                EventType = WorkbenchRefreshHint.EventType, EntityType = "Fab", EntityId = fabId,
                Payload = payload, OccurredAt = DateTime.UtcNow.AddMinutes(-1), CorrelationId = Guid.NewGuid()
            };
            db.Add(evt);
            var message = new OutboxMessage
            {
                BusinessEventId = evt.Id, EventType = evt.EventType, AggregateType = evt.EntityType,
                AggregateId = fabId, Payload = evt.Payload, OccurredAt = evt.OccurredAt,
                CorrelationId = evt.CorrelationId, DeadLetteredAt = deadLettered ? DateTime.UtcNow : null
            };
            db.Add(message);
            return message;
        }
    }
}
