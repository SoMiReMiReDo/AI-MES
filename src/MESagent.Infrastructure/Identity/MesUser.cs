using Microsoft.AspNetCore.Identity;

namespace MESagent.Infrastructure.Identity;

public sealed class MesUser : IdentityUser<Guid>
{
    public string Name { get; set; } = string.Empty;
    public Guid? WorkerId { get; set; }
    public string AccountType { get; set; } = "Human";
    public bool IsActive { get; set; } = true;
    public bool ForcePasswordChange { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public sealed class MesRole : IdentityRole<Guid>;
