-- F7 incremental grants. Preserve role credentials and unrelated grants.
GRANT SELECT ON equipment.operation_policies,equipment.material_requirements,
    equipment.reticle_mounts,equipment.material_run_usages TO mesagent_app;
GRANT INSERT,UPDATE ON equipment.equipment_types,equipment.equipment_groups,equipment.chambers,
    equipment.equipment,equipment.equipment_states,equipment.downtimes,equipment.tools,
    equipment.maintenance_schedules,equipment.maintenance_events,equipment.equipment_qualifications,
    equipment.worker_qualifications,equipment.operation_policies,equipment.reticles,
    equipment.reticle_mounts,equipment.reticle_usages,equipment.material_mounts TO mesagent_app;
GRANT INSERT,DELETE ON equipment.material_requirements TO mesagent_app;
GRANT INSERT ON equipment.material_run_usages,equipment.equipment_events TO mesagent_app;
GRANT INSERT ON materials.material_types,materials.materials,materials.warehouses,materials.material_lots,
    organization.shifts TO mesagent_app;
GRANT INSERT,UPDATE ON quality.reason_codes TO mesagent_app;
