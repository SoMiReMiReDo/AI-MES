using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MESagent.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SiteAppearanceFollowSystemTheme : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_site_appearance_theme",
                schema: "settings",
                table: "site_appearance");

            migrationBuilder.AlterColumn<string>(
                name: "default_theme",
                schema: "settings",
                table: "site_appearance",
                type: "character varying(6)",
                maxLength: 6,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(5)",
                oldMaxLength: 5);

            migrationBuilder.AddCheckConstraint(
                name: "ck_site_appearance_theme",
                schema: "settings",
                table: "site_appearance",
                sql: "default_theme IN ('light', 'dark', 'system')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_site_appearance_theme",
                schema: "settings",
                table: "site_appearance");

            migrationBuilder.AlterColumn<string>(
                name: "default_theme",
                schema: "settings",
                table: "site_appearance",
                type: "character varying(5)",
                maxLength: 5,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(6)",
                oldMaxLength: 6);

            migrationBuilder.AddCheckConstraint(
                name: "ck_site_appearance_theme",
                schema: "settings",
                table: "site_appearance",
                sql: "default_theme IN ('light', 'dark')");
        }
    }
}
