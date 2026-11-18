using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MESagent.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class EquipmentMaintenanceResources : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_reticle_usages_reticle_id",
                schema: "equipment",
                table: "reticle_usages");

            migrationBuilder.DropIndex(
                name: "IX_material_mounts_equipment_id",
                schema: "equipment",
                table: "material_mounts");

            migrationBuilder.DropIndex(
                name: "IX_maintenance_events_schedule_id",
                schema: "equipment",
                table: "maintenance_events");

            migrationBuilder.AddColumn<Guid>(
                name: "equipment_id",
                schema: "equipment",
                table: "tools",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "fab_id",
                schema: "equipment",
                table: "tools",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "fab_id",
                schema: "equipment",
                table: "reticles",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "max_usage_count",
                schema: "equipment",
                table: "reticles",
                type: "bigint",
                nullable: false,
                defaultValue: 1000L);

            migrationBuilder.AddColumn<DateTime>(
                name: "usable_until",
                schema: "equipment",
                table: "reticles",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "reticle_mount_id",
                schema: "equipment",
                table: "reticle_usages",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "material_id",
                schema: "equipment",
                table: "material_mounts",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "resource_expiry_at",
                schema: "materials",
                table: "material_lots",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "released_at",
                schema: "equipment",
                table: "maintenance_events",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "released_by_id",
                schema: "equipment",
                table: "maintenance_events",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "processing_status",
                schema: "equipment",
                table: "equipment_events",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "Applied");

            migrationBuilder.AddColumn<string>(
                name: "rejection_reason",
                schema: "equipment",
                table: "equipment_events",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "source",
                schema: "equipment",
                table: "equipment_events",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "Manual");

            migrationBuilder.AddColumn<string>(
                name: "source_event_id",
                schema: "equipment",
                table: "equipment_events",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "communication_state",
                schema: "equipment",
                table: "equipment",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "Unknown");

            migrationBuilder.AddColumn<Guid>(
                name: "equipment_state_id",
                schema: "equipment",
                table: "downtimes",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "material_requirements",
                schema: "equipment",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    operation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    material_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_material_requirements", x => x.id);
                    table.ForeignKey(
                        name: "FK_material_requirements_materials_material_id",
                        column: x => x.material_id,
                        principalSchema: "materials",
                        principalTable: "materials",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_material_requirements_operations_operation_id",
                        column: x => x.operation_id,
                        principalSchema: "engineering",
                        principalTable: "operations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "material_run_usages",
                schema: "equipment",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    material_mount_id = table.Column<Guid>(type: "uuid", nullable: false),
                    process_run_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_material_run_usages", x => x.id);
                    table.ForeignKey(
                        name: "FK_material_run_usages_material_mounts_material_mount_id",
                        column: x => x.material_mount_id,
                        principalSchema: "equipment",
                        principalTable: "material_mounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_material_run_usages_process_runs_process_run_id",
                        column: x => x.process_run_id,
                        principalSchema: "production",
                        principalTable: "process_runs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "operation_policies",
                schema: "equipment",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    operation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    require_equipment_qualification = table.Column<bool>(type: "boolean", nullable: false),
                    require_worker_qualification = table.Column<bool>(type: "boolean", nullable: false),
                    required_reticle_layer = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_operation_policies", x => x.id);
                    table.ForeignKey(
                        name: "FK_operation_policies_operations_operation_id",
                        column: x => x.operation_id,
                        principalSchema: "engineering",
                        principalTable: "operations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "reticle_mounts",
                schema: "equipment",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    equipment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reticle_id = table.Column<Guid>(type: "uuid", nullable: false),
                    loaded_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    unloaded_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_reticle_mounts", x => x.id);
                    table.CheckConstraint("ck_reticle_mount_time", "unloaded_at IS NULL OR unloaded_at >= loaded_at");
                    table.ForeignKey(
                        name: "FK_reticle_mounts_equipment_equipment_id",
                        column: x => x.equipment_id,
                        principalSchema: "equipment",
                        principalTable: "equipment",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_reticle_mounts_reticles_reticle_id",
                        column: x => x.reticle_id,
                        principalSchema: "equipment",
                        principalTable: "reticles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_tools_equipment_id",
                schema: "equipment",
                table: "tools",
                column: "equipment_id");

            migrationBuilder.CreateIndex(
                name: "IX_tools_fab_id",
                schema: "equipment",
                table: "tools",
                column: "fab_id");

            migrationBuilder.CreateIndex(
                name: "IX_reticles_fab_id",
                schema: "equipment",
                table: "reticles",
                column: "fab_id");

            migrationBuilder.AddCheckConstraint(
                name: "ck_reticle_life",
                schema: "equipment",
                table: "reticles",
                sql: "usage_count >= 0 AND max_usage_count > 0");

            migrationBuilder.CreateIndex(
                name: "IX_reticle_usages_reticle_id_process_run_id",
                schema: "equipment",
                table: "reticle_usages",
                columns: new[] { "reticle_id", "process_run_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_reticle_usages_reticle_mount_id",
                schema: "equipment",
                table: "reticle_usages",
                column: "reticle_mount_id");

            migrationBuilder.CreateIndex(
                name: "IX_material_mounts_equipment_id_material_id",
                schema: "equipment",
                table: "material_mounts",
                columns: new[] { "equipment_id", "material_id" },
                unique: true,
                filter: "unmounted_at IS NULL AND material_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_material_mounts_material_id",
                schema: "equipment",
                table: "material_mounts",
                column: "material_id");

            migrationBuilder.CreateIndex(
                name: "IX_maintenance_events_released_by_id",
                schema: "equipment",
                table: "maintenance_events",
                column: "released_by_id");

            migrationBuilder.CreateIndex(
                name: "IX_maintenance_events_schedule_id",
                schema: "equipment",
                table: "maintenance_events",
                column: "schedule_id",
                unique: true,
                filter: "schedule_id IS NOT NULL AND status NOT IN ('Released','Cancelled')");

            migrationBuilder.CreateIndex(
                name: "IX_equipment_events_equipment_id_source_source_event_id",
                schema: "equipment",
                table: "equipment_events",
                columns: new[] { "equipment_id", "source", "source_event_id" },
                unique: true,
                filter: "source_event_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_downtimes_equipment_state_id",
                schema: "equipment",
                table: "downtimes",
                column: "equipment_state_id",
                unique: true,
                filter: "equipment_state_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_material_requirements_material_id",
                schema: "equipment",
                table: "material_requirements",
                column: "material_id");

            migrationBuilder.CreateIndex(
                name: "IX_material_requirements_operation_id_material_id",
                schema: "equipment",
                table: "material_requirements",
                columns: new[] { "operation_id", "material_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_material_run_usages_material_mount_id_process_run_id",
                schema: "equipment",
                table: "material_run_usages",
                columns: new[] { "material_mount_id", "process_run_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_material_run_usages_process_run_id",
                schema: "equipment",
                table: "material_run_usages",
                column: "process_run_id");

            migrationBuilder.CreateIndex(
                name: "IX_operation_policies_operation_id",
                schema: "equipment",
                table: "operation_policies",
                column: "operation_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_reticle_mounts_equipment_id",
                schema: "equipment",
                table: "reticle_mounts",
                column: "equipment_id",
                unique: true,
                filter: "unloaded_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_reticle_mounts_reticle_id",
                schema: "equipment",
                table: "reticle_mounts",
                column: "reticle_id",
                unique: true,
                filter: "unloaded_at IS NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_downtimes_equipment_states_equipment_state_id",
                schema: "equipment",
                table: "downtimes",
                column: "equipment_state_id",
                principalSchema: "equipment",
                principalTable: "equipment_states",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_maintenance_events_users_released_by_id",
                schema: "equipment",
                table: "maintenance_events",
                column: "released_by_id",
                principalSchema: "security",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_material_mounts_materials_material_id",
                schema: "equipment",
                table: "material_mounts",
                column: "material_id",
                principalSchema: "materials",
                principalTable: "materials",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_reticle_usages_reticle_mounts_reticle_mount_id",
                schema: "equipment",
                table: "reticle_usages",
                column: "reticle_mount_id",
                principalSchema: "equipment",
                principalTable: "reticle_mounts",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_reticles_fabs_fab_id",
                schema: "equipment",
                table: "reticles",
                column: "fab_id",
                principalSchema: "organization",
                principalTable: "fabs",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_tools_equipment_equipment_id",
                schema: "equipment",
                table: "tools",
                column: "equipment_id",
                principalSchema: "equipment",
                principalTable: "equipment",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_tools_fabs_fab_id",
                schema: "equipment",
                table: "tools",
                column: "fab_id",
                principalSchema: "organization",
                principalTable: "fabs",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
            migrationBuilder.Sql(ReadSql("EquipmentResources.up.sql"));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(ReadSql("EquipmentResources.down.sql"));
            migrationBuilder.DropForeignKey(
                name: "FK_downtimes_equipment_states_equipment_state_id",
                schema: "equipment",
                table: "downtimes");

            migrationBuilder.DropForeignKey(
                name: "FK_maintenance_events_users_released_by_id",
                schema: "equipment",
                table: "maintenance_events");

            migrationBuilder.DropForeignKey(
                name: "FK_material_mounts_materials_material_id",
                schema: "equipment",
                table: "material_mounts");

            migrationBuilder.DropForeignKey(
                name: "FK_reticle_usages_reticle_mounts_reticle_mount_id",
                schema: "equipment",
                table: "reticle_usages");

            migrationBuilder.DropForeignKey(
                name: "FK_reticles_fabs_fab_id",
                schema: "equipment",
                table: "reticles");

            migrationBuilder.DropForeignKey(
                name: "FK_tools_equipment_equipment_id",
                schema: "equipment",
                table: "tools");

            migrationBuilder.DropForeignKey(
                name: "FK_tools_fabs_fab_id",
                schema: "equipment",
                table: "tools");

            migrationBuilder.DropTable(
                name: "material_requirements",
                schema: "equipment");

            migrationBuilder.DropTable(
                name: "material_run_usages",
                schema: "equipment");

            migrationBuilder.DropTable(
                name: "operation_policies",
                schema: "equipment");

            migrationBuilder.DropTable(
                name: "reticle_mounts",
                schema: "equipment");

            migrationBuilder.DropIndex(
                name: "IX_tools_equipment_id",
                schema: "equipment",
                table: "tools");

            migrationBuilder.DropIndex(
                name: "IX_tools_fab_id",
                schema: "equipment",
                table: "tools");

            migrationBuilder.DropIndex(
                name: "IX_reticles_fab_id",
                schema: "equipment",
                table: "reticles");

            migrationBuilder.DropCheckConstraint(
                name: "ck_reticle_life",
                schema: "equipment",
                table: "reticles");

            migrationBuilder.DropIndex(
                name: "IX_reticle_usages_reticle_id_process_run_id",
                schema: "equipment",
                table: "reticle_usages");

            migrationBuilder.DropIndex(
                name: "IX_reticle_usages_reticle_mount_id",
                schema: "equipment",
                table: "reticle_usages");

            migrationBuilder.DropIndex(
                name: "IX_material_mounts_equipment_id_material_id",
                schema: "equipment",
                table: "material_mounts");

            migrationBuilder.DropIndex(
                name: "IX_material_mounts_material_id",
                schema: "equipment",
                table: "material_mounts");

            migrationBuilder.DropIndex(
                name: "IX_maintenance_events_released_by_id",
                schema: "equipment",
                table: "maintenance_events");

            migrationBuilder.DropIndex(
                name: "IX_maintenance_events_schedule_id",
                schema: "equipment",
                table: "maintenance_events");

            migrationBuilder.DropIndex(
                name: "IX_equipment_events_equipment_id_source_source_event_id",
                schema: "equipment",
                table: "equipment_events");

            migrationBuilder.DropIndex(
                name: "IX_downtimes_equipment_state_id",
                schema: "equipment",
                table: "downtimes");

            migrationBuilder.DropColumn(
                name: "equipment_id",
                schema: "equipment",
                table: "tools");

            migrationBuilder.DropColumn(
                name: "fab_id",
                schema: "equipment",
                table: "tools");

            migrationBuilder.DropColumn(
                name: "fab_id",
                schema: "equipment",
                table: "reticles");

            migrationBuilder.DropColumn(
                name: "max_usage_count",
                schema: "equipment",
                table: "reticles");

            migrationBuilder.DropColumn(
                name: "usable_until",
                schema: "equipment",
                table: "reticles");

            migrationBuilder.DropColumn(
                name: "reticle_mount_id",
                schema: "equipment",
                table: "reticle_usages");

            migrationBuilder.DropColumn(
                name: "material_id",
                schema: "equipment",
                table: "material_mounts");

            migrationBuilder.DropColumn(
                name: "resource_expiry_at",
                schema: "materials",
                table: "material_lots");

            migrationBuilder.DropColumn(
                name: "released_at",
                schema: "equipment",
                table: "maintenance_events");

            migrationBuilder.DropColumn(
                name: "released_by_id",
                schema: "equipment",
                table: "maintenance_events");

            migrationBuilder.DropColumn(
                name: "processing_status",
                schema: "equipment",
                table: "equipment_events");

            migrationBuilder.DropColumn(
                name: "rejection_reason",
                schema: "equipment",
                table: "equipment_events");

            migrationBuilder.DropColumn(
                name: "source",
                schema: "equipment",
                table: "equipment_events");

            migrationBuilder.DropColumn(
                name: "source_event_id",
                schema: "equipment",
                table: "equipment_events");

            migrationBuilder.DropColumn(
                name: "communication_state",
                schema: "equipment",
                table: "equipment");

            migrationBuilder.DropColumn(
                name: "equipment_state_id",
                schema: "equipment",
                table: "downtimes");

            migrationBuilder.CreateIndex(
                name: "IX_reticle_usages_reticle_id",
                schema: "equipment",
                table: "reticle_usages",
                column: "reticle_id");

            migrationBuilder.CreateIndex(
                name: "IX_material_mounts_equipment_id",
                schema: "equipment",
                table: "material_mounts",
                column: "equipment_id");

            migrationBuilder.CreateIndex(
                name: "IX_maintenance_events_schedule_id",
                schema: "equipment",
                table: "maintenance_events",
                column: "schedule_id");
        }
        private static string ReadSql(string filename)
        {
            using var stream = typeof(EquipmentMaintenanceResources).Assembly.GetManifestResourceStream(
                "MESagent.Infrastructure.Persistence.Sql." + filename)
                ?? throw new InvalidOperationException("Missing migration resource: " + filename);
            using var reader = new System.IO.StreamReader(stream);
            return reader.ReadToEnd();
        }
    }
}
