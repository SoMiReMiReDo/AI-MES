using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MESagent.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialFabSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "materials");

            migrationBuilder.EnsureSchema(
                name: "integration");

            migrationBuilder.EnsureSchema(
                name: "organization");

            migrationBuilder.EnsureSchema(
                name: "audit");

            migrationBuilder.EnsureSchema(
                name: "production");

            migrationBuilder.EnsureSchema(
                name: "equipment");

            migrationBuilder.EnsureSchema(
                name: "engineering");

            migrationBuilder.EnsureSchema(
                name: "quality");

            migrationBuilder.EnsureSchema(
                name: "security");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:btree_gist", ",,");

            migrationBuilder.CreateTable(
                name: "companies",
                schema: "materials",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    company_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_companies", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "custom_field_definitions",
                schema: "integration",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    entity_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    key = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    label = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    field_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    config = table.Column<string>(type: "jsonb", nullable: true),
                    required = table.Column<bool>(type: "boolean", nullable: false),
                    position = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_custom_field_definitions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "equipment_types",
                schema: "equipment",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_equipment_types", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "fabs",
                schema: "organization",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    timezone = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fabs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "import_mappings",
                schema: "integration",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    mapping_config = table.Column<string>(type: "jsonb", nullable: false),
                    entity = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_import_mappings", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "integration_configs",
                schema: "integration",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    system_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    system_name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    config = table.Column<string>(type: "jsonb", nullable: true),
                    secret_reference = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_integration_configs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "issue_types",
                schema: "quality",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    severity = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    is_blocking = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_issue_types", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "label_templates",
                schema: "integration",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    label_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    size = table.Column<string>(type: "jsonb", nullable: false),
                    fields_config = table.Column<string>(type: "jsonb", nullable: false),
                    barcode_format = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_label_templates", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "material_types",
                schema: "materials",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_material_types", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "outbox_messages",
                schema: "audit",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    event_type = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    aggregate_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    aggregate_id = table.Column<Guid>(type: "uuid", nullable: false),
                    payload = table.Column<string>(type: "jsonb", nullable: false),
                    occurred_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    published_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    attempts = table.Column<int>(type: "integer", nullable: false),
                    next_attempt_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_error = table.Column<string>(type: "text", nullable: true),
                    correlation_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_outbox_messages", x => x.id);
                    table.CheckConstraint("ck_outbox_messages_1", "attempts >= 0");
                });

            migrationBuilder.CreateTable(
                name: "permissions",
                schema: "security",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_permissions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "products",
                schema: "engineering",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    unit_of_measure = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    category = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    external_code = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    external_system = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_products", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "reason_codes",
                schema: "quality",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    category = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_reason_codes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "recipes",
                schema: "engineering",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_recipes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "reticles",
                schema: "equipment",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    revision = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    layer_code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    usage_count = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_reticles", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "roles",
                schema: "security",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    normalized_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    concurrency_stamp = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_roles", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "operations",
                schema: "engineering",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    segment_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    equipment_type_id = table.Column<Guid>(type: "uuid", nullable: true),
                    standard_instruction = table.Column<string>(type: "text", nullable: true),
                    parameters = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_operations", x => x.id);
                    table.ForeignKey(
                        name: "FK_operations_equipment_types_equipment_type_id",
                        column: x => x.equipment_type_id,
                        principalSchema: "equipment",
                        principalTable: "equipment_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tools",
                schema: "equipment",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    equipment_type_id = table.Column<Guid>(type: "uuid", nullable: true),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    next_service_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tools", x => x.id);
                    table.ForeignKey(
                        name: "FK_tools_equipment_types_equipment_type_id",
                        column: x => x.equipment_type_id,
                        principalSchema: "equipment",
                        principalTable: "equipment_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "areas",
                schema: "organization",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    fab_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_areas", x => x.id);
                    table.ForeignKey(
                        name: "FK_areas_fabs_fab_id",
                        column: x => x.fab_id,
                        principalSchema: "organization",
                        principalTable: "fabs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "equipment_groups",
                schema: "equipment",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    fab_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_equipment_groups", x => x.id);
                    table.ForeignKey(
                        name: "FK_equipment_groups_fabs_fab_id",
                        column: x => x.fab_id,
                        principalSchema: "organization",
                        principalTable: "fabs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "warehouses",
                schema: "materials",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    fab_id = table.Column<Guid>(type: "uuid", nullable: false),
                    kind = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    erp_code = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_warehouses", x => x.id);
                    table.ForeignKey(
                        name: "FK_warehouses_fabs_fab_id",
                        column: x => x.fab_id,
                        principalSchema: "organization",
                        principalTable: "fabs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "workers",
                schema: "organization",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    fab_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_workers", x => x.id);
                    table.ForeignKey(
                        name: "FK_workers_fabs_fab_id",
                        column: x => x.fab_id,
                        principalSchema: "organization",
                        principalTable: "fabs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "api_keys",
                schema: "integration",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    integration_config_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    key_prefix = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    key_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    scopes = table.Column<string>(type: "jsonb", nullable: false),
                    ip_allowlist = table.Column<string>(type: "jsonb", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    last_used_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_api_keys", x => x.id);
                    table.ForeignKey(
                        name: "FK_api_keys_integration_configs_integration_config_id",
                        column: x => x.integration_config_id,
                        principalSchema: "integration",
                        principalTable: "integration_configs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "materials",
                schema: "materials",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    material_type_id = table.Column<Guid>(type: "uuid", nullable: false),
                    unit_of_measure = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    tracking_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    supplier_id = table.Column<Guid>(type: "uuid", nullable: true),
                    supplier_code = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    external_code = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    external_system = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_materials", x => x.id);
                    table.ForeignKey(
                        name: "FK_materials_companies_supplier_id",
                        column: x => x.supplier_id,
                        principalSchema: "materials",
                        principalTable: "companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_materials_material_types_material_type_id",
                        column: x => x.material_type_id,
                        principalSchema: "materials",
                        principalTable: "material_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "number_sequences",
                schema: "engineering",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: true),
                    purpose = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    prefix = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    suffix = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    next_number = table.Column<long>(type: "bigint", nullable: false),
                    pad_size = table.Column<int>(type: "integer", nullable: false),
                    pattern = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    reset_period = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    last_reset_key = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_number_sequences", x => x.id);
                    table.CheckConstraint("ck_number_sequences_1", "next_number >= 0 AND pad_size >= 0");
                    table.ForeignKey(
                        name: "FK_number_sequences_products_product_id",
                        column: x => x.product_id,
                        principalSchema: "engineering",
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "routes",
                schema: "engineering",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    product_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_routes", x => x.id);
                    table.ForeignKey(
                        name: "FK_routes_products_product_id",
                        column: x => x.product_id,
                        principalSchema: "engineering",
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "role_claims",
                schema: "security",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    role_id = table.Column<Guid>(type: "uuid", nullable: false),
                    claim_type = table.Column<string>(type: "text", nullable: true),
                    claim_value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_role_claims", x => x.id);
                    table.ForeignKey(
                        name: "FK_role_claims_roles_role_id",
                        column: x => x.role_id,
                        principalSchema: "security",
                        principalTable: "roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "role_permissions",
                schema: "security",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    role_id = table.Column<Guid>(type: "uuid", nullable: false),
                    permission_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_role_permissions", x => x.id);
                    table.ForeignKey(
                        name: "FK_role_permissions_permissions_permission_id",
                        column: x => x.permission_id,
                        principalSchema: "security",
                        principalTable: "permissions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_role_permissions_roles_role_id",
                        column: x => x.role_id,
                        principalSchema: "security",
                        principalTable: "roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "lines",
                schema: "organization",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    area_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_lines", x => x.id);
                    table.ForeignKey(
                        name: "FK_lines_areas_area_id",
                        column: x => x.area_id,
                        principalSchema: "organization",
                        principalTable: "areas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "locations",
                schema: "organization",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    area_id = table.Column<Guid>(type: "uuid", nullable: false),
                    location_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_locations", x => x.id);
                    table.ForeignKey(
                        name: "FK_locations_areas_area_id",
                        column: x => x.area_id,
                        principalSchema: "organization",
                        principalTable: "areas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "users",
                schema: "security",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    worker_id = table.Column<Guid>(type: "uuid", nullable: true),
                    account_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    force_password_change = table.Column<bool>(type: "boolean", nullable: false),
                    last_login_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    user_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    normalized_user_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    normalized_email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    email_confirmed = table.Column<bool>(type: "boolean", nullable: false),
                    password_hash = table.Column<string>(type: "text", nullable: true),
                    security_stamp = table.Column<string>(type: "text", nullable: true),
                    concurrency_stamp = table.Column<string>(type: "text", nullable: true),
                    phone_number = table.Column<string>(type: "text", nullable: true),
                    phone_number_confirmed = table.Column<bool>(type: "boolean", nullable: false),
                    two_factor_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    lockout_end = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    lockout_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    access_failed_count = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.id);
                    table.ForeignKey(
                        name: "FK_users_workers_worker_id",
                        column: x => x.worker_id,
                        principalSchema: "organization",
                        principalTable: "workers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "inspection_plans",
                schema: "quality",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    material_id = table.Column<Guid>(type: "uuid", nullable: true),
                    material_type_id = table.Column<Guid>(type: "uuid", nullable: true),
                    criteria = table.Column<string>(type: "jsonb", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    published_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    root_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inspection_plans", x => x.id);
                    table.CheckConstraint("ck_inspection_plans_1", "version > 0");
                    table.ForeignKey(
                        name: "FK_inspection_plans_inspection_plans_root_id",
                        column: x => x.root_id,
                        principalSchema: "quality",
                        principalTable: "inspection_plans",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_inspection_plans_material_types_material_type_id",
                        column: x => x.material_type_id,
                        principalSchema: "materials",
                        principalTable: "material_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_inspection_plans_materials_material_id",
                        column: x => x.material_id,
                        principalSchema: "materials",
                        principalTable: "materials",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "material_sources",
                schema: "materials",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    material_id = table.Column<Guid>(type: "uuid", nullable: false),
                    integration_config_id = table.Column<Guid>(type: "uuid", nullable: false),
                    external_code = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    external_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    unit_mapping = table.Column<string>(type: "jsonb", nullable: true),
                    conversion_factor = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    last_synced_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    sync_enabled = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_material_sources", x => x.id);
                    table.CheckConstraint("ck_material_sources_1", "conversion_factor > 0");
                    table.ForeignKey(
                        name: "FK_material_sources_integration_configs_integration_config_id",
                        column: x => x.integration_config_id,
                        principalSchema: "integration",
                        principalTable: "integration_configs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_material_sources_materials_material_id",
                        column: x => x.material_id,
                        principalSchema: "materials",
                        principalTable: "materials",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "equipment",
                schema: "equipment",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    area_id = table.Column<Guid>(type: "uuid", nullable: false),
                    line_id = table.Column<Guid>(type: "uuid", nullable: true),
                    equipment_group_id = table.Column<Guid>(type: "uuid", nullable: false),
                    equipment_type_id = table.Column<Guid>(type: "uuid", nullable: false),
                    operational_state = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_equipment", x => x.id);
                    table.ForeignKey(
                        name: "FK_equipment_areas_area_id",
                        column: x => x.area_id,
                        principalSchema: "organization",
                        principalTable: "areas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_equipment_equipment_groups_equipment_group_id",
                        column: x => x.equipment_group_id,
                        principalSchema: "equipment",
                        principalTable: "equipment_groups",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_equipment_equipment_types_equipment_type_id",
                        column: x => x.equipment_type_id,
                        principalSchema: "equipment",
                        principalTable: "equipment_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_equipment_lines_line_id",
                        column: x => x.line_id,
                        principalSchema: "organization",
                        principalTable: "lines",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "shifts",
                schema: "organization",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    fab_id = table.Column<Guid>(type: "uuid", nullable: false),
                    line_id = table.Column<Guid>(type: "uuid", nullable: true),
                    start_time = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    end_time = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    days_of_week = table.Column<string>(type: "jsonb", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_shifts", x => x.id);
                    table.ForeignKey(
                        name: "FK_shifts_fabs_fab_id",
                        column: x => x.fab_id,
                        principalSchema: "organization",
                        principalTable: "fabs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_shifts_lines_line_id",
                        column: x => x.line_id,
                        principalSchema: "organization",
                        principalTable: "lines",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "workstations",
                schema: "organization",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    line_id = table.Column<Guid>(type: "uuid", nullable: true),
                    area_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_workstations", x => x.id);
                    table.ForeignKey(
                        name: "FK_workstations_areas_area_id",
                        column: x => x.area_id,
                        principalSchema: "organization",
                        principalTable: "areas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_workstations_lines_line_id",
                        column: x => x.line_id,
                        principalSchema: "organization",
                        principalTable: "lines",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "carriers",
                schema: "production",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    fab_id = table.Column<Guid>(type: "uuid", nullable: false),
                    carrier_code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    carrier_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    slot_capacity = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    current_location_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_carriers", x => x.id);
                    table.CheckConstraint("ck_carriers_1", "slot_capacity > 0");
                    table.ForeignKey(
                        name: "FK_carriers_fabs_fab_id",
                        column: x => x.fab_id,
                        principalSchema: "organization",
                        principalTable: "fabs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_carriers_locations_current_location_id",
                        column: x => x.current_location_id,
                        principalSchema: "organization",
                        principalTable: "locations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "attachments",
                schema: "audit",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    entity_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    entity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    original_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    storage_path = table.Column<string>(type: "text", nullable: false),
                    mime_type = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    file_size = table.Column<long>(type: "bigint", nullable: false),
                    checksum = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    uploaded_by_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_attachments", x => x.id);
                    table.CheckConstraint("ck_attachments_1", "file_size >= 0");
                    table.ForeignKey(
                        name: "FK_attachments_users_uploaded_by_id",
                        column: x => x.uploaded_by_id,
                        principalSchema: "security",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "audit_logs",
                schema: "audit",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    entity_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    entity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    action = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    before_state = table.Column<string>(type: "jsonb", nullable: true),
                    after_state = table.Column<string>(type: "jsonb", nullable: true),
                    ip_address = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    user_agent = table.Column<string>(type: "text", nullable: true),
                    occurred_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    correlation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_channel = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    reason_code_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_audit_logs", x => x.id);
                    table.ForeignKey(
                        name: "FK_audit_logs_reason_codes_reason_code_id",
                        column: x => x.reason_code_id,
                        principalSchema: "quality",
                        principalTable: "reason_codes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_audit_logs_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "security",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "business_events",
                schema: "audit",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    event_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    entity_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    entity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    payload = table.Column<string>(type: "jsonb", nullable: false),
                    occurred_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    correlation_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_business_events", x => x.id);
                    table.ForeignKey(
                        name: "FK_business_events_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "security",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "command_receipts",
                schema: "audit",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    scope = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    idempotency_key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    request_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    actor_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    correlation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    result = table.Column<string>(type: "jsonb", nullable: true),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_command_receipts", x => x.id);
                    table.ForeignKey(
                        name: "FK_command_receipts_users_actor_user_id",
                        column: x => x.actor_user_id,
                        principalSchema: "security",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "engineering_documents",
                schema: "engineering",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    entity_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    entity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    original_filename = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    document_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    mime_type = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    file_size = table.Column<long>(type: "bigint", nullable: false),
                    revision = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    checksum = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    storage_path = table.Column<string>(type: "text", nullable: false),
                    lifecycle_status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    effective_from = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    effective_to = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    released_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    released_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    uploaded_by_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_engineering_documents", x => x.id);
                    table.CheckConstraint("ck_engineering_documents_1", "file_size >= 0");
                    table.ForeignKey(
                        name: "FK_engineering_documents_users_released_by_id",
                        column: x => x.released_by_id,
                        principalSchema: "security",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_engineering_documents_users_uploaded_by_id",
                        column: x => x.uploaded_by_id,
                        principalSchema: "security",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "genealogy_events",
                schema: "production",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    event_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    reason = table.Column<string>(type: "text", nullable: false),
                    occurred_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    performed_by_id = table.Column<Guid>(type: "uuid", nullable: false),
                    idempotency_key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    correlation_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_genealogy_events", x => x.id);
                    table.CheckConstraint("ck_genealogy_events_1", "event_type IN ('Split', 'Merge', 'Transfer', 'Create')");
                    table.ForeignKey(
                        name: "FK_genealogy_events_users_performed_by_id",
                        column: x => x.performed_by_id,
                        principalSchema: "security",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "import_jobs",
                schema: "integration",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    entity = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    mapping_id = table.Column<Guid>(type: "uuid", nullable: true),
                    original_filename = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    file_path = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    total_rows = table.Column<int>(type: "integer", nullable: false),
                    successful_rows = table.Column<int>(type: "integer", nullable: false),
                    failed_rows = table.Column<int>(type: "integer", nullable: false),
                    error_log = table.Column<string>(type: "jsonb", nullable: true),
                    started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    dry_run = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_import_jobs", x => x.id);
                    table.CheckConstraint("ck_import_jobs_1", "total_rows >= 0 AND successful_rows >= 0 AND failed_rows >= 0");
                    table.ForeignKey(
                        name: "FK_import_jobs_import_mappings_mapping_id",
                        column: x => x.mapping_id,
                        principalSchema: "integration",
                        principalTable: "import_mappings",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_import_jobs_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "security",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "recipe_versions",
                schema: "engineering",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    recipe_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    lifecycle_status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    checksum = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    released_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    released_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    parameters = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_recipe_versions", x => x.id);
                    table.ForeignKey(
                        name: "FK_recipe_versions_recipes_recipe_id",
                        column: x => x.recipe_id,
                        principalSchema: "engineering",
                        principalTable: "recipes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_recipe_versions_users_released_by_id",
                        column: x => x.released_by_id,
                        principalSchema: "security",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "route_versions",
                schema: "engineering",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    route_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    lifecycle_status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    effective_from = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    effective_to = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    released_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    released_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    change_reason = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_route_versions", x => x.id);
                    table.CheckConstraint("ck_route_versions_1", "effective_to IS NULL OR effective_from IS NULL OR effective_to > effective_from");
                    table.ForeignKey(
                        name: "FK_route_versions_routes_route_id",
                        column: x => x.route_id,
                        principalSchema: "engineering",
                        principalTable: "routes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_route_versions_users_released_by_id",
                        column: x => x.released_by_id,
                        principalSchema: "security",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "specification_versions",
                schema: "quality",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    parameter_code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    value_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    unit = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    spec_min = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: true),
                    spec_max = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: true),
                    lifecycle_status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    released_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    released_by_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_specification_versions", x => x.id);
                    table.CheckConstraint("ck_specification_versions_1", "version > 0");
                    table.CheckConstraint("ck_specification_versions_2", "spec_min IS NULL OR spec_max IS NULL OR spec_max >= spec_min");
                    table.ForeignKey(
                        name: "FK_specification_versions_users_released_by_id",
                        column: x => x.released_by_id,
                        principalSchema: "security",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "user_claims",
                schema: "security",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    claim_type = table.Column<string>(type: "text", nullable: true),
                    claim_value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_claims", x => x.id);
                    table.ForeignKey(
                        name: "FK_user_claims_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "security",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_fab_scopes",
                schema: "security",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    fab_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_fab_scopes", x => x.id);
                    table.ForeignKey(
                        name: "FK_user_fab_scopes_fabs_fab_id",
                        column: x => x.fab_id,
                        principalSchema: "organization",
                        principalTable: "fabs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_user_fab_scopes_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "security",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "user_logins",
                schema: "security",
                columns: table => new
                {
                    login_provider = table.Column<string>(type: "text", nullable: false),
                    provider_key = table.Column<string>(type: "text", nullable: false),
                    provider_display_name = table.Column<string>(type: "text", nullable: true),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_logins", x => new { x.login_provider, x.provider_key });
                    table.ForeignKey(
                        name: "FK_user_logins_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "security",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_permissions",
                schema: "security",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    permission_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_permissions", x => x.id);
                    table.ForeignKey(
                        name: "FK_user_permissions_permissions_permission_id",
                        column: x => x.permission_id,
                        principalSchema: "security",
                        principalTable: "permissions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_user_permissions_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "security",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "user_roles",
                schema: "security",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_roles", x => new { x.user_id, x.role_id });
                    table.ForeignKey(
                        name: "FK_user_roles_roles_role_id",
                        column: x => x.role_id,
                        principalSchema: "security",
                        principalTable: "roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_user_roles_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "security",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_tokens",
                schema: "security",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    login_provider = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_tokens", x => new { x.user_id, x.login_provider, x.name });
                    table.ForeignKey(
                        name: "FK_user_tokens_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "security",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "chambers",
                schema: "equipment",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    equipment_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_chambers", x => x.id);
                    table.ForeignKey(
                        name: "FK_chambers_equipment_equipment_id",
                        column: x => x.equipment_id,
                        principalSchema: "equipment",
                        principalTable: "equipment",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "equipment_states",
                schema: "equipment",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    equipment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    state = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ended_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    source = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    metadata = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_equipment_states", x => x.id);
                    table.CheckConstraint("ck_equipment_states_1", "ended_at IS NULL OR ended_at > started_at");
                    table.ForeignKey(
                        name: "FK_equipment_states_equipment_equipment_id",
                        column: x => x.equipment_id,
                        principalSchema: "equipment",
                        principalTable: "equipment",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "machine_connections",
                schema: "integration",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    equipment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    protocol = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    status_message = table.Column<string>(type: "text", nullable: true),
                    last_connected_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    config = table.Column<string>(type: "jsonb", nullable: true),
                    secret_reference = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_machine_connections", x => x.id);
                    table.ForeignKey(
                        name: "FK_machine_connections_equipment_equipment_id",
                        column: x => x.equipment_id,
                        principalSchema: "equipment",
                        principalTable: "equipment",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "maintenance_schedules",
                schema: "equipment",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    tool_id = table.Column<Guid>(type: "uuid", nullable: true),
                    equipment_id = table.Column<Guid>(type: "uuid", nullable: true),
                    event_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    assigned_to_id = table.Column<Guid>(type: "uuid", nullable: true),
                    frequency = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    interval_value = table.Column<int>(type: "integer", nullable: false),
                    last_executed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    next_due_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_maintenance_schedules", x => x.id);
                    table.CheckConstraint("ck_maintenance_schedules_1", "interval_value > 0");
                    table.CheckConstraint("ck_maintenance_schedules_2", "num_nonnulls(tool_id, equipment_id) = 1");
                    table.ForeignKey(
                        name: "FK_maintenance_schedules_equipment_equipment_id",
                        column: x => x.equipment_id,
                        principalSchema: "equipment",
                        principalTable: "equipment",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_maintenance_schedules_tools_tool_id",
                        column: x => x.tool_id,
                        principalSchema: "equipment",
                        principalTable: "tools",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_maintenance_schedules_users_assigned_to_id",
                        column: x => x.assigned_to_id,
                        principalSchema: "security",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "process_batches",
                schema: "production",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    equipment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_process_batches", x => x.id);
                    table.ForeignKey(
                        name: "FK_process_batches_equipment_equipment_id",
                        column: x => x.equipment_id,
                        principalSchema: "equipment",
                        principalTable: "equipment",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "downtimes",
                schema: "equipment",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    line_id = table.Column<Guid>(type: "uuid", nullable: true),
                    equipment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reason_code_id = table.Column<Guid>(type: "uuid", nullable: true),
                    shift_id = table.Column<Guid>(type: "uuid", nullable: true),
                    started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ended_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    notes = table.Column<string>(type: "text", nullable: true),
                    reported_by_id = table.Column<Guid>(type: "uuid", nullable: false),
                    needs_reason = table.Column<bool>(type: "boolean", nullable: false),
                    classified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    classified_by_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_downtimes", x => x.id);
                    table.CheckConstraint("ck_downtimes_1", "ended_at IS NULL OR ended_at >= started_at");
                    table.ForeignKey(
                        name: "FK_downtimes_equipment_equipment_id",
                        column: x => x.equipment_id,
                        principalSchema: "equipment",
                        principalTable: "equipment",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_downtimes_lines_line_id",
                        column: x => x.line_id,
                        principalSchema: "organization",
                        principalTable: "lines",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_downtimes_reason_codes_reason_code_id",
                        column: x => x.reason_code_id,
                        principalSchema: "quality",
                        principalTable: "reason_codes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_downtimes_shifts_shift_id",
                        column: x => x.shift_id,
                        principalSchema: "organization",
                        principalTable: "shifts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_downtimes_users_classified_by_id",
                        column: x => x.classified_by_id,
                        principalSchema: "security",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_downtimes_users_reported_by_id",
                        column: x => x.reported_by_id,
                        principalSchema: "security",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "shift_handovers",
                schema: "organization",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    shift_id = table.Column<Guid>(type: "uuid", nullable: false),
                    line_id = table.Column<Guid>(type: "uuid", nullable: true),
                    business_date = table.Column<DateOnly>(type: "date", nullable: false),
                    shift_start = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    shift_end = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    discrepancies = table.Column<string>(type: "jsonb", nullable: true),
                    notes = table.Column<string>(type: "text", nullable: true),
                    confirmed_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    confirmed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_shift_handovers", x => x.id);
                    table.CheckConstraint("ck_shift_handovers_1", "shift_end > shift_start");
                    table.ForeignKey(
                        name: "FK_shift_handovers_lines_line_id",
                        column: x => x.line_id,
                        principalSchema: "organization",
                        principalTable: "lines",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_shift_handovers_shifts_shift_id",
                        column: x => x.shift_id,
                        principalSchema: "organization",
                        principalTable: "shifts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_shift_handovers_users_confirmed_by_id",
                        column: x => x.confirmed_by_id,
                        principalSchema: "security",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "terminals",
                schema: "organization",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    device_uuid = table.Column<Guid>(type: "uuid", nullable: false),
                    workstation_id = table.Column<Guid>(type: "uuid", nullable: true),
                    last_seen_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    app_version = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_terminals", x => x.id);
                    table.ForeignKey(
                        name: "FK_terminals_workstations_workstation_id",
                        column: x => x.workstation_id,
                        principalSchema: "organization",
                        principalTable: "workstations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "carrier_movements",
                schema: "production",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    carrier_id = table.Column<Guid>(type: "uuid", nullable: false),
                    from_location_id = table.Column<Guid>(type: "uuid", nullable: true),
                    to_location_id = table.Column<Guid>(type: "uuid", nullable: false),
                    moved_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    performed_by_id = table.Column<Guid>(type: "uuid", nullable: false),
                    notes = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_carrier_movements", x => x.id);
                    table.ForeignKey(
                        name: "FK_carrier_movements_carriers_carrier_id",
                        column: x => x.carrier_id,
                        principalSchema: "production",
                        principalTable: "carriers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_carrier_movements_locations_from_location_id",
                        column: x => x.from_location_id,
                        principalSchema: "organization",
                        principalTable: "locations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_carrier_movements_locations_to_location_id",
                        column: x => x.to_location_id,
                        principalSchema: "organization",
                        principalTable: "locations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_carrier_movements_users_performed_by_id",
                        column: x => x.performed_by_id,
                        principalSchema: "security",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "carrier_slots",
                schema: "production",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    carrier_id = table.Column<Guid>(type: "uuid", nullable: false),
                    slot_no = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_carrier_slots", x => x.id);
                    table.CheckConstraint("ck_carrier_slots_1", "slot_no > 0");
                    table.ForeignKey(
                        name: "FK_carrier_slots_carriers_carrier_id",
                        column: x => x.carrier_id,
                        principalSchema: "production",
                        principalTable: "carriers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "equipment_qualifications",
                schema: "equipment",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    equipment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    operation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    recipe_version_id = table.Column<Guid>(type: "uuid", nullable: true),
                    qualification_status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    valid_from = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    valid_to = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    qualified_by_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_equipment_qualifications", x => x.id);
                    table.CheckConstraint("ck_equipment_qualifications_1", "valid_to IS NULL OR valid_to > valid_from");
                    table.ForeignKey(
                        name: "FK_equipment_qualifications_equipment_equipment_id",
                        column: x => x.equipment_id,
                        principalSchema: "equipment",
                        principalTable: "equipment",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_equipment_qualifications_operations_operation_id",
                        column: x => x.operation_id,
                        principalSchema: "engineering",
                        principalTable: "operations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_equipment_qualifications_recipe_versions_recipe_version_id",
                        column: x => x.recipe_version_id,
                        principalSchema: "engineering",
                        principalTable: "recipe_versions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_equipment_qualifications_users_qualified_by_id",
                        column: x => x.qualified_by_id,
                        principalSchema: "security",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "worker_qualifications",
                schema: "equipment",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    worker_id = table.Column<Guid>(type: "uuid", nullable: false),
                    operation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    recipe_version_id = table.Column<Guid>(type: "uuid", nullable: true),
                    qualification_status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    valid_from = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    valid_to = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    qualified_by_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_worker_qualifications", x => x.id);
                    table.CheckConstraint("ck_worker_qualifications_1", "valid_to IS NULL OR valid_to > valid_from");
                    table.ForeignKey(
                        name: "FK_worker_qualifications_operations_operation_id",
                        column: x => x.operation_id,
                        principalSchema: "engineering",
                        principalTable: "operations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_worker_qualifications_recipe_versions_recipe_version_id",
                        column: x => x.recipe_version_id,
                        principalSchema: "engineering",
                        principalTable: "recipe_versions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_worker_qualifications_users_qualified_by_id",
                        column: x => x.qualified_by_id,
                        principalSchema: "security",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_worker_qualifications_workers_worker_id",
                        column: x => x.worker_id,
                        principalSchema: "organization",
                        principalTable: "workers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "product_revisions",
                schema: "engineering",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    revision_code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    lifecycle_status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    route_version_id = table.Column<Guid>(type: "uuid", nullable: true),
                    change_reason = table.Column<string>(type: "text", nullable: true),
                    external_ref = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    effective_from = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    effective_to = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    released_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    obsolete_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    released_by_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product_revisions", x => x.id);
                    table.UniqueConstraint("AK_product_revisions_id_product_id", x => new { x.id, x.product_id });
                    table.CheckConstraint("ck_product_revisions_1", "effective_to IS NULL OR effective_from IS NULL OR effective_to > effective_from");
                    table.ForeignKey(
                        name: "FK_product_revisions_products_product_id",
                        column: x => x.product_id,
                        principalSchema: "engineering",
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_product_revisions_route_versions_route_version_id",
                        column: x => x.route_version_id,
                        principalSchema: "engineering",
                        principalTable: "route_versions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_product_revisions_users_released_by_id",
                        column: x => x.released_by_id,
                        principalSchema: "security",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "route_steps",
                schema: "engineering",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    route_version_id = table.Column<Guid>(type: "uuid", nullable: false),
                    operation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sequence_no = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    instruction = table.Column<string>(type: "text", nullable: true),
                    equipment_type_id = table.Column<Guid>(type: "uuid", nullable: true),
                    requires_confirmation = table.Column<bool>(type: "boolean", nullable: false),
                    parameters = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_route_steps", x => x.id);
                    table.UniqueConstraint("AK_route_steps_id_route_version_id", x => new { x.id, x.route_version_id });
                    table.CheckConstraint("ck_route_steps_1", "sequence_no > 0");
                    table.ForeignKey(
                        name: "FK_route_steps_equipment_types_equipment_type_id",
                        column: x => x.equipment_type_id,
                        principalSchema: "equipment",
                        principalTable: "equipment_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_route_steps_operations_operation_id",
                        column: x => x.operation_id,
                        principalSchema: "engineering",
                        principalTable: "operations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_route_steps_route_versions_route_version_id",
                        column: x => x.route_version_id,
                        principalSchema: "engineering",
                        principalTable: "route_versions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "equipment_events",
                schema: "equipment",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    equipment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    machine_connection_id = table.Column<Guid>(type: "uuid", nullable: true),
                    event_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    state_from = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    state_to = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    payload = table.Column<string>(type: "jsonb", nullable: true),
                    event_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    received_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    correlation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_simulated = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_equipment_events", x => x.id);
                    table.ForeignKey(
                        name: "FK_equipment_events_equipment_equipment_id",
                        column: x => x.equipment_id,
                        principalSchema: "equipment",
                        principalTable: "equipment",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_equipment_events_machine_connections_machine_connection_id",
                        column: x => x.machine_connection_id,
                        principalSchema: "integration",
                        principalTable: "machine_connections",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "machine_messages",
                schema: "integration",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    machine_connection_id = table.Column<Guid>(type: "uuid", nullable: false),
                    topic = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    raw_payload = table.Column<string>(type: "text", nullable: false),
                    parsed_data = table.Column<string>(type: "jsonb", nullable: true),
                    processing_status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    processing_error = table.Column<string>(type: "text", nullable: true),
                    received_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    source_message_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_machine_messages", x => x.id);
                    table.ForeignKey(
                        name: "FK_machine_messages_machine_connections_machine_connection_id",
                        column: x => x.machine_connection_id,
                        principalSchema: "integration",
                        principalTable: "machine_connections",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "machine_tags",
                schema: "integration",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    machine_connection_id = table.Column<Guid>(type: "uuid", nullable: false),
                    equipment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    address = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    signal_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    data_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    transform = table.Column<string>(type: "jsonb", nullable: true),
                    unit = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_machine_tags", x => x.id);
                    table.ForeignKey(
                        name: "FK_machine_tags_equipment_equipment_id",
                        column: x => x.equipment_id,
                        principalSchema: "equipment",
                        principalTable: "equipment",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_machine_tags_machine_connections_machine_connection_id",
                        column: x => x.machine_connection_id,
                        principalSchema: "integration",
                        principalTable: "machine_connections",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "maintenance_events",
                schema: "equipment",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    event_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    tool_id = table.Column<Guid>(type: "uuid", nullable: true),
                    equipment_id = table.Column<Guid>(type: "uuid", nullable: true),
                    schedule_id = table.Column<Guid>(type: "uuid", nullable: true),
                    assigned_to_id = table.Column<Guid>(type: "uuid", nullable: true),
                    scheduled_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    resolution_notes = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_maintenance_events", x => x.id);
                    table.CheckConstraint("ck_maintenance_events_1", "num_nonnulls(tool_id, equipment_id) = 1");
                    table.CheckConstraint("ck_maintenance_events_2", "completed_at IS NULL OR started_at IS NULL OR completed_at >= started_at");
                    table.ForeignKey(
                        name: "FK_maintenance_events_equipment_equipment_id",
                        column: x => x.equipment_id,
                        principalSchema: "equipment",
                        principalTable: "equipment",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_maintenance_events_maintenance_schedules_schedule_id",
                        column: x => x.schedule_id,
                        principalSchema: "equipment",
                        principalTable: "maintenance_schedules",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_maintenance_events_tools_tool_id",
                        column: x => x.tool_id,
                        principalSchema: "equipment",
                        principalTable: "tools",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_maintenance_events_users_assigned_to_id",
                        column: x => x.assigned_to_id,
                        principalSchema: "security",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "work_orders",
                schema: "production",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    order_no = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    fab_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_revision_id = table.Column<Guid>(type: "uuid", nullable: false),
                    planned_qty = table.Column<int>(type: "integer", nullable: false),
                    produced_qty = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    priority = table.Column<int>(type: "integer", nullable: false),
                    due_date = table.Column<DateOnly>(type: "date", nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    planned_start_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    planned_end_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: true),
                    customer_order_no = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    process_snapshot = table.Column<string>(type: "jsonb", nullable: true),
                    snapshot_version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_work_orders", x => x.id);
                    table.CheckConstraint("ck_work_orders_1", "planned_qty >= 0 AND produced_qty >= 0");
                    table.CheckConstraint("ck_work_orders_2", "planned_end_at IS NULL OR planned_start_at IS NULL OR planned_end_at >= planned_start_at");
                    table.ForeignKey(
                        name: "FK_work_orders_companies_customer_id",
                        column: x => x.customer_id,
                        principalSchema: "materials",
                        principalTable: "companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_work_orders_fabs_fab_id",
                        column: x => x.fab_id,
                        principalSchema: "organization",
                        principalTable: "fabs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_work_orders_product_revisions_product_revision_id",
                        column: x => x.product_revision_id,
                        principalSchema: "engineering",
                        principalTable: "product_revisions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_work_orders_product_revisions_product_revision_id_product_id",
                        columns: x => new { x.product_revision_id, x.product_id },
                        principalSchema: "engineering",
                        principalTable: "product_revisions",
                        principalColumns: new[] { "id", "product_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_work_orders_products_product_id",
                        column: x => x.product_id,
                        principalSchema: "engineering",
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "bom_items",
                schema: "materials",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    route_version_id = table.Column<Guid>(type: "uuid", nullable: false),
                    route_step_id = table.Column<Guid>(type: "uuid", nullable: true),
                    material_id = table.Column<Guid>(type: "uuid", nullable: false),
                    quantity_per_unit = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    per = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    scrap_percentage = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    notes = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bom_items", x => x.id);
                    table.CheckConstraint("ck_bom_items_1", "quantity_per_unit >= 0 AND scrap_percentage BETWEEN 0 AND 100");
                    table.ForeignKey(
                        name: "FK_bom_items_materials_material_id",
                        column: x => x.material_id,
                        principalSchema: "materials",
                        principalTable: "materials",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_bom_items_route_steps_route_step_id",
                        column: x => x.route_step_id,
                        principalSchema: "engineering",
                        principalTable: "route_steps",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_bom_items_route_versions_route_version_id",
                        column: x => x.route_version_id,
                        principalSchema: "engineering",
                        principalTable: "route_versions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "checklist_items",
                schema: "engineering",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    route_step_id = table.Column<Guid>(type: "uuid", nullable: false),
                    label = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    is_required = table.Column<bool>(type: "boolean", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_checklist_items", x => x.id);
                    table.ForeignKey(
                        name: "FK_checklist_items_route_steps_route_step_id",
                        column: x => x.route_step_id,
                        principalSchema: "engineering",
                        principalTable: "route_steps",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "output_definitions",
                schema: "engineering",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    route_step_id = table.Column<Guid>(type: "uuid", nullable: false),
                    key = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    label = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    value_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    unit = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    options = table.Column<string>(type: "jsonb", nullable: true),
                    is_required = table.Column<bool>(type: "boolean", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_output_definitions", x => x.id);
                    table.ForeignKey(
                        name: "FK_output_definitions_route_steps_route_step_id",
                        column: x => x.route_step_id,
                        principalSchema: "engineering",
                        principalTable: "route_steps",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "q_time_rules",
                schema: "engineering",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    start_route_step_id = table.Column<Guid>(type: "uuid", nullable: false),
                    end_route_step_id = table.Column<Guid>(type: "uuid", nullable: false),
                    start_event_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    end_event_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    limit_minutes = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_q_time_rules", x => x.id);
                    table.CheckConstraint("ck_q_time_rules_1", "limit_minutes > 0");
                    table.ForeignKey(
                        name: "FK_q_time_rules_route_steps_end_route_step_id",
                        column: x => x.end_route_step_id,
                        principalSchema: "engineering",
                        principalTable: "route_steps",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_q_time_rules_route_steps_start_route_step_id",
                        column: x => x.start_route_step_id,
                        principalSchema: "engineering",
                        principalTable: "route_steps",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "quality_check_templates",
                schema: "quality",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    route_version_id = table.Column<Guid>(type: "uuid", nullable: false),
                    route_step_id = table.Column<Guid>(type: "uuid", nullable: true),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    min_checks_per_batch = table.Column<int>(type: "integer", nullable: false),
                    samples_per_check = table.Column<int>(type: "integer", nullable: false),
                    parameters = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_quality_check_templates", x => x.id);
                    table.CheckConstraint("ck_quality_check_templates_1", "version > 0 AND min_checks_per_batch >= 0 AND samples_per_check > 0");
                    table.ForeignKey(
                        name: "FK_quality_check_templates_route_steps_route_step_id",
                        column: x => x.route_step_id,
                        principalSchema: "engineering",
                        principalTable: "route_steps",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_quality_check_templates_route_versions_route_version_id",
                        column: x => x.route_version_id,
                        principalSchema: "engineering",
                        principalTable: "route_versions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "recipe_applicabilities",
                schema: "engineering",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    recipe_version_id = table.Column<Guid>(type: "uuid", nullable: false),
                    route_step_id = table.Column<Guid>(type: "uuid", nullable: false),
                    equipment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    effective_from = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    effective_to = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_recipe_applicabilities", x => x.id);
                    table.CheckConstraint("ck_recipe_applicabilities_1", "effective_to IS NULL OR effective_to > effective_from");
                    table.ForeignKey(
                        name: "FK_recipe_applicabilities_equipment_equipment_id",
                        column: x => x.equipment_id,
                        principalSchema: "equipment",
                        principalTable: "equipment",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_recipe_applicabilities_recipe_versions_recipe_version_id",
                        column: x => x.recipe_version_id,
                        principalSchema: "engineering",
                        principalTable: "recipe_versions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_recipe_applicabilities_route_steps_route_step_id",
                        column: x => x.route_step_id,
                        principalSchema: "engineering",
                        principalTable: "route_steps",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "lots",
                schema: "production",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    fab_id = table.Column<Guid>(type: "uuid", nullable: false),
                    work_order_id = table.Column<Guid>(type: "uuid", nullable: true),
                    lot_number = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    legacy_batch_number = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    lot_assigned_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    product_revision_id = table.Column<Guid>(type: "uuid", nullable: false),
                    route_version_id = table.Column<Guid>(type: "uuid", nullable: false),
                    current_route_step_id = table.Column<Guid>(type: "uuid", nullable: true),
                    lot_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    priority = table.Column<int>(type: "integer", nullable: false),
                    target_qty = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    released_to_route_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    released_to_route_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    notes = table.Column<string>(type: "text", nullable: true),
                    snapshot_version = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_lots", x => x.id);
                    table.UniqueConstraint("AK_lots_id_route_version_id", x => new { x.id, x.route_version_id });
                    table.CheckConstraint("ck_lots_1", "target_qty >= 0");
                    table.CheckConstraint("ck_lots_2", "completed_at IS NULL OR started_at IS NULL OR completed_at >= started_at");
                    table.ForeignKey(
                        name: "FK_lots_fabs_fab_id",
                        column: x => x.fab_id,
                        principalSchema: "organization",
                        principalTable: "fabs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_lots_product_revisions_product_revision_id",
                        column: x => x.product_revision_id,
                        principalSchema: "engineering",
                        principalTable: "product_revisions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_lots_route_steps_current_route_step_id",
                        column: x => x.current_route_step_id,
                        principalSchema: "engineering",
                        principalTable: "route_steps",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_lots_route_steps_current_route_step_id_route_version_id",
                        columns: x => new { x.current_route_step_id, x.route_version_id },
                        principalSchema: "engineering",
                        principalTable: "route_steps",
                        principalColumns: new[] { "id", "route_version_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_lots_route_versions_route_version_id",
                        column: x => x.route_version_id,
                        principalSchema: "engineering",
                        principalTable: "route_versions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_lots_users_released_to_route_by_id",
                        column: x => x.released_to_route_by_id,
                        principalSchema: "security",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_lots_work_orders_work_order_id",
                        column: x => x.work_order_id,
                        principalSchema: "production",
                        principalTable: "work_orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "quality_triggers",
                schema: "quality",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    trigger_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    quality_check_template_id = table.Column<Guid>(type: "uuid", nullable: false),
                    equipment_id = table.Column<Guid>(type: "uuid", nullable: true),
                    product_id = table.Column<Guid>(type: "uuid", nullable: true),
                    threshold_n = table.Column<int>(type: "integer", nullable: true),
                    downtime_min_minutes = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: true),
                    is_blocking = table.Column<bool>(type: "boolean", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_quality_triggers", x => x.id);
                    table.ForeignKey(
                        name: "FK_quality_triggers_equipment_equipment_id",
                        column: x => x.equipment_id,
                        principalSchema: "equipment",
                        principalTable: "equipment",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_quality_triggers_products_product_id",
                        column: x => x.product_id,
                        principalSchema: "engineering",
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_quality_triggers_quality_check_templates_quality_check_temp~",
                        column: x => x.quality_check_template_id,
                        principalSchema: "quality",
                        principalTable: "quality_check_templates",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "q_time_windows",
                schema: "production",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    q_time_rule_id = table.Column<Guid>(type: "uuid", nullable: false),
                    lot_id = table.Column<Guid>(type: "uuid", nullable: false),
                    started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    deadline_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ended_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    violated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_q_time_windows", x => x.id);
                    table.CheckConstraint("ck_q_time_windows_1", "deadline_at > started_at");
                    table.CheckConstraint("ck_q_time_windows_2", "ended_at IS NULL OR ended_at >= started_at");
                    table.ForeignKey(
                        name: "FK_q_time_windows_lots_lot_id",
                        column: x => x.lot_id,
                        principalSchema: "production",
                        principalTable: "lots",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_q_time_windows_q_time_rules_q_time_rule_id",
                        column: x => x.q_time_rule_id,
                        principalSchema: "engineering",
                        principalTable: "q_time_rules",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "step_executions",
                schema: "production",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    lot_id = table.Column<Guid>(type: "uuid", nullable: false),
                    route_version_id = table.Column<Guid>(type: "uuid", nullable: false),
                    route_step_id = table.Column<Guid>(type: "uuid", nullable: false),
                    visit_no = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    instruction = table.Column<string>(type: "text", nullable: true),
                    entered_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    exited_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    confirmed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    confirmed_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    requires_confirmation = table.Column<bool>(type: "boolean", nullable: false),
                    assigned_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    assigned_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    passed_qty = table.Column<int>(type: "integer", nullable: false),
                    scrap_qty = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_step_executions", x => x.id);
                    table.CheckConstraint("ck_step_executions_1", "visit_no > 0 AND passed_qty >= 0 AND scrap_qty >= 0");
                    table.CheckConstraint("ck_step_executions_2", "exited_at IS NULL OR exited_at >= entered_at");
                    table.ForeignKey(
                        name: "FK_step_executions_lots_lot_id",
                        column: x => x.lot_id,
                        principalSchema: "production",
                        principalTable: "lots",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_step_executions_lots_lot_id_route_version_id",
                        columns: x => new { x.lot_id, x.route_version_id },
                        principalSchema: "production",
                        principalTable: "lots",
                        principalColumns: new[] { "id", "route_version_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_step_executions_route_steps_route_step_id",
                        column: x => x.route_step_id,
                        principalSchema: "engineering",
                        principalTable: "route_steps",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_step_executions_route_steps_route_step_id_route_version_id",
                        columns: x => new { x.route_step_id, x.route_version_id },
                        principalSchema: "engineering",
                        principalTable: "route_steps",
                        principalColumns: new[] { "id", "route_version_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_step_executions_route_versions_route_version_id",
                        column: x => x.route_version_id,
                        principalSchema: "engineering",
                        principalTable: "route_versions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_step_executions_users_assigned_by_id",
                        column: x => x.assigned_by_id,
                        principalSchema: "security",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_step_executions_users_confirmed_by_id",
                        column: x => x.confirmed_by_id,
                        principalSchema: "security",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "stock_documents",
                schema: "materials",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    document_no = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    document_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    warehouse_id = table.Column<Guid>(type: "uuid", nullable: false),
                    work_order_id = table.Column<Guid>(type: "uuid", nullable: true),
                    lot_id = table.Column<Guid>(type: "uuid", nullable: true),
                    posted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    posted_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    erp_reference = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    erp_synced_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    notes = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stock_documents", x => x.id);
                    table.ForeignKey(
                        name: "FK_stock_documents_lots_lot_id",
                        column: x => x.lot_id,
                        principalSchema: "production",
                        principalTable: "lots",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_stock_documents_users_posted_by_id",
                        column: x => x.posted_by_id,
                        principalSchema: "security",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_stock_documents_warehouses_warehouse_id",
                        column: x => x.warehouse_id,
                        principalSchema: "materials",
                        principalTable: "warehouses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_stock_documents_work_orders_work_order_id",
                        column: x => x.work_order_id,
                        principalSchema: "production",
                        principalTable: "work_orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "work_order_change_requests",
                schema: "production",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    work_order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reason = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    proposed = table.Column<string>(type: "jsonb", nullable: false),
                    previous_values = table.Column<string>(type: "jsonb", nullable: false),
                    impact = table.Column<string>(type: "jsonb", nullable: true),
                    effective_from = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    effective_from_lot_id = table.Column<Guid>(type: "uuid", nullable: true),
                    requested_by_id = table.Column<Guid>(type: "uuid", nullable: false),
                    submitted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    approved_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    approved_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    rejected_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    rejected_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    rejection_reason = table.Column<string>(type: "text", nullable: true),
                    applied_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    applied_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    resulting_snapshot_version = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_work_order_change_requests", x => x.id);
                    table.ForeignKey(
                        name: "FK_work_order_change_requests_lots_effective_from_lot_id",
                        column: x => x.effective_from_lot_id,
                        principalSchema: "production",
                        principalTable: "lots",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_work_order_change_requests_users_applied_by_id",
                        column: x => x.applied_by_id,
                        principalSchema: "security",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_work_order_change_requests_users_approved_by_id",
                        column: x => x.approved_by_id,
                        principalSchema: "security",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_work_order_change_requests_users_rejected_by_id",
                        column: x => x.rejected_by_id,
                        principalSchema: "security",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_work_order_change_requests_users_requested_by_id",
                        column: x => x.requested_by_id,
                        principalSchema: "security",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_work_order_change_requests_work_orders_work_order_id",
                        column: x => x.work_order_id,
                        principalSchema: "production",
                        principalTable: "work_orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "checklist_completions",
                schema: "production",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    step_execution_id = table.Column<Guid>(type: "uuid", nullable: false),
                    checklist_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    checked_by_id = table.Column<Guid>(type: "uuid", nullable: false),
                    checked_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_checklist_completions", x => x.id);
                    table.ForeignKey(
                        name: "FK_checklist_completions_checklist_items_checklist_item_id",
                        column: x => x.checklist_item_id,
                        principalSchema: "engineering",
                        principalTable: "checklist_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_checklist_completions_step_executions_step_execution_id",
                        column: x => x.step_execution_id,
                        principalSchema: "production",
                        principalTable: "step_executions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_checklist_completions_users_checked_by_id",
                        column: x => x.checked_by_id,
                        principalSchema: "security",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "material_allocations",
                schema: "materials",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    lot_id = table.Column<Guid>(type: "uuid", nullable: true),
                    step_execution_id = table.Column<Guid>(type: "uuid", nullable: true),
                    material_id = table.Column<Guid>(type: "uuid", nullable: false),
                    work_order_id = table.Column<Guid>(type: "uuid", nullable: true),
                    allocated_qty = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    returned_qty = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    expected_qty = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    consumed_qty = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    adjustment_qty = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    scrap_qty = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    allocated_by_id = table.Column<Guid>(type: "uuid", nullable: false),
                    allocated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    consumed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    consumption_warehouse_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_material_allocations", x => x.id);
                    table.CheckConstraint("ck_material_allocations_1", "allocated_qty >= 0 AND returned_qty >= 0 AND expected_qty >= 0 AND consumed_qty >= 0 AND scrap_qty >= 0");
                    table.ForeignKey(
                        name: "FK_material_allocations_lots_lot_id",
                        column: x => x.lot_id,
                        principalSchema: "production",
                        principalTable: "lots",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_material_allocations_materials_material_id",
                        column: x => x.material_id,
                        principalSchema: "materials",
                        principalTable: "materials",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_material_allocations_step_executions_step_execution_id",
                        column: x => x.step_execution_id,
                        principalSchema: "production",
                        principalTable: "step_executions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_material_allocations_users_allocated_by_id",
                        column: x => x.allocated_by_id,
                        principalSchema: "security",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_material_allocations_warehouses_consumption_warehouse_id",
                        column: x => x.consumption_warehouse_id,
                        principalSchema: "materials",
                        principalTable: "warehouses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_material_allocations_work_orders_work_order_id",
                        column: x => x.work_order_id,
                        principalSchema: "production",
                        principalTable: "work_orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "output_values",
                schema: "production",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    step_execution_id = table.Column<Guid>(type: "uuid", nullable: false),
                    output_id = table.Column<Guid>(type: "uuid", nullable: false),
                    value_text = table.Column<string>(type: "text", nullable: true),
                    value_number = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: true),
                    value_boolean = table.Column<bool>(type: "boolean", nullable: true),
                    value_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    recorded_by_id = table.Column<Guid>(type: "uuid", nullable: false),
                    recorded_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_output_values", x => x.id);
                    table.CheckConstraint("ck_output_values_1", "num_nonnulls(value_text, value_number, value_boolean, value_date) = 1");
                    table.ForeignKey(
                        name: "FK_output_values_output_definitions_output_id",
                        column: x => x.output_id,
                        principalSchema: "engineering",
                        principalTable: "output_definitions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_output_values_step_executions_step_execution_id",
                        column: x => x.step_execution_id,
                        principalSchema: "production",
                        principalTable: "step_executions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_output_values_users_recorded_by_id",
                        column: x => x.recorded_by_id,
                        principalSchema: "security",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "process_confirmations",
                schema: "production",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    lot_id = table.Column<Guid>(type: "uuid", nullable: false),
                    step_execution_id = table.Column<Guid>(type: "uuid", nullable: true),
                    confirmed_by_id = table.Column<Guid>(type: "uuid", nullable: false),
                    confirmed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    confirmation_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    notes = table.Column<string>(type: "text", nullable: true),
                    value = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_process_confirmations", x => x.id);
                    table.ForeignKey(
                        name: "FK_process_confirmations_lots_lot_id",
                        column: x => x.lot_id,
                        principalSchema: "production",
                        principalTable: "lots",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_process_confirmations_step_executions_step_execution_id",
                        column: x => x.step_execution_id,
                        principalSchema: "production",
                        principalTable: "step_executions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_process_confirmations_users_confirmed_by_id",
                        column: x => x.confirmed_by_id,
                        principalSchema: "security",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "process_runs",
                schema: "production",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    step_execution_id = table.Column<Guid>(type: "uuid", nullable: false),
                    equipment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    chamber_id = table.Column<Guid>(type: "uuid", nullable: true),
                    recipe_version_id = table.Column<Guid>(type: "uuid", nullable: false),
                    track_in_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    track_out_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    track_in_by_id = table.Column<Guid>(type: "uuid", nullable: false),
                    track_out_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_process_runs", x => x.id);
                    table.CheckConstraint("ck_process_runs_1", "track_out_at IS NULL OR track_out_at >= track_in_at");
                    table.CheckConstraint("ck_process_runs_2", "status IN ('Running', 'Completed', 'Aborted')");
                    table.CheckConstraint("ck_process_runs_3", "(status = 'Running' AND track_out_at IS NULL AND track_out_by_id IS NULL) OR (status <> 'Running' AND track_out_at IS NOT NULL AND track_out_by_id IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_process_runs_chambers_chamber_id",
                        column: x => x.chamber_id,
                        principalSchema: "equipment",
                        principalTable: "chambers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_process_runs_equipment_equipment_id",
                        column: x => x.equipment_id,
                        principalSchema: "equipment",
                        principalTable: "equipment",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_process_runs_recipe_versions_recipe_version_id",
                        column: x => x.recipe_version_id,
                        principalSchema: "engineering",
                        principalTable: "recipe_versions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_process_runs_step_executions_step_execution_id",
                        column: x => x.step_execution_id,
                        principalSchema: "production",
                        principalTable: "step_executions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_process_runs_users_track_in_by_id",
                        column: x => x.track_in_by_id,
                        principalSchema: "security",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_process_runs_users_track_out_by_id",
                        column: x => x.track_out_by_id,
                        principalSchema: "security",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "rework_records",
                schema: "production",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    lot_id = table.Column<Guid>(type: "uuid", nullable: false),
                    original_step_execution_id = table.Column<Guid>(type: "uuid", nullable: false),
                    rework_route_version_id = table.Column<Guid>(type: "uuid", nullable: false),
                    return_route_step_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reason_code_id = table.Column<Guid>(type: "uuid", nullable: false),
                    approved_by_id = table.Column<Guid>(type: "uuid", nullable: false),
                    approved_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rework_records", x => x.id);
                    table.ForeignKey(
                        name: "FK_rework_records_lots_lot_id",
                        column: x => x.lot_id,
                        principalSchema: "production",
                        principalTable: "lots",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_rework_records_reason_codes_reason_code_id",
                        column: x => x.reason_code_id,
                        principalSchema: "quality",
                        principalTable: "reason_codes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_rework_records_route_steps_return_route_step_id",
                        column: x => x.return_route_step_id,
                        principalSchema: "engineering",
                        principalTable: "route_steps",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_rework_records_route_versions_rework_route_version_id",
                        column: x => x.rework_route_version_id,
                        principalSchema: "engineering",
                        principalTable: "route_versions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_rework_records_step_executions_original_step_execution_id",
                        column: x => x.original_step_execution_id,
                        principalSchema: "production",
                        principalTable: "step_executions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_rework_records_users_approved_by_id",
                        column: x => x.approved_by_id,
                        principalSchema: "security",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "scrap_entries",
                schema: "quality",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    work_order_id = table.Column<Guid>(type: "uuid", nullable: true),
                    step_execution_id = table.Column<Guid>(type: "uuid", nullable: true),
                    reason_code_id = table.Column<Guid>(type: "uuid", nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    shift_id = table.Column<Guid>(type: "uuid", nullable: true),
                    notes = table.Column<string>(type: "text", nullable: true),
                    reported_by_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reported_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_scrap_entries", x => x.id);
                    table.CheckConstraint("ck_scrap_entries_1", "quantity > 0");
                    table.ForeignKey(
                        name: "FK_scrap_entries_reason_codes_reason_code_id",
                        column: x => x.reason_code_id,
                        principalSchema: "quality",
                        principalTable: "reason_codes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_scrap_entries_shifts_shift_id",
                        column: x => x.shift_id,
                        principalSchema: "organization",
                        principalTable: "shifts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_scrap_entries_step_executions_step_execution_id",
                        column: x => x.step_execution_id,
                        principalSchema: "production",
                        principalTable: "step_executions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_scrap_entries_users_reported_by_id",
                        column: x => x.reported_by_id,
                        principalSchema: "security",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_scrap_entries_work_orders_work_order_id",
                        column: x => x.work_order_id,
                        principalSchema: "production",
                        principalTable: "work_orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "step_documents",
                schema: "production",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    step_execution_id = table.Column<Guid>(type: "uuid", nullable: false),
                    engineering_document_id = table.Column<Guid>(type: "uuid", nullable: true),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    reference = table.Column<string>(type: "text", nullable: true),
                    file_path = table.Column<string>(type: "text", nullable: true),
                    is_mandatory = table.Column<bool>(type: "boolean", nullable: false),
                    requires_validation = table.Column<bool>(type: "boolean", nullable: false),
                    validated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    validated_by_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_step_documents", x => x.id);
                    table.ForeignKey(
                        name: "FK_step_documents_engineering_documents_engineering_document_id",
                        column: x => x.engineering_document_id,
                        principalSchema: "engineering",
                        principalTable: "engineering_documents",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_step_documents_step_executions_step_execution_id",
                        column: x => x.step_execution_id,
                        principalSchema: "production",
                        principalTable: "step_executions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_step_documents_users_validated_by_id",
                        column: x => x.validated_by_id,
                        principalSchema: "security",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "work_order_snapshots",
                schema: "production",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    work_order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    snapshot = table.Column<string>(type: "jsonb", nullable: false),
                    effective_from = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    effective_from_lot_id = table.Column<Guid>(type: "uuid", nullable: true),
                    change_request_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_by_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_work_order_snapshots", x => x.id);
                    table.CheckConstraint("ck_work_order_snapshots_1", "version > 0");
                    table.ForeignKey(
                        name: "FK_work_order_snapshots_lots_effective_from_lot_id",
                        column: x => x.effective_from_lot_id,
                        principalSchema: "production",
                        principalTable: "lots",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_work_order_snapshots_users_created_by_id",
                        column: x => x.created_by_id,
                        principalSchema: "security",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_work_order_snapshots_work_order_change_requests_change_requ~",
                        column: x => x.change_request_id,
                        principalSchema: "production",
                        principalTable: "work_order_change_requests",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_work_order_snapshots_work_orders_work_order_id",
                        column: x => x.work_order_id,
                        principalSchema: "production",
                        principalTable: "work_orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "quality_checks",
                schema: "quality",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    lot_id = table.Column<Guid>(type: "uuid", nullable: false),
                    process_run_id = table.Column<Guid>(type: "uuid", nullable: true),
                    quality_check_template_id = table.Column<Guid>(type: "uuid", nullable: false),
                    checked_by_id = table.Column<Guid>(type: "uuid", nullable: false),
                    checked_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    production_quantity = table.Column<int>(type: "integer", nullable: false),
                    all_passed = table.Column<bool>(type: "boolean", nullable: true),
                    notes = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_quality_checks", x => x.id);
                    table.CheckConstraint("ck_quality_checks_1", "production_quantity >= 0");
                    table.ForeignKey(
                        name: "FK_quality_checks_lots_lot_id",
                        column: x => x.lot_id,
                        principalSchema: "production",
                        principalTable: "lots",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_quality_checks_process_runs_process_run_id",
                        column: x => x.process_run_id,
                        principalSchema: "production",
                        principalTable: "process_runs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_quality_checks_quality_check_templates_quality_check_templa~",
                        column: x => x.quality_check_template_id,
                        principalSchema: "quality",
                        principalTable: "quality_check_templates",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_quality_checks_users_checked_by_id",
                        column: x => x.checked_by_id,
                        principalSchema: "security",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "reticle_usages",
                schema: "equipment",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    reticle_id = table.Column<Guid>(type: "uuid", nullable: false),
                    process_run_id = table.Column<Guid>(type: "uuid", nullable: false),
                    loaded_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    unloaded_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_reticle_usages", x => x.id);
                    table.CheckConstraint("ck_reticle_usages_1", "unloaded_at IS NULL OR unloaded_at >= loaded_at");
                    table.ForeignKey(
                        name: "FK_reticle_usages_process_runs_process_run_id",
                        column: x => x.process_run_id,
                        principalSchema: "production",
                        principalTable: "process_runs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_reticle_usages_reticles_reticle_id",
                        column: x => x.reticle_id,
                        principalSchema: "equipment",
                        principalTable: "reticles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "track_events",
                schema: "production",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    process_run_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    occurred_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    recorded_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    actor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    correlation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    idempotency_key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    payload = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_track_events", x => x.id);
                    table.ForeignKey(
                        name: "FK_track_events_process_runs_process_run_id",
                        column: x => x.process_run_id,
                        principalSchema: "production",
                        principalTable: "process_runs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_track_events_users_actor_id",
                        column: x => x.actor_id,
                        principalSchema: "security",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "allocation_lot_picks",
                schema: "materials",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    material_allocation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    material_lot_id = table.Column<Guid>(type: "uuid", nullable: false),
                    picked_qty = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    picking_strategy = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    consumption_warehouse_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_allocation_lot_picks", x => x.id);
                    table.CheckConstraint("ck_allocation_lot_picks_1", "picked_qty > 0");
                    table.ForeignKey(
                        name: "FK_allocation_lot_picks_material_allocations_material_allocati~",
                        column: x => x.material_allocation_id,
                        principalSchema: "materials",
                        principalTable: "material_allocations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_allocation_lot_picks_warehouses_consumption_warehouse_id",
                        column: x => x.consumption_warehouse_id,
                        principalSchema: "materials",
                        principalTable: "warehouses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "carrier_loads",
                schema: "production",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    carrier_slot_id = table.Column<Guid>(type: "uuid", nullable: false),
                    wafer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    loaded_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    unloaded_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_carrier_loads", x => x.id);
                    table.CheckConstraint("ck_carrier_loads_1", "unloaded_at IS NULL OR unloaded_at > loaded_at");
                    table.ForeignKey(
                        name: "FK_carrier_loads_carrier_slots_carrier_slot_id",
                        column: x => x.carrier_slot_id,
                        principalSchema: "production",
                        principalTable: "carrier_slots",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "genealogy_details",
                schema: "production",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    wafer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    from_lot_id = table.Column<Guid>(type: "uuid", nullable: true),
                    to_lot_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_genealogy_details", x => x.id);
                    table.CheckConstraint("ck_genealogy_details_1", "from_lot_id IS NULL OR from_lot_id <> to_lot_id");
                    table.ForeignKey(
                        name: "FK_genealogy_details_genealogy_events_event_id",
                        column: x => x.event_id,
                        principalSchema: "production",
                        principalTable: "genealogy_events",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_genealogy_details_lots_from_lot_id",
                        column: x => x.from_lot_id,
                        principalSchema: "production",
                        principalTable: "lots",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_genealogy_details_lots_to_lot_id",
                        column: x => x.to_lot_id,
                        principalSchema: "production",
                        principalTable: "lots",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "hold_records",
                schema: "quality",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    lot_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reason_code_id = table.Column<Guid>(type: "uuid", nullable: false),
                    issue_id = table.Column<Guid>(type: "uuid", nullable: true),
                    hold_scope = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    held_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    held_by_id = table.Column<Guid>(type: "uuid", nullable: false),
                    released_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    released_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    release_reason = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_hold_records", x => x.id);
                    table.CheckConstraint("ck_hold_records_1", "released_at IS NULL OR released_at >= held_at");
                    table.CheckConstraint("ck_hold_records_2", "(released_at IS NULL AND released_by_id IS NULL AND release_reason IS NULL) OR (released_at IS NOT NULL AND released_by_id IS NOT NULL AND release_reason IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_hold_records_lots_lot_id",
                        column: x => x.lot_id,
                        principalSchema: "production",
                        principalTable: "lots",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_hold_records_reason_codes_reason_code_id",
                        column: x => x.reason_code_id,
                        principalSchema: "quality",
                        principalTable: "reason_codes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_hold_records_users_held_by_id",
                        column: x => x.held_by_id,
                        principalSchema: "security",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_hold_records_users_released_by_id",
                        column: x => x.released_by_id,
                        principalSchema: "security",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "incoming_inspections",
                schema: "quality",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    inspection_plan_id = table.Column<Guid>(type: "uuid", nullable: false),
                    material_id = table.Column<Guid>(type: "uuid", nullable: false),
                    material_lot_id = table.Column<Guid>(type: "uuid", nullable: true),
                    lot_number = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    supplier_lot_ref = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    quantity_received = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    inspector_id = table.Column<Guid>(type: "uuid", nullable: false),
                    started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    disposition = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    disposition_notes = table.Column<string>(type: "text", nullable: true),
                    disposition_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    disposition_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    plan_version = table.Column<int>(type: "integer", nullable: false),
                    issue_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_incoming_inspections", x => x.id);
                    table.CheckConstraint("ck_incoming_inspections_1", "quantity_received >= 0 AND plan_version > 0");
                    table.CheckConstraint("ck_incoming_inspections_2", "completed_at IS NULL OR completed_at >= started_at");
                    table.ForeignKey(
                        name: "FK_incoming_inspections_inspection_plans_inspection_plan_id",
                        column: x => x.inspection_plan_id,
                        principalSchema: "quality",
                        principalTable: "inspection_plans",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_incoming_inspections_materials_material_id",
                        column: x => x.material_id,
                        principalSchema: "materials",
                        principalTable: "materials",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_incoming_inspections_users_disposition_by_id",
                        column: x => x.disposition_by_id,
                        principalSchema: "security",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_incoming_inspections_users_inspector_id",
                        column: x => x.inspector_id,
                        principalSchema: "security",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "inspection_results",
                schema: "quality",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    inspection_id = table.Column<Guid>(type: "uuid", nullable: false),
                    criterion_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    criterion_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    required = table.Column<bool>(type: "boolean", nullable: false),
                    unit = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    spec_min = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: true),
                    spec_max = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: true),
                    value_numeric = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: true),
                    value_boolean = table.Column<bool>(type: "boolean", nullable: true),
                    value_text = table.Column<string>(type: "text", nullable: true),
                    is_passed = table.Column<bool>(type: "boolean", nullable: true),
                    notes = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inspection_results", x => x.id);
                    table.CheckConstraint("ck_inspection_results_1", "spec_min IS NULL OR spec_max IS NULL OR spec_max >= spec_min");
                    table.ForeignKey(
                        name: "FK_inspection_results_incoming_inspections_inspection_id",
                        column: x => x.inspection_id,
                        principalSchema: "quality",
                        principalTable: "incoming_inspections",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "issue_actions",
                schema: "quality",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    issue_id = table.Column<Guid>(type: "uuid", nullable: false),
                    action_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    assigned_to_id = table.Column<Guid>(type: "uuid", nullable: false),
                    due_date = table.Column<DateOnly>(type: "date", nullable: true),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    completed_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    verified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    verified_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    notes = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_issue_actions", x => x.id);
                    table.ForeignKey(
                        name: "FK_issue_actions_users_assigned_to_id",
                        column: x => x.assigned_to_id,
                        principalSchema: "security",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_issue_actions_users_completed_by_id",
                        column: x => x.completed_by_id,
                        principalSchema: "security",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_issue_actions_users_verified_by_id",
                        column: x => x.verified_by_id,
                        principalSchema: "security",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "issues",
                schema: "quality",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    work_order_id = table.Column<Guid>(type: "uuid", nullable: true),
                    step_execution_id = table.Column<Guid>(type: "uuid", nullable: true),
                    lot_id = table.Column<Guid>(type: "uuid", nullable: true),
                    wafer_id = table.Column<Guid>(type: "uuid", nullable: true),
                    equipment_id = table.Column<Guid>(type: "uuid", nullable: true),
                    issue_type_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    reported_by_id = table.Column<Guid>(type: "uuid", nullable: false),
                    assigned_to_id = table.Column<Guid>(type: "uuid", nullable: true),
                    reported_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    acknowledged_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    resolved_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    closed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    resolution_notes = table.Column<string>(type: "text", nullable: true),
                    disposition = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    non_conforming_qty = table.Column<int>(type: "integer", nullable: true),
                    root_cause = table.Column<string>(type: "text", nullable: true),
                    containment_action = table.Column<string>(type: "text", nullable: true),
                    disposition_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    disposition_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_issues", x => x.id);
                    table.CheckConstraint("ck_issues_1", "non_conforming_qty IS NULL OR non_conforming_qty >= 0");
                    table.ForeignKey(
                        name: "FK_issues_equipment_equipment_id",
                        column: x => x.equipment_id,
                        principalSchema: "equipment",
                        principalTable: "equipment",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_issues_issue_types_issue_type_id",
                        column: x => x.issue_type_id,
                        principalSchema: "quality",
                        principalTable: "issue_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_issues_lots_lot_id",
                        column: x => x.lot_id,
                        principalSchema: "production",
                        principalTable: "lots",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_issues_step_executions_step_execution_id",
                        column: x => x.step_execution_id,
                        principalSchema: "production",
                        principalTable: "step_executions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_issues_users_assigned_to_id",
                        column: x => x.assigned_to_id,
                        principalSchema: "security",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_issues_users_disposition_by_id",
                        column: x => x.disposition_by_id,
                        principalSchema: "security",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_issues_users_reported_by_id",
                        column: x => x.reported_by_id,
                        principalSchema: "security",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_issues_work_orders_work_order_id",
                        column: x => x.work_order_id,
                        principalSchema: "production",
                        principalTable: "work_orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "lot_releases",
                schema: "quality",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    lot_id = table.Column<Guid>(type: "uuid", nullable: false),
                    release_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    released_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    released_by_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reason = table.Column<string>(type: "text", nullable: false),
                    issue_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_lot_releases", x => x.id);
                    table.CheckConstraint("ck_lot_releases_1", "release_type IN ('Production', 'Quality')");
                    table.ForeignKey(
                        name: "FK_lot_releases_issues_issue_id",
                        column: x => x.issue_id,
                        principalSchema: "quality",
                        principalTable: "issues",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_lot_releases_lots_lot_id",
                        column: x => x.lot_id,
                        principalSchema: "production",
                        principalTable: "lots",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_lot_releases_users_released_by_id",
                        column: x => x.released_by_id,
                        principalSchema: "security",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "material_lots",
                schema: "materials",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    lot_number = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    material_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_id = table.Column<Guid>(type: "uuid", nullable: true),
                    quantity_received = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    quantity_available = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    unit_of_measure = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    received_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    manufacturing_date = table.Column<DateOnly>(type: "date", nullable: true),
                    expiry_date = table.Column<DateOnly>(type: "date", nullable: true),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    supplier_lot_no = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    supplier_reference = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    source_container_no = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    warehouse_id = table.Column<Guid>(type: "uuid", nullable: true),
                    inspection_id = table.Column<Guid>(type: "uuid", nullable: true),
                    issue_id = table.Column<Guid>(type: "uuid", nullable: true),
                    hold_reason = table.Column<string>(type: "text", nullable: true),
                    held_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    held_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    released_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    released_by_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_material_lots", x => x.id);
                    table.CheckConstraint("ck_material_lots_1", "quantity_received >= 0 AND quantity_available >= 0");
                    table.CheckConstraint("ck_material_lots_2", "expiry_date IS NULL OR manufacturing_date IS NULL OR expiry_date >= manufacturing_date");
                    table.ForeignKey(
                        name: "FK_material_lots_incoming_inspections_inspection_id",
                        column: x => x.inspection_id,
                        principalSchema: "quality",
                        principalTable: "incoming_inspections",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_material_lots_issues_issue_id",
                        column: x => x.issue_id,
                        principalSchema: "quality",
                        principalTable: "issues",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_material_lots_material_sources_source_id",
                        column: x => x.source_id,
                        principalSchema: "materials",
                        principalTable: "material_sources",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_material_lots_materials_material_id",
                        column: x => x.material_id,
                        principalSchema: "materials",
                        principalTable: "materials",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_material_lots_users_held_by_id",
                        column: x => x.held_by_id,
                        principalSchema: "security",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_material_lots_users_released_by_id",
                        column: x => x.released_by_id,
                        principalSchema: "security",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_material_lots_warehouses_warehouse_id",
                        column: x => x.warehouse_id,
                        principalSchema: "materials",
                        principalTable: "warehouses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "quality_tasks",
                schema: "quality",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    quality_trigger_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    lot_id = table.Column<Guid>(type: "uuid", nullable: true),
                    equipment_id = table.Column<Guid>(type: "uuid", nullable: true),
                    due_reason = table.Column<string>(type: "text", nullable: false),
                    quality_check_id = table.Column<Guid>(type: "uuid", nullable: true),
                    issue_id = table.Column<Guid>(type: "uuid", nullable: true),
                    fired_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    completed_by_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_quality_tasks", x => x.id);
                    table.ForeignKey(
                        name: "FK_quality_tasks_equipment_equipment_id",
                        column: x => x.equipment_id,
                        principalSchema: "equipment",
                        principalTable: "equipment",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_quality_tasks_issues_issue_id",
                        column: x => x.issue_id,
                        principalSchema: "quality",
                        principalTable: "issues",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_quality_tasks_lots_lot_id",
                        column: x => x.lot_id,
                        principalSchema: "production",
                        principalTable: "lots",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_quality_tasks_quality_checks_quality_check_id",
                        column: x => x.quality_check_id,
                        principalSchema: "quality",
                        principalTable: "quality_checks",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_quality_tasks_quality_triggers_quality_trigger_id",
                        column: x => x.quality_trigger_id,
                        principalSchema: "quality",
                        principalTable: "quality_triggers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_quality_tasks_users_completed_by_id",
                        column: x => x.completed_by_id,
                        principalSchema: "security",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "work_order_stops",
                schema: "production",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    work_order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    lot_id = table.Column<Guid>(type: "uuid", nullable: true),
                    stop_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    reason = table.Column<string>(type: "text", nullable: false),
                    issue_id = table.Column<Guid>(type: "uuid", nullable: true),
                    stopped_by_id = table.Column<Guid>(type: "uuid", nullable: false),
                    stopped_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    resumed_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    resumed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    resume_notes = table.Column<string>(type: "text", nullable: true),
                    context = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_work_order_stops", x => x.id);
                    table.CheckConstraint("ck_work_order_stops_1", "resumed_at IS NULL OR resumed_at >= stopped_at");
                    table.ForeignKey(
                        name: "FK_work_order_stops_issues_issue_id",
                        column: x => x.issue_id,
                        principalSchema: "quality",
                        principalTable: "issues",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_work_order_stops_lots_lot_id",
                        column: x => x.lot_id,
                        principalSchema: "production",
                        principalTable: "lots",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_work_order_stops_users_resumed_by_id",
                        column: x => x.resumed_by_id,
                        principalSchema: "security",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_work_order_stops_users_stopped_by_id",
                        column: x => x.stopped_by_id,
                        principalSchema: "security",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_work_order_stops_work_orders_work_order_id",
                        column: x => x.work_order_id,
                        principalSchema: "production",
                        principalTable: "work_orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "material_holds",
                schema: "quality",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    material_lot_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reason_code_id = table.Column<Guid>(type: "uuid", nullable: false),
                    issue_id = table.Column<Guid>(type: "uuid", nullable: true),
                    held_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    held_by_id = table.Column<Guid>(type: "uuid", nullable: false),
                    released_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    released_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    release_reason = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_material_holds", x => x.id);
                    table.CheckConstraint("ck_material_holds_1", "released_at IS NULL OR released_at >= held_at");
                    table.ForeignKey(
                        name: "FK_material_holds_issues_issue_id",
                        column: x => x.issue_id,
                        principalSchema: "quality",
                        principalTable: "issues",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_material_holds_material_lots_material_lot_id",
                        column: x => x.material_lot_id,
                        principalSchema: "materials",
                        principalTable: "material_lots",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_material_holds_reason_codes_reason_code_id",
                        column: x => x.reason_code_id,
                        principalSchema: "quality",
                        principalTable: "reason_codes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_material_holds_users_held_by_id",
                        column: x => x.held_by_id,
                        principalSchema: "security",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_material_holds_users_released_by_id",
                        column: x => x.released_by_id,
                        principalSchema: "security",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "material_mounts",
                schema: "equipment",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    material_lot_id = table.Column<Guid>(type: "uuid", nullable: false),
                    equipment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    opened_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    mounted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    unmounted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    usable_until = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    usage_quantity = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    unit = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_material_mounts", x => x.id);
                    table.CheckConstraint("ck_material_mounts_1", "usage_quantity >= 0");
                    table.CheckConstraint("ck_material_mounts_2", "unmounted_at IS NULL OR unmounted_at >= mounted_at");
                    table.ForeignKey(
                        name: "FK_material_mounts_equipment_equipment_id",
                        column: x => x.equipment_id,
                        principalSchema: "equipment",
                        principalTable: "equipment",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_material_mounts_material_lots_material_lot_id",
                        column: x => x.material_lot_id,
                        principalSchema: "materials",
                        principalTable: "material_lots",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "material_sublots",
                schema: "materials",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    parent_lot_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sublot_number = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    unit_of_measure = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    notes = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_material_sublots", x => x.id);
                    table.CheckConstraint("ck_material_sublots_1", "quantity >= 0");
                    table.ForeignKey(
                        name: "FK_material_sublots_material_lots_parent_lot_id",
                        column: x => x.parent_lot_id,
                        principalSchema: "materials",
                        principalTable: "material_lots",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "stock_document_lines",
                schema: "materials",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    stock_document_id = table.Column<Guid>(type: "uuid", nullable: false),
                    material_id = table.Column<Guid>(type: "uuid", nullable: true),
                    product_id = table.Column<Guid>(type: "uuid", nullable: true),
                    material_lot_id = table.Column<Guid>(type: "uuid", nullable: true),
                    lot_number = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    quantity = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    unit_of_measure = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    notes = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stock_document_lines", x => x.id);
                    table.CheckConstraint("ck_stock_document_lines_1", "quantity > 0");
                    table.CheckConstraint("ck_stock_document_lines_2", "num_nonnulls(material_id, product_id) = 1");
                    table.ForeignKey(
                        name: "FK_stock_document_lines_material_lots_material_lot_id",
                        column: x => x.material_lot_id,
                        principalSchema: "materials",
                        principalTable: "material_lots",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_stock_document_lines_materials_material_id",
                        column: x => x.material_id,
                        principalSchema: "materials",
                        principalTable: "materials",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_stock_document_lines_products_product_id",
                        column: x => x.product_id,
                        principalSchema: "engineering",
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_stock_document_lines_stock_documents_stock_document_id",
                        column: x => x.stock_document_id,
                        principalSchema: "materials",
                        principalTable: "stock_documents",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "stock_movements",
                schema: "materials",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    material_id = table.Column<Guid>(type: "uuid", nullable: false),
                    material_lot_id = table.Column<Guid>(type: "uuid", nullable: true),
                    warehouse_id = table.Column<Guid>(type: "uuid", nullable: false),
                    movement_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    unit_of_measure = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    balance_after = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    source_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    source_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reason = table.Column<string>(type: "text", nullable: false),
                    performed_by_id = table.Column<Guid>(type: "uuid", nullable: false),
                    performed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stock_movements", x => x.id);
                    table.CheckConstraint("ck_stock_movements_1", "quantity <> 0 AND balance_after >= 0");
                    table.ForeignKey(
                        name: "FK_stock_movements_material_lots_material_lot_id",
                        column: x => x.material_lot_id,
                        principalSchema: "materials",
                        principalTable: "material_lots",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_stock_movements_materials_material_id",
                        column: x => x.material_id,
                        principalSchema: "materials",
                        principalTable: "materials",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_stock_movements_users_performed_by_id",
                        column: x => x.performed_by_id,
                        principalSchema: "security",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_stock_movements_warehouses_warehouse_id",
                        column: x => x.warehouse_id,
                        principalSchema: "materials",
                        principalTable: "warehouses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "wafers",
                schema: "production",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    wafer_code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    origin_material_lot_id = table.Column<Guid>(type: "uuid", nullable: true),
                    supplier_wafer_code = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    wafer_size_mm = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    lifecycle_status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    produced_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_wafers", x => x.id);
                    table.CheckConstraint("ck_wafers_1", "wafer_size_mm > 0");
                    table.ForeignKey(
                        name: "FK_wafers_material_lots_origin_material_lot_id",
                        column: x => x.origin_material_lot_id,
                        principalSchema: "materials",
                        principalTable: "material_lots",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "warehouse_stocks",
                schema: "materials",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    warehouse_id = table.Column<Guid>(type: "uuid", nullable: false),
                    material_id = table.Column<Guid>(type: "uuid", nullable: false),
                    material_lot_id = table.Column<Guid>(type: "uuid", nullable: true),
                    quantity = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    unit_of_measure = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    erp_synced_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_warehouse_stocks", x => x.id);
                    table.CheckConstraint("ck_warehouse_stocks_1", "quantity >= 0");
                    table.ForeignKey(
                        name: "FK_warehouse_stocks_material_lots_material_lot_id",
                        column: x => x.material_lot_id,
                        principalSchema: "materials",
                        principalTable: "material_lots",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_warehouse_stocks_materials_material_id",
                        column: x => x.material_id,
                        principalSchema: "materials",
                        principalTable: "materials",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_warehouse_stocks_warehouses_warehouse_id",
                        column: x => x.warehouse_id,
                        principalSchema: "materials",
                        principalTable: "warehouses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "material_consumptions",
                schema: "materials",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    step_execution_id = table.Column<Guid>(type: "uuid", nullable: false),
                    process_run_id = table.Column<Guid>(type: "uuid", nullable: true),
                    material_lot_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sublot_id = table.Column<Guid>(type: "uuid", nullable: true),
                    quantity_consumed = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    unit_of_measure = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    consumed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    recorded_by_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_material_consumptions", x => x.id);
                    table.CheckConstraint("ck_material_consumptions_1", "quantity_consumed > 0");
                    table.ForeignKey(
                        name: "FK_material_consumptions_material_lots_material_lot_id",
                        column: x => x.material_lot_id,
                        principalSchema: "materials",
                        principalTable: "material_lots",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_material_consumptions_material_sublots_sublot_id",
                        column: x => x.sublot_id,
                        principalSchema: "materials",
                        principalTable: "material_sublots",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_material_consumptions_process_runs_process_run_id",
                        column: x => x.process_run_id,
                        principalSchema: "production",
                        principalTable: "process_runs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_material_consumptions_step_executions_step_execution_id",
                        column: x => x.step_execution_id,
                        principalSchema: "production",
                        principalTable: "step_executions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_material_consumptions_users_recorded_by_id",
                        column: x => x.recorded_by_id,
                        principalSchema: "security",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "lot_memberships",
                schema: "production",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    wafer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    lot_id = table.Column<Guid>(type: "uuid", nullable: false),
                    valid_from = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    valid_to = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    change_event_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_lot_memberships", x => x.id);
                    table.CheckConstraint("ck_lot_memberships_1", "valid_to IS NULL OR valid_to > valid_from");
                    table.ForeignKey(
                        name: "FK_lot_memberships_genealogy_events_change_event_id",
                        column: x => x.change_event_id,
                        principalSchema: "production",
                        principalTable: "genealogy_events",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_lot_memberships_lots_lot_id",
                        column: x => x.lot_id,
                        principalSchema: "production",
                        principalTable: "lots",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_lot_memberships_wafers_wafer_id",
                        column: x => x.wafer_id,
                        principalSchema: "production",
                        principalTable: "wafers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "measurements",
                schema: "quality",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    quality_check_id = table.Column<Guid>(type: "uuid", nullable: false),
                    process_run_id = table.Column<Guid>(type: "uuid", nullable: true),
                    lot_id = table.Column<Guid>(type: "uuid", nullable: false),
                    wafer_id = table.Column<Guid>(type: "uuid", nullable: true),
                    sample_number = table.Column<int>(type: "integer", nullable: false),
                    sample_point = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    specification_version_id = table.Column<Guid>(type: "uuid", nullable: false),
                    parameter_code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    parameter_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    unit = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    spec_min = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: true),
                    spec_max = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: true),
                    value_numeric = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: true),
                    value_boolean = table.Column<bool>(type: "boolean", nullable: true),
                    value_text = table.Column<string>(type: "text", nullable: true),
                    is_passed = table.Column<bool>(type: "boolean", nullable: false),
                    measured_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    measurement_equipment_id = table.Column<Guid>(type: "uuid", nullable: true),
                    recorded_by_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_measurements", x => x.id);
                    table.CheckConstraint("ck_measurements_1", "sample_number > 0");
                    table.CheckConstraint("ck_measurements_2", "spec_min IS NULL OR spec_max IS NULL OR spec_max >= spec_min");
                    table.CheckConstraint("ck_measurements_3", "num_nonnulls(value_numeric, value_boolean, value_text) = 1");
                    table.ForeignKey(
                        name: "FK_measurements_equipment_measurement_equipment_id",
                        column: x => x.measurement_equipment_id,
                        principalSchema: "equipment",
                        principalTable: "equipment",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_measurements_lots_lot_id",
                        column: x => x.lot_id,
                        principalSchema: "production",
                        principalTable: "lots",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_measurements_process_runs_process_run_id",
                        column: x => x.process_run_id,
                        principalSchema: "production",
                        principalTable: "process_runs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_measurements_quality_checks_quality_check_id",
                        column: x => x.quality_check_id,
                        principalSchema: "quality",
                        principalTable: "quality_checks",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_measurements_specification_versions_specification_version_id",
                        column: x => x.specification_version_id,
                        principalSchema: "quality",
                        principalTable: "specification_versions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_measurements_users_recorded_by_id",
                        column: x => x.recorded_by_id,
                        principalSchema: "security",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_measurements_wafers_wafer_id",
                        column: x => x.wafer_id,
                        principalSchema: "production",
                        principalTable: "wafers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "process_batch_members",
                schema: "production",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    process_batch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    process_run_id = table.Column<Guid>(type: "uuid", nullable: false),
                    lot_id = table.Column<Guid>(type: "uuid", nullable: false),
                    wafer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    position = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_process_batch_members", x => x.id);
                    table.CheckConstraint("ck_process_batch_members_1", "position > 0");
                    table.ForeignKey(
                        name: "FK_process_batch_members_lots_lot_id",
                        column: x => x.lot_id,
                        principalSchema: "production",
                        principalTable: "lots",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_process_batch_members_process_batches_process_batch_id",
                        column: x => x.process_batch_id,
                        principalSchema: "production",
                        principalTable: "process_batches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_process_batch_members_process_runs_process_run_id",
                        column: x => x.process_run_id,
                        principalSchema: "production",
                        principalTable: "process_runs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_process_batch_members_wafers_wafer_id",
                        column: x => x.wafer_id,
                        principalSchema: "production",
                        principalTable: "wafers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "process_run_wafers",
                schema: "production",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    process_run_id = table.Column<Guid>(type: "uuid", nullable: false),
                    wafer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    result = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    parameters = table.Column<string>(type: "jsonb", nullable: true),
                    notes = table.Column<string>(type: "text", nullable: true),
                    processed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_process_run_wafers", x => x.id);
                    table.ForeignKey(
                        name: "FK_process_run_wafers_process_runs_process_run_id",
                        column: x => x.process_run_id,
                        principalSchema: "production",
                        principalTable: "process_runs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_process_run_wafers_wafers_wafer_id",
                        column: x => x.wafer_id,
                        principalSchema: "production",
                        principalTable: "wafers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "wafer_dispositions",
                schema: "quality",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    wafer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    lot_id_at_event = table.Column<Guid>(type: "uuid", nullable: false),
                    process_run_id = table.Column<Guid>(type: "uuid", nullable: true),
                    scrap_entry_id = table.Column<Guid>(type: "uuid", nullable: true),
                    disposition_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    reason_code_id = table.Column<Guid>(type: "uuid", nullable: false),
                    issue_id = table.Column<Guid>(type: "uuid", nullable: true),
                    approved_by_id = table.Column<Guid>(type: "uuid", nullable: false),
                    performed_by_id = table.Column<Guid>(type: "uuid", nullable: false),
                    occurred_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_wafer_dispositions", x => x.id);
                    table.ForeignKey(
                        name: "FK_wafer_dispositions_issues_issue_id",
                        column: x => x.issue_id,
                        principalSchema: "quality",
                        principalTable: "issues",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_wafer_dispositions_lots_lot_id_at_event",
                        column: x => x.lot_id_at_event,
                        principalSchema: "production",
                        principalTable: "lots",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_wafer_dispositions_process_runs_process_run_id",
                        column: x => x.process_run_id,
                        principalSchema: "production",
                        principalTable: "process_runs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_wafer_dispositions_reason_codes_reason_code_id",
                        column: x => x.reason_code_id,
                        principalSchema: "quality",
                        principalTable: "reason_codes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_wafer_dispositions_scrap_entries_scrap_entry_id",
                        column: x => x.scrap_entry_id,
                        principalSchema: "quality",
                        principalTable: "scrap_entries",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_wafer_dispositions_users_approved_by_id",
                        column: x => x.approved_by_id,
                        principalSchema: "security",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_wafer_dispositions_users_performed_by_id",
                        column: x => x.performed_by_id,
                        principalSchema: "security",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_wafer_dispositions_wafers_wafer_id",
                        column: x => x.wafer_id,
                        principalSchema: "production",
                        principalTable: "wafers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_allocation_lot_picks_consumption_warehouse_id",
                schema: "materials",
                table: "allocation_lot_picks",
                column: "consumption_warehouse_id");

            migrationBuilder.CreateIndex(
                name: "IX_allocation_lot_picks_material_allocation_id",
                schema: "materials",
                table: "allocation_lot_picks",
                column: "material_allocation_id");

            migrationBuilder.CreateIndex(
                name: "IX_allocation_lot_picks_material_lot_id",
                schema: "materials",
                table: "allocation_lot_picks",
                column: "material_lot_id");

            migrationBuilder.CreateIndex(
                name: "IX_api_keys_integration_config_id",
                schema: "integration",
                table: "api_keys",
                column: "integration_config_id");

            migrationBuilder.CreateIndex(
                name: "IX_api_keys_key_hash",
                schema: "integration",
                table: "api_keys",
                column: "key_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_areas_fab_id_code",
                schema: "organization",
                table: "areas",
                columns: new[] { "fab_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_attachments_uploaded_by_id",
                schema: "audit",
                table: "attachments",
                column: "uploaded_by_id");

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_correlation_id",
                schema: "audit",
                table: "audit_logs",
                column: "correlation_id");

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_entity_type_entity_id_occurred_at",
                schema: "audit",
                table: "audit_logs",
                columns: new[] { "entity_type", "entity_id", "occurred_at" });

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_reason_code_id",
                schema: "audit",
                table: "audit_logs",
                column: "reason_code_id");

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_user_id",
                schema: "audit",
                table: "audit_logs",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_bom_items_material_id",
                schema: "materials",
                table: "bom_items",
                column: "material_id");

            migrationBuilder.CreateIndex(
                name: "IX_bom_items_route_step_id",
                schema: "materials",
                table: "bom_items",
                column: "route_step_id");

            migrationBuilder.CreateIndex(
                name: "IX_bom_items_route_version_id",
                schema: "materials",
                table: "bom_items",
                column: "route_version_id");

            migrationBuilder.CreateIndex(
                name: "IX_business_events_correlation_id",
                schema: "audit",
                table: "business_events",
                column: "correlation_id");

            migrationBuilder.CreateIndex(
                name: "IX_business_events_user_id",
                schema: "audit",
                table: "business_events",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_carrier_loads_carrier_slot_id",
                schema: "production",
                table: "carrier_loads",
                column: "carrier_slot_id",
                unique: true,
                filter: "unloaded_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_carrier_loads_wafer_id",
                schema: "production",
                table: "carrier_loads",
                column: "wafer_id",
                unique: true,
                filter: "unloaded_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_carrier_movements_carrier_id",
                schema: "production",
                table: "carrier_movements",
                column: "carrier_id");

            migrationBuilder.CreateIndex(
                name: "IX_carrier_movements_from_location_id",
                schema: "production",
                table: "carrier_movements",
                column: "from_location_id");

            migrationBuilder.CreateIndex(
                name: "IX_carrier_movements_performed_by_id",
                schema: "production",
                table: "carrier_movements",
                column: "performed_by_id");

            migrationBuilder.CreateIndex(
                name: "IX_carrier_movements_to_location_id",
                schema: "production",
                table: "carrier_movements",
                column: "to_location_id");

            migrationBuilder.CreateIndex(
                name: "IX_carrier_slots_carrier_id_slot_no",
                schema: "production",
                table: "carrier_slots",
                columns: new[] { "carrier_id", "slot_no" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_carriers_carrier_code",
                schema: "production",
                table: "carriers",
                column: "carrier_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_carriers_current_location_id",
                schema: "production",
                table: "carriers",
                column: "current_location_id");

            migrationBuilder.CreateIndex(
                name: "IX_carriers_fab_id",
                schema: "production",
                table: "carriers",
                column: "fab_id");

            migrationBuilder.CreateIndex(
                name: "IX_chambers_equipment_id_code",
                schema: "equipment",
                table: "chambers",
                columns: new[] { "equipment_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_checklist_completions_checked_by_id",
                schema: "production",
                table: "checklist_completions",
                column: "checked_by_id");

            migrationBuilder.CreateIndex(
                name: "IX_checklist_completions_checklist_item_id",
                schema: "production",
                table: "checklist_completions",
                column: "checklist_item_id");

            migrationBuilder.CreateIndex(
                name: "IX_checklist_completions_step_execution_id_checklist_item_id",
                schema: "production",
                table: "checklist_completions",
                columns: new[] { "step_execution_id", "checklist_item_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_checklist_items_route_step_id",
                schema: "engineering",
                table: "checklist_items",
                column: "route_step_id");

            migrationBuilder.CreateIndex(
                name: "IX_command_receipts_actor_user_id",
                schema: "audit",
                table: "command_receipts",
                column: "actor_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_command_receipts_scope_idempotency_key",
                schema: "audit",
                table: "command_receipts",
                columns: new[] { "scope", "idempotency_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_companies_code",
                schema: "materials",
                table: "companies",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_custom_field_definitions_entity_type_key",
                schema: "integration",
                table: "custom_field_definitions",
                columns: new[] { "entity_type", "key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_downtimes_classified_by_id",
                schema: "equipment",
                table: "downtimes",
                column: "classified_by_id");

            migrationBuilder.CreateIndex(
                name: "IX_downtimes_equipment_id",
                schema: "equipment",
                table: "downtimes",
                column: "equipment_id");

            migrationBuilder.CreateIndex(
                name: "IX_downtimes_line_id",
                schema: "equipment",
                table: "downtimes",
                column: "line_id");

            migrationBuilder.CreateIndex(
                name: "IX_downtimes_reason_code_id",
                schema: "equipment",
                table: "downtimes",
                column: "reason_code_id");

            migrationBuilder.CreateIndex(
                name: "IX_downtimes_reported_by_id",
                schema: "equipment",
                table: "downtimes",
                column: "reported_by_id");

            migrationBuilder.CreateIndex(
                name: "IX_downtimes_shift_id",
                schema: "equipment",
                table: "downtimes",
                column: "shift_id");

            migrationBuilder.CreateIndex(
                name: "IX_engineering_documents_released_by_id",
                schema: "engineering",
                table: "engineering_documents",
                column: "released_by_id");

            migrationBuilder.CreateIndex(
                name: "IX_engineering_documents_uploaded_by_id",
                schema: "engineering",
                table: "engineering_documents",
                column: "uploaded_by_id");

            migrationBuilder.CreateIndex(
                name: "IX_equipment_area_id",
                schema: "equipment",
                table: "equipment",
                column: "area_id");

            migrationBuilder.CreateIndex(
                name: "IX_equipment_code",
                schema: "equipment",
                table: "equipment",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_equipment_equipment_group_id",
                schema: "equipment",
                table: "equipment",
                column: "equipment_group_id");

            migrationBuilder.CreateIndex(
                name: "IX_equipment_equipment_type_id",
                schema: "equipment",
                table: "equipment",
                column: "equipment_type_id");

            migrationBuilder.CreateIndex(
                name: "IX_equipment_line_id",
                schema: "equipment",
                table: "equipment",
                column: "line_id");

            migrationBuilder.CreateIndex(
                name: "IX_equipment_events_equipment_id_event_timestamp",
                schema: "equipment",
                table: "equipment_events",
                columns: new[] { "equipment_id", "event_timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_equipment_events_machine_connection_id",
                schema: "equipment",
                table: "equipment_events",
                column: "machine_connection_id");

            migrationBuilder.CreateIndex(
                name: "IX_equipment_groups_fab_id_code",
                schema: "equipment",
                table: "equipment_groups",
                columns: new[] { "fab_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_equipment_qualifications_equipment_id",
                schema: "equipment",
                table: "equipment_qualifications",
                column: "equipment_id");

            migrationBuilder.CreateIndex(
                name: "IX_equipment_qualifications_operation_id",
                schema: "equipment",
                table: "equipment_qualifications",
                column: "operation_id");

            migrationBuilder.CreateIndex(
                name: "IX_equipment_qualifications_qualified_by_id",
                schema: "equipment",
                table: "equipment_qualifications",
                column: "qualified_by_id");

            migrationBuilder.CreateIndex(
                name: "IX_equipment_qualifications_recipe_version_id",
                schema: "equipment",
                table: "equipment_qualifications",
                column: "recipe_version_id");

            migrationBuilder.CreateIndex(
                name: "IX_equipment_states_equipment_id",
                schema: "equipment",
                table: "equipment_states",
                column: "equipment_id",
                unique: true,
                filter: "ended_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_equipment_types_code",
                schema: "equipment",
                table: "equipment_types",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_fabs_code",
                schema: "organization",
                table: "fabs",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_genealogy_details_event_id_wafer_id",
                schema: "production",
                table: "genealogy_details",
                columns: new[] { "event_id", "wafer_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_genealogy_details_from_lot_id",
                schema: "production",
                table: "genealogy_details",
                column: "from_lot_id");

            migrationBuilder.CreateIndex(
                name: "IX_genealogy_details_to_lot_id",
                schema: "production",
                table: "genealogy_details",
                column: "to_lot_id");

            migrationBuilder.CreateIndex(
                name: "IX_genealogy_details_wafer_id",
                schema: "production",
                table: "genealogy_details",
                column: "wafer_id");

            migrationBuilder.CreateIndex(
                name: "IX_genealogy_events_idempotency_key",
                schema: "production",
                table: "genealogy_events",
                column: "idempotency_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_genealogy_events_performed_by_id",
                schema: "production",
                table: "genealogy_events",
                column: "performed_by_id");

            migrationBuilder.CreateIndex(
                name: "IX_hold_records_held_by_id",
                schema: "quality",
                table: "hold_records",
                column: "held_by_id");

            migrationBuilder.CreateIndex(
                name: "IX_hold_records_issue_id",
                schema: "quality",
                table: "hold_records",
                column: "issue_id");

            migrationBuilder.CreateIndex(
                name: "IX_hold_records_lot_id_held_at",
                schema: "quality",
                table: "hold_records",
                columns: new[] { "lot_id", "held_at" },
                filter: "released_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_hold_records_reason_code_id",
                schema: "quality",
                table: "hold_records",
                column: "reason_code_id");

            migrationBuilder.CreateIndex(
                name: "IX_hold_records_released_by_id",
                schema: "quality",
                table: "hold_records",
                column: "released_by_id");

            migrationBuilder.CreateIndex(
                name: "IX_import_jobs_mapping_id",
                schema: "integration",
                table: "import_jobs",
                column: "mapping_id");

            migrationBuilder.CreateIndex(
                name: "IX_import_jobs_user_id",
                schema: "integration",
                table: "import_jobs",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_incoming_inspections_disposition_by_id",
                schema: "quality",
                table: "incoming_inspections",
                column: "disposition_by_id");

            migrationBuilder.CreateIndex(
                name: "IX_incoming_inspections_inspection_plan_id",
                schema: "quality",
                table: "incoming_inspections",
                column: "inspection_plan_id");

            migrationBuilder.CreateIndex(
                name: "IX_incoming_inspections_inspector_id",
                schema: "quality",
                table: "incoming_inspections",
                column: "inspector_id");

            migrationBuilder.CreateIndex(
                name: "IX_incoming_inspections_issue_id",
                schema: "quality",
                table: "incoming_inspections",
                column: "issue_id");

            migrationBuilder.CreateIndex(
                name: "IX_incoming_inspections_material_id",
                schema: "quality",
                table: "incoming_inspections",
                column: "material_id");

            migrationBuilder.CreateIndex(
                name: "IX_incoming_inspections_material_lot_id",
                schema: "quality",
                table: "incoming_inspections",
                column: "material_lot_id");

            migrationBuilder.CreateIndex(
                name: "IX_inspection_plans_material_id",
                schema: "quality",
                table: "inspection_plans",
                column: "material_id");

            migrationBuilder.CreateIndex(
                name: "IX_inspection_plans_material_type_id",
                schema: "quality",
                table: "inspection_plans",
                column: "material_type_id");

            migrationBuilder.CreateIndex(
                name: "IX_inspection_plans_root_id",
                schema: "quality",
                table: "inspection_plans",
                column: "root_id");

            migrationBuilder.CreateIndex(
                name: "IX_inspection_results_inspection_id",
                schema: "quality",
                table: "inspection_results",
                column: "inspection_id");

            migrationBuilder.CreateIndex(
                name: "IX_integration_configs_system_name",
                schema: "integration",
                table: "integration_configs",
                column: "system_name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_issue_actions_assigned_to_id",
                schema: "quality",
                table: "issue_actions",
                column: "assigned_to_id");

            migrationBuilder.CreateIndex(
                name: "IX_issue_actions_completed_by_id",
                schema: "quality",
                table: "issue_actions",
                column: "completed_by_id");

            migrationBuilder.CreateIndex(
                name: "IX_issue_actions_issue_id",
                schema: "quality",
                table: "issue_actions",
                column: "issue_id");

            migrationBuilder.CreateIndex(
                name: "IX_issue_actions_verified_by_id",
                schema: "quality",
                table: "issue_actions",
                column: "verified_by_id");

            migrationBuilder.CreateIndex(
                name: "IX_issue_types_code",
                schema: "quality",
                table: "issue_types",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_issues_assigned_to_id",
                schema: "quality",
                table: "issues",
                column: "assigned_to_id");

            migrationBuilder.CreateIndex(
                name: "IX_issues_disposition_by_id",
                schema: "quality",
                table: "issues",
                column: "disposition_by_id");

            migrationBuilder.CreateIndex(
                name: "IX_issues_equipment_id",
                schema: "quality",
                table: "issues",
                column: "equipment_id");

            migrationBuilder.CreateIndex(
                name: "IX_issues_issue_type_id",
                schema: "quality",
                table: "issues",
                column: "issue_type_id");

            migrationBuilder.CreateIndex(
                name: "IX_issues_lot_id",
                schema: "quality",
                table: "issues",
                column: "lot_id");

            migrationBuilder.CreateIndex(
                name: "IX_issues_reported_by_id",
                schema: "quality",
                table: "issues",
                column: "reported_by_id");

            migrationBuilder.CreateIndex(
                name: "IX_issues_step_execution_id",
                schema: "quality",
                table: "issues",
                column: "step_execution_id");

            migrationBuilder.CreateIndex(
                name: "IX_issues_wafer_id",
                schema: "quality",
                table: "issues",
                column: "wafer_id");

            migrationBuilder.CreateIndex(
                name: "IX_issues_work_order_id",
                schema: "quality",
                table: "issues",
                column: "work_order_id");

            migrationBuilder.CreateIndex(
                name: "IX_lines_area_id_code",
                schema: "organization",
                table: "lines",
                columns: new[] { "area_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_locations_area_id_code",
                schema: "organization",
                table: "locations",
                columns: new[] { "area_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_lot_memberships_change_event_id",
                schema: "production",
                table: "lot_memberships",
                column: "change_event_id");

            migrationBuilder.CreateIndex(
                name: "IX_lot_memberships_lot_id_valid_from",
                schema: "production",
                table: "lot_memberships",
                columns: new[] { "lot_id", "valid_from" });

            migrationBuilder.CreateIndex(
                name: "IX_lot_memberships_wafer_id",
                schema: "production",
                table: "lot_memberships",
                column: "wafer_id",
                unique: true,
                filter: "valid_to IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_lot_releases_issue_id",
                schema: "quality",
                table: "lot_releases",
                column: "issue_id");

            migrationBuilder.CreateIndex(
                name: "IX_lot_releases_lot_id",
                schema: "quality",
                table: "lot_releases",
                column: "lot_id");

            migrationBuilder.CreateIndex(
                name: "IX_lot_releases_released_by_id",
                schema: "quality",
                table: "lot_releases",
                column: "released_by_id");

            migrationBuilder.CreateIndex(
                name: "IX_lots_current_route_step_id_route_version_id",
                schema: "production",
                table: "lots",
                columns: new[] { "current_route_step_id", "route_version_id" });

            migrationBuilder.CreateIndex(
                name: "IX_lots_fab_id_lot_number",
                schema: "production",
                table: "lots",
                columns: new[] { "fab_id", "lot_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_lots_product_revision_id",
                schema: "production",
                table: "lots",
                column: "product_revision_id");

            migrationBuilder.CreateIndex(
                name: "IX_lots_released_to_route_by_id",
                schema: "production",
                table: "lots",
                column: "released_to_route_by_id");

            migrationBuilder.CreateIndex(
                name: "IX_lots_route_version_id",
                schema: "production",
                table: "lots",
                column: "route_version_id");

            migrationBuilder.CreateIndex(
                name: "IX_lots_work_order_id",
                schema: "production",
                table: "lots",
                column: "work_order_id");

            migrationBuilder.CreateIndex(
                name: "IX_machine_connections_equipment_id",
                schema: "integration",
                table: "machine_connections",
                column: "equipment_id");

            migrationBuilder.CreateIndex(
                name: "IX_machine_messages_machine_connection_id_source_message_id",
                schema: "integration",
                table: "machine_messages",
                columns: new[] { "machine_connection_id", "source_message_id" },
                unique: true,
                filter: "source_message_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_machine_tags_equipment_id",
                schema: "integration",
                table: "machine_tags",
                column: "equipment_id");

            migrationBuilder.CreateIndex(
                name: "IX_machine_tags_machine_connection_id_name",
                schema: "integration",
                table: "machine_tags",
                columns: new[] { "machine_connection_id", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_maintenance_events_assigned_to_id",
                schema: "equipment",
                table: "maintenance_events",
                column: "assigned_to_id");

            migrationBuilder.CreateIndex(
                name: "IX_maintenance_events_equipment_id",
                schema: "equipment",
                table: "maintenance_events",
                column: "equipment_id");

            migrationBuilder.CreateIndex(
                name: "IX_maintenance_events_schedule_id",
                schema: "equipment",
                table: "maintenance_events",
                column: "schedule_id");

            migrationBuilder.CreateIndex(
                name: "IX_maintenance_events_tool_id",
                schema: "equipment",
                table: "maintenance_events",
                column: "tool_id");

            migrationBuilder.CreateIndex(
                name: "IX_maintenance_schedules_assigned_to_id",
                schema: "equipment",
                table: "maintenance_schedules",
                column: "assigned_to_id");

            migrationBuilder.CreateIndex(
                name: "IX_maintenance_schedules_equipment_id",
                schema: "equipment",
                table: "maintenance_schedules",
                column: "equipment_id");

            migrationBuilder.CreateIndex(
                name: "IX_maintenance_schedules_tool_id",
                schema: "equipment",
                table: "maintenance_schedules",
                column: "tool_id");

            migrationBuilder.CreateIndex(
                name: "IX_material_allocations_allocated_by_id",
                schema: "materials",
                table: "material_allocations",
                column: "allocated_by_id");

            migrationBuilder.CreateIndex(
                name: "IX_material_allocations_consumption_warehouse_id",
                schema: "materials",
                table: "material_allocations",
                column: "consumption_warehouse_id");

            migrationBuilder.CreateIndex(
                name: "IX_material_allocations_lot_id",
                schema: "materials",
                table: "material_allocations",
                column: "lot_id");

            migrationBuilder.CreateIndex(
                name: "IX_material_allocations_material_id",
                schema: "materials",
                table: "material_allocations",
                column: "material_id");

            migrationBuilder.CreateIndex(
                name: "IX_material_allocations_step_execution_id",
                schema: "materials",
                table: "material_allocations",
                column: "step_execution_id");

            migrationBuilder.CreateIndex(
                name: "IX_material_allocations_work_order_id",
                schema: "materials",
                table: "material_allocations",
                column: "work_order_id");

            migrationBuilder.CreateIndex(
                name: "IX_material_consumptions_material_lot_id",
                schema: "materials",
                table: "material_consumptions",
                column: "material_lot_id");

            migrationBuilder.CreateIndex(
                name: "IX_material_consumptions_process_run_id",
                schema: "materials",
                table: "material_consumptions",
                column: "process_run_id");

            migrationBuilder.CreateIndex(
                name: "IX_material_consumptions_recorded_by_id",
                schema: "materials",
                table: "material_consumptions",
                column: "recorded_by_id");

            migrationBuilder.CreateIndex(
                name: "IX_material_consumptions_step_execution_id",
                schema: "materials",
                table: "material_consumptions",
                column: "step_execution_id");

            migrationBuilder.CreateIndex(
                name: "IX_material_consumptions_sublot_id",
                schema: "materials",
                table: "material_consumptions",
                column: "sublot_id");

            migrationBuilder.CreateIndex(
                name: "IX_material_holds_held_by_id",
                schema: "quality",
                table: "material_holds",
                column: "held_by_id");

            migrationBuilder.CreateIndex(
                name: "IX_material_holds_issue_id",
                schema: "quality",
                table: "material_holds",
                column: "issue_id");

            migrationBuilder.CreateIndex(
                name: "IX_material_holds_material_lot_id",
                schema: "quality",
                table: "material_holds",
                column: "material_lot_id");

            migrationBuilder.CreateIndex(
                name: "IX_material_holds_reason_code_id",
                schema: "quality",
                table: "material_holds",
                column: "reason_code_id");

            migrationBuilder.CreateIndex(
                name: "IX_material_holds_released_by_id",
                schema: "quality",
                table: "material_holds",
                column: "released_by_id");

            migrationBuilder.CreateIndex(
                name: "IX_material_lots_held_by_id",
                schema: "materials",
                table: "material_lots",
                column: "held_by_id");

            migrationBuilder.CreateIndex(
                name: "IX_material_lots_inspection_id",
                schema: "materials",
                table: "material_lots",
                column: "inspection_id");

            migrationBuilder.CreateIndex(
                name: "IX_material_lots_issue_id",
                schema: "materials",
                table: "material_lots",
                column: "issue_id");

            migrationBuilder.CreateIndex(
                name: "IX_material_lots_material_id_lot_number",
                schema: "materials",
                table: "material_lots",
                columns: new[] { "material_id", "lot_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_material_lots_released_by_id",
                schema: "materials",
                table: "material_lots",
                column: "released_by_id");

            migrationBuilder.CreateIndex(
                name: "IX_material_lots_source_id",
                schema: "materials",
                table: "material_lots",
                column: "source_id");

            migrationBuilder.CreateIndex(
                name: "IX_material_lots_warehouse_id",
                schema: "materials",
                table: "material_lots",
                column: "warehouse_id");

            migrationBuilder.CreateIndex(
                name: "IX_material_mounts_equipment_id",
                schema: "equipment",
                table: "material_mounts",
                column: "equipment_id");

            migrationBuilder.CreateIndex(
                name: "IX_material_mounts_material_lot_id",
                schema: "equipment",
                table: "material_mounts",
                column: "material_lot_id");

            migrationBuilder.CreateIndex(
                name: "IX_material_sources_integration_config_id",
                schema: "materials",
                table: "material_sources",
                column: "integration_config_id");

            migrationBuilder.CreateIndex(
                name: "IX_material_sources_material_id_integration_config_id_external~",
                schema: "materials",
                table: "material_sources",
                columns: new[] { "material_id", "integration_config_id", "external_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_material_sublots_parent_lot_id_sublot_number",
                schema: "materials",
                table: "material_sublots",
                columns: new[] { "parent_lot_id", "sublot_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_material_types_code",
                schema: "materials",
                table: "material_types",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_materials_code",
                schema: "materials",
                table: "materials",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_materials_material_type_id",
                schema: "materials",
                table: "materials",
                column: "material_type_id");

            migrationBuilder.CreateIndex(
                name: "IX_materials_supplier_id",
                schema: "materials",
                table: "materials",
                column: "supplier_id");

            migrationBuilder.CreateIndex(
                name: "IX_measurements_lot_id",
                schema: "quality",
                table: "measurements",
                column: "lot_id");

            migrationBuilder.CreateIndex(
                name: "IX_measurements_measurement_equipment_id",
                schema: "quality",
                table: "measurements",
                column: "measurement_equipment_id");

            migrationBuilder.CreateIndex(
                name: "IX_measurements_process_run_id",
                schema: "quality",
                table: "measurements",
                column: "process_run_id");

            migrationBuilder.CreateIndex(
                name: "IX_measurements_quality_check_id",
                schema: "quality",
                table: "measurements",
                column: "quality_check_id");

            migrationBuilder.CreateIndex(
                name: "IX_measurements_recorded_by_id",
                schema: "quality",
                table: "measurements",
                column: "recorded_by_id");

            migrationBuilder.CreateIndex(
                name: "IX_measurements_specification_version_id",
                schema: "quality",
                table: "measurements",
                column: "specification_version_id");

            migrationBuilder.CreateIndex(
                name: "IX_measurements_wafer_id_measured_at",
                schema: "quality",
                table: "measurements",
                columns: new[] { "wafer_id", "measured_at" });

            migrationBuilder.CreateIndex(
                name: "IX_number_sequences_name",
                schema: "engineering",
                table: "number_sequences",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_number_sequences_product_id",
                schema: "engineering",
                table: "number_sequences",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "IX_operations_code",
                schema: "engineering",
                table: "operations",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_operations_equipment_type_id",
                schema: "engineering",
                table: "operations",
                column: "equipment_type_id");

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_next_attempt_at_occurred_at",
                schema: "audit",
                table: "outbox_messages",
                columns: new[] { "next_attempt_at", "occurred_at" },
                filter: "published_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_output_definitions_route_step_id_key",
                schema: "engineering",
                table: "output_definitions",
                columns: new[] { "route_step_id", "key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_output_values_output_id",
                schema: "production",
                table: "output_values",
                column: "output_id");

            migrationBuilder.CreateIndex(
                name: "IX_output_values_recorded_by_id",
                schema: "production",
                table: "output_values",
                column: "recorded_by_id");

            migrationBuilder.CreateIndex(
                name: "IX_output_values_step_execution_id",
                schema: "production",
                table: "output_values",
                column: "step_execution_id");

            migrationBuilder.CreateIndex(
                name: "IX_permissions_code",
                schema: "security",
                table: "permissions",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_process_batch_members_lot_id",
                schema: "production",
                table: "process_batch_members",
                column: "lot_id");

            migrationBuilder.CreateIndex(
                name: "IX_process_batch_members_process_batch_id_position",
                schema: "production",
                table: "process_batch_members",
                columns: new[] { "process_batch_id", "position" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_process_batch_members_process_batch_id_wafer_id",
                schema: "production",
                table: "process_batch_members",
                columns: new[] { "process_batch_id", "wafer_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_process_batch_members_process_run_id",
                schema: "production",
                table: "process_batch_members",
                column: "process_run_id");

            migrationBuilder.CreateIndex(
                name: "IX_process_batch_members_wafer_id",
                schema: "production",
                table: "process_batch_members",
                column: "wafer_id");

            migrationBuilder.CreateIndex(
                name: "IX_process_batches_code",
                schema: "production",
                table: "process_batches",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_process_batches_equipment_id",
                schema: "production",
                table: "process_batches",
                column: "equipment_id");

            migrationBuilder.CreateIndex(
                name: "IX_process_confirmations_confirmed_by_id",
                schema: "production",
                table: "process_confirmations",
                column: "confirmed_by_id");

            migrationBuilder.CreateIndex(
                name: "IX_process_confirmations_lot_id",
                schema: "production",
                table: "process_confirmations",
                column: "lot_id");

            migrationBuilder.CreateIndex(
                name: "IX_process_confirmations_step_execution_id",
                schema: "production",
                table: "process_confirmations",
                column: "step_execution_id");

            migrationBuilder.CreateIndex(
                name: "IX_process_run_wafers_process_run_id_wafer_id",
                schema: "production",
                table: "process_run_wafers",
                columns: new[] { "process_run_id", "wafer_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_process_run_wafers_wafer_id",
                schema: "production",
                table: "process_run_wafers",
                column: "wafer_id");

            migrationBuilder.CreateIndex(
                name: "IX_process_runs_chamber_id",
                schema: "production",
                table: "process_runs",
                column: "chamber_id");

            migrationBuilder.CreateIndex(
                name: "IX_process_runs_equipment_id",
                schema: "production",
                table: "process_runs",
                column: "equipment_id",
                unique: true,
                filter: "status = 'Running'");

            migrationBuilder.CreateIndex(
                name: "IX_process_runs_recipe_version_id",
                schema: "production",
                table: "process_runs",
                column: "recipe_version_id");

            migrationBuilder.CreateIndex(
                name: "IX_process_runs_step_execution_id",
                schema: "production",
                table: "process_runs",
                column: "step_execution_id",
                unique: true,
                filter: "status = 'Running'");

            migrationBuilder.CreateIndex(
                name: "IX_process_runs_track_in_by_id",
                schema: "production",
                table: "process_runs",
                column: "track_in_by_id");

            migrationBuilder.CreateIndex(
                name: "IX_process_runs_track_out_by_id",
                schema: "production",
                table: "process_runs",
                column: "track_out_by_id");

            migrationBuilder.CreateIndex(
                name: "IX_product_revisions_product_id_revision_code",
                schema: "engineering",
                table: "product_revisions",
                columns: new[] { "product_id", "revision_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_product_revisions_released_by_id",
                schema: "engineering",
                table: "product_revisions",
                column: "released_by_id");

            migrationBuilder.CreateIndex(
                name: "IX_product_revisions_route_version_id",
                schema: "engineering",
                table: "product_revisions",
                column: "route_version_id");

            migrationBuilder.CreateIndex(
                name: "IX_products_code",
                schema: "engineering",
                table: "products",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_q_time_rules_end_route_step_id",
                schema: "engineering",
                table: "q_time_rules",
                column: "end_route_step_id");

            migrationBuilder.CreateIndex(
                name: "IX_q_time_rules_start_route_step_id",
                schema: "engineering",
                table: "q_time_rules",
                column: "start_route_step_id");

            migrationBuilder.CreateIndex(
                name: "IX_q_time_windows_lot_id",
                schema: "production",
                table: "q_time_windows",
                column: "lot_id");

            migrationBuilder.CreateIndex(
                name: "IX_q_time_windows_q_time_rule_id",
                schema: "production",
                table: "q_time_windows",
                column: "q_time_rule_id");

            migrationBuilder.CreateIndex(
                name: "IX_quality_check_templates_route_step_id",
                schema: "quality",
                table: "quality_check_templates",
                column: "route_step_id");

            migrationBuilder.CreateIndex(
                name: "IX_quality_check_templates_route_version_id_name_version",
                schema: "quality",
                table: "quality_check_templates",
                columns: new[] { "route_version_id", "name", "version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_quality_checks_checked_by_id",
                schema: "quality",
                table: "quality_checks",
                column: "checked_by_id");

            migrationBuilder.CreateIndex(
                name: "IX_quality_checks_lot_id",
                schema: "quality",
                table: "quality_checks",
                column: "lot_id");

            migrationBuilder.CreateIndex(
                name: "IX_quality_checks_process_run_id",
                schema: "quality",
                table: "quality_checks",
                column: "process_run_id");

            migrationBuilder.CreateIndex(
                name: "IX_quality_checks_quality_check_template_id",
                schema: "quality",
                table: "quality_checks",
                column: "quality_check_template_id");

            migrationBuilder.CreateIndex(
                name: "IX_quality_tasks_completed_by_id",
                schema: "quality",
                table: "quality_tasks",
                column: "completed_by_id");

            migrationBuilder.CreateIndex(
                name: "IX_quality_tasks_equipment_id",
                schema: "quality",
                table: "quality_tasks",
                column: "equipment_id");

            migrationBuilder.CreateIndex(
                name: "IX_quality_tasks_issue_id",
                schema: "quality",
                table: "quality_tasks",
                column: "issue_id");

            migrationBuilder.CreateIndex(
                name: "IX_quality_tasks_lot_id",
                schema: "quality",
                table: "quality_tasks",
                column: "lot_id");

            migrationBuilder.CreateIndex(
                name: "IX_quality_tasks_quality_check_id",
                schema: "quality",
                table: "quality_tasks",
                column: "quality_check_id");

            migrationBuilder.CreateIndex(
                name: "IX_quality_tasks_quality_trigger_id",
                schema: "quality",
                table: "quality_tasks",
                column: "quality_trigger_id");

            migrationBuilder.CreateIndex(
                name: "IX_quality_triggers_equipment_id",
                schema: "quality",
                table: "quality_triggers",
                column: "equipment_id");

            migrationBuilder.CreateIndex(
                name: "IX_quality_triggers_product_id",
                schema: "quality",
                table: "quality_triggers",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "IX_quality_triggers_quality_check_template_id",
                schema: "quality",
                table: "quality_triggers",
                column: "quality_check_template_id");

            migrationBuilder.CreateIndex(
                name: "IX_reason_codes_category_code",
                schema: "quality",
                table: "reason_codes",
                columns: new[] { "category", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_recipe_applicabilities_equipment_id",
                schema: "engineering",
                table: "recipe_applicabilities",
                column: "equipment_id");

            migrationBuilder.CreateIndex(
                name: "IX_recipe_applicabilities_recipe_version_id",
                schema: "engineering",
                table: "recipe_applicabilities",
                column: "recipe_version_id");

            migrationBuilder.CreateIndex(
                name: "IX_recipe_applicabilities_route_step_id",
                schema: "engineering",
                table: "recipe_applicabilities",
                column: "route_step_id");

            migrationBuilder.CreateIndex(
                name: "IX_recipe_versions_recipe_id_version",
                schema: "engineering",
                table: "recipe_versions",
                columns: new[] { "recipe_id", "version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_recipe_versions_released_by_id",
                schema: "engineering",
                table: "recipe_versions",
                column: "released_by_id");

            migrationBuilder.CreateIndex(
                name: "IX_recipes_code",
                schema: "engineering",
                table: "recipes",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_reticle_usages_process_run_id",
                schema: "equipment",
                table: "reticle_usages",
                column: "process_run_id");

            migrationBuilder.CreateIndex(
                name: "IX_reticle_usages_reticle_id",
                schema: "equipment",
                table: "reticle_usages",
                column: "reticle_id");

            migrationBuilder.CreateIndex(
                name: "IX_reticles_code_revision",
                schema: "equipment",
                table: "reticles",
                columns: new[] { "code", "revision" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_rework_records_approved_by_id",
                schema: "production",
                table: "rework_records",
                column: "approved_by_id");

            migrationBuilder.CreateIndex(
                name: "IX_rework_records_lot_id",
                schema: "production",
                table: "rework_records",
                column: "lot_id");

            migrationBuilder.CreateIndex(
                name: "IX_rework_records_original_step_execution_id",
                schema: "production",
                table: "rework_records",
                column: "original_step_execution_id");

            migrationBuilder.CreateIndex(
                name: "IX_rework_records_reason_code_id",
                schema: "production",
                table: "rework_records",
                column: "reason_code_id");

            migrationBuilder.CreateIndex(
                name: "IX_rework_records_return_route_step_id",
                schema: "production",
                table: "rework_records",
                column: "return_route_step_id");

            migrationBuilder.CreateIndex(
                name: "IX_rework_records_rework_route_version_id",
                schema: "production",
                table: "rework_records",
                column: "rework_route_version_id");

            migrationBuilder.CreateIndex(
                name: "IX_role_claims_role_id",
                schema: "security",
                table: "role_claims",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "IX_role_permissions_permission_id",
                schema: "security",
                table: "role_permissions",
                column: "permission_id");

            migrationBuilder.CreateIndex(
                name: "IX_role_permissions_role_id_permission_id",
                schema: "security",
                table: "role_permissions",
                columns: new[] { "role_id", "permission_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                schema: "security",
                table: "roles",
                column: "normalized_name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_route_steps_equipment_type_id",
                schema: "engineering",
                table: "route_steps",
                column: "equipment_type_id");

            migrationBuilder.CreateIndex(
                name: "IX_route_steps_operation_id",
                schema: "engineering",
                table: "route_steps",
                column: "operation_id");

            migrationBuilder.CreateIndex(
                name: "IX_route_steps_route_version_id_sequence_no",
                schema: "engineering",
                table: "route_steps",
                columns: new[] { "route_version_id", "sequence_no" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_route_versions_released_by_id",
                schema: "engineering",
                table: "route_versions",
                column: "released_by_id");

            migrationBuilder.CreateIndex(
                name: "IX_route_versions_route_id_version",
                schema: "engineering",
                table: "route_versions",
                columns: new[] { "route_id", "version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_routes_code",
                schema: "engineering",
                table: "routes",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_routes_product_id",
                schema: "engineering",
                table: "routes",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "IX_scrap_entries_reason_code_id",
                schema: "quality",
                table: "scrap_entries",
                column: "reason_code_id");

            migrationBuilder.CreateIndex(
                name: "IX_scrap_entries_reported_by_id",
                schema: "quality",
                table: "scrap_entries",
                column: "reported_by_id");

            migrationBuilder.CreateIndex(
                name: "IX_scrap_entries_shift_id",
                schema: "quality",
                table: "scrap_entries",
                column: "shift_id");

            migrationBuilder.CreateIndex(
                name: "IX_scrap_entries_step_execution_id",
                schema: "quality",
                table: "scrap_entries",
                column: "step_execution_id");

            migrationBuilder.CreateIndex(
                name: "IX_scrap_entries_work_order_id",
                schema: "quality",
                table: "scrap_entries",
                column: "work_order_id");

            migrationBuilder.CreateIndex(
                name: "IX_shift_handovers_confirmed_by_id",
                schema: "organization",
                table: "shift_handovers",
                column: "confirmed_by_id");

            migrationBuilder.CreateIndex(
                name: "IX_shift_handovers_line_id",
                schema: "organization",
                table: "shift_handovers",
                column: "line_id");

            migrationBuilder.CreateIndex(
                name: "IX_shift_handovers_shift_id_business_date",
                schema: "organization",
                table: "shift_handovers",
                columns: new[] { "shift_id", "business_date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_shifts_fab_id_code",
                schema: "organization",
                table: "shifts",
                columns: new[] { "fab_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_shifts_line_id",
                schema: "organization",
                table: "shifts",
                column: "line_id");

            migrationBuilder.CreateIndex(
                name: "IX_specification_versions_parameter_code_version",
                schema: "quality",
                table: "specification_versions",
                columns: new[] { "parameter_code", "version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_specification_versions_released_by_id",
                schema: "quality",
                table: "specification_versions",
                column: "released_by_id");

            migrationBuilder.CreateIndex(
                name: "IX_step_documents_engineering_document_id",
                schema: "production",
                table: "step_documents",
                column: "engineering_document_id");

            migrationBuilder.CreateIndex(
                name: "IX_step_documents_step_execution_id",
                schema: "production",
                table: "step_documents",
                column: "step_execution_id");

            migrationBuilder.CreateIndex(
                name: "IX_step_documents_validated_by_id",
                schema: "production",
                table: "step_documents",
                column: "validated_by_id");

            migrationBuilder.CreateIndex(
                name: "IX_step_executions_assigned_by_id",
                schema: "production",
                table: "step_executions",
                column: "assigned_by_id");

            migrationBuilder.CreateIndex(
                name: "IX_step_executions_confirmed_by_id",
                schema: "production",
                table: "step_executions",
                column: "confirmed_by_id");

            migrationBuilder.CreateIndex(
                name: "IX_step_executions_lot_id",
                schema: "production",
                table: "step_executions",
                column: "lot_id",
                unique: true,
                filter: "exited_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_step_executions_lot_id_route_step_id_visit_no",
                schema: "production",
                table: "step_executions",
                columns: new[] { "lot_id", "route_step_id", "visit_no" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_step_executions_lot_id_route_version_id",
                schema: "production",
                table: "step_executions",
                columns: new[] { "lot_id", "route_version_id" });

            migrationBuilder.CreateIndex(
                name: "IX_step_executions_route_step_id_route_version_id",
                schema: "production",
                table: "step_executions",
                columns: new[] { "route_step_id", "route_version_id" });

            migrationBuilder.CreateIndex(
                name: "IX_step_executions_route_version_id",
                schema: "production",
                table: "step_executions",
                column: "route_version_id");

            migrationBuilder.CreateIndex(
                name: "IX_stock_document_lines_material_id",
                schema: "materials",
                table: "stock_document_lines",
                column: "material_id");

            migrationBuilder.CreateIndex(
                name: "IX_stock_document_lines_material_lot_id",
                schema: "materials",
                table: "stock_document_lines",
                column: "material_lot_id");

            migrationBuilder.CreateIndex(
                name: "IX_stock_document_lines_product_id",
                schema: "materials",
                table: "stock_document_lines",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "IX_stock_document_lines_stock_document_id",
                schema: "materials",
                table: "stock_document_lines",
                column: "stock_document_id");

            migrationBuilder.CreateIndex(
                name: "IX_stock_documents_document_no",
                schema: "materials",
                table: "stock_documents",
                column: "document_no",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_stock_documents_lot_id",
                schema: "materials",
                table: "stock_documents",
                column: "lot_id");

            migrationBuilder.CreateIndex(
                name: "IX_stock_documents_posted_by_id",
                schema: "materials",
                table: "stock_documents",
                column: "posted_by_id");

            migrationBuilder.CreateIndex(
                name: "IX_stock_documents_warehouse_id",
                schema: "materials",
                table: "stock_documents",
                column: "warehouse_id");

            migrationBuilder.CreateIndex(
                name: "IX_stock_documents_work_order_id",
                schema: "materials",
                table: "stock_documents",
                column: "work_order_id");

            migrationBuilder.CreateIndex(
                name: "IX_stock_movements_material_id",
                schema: "materials",
                table: "stock_movements",
                column: "material_id");

            migrationBuilder.CreateIndex(
                name: "IX_stock_movements_material_lot_id",
                schema: "materials",
                table: "stock_movements",
                column: "material_lot_id");

            migrationBuilder.CreateIndex(
                name: "IX_stock_movements_performed_by_id",
                schema: "materials",
                table: "stock_movements",
                column: "performed_by_id");

            migrationBuilder.CreateIndex(
                name: "IX_stock_movements_warehouse_id",
                schema: "materials",
                table: "stock_movements",
                column: "warehouse_id");

            migrationBuilder.CreateIndex(
                name: "IX_terminals_device_uuid",
                schema: "organization",
                table: "terminals",
                column: "device_uuid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_terminals_workstation_id",
                schema: "organization",
                table: "terminals",
                column: "workstation_id");

            migrationBuilder.CreateIndex(
                name: "IX_tools_code",
                schema: "equipment",
                table: "tools",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tools_equipment_type_id",
                schema: "equipment",
                table: "tools",
                column: "equipment_type_id");

            migrationBuilder.CreateIndex(
                name: "IX_track_events_actor_id",
                schema: "production",
                table: "track_events",
                column: "actor_id");

            migrationBuilder.CreateIndex(
                name: "IX_track_events_idempotency_key",
                schema: "production",
                table: "track_events",
                column: "idempotency_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_track_events_process_run_id_occurred_at",
                schema: "production",
                table: "track_events",
                columns: new[] { "process_run_id", "occurred_at" });

            migrationBuilder.CreateIndex(
                name: "IX_user_claims_user_id",
                schema: "security",
                table: "user_claims",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_fab_scopes_fab_id",
                schema: "security",
                table: "user_fab_scopes",
                column: "fab_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_fab_scopes_user_id_fab_id",
                schema: "security",
                table: "user_fab_scopes",
                columns: new[] { "user_id", "fab_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_logins_user_id",
                schema: "security",
                table: "user_logins",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_permissions_permission_id",
                schema: "security",
                table: "user_permissions",
                column: "permission_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_permissions_user_id_permission_id",
                schema: "security",
                table: "user_permissions",
                columns: new[] { "user_id", "permission_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_roles_role_id",
                schema: "security",
                table: "user_roles",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                schema: "security",
                table: "users",
                column: "normalized_email");

            migrationBuilder.CreateIndex(
                name: "IX_users_worker_id",
                schema: "security",
                table: "users",
                column: "worker_id");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                schema: "security",
                table: "users",
                column: "normalized_user_name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_wafer_dispositions_approved_by_id",
                schema: "quality",
                table: "wafer_dispositions",
                column: "approved_by_id");

            migrationBuilder.CreateIndex(
                name: "IX_wafer_dispositions_issue_id",
                schema: "quality",
                table: "wafer_dispositions",
                column: "issue_id");

            migrationBuilder.CreateIndex(
                name: "IX_wafer_dispositions_lot_id_at_event",
                schema: "quality",
                table: "wafer_dispositions",
                column: "lot_id_at_event");

            migrationBuilder.CreateIndex(
                name: "IX_wafer_dispositions_performed_by_id",
                schema: "quality",
                table: "wafer_dispositions",
                column: "performed_by_id");

            migrationBuilder.CreateIndex(
                name: "IX_wafer_dispositions_process_run_id",
                schema: "quality",
                table: "wafer_dispositions",
                column: "process_run_id");

            migrationBuilder.CreateIndex(
                name: "IX_wafer_dispositions_reason_code_id",
                schema: "quality",
                table: "wafer_dispositions",
                column: "reason_code_id");

            migrationBuilder.CreateIndex(
                name: "IX_wafer_dispositions_scrap_entry_id",
                schema: "quality",
                table: "wafer_dispositions",
                column: "scrap_entry_id");

            migrationBuilder.CreateIndex(
                name: "IX_wafer_dispositions_wafer_id",
                schema: "quality",
                table: "wafer_dispositions",
                column: "wafer_id",
                unique: true,
                filter: "disposition_type = 'Scrap'");

            migrationBuilder.CreateIndex(
                name: "IX_wafers_origin_material_lot_id",
                schema: "production",
                table: "wafers",
                column: "origin_material_lot_id");

            migrationBuilder.CreateIndex(
                name: "IX_wafers_wafer_code",
                schema: "production",
                table: "wafers",
                column: "wafer_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_warehouse_stocks_material_id",
                schema: "materials",
                table: "warehouse_stocks",
                column: "material_id");

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

            migrationBuilder.CreateIndex(
                name: "IX_warehouses_fab_id_code",
                schema: "materials",
                table: "warehouses",
                columns: new[] { "fab_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_work_order_change_requests_applied_by_id",
                schema: "production",
                table: "work_order_change_requests",
                column: "applied_by_id");

            migrationBuilder.CreateIndex(
                name: "IX_work_order_change_requests_approved_by_id",
                schema: "production",
                table: "work_order_change_requests",
                column: "approved_by_id");

            migrationBuilder.CreateIndex(
                name: "IX_work_order_change_requests_code",
                schema: "production",
                table: "work_order_change_requests",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_work_order_change_requests_effective_from_lot_id",
                schema: "production",
                table: "work_order_change_requests",
                column: "effective_from_lot_id");

            migrationBuilder.CreateIndex(
                name: "IX_work_order_change_requests_rejected_by_id",
                schema: "production",
                table: "work_order_change_requests",
                column: "rejected_by_id");

            migrationBuilder.CreateIndex(
                name: "IX_work_order_change_requests_requested_by_id",
                schema: "production",
                table: "work_order_change_requests",
                column: "requested_by_id");

            migrationBuilder.CreateIndex(
                name: "IX_work_order_change_requests_work_order_id",
                schema: "production",
                table: "work_order_change_requests",
                column: "work_order_id");

            migrationBuilder.CreateIndex(
                name: "IX_work_order_snapshots_change_request_id",
                schema: "production",
                table: "work_order_snapshots",
                column: "change_request_id");

            migrationBuilder.CreateIndex(
                name: "IX_work_order_snapshots_created_by_id",
                schema: "production",
                table: "work_order_snapshots",
                column: "created_by_id");

            migrationBuilder.CreateIndex(
                name: "IX_work_order_snapshots_effective_from_lot_id",
                schema: "production",
                table: "work_order_snapshots",
                column: "effective_from_lot_id");

            migrationBuilder.CreateIndex(
                name: "IX_work_order_snapshots_work_order_id_version",
                schema: "production",
                table: "work_order_snapshots",
                columns: new[] { "work_order_id", "version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_work_order_stops_issue_id",
                schema: "production",
                table: "work_order_stops",
                column: "issue_id");

            migrationBuilder.CreateIndex(
                name: "IX_work_order_stops_lot_id",
                schema: "production",
                table: "work_order_stops",
                column: "lot_id");

            migrationBuilder.CreateIndex(
                name: "IX_work_order_stops_resumed_by_id",
                schema: "production",
                table: "work_order_stops",
                column: "resumed_by_id");

            migrationBuilder.CreateIndex(
                name: "IX_work_order_stops_stopped_by_id",
                schema: "production",
                table: "work_order_stops",
                column: "stopped_by_id");

            migrationBuilder.CreateIndex(
                name: "IX_work_order_stops_work_order_id",
                schema: "production",
                table: "work_order_stops",
                column: "work_order_id");

            migrationBuilder.CreateIndex(
                name: "IX_work_orders_customer_id",
                schema: "production",
                table: "work_orders",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "IX_work_orders_fab_id",
                schema: "production",
                table: "work_orders",
                column: "fab_id");

            migrationBuilder.CreateIndex(
                name: "IX_work_orders_order_no",
                schema: "production",
                table: "work_orders",
                column: "order_no",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_work_orders_product_id",
                schema: "production",
                table: "work_orders",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "IX_work_orders_product_revision_id_product_id",
                schema: "production",
                table: "work_orders",
                columns: new[] { "product_revision_id", "product_id" });

            migrationBuilder.CreateIndex(
                name: "IX_worker_qualifications_operation_id",
                schema: "equipment",
                table: "worker_qualifications",
                column: "operation_id");

            migrationBuilder.CreateIndex(
                name: "IX_worker_qualifications_qualified_by_id",
                schema: "equipment",
                table: "worker_qualifications",
                column: "qualified_by_id");

            migrationBuilder.CreateIndex(
                name: "IX_worker_qualifications_recipe_version_id",
                schema: "equipment",
                table: "worker_qualifications",
                column: "recipe_version_id");

            migrationBuilder.CreateIndex(
                name: "IX_worker_qualifications_worker_id",
                schema: "equipment",
                table: "worker_qualifications",
                column: "worker_id");

            migrationBuilder.CreateIndex(
                name: "IX_workers_fab_id_code",
                schema: "organization",
                table: "workers",
                columns: new[] { "fab_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_workstations_area_id_code",
                schema: "organization",
                table: "workstations",
                columns: new[] { "area_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_workstations_line_id",
                schema: "organization",
                table: "workstations",
                column: "line_id");

            migrationBuilder.AddForeignKey(
                name: "FK_allocation_lot_picks_material_lots_material_lot_id",
                schema: "materials",
                table: "allocation_lot_picks",
                column: "material_lot_id",
                principalSchema: "materials",
                principalTable: "material_lots",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_carrier_loads_wafers_wafer_id",
                schema: "production",
                table: "carrier_loads",
                column: "wafer_id",
                principalSchema: "production",
                principalTable: "wafers",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_genealogy_details_wafers_wafer_id",
                schema: "production",
                table: "genealogy_details",
                column: "wafer_id",
                principalSchema: "production",
                principalTable: "wafers",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_hold_records_issues_issue_id",
                schema: "quality",
                table: "hold_records",
                column: "issue_id",
                principalSchema: "quality",
                principalTable: "issues",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_incoming_inspections_issues_issue_id",
                schema: "quality",
                table: "incoming_inspections",
                column: "issue_id",
                principalSchema: "quality",
                principalTable: "issues",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_incoming_inspections_material_lots_material_lot_id",
                schema: "quality",
                table: "incoming_inspections",
                column: "material_lot_id",
                principalSchema: "materials",
                principalTable: "material_lots",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_issue_actions_issues_issue_id",
                schema: "quality",
                table: "issue_actions",
                column: "issue_id",
                principalSchema: "quality",
                principalTable: "issues",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_issues_wafers_wafer_id",
                schema: "quality",
                table: "issues",
                column: "wafer_id",
                principalSchema: "production",
                principalTable: "wafers",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_incoming_inspections_material_lots_material_lot_id",
                schema: "quality",
                table: "incoming_inspections");

            migrationBuilder.DropForeignKey(
                name: "FK_wafers_material_lots_origin_material_lot_id",
                schema: "production",
                table: "wafers");

            migrationBuilder.DropTable(
                name: "allocation_lot_picks",
                schema: "materials");

            migrationBuilder.DropTable(
                name: "api_keys",
                schema: "integration");

            migrationBuilder.DropTable(
                name: "attachments",
                schema: "audit");

            migrationBuilder.DropTable(
                name: "audit_logs",
                schema: "audit");

            migrationBuilder.DropTable(
                name: "bom_items",
                schema: "materials");

            migrationBuilder.DropTable(
                name: "business_events",
                schema: "audit");

            migrationBuilder.DropTable(
                name: "carrier_loads",
                schema: "production");

            migrationBuilder.DropTable(
                name: "carrier_movements",
                schema: "production");

            migrationBuilder.DropTable(
                name: "checklist_completions",
                schema: "production");

            migrationBuilder.DropTable(
                name: "command_receipts",
                schema: "audit");

            migrationBuilder.DropTable(
                name: "custom_field_definitions",
                schema: "integration");

            migrationBuilder.DropTable(
                name: "downtimes",
                schema: "equipment");

            migrationBuilder.DropTable(
                name: "equipment_events",
                schema: "equipment");

            migrationBuilder.DropTable(
                name: "equipment_qualifications",
                schema: "equipment");

            migrationBuilder.DropTable(
                name: "equipment_states",
                schema: "equipment");

            migrationBuilder.DropTable(
                name: "genealogy_details",
                schema: "production");

            migrationBuilder.DropTable(
                name: "hold_records",
                schema: "quality");

            migrationBuilder.DropTable(
                name: "import_jobs",
                schema: "integration");

            migrationBuilder.DropTable(
                name: "inspection_results",
                schema: "quality");

            migrationBuilder.DropTable(
                name: "issue_actions",
                schema: "quality");

            migrationBuilder.DropTable(
                name: "label_templates",
                schema: "integration");

            migrationBuilder.DropTable(
                name: "lot_memberships",
                schema: "production");

            migrationBuilder.DropTable(
                name: "lot_releases",
                schema: "quality");

            migrationBuilder.DropTable(
                name: "machine_messages",
                schema: "integration");

            migrationBuilder.DropTable(
                name: "machine_tags",
                schema: "integration");

            migrationBuilder.DropTable(
                name: "maintenance_events",
                schema: "equipment");

            migrationBuilder.DropTable(
                name: "material_consumptions",
                schema: "materials");

            migrationBuilder.DropTable(
                name: "material_holds",
                schema: "quality");

            migrationBuilder.DropTable(
                name: "material_mounts",
                schema: "equipment");

            migrationBuilder.DropTable(
                name: "measurements",
                schema: "quality");

            migrationBuilder.DropTable(
                name: "number_sequences",
                schema: "engineering");

            migrationBuilder.DropTable(
                name: "outbox_messages",
                schema: "audit");

            migrationBuilder.DropTable(
                name: "output_values",
                schema: "production");

            migrationBuilder.DropTable(
                name: "process_batch_members",
                schema: "production");

            migrationBuilder.DropTable(
                name: "process_confirmations",
                schema: "production");

            migrationBuilder.DropTable(
                name: "process_run_wafers",
                schema: "production");

            migrationBuilder.DropTable(
                name: "q_time_windows",
                schema: "production");

            migrationBuilder.DropTable(
                name: "quality_tasks",
                schema: "quality");

            migrationBuilder.DropTable(
                name: "recipe_applicabilities",
                schema: "engineering");

            migrationBuilder.DropTable(
                name: "reticle_usages",
                schema: "equipment");

            migrationBuilder.DropTable(
                name: "rework_records",
                schema: "production");

            migrationBuilder.DropTable(
                name: "role_claims",
                schema: "security");

            migrationBuilder.DropTable(
                name: "role_permissions",
                schema: "security");

            migrationBuilder.DropTable(
                name: "shift_handovers",
                schema: "organization");

            migrationBuilder.DropTable(
                name: "step_documents",
                schema: "production");

            migrationBuilder.DropTable(
                name: "stock_document_lines",
                schema: "materials");

            migrationBuilder.DropTable(
                name: "stock_movements",
                schema: "materials");

            migrationBuilder.DropTable(
                name: "terminals",
                schema: "organization");

            migrationBuilder.DropTable(
                name: "track_events",
                schema: "production");

            migrationBuilder.DropTable(
                name: "user_claims",
                schema: "security");

            migrationBuilder.DropTable(
                name: "user_fab_scopes",
                schema: "security");

            migrationBuilder.DropTable(
                name: "user_logins",
                schema: "security");

            migrationBuilder.DropTable(
                name: "user_permissions",
                schema: "security");

            migrationBuilder.DropTable(
                name: "user_roles",
                schema: "security");

            migrationBuilder.DropTable(
                name: "user_tokens",
                schema: "security");

            migrationBuilder.DropTable(
                name: "wafer_dispositions",
                schema: "quality");

            migrationBuilder.DropTable(
                name: "warehouse_stocks",
                schema: "materials");

            migrationBuilder.DropTable(
                name: "work_order_snapshots",
                schema: "production");

            migrationBuilder.DropTable(
                name: "work_order_stops",
                schema: "production");

            migrationBuilder.DropTable(
                name: "worker_qualifications",
                schema: "equipment");

            migrationBuilder.DropTable(
                name: "material_allocations",
                schema: "materials");

            migrationBuilder.DropTable(
                name: "carrier_slots",
                schema: "production");

            migrationBuilder.DropTable(
                name: "checklist_items",
                schema: "engineering");

            migrationBuilder.DropTable(
                name: "import_mappings",
                schema: "integration");

            migrationBuilder.DropTable(
                name: "genealogy_events",
                schema: "production");

            migrationBuilder.DropTable(
                name: "machine_connections",
                schema: "integration");

            migrationBuilder.DropTable(
                name: "maintenance_schedules",
                schema: "equipment");

            migrationBuilder.DropTable(
                name: "material_sublots",
                schema: "materials");

            migrationBuilder.DropTable(
                name: "specification_versions",
                schema: "quality");

            migrationBuilder.DropTable(
                name: "output_definitions",
                schema: "engineering");

            migrationBuilder.DropTable(
                name: "process_batches",
                schema: "production");

            migrationBuilder.DropTable(
                name: "q_time_rules",
                schema: "engineering");

            migrationBuilder.DropTable(
                name: "quality_checks",
                schema: "quality");

            migrationBuilder.DropTable(
                name: "quality_triggers",
                schema: "quality");

            migrationBuilder.DropTable(
                name: "reticles",
                schema: "equipment");

            migrationBuilder.DropTable(
                name: "engineering_documents",
                schema: "engineering");

            migrationBuilder.DropTable(
                name: "stock_documents",
                schema: "materials");

            migrationBuilder.DropTable(
                name: "workstations",
                schema: "organization");

            migrationBuilder.DropTable(
                name: "permissions",
                schema: "security");

            migrationBuilder.DropTable(
                name: "roles",
                schema: "security");

            migrationBuilder.DropTable(
                name: "scrap_entries",
                schema: "quality");

            migrationBuilder.DropTable(
                name: "work_order_change_requests",
                schema: "production");

            migrationBuilder.DropTable(
                name: "carriers",
                schema: "production");

            migrationBuilder.DropTable(
                name: "tools",
                schema: "equipment");

            migrationBuilder.DropTable(
                name: "process_runs",
                schema: "production");

            migrationBuilder.DropTable(
                name: "quality_check_templates",
                schema: "quality");

            migrationBuilder.DropTable(
                name: "reason_codes",
                schema: "quality");

            migrationBuilder.DropTable(
                name: "shifts",
                schema: "organization");

            migrationBuilder.DropTable(
                name: "locations",
                schema: "organization");

            migrationBuilder.DropTable(
                name: "chambers",
                schema: "equipment");

            migrationBuilder.DropTable(
                name: "recipe_versions",
                schema: "engineering");

            migrationBuilder.DropTable(
                name: "recipes",
                schema: "engineering");

            migrationBuilder.DropTable(
                name: "material_lots",
                schema: "materials");

            migrationBuilder.DropTable(
                name: "incoming_inspections",
                schema: "quality");

            migrationBuilder.DropTable(
                name: "material_sources",
                schema: "materials");

            migrationBuilder.DropTable(
                name: "warehouses",
                schema: "materials");

            migrationBuilder.DropTable(
                name: "inspection_plans",
                schema: "quality");

            migrationBuilder.DropTable(
                name: "issues",
                schema: "quality");

            migrationBuilder.DropTable(
                name: "integration_configs",
                schema: "integration");

            migrationBuilder.DropTable(
                name: "materials",
                schema: "materials");

            migrationBuilder.DropTable(
                name: "equipment",
                schema: "equipment");

            migrationBuilder.DropTable(
                name: "issue_types",
                schema: "quality");

            migrationBuilder.DropTable(
                name: "step_executions",
                schema: "production");

            migrationBuilder.DropTable(
                name: "wafers",
                schema: "production");

            migrationBuilder.DropTable(
                name: "material_types",
                schema: "materials");

            migrationBuilder.DropTable(
                name: "equipment_groups",
                schema: "equipment");

            migrationBuilder.DropTable(
                name: "lines",
                schema: "organization");

            migrationBuilder.DropTable(
                name: "lots",
                schema: "production");

            migrationBuilder.DropTable(
                name: "areas",
                schema: "organization");

            migrationBuilder.DropTable(
                name: "route_steps",
                schema: "engineering");

            migrationBuilder.DropTable(
                name: "work_orders",
                schema: "production");

            migrationBuilder.DropTable(
                name: "operations",
                schema: "engineering");

            migrationBuilder.DropTable(
                name: "companies",
                schema: "materials");

            migrationBuilder.DropTable(
                name: "product_revisions",
                schema: "engineering");

            migrationBuilder.DropTable(
                name: "equipment_types",
                schema: "equipment");

            migrationBuilder.DropTable(
                name: "route_versions",
                schema: "engineering");

            migrationBuilder.DropTable(
                name: "routes",
                schema: "engineering");

            migrationBuilder.DropTable(
                name: "users",
                schema: "security");

            migrationBuilder.DropTable(
                name: "products",
                schema: "engineering");

            migrationBuilder.DropTable(
                name: "workers",
                schema: "organization");

            migrationBuilder.DropTable(
                name: "fabs",
                schema: "organization");
        }
    }
}
