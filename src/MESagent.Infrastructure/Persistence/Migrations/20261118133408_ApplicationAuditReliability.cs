using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MESagent.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ApplicationAuditReliability : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_outbox_messages_next_attempt_at_occurred_at",
                schema: "audit",
                table: "outbox_messages");

            migrationBuilder.AddColumn<Guid>(
                name: "business_event_id",
                schema: "audit",
                table: "outbox_messages",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "dead_lettered_at",
                schema: "audit",
                table: "outbox_messages",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "fab_id",
                schema: "audit",
                table: "audit_logs",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "module",
                schema: "audit",
                table: "audit_logs",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_aggregate_id_next_attempt_at_occurred_at_id",
                schema: "audit",
                table: "outbox_messages",
                columns: new[] { "aggregate_id", "next_attempt_at", "occurred_at", "id" },
                filter: "published_at IS NULL AND dead_lettered_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_business_event_id",
                schema: "audit",
                table: "outbox_messages",
                column: "business_event_id",
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_outbox_delivery_state",
                schema: "audit",
                table: "outbox_messages",
                sql: "dead_lettered_at IS NULL OR (published_at IS NULL AND next_attempt_at IS NULL)");

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_fab_id_occurred_at",
                schema: "audit",
                table: "audit_logs",
                columns: new[] { "fab_id", "occurred_at" });

            migrationBuilder.AddForeignKey(
                name: "FK_audit_logs_fabs_fab_id",
                schema: "audit",
                table: "audit_logs",
                column: "fab_id",
                principalSchema: "organization",
                principalTable: "fabs",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_outbox_messages_business_events_business_event_id",
                schema: "audit",
                table: "outbox_messages",
                column: "business_event_id",
                principalSchema: "audit",
                principalTable: "business_events",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.Sql("""
                INSERT INTO security.permissions(code,name,is_active)
                SELECT code,code,true FROM unnest(ARRAY[
                    'audit.operations.read','security.outbox.read','security.outbox.replay'
                ]) code ON CONFLICT(code) DO NOTHING;
                INSERT INTO security.role_permissions(role_id,permission_id)
                SELECT r.id,p.id FROM security.roles r CROSS JOIN security.permissions p
                WHERE r.name='SystemAdmin' AND r.is_system
                    AND p.code IN ('audit.operations.read','security.outbox.read','security.outbox.replay')
                ON CONFLICT(role_id,permission_id) DO NOTHING;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DO $$ BEGIN
                    IF EXISTS (SELECT FROM audit.outbox_messages WHERE business_event_id IS NOT NULL)
                        OR EXISTS (SELECT FROM audit.audit_logs WHERE module IS NOT NULL OR fab_id IS NOT NULL) THEN
                        RAISE EXCEPTION 'Cannot downgrade while scoped audit or delivery records exist';
                    END IF;
                END $$;
                DELETE FROM security.user_permissions WHERE permission_id IN (SELECT id FROM security.permissions
                    WHERE code IN ('audit.operations.read','security.outbox.read','security.outbox.replay'));
                DELETE FROM security.role_permissions WHERE permission_id IN (SELECT id FROM security.permissions
                    WHERE code IN ('audit.operations.read','security.outbox.read','security.outbox.replay'));
                DELETE FROM security.permissions
                    WHERE code IN ('audit.operations.read','security.outbox.read','security.outbox.replay');
                """);

            migrationBuilder.DropForeignKey(
                name: "FK_audit_logs_fabs_fab_id",
                schema: "audit",
                table: "audit_logs");

            migrationBuilder.DropForeignKey(
                name: "FK_outbox_messages_business_events_business_event_id",
                schema: "audit",
                table: "outbox_messages");

            migrationBuilder.DropIndex(
                name: "IX_outbox_messages_aggregate_id_next_attempt_at_occurred_at_id",
                schema: "audit",
                table: "outbox_messages");

            migrationBuilder.DropIndex(
                name: "IX_outbox_messages_business_event_id",
                schema: "audit",
                table: "outbox_messages");

            migrationBuilder.DropCheckConstraint(
                name: "ck_outbox_delivery_state",
                schema: "audit",
                table: "outbox_messages");

            migrationBuilder.DropIndex(
                name: "IX_audit_logs_fab_id_occurred_at",
                schema: "audit",
                table: "audit_logs");

            migrationBuilder.DropColumn(
                name: "business_event_id",
                schema: "audit",
                table: "outbox_messages");

            migrationBuilder.DropColumn(
                name: "dead_lettered_at",
                schema: "audit",
                table: "outbox_messages");

            migrationBuilder.DropColumn(
                name: "fab_id",
                schema: "audit",
                table: "audit_logs");

            migrationBuilder.DropColumn(
                name: "module",
                schema: "audit",
                table: "audit_logs");

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_next_attempt_at_occurred_at",
                schema: "audit",
                table: "outbox_messages",
                columns: new[] { "next_attempt_at", "occurred_at" },
                filter: "published_at IS NULL");
        }
    }
}
