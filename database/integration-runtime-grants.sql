DO $$ BEGIN
    IF NOT EXISTS (SELECT FROM pg_roles WHERE rolname = 'mesagent_app' AND NOT rolsuper) THEN
        RAISE EXCEPTION 'Existing non-superuser mesagent_app role is required';
    END IF;
END $$;
GRANT SELECT, INSERT ON integration.machine_connections, integration.machine_messages,
    integration.import_jobs TO mesagent_app;
GRANT UPDATE (status, updated_at, last_connected_at) ON integration.machine_connections TO mesagent_app;
GRANT UPDATE (status, updated_at, successful_rows, failed_rows, error_log, result_data, completed_at, dry_run)
    ON integration.import_jobs TO mesagent_app;
