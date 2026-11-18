WITH event AS (
    INSERT INTO audit.business_events (event_type, entity_type, entity_id, payload, occurred_at, correlation_id)
    VALUES ('WorkbenchRefreshRequested', 'Fab', '{{FAB_ID}}',
        '{"schemaVersion":1,"sources":["execution.read"]}', CURRENT_TIMESTAMP,
        'f1200000-0000-4000-8000-000000000001')
    RETURNING *
)
INSERT INTO audit.outbox_messages (
    business_event_id, event_type, aggregate_type, aggregate_id, payload,
    occurred_at, correlation_id, dead_lettered_at, attempts, last_error
)
SELECT id, event_type, entity_type, entity_id, payload,
    occurred_at, correlation_id, CURRENT_TIMESTAMP, 5, 'DeliveryFailed'
FROM event;
