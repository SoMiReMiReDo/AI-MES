INSERT INTO security.permissions(code,name,is_active)
SELECT code,code,true FROM unnest(ARRAY[
    'equipment.simulate','equipment.downtime','equipment.maintenance','equipment.release',
    'equipment.qualify','equipment.reticles','equipment.materials'
]) code ON CONFLICT(code) DO NOTHING;
INSERT INTO security.role_permissions(role_id,permission_id)
SELECT r.id,p.id FROM security.roles r CROSS JOIN security.permissions p
WHERE r.name='SystemAdmin' AND r.is_system AND p.code IN (
    'equipment.simulate','equipment.downtime','equipment.maintenance','equipment.release',
    'equipment.qualify','equipment.reticles','equipment.materials')
ON CONFLICT(role_id,permission_id) DO NOTHING;

-- Preserve known material identity; reject incompatible duplicates rather than discard history.
UPDATE equipment.material_mounts m SET material_id=l.material_id FROM materials.material_lots l
WHERE l.id=m.material_lot_id;

-- Start recording effective running state at activation; do not invent earlier state intervals.
UPDATE equipment.equipment_states s SET ended_at=GREATEST(CURRENT_TIMESTAMP,s.started_at+interval '1 microsecond')
WHERE s.ended_at IS NULL AND s.state='Idle' AND EXISTS (
    SELECT FROM equipment.equipment e JOIN production.process_runs r ON r.equipment_id=e.id
    WHERE e.id=s.equipment_id AND e.operational_state='Idle' AND r.status='Running');
INSERT INTO equipment.equipment_states(equipment_id,state,started_at,source)
SELECT e.id,'Running',GREATEST(CURRENT_TIMESTAMP,COALESCE((SELECT max(s.ended_at)
    FROM equipment.equipment_states s WHERE s.equipment_id=e.id),CURRENT_TIMESTAMP)),'Migration'
FROM equipment.equipment e WHERE e.operational_state='Idle'
    AND EXISTS (SELECT FROM production.process_runs r WHERE r.equipment_id=e.id AND r.status='Running')
    AND NOT EXISTS (SELECT FROM equipment.equipment_states s WHERE s.equipment_id=e.id AND s.ended_at IS NULL);

CREATE FUNCTION equipment.close_resource_interval() RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN
    IF TG_OP='DELETE' THEN
        RAISE EXCEPTION 'resource interval history cannot be deleted' USING ERRCODE='23514';
    END IF;
    IF (to_jsonb(OLD)->>TG_ARGV[0]) IS NOT NULL OR (to_jsonb(NEW)->>TG_ARGV[0]) IS NULL
        OR (to_jsonb(OLD)-ARRAY['updated_at',TG_ARGV[0]]) IS DISTINCT FROM
            (to_jsonb(NEW)-ARRAY['updated_at',TG_ARGV[0]]) THEN
        RAISE EXCEPTION 'resource interval may only be closed once' USING ERRCODE='23514';
    END IF;
    RETURN NEW;
END $$;
CREATE TRIGGER close_interval BEFORE UPDATE OR DELETE ON equipment.equipment_states
    FOR EACH ROW EXECUTE FUNCTION equipment.close_resource_interval('ended_at');
CREATE TRIGGER close_interval BEFORE UPDATE OR DELETE ON equipment.reticle_mounts
    FOR EACH ROW EXECUTE FUNCTION equipment.close_resource_interval('unloaded_at');
CREATE TRIGGER close_interval BEFORE UPDATE OR DELETE ON equipment.reticle_usages
    FOR EACH ROW EXECUTE FUNCTION equipment.close_resource_interval('unloaded_at');
CREATE TRIGGER close_interval BEFORE UPDATE OR DELETE ON equipment.material_mounts
    FOR EACH ROW EXECUTE FUNCTION equipment.close_resource_interval('unmounted_at');
CREATE TRIGGER immutable_history BEFORE UPDATE OR DELETE ON equipment.material_run_usages
    FOR EACH ROW EXECUTE FUNCTION audit.reject_history_mutation();

