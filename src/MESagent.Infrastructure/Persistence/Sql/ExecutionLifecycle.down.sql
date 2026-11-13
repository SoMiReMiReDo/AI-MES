DROP TRIGGER immutable_history ON production.output_values;
DROP TRIGGER protect_run_participant ON production.process_run_wafers;
DROP FUNCTION production.protect_run_participant();
DROP TRIGGER protect_process_run ON production.process_runs;
DROP FUNCTION production.protect_process_run();
DELETE FROM security.role_permissions WHERE permission_id IN
    (SELECT id FROM security.permissions WHERE code LIKE 'execution.%' OR code LIKE 'equipment.%'
        OR code='planning.work-orders.recalculate');
DELETE FROM security.user_permissions WHERE permission_id IN
    (SELECT id FROM security.permissions WHERE code LIKE 'execution.%' OR code LIKE 'equipment.%'
        OR code='planning.work-orders.recalculate');
DELETE FROM security.permissions WHERE code LIKE 'execution.%' OR code LIKE 'equipment.%'
    OR code='planning.work-orders.recalculate';
