using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MESagent.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class LotWaferCarrierGenealogy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "fab_id",
                schema: "production",
                table: "wafers",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "work_order_id",
                schema: "production",
                table: "wafers",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "close_reason",
                schema: "production",
                table: "lots",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "closed_at",
                schema: "production",
                table: "lots",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "allowed_wafer_size_mm",
                schema: "production",
                table: "carriers",
                type: "numeric(20,6)",
                precision: 20,
                scale: 6,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "step_execution_inheritances",
                schema: "production",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    genealogy_event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_step_execution_id = table.Column<Guid>(type: "uuid", nullable: false),
                    target_step_execution_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_step_execution_inheritances", x => x.id);
                    table.ForeignKey(
                        name: "FK_step_execution_inheritances_genealogy_events_genealogy_even~",
                        column: x => x.genealogy_event_id,
                        principalSchema: "production",
                        principalTable: "genealogy_events",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_step_execution_inheritances_step_executions_source_step_exe~",
                        column: x => x.source_step_execution_id,
                        principalSchema: "production",
                        principalTable: "step_executions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_step_execution_inheritances_step_executions_target_step_exe~",
                        column: x => x.target_step_execution_id,
                        principalSchema: "production",
                        principalTable: "step_executions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "wafer_completions",
                schema: "production",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    wafer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    work_order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    lot_id_at_completion = table.Column<Guid>(type: "uuid", nullable: false),
                    process_run_id = table.Column<Guid>(type: "uuid", nullable: false),
                    route_version_id = table.Column<Guid>(type: "uuid", nullable: false),
                    route_step_id = table.Column<Guid>(type: "uuid", nullable: false),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    completed_by_id = table.Column<Guid>(type: "uuid", nullable: false),
                    command_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_wafer_completions", x => x.id);
                    table.ForeignKey(
                        name: "FK_wafer_completions_lots_lot_id_at_completion",
                        column: x => x.lot_id_at_completion,
                        principalSchema: "production",
                        principalTable: "lots",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_wafer_completions_process_runs_process_run_id",
                        column: x => x.process_run_id,
                        principalSchema: "production",
                        principalTable: "process_runs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_wafer_completions_route_steps_route_step_id",
                        column: x => x.route_step_id,
                        principalSchema: "engineering",
                        principalTable: "route_steps",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_wafer_completions_route_versions_route_version_id",
                        column: x => x.route_version_id,
                        principalSchema: "engineering",
                        principalTable: "route_versions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_wafer_completions_users_completed_by_id",
                        column: x => x.completed_by_id,
                        principalSchema: "security",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_wafer_completions_wafers_wafer_id",
                        column: x => x.wafer_id,
                        principalSchema: "production",
                        principalTable: "wafers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_wafer_completions_work_orders_work_order_id",
                        column: x => x.work_order_id,
                        principalSchema: "production",
                        principalTable: "work_orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "wafer_releases",
                schema: "production",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    wafer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    work_order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    initial_lot_id = table.Column<Guid>(type: "uuid", nullable: false),
                    snapshot_version = table.Column<int>(type: "integer", nullable: false),
                    route_version_id = table.Column<Guid>(type: "uuid", nullable: false),
                    released_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    released_by_id = table.Column<Guid>(type: "uuid", nullable: false),
                    command_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_wafer_releases", x => x.id);
                    table.ForeignKey(
                        name: "FK_wafer_releases_lots_initial_lot_id",
                        column: x => x.initial_lot_id,
                        principalSchema: "production",
                        principalTable: "lots",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_wafer_releases_route_versions_route_version_id",
                        column: x => x.route_version_id,
                        principalSchema: "engineering",
                        principalTable: "route_versions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_wafer_releases_users_released_by_id",
                        column: x => x.released_by_id,
                        principalSchema: "security",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_wafer_releases_wafers_wafer_id",
                        column: x => x.wafer_id,
                        principalSchema: "production",
                        principalTable: "wafers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_wafer_releases_work_orders_work_order_id",
                        column: x => x.work_order_id,
                        principalSchema: "production",
                        principalTable: "work_orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_wafers_fab_id",
                schema: "production",
                table: "wafers",
                column: "fab_id");

            migrationBuilder.CreateIndex(
                name: "IX_wafers_work_order_id",
                schema: "production",
                table: "wafers",
                column: "work_order_id");

            migrationBuilder.CreateIndex(
                name: "IX_step_execution_inheritances_genealogy_event_id_source_step_~",
                schema: "production",
                table: "step_execution_inheritances",
                columns: new[] { "genealogy_event_id", "source_step_execution_id", "target_step_execution_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_step_execution_inheritances_source_step_execution_id",
                schema: "production",
                table: "step_execution_inheritances",
                column: "source_step_execution_id");

            migrationBuilder.CreateIndex(
                name: "IX_step_execution_inheritances_target_step_execution_id",
                schema: "production",
                table: "step_execution_inheritances",
                column: "target_step_execution_id");

            migrationBuilder.CreateIndex(
                name: "IX_wafer_completions_completed_by_id",
                schema: "production",
                table: "wafer_completions",
                column: "completed_by_id");

            migrationBuilder.CreateIndex(
                name: "IX_wafer_completions_lot_id_at_completion",
                schema: "production",
                table: "wafer_completions",
                column: "lot_id_at_completion");

            migrationBuilder.CreateIndex(
                name: "IX_wafer_completions_process_run_id",
                schema: "production",
                table: "wafer_completions",
                column: "process_run_id");

            migrationBuilder.CreateIndex(
                name: "IX_wafer_completions_route_step_id",
                schema: "production",
                table: "wafer_completions",
                column: "route_step_id");

            migrationBuilder.CreateIndex(
                name: "IX_wafer_completions_route_version_id",
                schema: "production",
                table: "wafer_completions",
                column: "route_version_id");

            migrationBuilder.CreateIndex(
                name: "IX_wafer_completions_wafer_id",
                schema: "production",
                table: "wafer_completions",
                column: "wafer_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_wafer_completions_work_order_id",
                schema: "production",
                table: "wafer_completions",
                column: "work_order_id");

            migrationBuilder.CreateIndex(
                name: "IX_wafer_releases_initial_lot_id",
                schema: "production",
                table: "wafer_releases",
                column: "initial_lot_id");

            migrationBuilder.CreateIndex(
                name: "IX_wafer_releases_released_by_id",
                schema: "production",
                table: "wafer_releases",
                column: "released_by_id");

            migrationBuilder.CreateIndex(
                name: "IX_wafer_releases_route_version_id",
                schema: "production",
                table: "wafer_releases",
                column: "route_version_id");

            migrationBuilder.CreateIndex(
                name: "IX_wafer_releases_wafer_id",
                schema: "production",
                table: "wafer_releases",
                column: "wafer_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_wafer_releases_work_order_id",
                schema: "production",
                table: "wafer_releases",
                column: "work_order_id");

            migrationBuilder.AddForeignKey(
                name: "FK_wafers_fabs_fab_id",
                schema: "production",
                table: "wafers",
                column: "fab_id",
                principalSchema: "organization",
                principalTable: "fabs",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_wafers_work_orders_work_order_id",
                schema: "production",
                table: "wafers",
                column: "work_order_id",
                principalSchema: "production",
                principalTable: "work_orders",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
            migrationBuilder.Sql(ReadSql("ProductionLifecycle.up.sql"));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(ReadSql("ProductionLifecycle.down.sql"));
            migrationBuilder.DropForeignKey(
                name: "FK_wafers_fabs_fab_id",
                schema: "production",
                table: "wafers");

            migrationBuilder.DropForeignKey(
                name: "FK_wafers_work_orders_work_order_id",
                schema: "production",
                table: "wafers");

            migrationBuilder.DropTable(
                name: "step_execution_inheritances",
                schema: "production");

            migrationBuilder.DropTable(
                name: "wafer_completions",
                schema: "production");

            migrationBuilder.DropTable(
                name: "wafer_releases",
                schema: "production");

            migrationBuilder.DropIndex(
                name: "IX_wafers_fab_id",
                schema: "production",
                table: "wafers");

            migrationBuilder.DropIndex(
                name: "IX_wafers_work_order_id",
                schema: "production",
                table: "wafers");

            migrationBuilder.DropColumn(
                name: "fab_id",
                schema: "production",
                table: "wafers");

            migrationBuilder.DropColumn(
                name: "work_order_id",
                schema: "production",
                table: "wafers");

            migrationBuilder.DropColumn(
                name: "close_reason",
                schema: "production",
                table: "lots");

            migrationBuilder.DropColumn(
                name: "closed_at",
                schema: "production",
                table: "lots");

            migrationBuilder.DropColumn(
                name: "allowed_wafer_size_mm",
                schema: "production",
                table: "carriers");
        }
        private static string ReadSql(string filename)
        {
            using var stream = typeof(LotWaferCarrierGenealogy).Assembly.GetManifestResourceStream(
                "MESagent.Infrastructure.Persistence.Sql." + filename)
                ?? throw new InvalidOperationException("Missing migration resource: " + filename);
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }
    }
}
