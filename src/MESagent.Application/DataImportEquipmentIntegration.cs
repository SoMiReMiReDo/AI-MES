using System.Text.Json;

namespace MESagent.Application;

public static class IntegrationAccess
{
    public const string Simulator = "integration.simulator.read";
    public const string WorkOrderImport = "integration.work-orders.import";
}

public sealed record RegisterSimulatorConnection(Guid EquipmentId, string Name);

public sealed record ReceiveSimulatorMessage(string SourceMessageId, string RawPayload);

public sealed record SimulatorPayload(
    DateTime OccurredAt,
    string Kind,
    string State,
    string Reason,
    Guid? ReasonCodeId = null,
    Guid? ShiftId = null);

public sealed record SimulatorConnectionView(
    Guid Id, Guid EquipmentId, string EquipmentCode, string Name, string Status, DateTime? LastConnectedAt);

public sealed record SimulatorMessageSummary(
    Guid Id, string SourceMessageId, DateTime ReceivedAt, string Status, Guid? EquipmentEventId);

public sealed record SimulatorMessageView(
    Guid Id,
    string SourceMessageId,
    DateTime ReceivedAt,
    string Status,
    Guid? EquipmentEventId,
    string RawPayload,
    JsonElement? ParsedData,
    string? Error);

public sealed record PreviewWorkOrderCsv(string Filename, string Csv, Dictionary<string, string> Mapping);

public sealed record WorkOrderImportIssue(int Row, string Field, string Message);

public sealed record WorkOrderImportRow(
    int Row,
    string OrderNo,
    string ProductCode,
    string RevisionCode,
    Guid ProductId,
    Guid ProductRevisionId,
    int PlannedQty,
    int Priority,
    DateOnly? DueDate,
    DateTime? PlannedStartAt,
    DateTime? PlannedEndAt,
    string? Description);

public sealed record ImportedWorkOrder(int Row, Guid Id, string OrderNo);

public sealed record WorkOrderImportSummary(
    Guid Id, string Filename, string Status, DateTime CreatedAt, int TotalRows, int SuccessfulRows, int FailedRows);

public sealed record WorkOrderImportView(
    Guid Id,
    string Filename,
    string Status,
    DateTime CreatedAt,
    int TotalRows,
    int SuccessfulRows,
    int FailedRows,
    string Etag,
    Dictionary<string, string> Mapping,
    WorkOrderImportRow[] Rows,
    WorkOrderImportIssue[] Errors,
    ImportedWorkOrder[] Orders);
