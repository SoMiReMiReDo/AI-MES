namespace MESagent.Domain.Entities;

public sealed class EquipmentOperationPolicy
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public uint RowVersion { get; set; }
    public Guid OperationId { get; set; }
    public bool RequireEquipmentQualification { get; set; }
    public bool RequireWorkerQualification { get; set; }
    public string? RequiredReticleLayer { get; set; }
}

public sealed class EquipmentMaterialRequirement
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public uint RowVersion { get; set; }
    public Guid OperationId { get; set; }
    public Guid MaterialId { get; set; }
}

public sealed class ReticleMount
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public uint RowVersion { get; set; }
    public Guid EquipmentId { get; set; }
    public Guid ReticleId { get; set; }
    public DateTime LoadedAt { get; set; }
    public DateTime? UnloadedAt { get; set; }
}

public sealed class MaterialRunUsage
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public uint RowVersion { get; set; }
    public Guid MaterialMountId { get; set; }
    public Guid ProcessRunId { get; set; }
}
