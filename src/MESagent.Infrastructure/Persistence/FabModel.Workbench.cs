using System.Text.RegularExpressions;
using MESagent.Domain.Entities;
using MESagent.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;

namespace MESagent.Infrastructure.Persistence;

internal static partial class FabModel
{
    private static void ConfigureWorkbench(ModelBuilder model)
    {
        model.Entity<EquipmentIdealRate>(e =>
        {
            e.ToTable("equipment_ideal_rates", "workbench", t =>
                t.HasCheckConstraint("ck_equipment_ideal_rate", "ideal_seconds > 0 AND ideal_seconds <= 86400"));
            e.HasOne<Fab>().WithMany().HasForeignKey(x => x.FabId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<Equipment>().WithMany().HasForeignKey(x => x.EquipmentId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => new { x.FabId, x.EquipmentId, x.EffectiveOn }).IsUnique();
            e.Property(x => x.IdealSeconds).HasPrecision(12, 3);
        });
        model.Entity<ProductionScheduleBlock>(e =>
        {
            e.ToTable("production_schedule_blocks", "workbench", t =>
                t.HasCheckConstraint("ck_schedule_block_range", "starts_at < ends_at"));
            e.HasOne<Fab>().WithMany().HasForeignKey(x => x.FabId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<Equipment>().WithMany().HasForeignKey(x => x.EquipmentId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<StepExecution>().WithMany().HasForeignKey(x => x.StepExecutionId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => new { x.FabId, x.EquipmentId, x.StartsAt });
            e.HasIndex(x => new { x.FabId, x.StepExecutionId }).IsUnique().HasFilter("cancelled_at IS NULL");
            e.Property(x => x.RowVersion).HasColumnName("xmin").IsRowVersion();
        });
        model.Entity<WorkbenchSubscription>(e =>
        {
            e.ToTable("subscriptions", "workbench", t => t.HasCheckConstraint(
                "ck_subscription_escalation", "(escalation_minutes IS NULL AND escalate_to_id IS NULL) OR "
                + "(escalation_minutes IS NOT NULL AND escalation_minutes BETWEEN 1 AND 10080 "
                + "AND escalate_to_id IS NOT NULL AND escalate_to_id <> user_id)"));
            e.HasOne<Fab>().WithMany().HasForeignKey(x => x.FabId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<MesUser>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<MesUser>().WithMany().HasForeignKey(x => x.EscalateToId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => new { x.FabId, x.UserId, x.Kind }).IsUnique();
            e.HasIndex(x => new { x.FabId, x.EscalateToId, x.Kind });
            e.Property(x => x.Kind).HasMaxLength(24);
        });
        model.Entity<WorkbenchAlertReceipt>(e =>
        {
            e.ToTable("alert_receipts", "workbench");
            e.HasOne<Fab>().WithMany().HasForeignKey(x => x.FabId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<MesUser>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => new { x.FabId, x.UserId, x.Kind, x.SourceId }).IsUnique();
            e.Property(x => x.Kind).HasMaxLength(24);
        });
        foreach (var type in new[]
        {
            typeof(EquipmentIdealRate), typeof(ProductionScheduleBlock),
            typeof(WorkbenchSubscription), typeof(WorkbenchAlertReceipt)
        })
            foreach (var property in model.Model.FindEntityType(type)!.GetProperties().Where(p => p.Name != "RowVersion"))
                property.SetColumnName(Regex.Replace(property.Name, "(?<!^)(?=[A-Z])", "_").ToLowerInvariant());
    }
}
