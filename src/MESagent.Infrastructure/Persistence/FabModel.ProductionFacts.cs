using System.Text.RegularExpressions;
using MESagent.Domain.Entities;
using MESagent.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;

namespace MESagent.Infrastructure.Persistence;

internal static partial class FabModel
{
    private static void ConfigureProductionFacts(ModelBuilder b)
    {
        b.Entity<Wafer>().Property(x => x.FabId).HasColumnName("fab_id");
        b.Entity<Wafer>().Property(x => x.WorkOrderId).HasColumnName("work_order_id");
        b.Entity<Wafer>().HasOne<Fab>().WithMany().HasForeignKey(x => x.FabId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Wafer>().HasOne<WorkOrder>().WithMany().HasForeignKey(x => x.WorkOrderId)
            .OnDelete(DeleteBehavior.Restrict);
        b.Entity<Carrier>().Property(x => x.AllowedWaferSizeMm)
            .HasColumnName("allowed_wafer_size_mm").HasPrecision(20, 6);
        b.Entity<Lot>().Property(x => x.ClosedAt).HasColumnName("closed_at");
        b.Entity<Lot>().Property(x => x.CloseReason).HasColumnName("close_reason").HasMaxLength(1000);

        b.Entity<WaferRelease>(e =>
        {
            e.ToTable("wafer_releases", "production");
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.WaferId).IsUnique();
            e.HasOne<Wafer>().WithMany().HasForeignKey(x => x.WaferId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<WorkOrder>().WithMany().HasForeignKey(x => x.WorkOrderId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<Lot>().WithMany().HasForeignKey(x => x.InitialLotId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<RouteVersion>().WithMany().HasForeignKey(x => x.RouteVersionId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<MesUser>().WithMany().HasForeignKey(x => x.ReleasedById).OnDelete(DeleteBehavior.Restrict);
        });
        b.Entity<WaferCompletion>(e =>
        {
            e.ToTable("wafer_completions", "production");
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.WaferId).IsUnique();
            e.HasOne<Wafer>().WithMany().HasForeignKey(x => x.WaferId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<WorkOrder>().WithMany().HasForeignKey(x => x.WorkOrderId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<Lot>().WithMany().HasForeignKey(x => x.LotIdAtCompletion).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<ProcessRun>().WithMany().HasForeignKey(x => x.ProcessRunId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<RouteVersion>().WithMany().HasForeignKey(x => x.RouteVersionId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<RouteStep>().WithMany().HasForeignKey(x => x.RouteStepId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<MesUser>().WithMany().HasForeignKey(x => x.CompletedById).OnDelete(DeleteBehavior.Restrict);
        });
        b.Entity<StepExecutionInheritance>(e =>
        {
            e.ToTable("step_execution_inheritances", "production");
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.GenealogyEventId, x.SourceStepExecutionId, x.TargetStepExecutionId }).IsUnique();
            e.HasOne<GenealogyEvent>().WithMany().HasForeignKey(x => x.GenealogyEventId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne<StepExecution>().WithMany().HasForeignKey(x => x.SourceStepExecutionId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne<StepExecution>().WithMany().HasForeignKey(x => x.TargetStepExecutionId)
                .OnDelete(DeleteBehavior.Restrict);
        });
        foreach (var type in new[] { typeof(WaferRelease), typeof(WaferCompletion), typeof(StepExecutionInheritance) })
            foreach (var property in b.Model.FindEntityType(type)!.GetProperties())
                property.SetColumnName(Regex.Replace(property.Name, "(?<!^)(?=[A-Z])", "_").ToLowerInvariant());
    }
}
