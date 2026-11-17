INSERT INTO security.permissions(code,name,is_active)
SELECT code,code,true FROM unnest(ARRAY[
    'quality.read','quality.configuration','quality.issues','quality.hold',
    'quality.release','quality.inspect','quality.scrap'
]) code ON CONFLICT(code) DO NOTHING;
INSERT INTO security.role_permissions(role_id,permission_id)
SELECT r.id,p.id FROM security.roles r CROSS JOIN security.permissions p
WHERE r.name='SystemAdmin' AND r.is_system AND p.code LIKE 'quality.%'
ON CONFLICT(role_id,permission_id) DO NOTHING;

INSERT INTO quality.reason_codes(code,name,category,is_active)
VALUES ('SYS-QUALITY-CHECK-FAILED','检验不合格自动冻结','Hold',true) ON CONFLICT(category,code) DO NOTHING;
INSERT INTO quality.issue_types(code,name,severity,is_blocking,is_active)
VALUES ('SYS-QUALITY-CHECK-FAILED','检验不合格','Major',true,true) ON CONFLICT(code) DO NOTHING;
UPDATE quality.issues i SET is_blocking=t.is_blocking FROM quality.issue_types t WHERE t.id=i.issue_type_id;
-- Preserve legacy used templates as frozen history; do not guess how legacy parameter JSON binds specifications.
UPDATE quality.quality_check_templates t SET lifecycle_status='Released'
WHERE EXISTS (SELECT FROM quality.quality_checks c WHERE c.quality_check_template_id=t.id);

CREATE TRIGGER protect_released_template BEFORE UPDATE OR DELETE ON quality.quality_check_templates
    FOR EACH ROW EXECUTE FUNCTION engineering.protect_released_version();
CREATE TRIGGER immutable_history BEFORE UPDATE OR DELETE ON quality.quality_checks
    FOR EACH ROW EXECUTE FUNCTION audit.reject_history_mutation();
CREATE TRIGGER immutable_history BEFORE UPDATE OR DELETE ON quality.lot_releases
    FOR EACH ROW EXECUTE FUNCTION audit.reject_history_mutation();
CREATE FUNCTION quality.protect_hold() RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN
    IF TG_OP='DELETE' THEN
        RAISE EXCEPTION 'hold history cannot be deleted' USING ERRCODE='23514';
    END IF;
    IF OLD.released_at IS NOT NULL OR NEW.released_at IS NULL OR
        (to_jsonb(OLD) - ARRAY['updated_at','released_at','released_by_id','release_reason']) IS DISTINCT FROM
        (to_jsonb(NEW) - ARRAY['updated_at','released_at','released_by_id','release_reason']) THEN
        RAISE EXCEPTION 'hold may only be released once' USING ERRCODE='23514';
    END IF;
    RETURN NEW;
END $$;
CREATE TRIGGER protect_hold BEFORE UPDATE OR DELETE ON quality.hold_records
    FOR EACH ROW EXECUTE FUNCTION quality.protect_hold();
CREATE FUNCTION quality.protect_issue_context() RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN
    IF TG_OP='DELETE' THEN
        RAISE EXCEPTION 'issue history cannot be deleted' USING ERRCODE='23514';
    END IF;
    IF NEW.lot_id IS DISTINCT FROM OLD.lot_id OR NEW.work_order_id IS DISTINCT FROM OLD.work_order_id
        OR NEW.wafer_id IS DISTINCT FROM OLD.wafer_id OR NEW.step_execution_id IS DISTINCT FROM OLD.step_execution_id
        OR NEW.equipment_id IS DISTINCT FROM OLD.equipment_id OR NEW.issue_type_id<>OLD.issue_type_id
        OR NEW.is_blocking<>OLD.is_blocking OR NEW.reported_by_id<>OLD.reported_by_id THEN
        RAISE EXCEPTION 'issue source context is immutable' USING ERRCODE='23514';
    END IF;
    RETURN NEW;
END $$;
CREATE TRIGGER protect_issue_context BEFORE UPDATE OR DELETE ON quality.issues
    FOR EACH ROW EXECUTE FUNCTION quality.protect_issue_context();
