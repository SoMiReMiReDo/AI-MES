using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MESagent.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class WaferMaterialOrigins : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "integration_config_id",
                schema: "materials",
                table: "material_sources",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<Guid>(
                name: "manual_fab_id",
                schema: "materials",
                table: "material_sources",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_material_sources_manual_fab_id_material_id_external_code",
                schema: "materials",
                table: "material_sources",
                columns: new[] { "manual_fab_id", "material_id", "external_code" },
                unique: true,
                filter: "manual_fab_id IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "ck_material_sources_mode",
                schema: "materials",
                table: "material_sources",
                sql: "(integration_config_id IS NULL) <> (manual_fab_id IS NULL)");

            migrationBuilder.AddForeignKey(
                name: "FK_material_sources_fabs_manual_fab_id",
                schema: "materials",
                table: "material_sources",
                column: "manual_fab_id",
                principalSchema: "organization",
                principalTable: "fabs",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.Sql("""
                INSERT INTO security.permissions(code,name,is_active)
                SELECT code,code,true
                FROM unnest(ARRAY['materials.wafer-sources.read','materials.wafer-sources.manage']) code
                ON CONFLICT(code) DO NOTHING;
                INSERT INTO security.role_permissions(role_id,permission_id)
                SELECT r.id,p.id FROM security.roles r CROSS JOIN security.permissions p
                WHERE r.name='SystemAdmin' AND r.is_system
                    AND p.code IN ('materials.wafer-sources.read','materials.wafer-sources.manage')
                ON CONFLICT(role_id,permission_id) DO NOTHING;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DO $$ BEGIN
                    IF EXISTS (SELECT FROM materials.material_sources WHERE manual_fab_id IS NOT NULL) THEN
                        RAISE EXCEPTION 'Cannot downgrade while manual wafer sources exist';
                    END IF;
                END $$;
                DELETE FROM security.role_permissions
                WHERE permission_id IN (SELECT id FROM security.permissions
                    WHERE code IN ('materials.wafer-sources.read','materials.wafer-sources.manage'));
                DELETE FROM security.permissions
                WHERE code IN ('materials.wafer-sources.read','materials.wafer-sources.manage');
                """);


            migrationBuilder.DropForeignKey(
                name: "FK_material_sources_fabs_manual_fab_id",
                schema: "materials",
                table: "material_sources");

            migrationBuilder.DropIndex(
                name: "IX_material_sources_manual_fab_id_material_id_external_code",
                schema: "materials",
                table: "material_sources");

            migrationBuilder.DropCheckConstraint(
                name: "ck_material_sources_mode",
                schema: "materials",
                table: "material_sources");

            migrationBuilder.DropColumn(
                name: "manual_fab_id",
                schema: "materials",
                table: "material_sources");

            migrationBuilder.AlterColumn<Guid>(
                name: "integration_config_id",
                schema: "materials",
                table: "material_sources",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);
        }
    }
}
