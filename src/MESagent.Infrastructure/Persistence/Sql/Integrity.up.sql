-- PostgreSQL-only protections that EF Core does not model natively.
-- Keep this resource immutable once its migration has been deployed.
ALTER TABLE production.lot_memberships ADD CONSTRAINT ex_lot_membership_period
    EXCLUDE USING gist (wafer_id WITH =, tstzrange(valid_from, valid_to, '[)') WITH &&);
ALTER TABLE production.carrier_loads ADD CONSTRAINT ex_carrier_wafer_period
    EXCLUDE USING gist (wafer_id WITH =, tstzrange(loaded_at, unloaded_at, '[)') WITH &&);
ALTER TABLE production.carrier_loads ADD CONSTRAINT ex_carrier_slot_period
    EXCLUDE USING gist (carrier_slot_id WITH =, tstzrange(loaded_at, unloaded_at, '[)') WITH &&);
ALTER TABLE equipment.equipment_states ADD CONSTRAINT ex_equipment_state_period
    EXCLUDE USING gist (equipment_id WITH =, tstzrange(started_at, ended_at, '[)') WITH &&);

CREATE FUNCTION production.validate_carrier_slot() RETURNS trigger LANGUAGE plpgsql AS $$
DECLARE capacity integer;
BEGIN
    SELECT slot_capacity INTO capacity FROM production.carriers WHERE id = NEW.carrier_id FOR SHARE;
    IF NEW.slot_no > capacity THEN
        RAISE EXCEPTION 'slot number exceeds carrier capacity' USING ERRCODE = '23514';
    END IF;
    RETURN NEW;
END $$;
CREATE TRIGGER validate_carrier_slot BEFORE INSERT OR UPDATE ON production.carrier_slots
    FOR EACH ROW EXECUTE FUNCTION production.validate_carrier_slot();

CREATE FUNCTION production.validate_carrier_capacity() RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN
    IF EXISTS (SELECT 1 FROM production.carrier_slots WHERE carrier_id = NEW.id AND slot_no > NEW.slot_capacity) THEN
        RAISE EXCEPTION 'carrier capacity is smaller than existing slots' USING ERRCODE = '23514';
    END IF;
    RETURN NEW;
END $$;
CREATE TRIGGER validate_carrier_capacity BEFORE UPDATE OF slot_capacity ON production.carriers
    FOR EACH ROW EXECUTE FUNCTION production.validate_carrier_capacity();

CREATE FUNCTION audit.reject_history_mutation() RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN
    RAISE EXCEPTION 'immutable history: %.%; append a correction event instead', TG_TABLE_SCHEMA, TG_TABLE_NAME
        USING ERRCODE = '23514';
END $$;
DO $$
DECLARE table_name text;
BEGIN
    FOREACH table_name IN ARRAY ARRAY[
        'audit.audit_logs', 'audit.business_events', 'production.track_events',
        'production.genealogy_events', 'production.genealogy_details',
        'production.process_confirmations', 'production.checklist_completions',
        'quality.measurements', 'quality.wafer_dispositions', 'quality.scrap_entries',
        'materials.stock_movements', 'materials.material_consumptions', 'equipment.equipment_events'
    ] LOOP
        EXECUTE format('CREATE TRIGGER immutable_history BEFORE UPDATE OR DELETE ON %s FOR EACH ROW EXECUTE FUNCTION audit.reject_history_mutation()', table_name);
    END LOOP;
END $$;

CREATE FUNCTION engineering.protect_released_version() RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN
    IF OLD.lifecycle_status IN ('Released', 'Obsolete') THEN
        IF TG_OP = 'DELETE' THEN
            RAISE EXCEPTION 'released version cannot be deleted' USING ERRCODE = '23514';
        END IF;
        IF NEW.lifecycle_status NOT IN ('Released', 'Obsolete')
           OR (OLD.lifecycle_status = 'Obsolete' AND NEW.lifecycle_status <> 'Obsolete')
           OR (to_jsonb(NEW) - ARRAY['updated_at','lifecycle_status','effective_to','obsolete_at'])
              IS DISTINCT FROM (to_jsonb(OLD) - ARRAY['updated_at','lifecycle_status','effective_to','obsolete_at']) THEN
            RAISE EXCEPTION 'released version content is immutable; create a new version' USING ERRCODE = '23514';
        END IF;
    END IF;
    IF TG_OP = 'DELETE' THEN RETURN OLD; END IF;
    RETURN NEW;
