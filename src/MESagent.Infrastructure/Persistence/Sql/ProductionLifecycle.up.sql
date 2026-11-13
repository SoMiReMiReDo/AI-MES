-- Stable ownership may be backfilled only from unambiguous historical membership.
WITH ownership AS (
    SELECT m.wafer_id, (array_agg(DISTINCT l.fab_id))[1] AS fab_id,
        (array_agg(DISTINCT l.work_order_id))[1] AS work_order_id
    FROM production.lot_memberships m JOIN production.lots l ON l.id=m.lot_id
    GROUP BY m.wafer_id
    HAVING count(DISTINCT l.fab_id)=1 AND count(DISTINCT l.work_order_id)=1
        AND count(*)=count(l.work_order_id)
)
UPDATE production.wafers w SET fab_id=o.fab_id, work_order_id=o.work_order_id
FROM ownership o WHERE o.wafer_id=w.id;
DO $$ BEGIN
    IF EXISTS (SELECT FROM production.wafers WHERE fab_id IS NULL OR work_order_id IS NULL) THEN
        RAISE EXCEPTION 'F4 migration requires explicit reconciliation of legacy wafer ownership';
    END IF;
END $$;

ALTER TABLE production.carriers ADD CONSTRAINT ck_carrier_wafer_size
    CHECK (allowed_wafer_size_mm IS NULL OR allowed_wafer_size_mm > 0);
ALTER TABLE production.wafers ADD CONSTRAINT ck_wafer_stable_scope
    CHECK ((fab_id IS NULL) = (work_order_id IS NULL));

CREATE FUNCTION production.protect_wafer_identity() RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN
    IF OLD.work_order_id IS NOT NULL AND
        (NEW.fab_id IS DISTINCT FROM OLD.fab_id OR NEW.work_order_id IS DISTINCT FROM OLD.work_order_id
        OR NEW.wafer_code <> OLD.wafer_code OR NEW.wafer_size_mm <> OLD.wafer_size_mm
        OR NEW.origin_material_lot_id IS DISTINCT FROM OLD.origin_material_lot_id) THEN
        RAISE EXCEPTION 'stable wafer identity is immutable' USING ERRCODE='23514';
    END IF;
    RETURN NEW;
END $$;
CREATE TRIGGER protect_wafer_identity BEFORE UPDATE ON production.wafers
    FOR EACH ROW EXECUTE FUNCTION production.protect_wafer_identity();

CREATE FUNCTION production.protect_managed_lot() RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN
    IF NEW.snapshot_version IS NOT NULL THEN
        IF NOT EXISTS (SELECT FROM production.work_orders w JOIN production.work_order_snapshots s
            ON s.work_order_id=w.id AND s.version=NEW.snapshot_version
            WHERE w.id=NEW.work_order_id AND w.fab_id=NEW.fab_id AND w.product_revision_id=NEW.product_revision_id
                AND (s.snapshot->'route'->>'id')::uuid=NEW.route_version_id) THEN
            RAISE EXCEPTION 'invalid managed lot baseline' USING ERRCODE='23514';
        END IF;
        IF TG_OP='UPDATE' AND (NEW.fab_id<>OLD.fab_id OR NEW.work_order_id IS DISTINCT FROM OLD.work_order_id
            OR NEW.product_revision_id<>OLD.product_revision_id OR NEW.route_version_id<>OLD.route_version_id
            OR NEW.snapshot_version IS DISTINCT FROM OLD.snapshot_version OR NEW.lot_number<>OLD.lot_number) THEN
            RAISE EXCEPTION 'managed lot baseline is immutable' USING ERRCODE='23514';
        END IF;
    ELSIF TG_OP='UPDATE' AND OLD.snapshot_version IS NOT NULL THEN
        RAISE EXCEPTION 'managed baseline cannot be cleared' USING ERRCODE='23514';
    END IF;
    RETURN NEW;
END $$;
CREATE TRIGGER protect_managed_lot BEFORE INSERT OR UPDATE ON production.lots
    FOR EACH ROW EXECUTE FUNCTION production.protect_managed_lot();

