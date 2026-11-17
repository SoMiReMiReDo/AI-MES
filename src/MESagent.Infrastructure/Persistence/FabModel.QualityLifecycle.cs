using MESagent.Domain.Entities;
using MESagent.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;

namespace MESagent.Infrastructure.Persistence;

internal static partial class FabModel
{
    private static void ConfigureQualityLifecycle(ModelBuilder b)
    {
        b.Entity<Issue>().Property(x => x.IsBlocking).HasColumnName("is_blocking");
        b.Entity<SpecificationVersion>().Property(x => x.ExpectedBoolean).HasColumnName("expected_boolean");
        b.Entity<SpecificationVersion>().Property(x => x.ExpectedText).HasColumnName("expected_text");
        b.Entity<QualityCheckTemplate>(e =>
        {
            e.Property(x => x.LifecycleStatus).HasColumnName("lifecycle_status").HasMaxLength(32)
                .HasDefaultValue("Draft");
            e.Property(x => x.ReleasedAt).HasColumnName("released_at");
            e.Property(x => x.ReleasedById).HasColumnName("released_by_id");
            e.HasOne<MesUser>().WithMany().HasForeignKey(x => x.ReleasedById).OnDelete(DeleteBehavior.Restrict);
        });
        b.Entity<QualityCheck>(e =>
        {
            e.Property(x => x.IssueId).HasColumnName("issue_id");
            e.HasOne<Issue>().WithMany().HasForeignKey(x => x.IssueId).OnDelete(DeleteBehavior.Restrict);
        });
        b.Entity<LotRelease>(e =>
        {
            e.Property(x => x.RecoveryAction).HasColumnName("recovery_action").HasMaxLength(32)
                .HasDefaultValue("None");
            e.Property(x => x.StepExecutionId).HasColumnName("step_execution_id");
            e.Property(x => x.ProcessRunId).HasColumnName("process_run_id");
            e.Property(x => x.CommandId).HasColumnName("command_id");
            e.HasOne<StepExecution>().WithMany().HasForeignKey(x => x.StepExecutionId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne<ProcessRun>().WithMany().HasForeignKey(x => x.ProcessRunId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => x.CommandId).IsUnique().HasFilter("command_id IS NOT NULL");
        });
    }
}
