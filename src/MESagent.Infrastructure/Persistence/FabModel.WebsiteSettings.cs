using MESagent.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MESagent.Infrastructure.Persistence;

internal static partial class FabModel
{
    private static void ConfigureWebsiteSettings(ModelBuilder model)
    {
        model.Entity<SiteAppearance>(e =>
        {
            e.ToTable("site_appearance", "settings", t =>
            {
                t.HasCheckConstraint("ck_site_appearance_singleton", $"id = '{SiteAppearance.SingletonId}'::uuid");
                t.HasCheckConstraint("ck_site_appearance_name", "char_length(btrim(name)) BETWEEN 1 AND 60");
                t.HasCheckConstraint("ck_site_appearance_theme", "default_theme IN ('light', 'dark')");
            });
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
            e.Property(x => x.Name).HasColumnName("name").HasMaxLength(60);
            e.Property(x => x.Subtitle).HasColumnName("subtitle").HasMaxLength(100);
            e.Property(x => x.DefaultTheme).HasColumnName("default_theme").HasMaxLength(5);
            e.Property(x => x.Version).HasColumnName("version").IsConcurrencyToken();
            e.HasData(new SiteAppearance());
        });
        model.Entity<FabSchedulePreference>(e =>
        {
            e.ToTable("fab_schedule_preferences", "settings", t =>
                t.HasCheckConstraint("ck_fab_schedule_view", "default_view IN ('day', 'week')"));
            e.HasKey(x => x.FabId);
            e.HasOne<Fab>().WithMany().HasForeignKey(x => x.FabId).OnDelete(DeleteBehavior.Restrict);
            e.Property(x => x.FabId).HasColumnName("fab_id").ValueGeneratedNever();
            e.Property(x => x.DefaultView).HasColumnName("default_view").HasMaxLength(4);
            e.Property(x => x.Version).HasColumnName("version").IsConcurrencyToken();
        });
    }
}
