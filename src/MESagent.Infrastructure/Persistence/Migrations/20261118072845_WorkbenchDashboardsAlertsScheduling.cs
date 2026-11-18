using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MESagent.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class WorkbenchDashboardsAlertsScheduling : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "workbench");

            migrationBuilder.CreateTable(
                name: "alert_receipts",
                schema: "workbench",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    fab_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    kind = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    source_id = table.Column<Guid>(type: "uuid", nullable: false),
                    acknowledged_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    silenced_until = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_alert_receipts", x => x.id);
                    table.ForeignKey(
                        name: "FK_alert_receipts_fabs_fab_id",
                        column: x => x.fab_id,
                        principalSchema: "organization",
                        principalTable: "fabs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_alert_receipts_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "security",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "equipment_ideal_rates",
                schema: "workbench",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    fab_id = table.Column<Guid>(type: "uuid", nullable: false),
                    equipment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    effective_on = table.Column<DateOnly>(type: "date", nullable: false),
                    ideal_seconds = table.Column<decimal>(type: "numeric(12,3)", precision: 12, scale: 3, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_equipment_ideal_rates", x => x.id);
                    table.CheckConstraint("ck_equipment_ideal_rate", "ideal_seconds > 0 AND ideal_seconds <= 86400");
                    table.ForeignKey(
                        name: "FK_equipment_ideal_rates_equipment_equipment_id",
                        column: x => x.equipment_id,
                        principalSchema: "equipment",
                        principalTable: "equipment",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_equipment_ideal_rates_fabs_fab_id",
                        column: x => x.fab_id,
                        principalSchema: "organization",
                        principalTable: "fabs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "production_schedule_blocks",
                schema: "workbench",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    fab_id = table.Column<Guid>(type: "uuid", nullable: false),
                    step_execution_id = table.Column<Guid>(type: "uuid", nullable: false),
                    equipment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    starts_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ends_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    cancelled_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_production_schedule_blocks", x => x.id);
                    table.CheckConstraint("ck_schedule_block_range", "starts_at < ends_at");
                    table.ForeignKey(
                        name: "FK_production_schedule_blocks_equipment_equipment_id",
                        column: x => x.equipment_id,
                        principalSchema: "equipment",
                        principalTable: "equipment",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_production_schedule_blocks_fabs_fab_id",
                        column: x => x.fab_id,
                        principalSchema: "organization",
                        principalTable: "fabs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_production_schedule_blocks_step_executions_step_execution_id",
                        column: x => x.step_execution_id,
                        principalSchema: "production",
                        principalTable: "step_executions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "subscriptions",
                schema: "workbench",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    fab_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    kind = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    enabled = table.Column<bool>(type: "boolean", nullable: false),
                    enabled_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    escalation_minutes = table.Column<int>(type: "integer", nullable: true),
                    escalate_to_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_subscriptions", x => x.id);
                    table.CheckConstraint("ck_subscription_escalation", "(escalation_minutes IS NULL AND escalate_to_id IS NULL) OR (escalation_minutes IS NOT NULL AND escalation_minutes BETWEEN 1 AND 10080 AND escalate_to_id IS NOT NULL AND escalate_to_id <> user_id)");
                    table.ForeignKey(
                        name: "FK_subscriptions_fabs_fab_id",
                        column: x => x.fab_id,
                        principalSchema: "organization",
                        principalTable: "fabs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_subscriptions_users_escalate_to_id",
                        column: x => x.escalate_to_id,
                        principalSchema: "security",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_subscriptions_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "security",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_alert_receipts_fab_id_user_id_kind_source_id",
                schema: "workbench",
                table: "alert_receipts",
                columns: new[] { "fab_id", "user_id", "kind", "source_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_alert_receipts_user_id",
                schema: "workbench",
                table: "alert_receipts",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_equipment_ideal_rates_equipment_id",
                schema: "workbench",
                table: "equipment_ideal_rates",
                column: "equipment_id");

            migrationBuilder.CreateIndex(
                name: "IX_equipment_ideal_rates_fab_id_equipment_id_effective_on",
                schema: "workbench",
                table: "equipment_ideal_rates",
                columns: new[] { "fab_id", "equipment_id", "effective_on" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_production_schedule_blocks_equipment_id",
                schema: "workbench",
                table: "production_schedule_blocks",
                column: "equipment_id");

            migrationBuilder.CreateIndex(
                name: "IX_production_schedule_blocks_fab_id_equipment_id_starts_at",
                schema: "workbench",
                table: "production_schedule_blocks",
                columns: new[] { "fab_id", "equipment_id", "starts_at" });

            migrationBuilder.CreateIndex(
                name: "IX_production_schedule_blocks_fab_id_step_execution_id",
                schema: "workbench",
                table: "production_schedule_blocks",
                columns: new[] { "fab_id", "step_execution_id" },
                unique: true,
                filter: "cancelled_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_production_schedule_blocks_step_execution_id",
                schema: "workbench",
                table: "production_schedule_blocks",
                column: "step_execution_id");

            migrationBuilder.CreateIndex(
                name: "IX_subscriptions_escalate_to_id",
                schema: "workbench",
                table: "subscriptions",
                column: "escalate_to_id");

            migrationBuilder.CreateIndex(
                name: "IX_subscriptions_fab_id_escalate_to_id_kind",
                schema: "workbench",
                table: "subscriptions",
                columns: new[] { "fab_id", "escalate_to_id", "kind" });

            migrationBuilder.CreateIndex(
                name: "IX_subscriptions_fab_id_user_id_kind",
                schema: "workbench",
                table: "subscriptions",
                columns: new[] { "fab_id", "user_id", "kind" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_subscriptions_user_id",
                schema: "workbench",
                table: "subscriptions",
                column: "user_id");

            migrationBuilder.Sql("""
                INSERT INTO security.permissions(code,name,is_active)
                SELECT code,code,true
                FROM unnest(ARRAY['workbench.schedule.manage','workbench.oee.configure']) code
                ON CONFLICT(code) DO NOTHING;
                INSERT INTO security.role_permissions(role_id,permission_id)
                SELECT r.id,p.id FROM security.roles r CROSS JOIN security.permissions p
                WHERE r.name='SystemAdmin' AND r.is_system
                    AND p.code IN ('workbench.schedule.manage','workbench.oee.configure')
                ON CONFLICT(role_id,permission_id) DO NOTHING;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DO $$ BEGIN
                    IF EXISTS (SELECT FROM workbench.equipment_ideal_rates)
                        OR EXISTS (SELECT FROM workbench.production_schedule_blocks)
                        OR EXISTS (SELECT FROM workbench.subscriptions)
                        OR EXISTS (SELECT FROM workbench.alert_receipts) THEN
                        RAISE EXCEPTION 'Cannot downgrade while workbench records exist';
                    END IF;
                END $$;
                DELETE FROM security.role_permissions
                WHERE permission_id IN (SELECT id FROM security.permissions
                    WHERE code IN ('workbench.schedule.manage','workbench.oee.configure'));
                DELETE FROM security.permissions
                WHERE code IN ('workbench.schedule.manage','workbench.oee.configure');
                """);

            migrationBuilder.DropTable(
                name: "alert_receipts",
                schema: "workbench");

            migrationBuilder.DropTable(
                name: "equipment_ideal_rates",
                schema: "workbench");

            migrationBuilder.DropTable(
                name: "production_schedule_blocks",
                schema: "workbench");

            migrationBuilder.DropTable(
                name: "subscriptions",
                schema: "workbench");
        }
    }
}
