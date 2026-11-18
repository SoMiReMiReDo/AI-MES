using System.Text.Json;

namespace MESagent.Application;

public static class ReliabilityAccess
{
    public const string Audit = "audit.operations.read";
    public const string OutboxRead = "security.outbox.read";
    public const string OutboxReplay = "security.outbox.replay";
}

public sealed record OperationAuditView(
    Guid Id, Guid? UserId, string? ActorName, string Module, string EntityType, Guid EntityId, string Action,
    string SourceChannel, DateTime OccurredAt, Guid CorrelationId, JsonElement? BeforeState, JsonElement? AfterState);

public sealed record OutboxView(
    Guid Id, Guid? BusinessEventId, string EventType, string Status, DateTime OccurredAt, Guid CorrelationId,
    int Attempts, DateTime? NextAttemptAt, DateTime? PublishedAt, DateTime? DeadLetteredAt, string? ErrorCode,
    string Etag);

public sealed record OutboxStatus(int Pending, int Published, int DeadLettered, DateTime? OldestPendingAt);

public sealed record ReplayOutboxInput(string Reason);

public sealed record WorkbenchRefreshHint(int SchemaVersion, string[] Sources)
{
    public const string EventType = "WorkbenchRefreshRequested";
}
