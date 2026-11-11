using MESagent.Domain.Entities;
using MESagent.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace MESagent.Infrastructure.Persistence;

public sealed class MesDbContext(DbContextOptions<MesDbContext> options)
    : IdentityDbContext<MesUser, MesRole, Guid>(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasPostgresExtension("btree_gist");
        ConfigureIdentity(modelBuilder);
        FabModel.Configure(modelBuilder);
    }

    private static void ConfigureIdentity(ModelBuilder b)
    {
        b.Entity<MesUser>().ToTable("users", "security");
        b.Entity<MesRole>().ToTable("roles", "security");
        b.Entity<IdentityUserRole<Guid>>().ToTable("user_roles", "security");
        b.Entity<IdentityUserClaim<Guid>>().ToTable("user_claims", "security");
        b.Entity<IdentityUserLogin<Guid>>().ToTable("user_logins", "security");
        b.Entity<IdentityRoleClaim<Guid>>().ToTable("role_claims", "security");
        b.Entity<IdentityUserToken<Guid>>().ToTable("user_tokens", "security");

        b.Entity<MesUser>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(200);
            e.Property(x => x.AccountType).HasMaxLength(32);
            e.HasOne<Worker>().WithMany().HasForeignKey(x => x.WorkerId).OnDelete(DeleteBehavior.Restrict);
        });
        b.Entity<MesUser>().HasIndex(x => x.WorkerId).IsUnique().HasFilter("worker_id IS NOT NULL");

        b.Entity<UserSession>(e =>
        {
            e.ToTable(
                "user_sessions",
                "security",
                t => t.HasCheckConstraint(
                    "ck_session_times",
                    "absolute_expires_at > created_at AND idle_expires_at <= absolute_expires_at"));
            e.HasKey(x => x.Id);
            e.HasOne<MesUser>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<Terminal>().WithMany().HasForeignKey(x => x.TerminalId).OnDelete(DeleteBehavior.Restrict);
            e.Property(x => x.RowVersion).IsRowVersion();
            e.Property(x => x.SecurityStamp).HasMaxLength(128);
            e.Property(x => x.RevokeReason).HasMaxLength(500);
            e.HasIndex(x => new { x.UserId, x.RevokedAt });
        });

        b.Entity<SecurityEvent>(e =>
        {
            e.ToTable("security_events", "security");
            e.HasKey(x => x.Id);
            e.HasOne<MesUser>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
            e.Property(x => x.EventType).HasMaxLength(64);
            e.Property(x => x.Outcome).HasMaxLength(64);
            e.HasIndex(x => x.OccurredAt);
        });

        // Identity keeps its standard field semantics; PostgreSQL names are consistently snake_case.
        foreach (var entity in b.Model.GetEntityTypes().Where(e => e.GetSchema() == "security"))
            foreach (var property in entity.GetProperties())
                property.SetColumnName(
                    System.Text.RegularExpressions.Regex.Replace(property.Name, "(?<!^)(?=[A-Z])", "_")
                        .ToLowerInvariant());
        b.Entity<UserSession>().Property(x => x.RowVersion).HasColumnName("xmin");
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        StampUpdates();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        StampUpdates();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void StampUpdates()
    {
        foreach (var entry in ChangeTracker.Entries().Where(e => e.State == EntityState.Modified))
            if (entry.Metadata.FindProperty("UpdatedAt") is not null)
                entry.Property("UpdatedAt").CurrentValue = DateTime.UtcNow;
    }
}
