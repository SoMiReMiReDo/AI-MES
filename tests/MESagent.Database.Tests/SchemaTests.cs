using MESagent.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using Xunit;

namespace MESagent.Database.Tests;

public sealed class SchemaTests(DatabaseFixture fixture) : IClassFixture<DatabaseFixture>
{
    private static async Task Reject(Func<Task> action, params string[] states)
    {
        var ex = await Assert.ThrowsAsync<DbUpdateException>(action);
        var pg = Assert.IsType<PostgresException>(ex.InnerException);
        Assert.Contains(pg.SqlState, states);
    }

    [Fact]
    public async Task Fresh_database_has_all_tables_and_repeat_migration_is_safe()
    {
        await using var db = fixture.CreateContext();
        Assert.Equal(123, db.Model.GetEntityTypes().Count());
        Assert.False(db.Database.HasPendingModelChanges());

        await db.Database.MigrateAsync();
        Assert.Empty(await db.Database.GetPendingMigrationsAsync());
        Assert.Equal(db.Database.GetMigrations().Count(), (await db.Database.GetAppliedMigrationsAsync()).Count());
        Assert.All(db.Model.GetEntityTypes(), e => Assert.DoesNotContain(e.GetProperties(), p => p.IsShadowProperty()));
    }

    [Fact]
    public async Task Missing_foreign_key_is_rejected()
    {
        await using var db = fixture.CreateContext();
        db.Add(new Area { Code = Guid.NewGuid().ToString(), Name = "Invalid", FabId = Guid.NewGuid() });
        await Reject(() => db.SaveChangesAsync(), "23503");
    }

    [Fact]
    public async Task Negative_quantity_is_rejected()
    {
        var s = await fixture.SeedAsync();
        await using var db = fixture.CreateContext();
        var lot = await db.Set<Lot>().FindAsync(s.Lot);
        lot!.TargetQty = -1;
        await Reject(() => db.SaveChangesAsync(), "23514");
    }

    private static LotMembership Membership(Seed s, Guid lot, DateTime start, DateTime? end = null) =>
        new() { WaferId = s.Wafer, LotId = lot, ChangeEventId = s.Event, ValidFrom = start, ValidTo = end };

    [Fact]
    public async Task Wafer_cannot_belong_to_two_current_lots()
    {
        var s = await fixture.SeedAsync();
        await using var db = fixture.CreateContext();
        db.Add(Membership(s, s.Lot, DateTime.UtcNow));
        await db.SaveChangesAsync();
        db.Add(Membership(s, s.OtherLot, DateTime.UtcNow));
        await Reject(() => db.SaveChangesAsync(), "23505", "23P01");
    }

    [Fact]
    public async Task Historical_membership_overlap_is_rejected_but_adjacent_transfer_is_valid()
    {
        var s = await fixture.SeedAsync();
        var start = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        await using var db = fixture.CreateContext();
        db.Add(Membership(s, s.Lot, start, start.AddHours(1)));
        db.Add(Membership(s, s.OtherLot, start.AddHours(1), start.AddHours(2)));
        await db.SaveChangesAsync();
        db.Add(Membership(s, s.Lot, start.AddMinutes(30), start.AddMinutes(90)));
        await Reject(() => db.SaveChangesAsync(), "23P01");
    }

