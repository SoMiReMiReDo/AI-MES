using System.Text.Json;
using MESagent.Application;
using MESagent.Domain.Entities;
using MESagent.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MESagent.Infrastructure.ApplicationAuditReliability;

public sealed class OutboxDelivery(
    MesDbContext db, OrganizationIdentityAccessControlOptions options, TimeProvider clock)
{
    private const int AttemptLimit = 5;
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    internal static IQueryable<OutboxMessage> Scope(MesDbContext db, Guid fabId) => db.Set<OutboxMessage>()
        .Where(x => x.AggregateType == "Fab" && x.AggregateId == fabId
            && x.EventType == WorkbenchRefreshHint.EventType && x.BusinessEventId != null);

    internal static void Enqueue(
        MesDbContext db, Guid fabId, RequestActor actor,
        OrganizationIdentityAccessControlRequest request, OrganizationIdentityAccessControlResult result, DateTime now)
    {
        if (request.Method == "GET" || result.Replayed || result.Status is < 200 or >= 300)
            return;

        var path = request.Path.Split('/');
        var sources = path[0] switch
        {
            "production" => new[] { WorkbenchAccess.Lots, WorkbenchAccess.Execution, WorkbenchAccess.Quality },
            "execution" => WorkbenchAccess.Sources,
            "quality" => [WorkbenchAccess.Quality, WorkbenchAccess.Lots, WorkbenchAccess.Execution],
            "equipment" => [WorkbenchAccess.Equipment, WorkbenchAccess.Execution],
            "planning" or "engineering" => [WorkbenchAccess.Lots, WorkbenchAccess.Execution],
            "workbench" when path[1] is "rates" or "schedule" => [WorkbenchAccess.Execution, WorkbenchAccess.Equipment],
            "integration" when path[1] == "connections" && db.ChangeTracker.Entries<EquipmentEvent>()
                .Any(x => x.Entity.ProcessingStatus == "Applied") =>
                    [WorkbenchAccess.Equipment, WorkbenchAccess.Execution],
            "integration" when path[^1] == "confirm" && db.ChangeTracker.Entries<ImportJob>()
                .Any(x => x.Entity.Status == "Imported") => [WorkbenchAccess.Lots, WorkbenchAccess.Execution],
            _ => []
        };
        if (sources.Length == 0 || !db.ChangeTracker.Entries<AuditLog>()
            .Any(x => x.Entity.CorrelationId == actor.CorrelationId && x.Entity.FabId == fabId))
            return;

        var payload = JsonSerializer.Serialize(new WorkbenchRefreshHint(1, sources), Json);
        var evt = new BusinessEvent
        {
            UserId = actor.UserId,
            EntityType = "Fab",
            EntityId = fabId,
            EventType = WorkbenchRefreshHint.EventType,
            Payload = payload,
            OccurredAt = now,
            CorrelationId = actor.CorrelationId
        };
        db.Add(evt);
        db.Add(new OutboxMessage
        {
            BusinessEventId = evt.Id,
            EventType = evt.EventType,
            AggregateType = evt.EntityType,
            AggregateId = evt.EntityId,
            Payload = evt.Payload,
            OccurredAt = evt.OccurredAt,
            CorrelationId = evt.CorrelationId
        });
    }

    public async Task<OutboxDeliveryResult?> DeliverNext(
        Action<Guid, string[]> publish, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var rows = await db.Set<OutboxMessage>().FromSqlInterpolated($"""
            SELECT m.*, m.xmin
            FROM audit.outbox_messages m
            WHERE m.aggregate_type = 'Fab' AND m.aggregate_id = {options.DeploymentFabId}
                AND m.event_type = {WorkbenchRefreshHint.EventType} AND m.business_event_id IS NOT NULL
                AND m.published_at IS NULL AND m.dead_lettered_at IS NULL
                AND (m.next_attempt_at IS NULL OR m.next_attempt_at <= {now})
            ORDER BY coalesce(m.next_attempt_at, m.occurred_at), m.occurred_at, m.id
            LIMIT 1 FOR UPDATE SKIP LOCKED
            """).ToListAsync(cancellationToken);
        if (rows.Count == 0)
            return null;

        var message = rows[0];
        string? error = null;
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var hint = JsonSerializer.Deserialize<WorkbenchRefreshHint>(message.Payload, Json);
            if (hint is not { SchemaVersion: 1, Sources.Length: > 0 }
                || !hint.Sources.All(WorkbenchAccess.Sources.Contains))
                throw new JsonException();
            publish(message.BusinessEventId!.Value, hint.Sources);
        }
        catch (JsonException)
        {
            error = "InvalidPayload";
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            error = "DeliveryFailed";
        }

        now = clock.GetUtcNow().UtcDateTime;
        message.Attempts++;
        message.LastError = error;
        message.NextAttemptAt = null;
        if (error == null)
            message.PublishedAt = now;
        else if (message.Attempts >= AttemptLimit)
            message.DeadLetteredAt = now;
        else
            message.NextAttemptAt = now.AddSeconds(5 * (1 << (message.Attempts - 1)));

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return new OutboxDeliveryResult(message.Id, message.CorrelationId, message.Attempts,
            message.PublishedAt != null ? "Published" : message.DeadLetteredAt != null ? "DeadLettered" : "Retry",
            error);
    }
}

public sealed record OutboxDeliveryResult(Guid Id, Guid CorrelationId, int Attempts, string Outcome, string? ErrorCode);
