DO $$ BEGIN
    IF NOT EXISTS (SELECT FROM pg_roles WHERE rolname = 'mesagent_app' AND NOT rolsuper) THEN
        RAISE EXCEPTION 'Existing non-superuser mesagent_app role is required';
    END IF;
END $$;
GRANT USAGE ON SCHEMA settings TO mesagent_app;
GRANT SELECT, UPDATE ON settings.site_appearance TO mesagent_app;
GRANT SELECT, INSERT, UPDATE ON settings.fab_schedule_preferences TO mesagent_app;
