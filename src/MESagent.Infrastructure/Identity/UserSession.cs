namespace MESagent.Infrastructure.Identity;

public sealed class UserSession
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid UserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime LastActivityAt { get; set; }
    public DateTime IdleExpiresAt { get; set; }
    public DateTime AbsoluteExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public string? RevokeReason { get; set; }
    public bool Restricted { get; set; }
    public Guid? TerminalId { get; set; }
    public string SecurityStamp { get; set; } = null!;
    public uint RowVersion { get; set; }
}

public sealed class SecurityEvent
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public DateTime OccurredAt { get; set; }
    public Guid? UserId { get; set; }
    public Guid? SessionId { get; set; }
    public string EventType { get; set; } = null!;
    public string Outcome { get; set; } = null!;
    public Guid CorrelationId { get; set; }
}
