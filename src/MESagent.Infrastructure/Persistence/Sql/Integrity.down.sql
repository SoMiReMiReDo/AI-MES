DROP VIEW equipment.state_durations;
DROP VIEW production.lot_quantities;
DROP TRIGGER protect_step_definition ON engineering.checklist_items;
DROP TRIGGER protect_step_definition ON engineering.output_definitions;
DROP FUNCTION engineering.protect_step_definition();
DROP TRIGGER protect_route_step ON engineering.route_steps;
DROP FUNCTION engineering.protect_route_step();
DROP TRIGGER protect_released_version ON engineering.route_versions;
DROP TRIGGER protect_released_version ON engineering.recipe_versions;
DROP TRIGGER protect_released_version ON engineering.product_revisions;
DROP TRIGGER protect_released_version ON quality.specification_versions;
DROP FUNCTION engineering.protect_released_version();
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
        EXECUTE format('DROP TRIGGER immutable_history ON %s', table_name);
    END LOOP;
END $$;
DROP FUNCTION audit.reject_history_mutation();
DROP TRIGGER validate_carrier_capacity ON production.carriers;
DROP FUNCTION production.validate_carrier_capacity();
DROP TRIGGER validate_carrier_slot ON production.carrier_slots;
DROP FUNCTION production.validate_carrier_slot();
ALTER TABLE equipment.equipment_states DROP CONSTRAINT ex_equipment_state_period;
ALTER TABLE production.carrier_loads DROP CONSTRAINT ex_carrier_slot_period;
ALTER TABLE production.carrier_loads DROP CONSTRAINT ex_carrier_wafer_period;
ALTER TABLE production.lot_memberships DROP CONSTRAINT ex_lot_membership_period;
