namespace MESagent.Application;

public sealed record WorkbenchMonitorWindow(string Label, DateOnly Day, DateTime StartsAt, DateTime EndsAt);

public sealed record WorkbenchMonitorRun(
    Guid Id, Guid EquipmentId, string LotNumber, string? OrderNo, string Step, string Status,
    DateTime StartsAt, DateTime? EndsAt);

public sealed record WorkbenchMonitorStop(
    Guid Id, Guid EquipmentId, DateTime StartsAt, DateTime? EndsAt, string? Reason, bool NeedsReason, string? Notes);

public sealed record WorkbenchMonitorEquipment(
    Guid Id, string Code, string Name, string Area, string? Line, string State, DateTime? StateSince,
    WorkbenchMonitorStop? CurrentStop, WorkbenchMonitorRun? ActiveRun, WorkbenchMonitorWindow? Shift,
    WorkbenchShiftMetric? Metrics);

public sealed record WorkbenchMonitorCount(string State, int Count);

public sealed record WorkbenchPlantBoard(
    DateTime AsOf, WorkbenchOption[] Areas, WorkbenchMonitorCount[] Counts,
    PageView<WorkbenchMonitorEquipment> Equipment);

public sealed record WorkbenchMonitorState(string State, DateTime StartsAt, DateTime EndsAt);

public sealed record WorkbenchMonitorHour(DateTime StartsAt, DateTime EndsAt, int Passed, int Failed);

public sealed record WorkbenchShiftMonitor(
    DateTime AsOf, WorkbenchMonitorEquipment Equipment, WorkbenchOption[] Devices, WorkbenchMonitorWindow? Shift,
    DateTime? PreviousShift, DateTime? NextShift, WorkbenchShiftMetric? Metrics,
    WorkbenchMonitorState[] States, WorkbenchMonitorStop[] Stops,
    WorkbenchMonitorRun[]? Runs, WorkbenchMonitorHour[]? Hours);
