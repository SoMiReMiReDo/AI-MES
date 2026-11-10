namespace MESagent.Domain.Entities;

/// <summary>Security / permissions，字段范围见 docs/development-process/04-fab-data-field-retention.md。</summary>
public sealed class Permission
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public uint RowVersion { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
}

/// <summary>Security / role_permissions，字段范围见 docs/development-process/04-fab-data-field-retention.md。</summary>
public sealed class RolePermission
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public uint RowVersion { get; set; }
    public Guid RoleId { get; set; }
    public Guid PermissionId { get; set; }
}

/// <summary>Security / user_permissions，字段范围见 docs/development-process/04-fab-data-field-retention.md。</summary>
public sealed class UserPermission
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public uint RowVersion { get; set; }
    public Guid UserId { get; set; }
    public Guid PermissionId { get; set; }
}

/// <summary>Security / user_fab_scopes，字段范围见 docs/development-process/04-fab-data-field-retention.md。</summary>
public sealed class UserFabScope
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public uint RowVersion { get; set; }
    public Guid UserId { get; set; }
    public Guid FabId { get; set; }
}
