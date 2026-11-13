INSERT INTO security.permissions(code,name,is_active)
SELECT code,code,true FROM unnest(ARRAY[
    'execution.read','execution.dispatch','execution.track-in','execution.record',
    'execution.track-out','execution.abort','equipment.read','equipment.manage','equipment.state',
    'planning.work-orders.recalculate'
]) code ON CONFLICT(code) DO NOTHING;
INSERT INTO security.role_permissions(role_id,permission_id)
SELECT r.id,p.id FROM security.roles r CROSS JOIN security.permissions p
WHERE r.name='SystemAdmin' AND r.is_system AND
    (p.code LIKE 'execution.%' OR p.code LIKE 'equipment.%' OR p.code='planning.work-orders.recalculate')
ON CONFLICT(role_id,permission_id) DO NOTHING;

CREATE FUNCTION production.protect_process_run() RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN
    IF TG_OP='DELETE' THEN
        RAISE EXCEPTION 'run history cannot be deleted' USING ERRCODE='23514';
    END IF;
    IF OLD.status <> 'Running' OR
        (to_jsonb(OLD) - ARRAY['updated_at','status','track_out_at','track_out_by_id']) IS DISTINCT FROM
        (to_jsonb(NEW) - ARRAY['updated_at','status','track_out_at','track_out_by_id']) THEN
        RAISE EXCEPTION 'run identity and ended run are immutable' USING ERRCODE='23514';
    END IF;
    RETURN NEW;
END $$;
CREATE TRIGGER protect_process_run BEFORE UPDATE OR DELETE ON production.process_runs
    FOR EACH ROW EXECUTE FUNCTION production.protect_process_run();

CREATE FUNCTION production.protect_run_participant() RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN
    IF TG_OP='DELETE' THEN
        RAISE EXCEPTION 'run participants cannot be deleted' USING ERRCODE='23514';
    END IF;
    IF OLD.processed_at IS NOT NULL OR NEW.processed_at IS NULL
        OR OLD.process_run_id<>NEW.process_run_id OR OLD.wafer_id<>NEW.wafer_id OR OLD.id<>NEW.id THEN
        RAISE EXCEPTION 'participant identity and recorded result are immutable' USING ERRCODE='23514';
    END IF;
    RETURN NEW;
END $$;
CREATE TRIGGER protect_run_participant BEFORE UPDATE OR DELETE ON production.process_run_wafers
    FOR EACH ROW EXECUTE FUNCTION production.protect_run_participant();
CREATE TRIGGER immutable_history BEFORE UPDATE OR DELETE ON production.output_values
    FOR EACH ROW EXECUTE FUNCTION audit.reject_history_mutation();
