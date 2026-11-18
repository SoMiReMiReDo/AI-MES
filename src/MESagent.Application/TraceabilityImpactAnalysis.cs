namespace MESagent.Application;

public sealed class TraceObject
{
    public Guid Id { get; init; }
    public required string Kind { get; init; }
    public required string Code { get; init; }
    public required string Status { get; init; }
    public string? Name { get; init; }
}

public sealed record TraceLink(Guid Id, string Kind, string Code, string Relation);

public sealed record TraceOverview(
    TraceObject Subject,
    string[] Groups,
    TraceLink[] Related,
    string? OriginCoverage,
    DateTime AsOf);

public sealed record TraceEvent(
    Guid SourceId,
    string Type,
    DateTime OccurredAt,
    DateTime? EndedAt,
    string? Summary,
    string? Result,
    string? Actor,
    TraceLink[] Related);

public sealed record TraceField(string Label, string Value);

public sealed record TraceEvidence(TraceEvent Event, TraceField[] Fields);