CREATE FUNCTION production.protect_production_interval() RETURNS trigger LANGUAGE plpgsql AS $$
DECLARE ending text; previous jsonb; next_row jsonb;
BEGIN
    IF TG_OP='DELETE' THEN
        RAISE EXCEPTION 'production history cannot be deleted' USING ERRCODE='23514';
    END IF;
    ending := CASE WHEN TG_TABLE_NAME='lot_memberships' THEN 'valid_to' ELSE 'unloaded_at' END;
    previous := to_jsonb(OLD); next_row := to_jsonb(NEW);
    IF previous->>ending IS NOT NULL OR next_row->>ending IS NULL
        OR (previous - ARRAY[ending,'updated_at']) IS DISTINCT FROM (next_row - ARRAY[ending,'updated_at']) THEN
        RAISE EXCEPTION 'interval may only be closed once' USING ERRCODE='23514';
    END IF;
    RETURN NEW;
END $$;
CREATE TRIGGER protect_membership_interval BEFORE UPDATE OR DELETE ON production.lot_memberships
    FOR EACH ROW EXECUTE FUNCTION production.protect_production_interval();
CREATE TRIGGER protect_load_interval BEFORE UPDATE OR DELETE ON production.carrier_loads
    FOR EACH ROW EXECUTE FUNCTION production.protect_production_interval();

CREATE FUNCTION production.validate_membership_scope() RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN
    IF EXISTS (SELECT FROM production.wafers w JOIN production.lots l ON l.id=NEW.lot_id
        WHERE w.id=NEW.wafer_id AND w.work_order_id IS NOT NULL
            AND (w.fab_id<>l.fab_id OR w.work_order_id IS DISTINCT FROM l.work_order_id)) THEN
        RAISE EXCEPTION 'wafer lot scope mismatch' USING ERRCODE='23514';
    END IF;
    RETURN NEW;
END $$;
CREATE TRIGGER validate_membership_scope BEFORE INSERT ON production.lot_memberships
    FOR EACH ROW EXECUTE FUNCTION production.validate_membership_scope();

CREATE FUNCTION production.validate_load_scope() RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN
    IF EXISTS (SELECT FROM production.wafers w JOIN production.carrier_slots s ON s.id=NEW.carrier_slot_id
        JOIN production.carriers c ON c.id=s.carrier_id
        WHERE w.id=NEW.wafer_id AND w.work_order_id IS NOT NULL
            AND (w.fab_id<>c.fab_id OR c.allowed_wafer_size_mm IS DISTINCT FROM w.wafer_size_mm)) THEN
        RAISE EXCEPTION 'wafer carrier scope or size mismatch' USING ERRCODE='23514';
    END IF;
    RETURN NEW;
END $$;
CREATE TRIGGER validate_load_scope BEFORE INSERT ON production.carrier_loads
    FOR EACH ROW EXECUTE FUNCTION production.validate_load_scope();

DO $$ DECLARE name text; BEGIN
    FOREACH name IN ARRAY ARRAY['wafer_releases','wafer_completions','step_execution_inheritances','carrier_movements'] LOOP
        EXECUTE format('CREATE TRIGGER immutable_history BEFORE UPDATE OR DELETE ON production.%I
            FOR EACH ROW EXECUTE FUNCTION audit.reject_history_mutation()',name);
    END LOOP;
END $$;
INSERT INTO security.permissions(code,name,is_active)
SELECT code,code,true FROM unnest(ARRAY[
    'production.lots.read','production.lots.create','production.lots.manage','production.lots.release',
    'production.lots.split','production.lots.merge','production.lots.cancel','production.wafers.read',
    'production.genealogy.read','production.carriers.read','production.carriers.manage',
    'production.carriers.load','production.carriers.move'
]) code ON CONFLICT(code) DO NOTHING;
INSERT INTO security.role_permissions(role_id,permission_id)
SELECT r.id,p.id FROM security.roles r CROSS JOIN security.permissions p
WHERE r.name='SystemAdmin' AND r.is_system AND p.code LIKE 'production.%'
ON CONFLICT(role_id,permission_id) DO NOTHING;
