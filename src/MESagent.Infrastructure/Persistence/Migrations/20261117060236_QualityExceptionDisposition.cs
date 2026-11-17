using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MESagent.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class QualityExceptionDisposition : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "expected_boolean",
                schema: "quality",
                table: "specification_versions",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "expected_text",
                schema: "quality",
                table: "specification_versions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "issue_id",
                schema: "quality",
                table: "quality_checks",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "lifecycle_status",
                schema: "quality",
                table: "quality_check_templates",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "Draft");

            migrationBuilder.AddColumn<DateTime>(
                name: "released_at",
                schema: "quality",
                table: "quality_check_templates",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "released_by_id",
                schema: "quality",
                table: "quality_check_templates",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "command_id",
                schema: "quality",
                table: "lot_releases",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "process_run_id",
                schema: "quality",
                table: "lot_releases",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "recovery_action",
                schema: "quality",
                table: "lot_releases",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "None");

            migrationBuilder.AddColumn<Guid>(
                name: "step_execution_id",
                schema: "quality",
                table: "lot_releases",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_blocking",
                schema: "quality",
                table: "issues",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_quality_checks_issue_id",
                schema: "quality",
                table: "quality_checks",
                column: "issue_id");

            migrationBuilder.CreateIndex(
                name: "IX_quality_check_templates_released_by_id",
                schema: "quality",
                table: "quality_check_templates",
                column: "released_by_id");

            migrationBuilder.CreateIndex(
                name: "IX_lot_releases_command_id",
                schema: "quality",
                table: "lot_releases",
                column: "command_id",
                unique: true,
                filter: "command_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_lot_releases_process_run_id",
                schema: "quality",
                table: "lot_releases",
                column: "process_run_id");

            migrationBuilder.CreateIndex(
                name: "IX_lot_releases_step_execution_id",
                schema: "quality",
                table: "lot_releases",
                column: "step_execution_id");

            migrationBuilder.AddForeignKey(
                name: "FK_lot_releases_process_runs_process_run_id",
                schema: "quality",
                table: "lot_releases",
                column: "process_run_id",
                principalSchema: "production",
                principalTable: "process_runs",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_lot_releases_step_executions_step_execution_id",
                schema: "quality",
                table: "lot_releases",
                column: "step_execution_id",
                principalSchema: "production",
                principalTable: "step_executions",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_quality_check_templates_users_released_by_id",
                schema: "quality",
                table: "quality_check_templates",
                column: "released_by_id",
                principalSchema: "security",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_quality_checks_issues_issue_id",
                schema: "quality",
                table: "quality_checks",
                column: "issue_id",
                principalSchema: "quality",
                principalTable: "issues",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
            migrationBuilder.Sql(ReadSql("QualityLifecycle.up.sql"));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(ReadSql("QualityLifecycle.down.sql"));
            migrationBuilder.DropForeignKey(
                name: "FK_lot_releases_process_runs_process_run_id",
                schema: "quality",
                table: "lot_releases");

            migrationBuilder.DropForeignKey(
                name: "FK_lot_releases_step_executions_step_execution_id",
                schema: "quality",
                table: "lot_releases");

            migrationBuilder.DropForeignKey(
                name: "FK_quality_check_templates_users_released_by_id",
                schema: "quality",
                table: "quality_check_templates");

            migrationBuilder.DropForeignKey(
                name: "FK_quality_checks_issues_issue_id",
                schema: "quality",
                table: "quality_checks");

            migrationBuilder.DropIndex(
                name: "IX_quality_checks_issue_id",
                schema: "quality",
                table: "quality_checks");

            migrationBuilder.DropIndex(
                name: "IX_quality_check_templates_released_by_id",
                schema: "quality",
                table: "quality_check_templates");

            migrationBuilder.DropIndex(
                name: "IX_lot_releases_command_id",
                schema: "quality",
                table: "lot_releases");

            migrationBuilder.DropIndex(
                name: "IX_lot_releases_process_run_id",
                schema: "quality",
                table: "lot_releases");

            migrationBuilder.DropIndex(
                name: "IX_lot_releases_step_execution_id",
                schema: "quality",
                table: "lot_releases");

            migrationBuilder.DropColumn(
                name: "expected_boolean",
                schema: "quality",
                table: "specification_versions");

            migrationBuilder.DropColumn(
                name: "expected_text",
                schema: "quality",
                table: "specification_versions");

            migrationBuilder.DropColumn(
                name: "issue_id",
                schema: "quality",
                table: "quality_checks");

            migrationBuilder.DropColumn(
                name: "lifecycle_status",
                schema: "quality",
                table: "quality_check_templates");

            migrationBuilder.DropColumn(
                name: "released_at",
                schema: "quality",
                table: "quality_check_templates");

            migrationBuilder.DropColumn(
                name: "released_by_id",
                schema: "quality",
                table: "quality_check_templates");

            migrationBuilder.DropColumn(
                name: "command_id",
                schema: "quality",
                table: "lot_releases");

            migrationBuilder.DropColumn(
                name: "process_run_id",
                schema: "quality",
                table: "lot_releases");

            migrationBuilder.DropColumn(
                name: "recovery_action",
                schema: "quality",
                table: "lot_releases");

            migrationBuilder.DropColumn(
                name: "step_execution_id",
                schema: "quality",
                table: "lot_releases");

            migrationBuilder.DropColumn(
                name: "is_blocking",
                schema: "quality",
                table: "issues");
        }
        private static string ReadSql(string filename)
        {
            using var stream = typeof(QualityExceptionDisposition).Assembly.GetManifestResourceStream(
                "MESagent.Infrastructure.Persistence.Sql." + filename)
                ?? throw new InvalidOperationException("Missing migration resource: " + filename);
            using var reader = new System.IO.StreamReader(stream);
            return reader.ReadToEnd();
        }
    }
}