CREATE FUNCTION equipment.protect_downtime() RETURNS trigger LANGUAGE plpgsql AS $$
DECLARE s equipment.equipment_states;
BEGIN
    IF TG_OP='DELETE' THEN
        RAISE EXCEPTION 'downtime history cannot be deleted' USING ERRCODE='23514';
    END IF;
    IF NEW.equipment_state_id IS NOT NULL THEN
        SELECT * INTO STRICT s FROM equipment.equipment_states WHERE id=NEW.equipment_state_id;
        IF s.equipment_id<>NEW.equipment_id OR s.state<>'Down' OR s.started_at<>NEW.started_at
            OR (NEW.ended_at IS NOT NULL AND NEW.ended_at IS DISTINCT FROM s.ended_at) THEN
            RAISE EXCEPTION 'downtime must follow its Down interval' USING ERRCODE='23514';
        END IF;
    END IF;
    IF TG_OP='UPDATE' AND ((to_jsonb(OLD)-ARRAY['updated_at','reason_code_id','shift_id','notes',
        'needs_reason','classified_at','classified_by_id','ended_at']) IS DISTINCT FROM
        (to_jsonb(NEW)-ARRAY['updated_at','reason_code_id','shift_id','notes',
        'needs_reason','classified_at','classified_by_id','ended_at'])
        OR OLD.ended_at IS NOT NULL AND NEW.ended_at IS DISTINCT FROM OLD.ended_at) THEN
        RAISE EXCEPTION 'downtime identity and closed time are immutable' USING ERRCODE='23514';
    END IF;
    RETURN NEW;
END $$;
CREATE TRIGGER protect_downtime BEFORE INSERT OR UPDATE OR DELETE ON equipment.downtimes
    FOR EACH ROW EXECUTE FUNCTION equipment.protect_downtime();

CREATE FUNCTION equipment.protect_qualification() RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN
    IF TG_OP='DELETE' THEN
        RAISE EXCEPTION 'qualification history cannot be deleted' USING ERRCODE='23514';
    END IF;
    IF OLD.qualification_status<>'Qualified' OR NEW.qualification_status<>'Revoked'
        OR (to_jsonb(OLD)-ARRAY['updated_at','qualification_status']) IS DISTINCT FROM
            (to_jsonb(NEW)-ARRAY['updated_at','qualification_status']) THEN
        RAISE EXCEPTION 'qualification may only be revoked; recertify with a new record' USING ERRCODE='23514';
    END IF;
    RETURN NEW;
END $$;
CREATE TRIGGER protect_qualification BEFORE UPDATE OR DELETE ON equipment.equipment_qualifications
    FOR EACH ROW EXECUTE FUNCTION equipment.protect_qualification();
CREATE TRIGGER protect_qualification BEFORE UPDATE OR DELETE ON equipment.worker_qualifications
    FOR EACH ROW EXECUTE FUNCTION equipment.protect_qualification();

CREATE FUNCTION equipment.protect_maintenance() RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN
    IF TG_OP='DELETE' THEN
        RAISE EXCEPTION 'maintenance history cannot be deleted' USING ERRCODE='23514';
    END IF;
    IF NEW.equipment_id IS DISTINCT FROM OLD.equipment_id OR NEW.tool_id IS DISTINCT FROM OLD.tool_id
        OR NEW.schedule_id IS DISTINCT FROM OLD.schedule_id OR NEW.created_at<>OLD.created_at
        OR OLD.status IN ('Released','Cancelled')
        OR NOT ((OLD.status='Planned' AND NEW.status IN ('Planned','InProgress','Cancelled'))
            OR (OLD.status='InProgress' AND NEW.status='Completed')
            OR (OLD.status='Completed' AND NEW.status='Released')) THEN
        RAISE EXCEPTION 'invalid maintenance transition or identity change' USING ERRCODE='23514';
    END IF;
    RETURN NEW;
END $$;
CREATE TRIGGER protect_maintenance BEFORE UPDATE OR DELETE ON equipment.maintenance_events
    FOR EACH ROW EXECUTE FUNCTION equipment.protect_maintenance();
