using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace MESagent.Infrastructure.Persistence.Migrations;

// Data-only migration: the EF model snapshot does not change.
[DbContext(typeof(MesDbContext))]
[Migration("20261111120000_ManufacturingPermissions")]
public sealed class ManufacturingPermissions : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            INSERT INTO security.permissions(code, name, is_active)
            SELECT code, code, true FROM unnest(ARRAY[
                'engineering.read','engineering.manage','engineering.release',
                'engineering.numbers.allocate','engineering.documents.manage',
                'planning.work-orders.read','planning.work-orders.manage',
                'planning.work-orders.release','planning.work-orders.cancel','planning.work-orders.close'
            ]) AS code ON CONFLICT (code) DO NOTHING;
            INSERT INTO security.role_permissions(role_id, permission_id)
            SELECT r.id, p.id FROM security.roles r CROSS JOIN security.permissions p
            WHERE r.name = 'SystemAdmin' AND r.is_system
                AND (p.code LIKE 'engineering.%' OR p.code LIKE 'planning.work-orders.%')
            ON CONFLICT (role_id, permission_id) DO NOTHING;
            CREATE FUNCTION production.protect_work_order_snapshot() RETURNS trigger LANGUAGE plpgsql AS $$
            BEGIN
                RAISE EXCEPTION 'work order snapshots are immutable' USING ERRCODE = '23514';
            END $$;
            CREATE TRIGGER protect_work_order_snapshot BEFORE UPDATE OR DELETE ON production.work_order_snapshots
                FOR EACH ROW EXECUTE FUNCTION production.protect_work_order_snapshot();
            """);
    }
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DROP TRIGGER protect_work_order_snapshot ON production.work_order_snapshots;
            DROP FUNCTION production.protect_work_order_snapshot();
            DELETE FROM security.role_permissions WHERE permission_id IN
                (SELECT id FROM security.permissions WHERE code LIKE 'engineering.%' OR code LIKE 'planning.work-orders.%');
            DELETE FROM security.user_permissions WHERE permission_id IN
                (SELECT id FROM security.permissions WHERE code LIKE 'engineering.%' OR code LIKE 'planning.work-orders.%');
            DELETE FROM security.permissions WHERE code LIKE 'engineering.%' OR code LIKE 'planning.work-orders.%';
            """);
    }
}
