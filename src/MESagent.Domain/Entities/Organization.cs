namespace MESagent.Domain.Entities;

/// <summary>Organization / fabs，字段范围见 docs/development-process/04-fab-data-field-retention.md。</summary>
public sealed class Fab
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public uint RowVersion { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public string Timezone { get; set; } = null!;
}

/// <summary>Organization / areas，字段范围见 docs/development-process/04-fab-data-field-retention.md。</summary>
public sealed class Area
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public uint RowVersion { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public Guid FabId { get; set; }
}

/// <summary>Organization / lines，字段范围见 docs/development-process/04-fab-data-field-retention.md。</summary>
public sealed class Line
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public uint RowVersion { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public Guid AreaId { get; set; }
}

/// <summary>Organization / locations，字段范围见 docs/development-process/04-fab-data-field-retention.md。</summary>
public sealed class Location
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public uint RowVersion { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public Guid AreaId { get; set; }
    public string LocationType { get; set; } = null!;
}

/// <summary>Organization / workers，字段范围见 docs/development-process/04-fab-data-field-retention.md。</summary>
public sealed class Worker
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public uint RowVersion { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public Guid FabId { get; set; }
}

/// <summary>Organization / workstations，字段范围见 docs/development-process/04-fab-data-field-retention.md。</summary>
public sealed class Workstation
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public uint RowVersion { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public Guid? LineId { get; set; }
    public Guid AreaId { get; set; }
}

/// <summary>Organization / terminals，字段范围见 docs/development-process/04-fab-data-field-retention.md。</summary>
public sealed class Terminal
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public uint RowVersion { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public Guid DeviceUuid { get; set; }
    public Guid? WorkstationId { get; set; }
    public DateTime? LastSeenAt { get; set; }
    public string? AppVersion { get; set; }
}

/// <summary>Organization / shifts，字段范围见 docs/development-process/04-fab-data-field-retention.md。</summary>
public sealed class Shift
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public uint RowVersion { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public Guid FabId { get; set; }
    public Guid? LineId { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public string DaysOfWeek { get; set; } = null!;
    public int SortOrder { get; set; }
}

/// <summary>Organization / shift_handovers，字段范围见 docs/development-process/04-fab-data-field-retention.md。</summary>
public sealed class ShiftHandover
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public uint RowVersion { get; set; }
    public Guid ShiftId { get; set; }
    public Guid? LineId { get; set; }
    public DateOnly BusinessDate { get; set; }
    public DateTime ShiftStart { get; set; }
    public DateTime ShiftEnd { get; set; }
    public string? Discrepancies { get; set; }
    public string? Notes { get; set; }
    public Guid? ConfirmedById { get; set; }
    public DateTime? ConfirmedAt { get; set; }
}