    [Fact]
    public async Task Carrier_slot_cannot_exceed_capacity_or_have_overlapping_occupants()
    {
        var s = await fixture.SeedAsync();
        await using var db = fixture.CreateContext();

        var carrier = new Carrier
        {
            FabId = s.Fab,
            CarrierCode = Guid.NewGuid().ToString(),
            CarrierType = "FOUP",
            SlotCapacity = 1,
            Status = "Available"
        };
        db.Add(carrier);
        await db.SaveChangesAsync();

        var invalid = new CarrierSlot { CarrierId = carrier.Id, SlotNo = 2 };
        db.Add(invalid);
        await Reject(() => db.SaveChangesAsync(), "23514");

        db.Entry(invalid).State = EntityState.Detached;
        var slot = new CarrierSlot { CarrierId = carrier.Id, SlotNo = 1 };
        db.Add(slot);
        db.Add(new CarrierLoad { CarrierSlotId = slot.Id, WaferId = s.Wafer, LoadedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var otherWafer = new Wafer
        {
            WaferCode = Guid.NewGuid().ToString(),
            WaferSizeMm = 300,
            LifecycleStatus = "Active"
        };
        db.Add(otherWafer);
        db.Add(new CarrierLoad { CarrierSlotId = slot.Id, WaferId = otherWafer.Id, LoadedAt = DateTime.UtcNow });
        await Reject(() => db.SaveChangesAsync(), "23505", "23P01");
    }

    [Fact]
    public async Task Lot_cannot_point_to_step_from_different_route_version()
    {
        var s = await fixture.SeedAsync();
        var other = await fixture.SeedAsync();
        await using var db = fixture.CreateContext();
        var lot = await db.Set<Lot>().FindAsync(s.Lot);
        lot!.CurrentRouteStepId = other.Step;
        await Reject(() => db.SaveChangesAsync(), "23503");
    }

    [Fact]
    public async Task Multiple_holds_remain_independent()
    {
        var s = await fixture.SeedAsync();
        await using var db = fixture.CreateContext();
        var a = new HoldRecord
        {
            LotId = s.Lot,
            ReasonCodeId = s.Reason,
            HoldScope = "All",
            HeldAt = DateTime.UtcNow,
            HeldById = s.User
        };
        var b = new HoldRecord
        {
            LotId = s.Lot,
            ReasonCodeId = s.Reason,
            HoldScope = "All",
            HeldAt = DateTime.UtcNow,
            HeldById = s.User
        };
        db.AddRange(a, b);
        await db.SaveChangesAsync();

        a.ReleasedAt = DateTime.UtcNow;
        a.ReleasedById = s.User;
        a.ReleaseReason = "Reviewed";
        await db.SaveChangesAsync();
        Assert.Equal(1, await db.Set<HoldRecord>().CountAsync(x => x.LotId == s.Lot && x.ReleasedAt == null));
    }

    private static ProcessRun Run(Seed s) => new()
    {
        StepExecutionId = s.Execution,
        EquipmentId = s.Equipment,
        RecipeVersionId = s.RecipeVersion,
        TrackInAt = DateTime.UtcNow,
        TrackInById = s.User,
        Status = "Running"
    };

    [Fact]
    public async Task Equipment_cannot_run_two_lots_concurrently()
    {
        var s = await fixture.SeedAsync();
        var other = await fixture.SeedAsync();
        await using var db = fixture.CreateContext();
        db.Add(Run(s));
        await db.SaveChangesAsync();
        var second = Run(other);
        second.EquipmentId = s.Equipment;
        db.Add(second);
        await Reject(() => db.SaveChangesAsync(), "23505");
    }

    [Fact]
    public async Task Track_event_idempotency_key_cannot_be_reused()
    {
        var s = await fixture.SeedAsync();
        await using var db = fixture.CreateContext();
        var run = Run(s);
        db.Add(run);
        var key = Guid.NewGuid().ToString();
        TrackEvent Event() => new()
        {
            ProcessRunId = run.Id,
            EventType = "TrackIn",
            OccurredAt = DateTime.UtcNow,
            RecordedAt = DateTime.UtcNow,
            ActorId = s.User,
            Source = "Test",
            CorrelationId = Guid.NewGuid(),
            IdempotencyKey = key
        };
        db.Add(Event());
        await db.SaveChangesAsync();
        db.Add(Event());
        await Reject(() => db.SaveChangesAsync(), "23505");
    }

    [Fact]
    public async Task Xmin_detects_stale_write()
    {
        var s = await fixture.SeedAsync();
        await using var first = fixture.CreateContext();
        await using var second = fixture.CreateContext();
        var a = await first.Set<Lot>().FindAsync(s.Lot);
        var b = await second.Set<Lot>().FindAsync(s.Lot);
        a!.Priority = 1;
        await first.SaveChangesAsync();
        b!.Priority = 2;
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => second.SaveChangesAsync());
    }

    [Fact]
    public async Task Rolled_back_membership_never_becomes_visible()
    {
        var s = await fixture.SeedAsync();
        await using (var db = fixture.CreateContext())
        {
            await using var transaction = await db.Database.BeginTransactionAsync();
            db.Add(Membership(s, s.Lot, DateTime.UtcNow));
            await db.SaveChangesAsync();
            await transaction.RollbackAsync();
        }
        await using var verify = fixture.CreateContext();
        Assert.False(await verify.Set<LotMembership>().AnyAsync(x => x.WaferId == s.Wafer));
    }

    [Fact]
    public async Task Released_route_content_and_steps_cannot_be_changed()
    {
        var s = await fixture.SeedAsync();
        await using var db = fixture.CreateContext();
        var version = await db.Set<RouteVersion>().FindAsync(s.RouteVersion);
        version!.LifecycleStatus = "Released";
        version.ReleasedAt = DateTime.UtcNow;
        version.ReleasedById = s.User;
        await db.SaveChangesAsync();

        version.Version = "2";
        await Reject(() => db.SaveChangesAsync(), "23514");
        db.Entry(version).State = EntityState.Detached;
        var step = await db.Set<RouteStep>().FindAsync(s.Step);
        step!.Name = "Changed";
        await Reject(() => db.SaveChangesAsync(), "23514");
    }

    [Fact]
    public async Task Audit_history_cannot_be_updated_or_deleted()
    {
        var s = await fixture.SeedAsync();
        await using var db = fixture.CreateContext();
        var audit = new AuditLog
        {
            UserId = s.User,
            EntityType = "Lot",
            EntityId = s.Lot,
            Action = "Create",
            OccurredAt = DateTime.UtcNow,
            CorrelationId = Guid.NewGuid(),
            SourceChannel = "Test"
        };
        db.Add(audit);
        await db.SaveChangesAsync();

        audit.Action = "Changed";
        await Reject(() => db.SaveChangesAsync(), "23514");
        db.Entry(audit).State = EntityState.Unchanged;
        db.Remove(audit);
        await Reject(() => db.SaveChangesAsync(), "23514");
    }

    [Fact]
    public async Task Product_stock_is_supported_and_duplicate_null_lot_balance_is_rejected()
    {
        var s = await fixture.SeedAsync();
        await using var db = fixture.CreateContext();
        var warehouse = new Warehouse
        {
            FabId = s.Fab,
            Code = Guid.NewGuid().ToString(),
            Name = "Test stock",
            Kind = "Finished"
        };
        db.Add(warehouse);
        db.Add(new WarehouseStock
        {
            WarehouseId = warehouse.Id,
            ProductId = s.Product,
            Quantity = 1,
            UnitOfMeasure = "wafer"
        });
        await db.SaveChangesAsync();
        db.Add(new WarehouseStock
        {
            WarehouseId = warehouse.Id,
            ProductId = s.Product,
            Quantity = 2,
            UnitOfMeasure = "wafer"
        });
        await Reject(() => db.SaveChangesAsync(), "23505");
    }

    [Fact]
    public async Task Lot_quantity_view_follows_membership_transfer_without_double_counting()
    {
        var s = await fixture.SeedAsync();
        var start = DateTime.UtcNow.AddMinutes(-1);
        await using var db = fixture.CreateContext();
        db.Add(Membership(s, s.Lot, start, start.AddSeconds(30)));
        db.Add(Membership(s, s.OtherLot, start.AddSeconds(30)));
        await db.SaveChangesAsync();
        var first = await db.Database.SqlQuery<long>(
            $"SELECT current_wafer_count AS \"Value\" FROM production.lot_quantities WHERE lot_id = {s.Lot}")
            .SingleAsync();
        var second = await db.Database.SqlQuery<long>(
            $"SELECT current_wafer_count AS \"Value\" FROM production.lot_quantities WHERE lot_id = {s.OtherLot}")
            .SingleAsync();
        Assert.Equal(0, first);
        Assert.Equal(1, second);
    }

    [Fact]
    public async Task Schema_migrations_can_be_rolled_back_and_reapplied_on_disposable_database()
    {
        var scratch = new DatabaseFixture();
        try
        {
            await scratch.InitializeAsync();
            await using var db = scratch.CreateContext();
            await db.GetService<IMigrator>().MigrateAsync("0");
            Assert.Empty(await db.Database.GetAppliedMigrationsAsync());
            await db.Database.MigrateAsync();
            Assert.Equal(db.Database.GetMigrations().Count(), (await db.Database.GetAppliedMigrationsAsync()).Count());
        }
        finally
        {
            await scratch.DisposeAsync();
        }
    }
}
