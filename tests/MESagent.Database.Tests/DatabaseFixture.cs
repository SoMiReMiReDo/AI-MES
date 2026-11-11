using MESagent.Domain.Entities;
using MESagent.Infrastructure.Identity;
using MESagent.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace MESagent.Database.Tests;

public sealed class DatabaseFixture : IAsyncLifetime
{
    private string adminConnection = null!;
    private readonly string databaseName = "mesagent_test_" + Guid.NewGuid().ToString("N");
    public string ConnectionString { get; private set; } = null!;

    public MesDbContext CreateContext() => MesDbContextFactory.Create(ConnectionString);

    public async Task InitializeAsync()
    {
        adminConnection = Environment.GetEnvironmentVariable("MES_TEST_ADMIN_CONNECTION")
            ?? throw new InvalidOperationException(
                "真实 PostgreSQL 测试需要 MES_TEST_ADMIN_CONNECTION；运行 python3 scripts/database.py test。");
        var builder = new NpgsqlConnectionStringBuilder(adminConnection) { Database = "postgres" };
        await using var admin = new NpgsqlConnection(builder.ConnectionString);
        await admin.OpenAsync();

        // Name is generated exclusively by this fixture, never provided by a test/user.
        await using var create = new NpgsqlCommand($"CREATE DATABASE {databaseName}", admin);
        await create.ExecuteNonQueryAsync();
        builder.Database = databaseName;
        ConnectionString = builder.ConnectionString;

        await using var db = CreateContext();
        await db.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        if (adminConnection is null)
            return;

        NpgsqlConnection.ClearAllPools();
        var builder = new NpgsqlConnectionStringBuilder(adminConnection) { Database = "postgres" };
        await using var admin = new NpgsqlConnection(builder.ConnectionString);
        await admin.OpenAsync();
        await using var drop = new NpgsqlCommand($"DROP DATABASE IF EXISTS {databaseName} WITH (FORCE)", admin);
        await drop.ExecuteNonQueryAsync();
    }

    public async Task<Seed> SeedAsync()
    {
        await using var db = CreateContext();
        var suffix = Guid.NewGuid().ToString("N");
        var user = new MesUser
        {
            Id = Guid.NewGuid(),
            UserName = suffix,
            NormalizedUserName = suffix.ToUpperInvariant(),
            Name = "Test operator"
        };
        var fab = new Fab { Code = suffix, Name = "Test Fab", Timezone = "Asia/Shanghai" };
        var area = new Area { Code = suffix, Name = "Test Area", FabId = fab.Id };
        var group = new EquipmentGroup { Code = suffix, Name = "Test Group", FabId = fab.Id };
        var type = new EquipmentType { Code = suffix, Name = "Test Type" };
        var equipment = new Equipment
        {
            Code = suffix,
            Name = "Test Equipment",
            AreaId = area.Id,
            EquipmentGroupId = group.Id,
            EquipmentTypeId = type.Id,
            OperationalState = "Idle"
        };

        var product = new Product { Code = suffix, Name = "Test Product", UnitOfMeasure = "wafer" };
        var route = new Route { Code = suffix, Name = "Test Route", ProductId = product.Id };
        var routeVersion = new RouteVersion { RouteId = route.Id, Version = "1", LifecycleStatus = "Draft" };
        var revision = new ProductRevision
        {
            ProductId = product.Id,
            RevisionCode = "1",
            LifecycleStatus = "Draft",
            RouteVersionId = routeVersion.Id
        };
        var operation = new Operation { Code = suffix, Name = "Test Operation", SegmentType = "Process" };
        var step = new RouteStep
        {
            RouteVersionId = routeVersion.Id,
            OperationId = operation.Id,
            SequenceNo = 1,
            Name = "Test step"
        };
        var recipe = new Recipe { Code = suffix, Name = "Test Recipe" };
        var recipeVersion = new RecipeVersion { RecipeId = recipe.Id, Version = "1", LifecycleStatus = "Draft" };

        var lot = new Lot
        {
            FabId = fab.Id,
            LotNumber = suffix,
            LotAssignedAt = DateTime.UtcNow,
            ProductRevisionId = revision.Id,
            RouteVersionId = routeVersion.Id,
            CurrentRouteStepId = step.Id,
            LotType = "Production",
            Status = "Ready",
            TargetQty = 1
        };
        var otherLot = new Lot
        {
            FabId = fab.Id,
            LotNumber = suffix + "B",
            LotAssignedAt = DateTime.UtcNow,
            ProductRevisionId = revision.Id,
            RouteVersionId = routeVersion.Id,
            CurrentRouteStepId = step.Id,
            LotType = "Production",
            Status = "Ready",
            TargetQty = 1
        };
        var wafer = new Wafer { WaferCode = suffix, WaferSizeMm = 300, LifecycleStatus = "Active" };
        var evt = new GenealogyEvent
        {
            EventType = "Create",
            Reason = "Test",
            OccurredAt = DateTime.UtcNow,
            PerformedById = user.Id,
            IdempotencyKey = suffix,
            CorrelationId = Guid.NewGuid()
        };
        var reason = new ReasonCode { Code = suffix, Name = "Test reason", Category = "Hold" };
        var execution = new StepExecution
        {
            LotId = lot.Id,
            RouteVersionId = routeVersion.Id,
            RouteStepId = step.Id,
            VisitNo = 1,
            Status = "Active",
            Name = "Test step",
            EnteredAt = DateTime.UtcNow
        };
        db.AddRange(
            user,
            fab,
            area,
            group,
            type,
            equipment,
            product,
            route,
            routeVersion,
            revision,
            operation,
            step,
            recipe,
            recipeVersion,
            lot,
            otherLot,
            wafer,
            evt,
            reason,
            execution);
        await db.SaveChangesAsync();

        return new Seed(
            user.Id,
            fab.Id,
            equipment.Id,
            product.Id,
            routeVersion.Id,
            step.Id,
            recipeVersion.Id,
            lot.Id,
            otherLot.Id,
            wafer.Id,
            evt.Id,
            reason.Id,
            execution.Id);
    }
}

public sealed record Seed(
    Guid User,
    Guid Fab,
    Guid Equipment,
    Guid Product,
    Guid RouteVersion,
    Guid Step,
    Guid RecipeVersion,
    Guid Lot,
    Guid OtherLot,
    Guid Wafer,
    Guid Event,
    Guid Reason,
    Guid Execution);
