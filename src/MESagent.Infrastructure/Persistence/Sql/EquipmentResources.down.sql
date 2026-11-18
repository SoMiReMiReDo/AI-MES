DROP TRIGGER protect_maintenance ON equipment.maintenance_events;
DROP FUNCTION equipment.protect_maintenance();
DROP TRIGGER protect_qualification ON equipment.worker_qualifications;
DROP TRIGGER protect_qualification ON equipment.equipment_qualifications;
DROP FUNCTION equipment.protect_qualification();
DROP TRIGGER protect_downtime ON equipment.downtimes;
DROP FUNCTION equipment.protect_downtime();
DROP TRIGGER immutable_history ON equipment.material_run_usages;
DROP TRIGGER close_interval ON equipment.material_mounts;
DROP TRIGGER close_interval ON equipment.reticle_usages;
DROP TRIGGER close_interval ON equipment.reticle_mounts;
DROP TRIGGER close_interval ON equipment.equipment_states;
DROP FUNCTION equipment.close_resource_interval();
-- Keep granted permission definitions on rollback, matching earlier capability migrations.
