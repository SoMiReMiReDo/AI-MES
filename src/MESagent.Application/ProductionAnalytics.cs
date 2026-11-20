using System.Text.Json.Serialization;

namespace MESagent.Application;

public sealed record AnalyticsEvidence(string Kind, Guid Id, DateTime? OccurredAt, string Url);

public sealed record AnalyticsContext(
    DateTime AsOf, string Timezone, string MetricVersion, IReadOnlyDictionary<string, string> Filters,
    IReadOnlyDictionary<string, string> Units, string[] Missing, AnalyticsEvidence[] Evidence);

public sealed record AnalyticsWorkOrder(
    Guid Id, string OrderNo, WorkbenchOption Product, WorkbenchOption Revision, string Status, string Event,
    DateTime? EventAt, DateTime? CompletedAt, DateTime? TerminalAt, DateOnly? DueDate, int Planned,
    int? Released, int? Completed,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] int? Scrapped,
    bool? OnTime, double? ProductionMinutes, string[] Missing,
    AnalyticsEvidence[] Evidence);

public sealed record AnalyticsOrderSummary(
    int Orders, long Planned, long? Released, long? Completed,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] long? Scrapped,
    int Reached, int ClosedBelowPlan,
    int OnTime, int WithDueDate, double? OnTimeRatio, double? AverageProductionMinutes, int DurationSamples);

public sealed record AnalyticsOrders(
    AnalyticsContext Context, AnalyticsOrderSummary Summary, PageView<AnalyticsWorkOrder> Records,
    WorkbenchOption[] Products, WorkbenchOption[] Revisions);

public sealed record AnalyticsOrderBaseline(int Version, DateTime ReleasedAt, int? Planned, int? Priority,
    DateOnly? DueDate);

public sealed record AnalyticsOrderLot(Guid Id, string Number, string Status, AnalyticsEvidence Evidence);

public sealed record AnalyticsOrderDetail(
    AnalyticsContext Context, AnalyticsWorkOrder Order, AnalyticsOrderBaseline? Baseline,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] PageView<AnalyticsOrderLot>? Lots);

public sealed record AnalyticsExport(byte[] Content, string Filename);

public sealed record AnalyticsBreakdown(Guid? Id, string? Code, string Name, int Wafers,
    double? CumulativeRatio = null);

public sealed record AnalyticsScrapDay(DateOnly Date, int Wafers);

public sealed record AnalyticsScrapRecord(
    Guid Id, DateTime OccurredAt, Guid EntryId, string LotNumber, string WaferCode,
    WorkbenchOption Product, WorkbenchOption Reason, WorkbenchOption? Operation,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] WorkbenchOption? Equipment,
    AnalyticsEvidence[] Evidence);

public sealed record AnalyticsScrap(
    AnalyticsContext Context, int Wafers, int Entries, int? Completed, int? Settled, double? SettledScrapRatio,
    AnalyticsBreakdown[] Reasons, AnalyticsScrapDay[] Days, AnalyticsBreakdown[] Operations,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] AnalyticsBreakdown[]? Equipment,
    WorkbenchOption[] Products, WorkbenchOption[] OperationOptions, WorkbenchOption[] ReasonOptions,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] WorkbenchOption[]? EquipmentOptions);

public sealed record AnalyticsScrapRecords(AnalyticsContext Context, PageView<AnalyticsScrapRecord> Records);

public sealed record AnalyticsOeeMetrics(
    double? PlannedMinutes, double? RunMinutes, double? DowntimeMinutes, double? IdealMinutes,
    double? Processed, double? Passed, double? Failed,
    double? Availability, double? Performance, double? Quality, double? Oee, string[] Missing);

public sealed record AnalyticsOeePoint(DateOnly Date, AnalyticsOeeMetrics Metrics);

public sealed record AnalyticsOeeDevice(
    Guid Id, string Code, string Name, Guid AreaId, Guid? LineId, int StartedShifts, int CompleteShifts,
    AnalyticsOeeMetrics Metrics, AnalyticsOeePoint[] Days);

public sealed record AnalyticsOeeShift(
    Guid EquipmentId, string EquipmentCode, Guid? ShiftId, string? Shift,
    DateOnly? Date, DateTime? StartsAt, DateTime? EndsAt, decimal? IdealSeconds,
    AnalyticsOeeMetrics Metrics, AnalyticsEvidence[] Evidence);

public sealed record AnalyticsOee(
    AnalyticsContext Context, PageView<AnalyticsOeeDevice> Devices,
    WorkbenchOption[] Areas, WorkbenchOption[] Lines, WorkbenchOption[] Equipment, WorkbenchOption[] Shifts);

public sealed record AnalyticsOeeShifts(AnalyticsContext Context, PageView<AnalyticsOeeShift> Records);

public sealed record AnalyticsCostSource(string Kind, string Status, string Description, AnalyticsEvidence? Evidence);

public sealed record AnalyticsCostReadiness(
    AnalyticsContext Context, AnalyticsCostSource[] Sources, decimal? TotalCost, decimal? UnitCost);
