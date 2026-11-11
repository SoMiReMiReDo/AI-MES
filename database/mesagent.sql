CREATE TABLE IF NOT EXISTS public.__ef_migrations_history (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___ef_migrations_history" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
        IF NOT EXISTS(SELECT 1 FROM pg_namespace WHERE nspname = 'materials') THEN
            CREATE SCHEMA materials;
        END IF;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
        IF NOT EXISTS(SELECT 1 FROM pg_namespace WHERE nspname = 'integration') THEN
            CREATE SCHEMA integration;
        END IF;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
        IF NOT EXISTS(SELECT 1 FROM pg_namespace WHERE nspname = 'organization') THEN
            CREATE SCHEMA organization;
        END IF;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
        IF NOT EXISTS(SELECT 1 FROM pg_namespace WHERE nspname = 'audit') THEN
            CREATE SCHEMA audit;
        END IF;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
        IF NOT EXISTS(SELECT 1 FROM pg_namespace WHERE nspname = 'production') THEN
            CREATE SCHEMA production;
        END IF;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
        IF NOT EXISTS(SELECT 1 FROM pg_namespace WHERE nspname = 'equipment') THEN
            CREATE SCHEMA equipment;
        END IF;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
        IF NOT EXISTS(SELECT 1 FROM pg_namespace WHERE nspname = 'engineering') THEN
            CREATE SCHEMA engineering;
        END IF;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
        IF NOT EXISTS(SELECT 1 FROM pg_namespace WHERE nspname = 'quality') THEN
            CREATE SCHEMA quality;
        END IF;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
        IF NOT EXISTS(SELECT 1 FROM pg_namespace WHERE nspname = 'security') THEN
            CREATE SCHEMA security;
        END IF;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE EXTENSION IF NOT EXISTS btree_gist;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE TABLE materials.companies (
        id uuid NOT NULL DEFAULT (uuidv7()),
        created_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        updated_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        code character varying(64) NOT NULL,
        name character varying(200) NOT NULL,
        description text,
        is_active boolean NOT NULL DEFAULT TRUE,
        company_type character varying(32) NOT NULL,
        CONSTRAINT "PK_companies" PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE TABLE integration.custom_field_definitions (
        id uuid NOT NULL DEFAULT (uuidv7()),
        created_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        updated_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        entity_type character varying(64) NOT NULL,
        key character varying(64) NOT NULL,
        label character varying(200) NOT NULL,
        field_type character varying(32) NOT NULL,
        config jsonb,
        required boolean NOT NULL,
        position integer NOT NULL,
        is_active boolean NOT NULL DEFAULT TRUE,
        CONSTRAINT "PK_custom_field_definitions" PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE TABLE equipment.equipment_types (
        id uuid NOT NULL DEFAULT (uuidv7()),
        created_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        updated_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        code character varying(64) NOT NULL,
        name character varying(200) NOT NULL,
        description text,
        is_active boolean NOT NULL DEFAULT TRUE,
        CONSTRAINT "PK_equipment_types" PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE TABLE organization.fabs (
        id uuid NOT NULL DEFAULT (uuidv7()),
        created_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        updated_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        code character varying(64) NOT NULL,
        name character varying(200) NOT NULL,
        description text,
        is_active boolean NOT NULL DEFAULT TRUE,
        timezone character varying(64) NOT NULL,
        CONSTRAINT "PK_fabs" PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE TABLE integration.import_mappings (
        id uuid NOT NULL DEFAULT (uuidv7()),
        created_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        updated_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        name character varying(200) NOT NULL,
        mapping_config jsonb NOT NULL,
        entity character varying(64) NOT NULL,
        CONSTRAINT "PK_import_mappings" PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE TABLE integration.integration_configs (
        id uuid NOT NULL DEFAULT (uuidv7()),
        created_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        updated_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        system_type character varying(32) NOT NULL,
        system_name character varying(128) NOT NULL,
        config jsonb,
        secret_reference character varying(255),
        is_active boolean NOT NULL DEFAULT TRUE,
        CONSTRAINT "PK_integration_configs" PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE TABLE quality.issue_types (
        id uuid NOT NULL DEFAULT (uuidv7()),
        created_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        updated_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        code character varying(64) NOT NULL,
        name character varying(200) NOT NULL,
        description text,
        is_active boolean NOT NULL DEFAULT TRUE,
        severity character varying(32) NOT NULL,
        is_blocking boolean NOT NULL,
        CONSTRAINT "PK_issue_types" PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE TABLE integration.label_templates (
        id uuid NOT NULL DEFAULT (uuidv7()),
        created_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        updated_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        name character varying(200) NOT NULL,
        label_type character varying(32) NOT NULL,
        size jsonb NOT NULL,
        fields_config jsonb NOT NULL,
        barcode_format character varying(32) NOT NULL,
        is_active boolean NOT NULL DEFAULT TRUE,
        CONSTRAINT "PK_label_templates" PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE TABLE materials.material_types (
        id uuid NOT NULL DEFAULT (uuidv7()),
        created_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        updated_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        code character varying(64) NOT NULL,
        name character varying(200) NOT NULL,
        description text,
        is_active boolean NOT NULL DEFAULT TRUE,
        CONSTRAINT "PK_material_types" PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE TABLE audit.outbox_messages (
        id uuid NOT NULL DEFAULT (uuidv7()),
        created_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        updated_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        event_type character varying(128) NOT NULL,
        aggregate_type character varying(64) NOT NULL,
        aggregate_id uuid NOT NULL,
        payload jsonb NOT NULL,
        occurred_at timestamp with time zone NOT NULL,
        published_at timestamp with time zone,
        attempts integer NOT NULL,
        next_attempt_at timestamp with time zone,
        last_error text,
        correlation_id uuid NOT NULL,
        CONSTRAINT "PK_outbox_messages" PRIMARY KEY (id),
        CONSTRAINT ck_outbox_messages_1 CHECK (attempts >= 0)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE TABLE security.permissions (
        id uuid NOT NULL DEFAULT (uuidv7()),
        created_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        updated_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        code character varying(64) NOT NULL,
        name character varying(200) NOT NULL,
        description text,
        is_active boolean NOT NULL DEFAULT TRUE,
        CONSTRAINT "PK_permissions" PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE TABLE engineering.products (
        id uuid NOT NULL DEFAULT (uuidv7()),
        created_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        updated_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        code character varying(64) NOT NULL,
        name character varying(200) NOT NULL,
        description text,
        is_active boolean NOT NULL DEFAULT TRUE,
        unit_of_measure character varying(16) NOT NULL,
        category character varying(64),
        external_code character varying(128),
        external_system character varying(64),
        CONSTRAINT "PK_products" PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE TABLE quality.reason_codes (
        id uuid NOT NULL DEFAULT (uuidv7()),
        created_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        updated_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        code character varying(64) NOT NULL,
        name character varying(200) NOT NULL,
        description text,
        is_active boolean NOT NULL DEFAULT TRUE,
        category character varying(32) NOT NULL,
        CONSTRAINT "PK_reason_codes" PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE TABLE engineering.recipes (
        id uuid NOT NULL DEFAULT (uuidv7()),
        created_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        updated_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        code character varying(64) NOT NULL,
        name character varying(200) NOT NULL,
        description text,
        is_active boolean NOT NULL DEFAULT TRUE,
        CONSTRAINT "PK_recipes" PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE TABLE equipment.reticles (
        id uuid NOT NULL DEFAULT (uuidv7()),
        created_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        updated_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        code character varying(64) NOT NULL,
        name character varying(200) NOT NULL,
        description text,
        is_active boolean NOT NULL DEFAULT TRUE,
        revision character varying(32) NOT NULL,
        layer_code character varying(64) NOT NULL,
        status character varying(32) NOT NULL,
        usage_count bigint NOT NULL,
        CONSTRAINT "PK_reticles" PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE TABLE security.roles (
        id uuid NOT NULL,
        name character varying(256),
        normalized_name character varying(256),
        concurrency_stamp text,
        CONSTRAINT "PK_roles" PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE TABLE engineering.operations (
        id uuid NOT NULL DEFAULT (uuidv7()),
        created_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        updated_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        code character varying(64) NOT NULL,
        name character varying(200) NOT NULL,
        description text,
        is_active boolean NOT NULL DEFAULT TRUE,
        segment_type character varying(32) NOT NULL,
        equipment_type_id uuid,
        standard_instruction text,
        parameters jsonb,
        CONSTRAINT "PK_operations" PRIMARY KEY (id),
        CONSTRAINT "FK_operations_equipment_types_equipment_type_id" FOREIGN KEY (equipment_type_id) REFERENCES equipment.equipment_types (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE TABLE equipment.tools (
        id uuid NOT NULL DEFAULT (uuidv7()),
        created_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        updated_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        code character varying(64) NOT NULL,
        name character varying(200) NOT NULL,
        description text,
        is_active boolean NOT NULL DEFAULT TRUE,
        equipment_type_id uuid,
        status character varying(32) NOT NULL,
        next_service_at timestamp with time zone,
        CONSTRAINT "PK_tools" PRIMARY KEY (id),
        CONSTRAINT "FK_tools_equipment_types_equipment_type_id" FOREIGN KEY (equipment_type_id) REFERENCES equipment.equipment_types (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE TABLE organization.areas (
        id uuid NOT NULL DEFAULT (uuidv7()),
        created_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        updated_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        code character varying(64) NOT NULL,
        name character varying(200) NOT NULL,
        description text,
        is_active boolean NOT NULL DEFAULT TRUE,
        fab_id uuid NOT NULL,
        CONSTRAINT "PK_areas" PRIMARY KEY (id),
        CONSTRAINT "FK_areas_fabs_fab_id" FOREIGN KEY (fab_id) REFERENCES organization.fabs (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE TABLE equipment.equipment_groups (
        id uuid NOT NULL DEFAULT (uuidv7()),
        created_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        updated_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        code character varying(64) NOT NULL,
        name character varying(200) NOT NULL,
        description text,
        is_active boolean NOT NULL DEFAULT TRUE,
        fab_id uuid NOT NULL,
        CONSTRAINT "PK_equipment_groups" PRIMARY KEY (id),
        CONSTRAINT "FK_equipment_groups_fabs_fab_id" FOREIGN KEY (fab_id) REFERENCES organization.fabs (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE TABLE materials.warehouses (
        id uuid NOT NULL DEFAULT (uuidv7()),
        created_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        updated_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        code character varying(64) NOT NULL,
        name character varying(200) NOT NULL,
        description text,
        is_active boolean NOT NULL DEFAULT TRUE,
        fab_id uuid NOT NULL,
        kind character varying(32) NOT NULL,
        erp_code character varying(128),
        CONSTRAINT "PK_warehouses" PRIMARY KEY (id),
        CONSTRAINT "FK_warehouses_fabs_fab_id" FOREIGN KEY (fab_id) REFERENCES organization.fabs (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE TABLE organization.workers (
        id uuid NOT NULL DEFAULT (uuidv7()),
        created_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        updated_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        code character varying(64) NOT NULL,
        name character varying(200) NOT NULL,
        description text,
        is_active boolean NOT NULL DEFAULT TRUE,
        fab_id uuid NOT NULL,
        CONSTRAINT "PK_workers" PRIMARY KEY (id),
        CONSTRAINT "FK_workers_fabs_fab_id" FOREIGN KEY (fab_id) REFERENCES organization.fabs (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE TABLE integration.api_keys (
        id uuid NOT NULL DEFAULT (uuidv7()),
        created_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        updated_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        integration_config_id uuid NOT NULL,
        name character varying(200) NOT NULL,
        key_prefix character varying(32) NOT NULL,
        key_hash character varying(128) NOT NULL,
        scopes jsonb NOT NULL,
        ip_allowlist jsonb,
        is_active boolean NOT NULL DEFAULT TRUE,
        last_used_at timestamp with time zone,
        expires_at timestamp with time zone,
        CONSTRAINT "PK_api_keys" PRIMARY KEY (id),
        CONSTRAINT "FK_api_keys_integration_configs_integration_config_id" FOREIGN KEY (integration_config_id) REFERENCES integration.integration_configs (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE TABLE materials.materials (
        id uuid NOT NULL DEFAULT (uuidv7()),
        created_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        updated_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        code character varying(64) NOT NULL,
        name character varying(200) NOT NULL,
        description text,
        is_active boolean NOT NULL DEFAULT TRUE,
        material_type_id uuid NOT NULL,
        unit_of_measure character varying(32) NOT NULL,
        tracking_type character varying(32) NOT NULL,
        supplier_id uuid,
        supplier_code character varying(128),
        external_code character varying(128),
        external_system character varying(64),
        CONSTRAINT "PK_materials" PRIMARY KEY (id),
        CONSTRAINT "FK_materials_companies_supplier_id" FOREIGN KEY (supplier_id) REFERENCES materials.companies (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_materials_material_types_material_type_id" FOREIGN KEY (material_type_id) REFERENCES materials.material_types (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE TABLE engineering.number_sequences (
        id uuid NOT NULL DEFAULT (uuidv7()),
        created_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        updated_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        name character varying(200) NOT NULL,
        product_id uuid,
        purpose character varying(32) NOT NULL,
        prefix character varying(64),
        suffix character varying(64),
        next_number bigint NOT NULL,
        pad_size integer NOT NULL,
        pattern character varying(200),
        reset_period character varying(32),
        last_reset_key character varying(64),
        CONSTRAINT "PK_number_sequences" PRIMARY KEY (id),
        CONSTRAINT ck_number_sequences_1 CHECK (next_number >= 0 AND pad_size >= 0),
        CONSTRAINT "FK_number_sequences_products_product_id" FOREIGN KEY (product_id) REFERENCES engineering.products (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE TABLE engineering.routes (
        id uuid NOT NULL DEFAULT (uuidv7()),
        created_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        updated_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        code character varying(64) NOT NULL,
        name character varying(200) NOT NULL,
        description text,
        is_active boolean NOT NULL DEFAULT TRUE,
        product_id uuid,
        CONSTRAINT "PK_routes" PRIMARY KEY (id),
        CONSTRAINT "FK_routes_products_product_id" FOREIGN KEY (product_id) REFERENCES engineering.products (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE TABLE security.role_claims (
        id integer GENERATED BY DEFAULT AS IDENTITY,
        role_id uuid NOT NULL,
        claim_type text,
        claim_value text,
        CONSTRAINT "PK_role_claims" PRIMARY KEY (id),
        CONSTRAINT "FK_role_claims_roles_role_id" FOREIGN KEY (role_id) REFERENCES security.roles (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE TABLE security.role_permissions (
        id uuid NOT NULL DEFAULT (uuidv7()),
        created_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        updated_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        role_id uuid NOT NULL,
        permission_id uuid NOT NULL,
        CONSTRAINT "PK_role_permissions" PRIMARY KEY (id),
        CONSTRAINT "FK_role_permissions_permissions_permission_id" FOREIGN KEY (permission_id) REFERENCES security.permissions (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_role_permissions_roles_role_id" FOREIGN KEY (role_id) REFERENCES security.roles (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE TABLE organization.lines (
        id uuid NOT NULL DEFAULT (uuidv7()),
        created_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        updated_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        code character varying(64) NOT NULL,
        name character varying(200) NOT NULL,
        description text,
        is_active boolean NOT NULL DEFAULT TRUE,
        area_id uuid NOT NULL,
        CONSTRAINT "PK_lines" PRIMARY KEY (id),
        CONSTRAINT "FK_lines_areas_area_id" FOREIGN KEY (area_id) REFERENCES organization.areas (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE TABLE organization.locations (
        id uuid NOT NULL DEFAULT (uuidv7()),
        created_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        updated_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        code character varying(64) NOT NULL,
        name character varying(200) NOT NULL,
        description text,
        is_active boolean NOT NULL DEFAULT TRUE,
        area_id uuid NOT NULL,
        location_type character varying(32) NOT NULL,
        CONSTRAINT "PK_locations" PRIMARY KEY (id),
        CONSTRAINT "FK_locations_areas_area_id" FOREIGN KEY (area_id) REFERENCES organization.areas (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE TABLE security.users (
        id uuid NOT NULL,
        name character varying(200) NOT NULL,
        worker_id uuid,
        account_type character varying(32) NOT NULL,
        is_active boolean NOT NULL,
        force_password_change boolean NOT NULL,
        last_login_at timestamp with time zone,
        created_at timestamp with time zone NOT NULL,
        user_name character varying(256),
        normalized_user_name character varying(256),
        email character varying(256),
        normalized_email character varying(256),
        email_confirmed boolean NOT NULL,
        password_hash text,
        security_stamp text,
        concurrency_stamp text,
        phone_number text,
        phone_number_confirmed boolean NOT NULL,
        two_factor_enabled boolean NOT NULL,
        lockout_end timestamp with time zone,
        lockout_enabled boolean NOT NULL,
        access_failed_count integer NOT NULL,
        CONSTRAINT "PK_users" PRIMARY KEY (id),
        CONSTRAINT "FK_users_workers_worker_id" FOREIGN KEY (worker_id) REFERENCES organization.workers (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE TABLE quality.inspection_plans (
        id uuid NOT NULL DEFAULT (uuidv7()),
        created_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        updated_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        name character varying(200) NOT NULL,
        material_id uuid,
        material_type_id uuid,
        criteria jsonb NOT NULL,
        version integer NOT NULL,
        published_at timestamp with time zone,
        root_id uuid,
        is_active boolean NOT NULL DEFAULT TRUE,
        CONSTRAINT "PK_inspection_plans" PRIMARY KEY (id),
        CONSTRAINT ck_inspection_plans_1 CHECK (version > 0),
        CONSTRAINT "FK_inspection_plans_inspection_plans_root_id" FOREIGN KEY (root_id) REFERENCES quality.inspection_plans (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_inspection_plans_material_types_material_type_id" FOREIGN KEY (material_type_id) REFERENCES materials.material_types (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_inspection_plans_materials_material_id" FOREIGN KEY (material_id) REFERENCES materials.materials (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE TABLE materials.material_sources (
        id uuid NOT NULL DEFAULT (uuidv7()),
        created_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        updated_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        material_id uuid NOT NULL,
        integration_config_id uuid NOT NULL,
        external_code character varying(128) NOT NULL,
        external_name character varying(200),
        unit_mapping jsonb,
        conversion_factor numeric(20,6) NOT NULL,
        last_synced_at timestamp with time zone,
        sync_enabled boolean NOT NULL,
        CONSTRAINT "PK_material_sources" PRIMARY KEY (id),
        CONSTRAINT ck_material_sources_1 CHECK (conversion_factor > 0),
        CONSTRAINT "FK_material_sources_integration_configs_integration_config_id" FOREIGN KEY (integration_config_id) REFERENCES integration.integration_configs (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_material_sources_materials_material_id" FOREIGN KEY (material_id) REFERENCES materials.materials (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE TABLE equipment.equipment (
        id uuid NOT NULL DEFAULT (uuidv7()),
        created_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        updated_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        code character varying(64) NOT NULL,
        name character varying(200) NOT NULL,
        description text,
        is_active boolean NOT NULL DEFAULT TRUE,
        area_id uuid NOT NULL,
        line_id uuid,
        equipment_group_id uuid NOT NULL,
        equipment_type_id uuid NOT NULL,
        operational_state character varying(32) NOT NULL,
        CONSTRAINT "PK_equipment" PRIMARY KEY (id),
        CONSTRAINT "FK_equipment_areas_area_id" FOREIGN KEY (area_id) REFERENCES organization.areas (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_equipment_equipment_groups_equipment_group_id" FOREIGN KEY (equipment_group_id) REFERENCES equipment.equipment_groups (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_equipment_equipment_types_equipment_type_id" FOREIGN KEY (equipment_type_id) REFERENCES equipment.equipment_types (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_equipment_lines_line_id" FOREIGN KEY (line_id) REFERENCES organization.lines (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE TABLE organization.shifts (
        id uuid NOT NULL DEFAULT (uuidv7()),
        created_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        updated_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        code character varying(64) NOT NULL,
        name character varying(200) NOT NULL,
        description text,
        is_active boolean NOT NULL DEFAULT TRUE,
        fab_id uuid NOT NULL,
        line_id uuid,
        start_time time without time zone NOT NULL,
        end_time time without time zone NOT NULL,
        days_of_week jsonb NOT NULL,
        sort_order integer NOT NULL,
        CONSTRAINT "PK_shifts" PRIMARY KEY (id),
        CONSTRAINT "FK_shifts_fabs_fab_id" FOREIGN KEY (fab_id) REFERENCES organization.fabs (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_shifts_lines_line_id" FOREIGN KEY (line_id) REFERENCES organization.lines (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE TABLE organization.workstations (
        id uuid NOT NULL DEFAULT (uuidv7()),
        created_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        updated_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        code character varying(64) NOT NULL,
        name character varying(200) NOT NULL,
        description text,
        is_active boolean NOT NULL DEFAULT TRUE,
        line_id uuid,
        area_id uuid NOT NULL,
        CONSTRAINT "PK_workstations" PRIMARY KEY (id),
        CONSTRAINT "FK_workstations_areas_area_id" FOREIGN KEY (area_id) REFERENCES organization.areas (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_workstations_lines_line_id" FOREIGN KEY (line_id) REFERENCES organization.lines (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE TABLE production.carriers (
        id uuid NOT NULL DEFAULT (uuidv7()),
        created_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        updated_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        fab_id uuid NOT NULL,
        carrier_code character varying(64) NOT NULL,
        carrier_type character varying(32) NOT NULL,
        slot_capacity integer NOT NULL,
        status character varying(32) NOT NULL,
        current_location_id uuid,
        CONSTRAINT "PK_carriers" PRIMARY KEY (id),
        CONSTRAINT ck_carriers_1 CHECK (slot_capacity > 0),
        CONSTRAINT "FK_carriers_fabs_fab_id" FOREIGN KEY (fab_id) REFERENCES organization.fabs (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_carriers_locations_current_location_id" FOREIGN KEY (current_location_id) REFERENCES organization.locations (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE TABLE audit.attachments (
        id uuid NOT NULL DEFAULT (uuidv7()),
        created_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        updated_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        entity_type character varying(64) NOT NULL,
        entity_id uuid NOT NULL,
        original_name character varying(255) NOT NULL,
        storage_path text NOT NULL,
        mime_type character varying(128) NOT NULL,
        file_size bigint NOT NULL,
        checksum character varying(128) NOT NULL,
        uploaded_by_id uuid NOT NULL,
        CONSTRAINT "PK_attachments" PRIMARY KEY (id),
        CONSTRAINT ck_attachments_1 CHECK (file_size >= 0),
        CONSTRAINT "FK_attachments_users_uploaded_by_id" FOREIGN KEY (uploaded_by_id) REFERENCES security.users (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE TABLE audit.audit_logs (
        id uuid NOT NULL DEFAULT (uuidv7()),
        created_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        updated_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        user_id uuid,
        entity_type character varying(64) NOT NULL,
        entity_id uuid NOT NULL,
        action character varying(64) NOT NULL,
        before_state jsonb,
        after_state jsonb,
        ip_address character varying(64),
        user_agent text,
        occurred_at timestamp with time zone NOT NULL,
        correlation_id uuid NOT NULL,
        source_channel character varying(32) NOT NULL,
        reason_code_id uuid,
        CONSTRAINT "PK_audit_logs" PRIMARY KEY (id),
        CONSTRAINT "FK_audit_logs_reason_codes_reason_code_id" FOREIGN KEY (reason_code_id) REFERENCES quality.reason_codes (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_audit_logs_users_user_id" FOREIGN KEY (user_id) REFERENCES security.users (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE TABLE audit.business_events (
        id uuid NOT NULL DEFAULT (uuidv7()),
        created_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        updated_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        event_type character varying(64) NOT NULL,
        entity_type character varying(64) NOT NULL,
        entity_id uuid NOT NULL,
        user_id uuid,
        payload jsonb NOT NULL,
        occurred_at timestamp with time zone NOT NULL,
        correlation_id uuid NOT NULL,
        CONSTRAINT "PK_business_events" PRIMARY KEY (id),
        CONSTRAINT "FK_business_events_users_user_id" FOREIGN KEY (user_id) REFERENCES security.users (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE TABLE audit.command_receipts (
        id uuid NOT NULL DEFAULT (uuidv7()),
        created_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        updated_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        scope character varying(128) NOT NULL,
        idempotency_key character varying(128) NOT NULL,
        request_hash character varying(128) NOT NULL,
        actor_user_id uuid,
        correlation_id uuid NOT NULL,
        status character varying(32) NOT NULL,
        result jsonb,
        completed_at timestamp with time zone,
        CONSTRAINT "PK_command_receipts" PRIMARY KEY (id),
        CONSTRAINT "FK_command_receipts_users_actor_user_id" FOREIGN KEY (actor_user_id) REFERENCES security.users (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE TABLE engineering.engineering_documents (
        id uuid NOT NULL DEFAULT (uuidv7()),
        created_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        updated_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        entity_type character varying(64) NOT NULL,
        entity_id uuid NOT NULL,
        original_filename character varying(255) NOT NULL,
        document_type character varying(64) NOT NULL,
        mime_type character varying(128) NOT NULL,
        file_size bigint NOT NULL,
        revision character varying(32) NOT NULL,
        checksum character varying(128) NOT NULL,
        storage_path text NOT NULL,
        lifecycle_status character varying(32) NOT NULL,
        effective_from timestamp with time zone,
        effective_to timestamp with time zone,
        released_at timestamp with time zone,
        released_by_id uuid,
        uploaded_by_id uuid NOT NULL,
        CONSTRAINT "PK_engineering_documents" PRIMARY KEY (id),
        CONSTRAINT ck_engineering_documents_1 CHECK (file_size >= 0),
        CONSTRAINT "FK_engineering_documents_users_released_by_id" FOREIGN KEY (released_by_id) REFERENCES security.users (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_engineering_documents_users_uploaded_by_id" FOREIGN KEY (uploaded_by_id) REFERENCES security.users (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE TABLE production.genealogy_events (
        id uuid NOT NULL DEFAULT (uuidv7()),
        created_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        updated_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        event_type character varying(32) NOT NULL,
        reason text NOT NULL,
        occurred_at timestamp with time zone NOT NULL,
        performed_by_id uuid NOT NULL,
        idempotency_key character varying(128) NOT NULL,
        correlation_id uuid NOT NULL,
        CONSTRAINT "PK_genealogy_events" PRIMARY KEY (id),
        CONSTRAINT ck_genealogy_events_1 CHECK (event_type IN ('Split', 'Merge', 'Transfer', 'Create')),
        CONSTRAINT "FK_genealogy_events_users_performed_by_id" FOREIGN KEY (performed_by_id) REFERENCES security.users (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE TABLE integration.import_jobs (
        id uuid NOT NULL DEFAULT (uuidv7()),
        created_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        updated_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        user_id uuid NOT NULL,
        entity character varying(64) NOT NULL,
        mapping_id uuid,
        original_filename character varying(255) NOT NULL,
        file_path text NOT NULL,
        status character varying(32) NOT NULL,
        total_rows integer NOT NULL,
        successful_rows integer NOT NULL,
        failed_rows integer NOT NULL,
        error_log jsonb,
        started_at timestamp with time zone,
        completed_at timestamp with time zone,
        dry_run boolean NOT NULL,
        CONSTRAINT "PK_import_jobs" PRIMARY KEY (id),
        CONSTRAINT ck_import_jobs_1 CHECK (total_rows >= 0 AND successful_rows >= 0 AND failed_rows >= 0),
        CONSTRAINT "FK_import_jobs_import_mappings_mapping_id" FOREIGN KEY (mapping_id) REFERENCES integration.import_mappings (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_import_jobs_users_user_id" FOREIGN KEY (user_id) REFERENCES security.users (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE TABLE engineering.recipe_versions (
        id uuid NOT NULL DEFAULT (uuidv7()),
        created_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        updated_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        recipe_id uuid NOT NULL,
        version character varying(32) NOT NULL,
        lifecycle_status character varying(32) NOT NULL,
        checksum character varying(128),
        released_at timestamp with time zone,
        released_by_id uuid,
        parameters jsonb,
        CONSTRAINT "PK_recipe_versions" PRIMARY KEY (id),
        CONSTRAINT "FK_recipe_versions_recipes_recipe_id" FOREIGN KEY (recipe_id) REFERENCES engineering.recipes (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_recipe_versions_users_released_by_id" FOREIGN KEY (released_by_id) REFERENCES security.users (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE TABLE engineering.route_versions (
        id uuid NOT NULL DEFAULT (uuidv7()),
        created_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        updated_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        route_id uuid NOT NULL,
        version character varying(32) NOT NULL,
        lifecycle_status character varying(32) NOT NULL,
        effective_from timestamp with time zone,
        effective_to timestamp with time zone,
        released_at timestamp with time zone,
        released_by_id uuid,
        change_reason text,
        CONSTRAINT "PK_route_versions" PRIMARY KEY (id),
        CONSTRAINT ck_route_versions_1 CHECK (effective_to IS NULL OR effective_from IS NULL OR effective_to > effective_from),
        CONSTRAINT "FK_route_versions_routes_route_id" FOREIGN KEY (route_id) REFERENCES engineering.routes (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_route_versions_users_released_by_id" FOREIGN KEY (released_by_id) REFERENCES security.users (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE TABLE quality.specification_versions (
        id uuid NOT NULL DEFAULT (uuidv7()),
        created_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        updated_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        parameter_code character varying(64) NOT NULL,
        version integer NOT NULL,
        value_type character varying(32) NOT NULL,
        unit character varying(32),
        spec_min numeric(20,6),
        spec_max numeric(20,6),
        lifecycle_status character varying(32) NOT NULL,
        released_at timestamp with time zone,
        released_by_id uuid,
        CONSTRAINT "PK_specification_versions" PRIMARY KEY (id),
        CONSTRAINT ck_specification_versions_1 CHECK (version > 0),
        CONSTRAINT ck_specification_versions_2 CHECK (spec_min IS NULL OR spec_max IS NULL OR spec_max >= spec_min),
        CONSTRAINT "FK_specification_versions_users_released_by_id" FOREIGN KEY (released_by_id) REFERENCES security.users (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE TABLE security.user_claims (
        id integer GENERATED BY DEFAULT AS IDENTITY,
        user_id uuid NOT NULL,
        claim_type text,
        claim_value text,
        CONSTRAINT "PK_user_claims" PRIMARY KEY (id),
        CONSTRAINT "FK_user_claims_users_user_id" FOREIGN KEY (user_id) REFERENCES security.users (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE TABLE security.user_fab_scopes (
        id uuid NOT NULL DEFAULT (uuidv7()),
        created_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        updated_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        user_id uuid NOT NULL,
        fab_id uuid NOT NULL,
        CONSTRAINT "PK_user_fab_scopes" PRIMARY KEY (id),
        CONSTRAINT "FK_user_fab_scopes_fabs_fab_id" FOREIGN KEY (fab_id) REFERENCES organization.fabs (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_user_fab_scopes_users_user_id" FOREIGN KEY (user_id) REFERENCES security.users (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE TABLE security.user_logins (
        login_provider text NOT NULL,
        provider_key text NOT NULL,
        provider_display_name text,
        user_id uuid NOT NULL,
        CONSTRAINT "PK_user_logins" PRIMARY KEY (login_provider, provider_key),
        CONSTRAINT "FK_user_logins_users_user_id" FOREIGN KEY (user_id) REFERENCES security.users (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE TABLE security.user_permissions (
        id uuid NOT NULL DEFAULT (uuidv7()),
        created_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        updated_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        user_id uuid NOT NULL,
        permission_id uuid NOT NULL,
        CONSTRAINT "PK_user_permissions" PRIMARY KEY (id),
        CONSTRAINT "FK_user_permissions_permissions_permission_id" FOREIGN KEY (permission_id) REFERENCES security.permissions (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_user_permissions_users_user_id" FOREIGN KEY (user_id) REFERENCES security.users (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE TABLE security.user_roles (
        user_id uuid NOT NULL,
        role_id uuid NOT NULL,
        CONSTRAINT "PK_user_roles" PRIMARY KEY (user_id, role_id),
        CONSTRAINT "FK_user_roles_roles_role_id" FOREIGN KEY (role_id) REFERENCES security.roles (id) ON DELETE CASCADE,
        CONSTRAINT "FK_user_roles_users_user_id" FOREIGN KEY (user_id) REFERENCES security.users (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE TABLE security.user_tokens (
        user_id uuid NOT NULL,
        login_provider text NOT NULL,
        name text NOT NULL,
        value text,
        CONSTRAINT "PK_user_tokens" PRIMARY KEY (user_id, login_provider, name),
        CONSTRAINT "FK_user_tokens_users_user_id" FOREIGN KEY (user_id) REFERENCES security.users (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE TABLE equipment.chambers (
        id uuid NOT NULL DEFAULT (uuidv7()),
        created_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        updated_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        code character varying(64) NOT NULL,
        name character varying(200) NOT NULL,
        description text,
        is_active boolean NOT NULL DEFAULT TRUE,
        equipment_id uuid NOT NULL,
        CONSTRAINT "PK_chambers" PRIMARY KEY (id),
        CONSTRAINT "FK_chambers_equipment_equipment_id" FOREIGN KEY (equipment_id) REFERENCES equipment.equipment (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE TABLE equipment.equipment_states (
        id uuid NOT NULL DEFAULT (uuidv7()),
        created_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        updated_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        equipment_id uuid NOT NULL,
        state character varying(32) NOT NULL,
        started_at timestamp with time zone NOT NULL,
        ended_at timestamp with time zone,
        source character varying(32) NOT NULL,
        metadata jsonb,
        CONSTRAINT "PK_equipment_states" PRIMARY KEY (id),
        CONSTRAINT ck_equipment_states_1 CHECK (ended_at IS NULL OR ended_at > started_at),
        CONSTRAINT "FK_equipment_states_equipment_equipment_id" FOREIGN KEY (equipment_id) REFERENCES equipment.equipment (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE TABLE integration.machine_connections (
        id uuid NOT NULL DEFAULT (uuidv7()),
        created_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        updated_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        equipment_id uuid NOT NULL,
        name character varying(200) NOT NULL,
        protocol character varying(32) NOT NULL,
        is_active boolean NOT NULL DEFAULT TRUE,
        status character varying(32) NOT NULL,
        status_message text,
        last_connected_at timestamp with time zone,
        config jsonb,
        secret_reference character varying(255),
        CONSTRAINT "PK_machine_connections" PRIMARY KEY (id),
        CONSTRAINT "FK_machine_connections_equipment_equipment_id" FOREIGN KEY (equipment_id) REFERENCES equipment.equipment (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE TABLE equipment.maintenance_schedules (
        id uuid NOT NULL DEFAULT (uuidv7()),
        created_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        updated_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        name character varying(200) NOT NULL,
        tool_id uuid,
        equipment_id uuid,
        event_type character varying(32) NOT NULL,
        assigned_to_id uuid,
        frequency character varying(32) NOT NULL,
        interval_value integer NOT NULL,
        last_executed_at timestamp with time zone,
        next_due_at timestamp with time zone NOT NULL,
        is_active boolean NOT NULL DEFAULT TRUE,
        CONSTRAINT "PK_maintenance_schedules" PRIMARY KEY (id),
        CONSTRAINT ck_maintenance_schedules_1 CHECK (interval_value > 0),
        CONSTRAINT ck_maintenance_schedules_2 CHECK (num_nonnulls(tool_id, equipment_id) = 1),
        CONSTRAINT "FK_maintenance_schedules_equipment_equipment_id" FOREIGN KEY (equipment_id) REFERENCES equipment.equipment (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_maintenance_schedules_tools_tool_id" FOREIGN KEY (tool_id) REFERENCES equipment.tools (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_maintenance_schedules_users_assigned_to_id" FOREIGN KEY (assigned_to_id) REFERENCES security.users (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE TABLE production.process_batches (
        id uuid NOT NULL DEFAULT (uuidv7()),
        created_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        updated_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        code character varying(64) NOT NULL,
        equipment_id uuid NOT NULL,
        status character varying(32) NOT NULL,
        CONSTRAINT "PK_process_batches" PRIMARY KEY (id),
        CONSTRAINT "FK_process_batches_equipment_equipment_id" FOREIGN KEY (equipment_id) REFERENCES equipment.equipment (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE TABLE equipment.downtimes (
        id uuid NOT NULL DEFAULT (uuidv7()),
        created_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        updated_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        line_id uuid,
        equipment_id uuid NOT NULL,
        reason_code_id uuid,
        shift_id uuid,
        started_at timestamp with time zone NOT NULL,
        ended_at timestamp with time zone,
        notes text,
        reported_by_id uuid NOT NULL,
        needs_reason boolean NOT NULL,
        classified_at timestamp with time zone,
        classified_by_id uuid,
        CONSTRAINT "PK_downtimes" PRIMARY KEY (id),
        CONSTRAINT ck_downtimes_1 CHECK (ended_at IS NULL OR ended_at >= started_at),
        CONSTRAINT "FK_downtimes_equipment_equipment_id" FOREIGN KEY (equipment_id) REFERENCES equipment.equipment (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_downtimes_lines_line_id" FOREIGN KEY (line_id) REFERENCES organization.lines (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_downtimes_reason_codes_reason_code_id" FOREIGN KEY (reason_code_id) REFERENCES quality.reason_codes (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_downtimes_shifts_shift_id" FOREIGN KEY (shift_id) REFERENCES organization.shifts (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_downtimes_users_classified_by_id" FOREIGN KEY (classified_by_id) REFERENCES security.users (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_downtimes_users_reported_by_id" FOREIGN KEY (reported_by_id) REFERENCES security.users (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE TABLE organization.shift_handovers (
        id uuid NOT NULL DEFAULT (uuidv7()),
        created_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        updated_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        shift_id uuid NOT NULL,
        line_id uuid,
        business_date date NOT NULL,
        shift_start timestamp with time zone NOT NULL,
        shift_end timestamp with time zone NOT NULL,
        discrepancies jsonb,
        notes text,
        confirmed_by_id uuid,
        confirmed_at timestamp with time zone,
        CONSTRAINT "PK_shift_handovers" PRIMARY KEY (id),
        CONSTRAINT ck_shift_handovers_1 CHECK (shift_end > shift_start),
        CONSTRAINT "FK_shift_handovers_lines_line_id" FOREIGN KEY (line_id) REFERENCES organization.lines (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_shift_handovers_shifts_shift_id" FOREIGN KEY (shift_id) REFERENCES organization.shifts (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_shift_handovers_users_confirmed_by_id" FOREIGN KEY (confirmed_by_id) REFERENCES security.users (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE TABLE organization.terminals (
        id uuid NOT NULL DEFAULT (uuidv7()),
        created_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        updated_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        code character varying(64) NOT NULL,
        name character varying(200) NOT NULL,
        description text,
        is_active boolean NOT NULL DEFAULT TRUE,
        device_uuid uuid NOT NULL,
        workstation_id uuid,
        last_seen_at timestamp with time zone,
        app_version character varying(64),
        CONSTRAINT "PK_terminals" PRIMARY KEY (id),
        CONSTRAINT "FK_terminals_workstations_workstation_id" FOREIGN KEY (workstation_id) REFERENCES organization.workstations (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE TABLE production.carrier_movements (
        id uuid NOT NULL DEFAULT (uuidv7()),
        created_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        updated_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        carrier_id uuid NOT NULL,
        from_location_id uuid,
        to_location_id uuid NOT NULL,
        moved_at timestamp with time zone NOT NULL,
        performed_by_id uuid NOT NULL,
        notes text,
        CONSTRAINT "PK_carrier_movements" PRIMARY KEY (id),
        CONSTRAINT "FK_carrier_movements_carriers_carrier_id" FOREIGN KEY (carrier_id) REFERENCES production.carriers (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_carrier_movements_locations_from_location_id" FOREIGN KEY (from_location_id) REFERENCES organization.locations (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_carrier_movements_locations_to_location_id" FOREIGN KEY (to_location_id) REFERENCES organization.locations (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_carrier_movements_users_performed_by_id" FOREIGN KEY (performed_by_id) REFERENCES security.users (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE TABLE production.carrier_slots (
        id uuid NOT NULL DEFAULT (uuidv7()),
        created_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        updated_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        carrier_id uuid NOT NULL,
        slot_no integer NOT NULL,
        CONSTRAINT "PK_carrier_slots" PRIMARY KEY (id),
        CONSTRAINT ck_carrier_slots_1 CHECK (slot_no > 0),
        CONSTRAINT "FK_carrier_slots_carriers_carrier_id" FOREIGN KEY (carrier_id) REFERENCES production.carriers (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE TABLE equipment.equipment_qualifications (
        id uuid NOT NULL DEFAULT (uuidv7()),
        created_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        updated_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        equipment_id uuid NOT NULL,
        operation_id uuid NOT NULL,
        recipe_version_id uuid,
        qualification_status character varying(32) NOT NULL,
        valid_from timestamp with time zone NOT NULL,
        valid_to timestamp with time zone,
        qualified_by_id uuid NOT NULL,
        CONSTRAINT "PK_equipment_qualifications" PRIMARY KEY (id),
        CONSTRAINT ck_equipment_qualifications_1 CHECK (valid_to IS NULL OR valid_to > valid_from),
        CONSTRAINT "FK_equipment_qualifications_equipment_equipment_id" FOREIGN KEY (equipment_id) REFERENCES equipment.equipment (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_equipment_qualifications_operations_operation_id" FOREIGN KEY (operation_id) REFERENCES engineering.operations (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_equipment_qualifications_recipe_versions_recipe_version_id" FOREIGN KEY (recipe_version_id) REFERENCES engineering.recipe_versions (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_equipment_qualifications_users_qualified_by_id" FOREIGN KEY (qualified_by_id) REFERENCES security.users (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE TABLE equipment.worker_qualifications (
        id uuid NOT NULL DEFAULT (uuidv7()),
        created_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        updated_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        worker_id uuid NOT NULL,
        operation_id uuid NOT NULL,
        recipe_version_id uuid,
        qualification_status character varying(32) NOT NULL,
        valid_from timestamp with time zone NOT NULL,
        valid_to timestamp with time zone,
        qualified_by_id uuid NOT NULL,
        CONSTRAINT "PK_worker_qualifications" PRIMARY KEY (id),
        CONSTRAINT ck_worker_qualifications_1 CHECK (valid_to IS NULL OR valid_to > valid_from),
        CONSTRAINT "FK_worker_qualifications_operations_operation_id" FOREIGN KEY (operation_id) REFERENCES engineering.operations (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_worker_qualifications_recipe_versions_recipe_version_id" FOREIGN KEY (recipe_version_id) REFERENCES engineering.recipe_versions (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_worker_qualifications_users_qualified_by_id" FOREIGN KEY (qualified_by_id) REFERENCES security.users (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_worker_qualifications_workers_worker_id" FOREIGN KEY (worker_id) REFERENCES organization.workers (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE TABLE engineering.product_revisions (
        id uuid NOT NULL DEFAULT (uuidv7()),
        created_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        updated_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        product_id uuid NOT NULL,
        revision_code character varying(32) NOT NULL,
        description text,
        lifecycle_status character varying(32) NOT NULL,
        route_version_id uuid,
        change_reason text,
        external_ref character varying(128),
        effective_from timestamp with time zone,
        effective_to timestamp with time zone,
        released_at timestamp with time zone,
        obsolete_at timestamp with time zone,
        released_by_id uuid,
        CONSTRAINT "PK_product_revisions" PRIMARY KEY (id),
        CONSTRAINT "AK_product_revisions_id_product_id" UNIQUE (id, product_id),
        CONSTRAINT ck_product_revisions_1 CHECK (effective_to IS NULL OR effective_from IS NULL OR effective_to > effective_from),
        CONSTRAINT "FK_product_revisions_products_product_id" FOREIGN KEY (product_id) REFERENCES engineering.products (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_product_revisions_route_versions_route_version_id" FOREIGN KEY (route_version_id) REFERENCES engineering.route_versions (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_product_revisions_users_released_by_id" FOREIGN KEY (released_by_id) REFERENCES security.users (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE TABLE engineering.route_steps (
        id uuid NOT NULL DEFAULT (uuidv7()),
        created_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        updated_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        route_version_id uuid NOT NULL,
        operation_id uuid NOT NULL,
        sequence_no integer NOT NULL,
        name character varying(200) NOT NULL,
        instruction text,
        equipment_type_id uuid,
        requires_confirmation boolean NOT NULL,
        parameters jsonb,
        CONSTRAINT "PK_route_steps" PRIMARY KEY (id),
        CONSTRAINT "AK_route_steps_id_route_version_id" UNIQUE (id, route_version_id),
        CONSTRAINT ck_route_steps_1 CHECK (sequence_no > 0),
        CONSTRAINT "FK_route_steps_equipment_types_equipment_type_id" FOREIGN KEY (equipment_type_id) REFERENCES equipment.equipment_types (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_route_steps_operations_operation_id" FOREIGN KEY (operation_id) REFERENCES engineering.operations (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_route_steps_route_versions_route_version_id" FOREIGN KEY (route_version_id) REFERENCES engineering.route_versions (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE TABLE equipment.equipment_events (
        id uuid NOT NULL DEFAULT (uuidv7()),
        created_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        updated_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        equipment_id uuid NOT NULL,
        machine_connection_id uuid,
        event_type character varying(32) NOT NULL,
        state_from character varying(32),
        state_to character varying(32),
        payload jsonb,
        event_timestamp timestamp with time zone NOT NULL,
        received_at timestamp with time zone NOT NULL,
        correlation_id uuid NOT NULL,
        is_simulated boolean NOT NULL,
        CONSTRAINT "PK_equipment_events" PRIMARY KEY (id),
        CONSTRAINT "FK_equipment_events_equipment_equipment_id" FOREIGN KEY (equipment_id) REFERENCES equipment.equipment (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_equipment_events_machine_connections_machine_connection_id" FOREIGN KEY (machine_connection_id) REFERENCES integration.machine_connections (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE TABLE integration.machine_messages (
        id uuid NOT NULL DEFAULT (uuidv7()),
        created_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        updated_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        machine_connection_id uuid NOT NULL,
        topic character varying(255),
        raw_payload text NOT NULL,
        parsed_data jsonb,
        processing_status character varying(32) NOT NULL,
        processing_error text,
        received_at timestamp with time zone NOT NULL,
        source_message_id character varying(128),
        CONSTRAINT "PK_machine_messages" PRIMARY KEY (id),
        CONSTRAINT "FK_machine_messages_machine_connections_machine_connection_id" FOREIGN KEY (machine_connection_id) REFERENCES integration.machine_connections (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE TABLE integration.machine_tags (
        id uuid NOT NULL DEFAULT (uuidv7()),
        created_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        updated_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        machine_connection_id uuid NOT NULL,
        equipment_id uuid NOT NULL,
        name character varying(200) NOT NULL,
        address character varying(255) NOT NULL,
        signal_type character varying(32) NOT NULL,
        data_type character varying(32) NOT NULL,
        transform jsonb,
        unit character varying(32),
        is_active boolean NOT NULL DEFAULT TRUE,
        CONSTRAINT "PK_machine_tags" PRIMARY KEY (id),
        CONSTRAINT "FK_machine_tags_equipment_equipment_id" FOREIGN KEY (equipment_id) REFERENCES equipment.equipment (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_machine_tags_machine_connections_machine_connection_id" FOREIGN KEY (machine_connection_id) REFERENCES integration.machine_connections (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE TABLE equipment.maintenance_events (
        id uuid NOT NULL DEFAULT (uuidv7()),
        created_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        updated_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        title character varying(200) NOT NULL,
        event_type character varying(32) NOT NULL,
        status character varying(32) NOT NULL,
        tool_id uuid,
        equipment_id uuid,
        schedule_id uuid,
        assigned_to_id uuid,
        scheduled_at timestamp with time zone NOT NULL,
        started_at timestamp with time zone,
        completed_at timestamp with time zone,
        resolution_notes text,
        CONSTRAINT "PK_maintenance_events" PRIMARY KEY (id),
        CONSTRAINT ck_maintenance_events_1 CHECK (num_nonnulls(tool_id, equipment_id) = 1),
        CONSTRAINT ck_maintenance_events_2 CHECK (completed_at IS NULL OR started_at IS NULL OR completed_at >= started_at),
        CONSTRAINT "FK_maintenance_events_equipment_equipment_id" FOREIGN KEY (equipment_id) REFERENCES equipment.equipment (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_maintenance_events_maintenance_schedules_schedule_id" FOREIGN KEY (schedule_id) REFERENCES equipment.maintenance_schedules (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_maintenance_events_tools_tool_id" FOREIGN KEY (tool_id) REFERENCES equipment.tools (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_maintenance_events_users_assigned_to_id" FOREIGN KEY (assigned_to_id) REFERENCES security.users (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE TABLE production.work_orders (
        id uuid NOT NULL DEFAULT (uuidv7()),
        created_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        updated_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        order_no character varying(64) NOT NULL,
        fab_id uuid NOT NULL,
        product_id uuid NOT NULL,
        product_revision_id uuid NOT NULL,
        planned_qty integer NOT NULL,
        produced_qty integer NOT NULL,
        status character varying(32) NOT NULL,
        priority integer NOT NULL,
        due_date date,
        description text,
        planned_start_at timestamp with time zone,
        planned_end_at timestamp with time zone,
        completed_at timestamp with time zone,
        customer_id uuid,
        customer_order_no character varying(128),
        process_snapshot jsonb,
        snapshot_version integer NOT NULL,
        CONSTRAINT "PK_work_orders" PRIMARY KEY (id),
        CONSTRAINT ck_work_orders_1 CHECK (planned_qty >= 0 AND produced_qty >= 0),
        CONSTRAINT ck_work_orders_2 CHECK (planned_end_at IS NULL OR planned_start_at IS NULL OR planned_end_at >= planned_start_at),
        CONSTRAINT "FK_work_orders_companies_customer_id" FOREIGN KEY (customer_id) REFERENCES materials.companies (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_work_orders_fabs_fab_id" FOREIGN KEY (fab_id) REFERENCES organization.fabs (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_work_orders_product_revisions_product_revision_id" FOREIGN KEY (product_revision_id) REFERENCES engineering.product_revisions (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_work_orders_product_revisions_product_revision_id_product_id" FOREIGN KEY (product_revision_id, product_id) REFERENCES engineering.product_revisions (id, product_id) ON DELETE RESTRICT,
        CONSTRAINT "FK_work_orders_products_product_id" FOREIGN KEY (product_id) REFERENCES engineering.products (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE TABLE materials.bom_items (
        id uuid NOT NULL DEFAULT (uuidv7()),
        created_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        updated_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        route_version_id uuid NOT NULL,
        route_step_id uuid,
        material_id uuid NOT NULL,
        quantity_per_unit numeric(20,6) NOT NULL,
        per character varying(32) NOT NULL,
        scrap_percentage numeric(20,6) NOT NULL,
        notes text,
        CONSTRAINT "PK_bom_items" PRIMARY KEY (id),
        CONSTRAINT ck_bom_items_1 CHECK (quantity_per_unit >= 0 AND scrap_percentage BETWEEN 0 AND 100),
        CONSTRAINT "FK_bom_items_materials_material_id" FOREIGN KEY (material_id) REFERENCES materials.materials (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_bom_items_route_steps_route_step_id" FOREIGN KEY (route_step_id) REFERENCES engineering.route_steps (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_bom_items_route_versions_route_version_id" FOREIGN KEY (route_version_id) REFERENCES engineering.route_versions (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE TABLE engineering.checklist_items (
        id uuid NOT NULL DEFAULT (uuidv7()),
        created_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        updated_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        route_step_id uuid NOT NULL,
        label character varying(200) NOT NULL,
        is_required boolean NOT NULL,
        sort_order integer NOT NULL,
        CONSTRAINT "PK_checklist_items" PRIMARY KEY (id),
        CONSTRAINT "FK_checklist_items_route_steps_route_step_id" FOREIGN KEY (route_step_id) REFERENCES engineering.route_steps (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE TABLE engineering.output_definitions (
        id uuid NOT NULL DEFAULT (uuidv7()),
        created_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        updated_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        route_step_id uuid NOT NULL,
        key character varying(64) NOT NULL,
        label character varying(200) NOT NULL,
        value_type character varying(32) NOT NULL,
        unit character varying(32),
        options jsonb,
        is_required boolean NOT NULL,
        sort_order integer NOT NULL,
        CONSTRAINT "PK_output_definitions" PRIMARY KEY (id),
        CONSTRAINT "FK_output_definitions_route_steps_route_step_id" FOREIGN KEY (route_step_id) REFERENCES engineering.route_steps (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE TABLE engineering.q_time_rules (
        id uuid NOT NULL DEFAULT (uuidv7()),
        created_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        updated_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        start_route_step_id uuid NOT NULL,
        end_route_step_id uuid NOT NULL,
        start_event_type character varying(32) NOT NULL,
        end_event_type character varying(32) NOT NULL,
        limit_minutes numeric(20,6) NOT NULL,
        is_active boolean NOT NULL DEFAULT TRUE,
        CONSTRAINT "PK_q_time_rules" PRIMARY KEY (id),
        CONSTRAINT ck_q_time_rules_1 CHECK (limit_minutes > 0),
        CONSTRAINT "FK_q_time_rules_route_steps_end_route_step_id" FOREIGN KEY (end_route_step_id) REFERENCES engineering.route_steps (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_q_time_rules_route_steps_start_route_step_id" FOREIGN KEY (start_route_step_id) REFERENCES engineering.route_steps (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE TABLE quality.quality_check_templates (
        id uuid NOT NULL DEFAULT (uuidv7()),
        created_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        updated_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        route_version_id uuid NOT NULL,
        route_step_id uuid,
        name character varying(200) NOT NULL,
        version integer NOT NULL,
        min_checks_per_batch integer NOT NULL,
        samples_per_check integer NOT NULL,
        parameters jsonb,
        CONSTRAINT "PK_quality_check_templates" PRIMARY KEY (id),
        CONSTRAINT ck_quality_check_templates_1 CHECK (version > 0 AND min_checks_per_batch >= 0 AND samples_per_check > 0),
        CONSTRAINT "FK_quality_check_templates_route_steps_route_step_id" FOREIGN KEY (route_step_id) REFERENCES engineering.route_steps (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_quality_check_templates_route_versions_route_version_id" FOREIGN KEY (route_version_id) REFERENCES engineering.route_versions (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE TABLE engineering.recipe_applicabilities (
        id uuid NOT NULL DEFAULT (uuidv7()),
        created_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        updated_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        recipe_version_id uuid NOT NULL,
        route_step_id uuid NOT NULL,
        equipment_id uuid NOT NULL,
        effective_from timestamp with time zone NOT NULL,
        effective_to timestamp with time zone,
        CONSTRAINT "PK_recipe_applicabilities" PRIMARY KEY (id),
        CONSTRAINT ck_recipe_applicabilities_1 CHECK (effective_to IS NULL OR effective_to > effective_from),
        CONSTRAINT "FK_recipe_applicabilities_equipment_equipment_id" FOREIGN KEY (equipment_id) REFERENCES equipment.equipment (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_recipe_applicabilities_recipe_versions_recipe_version_id" FOREIGN KEY (recipe_version_id) REFERENCES engineering.recipe_versions (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_recipe_applicabilities_route_steps_route_step_id" FOREIGN KEY (route_step_id) REFERENCES engineering.route_steps (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE TABLE production.lots (
        id uuid NOT NULL DEFAULT (uuidv7()),
        created_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        updated_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        fab_id uuid NOT NULL,
        work_order_id uuid,
        lot_number character varying(64) NOT NULL,
        legacy_batch_number character varying(64),
        lot_assigned_at timestamp with time zone NOT NULL,
        product_revision_id uuid NOT NULL,
        route_version_id uuid NOT NULL,
        current_route_step_id uuid,
        lot_type character varying(32) NOT NULL,
        priority integer NOT NULL,
        target_qty integer NOT NULL,
        status character varying(32) NOT NULL,
        started_at timestamp with time zone,
        completed_at timestamp with time zone,
        released_to_route_at timestamp with time zone,
        released_to_route_by_id uuid,
        notes text,
        snapshot_version integer,
        CONSTRAINT "PK_lots" PRIMARY KEY (id),
        CONSTRAINT "AK_lots_id_route_version_id" UNIQUE (id, route_version_id),
        CONSTRAINT ck_lots_1 CHECK (target_qty >= 0),
        CONSTRAINT ck_lots_2 CHECK (completed_at IS NULL OR started_at IS NULL OR completed_at >= started_at),
        CONSTRAINT "FK_lots_fabs_fab_id" FOREIGN KEY (fab_id) REFERENCES organization.fabs (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_lots_product_revisions_product_revision_id" FOREIGN KEY (product_revision_id) REFERENCES engineering.product_revisions (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_lots_route_steps_current_route_step_id" FOREIGN KEY (current_route_step_id) REFERENCES engineering.route_steps (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_lots_route_steps_current_route_step_id_route_version_id" FOREIGN KEY (current_route_step_id, route_version_id) REFERENCES engineering.route_steps (id, route_version_id) ON DELETE RESTRICT,
        CONSTRAINT "FK_lots_route_versions_route_version_id" FOREIGN KEY (route_version_id) REFERENCES engineering.route_versions (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_lots_users_released_to_route_by_id" FOREIGN KEY (released_to_route_by_id) REFERENCES security.users (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_lots_work_orders_work_order_id" FOREIGN KEY (work_order_id) REFERENCES production.work_orders (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE TABLE quality.quality_triggers (
        id uuid NOT NULL DEFAULT (uuidv7()),
        created_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        updated_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        name character varying(200) NOT NULL,
        trigger_type character varying(32) NOT NULL,
        quality_check_template_id uuid NOT NULL,
        equipment_id uuid,
        product_id uuid,
        threshold_n integer,
        downtime_min_minutes numeric(20,6),
        is_blocking boolean NOT NULL,
        is_active boolean NOT NULL DEFAULT TRUE,
        version integer NOT NULL,
        CONSTRAINT "PK_quality_triggers" PRIMARY KEY (id),
        CONSTRAINT "FK_quality_triggers_equipment_equipment_id" FOREIGN KEY (equipment_id) REFERENCES equipment.equipment (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_quality_triggers_products_product_id" FOREIGN KEY (product_id) REFERENCES engineering.products (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_quality_triggers_quality_check_templates_quality_check_temp~" FOREIGN KEY (quality_check_template_id) REFERENCES quality.quality_check_templates (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE TABLE production.q_time_windows (
        id uuid NOT NULL DEFAULT (uuidv7()),
        created_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        updated_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        q_time_rule_id uuid NOT NULL,
        lot_id uuid NOT NULL,
        started_at timestamp with time zone NOT NULL,
        deadline_at timestamp with time zone NOT NULL,
        ended_at timestamp with time zone,
        violated_at timestamp with time zone,
        CONSTRAINT "PK_q_time_windows" PRIMARY KEY (id),
        CONSTRAINT ck_q_time_windows_1 CHECK (deadline_at > started_at),
        CONSTRAINT ck_q_time_windows_2 CHECK (ended_at IS NULL OR ended_at >= started_at),
        CONSTRAINT "FK_q_time_windows_lots_lot_id" FOREIGN KEY (lot_id) REFERENCES production.lots (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_q_time_windows_q_time_rules_q_time_rule_id" FOREIGN KEY (q_time_rule_id) REFERENCES engineering.q_time_rules (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE TABLE production.step_executions (
        id uuid NOT NULL DEFAULT (uuidv7()),
        created_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        updated_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        lot_id uuid NOT NULL,
        route_version_id uuid NOT NULL,
        route_step_id uuid NOT NULL,
        visit_no integer NOT NULL,
        status character varying(32) NOT NULL,
        name character varying(200) NOT NULL,
        instruction text,
        entered_at timestamp with time zone NOT NULL,
        exited_at timestamp with time zone,
        confirmed_at timestamp with time zone,
        confirmed_by_id uuid,
        requires_confirmation boolean NOT NULL,
        assigned_by_id uuid,
        assigned_at timestamp with time zone,
        passed_qty integer NOT NULL,
        scrap_qty integer NOT NULL,
        CONSTRAINT "PK_step_executions" PRIMARY KEY (id),
        CONSTRAINT ck_step_executions_1 CHECK (visit_no > 0 AND passed_qty >= 0 AND scrap_qty >= 0),
        CONSTRAINT ck_step_executions_2 CHECK (exited_at IS NULL OR exited_at >= entered_at),
        CONSTRAINT "FK_step_executions_lots_lot_id" FOREIGN KEY (lot_id) REFERENCES production.lots (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_step_executions_lots_lot_id_route_version_id" FOREIGN KEY (lot_id, route_version_id) REFERENCES production.lots (id, route_version_id) ON DELETE RESTRICT,
        CONSTRAINT "FK_step_executions_route_steps_route_step_id" FOREIGN KEY (route_step_id) REFERENCES engineering.route_steps (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_step_executions_route_steps_route_step_id_route_version_id" FOREIGN KEY (route_step_id, route_version_id) REFERENCES engineering.route_steps (id, route_version_id) ON DELETE RESTRICT,
        CONSTRAINT "FK_step_executions_route_versions_route_version_id" FOREIGN KEY (route_version_id) REFERENCES engineering.route_versions (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_step_executions_users_assigned_by_id" FOREIGN KEY (assigned_by_id) REFERENCES security.users (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_step_executions_users_confirmed_by_id" FOREIGN KEY (confirmed_by_id) REFERENCES security.users (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE TABLE materials.stock_documents (
        id uuid NOT NULL DEFAULT (uuidv7()),
        created_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        updated_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        document_no character varying(64) NOT NULL,
        document_type character varying(32) NOT NULL,
        status character varying(32) NOT NULL,
        warehouse_id uuid NOT NULL,
        work_order_id uuid,
        lot_id uuid,
        posted_at timestamp with time zone,
        posted_by_id uuid,
        erp_reference character varying(128),
        erp_synced_at timestamp with time zone,
        notes text,
        CONSTRAINT "PK_stock_documents" PRIMARY KEY (id),
        CONSTRAINT "FK_stock_documents_lots_lot_id" FOREIGN KEY (lot_id) REFERENCES production.lots (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_stock_documents_users_posted_by_id" FOREIGN KEY (posted_by_id) REFERENCES security.users (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_stock_documents_warehouses_warehouse_id" FOREIGN KEY (warehouse_id) REFERENCES materials.warehouses (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_stock_documents_work_orders_work_order_id" FOREIGN KEY (work_order_id) REFERENCES production.work_orders (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE TABLE production.work_order_change_requests (
        id uuid NOT NULL DEFAULT (uuidv7()),
        created_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        updated_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        code character varying(64) NOT NULL,
        work_order_id uuid NOT NULL,
        reason text NOT NULL,
        status character varying(32) NOT NULL,
        proposed jsonb NOT NULL,
        previous_values jsonb NOT NULL,
        impact jsonb,
        effective_from timestamp with time zone,
        effective_from_lot_id uuid,
        requested_by_id uuid NOT NULL,
        submitted_at timestamp with time zone,
        approved_by_id uuid,
        approved_at timestamp with time zone,
        rejected_by_id uuid,
        rejected_at timestamp with time zone,
        rejection_reason text,
        applied_by_id uuid,
        applied_at timestamp with time zone,
        resulting_snapshot_version integer,
        CONSTRAINT "PK_work_order_change_requests" PRIMARY KEY (id),
        CONSTRAINT "FK_work_order_change_requests_lots_effective_from_lot_id" FOREIGN KEY (effective_from_lot_id) REFERENCES production.lots (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_work_order_change_requests_users_applied_by_id" FOREIGN KEY (applied_by_id) REFERENCES security.users (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_work_order_change_requests_users_approved_by_id" FOREIGN KEY (approved_by_id) REFERENCES security.users (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_work_order_change_requests_users_rejected_by_id" FOREIGN KEY (rejected_by_id) REFERENCES security.users (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_work_order_change_requests_users_requested_by_id" FOREIGN KEY (requested_by_id) REFERENCES security.users (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_work_order_change_requests_work_orders_work_order_id" FOREIGN KEY (work_order_id) REFERENCES production.work_orders (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE TABLE production.checklist_completions (
        id uuid NOT NULL DEFAULT (uuidv7()),
        created_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        updated_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        step_execution_id uuid NOT NULL,
        checklist_item_id uuid NOT NULL,
        checked_by_id uuid NOT NULL,
        checked_at timestamp with time zone NOT NULL,
        CONSTRAINT "PK_checklist_completions" PRIMARY KEY (id),
        CONSTRAINT "FK_checklist_completions_checklist_items_checklist_item_id" FOREIGN KEY (checklist_item_id) REFERENCES engineering.checklist_items (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_checklist_completions_step_executions_step_execution_id" FOREIGN KEY (step_execution_id) REFERENCES production.step_executions (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_checklist_completions_users_checked_by_id" FOREIGN KEY (checked_by_id) REFERENCES security.users (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE TABLE materials.material_allocations (
        id uuid NOT NULL DEFAULT (uuidv7()),
        created_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        updated_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        lot_id uuid,
        step_execution_id uuid,
        material_id uuid NOT NULL,
        work_order_id uuid,
        allocated_qty numeric(20,6) NOT NULL,
        returned_qty numeric(20,6) NOT NULL,
        expected_qty numeric(20,6) NOT NULL,
        consumed_qty numeric(20,6) NOT NULL,
        adjustment_qty numeric(20,6) NOT NULL,
        scrap_qty numeric(20,6) NOT NULL,
        status character varying(32) NOT NULL,
        allocated_by_id uuid NOT NULL,
        allocated_at timestamp with time zone NOT NULL,
        consumed_at timestamp with time zone,
        consumption_warehouse_id uuid,
        CONSTRAINT "PK_material_allocations" PRIMARY KEY (id),
        CONSTRAINT ck_material_allocations_1 CHECK (allocated_qty >= 0 AND returned_qty >= 0 AND expected_qty >= 0 AND consumed_qty >= 0 AND scrap_qty >= 0),
        CONSTRAINT "FK_material_allocations_lots_lot_id" FOREIGN KEY (lot_id) REFERENCES production.lots (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_material_allocations_materials_material_id" FOREIGN KEY (material_id) REFERENCES materials.materials (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_material_allocations_step_executions_step_execution_id" FOREIGN KEY (step_execution_id) REFERENCES production.step_executions (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_material_allocations_users_allocated_by_id" FOREIGN KEY (allocated_by_id) REFERENCES security.users (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_material_allocations_warehouses_consumption_warehouse_id" FOREIGN KEY (consumption_warehouse_id) REFERENCES materials.warehouses (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_material_allocations_work_orders_work_order_id" FOREIGN KEY (work_order_id) REFERENCES production.work_orders (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE TABLE production.output_values (
        id uuid NOT NULL DEFAULT (uuidv7()),
        created_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        updated_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        step_execution_id uuid NOT NULL,
        output_id uuid NOT NULL,
        value_text text,
        value_number numeric(20,6),
        value_boolean boolean,
        value_date timestamp with time zone,
        recorded_by_id uuid NOT NULL,
        recorded_at timestamp with time zone NOT NULL,
        CONSTRAINT "PK_output_values" PRIMARY KEY (id),
        CONSTRAINT ck_output_values_1 CHECK (num_nonnulls(value_text, value_number, value_boolean, value_date) = 1),
        CONSTRAINT "FK_output_values_output_definitions_output_id" FOREIGN KEY (output_id) REFERENCES engineering.output_definitions (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_output_values_step_executions_step_execution_id" FOREIGN KEY (step_execution_id) REFERENCES production.step_executions (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_output_values_users_recorded_by_id" FOREIGN KEY (recorded_by_id) REFERENCES security.users (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE TABLE production.process_confirmations (
        id uuid NOT NULL DEFAULT (uuidv7()),
        created_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        updated_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        lot_id uuid NOT NULL,
        step_execution_id uuid,
        confirmed_by_id uuid NOT NULL,
        confirmed_at timestamp with time zone NOT NULL,
        confirmation_type character varying(64) NOT NULL,
        notes text,
        value jsonb,
        CONSTRAINT "PK_process_confirmations" PRIMARY KEY (id),
        CONSTRAINT "FK_process_confirmations_lots_lot_id" FOREIGN KEY (lot_id) REFERENCES production.lots (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_process_confirmations_step_executions_step_execution_id" FOREIGN KEY (step_execution_id) REFERENCES production.step_executions (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_process_confirmations_users_confirmed_by_id" FOREIGN KEY (confirmed_by_id) REFERENCES security.users (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE TABLE production.process_runs (
        id uuid NOT NULL DEFAULT (uuidv7()),
        created_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        updated_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        step_execution_id uuid NOT NULL,
        equipment_id uuid NOT NULL,
        chamber_id uuid,
        recipe_version_id uuid NOT NULL,
        track_in_at timestamp with time zone NOT NULL,
        track_out_at timestamp with time zone,
        track_in_by_id uuid NOT NULL,
        track_out_by_id uuid,
        status character varying(32) NOT NULL,
        CONSTRAINT "PK_process_runs" PRIMARY KEY (id),
        CONSTRAINT ck_process_runs_1 CHECK (track_out_at IS NULL OR track_out_at >= track_in_at),
        CONSTRAINT ck_process_runs_2 CHECK (status IN ('Running', 'Completed', 'Aborted')),
        CONSTRAINT ck_process_runs_3 CHECK ((status = 'Running' AND track_out_at IS NULL AND track_out_by_id IS NULL) OR (status <> 'Running' AND track_out_at IS NOT NULL AND track_out_by_id IS NOT NULL)),
        CONSTRAINT "FK_process_runs_chambers_chamber_id" FOREIGN KEY (chamber_id) REFERENCES equipment.chambers (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_process_runs_equipment_equipment_id" FOREIGN KEY (equipment_id) REFERENCES equipment.equipment (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_process_runs_recipe_versions_recipe_version_id" FOREIGN KEY (recipe_version_id) REFERENCES engineering.recipe_versions (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_process_runs_step_executions_step_execution_id" FOREIGN KEY (step_execution_id) REFERENCES production.step_executions (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_process_runs_users_track_in_by_id" FOREIGN KEY (track_in_by_id) REFERENCES security.users (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_process_runs_users_track_out_by_id" FOREIGN KEY (track_out_by_id) REFERENCES security.users (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE TABLE production.rework_records (
        id uuid NOT NULL DEFAULT (uuidv7()),
        created_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        updated_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        lot_id uuid NOT NULL,
        original_step_execution_id uuid NOT NULL,
        rework_route_version_id uuid NOT NULL,
        return_route_step_id uuid NOT NULL,
        reason_code_id uuid NOT NULL,
        approved_by_id uuid NOT NULL,
        approved_at timestamp with time zone NOT NULL,
        CONSTRAINT "PK_rework_records" PRIMARY KEY (id),
        CONSTRAINT "FK_rework_records_lots_lot_id" FOREIGN KEY (lot_id) REFERENCES production.lots (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_rework_records_reason_codes_reason_code_id" FOREIGN KEY (reason_code_id) REFERENCES quality.reason_codes (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_rework_records_route_steps_return_route_step_id" FOREIGN KEY (return_route_step_id) REFERENCES engineering.route_steps (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_rework_records_route_versions_rework_route_version_id" FOREIGN KEY (rework_route_version_id) REFERENCES engineering.route_versions (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_rework_records_step_executions_original_step_execution_id" FOREIGN KEY (original_step_execution_id) REFERENCES production.step_executions (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_rework_records_users_approved_by_id" FOREIGN KEY (approved_by_id) REFERENCES security.users (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE TABLE quality.scrap_entries (
        id uuid NOT NULL DEFAULT (uuidv7()),
        created_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        updated_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        work_order_id uuid,
        step_execution_id uuid,
        reason_code_id uuid NOT NULL,
        quantity integer NOT NULL,
        shift_id uuid,
        notes text,
        reported_by_id uuid NOT NULL,
        reported_at timestamp with time zone NOT NULL,
        CONSTRAINT "PK_scrap_entries" PRIMARY KEY (id),
        CONSTRAINT ck_scrap_entries_1 CHECK (quantity > 0),
        CONSTRAINT "FK_scrap_entries_reason_codes_reason_code_id" FOREIGN KEY (reason_code_id) REFERENCES quality.reason_codes (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_scrap_entries_shifts_shift_id" FOREIGN KEY (shift_id) REFERENCES organization.shifts (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_scrap_entries_step_executions_step_execution_id" FOREIGN KEY (step_execution_id) REFERENCES production.step_executions (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_scrap_entries_users_reported_by_id" FOREIGN KEY (reported_by_id) REFERENCES security.users (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_scrap_entries_work_orders_work_order_id" FOREIGN KEY (work_order_id) REFERENCES production.work_orders (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE TABLE production.step_documents (
        id uuid NOT NULL DEFAULT (uuidv7()),
        created_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        updated_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        step_execution_id uuid NOT NULL,
        engineering_document_id uuid,
        name character varying(200) NOT NULL,
        reference text,
        file_path text,
        is_mandatory boolean NOT NULL,
        requires_validation boolean NOT NULL,
        validated_at timestamp with time zone,
        validated_by_id uuid,
        CONSTRAINT "PK_step_documents" PRIMARY KEY (id),
        CONSTRAINT "FK_step_documents_engineering_documents_engineering_document_id" FOREIGN KEY (engineering_document_id) REFERENCES engineering.engineering_documents (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_step_documents_step_executions_step_execution_id" FOREIGN KEY (step_execution_id) REFERENCES production.step_executions (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_step_documents_users_validated_by_id" FOREIGN KEY (validated_by_id) REFERENCES security.users (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE TABLE production.work_order_snapshots (
        id uuid NOT NULL DEFAULT (uuidv7()),
        created_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        updated_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        work_order_id uuid NOT NULL,
        version integer NOT NULL,
        snapshot jsonb NOT NULL,
        effective_from timestamp with time zone NOT NULL,
        effective_from_lot_id uuid,
        change_request_id uuid,
        created_by_id uuid NOT NULL,
        CONSTRAINT "PK_work_order_snapshots" PRIMARY KEY (id),
        CONSTRAINT ck_work_order_snapshots_1 CHECK (version > 0),
        CONSTRAINT "FK_work_order_snapshots_lots_effective_from_lot_id" FOREIGN KEY (effective_from_lot_id) REFERENCES production.lots (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_work_order_snapshots_users_created_by_id" FOREIGN KEY (created_by_id) REFERENCES security.users (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_work_order_snapshots_work_order_change_requests_change_requ~" FOREIGN KEY (change_request_id) REFERENCES production.work_order_change_requests (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_work_order_snapshots_work_orders_work_order_id" FOREIGN KEY (work_order_id) REFERENCES production.work_orders (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE TABLE quality.quality_checks (
        id uuid NOT NULL DEFAULT (uuidv7()),
        created_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        updated_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        lot_id uuid NOT NULL,
        process_run_id uuid,
        quality_check_template_id uuid NOT NULL,
        checked_by_id uuid NOT NULL,
        checked_at timestamp with time zone NOT NULL,
        production_quantity integer NOT NULL,
        all_passed boolean,
        notes text,
        CONSTRAINT "PK_quality_checks" PRIMARY KEY (id),
        CONSTRAINT ck_quality_checks_1 CHECK (production_quantity >= 0),
        CONSTRAINT "FK_quality_checks_lots_lot_id" FOREIGN KEY (lot_id) REFERENCES production.lots (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_quality_checks_process_runs_process_run_id" FOREIGN KEY (process_run_id) REFERENCES production.process_runs (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_quality_checks_quality_check_templates_quality_check_templa~" FOREIGN KEY (quality_check_template_id) REFERENCES quality.quality_check_templates (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_quality_checks_users_checked_by_id" FOREIGN KEY (checked_by_id) REFERENCES security.users (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE TABLE equipment.reticle_usages (
        id uuid NOT NULL DEFAULT (uuidv7()),
        created_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        updated_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        reticle_id uuid NOT NULL,
        process_run_id uuid NOT NULL,
        loaded_at timestamp with time zone NOT NULL,
        unloaded_at timestamp with time zone,
        CONSTRAINT "PK_reticle_usages" PRIMARY KEY (id),
        CONSTRAINT ck_reticle_usages_1 CHECK (unloaded_at IS NULL OR unloaded_at >= loaded_at),
        CONSTRAINT "FK_reticle_usages_process_runs_process_run_id" FOREIGN KEY (process_run_id) REFERENCES production.process_runs (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_reticle_usages_reticles_reticle_id" FOREIGN KEY (reticle_id) REFERENCES equipment.reticles (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE TABLE production.track_events (
        id uuid NOT NULL DEFAULT (uuidv7()),
        created_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        updated_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        process_run_id uuid NOT NULL,
        event_type character varying(32) NOT NULL,
        occurred_at timestamp with time zone NOT NULL,
        recorded_at timestamp with time zone NOT NULL,
        actor_id uuid NOT NULL,
        source character varying(32) NOT NULL,
        correlation_id uuid NOT NULL,
        idempotency_key character varying(128) NOT NULL,
        payload jsonb,
        CONSTRAINT "PK_track_events" PRIMARY KEY (id),
        CONSTRAINT "FK_track_events_process_runs_process_run_id" FOREIGN KEY (process_run_id) REFERENCES production.process_runs (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_track_events_users_actor_id" FOREIGN KEY (actor_id) REFERENCES security.users (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE TABLE materials.allocation_lot_picks (
        id uuid NOT NULL DEFAULT (uuidv7()),
        created_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        updated_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        material_allocation_id uuid NOT NULL,
        material_lot_id uuid NOT NULL,
        picked_qty numeric(20,6) NOT NULL,
        picking_strategy character varying(32) NOT NULL,
        consumption_warehouse_id uuid,
        CONSTRAINT "PK_allocation_lot_picks" PRIMARY KEY (id),
        CONSTRAINT ck_allocation_lot_picks_1 CHECK (picked_qty > 0),
        CONSTRAINT "FK_allocation_lot_picks_material_allocations_material_allocati~" FOREIGN KEY (material_allocation_id) REFERENCES materials.material_allocations (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_allocation_lot_picks_warehouses_consumption_warehouse_id" FOREIGN KEY (consumption_warehouse_id) REFERENCES materials.warehouses (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE TABLE production.carrier_loads (
        id uuid NOT NULL DEFAULT (uuidv7()),
        created_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        updated_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        carrier_slot_id uuid NOT NULL,
        wafer_id uuid NOT NULL,
        loaded_at timestamp with time zone NOT NULL,
        unloaded_at timestamp with time zone,
        CONSTRAINT "PK_carrier_loads" PRIMARY KEY (id),
        CONSTRAINT ck_carrier_loads_1 CHECK (unloaded_at IS NULL OR unloaded_at > loaded_at),
        CONSTRAINT "FK_carrier_loads_carrier_slots_carrier_slot_id" FOREIGN KEY (carrier_slot_id) REFERENCES production.carrier_slots (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE TABLE production.genealogy_details (
        id uuid NOT NULL DEFAULT (uuidv7()),
        created_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        updated_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        event_id uuid NOT NULL,
        wafer_id uuid NOT NULL,
        from_lot_id uuid,
        to_lot_id uuid NOT NULL,
        CONSTRAINT "PK_genealogy_details" PRIMARY KEY (id),
        CONSTRAINT ck_genealogy_details_1 CHECK (from_lot_id IS NULL OR from_lot_id <> to_lot_id),
        CONSTRAINT "FK_genealogy_details_genealogy_events_event_id" FOREIGN KEY (event_id) REFERENCES production.genealogy_events (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_genealogy_details_lots_from_lot_id" FOREIGN KEY (from_lot_id) REFERENCES production.lots (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_genealogy_details_lots_to_lot_id" FOREIGN KEY (to_lot_id) REFERENCES production.lots (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE TABLE quality.hold_records (
        id uuid NOT NULL DEFAULT (uuidv7()),
        created_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        updated_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        lot_id uuid NOT NULL,
        reason_code_id uuid NOT NULL,
        issue_id uuid,
        hold_scope character varying(32) NOT NULL,
        held_at timestamp with time zone NOT NULL,
        held_by_id uuid NOT NULL,
        released_at timestamp with time zone,
        released_by_id uuid,
        release_reason text,
        CONSTRAINT "PK_hold_records" PRIMARY KEY (id),
        CONSTRAINT ck_hold_records_1 CHECK (released_at IS NULL OR released_at >= held_at),
        CONSTRAINT ck_hold_records_2 CHECK ((released_at IS NULL AND released_by_id IS NULL AND release_reason IS NULL) OR (released_at IS NOT NULL AND released_by_id IS NOT NULL AND release_reason IS NOT NULL)),
        CONSTRAINT "FK_hold_records_lots_lot_id" FOREIGN KEY (lot_id) REFERENCES production.lots (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_hold_records_reason_codes_reason_code_id" FOREIGN KEY (reason_code_id) REFERENCES quality.reason_codes (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_hold_records_users_held_by_id" FOREIGN KEY (held_by_id) REFERENCES security.users (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_hold_records_users_released_by_id" FOREIGN KEY (released_by_id) REFERENCES security.users (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE TABLE quality.incoming_inspections (
        id uuid NOT NULL DEFAULT (uuidv7()),
        created_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        updated_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        inspection_plan_id uuid NOT NULL,
        material_id uuid NOT NULL,
        material_lot_id uuid,
        lot_number character varying(64) NOT NULL,
        supplier_lot_ref character varying(128),
        quantity_received numeric(20,6) NOT NULL,
        inspector_id uuid NOT NULL,
        started_at timestamp with time zone NOT NULL,
        completed_at timestamp with time zone,
        status character varying(32) NOT NULL,
        disposition character varying(32),
        disposition_notes text,
        disposition_by_id uuid,
        disposition_at timestamp with time zone,
        plan_version integer NOT NULL,
        issue_id uuid,
        CONSTRAINT "PK_incoming_inspections" PRIMARY KEY (id),
        CONSTRAINT ck_incoming_inspections_1 CHECK (quantity_received >= 0 AND plan_version > 0),
        CONSTRAINT ck_incoming_inspections_2 CHECK (completed_at IS NULL OR completed_at >= started_at),
        CONSTRAINT "FK_incoming_inspections_inspection_plans_inspection_plan_id" FOREIGN KEY (inspection_plan_id) REFERENCES quality.inspection_plans (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_incoming_inspections_materials_material_id" FOREIGN KEY (material_id) REFERENCES materials.materials (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_incoming_inspections_users_disposition_by_id" FOREIGN KEY (disposition_by_id) REFERENCES security.users (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_incoming_inspections_users_inspector_id" FOREIGN KEY (inspector_id) REFERENCES security.users (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE TABLE quality.inspection_results (
        id uuid NOT NULL DEFAULT (uuidv7()),
        created_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        updated_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        inspection_id uuid NOT NULL,
        criterion_name character varying(200) NOT NULL,
        criterion_type character varying(32) NOT NULL,
        required boolean NOT NULL,
        unit character varying(32),
        spec_min numeric(20,6),
        spec_max numeric(20,6),
        value_numeric numeric(20,6),
        value_boolean boolean,
        value_text text,
        is_passed boolean,
        notes text,
        CONSTRAINT "PK_inspection_results" PRIMARY KEY (id),
        CONSTRAINT ck_inspection_results_1 CHECK (spec_min IS NULL OR spec_max IS NULL OR spec_max >= spec_min),
        CONSTRAINT "FK_inspection_results_incoming_inspections_inspection_id" FOREIGN KEY (inspection_id) REFERENCES quality.incoming_inspections (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE TABLE quality.issue_actions (
        id uuid NOT NULL DEFAULT (uuidv7()),
        created_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        updated_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        issue_id uuid NOT NULL,
        action_type character varying(32) NOT NULL,
        title character varying(200) NOT NULL,
        description text,
        assigned_to_id uuid NOT NULL,
        due_date date,
        status character varying(32) NOT NULL,
        completed_at timestamp with time zone,
        completed_by_id uuid,
        verified_at timestamp with time zone,
        verified_by_id uuid,
        notes text,
        CONSTRAINT "PK_issue_actions" PRIMARY KEY (id),
        CONSTRAINT "FK_issue_actions_users_assigned_to_id" FOREIGN KEY (assigned_to_id) REFERENCES security.users (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_issue_actions_users_completed_by_id" FOREIGN KEY (completed_by_id) REFERENCES security.users (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_issue_actions_users_verified_by_id" FOREIGN KEY (verified_by_id) REFERENCES security.users (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE TABLE quality.issues (
        id uuid NOT NULL DEFAULT (uuidv7()),
        created_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        updated_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        work_order_id uuid,
        step_execution_id uuid,
        lot_id uuid,
        wafer_id uuid,
        equipment_id uuid,
        issue_type_id uuid NOT NULL,
        title character varying(200) NOT NULL,
        description text NOT NULL,
        status character varying(32) NOT NULL,
        reported_by_id uuid NOT NULL,
        assigned_to_id uuid,
        reported_at timestamp with time zone NOT NULL,
        acknowledged_at timestamp with time zone,
        resolved_at timestamp with time zone,
        closed_at timestamp with time zone,
        resolution_notes text,
        disposition character varying(32),
        non_conforming_qty integer,
        root_cause text,
        containment_action text,
        disposition_by_id uuid,
        disposition_at timestamp with time zone,
        CONSTRAINT "PK_issues" PRIMARY KEY (id),
        CONSTRAINT ck_issues_1 CHECK (non_conforming_qty IS NULL OR non_conforming_qty >= 0),
        CONSTRAINT "FK_issues_equipment_equipment_id" FOREIGN KEY (equipment_id) REFERENCES equipment.equipment (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_issues_issue_types_issue_type_id" FOREIGN KEY (issue_type_id) REFERENCES quality.issue_types (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_issues_lots_lot_id" FOREIGN KEY (lot_id) REFERENCES production.lots (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_issues_step_executions_step_execution_id" FOREIGN KEY (step_execution_id) REFERENCES production.step_executions (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_issues_users_assigned_to_id" FOREIGN KEY (assigned_to_id) REFERENCES security.users (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_issues_users_disposition_by_id" FOREIGN KEY (disposition_by_id) REFERENCES security.users (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_issues_users_reported_by_id" FOREIGN KEY (reported_by_id) REFERENCES security.users (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_issues_work_orders_work_order_id" FOREIGN KEY (work_order_id) REFERENCES production.work_orders (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE TABLE quality.lot_releases (
        id uuid NOT NULL DEFAULT (uuidv7()),
        created_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        updated_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        lot_id uuid NOT NULL,
        release_type character varying(32) NOT NULL,
        released_at timestamp with time zone NOT NULL,
        released_by_id uuid NOT NULL,
        reason text NOT NULL,
        issue_id uuid,
        CONSTRAINT "PK_lot_releases" PRIMARY KEY (id),
        CONSTRAINT ck_lot_releases_1 CHECK (release_type IN ('Production', 'Quality')),
        CONSTRAINT "FK_lot_releases_issues_issue_id" FOREIGN KEY (issue_id) REFERENCES quality.issues (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_lot_releases_lots_lot_id" FOREIGN KEY (lot_id) REFERENCES production.lots (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_lot_releases_users_released_by_id" FOREIGN KEY (released_by_id) REFERENCES security.users (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE TABLE materials.material_lots (
        id uuid NOT NULL DEFAULT (uuidv7()),
        created_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        updated_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        lot_number character varying(64) NOT NULL,
        material_id uuid NOT NULL,
        source_id uuid,
        quantity_received numeric(20,6) NOT NULL,
        quantity_available numeric(20,6) NOT NULL,
        unit_of_measure character varying(32) NOT NULL,
        received_at timestamp with time zone NOT NULL,
        manufacturing_date date,
        expiry_date date,
        status character varying(32) NOT NULL,
        supplier_lot_no character varying(128),
        supplier_reference character varying(128),
        source_container_no character varying(128),
        warehouse_id uuid,
        inspection_id uuid,
        issue_id uuid,
        hold_reason text,
        held_at timestamp with time zone,
        held_by_id uuid,
        released_at timestamp with time zone,
        released_by_id uuid,
        CONSTRAINT "PK_material_lots" PRIMARY KEY (id),
        CONSTRAINT ck_material_lots_1 CHECK (quantity_received >= 0 AND quantity_available >= 0),
        CONSTRAINT ck_material_lots_2 CHECK (expiry_date IS NULL OR manufacturing_date IS NULL OR expiry_date >= manufacturing_date),
        CONSTRAINT "FK_material_lots_incoming_inspections_inspection_id" FOREIGN KEY (inspection_id) REFERENCES quality.incoming_inspections (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_material_lots_issues_issue_id" FOREIGN KEY (issue_id) REFERENCES quality.issues (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_material_lots_material_sources_source_id" FOREIGN KEY (source_id) REFERENCES materials.material_sources (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_material_lots_materials_material_id" FOREIGN KEY (material_id) REFERENCES materials.materials (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_material_lots_users_held_by_id" FOREIGN KEY (held_by_id) REFERENCES security.users (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_material_lots_users_released_by_id" FOREIGN KEY (released_by_id) REFERENCES security.users (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_material_lots_warehouses_warehouse_id" FOREIGN KEY (warehouse_id) REFERENCES materials.warehouses (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE TABLE quality.quality_tasks (
        id uuid NOT NULL DEFAULT (uuidv7()),
        created_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        updated_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        quality_trigger_id uuid NOT NULL,
        status character varying(32) NOT NULL,
        lot_id uuid,
        equipment_id uuid,
        due_reason text NOT NULL,
        quality_check_id uuid,
        issue_id uuid,
        fired_at timestamp with time zone NOT NULL,
        completed_at timestamp with time zone,
        completed_by_id uuid,
        CONSTRAINT "PK_quality_tasks" PRIMARY KEY (id),
        CONSTRAINT "FK_quality_tasks_equipment_equipment_id" FOREIGN KEY (equipment_id) REFERENCES equipment.equipment (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_quality_tasks_issues_issue_id" FOREIGN KEY (issue_id) REFERENCES quality.issues (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_quality_tasks_lots_lot_id" FOREIGN KEY (lot_id) REFERENCES production.lots (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_quality_tasks_quality_checks_quality_check_id" FOREIGN KEY (quality_check_id) REFERENCES quality.quality_checks (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_quality_tasks_quality_triggers_quality_trigger_id" FOREIGN KEY (quality_trigger_id) REFERENCES quality.quality_triggers (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_quality_tasks_users_completed_by_id" FOREIGN KEY (completed_by_id) REFERENCES security.users (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE TABLE production.work_order_stops (
        id uuid NOT NULL DEFAULT (uuidv7()),
        created_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        updated_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        work_order_id uuid NOT NULL,
        lot_id uuid,
        stop_type character varying(32) NOT NULL,
        reason text NOT NULL,
        issue_id uuid,
        stopped_by_id uuid NOT NULL,
        stopped_at timestamp with time zone NOT NULL,
        resumed_by_id uuid,
        resumed_at timestamp with time zone,
        resume_notes text,
        context jsonb,
        CONSTRAINT "PK_work_order_stops" PRIMARY KEY (id),
        CONSTRAINT ck_work_order_stops_1 CHECK (resumed_at IS NULL OR resumed_at >= stopped_at),
        CONSTRAINT "FK_work_order_stops_issues_issue_id" FOREIGN KEY (issue_id) REFERENCES quality.issues (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_work_order_stops_lots_lot_id" FOREIGN KEY (lot_id) REFERENCES production.lots (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_work_order_stops_users_resumed_by_id" FOREIGN KEY (resumed_by_id) REFERENCES security.users (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_work_order_stops_users_stopped_by_id" FOREIGN KEY (stopped_by_id) REFERENCES security.users (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_work_order_stops_work_orders_work_order_id" FOREIGN KEY (work_order_id) REFERENCES production.work_orders (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE TABLE quality.material_holds (
        id uuid NOT NULL DEFAULT (uuidv7()),
        created_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        updated_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        material_lot_id uuid NOT NULL,
        reason_code_id uuid NOT NULL,
        issue_id uuid,
        held_at timestamp with time zone NOT NULL,
        held_by_id uuid NOT NULL,
        released_at timestamp with time zone,
        released_by_id uuid,
        release_reason text,
        CONSTRAINT "PK_material_holds" PRIMARY KEY (id),
        CONSTRAINT ck_material_holds_1 CHECK (released_at IS NULL OR released_at >= held_at),
        CONSTRAINT "FK_material_holds_issues_issue_id" FOREIGN KEY (issue_id) REFERENCES quality.issues (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_material_holds_material_lots_material_lot_id" FOREIGN KEY (material_lot_id) REFERENCES materials.material_lots (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_material_holds_reason_codes_reason_code_id" FOREIGN KEY (reason_code_id) REFERENCES quality.reason_codes (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_material_holds_users_held_by_id" FOREIGN KEY (held_by_id) REFERENCES security.users (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_material_holds_users_released_by_id" FOREIGN KEY (released_by_id) REFERENCES security.users (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE TABLE equipment.material_mounts (
        id uuid NOT NULL DEFAULT (uuidv7()),
        created_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        updated_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        material_lot_id uuid NOT NULL,
        equipment_id uuid NOT NULL,
        opened_at timestamp with time zone,
        mounted_at timestamp with time zone NOT NULL,
        unmounted_at timestamp with time zone,
        usable_until timestamp with time zone,
        usage_quantity numeric(20,6) NOT NULL,
        unit character varying(32) NOT NULL,
        CONSTRAINT "PK_material_mounts" PRIMARY KEY (id),
        CONSTRAINT ck_material_mounts_1 CHECK (usage_quantity >= 0),
        CONSTRAINT ck_material_mounts_2 CHECK (unmounted_at IS NULL OR unmounted_at >= mounted_at),
        CONSTRAINT "FK_material_mounts_equipment_equipment_id" FOREIGN KEY (equipment_id) REFERENCES equipment.equipment (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_material_mounts_material_lots_material_lot_id" FOREIGN KEY (material_lot_id) REFERENCES materials.material_lots (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE TABLE materials.material_sublots (
        id uuid NOT NULL DEFAULT (uuidv7()),
        created_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        updated_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        parent_lot_id uuid NOT NULL,
        sublot_number character varying(64) NOT NULL,
        quantity numeric(20,6) NOT NULL,
        unit_of_measure character varying(32) NOT NULL,
        status character varying(32) NOT NULL,
        notes text,
        CONSTRAINT "PK_material_sublots" PRIMARY KEY (id),
        CONSTRAINT ck_material_sublots_1 CHECK (quantity >= 0),
        CONSTRAINT "FK_material_sublots_material_lots_parent_lot_id" FOREIGN KEY (parent_lot_id) REFERENCES materials.material_lots (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE TABLE materials.stock_document_lines (
        id uuid NOT NULL DEFAULT (uuidv7()),
        created_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        updated_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        stock_document_id uuid NOT NULL,
        material_id uuid,
        product_id uuid,
        material_lot_id uuid,
        lot_number character varying(64),
        quantity numeric(20,6) NOT NULL,
        unit_of_measure character varying(32) NOT NULL,
        notes text,
        CONSTRAINT "PK_stock_document_lines" PRIMARY KEY (id),
        CONSTRAINT ck_stock_document_lines_1 CHECK (quantity > 0),
        CONSTRAINT ck_stock_document_lines_2 CHECK (num_nonnulls(material_id, product_id) = 1),
        CONSTRAINT "FK_stock_document_lines_material_lots_material_lot_id" FOREIGN KEY (material_lot_id) REFERENCES materials.material_lots (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_stock_document_lines_materials_material_id" FOREIGN KEY (material_id) REFERENCES materials.materials (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_stock_document_lines_products_product_id" FOREIGN KEY (product_id) REFERENCES engineering.products (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_stock_document_lines_stock_documents_stock_document_id" FOREIGN KEY (stock_document_id) REFERENCES materials.stock_documents (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE TABLE materials.stock_movements (
        id uuid NOT NULL DEFAULT (uuidv7()),
        created_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        updated_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        material_id uuid NOT NULL,
        material_lot_id uuid,
        warehouse_id uuid NOT NULL,
        movement_type character varying(32) NOT NULL,
        quantity numeric(20,6) NOT NULL,
        unit_of_measure character varying(32) NOT NULL,
        balance_after numeric(20,6) NOT NULL,
        source_type character varying(64) NOT NULL,
        source_id uuid NOT NULL,
        reason text NOT NULL,
        performed_by_id uuid NOT NULL,
        performed_at timestamp with time zone NOT NULL,
        CONSTRAINT "PK_stock_movements" PRIMARY KEY (id),
        CONSTRAINT ck_stock_movements_1 CHECK (quantity <> 0 AND balance_after >= 0),
        CONSTRAINT "FK_stock_movements_material_lots_material_lot_id" FOREIGN KEY (material_lot_id) REFERENCES materials.material_lots (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_stock_movements_materials_material_id" FOREIGN KEY (material_id) REFERENCES materials.materials (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_stock_movements_users_performed_by_id" FOREIGN KEY (performed_by_id) REFERENCES security.users (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_stock_movements_warehouses_warehouse_id" FOREIGN KEY (warehouse_id) REFERENCES materials.warehouses (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE TABLE production.wafers (
        id uuid NOT NULL DEFAULT (uuidv7()),
        created_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        updated_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        wafer_code character varying(64) NOT NULL,
        origin_material_lot_id uuid,
        supplier_wafer_code character varying(128),
        wafer_size_mm numeric(20,6) NOT NULL,
        lifecycle_status character varying(32) NOT NULL,
        produced_at timestamp with time zone,
        CONSTRAINT "PK_wafers" PRIMARY KEY (id),
        CONSTRAINT ck_wafers_1 CHECK (wafer_size_mm > 0),
        CONSTRAINT "FK_wafers_material_lots_origin_material_lot_id" FOREIGN KEY (origin_material_lot_id) REFERENCES materials.material_lots (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE TABLE materials.warehouse_stocks (
        id uuid NOT NULL DEFAULT (uuidv7()),
        created_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        updated_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        warehouse_id uuid NOT NULL,
        material_id uuid NOT NULL,
        material_lot_id uuid,
        quantity numeric(20,6) NOT NULL,
        unit_of_measure character varying(32) NOT NULL,
        erp_synced_at timestamp with time zone,
        CONSTRAINT "PK_warehouse_stocks" PRIMARY KEY (id),
        CONSTRAINT ck_warehouse_stocks_1 CHECK (quantity >= 0),
        CONSTRAINT "FK_warehouse_stocks_material_lots_material_lot_id" FOREIGN KEY (material_lot_id) REFERENCES materials.material_lots (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_warehouse_stocks_materials_material_id" FOREIGN KEY (material_id) REFERENCES materials.materials (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_warehouse_stocks_warehouses_warehouse_id" FOREIGN KEY (warehouse_id) REFERENCES materials.warehouses (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE TABLE materials.material_consumptions (
        id uuid NOT NULL DEFAULT (uuidv7()),
        created_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        updated_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        step_execution_id uuid NOT NULL,
        process_run_id uuid,
        material_lot_id uuid NOT NULL,
        sublot_id uuid,
        quantity_consumed numeric(20,6) NOT NULL,
        unit_of_measure character varying(32) NOT NULL,
        consumed_at timestamp with time zone NOT NULL,
        recorded_by_id uuid NOT NULL,
        CONSTRAINT "PK_material_consumptions" PRIMARY KEY (id),
        CONSTRAINT ck_material_consumptions_1 CHECK (quantity_consumed > 0),
        CONSTRAINT "FK_material_consumptions_material_lots_material_lot_id" FOREIGN KEY (material_lot_id) REFERENCES materials.material_lots (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_material_consumptions_material_sublots_sublot_id" FOREIGN KEY (sublot_id) REFERENCES materials.material_sublots (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_material_consumptions_process_runs_process_run_id" FOREIGN KEY (process_run_id) REFERENCES production.process_runs (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_material_consumptions_step_executions_step_execution_id" FOREIGN KEY (step_execution_id) REFERENCES production.step_executions (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_material_consumptions_users_recorded_by_id" FOREIGN KEY (recorded_by_id) REFERENCES security.users (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE TABLE production.lot_memberships (
        id uuid NOT NULL DEFAULT (uuidv7()),
        created_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        updated_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        wafer_id uuid NOT NULL,
        lot_id uuid NOT NULL,
        valid_from timestamp with time zone NOT NULL,
        valid_to timestamp with time zone,
        change_event_id uuid NOT NULL,
        CONSTRAINT "PK_lot_memberships" PRIMARY KEY (id),
        CONSTRAINT ck_lot_memberships_1 CHECK (valid_to IS NULL OR valid_to > valid_from),
        CONSTRAINT "FK_lot_memberships_genealogy_events_change_event_id" FOREIGN KEY (change_event_id) REFERENCES production.genealogy_events (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_lot_memberships_lots_lot_id" FOREIGN KEY (lot_id) REFERENCES production.lots (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_lot_memberships_wafers_wafer_id" FOREIGN KEY (wafer_id) REFERENCES production.wafers (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE TABLE quality.measurements (
        id uuid NOT NULL DEFAULT (uuidv7()),
        created_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        updated_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        quality_check_id uuid NOT NULL,
        process_run_id uuid,
        lot_id uuid NOT NULL,
        wafer_id uuid,
        sample_number integer NOT NULL,
        sample_point character varying(64),
        specification_version_id uuid NOT NULL,
        parameter_code character varying(64) NOT NULL,
        parameter_type character varying(32) NOT NULL,
        unit character varying(32),
        spec_min numeric(20,6),
        spec_max numeric(20,6),
        value_numeric numeric(20,6),
        value_boolean boolean,
        value_text text,
        is_passed boolean NOT NULL,
        measured_at timestamp with time zone NOT NULL,
        measurement_equipment_id uuid,
        recorded_by_id uuid NOT NULL,
        CONSTRAINT "PK_measurements" PRIMARY KEY (id),
        CONSTRAINT ck_measurements_1 CHECK (sample_number > 0),
        CONSTRAINT ck_measurements_2 CHECK (spec_min IS NULL OR spec_max IS NULL OR spec_max >= spec_min),
        CONSTRAINT ck_measurements_3 CHECK (num_nonnulls(value_numeric, value_boolean, value_text) = 1),
        CONSTRAINT "FK_measurements_equipment_measurement_equipment_id" FOREIGN KEY (measurement_equipment_id) REFERENCES equipment.equipment (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_measurements_lots_lot_id" FOREIGN KEY (lot_id) REFERENCES production.lots (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_measurements_process_runs_process_run_id" FOREIGN KEY (process_run_id) REFERENCES production.process_runs (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_measurements_quality_checks_quality_check_id" FOREIGN KEY (quality_check_id) REFERENCES quality.quality_checks (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_measurements_specification_versions_specification_version_id" FOREIGN KEY (specification_version_id) REFERENCES quality.specification_versions (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_measurements_users_recorded_by_id" FOREIGN KEY (recorded_by_id) REFERENCES security.users (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_measurements_wafers_wafer_id" FOREIGN KEY (wafer_id) REFERENCES production.wafers (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE TABLE production.process_batch_members (
        id uuid NOT NULL DEFAULT (uuidv7()),
        created_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        updated_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        process_batch_id uuid NOT NULL,
        process_run_id uuid NOT NULL,
        lot_id uuid NOT NULL,
        wafer_id uuid NOT NULL,
        position integer NOT NULL,
        CONSTRAINT "PK_process_batch_members" PRIMARY KEY (id),
        CONSTRAINT ck_process_batch_members_1 CHECK (position > 0),
        CONSTRAINT "FK_process_batch_members_lots_lot_id" FOREIGN KEY (lot_id) REFERENCES production.lots (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_process_batch_members_process_batches_process_batch_id" FOREIGN KEY (process_batch_id) REFERENCES production.process_batches (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_process_batch_members_process_runs_process_run_id" FOREIGN KEY (process_run_id) REFERENCES production.process_runs (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_process_batch_members_wafers_wafer_id" FOREIGN KEY (wafer_id) REFERENCES production.wafers (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE TABLE production.process_run_wafers (
        id uuid NOT NULL DEFAULT (uuidv7()),
        created_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        updated_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        process_run_id uuid NOT NULL,
        wafer_id uuid NOT NULL,
        result character varying(32),
        parameters jsonb,
        notes text,
        processed_at timestamp with time zone,
        CONSTRAINT "PK_process_run_wafers" PRIMARY KEY (id),
        CONSTRAINT "FK_process_run_wafers_process_runs_process_run_id" FOREIGN KEY (process_run_id) REFERENCES production.process_runs (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_process_run_wafers_wafers_wafer_id" FOREIGN KEY (wafer_id) REFERENCES production.wafers (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE TABLE quality.wafer_dispositions (
        id uuid NOT NULL DEFAULT (uuidv7()),
        created_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        updated_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        wafer_id uuid NOT NULL,
        lot_id_at_event uuid NOT NULL,
        process_run_id uuid,
        scrap_entry_id uuid,
        disposition_type character varying(32) NOT NULL,
        reason_code_id uuid NOT NULL,
        issue_id uuid,
        approved_by_id uuid NOT NULL,
        performed_by_id uuid NOT NULL,
        occurred_at timestamp with time zone NOT NULL,
        CONSTRAINT "PK_wafer_dispositions" PRIMARY KEY (id),
        CONSTRAINT "FK_wafer_dispositions_issues_issue_id" FOREIGN KEY (issue_id) REFERENCES quality.issues (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_wafer_dispositions_lots_lot_id_at_event" FOREIGN KEY (lot_id_at_event) REFERENCES production.lots (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_wafer_dispositions_process_runs_process_run_id" FOREIGN KEY (process_run_id) REFERENCES production.process_runs (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_wafer_dispositions_reason_codes_reason_code_id" FOREIGN KEY (reason_code_id) REFERENCES quality.reason_codes (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_wafer_dispositions_scrap_entries_scrap_entry_id" FOREIGN KEY (scrap_entry_id) REFERENCES quality.scrap_entries (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_wafer_dispositions_users_approved_by_id" FOREIGN KEY (approved_by_id) REFERENCES security.users (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_wafer_dispositions_users_performed_by_id" FOREIGN KEY (performed_by_id) REFERENCES security.users (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_wafer_dispositions_wafers_wafer_id" FOREIGN KEY (wafer_id) REFERENCES production.wafers (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_allocation_lot_picks_consumption_warehouse_id" ON materials.allocation_lot_picks (consumption_warehouse_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_allocation_lot_picks_material_allocation_id" ON materials.allocation_lot_picks (material_allocation_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_allocation_lot_picks_material_lot_id" ON materials.allocation_lot_picks (material_lot_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_api_keys_integration_config_id" ON integration.api_keys (integration_config_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE UNIQUE INDEX "IX_api_keys_key_hash" ON integration.api_keys (key_hash);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE UNIQUE INDEX "IX_areas_fab_id_code" ON organization.areas (fab_id, code);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_attachments_uploaded_by_id" ON audit.attachments (uploaded_by_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_audit_logs_correlation_id" ON audit.audit_logs (correlation_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_audit_logs_entity_type_entity_id_occurred_at" ON audit.audit_logs (entity_type, entity_id, occurred_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_audit_logs_reason_code_id" ON audit.audit_logs (reason_code_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_audit_logs_user_id" ON audit.audit_logs (user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_bom_items_material_id" ON materials.bom_items (material_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_bom_items_route_step_id" ON materials.bom_items (route_step_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_bom_items_route_version_id" ON materials.bom_items (route_version_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_business_events_correlation_id" ON audit.business_events (correlation_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_business_events_user_id" ON audit.business_events (user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE UNIQUE INDEX "IX_carrier_loads_carrier_slot_id" ON production.carrier_loads (carrier_slot_id) WHERE unloaded_at IS NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE UNIQUE INDEX "IX_carrier_loads_wafer_id" ON production.carrier_loads (wafer_id) WHERE unloaded_at IS NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_carrier_movements_carrier_id" ON production.carrier_movements (carrier_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_carrier_movements_from_location_id" ON production.carrier_movements (from_location_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_carrier_movements_performed_by_id" ON production.carrier_movements (performed_by_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_carrier_movements_to_location_id" ON production.carrier_movements (to_location_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE UNIQUE INDEX "IX_carrier_slots_carrier_id_slot_no" ON production.carrier_slots (carrier_id, slot_no);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE UNIQUE INDEX "IX_carriers_carrier_code" ON production.carriers (carrier_code);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_carriers_current_location_id" ON production.carriers (current_location_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_carriers_fab_id" ON production.carriers (fab_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE UNIQUE INDEX "IX_chambers_equipment_id_code" ON equipment.chambers (equipment_id, code);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_checklist_completions_checked_by_id" ON production.checklist_completions (checked_by_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_checklist_completions_checklist_item_id" ON production.checklist_completions (checklist_item_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE UNIQUE INDEX "IX_checklist_completions_step_execution_id_checklist_item_id" ON production.checklist_completions (step_execution_id, checklist_item_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_checklist_items_route_step_id" ON engineering.checklist_items (route_step_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_command_receipts_actor_user_id" ON audit.command_receipts (actor_user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE UNIQUE INDEX "IX_command_receipts_scope_idempotency_key" ON audit.command_receipts (scope, idempotency_key);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE UNIQUE INDEX "IX_companies_code" ON materials.companies (code);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE UNIQUE INDEX "IX_custom_field_definitions_entity_type_key" ON integration.custom_field_definitions (entity_type, key);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_downtimes_classified_by_id" ON equipment.downtimes (classified_by_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_downtimes_equipment_id" ON equipment.downtimes (equipment_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_downtimes_line_id" ON equipment.downtimes (line_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_downtimes_reason_code_id" ON equipment.downtimes (reason_code_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_downtimes_reported_by_id" ON equipment.downtimes (reported_by_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_downtimes_shift_id" ON equipment.downtimes (shift_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_engineering_documents_released_by_id" ON engineering.engineering_documents (released_by_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_engineering_documents_uploaded_by_id" ON engineering.engineering_documents (uploaded_by_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_equipment_area_id" ON equipment.equipment (area_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE UNIQUE INDEX "IX_equipment_code" ON equipment.equipment (code);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_equipment_equipment_group_id" ON equipment.equipment (equipment_group_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_equipment_equipment_type_id" ON equipment.equipment (equipment_type_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_equipment_line_id" ON equipment.equipment (line_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_equipment_events_equipment_id_event_timestamp" ON equipment.equipment_events (equipment_id, event_timestamp);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_equipment_events_machine_connection_id" ON equipment.equipment_events (machine_connection_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE UNIQUE INDEX "IX_equipment_groups_fab_id_code" ON equipment.equipment_groups (fab_id, code);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_equipment_qualifications_equipment_id" ON equipment.equipment_qualifications (equipment_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_equipment_qualifications_operation_id" ON equipment.equipment_qualifications (operation_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_equipment_qualifications_qualified_by_id" ON equipment.equipment_qualifications (qualified_by_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_equipment_qualifications_recipe_version_id" ON equipment.equipment_qualifications (recipe_version_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE UNIQUE INDEX "IX_equipment_states_equipment_id" ON equipment.equipment_states (equipment_id) WHERE ended_at IS NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE UNIQUE INDEX "IX_equipment_types_code" ON equipment.equipment_types (code);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE UNIQUE INDEX "IX_fabs_code" ON organization.fabs (code);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE UNIQUE INDEX "IX_genealogy_details_event_id_wafer_id" ON production.genealogy_details (event_id, wafer_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_genealogy_details_from_lot_id" ON production.genealogy_details (from_lot_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_genealogy_details_to_lot_id" ON production.genealogy_details (to_lot_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_genealogy_details_wafer_id" ON production.genealogy_details (wafer_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE UNIQUE INDEX "IX_genealogy_events_idempotency_key" ON production.genealogy_events (idempotency_key);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_genealogy_events_performed_by_id" ON production.genealogy_events (performed_by_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_hold_records_held_by_id" ON quality.hold_records (held_by_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_hold_records_issue_id" ON quality.hold_records (issue_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_hold_records_lot_id_held_at" ON quality.hold_records (lot_id, held_at) WHERE released_at IS NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_hold_records_reason_code_id" ON quality.hold_records (reason_code_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_hold_records_released_by_id" ON quality.hold_records (released_by_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_import_jobs_mapping_id" ON integration.import_jobs (mapping_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_import_jobs_user_id" ON integration.import_jobs (user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_incoming_inspections_disposition_by_id" ON quality.incoming_inspections (disposition_by_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_incoming_inspections_inspection_plan_id" ON quality.incoming_inspections (inspection_plan_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_incoming_inspections_inspector_id" ON quality.incoming_inspections (inspector_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_incoming_inspections_issue_id" ON quality.incoming_inspections (issue_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_incoming_inspections_material_id" ON quality.incoming_inspections (material_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_incoming_inspections_material_lot_id" ON quality.incoming_inspections (material_lot_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_inspection_plans_material_id" ON quality.inspection_plans (material_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_inspection_plans_material_type_id" ON quality.inspection_plans (material_type_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_inspection_plans_root_id" ON quality.inspection_plans (root_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_inspection_results_inspection_id" ON quality.inspection_results (inspection_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE UNIQUE INDEX "IX_integration_configs_system_name" ON integration.integration_configs (system_name);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_issue_actions_assigned_to_id" ON quality.issue_actions (assigned_to_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_issue_actions_completed_by_id" ON quality.issue_actions (completed_by_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_issue_actions_issue_id" ON quality.issue_actions (issue_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_issue_actions_verified_by_id" ON quality.issue_actions (verified_by_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE UNIQUE INDEX "IX_issue_types_code" ON quality.issue_types (code);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_issues_assigned_to_id" ON quality.issues (assigned_to_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_issues_disposition_by_id" ON quality.issues (disposition_by_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_issues_equipment_id" ON quality.issues (equipment_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_issues_issue_type_id" ON quality.issues (issue_type_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_issues_lot_id" ON quality.issues (lot_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_issues_reported_by_id" ON quality.issues (reported_by_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_issues_step_execution_id" ON quality.issues (step_execution_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_issues_wafer_id" ON quality.issues (wafer_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_issues_work_order_id" ON quality.issues (work_order_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE UNIQUE INDEX "IX_lines_area_id_code" ON organization.lines (area_id, code);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE UNIQUE INDEX "IX_locations_area_id_code" ON organization.locations (area_id, code);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_lot_memberships_change_event_id" ON production.lot_memberships (change_event_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_lot_memberships_lot_id_valid_from" ON production.lot_memberships (lot_id, valid_from);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE UNIQUE INDEX "IX_lot_memberships_wafer_id" ON production.lot_memberships (wafer_id) WHERE valid_to IS NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_lot_releases_issue_id" ON quality.lot_releases (issue_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_lot_releases_lot_id" ON quality.lot_releases (lot_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_lot_releases_released_by_id" ON quality.lot_releases (released_by_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_lots_current_route_step_id_route_version_id" ON production.lots (current_route_step_id, route_version_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE UNIQUE INDEX "IX_lots_fab_id_lot_number" ON production.lots (fab_id, lot_number);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_lots_product_revision_id" ON production.lots (product_revision_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_lots_released_to_route_by_id" ON production.lots (released_to_route_by_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_lots_route_version_id" ON production.lots (route_version_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_lots_work_order_id" ON production.lots (work_order_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_machine_connections_equipment_id" ON integration.machine_connections (equipment_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE UNIQUE INDEX "IX_machine_messages_machine_connection_id_source_message_id" ON integration.machine_messages (machine_connection_id, source_message_id) WHERE source_message_id IS NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_machine_tags_equipment_id" ON integration.machine_tags (equipment_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE UNIQUE INDEX "IX_machine_tags_machine_connection_id_name" ON integration.machine_tags (machine_connection_id, name);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_maintenance_events_assigned_to_id" ON equipment.maintenance_events (assigned_to_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_maintenance_events_equipment_id" ON equipment.maintenance_events (equipment_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_maintenance_events_schedule_id" ON equipment.maintenance_events (schedule_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_maintenance_events_tool_id" ON equipment.maintenance_events (tool_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_maintenance_schedules_assigned_to_id" ON equipment.maintenance_schedules (assigned_to_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_maintenance_schedules_equipment_id" ON equipment.maintenance_schedules (equipment_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_maintenance_schedules_tool_id" ON equipment.maintenance_schedules (tool_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_material_allocations_allocated_by_id" ON materials.material_allocations (allocated_by_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_material_allocations_consumption_warehouse_id" ON materials.material_allocations (consumption_warehouse_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_material_allocations_lot_id" ON materials.material_allocations (lot_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_material_allocations_material_id" ON materials.material_allocations (material_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_material_allocations_step_execution_id" ON materials.material_allocations (step_execution_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_material_allocations_work_order_id" ON materials.material_allocations (work_order_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_material_consumptions_material_lot_id" ON materials.material_consumptions (material_lot_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_material_consumptions_process_run_id" ON materials.material_consumptions (process_run_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_material_consumptions_recorded_by_id" ON materials.material_consumptions (recorded_by_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_material_consumptions_step_execution_id" ON materials.material_consumptions (step_execution_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_material_consumptions_sublot_id" ON materials.material_consumptions (sublot_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_material_holds_held_by_id" ON quality.material_holds (held_by_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_material_holds_issue_id" ON quality.material_holds (issue_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_material_holds_material_lot_id" ON quality.material_holds (material_lot_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_material_holds_reason_code_id" ON quality.material_holds (reason_code_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_material_holds_released_by_id" ON quality.material_holds (released_by_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_material_lots_held_by_id" ON materials.material_lots (held_by_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_material_lots_inspection_id" ON materials.material_lots (inspection_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_material_lots_issue_id" ON materials.material_lots (issue_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE UNIQUE INDEX "IX_material_lots_material_id_lot_number" ON materials.material_lots (material_id, lot_number);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_material_lots_released_by_id" ON materials.material_lots (released_by_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_material_lots_source_id" ON materials.material_lots (source_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_material_lots_warehouse_id" ON materials.material_lots (warehouse_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_material_mounts_equipment_id" ON equipment.material_mounts (equipment_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_material_mounts_material_lot_id" ON equipment.material_mounts (material_lot_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_material_sources_integration_config_id" ON materials.material_sources (integration_config_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE UNIQUE INDEX "IX_material_sources_material_id_integration_config_id_external~" ON materials.material_sources (material_id, integration_config_id, external_code);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE UNIQUE INDEX "IX_material_sublots_parent_lot_id_sublot_number" ON materials.material_sublots (parent_lot_id, sublot_number);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE UNIQUE INDEX "IX_material_types_code" ON materials.material_types (code);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE UNIQUE INDEX "IX_materials_code" ON materials.materials (code);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_materials_material_type_id" ON materials.materials (material_type_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_materials_supplier_id" ON materials.materials (supplier_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_measurements_lot_id" ON quality.measurements (lot_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_measurements_measurement_equipment_id" ON quality.measurements (measurement_equipment_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_measurements_process_run_id" ON quality.measurements (process_run_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_measurements_quality_check_id" ON quality.measurements (quality_check_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_measurements_recorded_by_id" ON quality.measurements (recorded_by_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_measurements_specification_version_id" ON quality.measurements (specification_version_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_measurements_wafer_id_measured_at" ON quality.measurements (wafer_id, measured_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE UNIQUE INDEX "IX_number_sequences_name" ON engineering.number_sequences (name);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_number_sequences_product_id" ON engineering.number_sequences (product_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE UNIQUE INDEX "IX_operations_code" ON engineering.operations (code);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_operations_equipment_type_id" ON engineering.operations (equipment_type_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_outbox_messages_next_attempt_at_occurred_at" ON audit.outbox_messages (next_attempt_at, occurred_at) WHERE published_at IS NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE UNIQUE INDEX "IX_output_definitions_route_step_id_key" ON engineering.output_definitions (route_step_id, key);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_output_values_output_id" ON production.output_values (output_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_output_values_recorded_by_id" ON production.output_values (recorded_by_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_output_values_step_execution_id" ON production.output_values (step_execution_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE UNIQUE INDEX "IX_permissions_code" ON security.permissions (code);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_process_batch_members_lot_id" ON production.process_batch_members (lot_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE UNIQUE INDEX "IX_process_batch_members_process_batch_id_position" ON production.process_batch_members (process_batch_id, position);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE UNIQUE INDEX "IX_process_batch_members_process_batch_id_wafer_id" ON production.process_batch_members (process_batch_id, wafer_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_process_batch_members_process_run_id" ON production.process_batch_members (process_run_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_process_batch_members_wafer_id" ON production.process_batch_members (wafer_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE UNIQUE INDEX "IX_process_batches_code" ON production.process_batches (code);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_process_batches_equipment_id" ON production.process_batches (equipment_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_process_confirmations_confirmed_by_id" ON production.process_confirmations (confirmed_by_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_process_confirmations_lot_id" ON production.process_confirmations (lot_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_process_confirmations_step_execution_id" ON production.process_confirmations (step_execution_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE UNIQUE INDEX "IX_process_run_wafers_process_run_id_wafer_id" ON production.process_run_wafers (process_run_id, wafer_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_process_run_wafers_wafer_id" ON production.process_run_wafers (wafer_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_process_runs_chamber_id" ON production.process_runs (chamber_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE UNIQUE INDEX "IX_process_runs_equipment_id" ON production.process_runs (equipment_id) WHERE status = 'Running';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_process_runs_recipe_version_id" ON production.process_runs (recipe_version_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE UNIQUE INDEX "IX_process_runs_step_execution_id" ON production.process_runs (step_execution_id) WHERE status = 'Running';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_process_runs_track_in_by_id" ON production.process_runs (track_in_by_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_process_runs_track_out_by_id" ON production.process_runs (track_out_by_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE UNIQUE INDEX "IX_product_revisions_product_id_revision_code" ON engineering.product_revisions (product_id, revision_code);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_product_revisions_released_by_id" ON engineering.product_revisions (released_by_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_product_revisions_route_version_id" ON engineering.product_revisions (route_version_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE UNIQUE INDEX "IX_products_code" ON engineering.products (code);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_q_time_rules_end_route_step_id" ON engineering.q_time_rules (end_route_step_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_q_time_rules_start_route_step_id" ON engineering.q_time_rules (start_route_step_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_q_time_windows_lot_id" ON production.q_time_windows (lot_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_q_time_windows_q_time_rule_id" ON production.q_time_windows (q_time_rule_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_quality_check_templates_route_step_id" ON quality.quality_check_templates (route_step_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE UNIQUE INDEX "IX_quality_check_templates_route_version_id_name_version" ON quality.quality_check_templates (route_version_id, name, version);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_quality_checks_checked_by_id" ON quality.quality_checks (checked_by_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_quality_checks_lot_id" ON quality.quality_checks (lot_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_quality_checks_process_run_id" ON quality.quality_checks (process_run_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_quality_checks_quality_check_template_id" ON quality.quality_checks (quality_check_template_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_quality_tasks_completed_by_id" ON quality.quality_tasks (completed_by_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_quality_tasks_equipment_id" ON quality.quality_tasks (equipment_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_quality_tasks_issue_id" ON quality.quality_tasks (issue_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_quality_tasks_lot_id" ON quality.quality_tasks (lot_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_quality_tasks_quality_check_id" ON quality.quality_tasks (quality_check_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_quality_tasks_quality_trigger_id" ON quality.quality_tasks (quality_trigger_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_quality_triggers_equipment_id" ON quality.quality_triggers (equipment_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_quality_triggers_product_id" ON quality.quality_triggers (product_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_quality_triggers_quality_check_template_id" ON quality.quality_triggers (quality_check_template_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE UNIQUE INDEX "IX_reason_codes_category_code" ON quality.reason_codes (category, code);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_recipe_applicabilities_equipment_id" ON engineering.recipe_applicabilities (equipment_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_recipe_applicabilities_recipe_version_id" ON engineering.recipe_applicabilities (recipe_version_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_recipe_applicabilities_route_step_id" ON engineering.recipe_applicabilities (route_step_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE UNIQUE INDEX "IX_recipe_versions_recipe_id_version" ON engineering.recipe_versions (recipe_id, version);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_recipe_versions_released_by_id" ON engineering.recipe_versions (released_by_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE UNIQUE INDEX "IX_recipes_code" ON engineering.recipes (code);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_reticle_usages_process_run_id" ON equipment.reticle_usages (process_run_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_reticle_usages_reticle_id" ON equipment.reticle_usages (reticle_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE UNIQUE INDEX "IX_reticles_code_revision" ON equipment.reticles (code, revision);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_rework_records_approved_by_id" ON production.rework_records (approved_by_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_rework_records_lot_id" ON production.rework_records (lot_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_rework_records_original_step_execution_id" ON production.rework_records (original_step_execution_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_rework_records_reason_code_id" ON production.rework_records (reason_code_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_rework_records_return_route_step_id" ON production.rework_records (return_route_step_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_rework_records_rework_route_version_id" ON production.rework_records (rework_route_version_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_role_claims_role_id" ON security.role_claims (role_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_role_permissions_permission_id" ON security.role_permissions (permission_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE UNIQUE INDEX "IX_role_permissions_role_id_permission_id" ON security.role_permissions (role_id, permission_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE UNIQUE INDEX "RoleNameIndex" ON security.roles (normalized_name);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_route_steps_equipment_type_id" ON engineering.route_steps (equipment_type_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_route_steps_operation_id" ON engineering.route_steps (operation_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE UNIQUE INDEX "IX_route_steps_route_version_id_sequence_no" ON engineering.route_steps (route_version_id, sequence_no);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_route_versions_released_by_id" ON engineering.route_versions (released_by_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE UNIQUE INDEX "IX_route_versions_route_id_version" ON engineering.route_versions (route_id, version);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE UNIQUE INDEX "IX_routes_code" ON engineering.routes (code);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_routes_product_id" ON engineering.routes (product_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_scrap_entries_reason_code_id" ON quality.scrap_entries (reason_code_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_scrap_entries_reported_by_id" ON quality.scrap_entries (reported_by_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_scrap_entries_shift_id" ON quality.scrap_entries (shift_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_scrap_entries_step_execution_id" ON quality.scrap_entries (step_execution_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_scrap_entries_work_order_id" ON quality.scrap_entries (work_order_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_shift_handovers_confirmed_by_id" ON organization.shift_handovers (confirmed_by_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_shift_handovers_line_id" ON organization.shift_handovers (line_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE UNIQUE INDEX "IX_shift_handovers_shift_id_business_date" ON organization.shift_handovers (shift_id, business_date);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE UNIQUE INDEX "IX_shifts_fab_id_code" ON organization.shifts (fab_id, code);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_shifts_line_id" ON organization.shifts (line_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE UNIQUE INDEX "IX_specification_versions_parameter_code_version" ON quality.specification_versions (parameter_code, version);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_specification_versions_released_by_id" ON quality.specification_versions (released_by_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_step_documents_engineering_document_id" ON production.step_documents (engineering_document_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_step_documents_step_execution_id" ON production.step_documents (step_execution_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_step_documents_validated_by_id" ON production.step_documents (validated_by_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_step_executions_assigned_by_id" ON production.step_executions (assigned_by_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_step_executions_confirmed_by_id" ON production.step_executions (confirmed_by_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE UNIQUE INDEX "IX_step_executions_lot_id" ON production.step_executions (lot_id) WHERE exited_at IS NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE UNIQUE INDEX "IX_step_executions_lot_id_route_step_id_visit_no" ON production.step_executions (lot_id, route_step_id, visit_no);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_step_executions_lot_id_route_version_id" ON production.step_executions (lot_id, route_version_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_step_executions_route_step_id_route_version_id" ON production.step_executions (route_step_id, route_version_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_step_executions_route_version_id" ON production.step_executions (route_version_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_stock_document_lines_material_id" ON materials.stock_document_lines (material_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_stock_document_lines_material_lot_id" ON materials.stock_document_lines (material_lot_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_stock_document_lines_product_id" ON materials.stock_document_lines (product_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_stock_document_lines_stock_document_id" ON materials.stock_document_lines (stock_document_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE UNIQUE INDEX "IX_stock_documents_document_no" ON materials.stock_documents (document_no);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_stock_documents_lot_id" ON materials.stock_documents (lot_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_stock_documents_posted_by_id" ON materials.stock_documents (posted_by_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_stock_documents_warehouse_id" ON materials.stock_documents (warehouse_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_stock_documents_work_order_id" ON materials.stock_documents (work_order_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_stock_movements_material_id" ON materials.stock_movements (material_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_stock_movements_material_lot_id" ON materials.stock_movements (material_lot_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_stock_movements_performed_by_id" ON materials.stock_movements (performed_by_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_stock_movements_warehouse_id" ON materials.stock_movements (warehouse_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE UNIQUE INDEX "IX_terminals_device_uuid" ON organization.terminals (device_uuid);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_terminals_workstation_id" ON organization.terminals (workstation_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE UNIQUE INDEX "IX_tools_code" ON equipment.tools (code);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_tools_equipment_type_id" ON equipment.tools (equipment_type_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_track_events_actor_id" ON production.track_events (actor_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE UNIQUE INDEX "IX_track_events_idempotency_key" ON production.track_events (idempotency_key);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_track_events_process_run_id_occurred_at" ON production.track_events (process_run_id, occurred_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_user_claims_user_id" ON security.user_claims (user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_user_fab_scopes_fab_id" ON security.user_fab_scopes (fab_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE UNIQUE INDEX "IX_user_fab_scopes_user_id_fab_id" ON security.user_fab_scopes (user_id, fab_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_user_logins_user_id" ON security.user_logins (user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_user_permissions_permission_id" ON security.user_permissions (permission_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE UNIQUE INDEX "IX_user_permissions_user_id_permission_id" ON security.user_permissions (user_id, permission_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_user_roles_role_id" ON security.user_roles (role_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "EmailIndex" ON security.users (normalized_email);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_users_worker_id" ON security.users (worker_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE UNIQUE INDEX "UserNameIndex" ON security.users (normalized_user_name);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_wafer_dispositions_approved_by_id" ON quality.wafer_dispositions (approved_by_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_wafer_dispositions_issue_id" ON quality.wafer_dispositions (issue_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_wafer_dispositions_lot_id_at_event" ON quality.wafer_dispositions (lot_id_at_event);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_wafer_dispositions_performed_by_id" ON quality.wafer_dispositions (performed_by_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_wafer_dispositions_process_run_id" ON quality.wafer_dispositions (process_run_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_wafer_dispositions_reason_code_id" ON quality.wafer_dispositions (reason_code_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_wafer_dispositions_scrap_entry_id" ON quality.wafer_dispositions (scrap_entry_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE UNIQUE INDEX "IX_wafer_dispositions_wafer_id" ON quality.wafer_dispositions (wafer_id) WHERE disposition_type = 'Scrap';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_wafers_origin_material_lot_id" ON production.wafers (origin_material_lot_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE UNIQUE INDEX "IX_wafers_wafer_code" ON production.wafers (wafer_code);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_warehouse_stocks_material_id" ON materials.warehouse_stocks (material_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_warehouse_stocks_material_lot_id" ON materials.warehouse_stocks (material_lot_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE UNIQUE INDEX "IX_warehouse_stocks_warehouse_id_material_id_material_lot_id" ON materials.warehouse_stocks (warehouse_id, material_id, material_lot_id) NULLS NOT DISTINCT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE UNIQUE INDEX "IX_warehouses_fab_id_code" ON materials.warehouses (fab_id, code);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_work_order_change_requests_applied_by_id" ON production.work_order_change_requests (applied_by_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_work_order_change_requests_approved_by_id" ON production.work_order_change_requests (approved_by_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE UNIQUE INDEX "IX_work_order_change_requests_code" ON production.work_order_change_requests (code);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_work_order_change_requests_effective_from_lot_id" ON production.work_order_change_requests (effective_from_lot_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_work_order_change_requests_rejected_by_id" ON production.work_order_change_requests (rejected_by_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_work_order_change_requests_requested_by_id" ON production.work_order_change_requests (requested_by_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_work_order_change_requests_work_order_id" ON production.work_order_change_requests (work_order_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_work_order_snapshots_change_request_id" ON production.work_order_snapshots (change_request_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_work_order_snapshots_created_by_id" ON production.work_order_snapshots (created_by_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_work_order_snapshots_effective_from_lot_id" ON production.work_order_snapshots (effective_from_lot_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE UNIQUE INDEX "IX_work_order_snapshots_work_order_id_version" ON production.work_order_snapshots (work_order_id, version);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_work_order_stops_issue_id" ON production.work_order_stops (issue_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_work_order_stops_lot_id" ON production.work_order_stops (lot_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_work_order_stops_resumed_by_id" ON production.work_order_stops (resumed_by_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_work_order_stops_stopped_by_id" ON production.work_order_stops (stopped_by_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_work_order_stops_work_order_id" ON production.work_order_stops (work_order_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_work_orders_customer_id" ON production.work_orders (customer_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_work_orders_fab_id" ON production.work_orders (fab_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE UNIQUE INDEX "IX_work_orders_order_no" ON production.work_orders (order_no);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_work_orders_product_id" ON production.work_orders (product_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_work_orders_product_revision_id_product_id" ON production.work_orders (product_revision_id, product_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_worker_qualifications_operation_id" ON equipment.worker_qualifications (operation_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_worker_qualifications_qualified_by_id" ON equipment.worker_qualifications (qualified_by_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_worker_qualifications_recipe_version_id" ON equipment.worker_qualifications (recipe_version_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_worker_qualifications_worker_id" ON equipment.worker_qualifications (worker_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE UNIQUE INDEX "IX_workers_fab_id_code" ON organization.workers (fab_id, code);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE UNIQUE INDEX "IX_workstations_area_id_code" ON organization.workstations (area_id, code);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    CREATE INDEX "IX_workstations_line_id" ON organization.workstations (line_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    ALTER TABLE materials.allocation_lot_picks ADD CONSTRAINT "FK_allocation_lot_picks_material_lots_material_lot_id" FOREIGN KEY (material_lot_id) REFERENCES materials.material_lots (id) ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    ALTER TABLE production.carrier_loads ADD CONSTRAINT "FK_carrier_loads_wafers_wafer_id" FOREIGN KEY (wafer_id) REFERENCES production.wafers (id) ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    ALTER TABLE production.genealogy_details ADD CONSTRAINT "FK_genealogy_details_wafers_wafer_id" FOREIGN KEY (wafer_id) REFERENCES production.wafers (id) ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    ALTER TABLE quality.hold_records ADD CONSTRAINT "FK_hold_records_issues_issue_id" FOREIGN KEY (issue_id) REFERENCES quality.issues (id) ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    ALTER TABLE quality.incoming_inspections ADD CONSTRAINT "FK_incoming_inspections_issues_issue_id" FOREIGN KEY (issue_id) REFERENCES quality.issues (id) ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    ALTER TABLE quality.incoming_inspections ADD CONSTRAINT "FK_incoming_inspections_material_lots_material_lot_id" FOREIGN KEY (material_lot_id) REFERENCES materials.material_lots (id) ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    ALTER TABLE quality.issue_actions ADD CONSTRAINT "FK_issue_actions_issues_issue_id" FOREIGN KEY (issue_id) REFERENCES quality.issues (id) ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    ALTER TABLE quality.issues ADD CONSTRAINT "FK_issues_wafers_wafer_id" FOREIGN KEY (wafer_id) REFERENCES production.wafers (id) ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194521_InitialFabSchema') THEN
    INSERT INTO public.__ef_migrations_history ("MigrationId", "ProductVersion")
    VALUES ('20261110194521_InitialFabSchema', '10.0.12');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194731_FabIntegrityGuards') THEN
    -- PostgreSQL-only protections that EF Core does not model natively.
    -- Keep this resource immutable once its migration has been deployed.
    ALTER TABLE production.lot_memberships ADD CONSTRAINT ex_lot_membership_period
        EXCLUDE USING gist (wafer_id WITH =, tstzrange(valid_from, valid_to, '[)') WITH &&);
    ALTER TABLE production.carrier_loads ADD CONSTRAINT ex_carrier_wafer_period
        EXCLUDE USING gist (wafer_id WITH =, tstzrange(loaded_at, unloaded_at, '[)') WITH &&);
    ALTER TABLE production.carrier_loads ADD CONSTRAINT ex_carrier_slot_period
        EXCLUDE USING gist (carrier_slot_id WITH =, tstzrange(loaded_at, unloaded_at, '[)') WITH &&);
    ALTER TABLE equipment.equipment_states ADD CONSTRAINT ex_equipment_state_period
        EXCLUDE USING gist (equipment_id WITH =, tstzrange(started_at, ended_at, '[)') WITH &&);

    CREATE FUNCTION production.validate_carrier_slot() RETURNS trigger LANGUAGE plpgsql AS $$
    DECLARE capacity integer;
    BEGIN
        SELECT slot_capacity INTO capacity FROM production.carriers WHERE id = NEW.carrier_id FOR SHARE;
        IF NEW.slot_no > capacity THEN
            RAISE EXCEPTION 'slot number exceeds carrier capacity' USING ERRCODE = '23514';
        END IF;
        RETURN NEW;
    END $$;
    CREATE TRIGGER validate_carrier_slot BEFORE INSERT OR UPDATE ON production.carrier_slots
        FOR EACH ROW EXECUTE FUNCTION production.validate_carrier_slot();

    CREATE FUNCTION production.validate_carrier_capacity() RETURNS trigger LANGUAGE plpgsql AS $$
    BEGIN
        IF EXISTS (SELECT 1 FROM production.carrier_slots WHERE carrier_id = NEW.id AND slot_no > NEW.slot_capacity) THEN
            RAISE EXCEPTION 'carrier capacity is smaller than existing slots' USING ERRCODE = '23514';
        END IF;
        RETURN NEW;
    END $$;
    CREATE TRIGGER validate_carrier_capacity BEFORE UPDATE OF slot_capacity ON production.carriers
        FOR EACH ROW EXECUTE FUNCTION production.validate_carrier_capacity();

    CREATE FUNCTION audit.reject_history_mutation() RETURNS trigger LANGUAGE plpgsql AS $$
    BEGIN
        RAISE EXCEPTION 'immutable history: %.%; append a correction event instead', TG_TABLE_SCHEMA, TG_TABLE_NAME
            USING ERRCODE = '23514';
    END $$;
    DO $$
    DECLARE table_name text;
    BEGIN
        FOREACH table_name IN ARRAY ARRAY[
            'audit.audit_logs', 'audit.business_events', 'production.track_events',
            'production.genealogy_events', 'production.genealogy_details',
            'production.process_confirmations', 'production.checklist_completions',
            'quality.measurements', 'quality.wafer_dispositions', 'quality.scrap_entries',
            'materials.stock_movements', 'materials.material_consumptions', 'equipment.equipment_events'
        ] LOOP
            EXECUTE format('CREATE TRIGGER immutable_history BEFORE UPDATE OR DELETE ON %s FOR EACH ROW EXECUTE FUNCTION audit.reject_history_mutation()', table_name);
        END LOOP;
    END $$;

    CREATE FUNCTION engineering.protect_released_version() RETURNS trigger LANGUAGE plpgsql AS $$
    BEGIN
        IF OLD.lifecycle_status IN ('Released', 'Obsolete') THEN
            IF TG_OP = 'DELETE' THEN
                RAISE EXCEPTION 'released version cannot be deleted' USING ERRCODE = '23514';
            END IF;
            IF NEW.lifecycle_status NOT IN ('Released', 'Obsolete')
               OR (OLD.lifecycle_status = 'Obsolete' AND NEW.lifecycle_status <> 'Obsolete')
               OR (to_jsonb(NEW) - ARRAY['updated_at','lifecycle_status','effective_to','obsolete_at'])
                  IS DISTINCT FROM (to_jsonb(OLD) - ARRAY['updated_at','lifecycle_status','effective_to','obsolete_at']) THEN
                RAISE EXCEPTION 'released version content is immutable; create a new version' USING ERRCODE = '23514';
            END IF;
        END IF;
        IF TG_OP = 'DELETE' THEN RETURN OLD; END IF;
        RETURN NEW;
    END $$;
    CREATE TRIGGER protect_released_version BEFORE UPDATE OR DELETE ON engineering.route_versions
        FOR EACH ROW EXECUTE FUNCTION engineering.protect_released_version();
    CREATE TRIGGER protect_released_version BEFORE UPDATE OR DELETE ON engineering.recipe_versions
        FOR EACH ROW EXECUTE FUNCTION engineering.protect_released_version();
    CREATE TRIGGER protect_released_version BEFORE UPDATE OR DELETE ON engineering.product_revisions
        FOR EACH ROW EXECUTE FUNCTION engineering.protect_released_version();
    CREATE TRIGGER protect_released_version BEFORE UPDATE OR DELETE ON quality.specification_versions
        FOR EACH ROW EXECUTE FUNCTION engineering.protect_released_version();

    CREATE FUNCTION engineering.protect_route_step() RETURNS trigger LANGUAGE plpgsql AS $$
    DECLARE version_id uuid; version_state text;
    BEGIN
        -- Lock the parent so publishing and editing its children cannot race.
        IF TG_OP <> 'INSERT' THEN
            SELECT lifecycle_status INTO version_state FROM engineering.route_versions WHERE id = OLD.route_version_id FOR SHARE;
            IF version_state IN ('Released', 'Obsolete') THEN
                RAISE EXCEPTION 'released route steps are immutable' USING ERRCODE = '23514';
            END IF;
        END IF;
        IF TG_OP <> 'DELETE' THEN
            version_id := NEW.route_version_id;
            SELECT lifecycle_status INTO version_state FROM engineering.route_versions WHERE id = version_id FOR SHARE;
            IF version_state IN ('Released', 'Obsolete') THEN
                RAISE EXCEPTION 'cannot add or move a step into a released route' USING ERRCODE = '23514';
            END IF;
            RETURN NEW;
        END IF;
        RETURN OLD;
    END $$;
    CREATE TRIGGER protect_route_step BEFORE INSERT OR UPDATE OR DELETE ON engineering.route_steps
        FOR EACH ROW EXECUTE FUNCTION engineering.protect_route_step();

    CREATE FUNCTION engineering.protect_step_definition() RETURNS trigger LANGUAGE plpgsql AS $$
    DECLARE parent_id uuid; version_state text;
    BEGIN
        IF TG_OP <> 'INSERT' THEN
            SELECT s.route_version_id INTO parent_id FROM engineering.route_steps s WHERE s.id = OLD.route_step_id;
            SELECT lifecycle_status INTO version_state FROM engineering.route_versions WHERE id = parent_id FOR SHARE;
            IF version_state IN ('Released', 'Obsolete') THEN
                RAISE EXCEPTION 'released step definitions are immutable' USING ERRCODE = '23514';
            END IF;
        END IF;
        IF TG_OP <> 'DELETE' THEN
            SELECT s.route_version_id INTO parent_id FROM engineering.route_steps s WHERE s.id = NEW.route_step_id;
            SELECT lifecycle_status INTO version_state FROM engineering.route_versions WHERE id = parent_id FOR SHARE;
            IF version_state IN ('Released', 'Obsolete') THEN
                RAISE EXCEPTION 'cannot change definitions of a released step' USING ERRCODE = '23514';
            END IF;
            RETURN NEW;
        END IF;
        RETURN OLD;
    END $$;
    CREATE TRIGGER protect_step_definition BEFORE INSERT OR UPDATE OR DELETE ON engineering.output_definitions
        FOR EACH ROW EXECUTE FUNCTION engineering.protect_step_definition();
    CREATE TRIGGER protect_step_definition BEFORE INSERT OR UPDATE OR DELETE ON engineering.checklist_items
        FOR EACH ROW EXECUTE FUNCTION engineering.protect_step_definition();

    -- Derived counts avoid storing a second editable source of Wafer membership truth.
    CREATE VIEW production.lot_quantities AS
    SELECT l.id AS lot_id,
           (SELECT count(*) FROM production.lot_memberships m WHERE m.lot_id = l.id AND m.valid_to IS NULL) AS current_wafer_count,
           (SELECT count(*) FROM quality.wafer_dispositions d WHERE d.lot_id_at_event = l.id AND d.disposition_type = 'Scrap') AS scrap_qty
    FROM production.lots l;
    CREATE VIEW equipment.state_durations AS
    SELECT id AS equipment_state_id, equipment_id, state, started_at, ended_at,
           extract(epoch FROM (coalesce(ended_at, CURRENT_TIMESTAMP) - started_at)) AS duration_seconds
    FROM equipment.equipment_states;

    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110194731_FabIntegrityGuards') THEN
    INSERT INTO public.__ef_migrations_history ("MigrationId", "ProductVersion")
    VALUES ('20261110194731_FabIntegrityGuards', '10.0.12');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110195026_CompleteRetainedStockFields') THEN
    DROP INDEX materials."IX_warehouse_stocks_material_lot_id";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110195026_CompleteRetainedStockFields') THEN
    DROP INDEX materials."IX_warehouse_stocks_warehouse_id_material_id_material_lot_id";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110195026_CompleteRetainedStockFields') THEN
    ALTER TABLE production.lots DROP CONSTRAINT ck_lots_1;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110195026_CompleteRetainedStockFields') THEN
    ALTER TABLE materials.warehouse_stocks ALTER COLUMN material_id DROP NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110195026_CompleteRetainedStockFields') THEN
    ALTER TABLE materials.warehouse_stocks ADD product_id uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110195026_CompleteRetainedStockFields') THEN
    ALTER TABLE production.process_run_wafers ADD operator_id uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110195026_CompleteRetainedStockFields') THEN
    ALTER TABLE production.lots ADD produced_qty integer NOT NULL DEFAULT 0;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110195026_CompleteRetainedStockFields') THEN
    ALTER TABLE materials.material_lots ADD CONSTRAINT "AK_material_lots_id_material_id" UNIQUE (id, material_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110195026_CompleteRetainedStockFields') THEN
    CREATE INDEX "IX_warehouse_stocks_material_lot_id_material_id" ON materials.warehouse_stocks (material_lot_id, material_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110195026_CompleteRetainedStockFields') THEN
    CREATE INDEX "IX_warehouse_stocks_product_id" ON materials.warehouse_stocks (product_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110195026_CompleteRetainedStockFields') THEN
    CREATE UNIQUE INDEX "IX_warehouse_stocks_warehouse_id_material_id_product_id_materi~" ON materials.warehouse_stocks (warehouse_id, material_id, product_id, material_lot_id) NULLS NOT DISTINCT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110195026_CompleteRetainedStockFields') THEN
    ALTER TABLE materials.warehouse_stocks ADD CONSTRAINT ck_warehouse_stocks_owner CHECK (num_nonnulls(material_id, product_id) = 1 AND (material_lot_id IS NULL OR material_id IS NOT NULL));
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110195026_CompleteRetainedStockFields') THEN
    CREATE INDEX "IX_process_run_wafers_operator_id" ON production.process_run_wafers (operator_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110195026_CompleteRetainedStockFields') THEN
    ALTER TABLE production.lots ADD CONSTRAINT ck_lots_1 CHECK (target_qty >= 0 AND produced_qty >= 0);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110195026_CompleteRetainedStockFields') THEN
    ALTER TABLE production.process_run_wafers ADD CONSTRAINT "FK_process_run_wafers_users_operator_id" FOREIGN KEY (operator_id) REFERENCES security.users (id) ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110195026_CompleteRetainedStockFields') THEN
    ALTER TABLE materials.warehouse_stocks ADD CONSTRAINT "FK_warehouse_stocks_material_lots_material_lot_id_material_id" FOREIGN KEY (material_lot_id, material_id) REFERENCES materials.material_lots (id, material_id) ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110195026_CompleteRetainedStockFields') THEN
    ALTER TABLE materials.warehouse_stocks ADD CONSTRAINT "FK_warehouse_stocks_products_product_id" FOREIGN KEY (product_id) REFERENCES engineering.products (id) ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110195026_CompleteRetainedStockFields') THEN
    INSERT INTO public.__ef_migrations_history ("MigrationId", "ProductVersion")
    VALUES ('20261110195026_CompleteRetainedStockFields', '10.0.12');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110213648_F01IdentityAndOrganization') THEN
    DROP INDEX security."IX_users_worker_id";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110213648_F01IdentityAndOrganization') THEN
    ALTER TABLE organization.terminals ADD fab_id uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110213648_F01IdentityAndOrganization') THEN
    UPDATE organization.terminals t SET fab_id = a.fab_id
    FROM organization.workstations w JOIN organization.areas a ON a.id = w.area_id
    WHERE t.workstation_id = w.id;
    DO $$ BEGIN
      IF EXISTS (SELECT 1 FROM organization.terminals WHERE fab_id IS NULL) THEN
        RAISE EXCEPTION 'F01 migration: unbound terminals require an explicit workstation/fab assignment before migration';
      END IF;
    END $$;
    ALTER TABLE organization.terminals ALTER COLUMN fab_id SET NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110213648_F01IdentityAndOrganization') THEN
    ALTER TABLE security.roles ADD is_active boolean NOT NULL DEFAULT TRUE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110213648_F01IdentityAndOrganization') THEN
    ALTER TABLE security.roles ADD is_system boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110213648_F01IdentityAndOrganization') THEN
    CREATE TABLE security.security_events (
        id uuid NOT NULL,
        occurred_at timestamp with time zone NOT NULL,
        user_id uuid,
        session_id uuid,
        event_type character varying(64) NOT NULL,
        outcome character varying(64) NOT NULL,
        correlation_id uuid NOT NULL,
        CONSTRAINT "PK_security_events" PRIMARY KEY (id),
        CONSTRAINT "FK_security_events_users_user_id" FOREIGN KEY (user_id) REFERENCES security.users (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110213648_F01IdentityAndOrganization') THEN
    CREATE TABLE security.user_sessions (
        id uuid NOT NULL,
        user_id uuid NOT NULL,
        created_at timestamp with time zone NOT NULL,
        last_activity_at timestamp with time zone NOT NULL,
        idle_expires_at timestamp with time zone NOT NULL,
        absolute_expires_at timestamp with time zone NOT NULL,
        revoked_at timestamp with time zone,
        revoke_reason character varying(500),
        restricted boolean NOT NULL,
        terminal_id uuid,
        security_stamp character varying(128) NOT NULL,
        CONSTRAINT "PK_user_sessions" PRIMARY KEY (id),
        CONSTRAINT ck_session_times CHECK (absolute_expires_at > created_at AND idle_expires_at <= absolute_expires_at),
        CONSTRAINT "FK_user_sessions_terminals_terminal_id" FOREIGN KEY (terminal_id) REFERENCES organization.terminals (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_user_sessions_users_user_id" FOREIGN KEY (user_id) REFERENCES security.users (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110213648_F01IdentityAndOrganization') THEN
    CREATE UNIQUE INDEX "IX_users_worker_id" ON security.users (worker_id) WHERE worker_id IS NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110213648_F01IdentityAndOrganization') THEN
    CREATE UNIQUE INDEX "IX_terminals_fab_id_code" ON organization.terminals (fab_id, code);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110213648_F01IdentityAndOrganization') THEN
    CREATE INDEX "IX_security_events_occurred_at" ON security.security_events (occurred_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110213648_F01IdentityAndOrganization') THEN
    CREATE INDEX "IX_security_events_user_id" ON security.security_events (user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110213648_F01IdentityAndOrganization') THEN
    CREATE INDEX "IX_user_sessions_terminal_id" ON security.user_sessions (terminal_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110213648_F01IdentityAndOrganization') THEN
    CREATE INDEX "IX_user_sessions_user_id_revoked_at" ON security.user_sessions (user_id, revoked_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110213648_F01IdentityAndOrganization') THEN
    ALTER TABLE organization.terminals ADD CONSTRAINT "FK_terminals_fabs_fab_id" FOREIGN KEY (fab_id) REFERENCES organization.fabs (id) ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110213648_F01IdentityAndOrganization') THEN
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
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20261110213648_F01IdentityAndOrganization') THEN
    INSERT INTO public.__ef_migrations_history ("MigrationId", "ProductVersion")
    VALUES ('20261110213648_F01IdentityAndOrganization', '10.0.12');
    END IF;
END $EF$;
COMMIT;

