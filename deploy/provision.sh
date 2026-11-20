#!/bin/sh
set -eu

case "$MES_APP_DB_PASSWORD" in
    *[!0-9a-f]*|'') exit 2 ;;
esac
test "${#MES_APP_DB_PASSWORD}" -eq 64

psql -X -q -h postgres -U mesagent_owner -d mesagent --single-transaction -v ON_ERROR_STOP=1 \
    -f - \
    -f /delivery/database/runtime-grants.sql \
    -f /delivery/database/equipment-runtime-grants.sql \
    -f /delivery/database/workbench-runtime-grants.sql \
    -f /delivery/database/integration-runtime-grants.sql \
    -f /delivery/database/reliability-runtime-grants.sql \
    -f /delivery/database/settings-runtime-grants.sql <<'SQL'
DO $$
BEGIN
    IF NOT EXISTS (SELECT FROM pg_roles WHERE rolname = 'mesagent_app') THEN
        CREATE ROLE mesagent_app LOGIN;
    END IF;
    IF EXISTS (
        SELECT FROM pg_roles
        WHERE rolname = 'mesagent_app'
            AND (rolsuper OR rolcreaterole OR rolcreatedb OR rolreplication OR rolbypassrls OR NOT rolcanlogin)
    ) THEN
        RAISE EXCEPTION 'mesagent_app must be an unprivileged login';
    END IF;
END $$;
\getenv app_password MES_APP_DB_PASSWORD
ALTER ROLE mesagent_app PASSWORD :'app_password';
SQL
