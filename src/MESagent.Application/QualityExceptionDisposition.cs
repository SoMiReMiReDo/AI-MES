namespace MESagent.Application;

public sealed record SaveQualityReason(string Code, string Name, string Category,
    string? Description = null, bool IsActive = true);

public sealed record SaveQualityIssueType(string Code, string Name, string Severity,
    bool IsBlocking, string? Description = null, bool IsActive = true);

public sealed record ReportQualityIssue(Guid IssueTypeId, string Title, string Description,
    Guid? WorkOrderId = null, Guid? LotId = null, Guid? WaferId = null,
    Guid? StepExecutionId = null, Guid? EquipmentId = null, Guid? HoldReasonCodeId = null);

public sealed record AssignQualityIssue(Guid AssignedToId);

public sealed record ResolveQualityIssue(string Disposition, string RootCause,
    string ContainmentAction, string Notes);

public sealed record CreateQualityAction(string Title, string ActionType, Guid AssignedToId,
    string? Description = null, DateOnly? DueDate = null);

public sealed record HoldQualityLot(Guid ReasonCodeId, Guid? IssueId = null);

public sealed record ReleaseQualityLot(string Reason, string Action = "None", Guid? IssueId = null);

public sealed record ScrapQualityWafers(Guid ReasonCodeId, Guid[] WaferIds, int Quantity,
    string Notes, Guid? IssueId = null, Guid? ProcessRunId = null);

public sealed record SaveQualitySpecification(string ParameterCode, int Version, string ValueType,
    string? Unit = null, decimal? SpecMin = null, decimal? SpecMax = null,
    bool? ExpectedBoolean = null, string? ExpectedText = null);

public sealed record SaveQualityTemplate(Guid RouteVersionId, string Name, int Version,
    int SamplesPerCheck, Guid[] SpecificationVersionIds, Guid? RouteStepId = null);

public sealed record RecordQualityMeasurement(Guid SpecificationVersionId, int SampleNumber,
    string? Unit = null, Guid? WaferId = null, string? SamplePoint = null,
    decimal? ValueNumeric = null, bool? ValueBoolean = null, string? ValueText = null,
    Guid? MeasurementEquipmentId = null);

public sealed record RecordQualityCheck(Guid LotId, string LotEtag, Guid QualityCheckTemplateId,
    RecordQualityMeasurement[] Measurements, Guid? ProcessRunId = null, string? Notes = null);

public interface IQualityExceptionDispositionBackend
{
    Task<OrganizationIdentityAccessControlResult> Execute(OrganizationIdentityAccessControlRequest request);
}
