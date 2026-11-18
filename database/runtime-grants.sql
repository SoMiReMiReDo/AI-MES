GRANT CONNECT ON DATABASE mesagent TO mesagent_app;
GRANT USAGE ON SCHEMA organization,security,audit TO mesagent_app;
GRANT SELECT ON ALL TABLES IN SCHEMA organization,security,audit TO mesagent_app;
GRANT INSERT,UPDATE,DELETE ON ALL TABLES IN SCHEMA organization,security TO mesagent_app;
REVOKE UPDATE,DELETE ON security.security_events FROM mesagent_app;
GRANT INSERT ON audit.audit_logs, audit.command_receipts TO mesagent_app;
GRANT INSERT,UPDATE,DELETE ON ALL TABLES IN SCHEMA engineering TO mesagent_app;
GRANT INSERT,UPDATE ON production.work_orders TO mesagent_app;
GRANT INSERT ON production.work_order_snapshots TO mesagent_app;
GRANT INSERT,UPDATE ON production.lots,production.wafers,production.carriers,production.carrier_loads,
    production.lot_memberships,production.step_executions TO mesagent_app;
GRANT INSERT ON production.carrier_slots,production.carrier_movements,production.genealogy_events,
    production.genealogy_details,production.wafer_releases,production.wafer_completions,
    production.step_execution_inheritances,production.step_documents,audit.business_events TO mesagent_app;
GRANT INSERT,UPDATE ON production.process_runs,production.process_run_wafers,production.step_documents,
    equipment.equipment,equipment.equipment_states TO mesagent_app;
GRANT INSERT ON production.track_events,production.checklist_completions,production.output_values,
    production.process_confirmations,equipment.equipment_types,equipment.equipment_groups,
    equipment.equipment_events TO mesagent_app;
GRANT INSERT,UPDATE ON quality.reason_codes,quality.issue_types,quality.issues,
    quality.issue_actions,quality.hold_records,quality.specification_versions,
    quality.quality_check_templates TO mesagent_app;
GRANT INSERT ON quality.quality_checks,quality.measurements,quality.lot_releases,
    quality.scrap_entries,quality.wafer_dispositions TO mesagent_app;
GRANT USAGE ON SCHEMA engineering,production,equipment,quality,materials,integration TO mesagent_app;
GRANT SELECT ON ALL TABLES IN SCHEMA engineering,production,equipment,quality,materials,integration TO mesagent_app;
GRANT INSERT ON materials.material_types,materials.materials,materials.material_sources,
    materials.warehouses,materials.material_lots TO mesagent_app;
