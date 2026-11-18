DO $$ BEGIN
    IF NOT EXISTS (SELECT FROM pg_roles WHERE rolname = 'mesagent_app' AND NOT rolsuper) THEN
        RAISE EXCEPTION 'Existing non-superuser mesagent_app role is required';
    END IF;
END $$;
GRANT SELECT, INSERT ON audit.outbox_messages TO mesagent_app;
GRANT UPDATE (attempts, published_at, dead_lettered_at, next_attempt_at, last_error, updated_at)
    ON audit.outbox_messages TO mesagent_app;
