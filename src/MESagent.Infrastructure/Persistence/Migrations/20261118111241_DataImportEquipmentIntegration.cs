using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MESagent.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DataImportEquipmentIntegration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_machine_connections_equipment_id",
                schema: "integration",
                table: "machine_connections");

            migrationBuilder.DropIndex(
                name: "IX_equipment_events_equipment_id_source_source_event_id",
                schema: "equipment",
                table: "equipment_events");

            migrationBuilder.DropIndex(
                name: "IX_equipment_events_machine_connection_id",
                schema: "equipment",
                table: "equipment_events");

            migrationBuilder.AddColumn<Guid>(
                name: "equipment_event_id",
                schema: "integration",
                table: "machine_messages",
                type: "uuid",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "file_path",
                schema: "integration",
                table: "import_jobs",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<Guid>(
                name: "fab_id",
                schema: "integration",
                table: "import_jobs",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "input_data",
                schema: "integration",
                table: "import_jobs",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "result_data",
                schema: "integration",
                table: "import_jobs",
                type: "jsonb",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_machine_messages_equipment_event_id",
                schema: "integration",
                table: "machine_messages",
                column: "equipment_event_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_machine_messages_machine_connection_id_received_at_id",
                schema: "integration",
                table: "machine_messages",
                columns: new[] { "machine_connection_id", "received_at", "id" });

            migrationBuilder.CreateIndex(
                name: "IX_machine_connections_equipment_id_name",
                schema: "integration",
                table: "machine_connections",
                columns: new[] { "equipment_id", "name" },
                unique: true,
                filter: "protocol = 'Simulator'");

            migrationBuilder.CreateIndex(
                name: "IX_import_jobs_fab_id_user_id_created_at_id",
                schema: "integration",
                table: "import_jobs",
                columns: new[] { "fab_id", "user_id", "created_at", "id" });

            migrationBuilder.CreateIndex(
                name: "IX_equipment_events_equipment_id_source_source_event_id",
                schema: "equipment",
                table: "equipment_events",
                columns: new[] { "equipment_id", "source", "source_event_id" },
                unique: true,
                filter: "source_event_id IS NOT NULL AND machine_connection_id IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_equipment_events_machine_connection_id_source_source_event_~",
                schema: "equipment",
                table: "equipment_events",
                columns: new[] { "machine_connection_id", "source", "source_event_id" },
                unique: true,
                filter: "source_event_id IS NOT NULL AND machine_connection_id IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_import_jobs_fabs_fab_id",
                schema: "integration",
                table: "import_jobs",
                column: "fab_id",
                principalSchema: "organization",
                principalTable: "fabs",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_machine_messages_equipment_events_equipment_event_id",
                schema: "integration",
                table: "machine_messages",
                column: "equipment_event_id",
                principalSchema: "equipment",
                principalTable: "equipment_events",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.Sql("""
                CREATE TRIGGER immutable_simulator_message BEFORE UPDATE OR DELETE ON integration.machine_messages
                    FOR EACH ROW WHEN (OLD.topic = 'simulator') EXECUTE FUNCTION audit.reject_history_mutation();

                INSERT INTO security.permissions(code,name,is_active)
                SELECT code,code,true FROM unnest(ARRAY['integration.simulator.read','integration.work-orders.import']) code
                ON CONFLICT(code) DO NOTHING;
                INSERT INTO security.role_permissions(role_id,permission_id)
                SELECT r.id,p.id FROM security.roles r CROSS JOIN security.permissions p
                WHERE r.name='SystemAdmin' AND r.is_system
                    AND p.code IN ('integration.simulator.read','integration.work-orders.import')
                ON CONFLICT(role_id,permission_id) DO NOTHING;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DO $$ BEGIN
                    IF EXISTS (SELECT FROM integration.import_jobs WHERE input_data IS NOT NULL)
                        OR EXISTS (SELECT FROM integration.machine_connections WHERE protocol = 'Simulator') THEN
                        RAISE EXCEPTION 'Cannot downgrade while integration records exist';
                    END IF;
                END $$;
                DROP TRIGGER immutable_simulator_message ON integration.machine_messages;
                DELETE FROM security.user_permissions WHERE permission_id IN (SELECT id FROM security.permissions
                    WHERE code IN ('integration.simulator.read','integration.work-orders.import'));
                DELETE FROM security.role_permissions WHERE permission_id IN (SELECT id FROM security.permissions
                    WHERE code IN ('integration.simulator.read','integration.work-orders.import'));
                DELETE FROM security.permissions WHERE code IN ('integration.simulator.read','integration.work-orders.import');
                """);

            migrationBuilder.DropForeignKey(
                name: "FK_import_jobs_fabs_fab_id",
                schema: "integration",
                table: "import_jobs");

            migrationBuilder.DropForeignKey(
                name: "FK_machine_messages_equipment_events_equipment_event_id",
                schema: "integration",
                table: "machine_messages");

            migrationBuilder.DropIndex(
                name: "IX_machine_messages_equipment_event_id",
                schema: "integration",
                table: "machine_messages");

            migrationBuilder.DropIndex(
                name: "IX_machine_messages_machine_connection_id_received_at_id",
                schema: "integration",
                table: "machine_messages");

            migrationBuilder.DropIndex(
                name: "IX_machine_connections_equipment_id_name",
                schema: "integration",
                table: "machine_connections");

            migrationBuilder.DropIndex(
                name: "IX_import_jobs_fab_id_user_id_created_at_id",
                schema: "integration",
                table: "import_jobs");

            migrationBuilder.DropIndex(
                name: "IX_equipment_events_equipment_id_source_source_event_id",
                schema: "equipment",
                table: "equipment_events");

            migrationBuilder.DropIndex(
                name: "IX_equipment_events_machine_connection_id_source_source_event_~",
                schema: "equipment",
                table: "equipment_events");

            migrationBuilder.DropColumn(
                name: "equipment_event_id",
                schema: "integration",
                table: "machine_messages");

            migrationBuilder.DropColumn(
                name: "fab_id",
                schema: "integration",
                table: "import_jobs");

            migrationBuilder.DropColumn(
                name: "input_data",
                schema: "integration",
                table: "import_jobs");

            migrationBuilder.DropColumn(
                name: "result_data",
                schema: "integration",
                table: "import_jobs");

            migrationBuilder.AlterColumn<string>(
                name: "file_path",
                schema: "integration",
                table: "import_jobs",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_machine_connections_equipment_id",
                schema: "integration",
                table: "machine_connections",
                column: "equipment_id");

            migrationBuilder.CreateIndex(
                name: "IX_equipment_events_equipment_id_source_source_event_id",
                schema: "equipment",
                table: "equipment_events",
                columns: new[] { "equipment_id", "source", "source_event_id" },
                unique: true,
                filter: "source_event_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_equipment_events_machine_connection_id",
                schema: "equipment",
                table: "equipment_events",
                column: "machine_connection_id");
        }
    }
}
