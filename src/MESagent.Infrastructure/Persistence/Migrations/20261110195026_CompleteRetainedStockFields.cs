using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MESagent.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CompleteRetainedStockFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_warehouse_stocks_material_lot_id",
                schema: "materials",
                table: "warehouse_stocks");

            migrationBuilder.DropIndex(
                name: "IX_warehouse_stocks_warehouse_id_material_id_material_lot_id",
                schema: "materials",
                table: "warehouse_stocks");

            migrationBuilder.DropCheckConstraint(
                name: "ck_lots_1",
                schema: "production",
                table: "lots");

            migrationBuilder.AlterColumn<Guid>(
                name: "material_id",
                schema: "materials",
                table: "warehouse_stocks",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<Guid>(
                name: "product_id",
                schema: "materials",
                table: "warehouse_stocks",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "operator_id",
                schema: "production",
                table: "process_run_wafers",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "produced_qty",
                schema: "production",
                table: "lots",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddUniqueConstraint(
                name: "AK_material_lots_id_material_id",
                schema: "materials",
                table: "material_lots",
                columns: new[] { "id", "material_id" });

            migrationBuilder.CreateIndex(
                name: "IX_warehouse_stocks_material_lot_id_material_id",
                schema: "materials",
                table: "warehouse_stocks",
                columns: new[] { "material_lot_id", "material_id" });

            migrationBuilder.CreateIndex(
                name: "IX_warehouse_stocks_product_id",
                schema: "materials",
                table: "warehouse_stocks",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "IX_warehouse_stocks_warehouse_id_material_id_product_id_materi~",
                schema: "materials",
                table: "warehouse_stocks",
                columns: new[] { "warehouse_id", "material_id", "product_id", "material_lot_id" },
                unique: true)
                .Annotation("Npgsql:NullsDistinct", false);

            migrationBuilder.AddCheckConstraint(
                name: "ck_warehouse_stocks_owner",
                schema: "materials",
                table: "warehouse_stocks",
                sql: "num_nonnulls(material_id, product_id) = 1 AND (material_lot_id IS NULL OR material_id IS NOT NULL)");

            migrationBuilder.CreateIndex(
                name: "IX_process_run_wafers_operator_id",
                schema: "production",
                table: "process_run_wafers",
                column: "operator_id");

            migrationBuilder.AddCheckConstraint(
                name: "ck_lots_1",
                schema: "production",
                table: "lots",
                sql: "target_qty >= 0 AND produced_qty >= 0");

            migrationBuilder.AddForeignKey(
                name: "FK_process_run_wafers_users_operator_id",
                schema: "production",
                table: "process_run_wafers",
                column: "operator_id",
                principalSchema: "security",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_warehouse_stocks_material_lots_material_lot_id_material_id",
                schema: "materials",
                table: "warehouse_stocks",
                columns: new[] { "material_lot_id", "material_id" },
                principalSchema: "materials",
                principalTable: "material_lots",
                principalColumns: new[] { "id", "material_id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_warehouse_stocks_products_product_id",
                schema: "materials",
                table: "warehouse_stocks",
                column: "product_id",
                principalSchema: "engineering",
                principalTable: "products",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_process_run_wafers_users_operator_id",
                schema: "production",
                table: "process_run_wafers");

            migrationBuilder.DropForeignKey(
                name: "FK_warehouse_stocks_material_lots_material_lot_id_material_id",
                schema: "materials",
                table: "warehouse_stocks");

            migrationBuilder.DropForeignKey(
                name: "FK_warehouse_stocks_products_product_id",
                schema: "materials",
                table: "warehouse_stocks");

            migrationBuilder.DropIndex(
                name: "IX_warehouse_stocks_material_lot_id_material_id",
                schema: "materials",
                table: "warehouse_stocks");

            migrationBuilder.DropIndex(
                name: "IX_warehouse_stocks_product_id",
                schema: "materials",
                table: "warehouse_stocks");

            migrationBuilder.DropIndex(
                name: "IX_warehouse_stocks_warehouse_id_material_id_product_id_materi~",
                schema: "materials",
                table: "warehouse_stocks");

            migrationBuilder.DropCheckConstraint(
                name: "ck_warehouse_stocks_owner",
                schema: "materials",
                table: "warehouse_stocks");

            migrationBuilder.DropIndex(
                name: "IX_process_run_wafers_operator_id",
                schema: "production",
                table: "process_run_wafers");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_material_lots_id_material_id",
                schema: "materials",
                table: "material_lots");

            migrationBuilder.DropCheckConstraint(
                name: "ck_lots_1",
                schema: "production",
                table: "lots");

            migrationBuilder.DropColumn(
                name: "product_id",
                schema: "materials",
                table: "warehouse_stocks");

            migrationBuilder.DropColumn(
                name: "operator_id",
                schema: "production",
                table: "process_run_wafers");

            migrationBuilder.DropColumn(
                name: "produced_qty",
                schema: "production",
                table: "lots");

            migrationBuilder.AlterColumn<Guid>(
                name: "material_id",
                schema: "materials",
                table: "warehouse_stocks",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_warehouse_stocks_material_lot_id",
                schema: "materials",
                table: "warehouse_stocks",
                column: "material_lot_id");

            migrationBuilder.CreateIndex(
                name: "IX_warehouse_stocks_warehouse_id_material_id_material_lot_id",
                schema: "materials",
                table: "warehouse_stocks",
                columns: new[] { "warehouse_id", "material_id", "material_lot_id" },
                unique: true)
                .Annotation("Npgsql:NullsDistinct", false);

            migrationBuilder.AddCheckConstraint(
                name: "ck_lots_1",
                schema: "production",
                table: "lots",
                sql: "target_qty >= 0");
        }
    }
}
