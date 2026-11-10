using MESagent.Domain.Entities;
using MESagent.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;

namespace MESagent.Infrastructure.Persistence;

internal static partial class FabModel
{
    private static void ConfigureOrganization(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Fab>(b =>
        {
            b.ToTable("fabs", "organization", t =>
            {
            });
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("uuidv7()");
            b.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.RowVersion).HasColumnName("xmin").IsRowVersion();
            b.Property(x => x.Code).HasColumnName("code").HasMaxLength(64);
            b.Property(x => x.Name).HasColumnName("name").HasMaxLength(200);
            b.Property(x => x.Description).HasColumnName("description");
            b.Property(x => x.IsActive).HasColumnName("is_active").HasDefaultValue(true);
            b.Property(x => x.Timezone).HasColumnName("timezone").HasMaxLength(64);
            b.HasIndex(x => x.Code).IsUnique();
        });
        modelBuilder.Entity<Area>(b =>
        {
            b.ToTable("areas", "organization", t =>
            {
            });
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("uuidv7()");
            b.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.RowVersion).HasColumnName("xmin").IsRowVersion();
            b.Property(x => x.Code).HasColumnName("code").HasMaxLength(64);
            b.Property(x => x.Name).HasColumnName("name").HasMaxLength(200);
            b.Property(x => x.Description).HasColumnName("description");
            b.Property(x => x.IsActive).HasColumnName("is_active").HasDefaultValue(true);
            b.Property(x => x.FabId).HasColumnName("fab_id");
            b.HasOne<Fab>().WithMany().HasForeignKey(x => x.FabId).OnDelete(DeleteBehavior.Restrict);
            b.HasIndex(x => new { x.FabId, x.Code }).IsUnique();
        });
        modelBuilder.Entity<Line>(b =>
        {
            b.ToTable("lines", "organization", t =>
            {
            });
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("uuidv7()");
            b.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.RowVersion).HasColumnName("xmin").IsRowVersion();
            b.Property(x => x.Code).HasColumnName("code").HasMaxLength(64);
            b.Property(x => x.Name).HasColumnName("name").HasMaxLength(200);
            b.Property(x => x.Description).HasColumnName("description");
            b.Property(x => x.IsActive).HasColumnName("is_active").HasDefaultValue(true);
            b.Property(x => x.AreaId).HasColumnName("area_id");
            b.HasOne<Area>().WithMany().HasForeignKey(x => x.AreaId).OnDelete(DeleteBehavior.Restrict);
            b.HasIndex(x => new { x.AreaId, x.Code }).IsUnique();
        });
        modelBuilder.Entity<Location>(b =>
        {
            b.ToTable("locations", "organization", t =>
            {
            });
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("uuidv7()");
            b.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.RowVersion).HasColumnName("xmin").IsRowVersion();
            b.Property(x => x.Code).HasColumnName("code").HasMaxLength(64);
            b.Property(x => x.Name).HasColumnName("name").HasMaxLength(200);
            b.Property(x => x.Description).HasColumnName("description");
            b.Property(x => x.IsActive).HasColumnName("is_active").HasDefaultValue(true);
            b.Property(x => x.AreaId).HasColumnName("area_id");
            b.HasOne<Area>().WithMany().HasForeignKey(x => x.AreaId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.LocationType).HasColumnName("location_type").HasMaxLength(32);
            b.HasIndex(x => new { x.AreaId, x.Code }).IsUnique();
        });
        modelBuilder.Entity<Worker>(b =>
        {
            b.ToTable("workers", "organization", t =>
            {
            });
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("uuidv7()");
            b.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.RowVersion).HasColumnName("xmin").IsRowVersion();
            b.Property(x => x.Code).HasColumnName("code").HasMaxLength(64);
            b.Property(x => x.Name).HasColumnName("name").HasMaxLength(200);
            b.Property(x => x.Description).HasColumnName("description");
            b.Property(x => x.IsActive).HasColumnName("is_active").HasDefaultValue(true);
            b.Property(x => x.FabId).HasColumnName("fab_id");
            b.HasOne<Fab>().WithMany().HasForeignKey(x => x.FabId).OnDelete(DeleteBehavior.Restrict);
            b.HasIndex(x => new { x.FabId, x.Code }).IsUnique();
        });
        modelBuilder.Entity<Workstation>(b =>
        {
            b.ToTable("workstations", "organization", t =>
            {
            });
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("uuidv7()");
            b.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.RowVersion).HasColumnName("xmin").IsRowVersion();
            b.Property(x => x.Code).HasColumnName("code").HasMaxLength(64);
            b.Property(x => x.Name).HasColumnName("name").HasMaxLength(200);
            b.Property(x => x.Description).HasColumnName("description");
            b.Property(x => x.IsActive).HasColumnName("is_active").HasDefaultValue(true);
            b.Property(x => x.LineId).HasColumnName("line_id");
            b.HasOne<Line>().WithMany().HasForeignKey(x => x.LineId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.AreaId).HasColumnName("area_id");
            b.HasOne<Area>().WithMany().HasForeignKey(x => x.AreaId).OnDelete(DeleteBehavior.Restrict);
            b.HasIndex(x => new { x.AreaId, x.Code }).IsUnique();
        });
        modelBuilder.Entity<Terminal>(b =>
        {
            b.ToTable("terminals", "organization", t =>
            {
            });
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("uuidv7()");
            b.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.RowVersion).HasColumnName("xmin").IsRowVersion();
            b.Property(x => x.Code).HasColumnName("code").HasMaxLength(64);
            b.Property(x => x.Name).HasColumnName("name").HasMaxLength(200);
            b.Property(x => x.Description).HasColumnName("description");
            b.Property(x => x.IsActive).HasColumnName("is_active").HasDefaultValue(true);
            b.Property(x => x.DeviceUuid).HasColumnName("device_uuid");
            b.Property(x => x.WorkstationId).HasColumnName("workstation_id");
            b.HasOne<Workstation>().WithMany().HasForeignKey(x => x.WorkstationId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.LastSeenAt).HasColumnName("last_seen_at");
            b.Property(x => x.AppVersion).HasColumnName("app_version").HasMaxLength(64);
            b.HasIndex(x => x.DeviceUuid).IsUnique();
        });
        modelBuilder.Entity<Shift>(b =>
        {
            b.ToTable("shifts", "organization", t =>
            {
            });
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("uuidv7()");
            b.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.RowVersion).HasColumnName("xmin").IsRowVersion();
            b.Property(x => x.Code).HasColumnName("code").HasMaxLength(64);
            b.Property(x => x.Name).HasColumnName("name").HasMaxLength(200);
            b.Property(x => x.Description).HasColumnName("description");
            b.Property(x => x.IsActive).HasColumnName("is_active").HasDefaultValue(true);
            b.Property(x => x.FabId).HasColumnName("fab_id");
            b.HasOne<Fab>().WithMany().HasForeignKey(x => x.FabId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.LineId).HasColumnName("line_id");
            b.HasOne<Line>().WithMany().HasForeignKey(x => x.LineId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.StartTime).HasColumnName("start_time");
            b.Property(x => x.EndTime).HasColumnName("end_time");
            b.Property(x => x.DaysOfWeek).HasColumnName("days_of_week").HasColumnType("jsonb");
            b.Property(x => x.SortOrder).HasColumnName("sort_order");
            b.HasIndex(x => new { x.FabId, x.Code }).IsUnique();
        });
        modelBuilder.Entity<ShiftHandover>(b =>
        {
            b.ToTable("shift_handovers", "organization", t =>
            {
                t.HasCheckConstraint("ck_shift_handovers_1", "shift_end > shift_start");
            });
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("uuidv7()");
            b.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.RowVersion).HasColumnName("xmin").IsRowVersion();
            b.Property(x => x.ShiftId).HasColumnName("shift_id");
            b.HasOne<Shift>().WithMany().HasForeignKey(x => x.ShiftId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.LineId).HasColumnName("line_id");
            b.HasOne<Line>().WithMany().HasForeignKey(x => x.LineId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.BusinessDate).HasColumnName("business_date");
            b.Property(x => x.ShiftStart).HasColumnName("shift_start");
            b.Property(x => x.ShiftEnd).HasColumnName("shift_end");
            b.Property(x => x.Discrepancies).HasColumnName("discrepancies").HasColumnType("jsonb");
            b.Property(x => x.Notes).HasColumnName("notes");
            b.Property(x => x.ConfirmedById).HasColumnName("confirmed_by_id");
            b.HasOne<MesUser>().WithMany().HasForeignKey(x => x.ConfirmedById).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.ConfirmedAt).HasColumnName("confirmed_at");
            b.HasIndex(x => new { x.ShiftId, x.BusinessDate }).IsUnique();
        });
    }
}
