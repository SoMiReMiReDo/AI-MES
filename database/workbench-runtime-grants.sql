DO $$ BEGIN
    IF NOT EXISTS (SELECT FROM pg_roles WHERE rolname = 'mesagent_app' AND NOT rolsuper) THEN
        RAISE EXCEPTION 'Existing non-superuser mesagent_app role is required';
    END IF;
END $$;
GRANT USAGE ON SCHEMA workbench TO mesagent_app;
GRANT SELECT ON ALL TABLES IN SCHEMA workbench TO mesagent_app;
GRANT INSERT ON workbench.equipment_ideal_rates TO mesagent_app;
GRANT INSERT, UPDATE ON workbench.production_schedule_blocks, workbench.subscriptions,
    workbench.alert_receipts TO mesagent_app;
