using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MESagent.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DispatchAndProductionExecution : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(ReadSql("ExecutionLifecycle.up.sql"));
            migrationBuilder.DropIndex(
                name: "IX_output_values_step_execution_id",
                schema: "production",
                table: "output_values");

            migrationBuilder.AddColumn<Guid>(
                name: "assigned_equipment_id",
                schema: "production",
                table: "step_executions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "assigned_recipe_version_id",
                schema: "production",
                table: "step_executions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "recipe_applicability_id",
                schema: "production",
                table: "process_runs",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_step_executions_assigned_equipment_id",
                schema: "production",
                table: "step_executions",
                column: "assigned_equipment_id");

            migrationBuilder.CreateIndex(
                name: "IX_step_executions_assigned_recipe_version_id",
                schema: "production",
                table: "step_executions",
                column: "assigned_recipe_version_id");

            migrationBuilder.CreateIndex(
                name: "IX_process_runs_recipe_applicability_id",
                schema: "production",
                table: "process_runs",
                column: "recipe_applicability_id");

            migrationBuilder.CreateIndex(
                name: "IX_output_values_step_execution_id_output_id",
                schema: "production",
                table: "output_values",
                columns: new[] { "step_execution_id", "output_id" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_process_runs_recipe_applicabilities_recipe_applicability_id",
                schema: "production",
                table: "process_runs",
                column: "recipe_applicability_id",
                principalSchema: "engineering",
                principalTable: "recipe_applicabilities",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_step_executions_equipment_assigned_equipment_id",
                schema: "production",
                table: "step_executions",
                column: "assigned_equipment_id",
                principalSchema: "equipment",
                principalTable: "equipment",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_step_executions_recipe_versions_assigned_recipe_version_id",
                schema: "production",
                table: "step_executions",
                column: "assigned_recipe_version_id",
                principalSchema: "engineering",
                principalTable: "recipe_versions",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(ReadSql("ExecutionLifecycle.down.sql"));
            migrationBuilder.DropForeignKey(
                name: "FK_process_runs_recipe_applicabilities_recipe_applicability_id",
                schema: "production",
                table: "process_runs");

            migrationBuilder.DropForeignKey(
                name: "FK_step_executions_equipment_assigned_equipment_id",
                schema: "production",
                table: "step_executions");

            migrationBuilder.DropForeignKey(
                name: "FK_step_executions_recipe_versions_assigned_recipe_version_id",
                schema: "production",
                table: "step_executions");

            migrationBuilder.DropIndex(
                name: "IX_step_executions_assigned_equipment_id",
                schema: "production",
                table: "step_executions");

            migrationBuilder.DropIndex(
                name: "IX_step_executions_assigned_recipe_version_id",
                schema: "production",
                table: "step_executions");

            migrationBuilder.DropIndex(
                name: "IX_process_runs_recipe_applicability_id",
                schema: "production",
                table: "process_runs");

            migrationBuilder.DropIndex(
                name: "IX_output_values_step_execution_id_output_id",
                schema: "production",
                table: "output_values");

            migrationBuilder.DropColumn(
                name: "assigned_equipment_id",
                schema: "production",
                table: "step_executions");

            migrationBuilder.DropColumn(
                name: "assigned_recipe_version_id",
                schema: "production",
                table: "step_executions");

            migrationBuilder.DropColumn(
                name: "recipe_applicability_id",
                schema: "production",
                table: "process_runs");

            migrationBuilder.CreateIndex(
                name: "IX_output_values_step_execution_id",
                schema: "production",
                table: "output_values",
                column: "step_execution_id");
        }

        private static string ReadSql(string filename)
        {
            using var stream = typeof(DispatchAndProductionExecution).Assembly.GetManifestResourceStream(
                "MESagent.Infrastructure.Persistence.Sql." + filename)
                ?? throw new InvalidOperationException("Missing migration resource: " + filename);
            using var reader = new System.IO.StreamReader(stream);
            return reader.ReadToEnd();
        }
    }
}
