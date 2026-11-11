using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MESagent.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class F01IdentityAndOrganization : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_users_worker_id",
                schema: "security",
                table: "users");

            migrationBuilder.AddColumn<Guid>(
                name: "fab_id",
                schema: "organization",
                table: "terminals",
                type: "uuid",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE organization.terminals t SET fab_id = a.fab_id
                FROM organization.workstations w JOIN organization.areas a ON a.id = w.area_id
                WHERE t.workstation_id = w.id;
                DO $$ BEGIN
                  IF EXISTS (SELECT 1 FROM organization.terminals WHERE fab_id IS NULL) THEN
                    RAISE EXCEPTION 'F01 migration: unbound terminals require an explicit workstation/fab assignment before migration';
                  END IF;
                END $$;
                ALTER TABLE organization.terminals ALTER COLUMN fab_id SET NOT NULL;
                """);

            migrationBuilder.AddColumn<bool>(
                name: "is_active",
                schema: "security",
                table: "roles",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_system",
                schema: "security",
                table: "roles",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "security_events",
                schema: "security",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    occurred_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    session_id = table.Column<Guid>(type: "uuid", nullable: true),
                    event_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    outcome = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    correlation_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_security_events", x => x.id);
                    table.ForeignKey(
                        name: "FK_security_events_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "security",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "user_sessions",
                schema: "security",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_activity_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    idle_expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    absolute_expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    revoked_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    revoke_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    restricted = table.Column<bool>(type: "boolean", nullable: false),
                    terminal_id = table.Column<Guid>(type: "uuid", nullable: true),
                    security_stamp = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_sessions", x => x.id);
                    table.CheckConstraint("ck_session_times", "absolute_expires_at > created_at AND idle_expires_at <= absolute_expires_at");
                    table.ForeignKey(
                        name: "FK_user_sessions_terminals_terminal_id",
                        column: x => x.terminal_id,
                        principalSchema: "organization",
                        principalTable: "terminals",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_user_sessions_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "security",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_users_worker_id",
                schema: "security",
                table: "users",
                column: "worker_id",
                unique: true,
                filter: "worker_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_terminals_fab_id_code",
                schema: "organization",
                table: "terminals",
                columns: new[] { "fab_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_security_events_occurred_at",
                schema: "security",
                table: "security_events",
                column: "occurred_at");

            migrationBuilder.CreateIndex(
                name: "IX_security_events_user_id",
                schema: "security",
                table: "security_events",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_sessions_terminal_id",
                schema: "security",
                table: "user_sessions",
                column: "terminal_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_sessions_user_id_revoked_at",
                schema: "security",
                table: "user_sessions",
                columns: new[] { "user_id", "revoked_at" });

            migrationBuilder.AddForeignKey(
                name: "FK_terminals_fabs_fab_id",
                schema: "organization",
                table: "terminals",
                column: "fab_id",
                principalSchema: "organization",
                principalTable: "fabs",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
            migrationBuilder.Sql("""
                CREATE FUNCTION organization.validate_f01_context() RETURNS trigger LANGUAGE plpgsql AS $$
                DECLARE expected_fab uuid;
                BEGIN
                  IF TG_TABLE_NAME = 'workstations' THEN
                    IF NEW.line_id IS NOT NULL AND NOT EXISTS (SELECT 1 FROM organization.lines l WHERE l.id = NEW.line_id AND l.area_id = NEW.area_id) THEN
                      RAISE EXCEPTION 'workstation line must belong to the same area' USING ERRCODE = '23514';
                    END IF;
                    SELECT fab_id INTO expected_fab FROM organization.areas WHERE id = NEW.area_id;
                    IF EXISTS (SELECT 1 FROM organization.terminals WHERE workstation_id = NEW.id AND fab_id <> expected_fab) THEN
                      RAISE EXCEPTION 'workstation change would invalidate terminal fab' USING ERRCODE = '23514';
                    END IF;
                  ELSE
                    IF NEW.workstation_id IS NOT NULL THEN
                      SELECT a.fab_id INTO expected_fab FROM organization.workstations w JOIN organization.areas a ON a.id=w.area_id WHERE w.id=NEW.workstation_id;
                      IF expected_fab IS DISTINCT FROM NEW.fab_id THEN
                        RAISE EXCEPTION 'terminal and workstation must belong to the same fab' USING ERRCODE = '23514';
                      END IF;
                    END IF;
                  END IF;
                  RETURN NEW;
                END $$;
                CREATE TRIGGER f01_context BEFORE INSERT OR UPDATE ON organization.workstations FOR EACH ROW EXECUTE FUNCTION organization.validate_f01_context();
                CREATE TRIGGER f01_context BEFORE INSERT OR UPDATE ON organization.terminals FOR EACH ROW EXECUTE FUNCTION organization.validate_f01_context();
                CREATE TRIGGER immutable_history BEFORE UPDATE OR DELETE ON security.security_events FOR EACH ROW EXECUTE FUNCTION audit.reject_history_mutation();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP TRIGGER f01_context ON organization.workstations;
                DROP TRIGGER f01_context ON organization.terminals;
                DROP FUNCTION organization.validate_f01_context();
                """);
            migrationBuilder.DropForeignKey(
                name: "FK_terminals_fabs_fab_id",
                schema: "organization",
                table: "terminals");

            migrationBuilder.DropTable(
                name: "security_events",
                schema: "security");

            migrationBuilder.DropTable(
                name: "user_sessions",
                schema: "security");

            migrationBuilder.DropIndex(
                name: "IX_users_worker_id",
                schema: "security",
                table: "users");

            migrationBuilder.DropIndex(
                name: "IX_terminals_fab_id_code",
                schema: "organization",
                table: "terminals");

            migrationBuilder.DropColumn(
                name: "fab_id",
                schema: "organization",
                table: "terminals");

            migrationBuilder.DropColumn(
                name: "is_active",
                schema: "security",
                table: "roles");

            migrationBuilder.DropColumn(
                name: "is_system",
                schema: "security",
                table: "roles");

            migrationBuilder.CreateIndex(
                name: "IX_users_worker_id",
                schema: "security",
                table: "users",
                column: "worker_id");
        }
    }
}