END $$;
CREATE TRIGGER protect_released_version BEFORE UPDATE OR DELETE ON engineering.route_versions
    FOR EACH ROW EXECUTE FUNCTION engineering.protect_released_version();
CREATE TRIGGER protect_released_version BEFORE UPDATE OR DELETE ON engineering.recipe_versions
    FOR EACH ROW EXECUTE FUNCTION engineering.protect_released_version();
CREATE TRIGGER protect_released_version BEFORE UPDATE OR DELETE ON engineering.product_revisions
    FOR EACH ROW EXECUTE FUNCTION engineering.protect_released_version();
CREATE TRIGGER protect_released_version BEFORE UPDATE OR DELETE ON quality.specification_versions
    FOR EACH ROW EXECUTE FUNCTION engineering.protect_released_version();

CREATE FUNCTION engineering.protect_route_step() RETURNS trigger LANGUAGE plpgsql AS $$
DECLARE version_id uuid; version_state text;
BEGIN
    -- Lock the parent so publishing and editing its children cannot race.
    IF TG_OP <> 'INSERT' THEN
        SELECT lifecycle_status INTO version_state FROM engineering.route_versions WHERE id = OLD.route_version_id FOR SHARE;
        IF version_state IN ('Released', 'Obsolete') THEN
            RAISE EXCEPTION 'released route steps are immutable' USING ERRCODE = '23514';
        END IF;
    END IF;
    IF TG_OP <> 'DELETE' THEN
        version_id := NEW.route_version_id;
        SELECT lifecycle_status INTO version_state FROM engineering.route_versions WHERE id = version_id FOR SHARE;
        IF version_state IN ('Released', 'Obsolete') THEN
            RAISE EXCEPTION 'cannot add or move a step into a released route' USING ERRCODE = '23514';
        END IF;
        RETURN NEW;
    END IF;
    RETURN OLD;
END $$;
CREATE TRIGGER protect_route_step BEFORE INSERT OR UPDATE OR DELETE ON engineering.route_steps
    FOR EACH ROW EXECUTE FUNCTION engineering.protect_route_step();

CREATE FUNCTION engineering.protect_step_definition() RETURNS trigger LANGUAGE plpgsql AS $$
DECLARE parent_id uuid; version_state text;
BEGIN
    IF TG_OP <> 'INSERT' THEN
        SELECT s.route_version_id INTO parent_id FROM engineering.route_steps s WHERE s.id = OLD.route_step_id;
        SELECT lifecycle_status INTO version_state FROM engineering.route_versions WHERE id = parent_id FOR SHARE;
        IF version_state IN ('Released', 'Obsolete') THEN
            RAISE EXCEPTION 'released step definitions are immutable' USING ERRCODE = '23514';
        END IF;
    END IF;
    IF TG_OP <> 'DELETE' THEN
        SELECT s.route_version_id INTO parent_id FROM engineering.route_steps s WHERE s.id = NEW.route_step_id;
        SELECT lifecycle_status INTO version_state FROM engineering.route_versions WHERE id = parent_id FOR SHARE;
        IF version_state IN ('Released', 'Obsolete') THEN
            RAISE EXCEPTION 'cannot change definitions of a released step' USING ERRCODE = '23514';
        END IF;
        RETURN NEW;
    END IF;
    RETURN OLD;
END $$;
CREATE TRIGGER protect_step_definition BEFORE INSERT OR UPDATE OR DELETE ON engineering.output_definitions
    FOR EACH ROW EXECUTE FUNCTION engineering.protect_step_definition();
CREATE TRIGGER protect_step_definition BEFORE INSERT OR UPDATE OR DELETE ON engineering.checklist_items
    FOR EACH ROW EXECUTE FUNCTION engineering.protect_step_definition();

-- Derived counts avoid storing a second editable source of Wafer membership truth.
CREATE VIEW production.lot_quantities AS
SELECT l.id AS lot_id,
       (SELECT count(*) FROM production.lot_memberships m WHERE m.lot_id = l.id AND m.valid_to IS NULL) AS current_wafer_count,
       (SELECT count(*) FROM quality.wafer_dispositions d WHERE d.lot_id_at_event = l.id AND d.disposition_type = 'Scrap') AS scrap_qty
FROM production.lots l;
CREATE VIEW equipment.state_durations AS
SELECT id AS equipment_state_id, equipment_id, state, started_at, ended_at,
       extract(epoch FROM (coalesce(ended_at, CURRENT_TIMESTAMP) - started_at)) AS duration_seconds
FROM equipment.equipment_states;
