using Microsoft.EntityFrameworkCore;

namespace MESagent.Infrastructure.Persistence;

internal static partial class FabModel
{
    public static void Configure(ModelBuilder modelBuilder)
    {
        ConfigureAudit(modelBuilder);
        ConfigureEngineering(modelBuilder);
        ConfigureEquipment(modelBuilder);
        ConfigureIntegration(modelBuilder);
        ConfigureMaterials(modelBuilder);
        ConfigureOrganization(modelBuilder);
        ConfigureProduction(modelBuilder);
        ConfigureQuality(modelBuilder);
        ConfigureSecurity(modelBuilder);
        ConfigureIntegrity(modelBuilder);
    }
}
