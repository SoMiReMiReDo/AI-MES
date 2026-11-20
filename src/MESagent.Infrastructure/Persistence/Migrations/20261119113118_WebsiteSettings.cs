using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MESagent.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class WebsiteSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "settings");

            migrationBuilder.CreateTable(
                name: "fab_schedule_preferences",
                schema: "settings",
                columns: table => new
                {
                    fab_id = table.Column<Guid>(type: "uuid", nullable: false),
                    default_view = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: false),
                    version = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fab_schedule_preferences", x => x.fab_id);
                    table.CheckConstraint("ck_fab_schedule_view", "default_view IN ('day', 'week')");
                    table.ForeignKey(
                        name: "FK_fab_schedule_preferences_fabs_fab_id",
                        column: x => x.fab_id,
                        principalSchema: "organization",
                        principalTable: "fabs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "site_appearance",
                schema: "settings",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    subtitle = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    default_theme = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    version = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_site_appearance", x => x.id);
                    table.CheckConstraint("ck_site_appearance_name", "char_length(btrim(name)) BETWEEN 1 AND 60");
                    table.CheckConstraint("ck_site_appearance_singleton", "id = '67c6f063-676d-452b-bdd1-3a4556b9a35c'::uuid");
                    table.CheckConstraint("ck_site_appearance_theme", "default_theme IN ('light', 'dark')");
                });

            migrationBuilder.InsertData(
                schema: "settings",
                table: "site_appearance",
                columns: new[] { "id", "default_theme", "name", "subtitle", "version" },
                values: new object[] { new Guid("67c6f063-676d-452b-bdd1-3a4556b9a35c"), "light", "MESagent", "MANUFACTURING SYSTEM", new Guid("00000000-0000-0000-0000-000000000000") });

            migrationBuilder.Sql("""
                INSERT INTO security.permissions(code,name,is_active)
                SELECT code,code,true FROM unnest(ARRAY[
                    'security.settings.read','security.settings.appearance.manage','security.settings.schedule.manage'
                ]) code ON CONFLICT(code) DO NOTHING;
                INSERT INTO security.role_permissions(role_id,permission_id)
                SELECT r.id,p.id FROM security.roles r CROSS JOIN security.permissions p
                WHERE r.name='SystemAdmin' AND r.is_system
                    AND p.code IN ('security.settings.read','security.settings.appearance.manage',
                                   'security.settings.schedule.manage')
                ON CONFLICT(role_id,permission_id) DO NOTHING;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DO $$ BEGIN
                    IF EXISTS (SELECT FROM settings.fab_schedule_preferences)
                        OR EXISTS (SELECT FROM settings.site_appearance WHERE version <> '00000000-0000-0000-0000-000000000000')
                        OR EXISTS (SELECT FROM audit.audit_logs WHERE module = 'settings') THEN
                        RAISE EXCEPTION 'Cannot downgrade while saved website settings or their audit history exist';
                    END IF;
                END $$;
                DELETE FROM security.user_permissions WHERE permission_id IN (
                    SELECT id FROM security.permissions WHERE code LIKE 'security.settings.%');
                DELETE FROM security.role_permissions WHERE permission_id IN (
                    SELECT id FROM security.permissions WHERE code LIKE 'security.settings.%');
                DELETE FROM security.permissions WHERE code LIKE 'security.settings.%';
                """);

            migrationBuilder.DropTable(
                name: "fab_schedule_preferences",
                schema: "settings");

            migrationBuilder.DropTable(
                name: "site_appearance",
                schema: "settings");
        }
    }
}
