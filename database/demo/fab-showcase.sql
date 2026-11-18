\set ON_ERROR_STOP on
BEGIN;
SET LOCAL TIME ZONE 'UTC';
SET LOCAL lock_timeout = '10s';
SELECT pg_advisory_xact_lock(78001001);

CREATE FUNCTION pg_temp.demo_id(key text) RETURNS uuid LANGUAGE sql IMMUTABLE STRICT AS $$
    SELECT md5('mesagent-fab-showcase-v1:' || key)::uuid;
$$;

SELECT EXISTS (
    SELECT FROM materials.material_lots WHERE id = pg_temp.demo_id('incoming')
) AS demo_loaded \gset
\if :demo_loaded
    \echo 'FAB 展示数据已装载，跳过写入。'
    ROLLBACK;
    \quit
\endif

CREATE TEMP TABLE demo_context ON COMMIT DROP AS
SELECT a.fab_id, a.id AS area_id, e.line_id, e.equipment_type_id, e.equipment_group_id,
    u.id AS admin_id, s.id AS template_lot_id, w.product_id, w.product_revision_id,
    s.route_version_id, w.process_snapshot, f.timezone,
    (CURRENT_TIMESTAMP AT TIME ZONE f.timezone)::date AS today,
    date_trunc('minute', CURRENT_TIMESTAMP) - interval '10 minutes' AS anchor
FROM organization.areas a
JOIN organization.fabs f ON f.id = a.fab_id
JOIN equipment.equipment e ON e.area_id = a.id AND e.code = 'DEMO-TOOL-01'
JOIN production.lots s ON s.fab_id = a.fab_id AND s.lot_number = 'DEMO-LOT-DONE'
JOIN production.work_orders w ON w.id = s.work_order_id
JOIN security.user_fab_scopes scope ON scope.fab_id = a.fab_id
JOIN security.users u ON u.id = scope.user_id AND u.normalized_user_name = 'ADMIN' AND u.is_active
WHERE a.code = 'DEMO-AREA' AND s.status = 'Completed';

DO $$
BEGIN
    IF current_database() <> 'mesagent' OR (SELECT count(*) FROM demo_context) <> 1
        OR (SELECT process_snapshot IS NULL OR line_id IS NULL FROM demo_context) THEN
        RAISE EXCEPTION '需要本地 mesagent 库及唯一的 F1～F5 DEMO- 基础数据。';
    END IF;
END $$;

-- Two dedicated sample tools keep the calendar, maintenance and simulated Down separate.
INSERT INTO equipment.equipment
    (id, code, name, area_id, line_id, equipment_type_id, equipment_group_id,
     operational_state, communication_state, is_active, created_at, updated_at)
SELECT pg_temp.demo_id(v.key), v.code, v.label, c.area_id, c.line_id, c.equipment_type_id,
    c.equipment_group_id, v.state, v.comms, true, c.anchor - interval '4 days', c.anchor
FROM demo_context c CROSS JOIN (VALUES
    ('history-tool', 'DEMO-FAB-CELL', '演示工艺机台', 'Idle', 'Unknown'),
    ('sim-tool', 'DEMO-FAB-SIM', '演示设备模拟机台', 'Down', 'Online')
) v(key, code, label, state, comms);

CREATE TEMP TABLE demo_steps ON COMMIT DROP AS
SELECT route.id AS route_step_id, route.sequence_no, route.operation_id,
    route.name, route.instruction, route.requires_confirmation,
    s.id AS example_step_id, r.recipe_version_id,
    pg_temp.demo_id('applicability-' || route.sequence_no) AS applicability_id
FROM demo_context c
JOIN production.step_executions s ON s.lot_id = c.template_lot_id
JOIN engineering.route_steps route ON route.id = s.route_step_id
JOIN production.process_runs r ON r.step_execution_id = s.id AND r.status = 'Completed';

DO $$
BEGIN
    IF (SELECT array_agg(sequence_no ORDER BY sequence_no) FROM demo_steps)
        IS DISTINCT FROM ARRAY[10, 20, 30] THEN
        RAISE EXCEPTION '演示基线须含三个各已完成一次的步骤。';
    END IF;
END $$;

INSERT INTO engineering.recipe_applicabilities
    (id, route_step_id, recipe_version_id, equipment_id, effective_from, created_at, updated_at)
SELECT s.applicability_id, s.route_step_id, s.recipe_version_id, pg_temp.demo_id('history-tool'),
    c.anchor - interval '4 days', c.anchor - interval '4 days', c.anchor
FROM demo_steps s CROSS JOIN demo_context c;

-- An incoming registration carries provenance, with the quantity interpreted by the existing UI.
INSERT INTO materials.material_types (id, code, name, is_active, created_at, updated_at)
SELECT pg_temp.demo_id('type-' || v.code), v.code, v.name, true, c.anchor, c.anchor
FROM demo_context c CROSS JOIN (VALUES ('SYS-WAFER-INPUT', '硅片来料'), ('SYS-EQUIPMENT-CONSUMABLE', '设备上机耗材'))
    v(code, name)
ON CONFLICT (code) DO NOTHING;

INSERT INTO materials.materials
    (id, code, name, material_type_id, unit_of_measure, tracking_type, is_active, created_at, updated_at)
SELECT pg_temp.demo_id(v.key), v.code, v.label, t.id, v.unit, 'Lot', true, c.anchor, c.anchor
FROM demo_context c CROSS JOIN (VALUES
    ('incoming-material', 'DEMO-FAB-SI300', '演示 300 mm 硅片', 'SYS-WAFER-INPUT', 'PCS'),
    ('consumable', 'DEMO-FAB-FILTER', '演示过滤组件', 'SYS-EQUIPMENT-CONSUMABLE', 'ea')
) v(key, code, label, type_code, unit)
JOIN materials.material_types t ON t.code = v.type_code;

INSERT INTO materials.warehouses (id, fab_id, code, name, kind, is_active, created_at, updated_at)
SELECT pg_temp.demo_id('warehouse-' || v.code), c.fab_id, v.code, v.label, v.kind, true, c.anchor, c.anchor
FROM demo_context c CROSS JOIN (VALUES
    ('SYS-WAFER-ORIGIN-REGISTER', '硅片来料来源登记', 'Registration'),
    ('SYS-EQUIPMENT-REGISTER', '演示设备耗材登记', 'Registration')
) v(code, label, kind)
ON CONFLICT (fab_id, code) DO NOTHING;

INSERT INTO materials.material_sources
    (id, material_id, manual_fab_id, external_code, external_name, conversion_factor,
     sync_enabled, created_at, updated_at)
SELECT pg_temp.demo_id('incoming-source'), pg_temp.demo_id('incoming-material'), c.fab_id,
    'DEMO-FAB-SUPPLIER', '演示硅片供应商', 1, false, c.anchor - interval '4 days', c.anchor
FROM demo_context c;

INSERT INTO materials.material_lots
    (id, material_id, warehouse_id, source_id, lot_number, supplier_lot_no, status,
     quantity_received, quantity_available, unit_of_measure, received_at, expiry_date,
     resource_expiry_at, created_at, updated_at)
SELECT pg_temp.demo_id(v.key), pg_temp.demo_id(v.material), w.id, pg_temp.demo_id(v.source),
    v.lot, v.supplier, v.status, v.qty, v.qty, v.unit, c.anchor - interval '4 days',
    c.today + v.days, CASE WHEN v.key = 'filter-lot' THEN c.anchor + interval '10 days' END,
    c.anchor - interval '4 days', c.anchor
FROM demo_context c CROSS JOIN (VALUES
    ('incoming', 'incoming-material', 'SYS-WAFER-ORIGIN-REGISTER', 'incoming-source',
      'DEMO-FAB-IN-01', 'DEMO-SUP-300-01', 'Registered', 0, 'PCS', 60),
    ('incoming-unused', 'incoming-material', 'SYS-WAFER-ORIGIN-REGISTER', 'incoming-source',
      'DEMO-FAB-IN-02', 'DEMO-SUP-300-02', 'Registered', 0, 'PCS', 90),
    ('filter-lot', 'consumable', 'SYS-EQUIPMENT-REGISTER', NULL,
      'DEMO-FAB-FILTER-01', NULL, 'Available', 0, 'ea', 10)
) v(key, material, warehouse, source, lot, supplier, status, qty, unit, days)
JOIN materials.warehouses w ON w.fab_id = c.fab_id AND w.code = v.warehouse;

-- Use the released DEMO route and its existing PDF. Every displayed quantity has per-wafer facts.
CREATE TEMP TABLE demo_lots ON COMMIT DROP AS
SELECT v.key, v.qty, v.status, v.scrap, c.anchor - v.age AS base,
    'DEMO-FAB-WO-' || v.key AS order_no, 'DEMO-FAB-' || v.key AS lot_no,
    pg_temp.demo_id('order-' || v.key) AS order_id, pg_temp.demo_id('lot-' || v.key) AS lot_id
FROM demo_context c CROSS JOIN (VALUES
    ('HIST-03', 4, 'Completed', 0, interval '3 days 2 hours'),
    ('HIST-01', 6, 'Completed', 0, interval '1 day 3 hours'),
    ('DONE', 5, 'Completed', 0, interval '100 minutes'),
    ('HOLD', 3, 'Ready', 0, interval '90 minutes'),
    ('SCRAP', 4, 'Ready', 1, interval '140 minutes'),
    ('PLAN', 4, 'Ready', 0, interval '80 minutes'),
    ('QUEUE', 2, 'Ready', 0, interval '70 minutes'),
    ('RETRY', 2, 'Ready', 0, interval '3 hours'),
    ('FAIL', 2, 'AwaitingDisposition', 0, interval '1 day 5 hours')
) v(key, qty, status, scrap, age);

CREATE TEMP TABLE demo_order_inputs ON COMMIT DROP AS
SELECT d.*, jsonb_set(jsonb_set(c.process_snapshot, '{plan}', jsonb_build_object(
        'orderNo', d.order_no, 'plannedQty', d.qty, 'priority', 65, 'dueDate', c.today + 7
    )), '{recipeApplicabilities}', c.process_snapshot->'recipeApplicabilities' || (
        SELECT jsonb_agg(jsonb_build_object('id', s.applicability_id, 'routeStepId', s.route_step_id,
            'recipeVersionId', s.recipe_version_id, 'equipmentId', pg_temp.demo_id('history-tool'),
            'effectiveFrom', c.anchor - interval '4 days', 'effectiveTo', NULL,
            'createdAt', c.anchor - interval '4 days', 'updatedAt', c.anchor, 'rowVersion', 0))
        FROM demo_steps s
    )) || jsonb_build_object('generatedAt', d.base, 'userId', c.admin_id) AS snapshot
FROM demo_lots d CROSS JOIN demo_context c;

INSERT INTO production.work_orders
    (id, order_no, product_id, product_revision_id, fab_id, planned_qty, produced_qty, status,
     priority, due_date, process_snapshot, snapshot_version, completed_at, description, created_at, updated_at)
SELECT d.order_id, d.order_no, c.product_id, c.product_revision_id, c.fab_id, d.qty,
    CASE WHEN d.status = 'Completed' THEN d.qty ELSE 0 END,
    'Released',
    65, c.today + 7, d.snapshot, 1, CASE WHEN d.status = 'Completed' THEN d.base + interval '61 minutes' END,
    '虚构晶圆制造示例', d.base - interval '10 minutes', c.anchor
FROM demo_order_inputs d CROSS JOIN demo_context c;

INSERT INTO production.work_order_snapshots
    (id, work_order_id, version, snapshot, effective_from, created_by_id, created_at, updated_at)
SELECT pg_temp.demo_id('snapshot-' || d.key), d.order_id, 1, d.snapshot, d.base, c.admin_id, d.base, d.base
FROM demo_order_inputs d CROSS JOIN demo_context c;

INSERT INTO production.lots
    (id, lot_number, lot_type, status, priority, target_qty, produced_qty, work_order_id,
     fab_id, product_revision_id, route_version_id, snapshot_version, current_route_step_id,
     lot_assigned_at, released_to_route_at, released_to_route_by_id, started_at, completed_at, created_at, updated_at)
SELECT d.lot_id, d.lot_no, 'Production', d.status, 65, d.qty,
    CASE WHEN d.status = 'Completed' THEN d.qty ELSE 0 END, d.order_id,
    c.fab_id, c.product_revision_id, c.route_version_id, 1,
    CASE WHEN d.status <> 'Completed' THEN s.route_step_id END, d.base, d.base + interval '1 minute',
    c.admin_id, CASE WHEN d.status <> 'Ready' OR d.key = 'RETRY' THEN d.base + interval '8 minutes' END,
    CASE WHEN d.status = 'Completed' THEN d.base + interval '61 minutes' END, d.base, c.anchor
FROM demo_lots d CROSS JOIN demo_context c JOIN demo_steps s ON s.sequence_no = 10;

CREATE TEMP TABLE demo_wafers ON COMMIT DROP AS
SELECT d.*, n, pg_temp.demo_id('wafer-' || d.key || '-' || n) AS wafer_id,
    'DEMO-FAB-' || d.key || '-W' || lpad(n::text, 2, '0') AS wafer_code,
    CASE WHEN n = 1 AND d.scrap > 0 THEN 'Scrapped'
        WHEN d.status = 'Completed' THEN 'Completed' ELSE 'Active' END AS lifecycle
FROM demo_lots d CROSS JOIN LATERAL generate_series(1, d.qty) n;

INSERT INTO production.wafers
    (id, wafer_code, wafer_size_mm, fab_id, work_order_id, lifecycle_status, produced_at,
     origin_material_lot_id, supplier_wafer_code, created_at, updated_at)
SELECT d.wafer_id, d.wafer_code, 300, c.fab_id, d.order_id, d.lifecycle,
    CASE WHEN d.lifecycle = 'Completed' THEN d.base + interval '61 minutes' END,
    pg_temp.demo_id('incoming'), 'SUP-' || d.wafer_code, d.base, c.anchor
FROM demo_wafers d CROSS JOIN demo_context c;

INSERT INTO production.genealogy_events
    (id, event_type, idempotency_key, correlation_id, performed_by_id, reason, occurred_at, created_at, updated_at)
SELECT pg_temp.demo_id('create-' || d.key), 'Create', 'demo-fab-create-' || d.key,
    pg_temp.demo_id('correlation-create-' || d.key), c.admin_id, '演示工单建批', d.base, d.base, d.base
FROM demo_lots d CROSS JOIN demo_context c;

INSERT INTO production.genealogy_details (id, event_id, to_lot_id, wafer_id, created_at, updated_at)
SELECT pg_temp.demo_id('genealogy-' || d.wafer_code), pg_temp.demo_id('create-' || d.key),
    d.lot_id, d.wafer_id, d.base, d.base FROM demo_wafers d;

INSERT INTO production.lot_memberships (id, lot_id, wafer_id, change_event_id, valid_from, valid_to, created_at, updated_at)
SELECT pg_temp.demo_id('membership-' || d.wafer_code), d.lot_id, d.wafer_id,
    pg_temp.demo_id('create-' || d.key), d.base,
    CASE WHEN d.lifecycle = 'Scrapped' THEN d.base + interval '36 minutes' END, d.base, c.anchor
FROM demo_wafers d CROSS JOIN demo_context c;

INSERT INTO production.wafer_releases
    (id, wafer_id, work_order_id, initial_lot_id, route_version_id, snapshot_version, released_at, released_by_id, command_id)
SELECT pg_temp.demo_id('released-' || d.wafer_code), d.wafer_id, d.order_id, d.lot_id,
    c.route_version_id, 1, d.base + interval '1 minute', c.admin_id, pg_temp.demo_id('release-' || d.key)
FROM demo_wafers d CROSS JOIN demo_context c;

CREATE TEMP TABLE demo_visits ON COMMIT DROP AS
SELECT row.*, pg_temp.demo_id('visit-' || key || '-' || sequence_no || '-' || visit_no) AS step_id,
    pg_temp.demo_id('run-' || key || '-' || sequence_no || '-' || visit_no) AS run_id
FROM (
    SELECT d.*, s.*, v.visit_no,
        d.base + make_interval(mins => 2 + 20 * (s.sequence_no / 10 - 1) + 24 * (v.visit_no - 1)) AS entered,
        d.base + make_interval(mins => 8 + 20 * (s.sequence_no / 10 - 1)) AS track_in,
        d.base + make_interval(mins => 21 + 20 * (s.sequence_no / 10 - 1)) AS track_out,
        (d.status = 'Completed' OR d.key IN ('RETRY', 'FAIL') AND v.visit_no = 1) AS has_run
    FROM demo_lots d CROSS JOIN demo_steps s
    CROSS JOIN LATERAL generate_series(1, CASE WHEN d.key = 'RETRY' THEN 2 ELSE 1 END) v(visit_no)
    WHERE d.status = 'Completed' OR s.sequence_no = 10
) row;

INSERT INTO production.step_executions
    (id, lot_id, route_step_id, route_version_id, visit_no, status, name, instruction, requires_confirmation,
     entered_at, exited_at, passed_qty, scrap_qty, assigned_equipment_id, assigned_recipe_version_id, assigned_by_id,
     assigned_at, confirmed_at, confirmed_by_id, created_at, updated_at)
SELECT v.step_id, v.lot_id, v.route_step_id, c.route_version_id, v.visit_no,
    CASE WHEN v.status = 'Completed' THEN 'Completed' WHEN v.key = 'FAIL' THEN 'AwaitingDisposition'
        WHEN v.key = 'RETRY' AND v.visit_no = 1 THEN 'RetryAuthorized' ELSE 'Waiting' END,
    v.name, v.instruction, v.requires_confirmation, v.entered,
    CASE WHEN v.status = 'Completed' THEN v.track_out
        WHEN v.key = 'RETRY' AND v.visit_no = 1 THEN v.base + interval '26 minutes' END,
    CASE WHEN v.status = 'Completed' THEN v.qty WHEN v.has_run AND v.key IN ('FAIL', 'RETRY') THEN 1 ELSE 0 END, v.scrap,
    CASE WHEN v.has_run THEN pg_temp.demo_id('history-tool') END,
    CASE WHEN v.has_run THEN v.recipe_version_id END, CASE WHEN v.has_run THEN c.admin_id END,
    CASE WHEN v.has_run THEN v.entered END,
    CASE WHEN v.has_run THEN v.track_in + interval '1 minute' END,
    CASE WHEN v.has_run THEN c.admin_id END, v.entered, c.anchor
FROM demo_visits v CROSS JOIN demo_context c;

INSERT INTO production.process_runs
    (id, step_execution_id, equipment_id, recipe_version_id, recipe_applicability_id,
     status, track_in_at, track_in_by_id, track_out_at, track_out_by_id, created_at, updated_at)
SELECT v.run_id, v.step_id, pg_temp.demo_id('history-tool'), v.recipe_version_id, v.applicability_id, 'Completed',
    v.track_in, c.admin_id, v.track_out, c.admin_id, v.track_in, v.track_out
FROM demo_visits v CROSS JOIN demo_context c WHERE v.has_run;

INSERT INTO production.process_run_wafers
    (id, process_run_id, wafer_id, operator_id, processed_at, result, notes, created_at, updated_at)
SELECT pg_temp.demo_id('participant-' || v.run_id || '-' || w.wafer_code), v.run_id, w.wafer_id, c.admin_id,
    v.track_out, CASE WHEN v.key IN ('RETRY', 'FAIL') AND w.n = 2 THEN 'Failed' ELSE 'Passed' END,
    '虚构逐片结果', v.track_in, v.track_out
FROM demo_visits v JOIN demo_wafers w ON w.lot_id = v.lot_id CROSS JOIN demo_context c WHERE v.has_run;

INSERT INTO production.track_events
    (id, process_run_id, event_type, occurred_at, recorded_at, source, actor_id,
     idempotency_key, correlation_id, payload, created_at, updated_at)
SELECT pg_temp.demo_id(e.event || '-' || v.run_id), v.run_id, e.event, e.at, e.at, 'DatabaseSeed', c.admin_id,
    'demo-fab-' || e.event || '-' || v.run_id, pg_temp.demo_id('correlation-' || e.event || '-' || v.run_id),
    CASE WHEN e.event = 'TrackIn' THEN jsonb_build_object('waferIds', (
        SELECT jsonb_agg(w.wafer_id ORDER BY w.n) FROM demo_wafers w WHERE w.lot_id = v.lot_id))
    ELSE jsonb_build_object('advance', v.status = 'Completed', 'wafers', (
        SELECT jsonb_agg(jsonb_build_object('waferId', w.wafer_id, 'result', p.result) ORDER BY w.n)
        FROM demo_wafers w JOIN production.process_run_wafers p ON p.wafer_id = w.wafer_id
        WHERE p.process_run_id = v.run_id)) END,
    e.at, e.at
FROM demo_visits v CROSS JOIN demo_context c
CROSS JOIN LATERAL (VALUES ('TrackIn', v.track_in), ('TrackOut', v.track_out)) e(event, at)
WHERE v.has_run;

INSERT INTO production.checklist_completions
    (id, checklist_item_id, step_execution_id, checked_at, checked_by_id, created_at, updated_at)
SELECT pg_temp.demo_id('checklist-' || v.step_id || '-' || i.checklist_item_id), i.checklist_item_id,
    v.step_id, v.track_in + interval '1 minute', c.admin_id, v.track_in, v.track_in
FROM demo_visits v JOIN production.checklist_completions i ON i.step_execution_id = v.example_step_id
CROSS JOIN demo_context c WHERE v.has_run;

INSERT INTO production.output_values
    (id, output_id, step_execution_id, value_number, value_boolean, value_text, value_date, recorded_at,
     recorded_by_id, created_at, updated_at)
SELECT pg_temp.demo_id('output-' || v.step_id || '-' || vo.output_id), vo.output_id, v.step_id,
    CASE WHEN def.key = 'wafer_count' THEN v.qty ELSE vo.value_number END, vo.value_boolean, vo.value_text,
    CASE WHEN vo.value_date IS NOT NULL THEN v.track_in + interval '1 minute' END,
    v.track_in + interval '1 minute', c.admin_id, v.track_in, v.track_in
FROM demo_visits v JOIN production.output_values vo ON vo.step_execution_id = v.example_step_id
JOIN engineering.output_definitions def ON def.id = vo.output_id CROSS JOIN demo_context c WHERE v.has_run;

INSERT INTO production.process_confirmations
    (id, lot_id, step_execution_id, confirmation_type, value, notes, confirmed_at, confirmed_by_id, created_at, updated_at)
SELECT pg_temp.demo_id('confirmed-' || v.step_id || '-' || p.id), v.lot_id, v.step_id, p.confirmation_type,
    p.value, '虚构点检确认', v.track_in + interval '1 minute', c.admin_id, v.track_in, v.track_in
FROM demo_visits v JOIN production.process_confirmations p ON p.step_execution_id = v.example_step_id
CROSS JOIN demo_context c WHERE v.has_run;

INSERT INTO production.step_documents
    (id, engineering_document_id, step_execution_id, name, reference, requires_validation,
     is_mandatory, validated_at, validated_by_id, created_at, updated_at)
SELECT pg_temp.demo_id('doc-' || v.step_id || '-' || doc.id), doc.engineering_document_id,
    v.step_id, doc.name, doc.reference, doc.requires_validation, doc.is_mandatory,
    CASE WHEN v.has_run THEN v.track_in + interval '1 minute' END, CASE WHEN v.has_run THEN c.admin_id END,
    v.entered, c.anchor
FROM demo_visits v JOIN production.step_documents doc ON doc.step_execution_id = v.example_step_id
CROSS JOIN demo_context c WHERE doc.engineering_document_id IS NOT NULL;

INSERT INTO production.wafer_completions
    (id, wafer_id, lot_id_at_completion, work_order_id, route_version_id, route_step_id,
     process_run_id, completed_at, completed_by_id, command_id)
SELECT pg_temp.demo_id('done-' || d.wafer_code), d.wafer_id, d.lot_id, d.order_id, c.route_version_id,
    v.route_step_id, v.run_id, v.track_out, c.admin_id, pg_temp.demo_id('complete-' || d.key)
FROM demo_wafers d JOIN demo_visits v ON v.lot_id = d.lot_id AND v.sequence_no = 30
CROSS JOIN demo_context c WHERE d.lifecycle = 'Completed';

-- Quality: out-of-spec measurement with open Hold, pass, partial scrap and same-step retry.
INSERT INTO quality.reason_codes (id, code, name, category, is_active, created_at, updated_at)
SELECT pg_temp.demo_id(v.key), v.code, v.label, v.category, true, c.anchor, c.anchor
FROM demo_context c CROSS JOIN (VALUES
    ('scrap-reason', 'DEMO-FAB-SCRAP', '演示外观缺陷', 'Scrap'),
    ('down-reason', 'DEMO-FAB-DOWN', '演示传感器复核', 'Downtime')
) v(key, code, label, category);

INSERT INTO quality.specification_versions
    (id, parameter_code, version, value_type, unit, spec_min, spec_max,
     lifecycle_status, released_by_id, released_at, created_at, updated_at)
SELECT pg_temp.demo_id('thickness-spec'), 'DEMO-FAB-THICKNESS', 1, 'decimal', 'nm', 95, 105,
    'Released', c.admin_id, c.anchor - interval '4 days', c.anchor - interval '4 days', c.anchor
FROM demo_context c;

INSERT INTO quality.quality_check_templates
    (id, name, route_version_id, version, parameters, min_checks_per_batch, samples_per_check,
     lifecycle_status, released_by_id, released_at, created_at, updated_at)
SELECT pg_temp.demo_id('inspection-template'), 'DEMO-FAB 膜厚抽检', c.route_version_id, 1,
    jsonb_build_array(pg_temp.demo_id('thickness-spec')), 1, 1, 'Released', c.admin_id,
    c.anchor - interval '4 days', c.anchor - interval '4 days', c.anchor
FROM demo_context c;

INSERT INTO quality.issues
    (id, issue_type_id, title, description, status, is_blocking, work_order_id, lot_id,
     step_execution_id, assigned_to_id, reported_by_id, reported_at, acknowledged_at, resolved_at, closed_at,
     non_conforming_qty, root_cause, containment_action, disposition, disposition_by_id, disposition_at,
     resolution_notes, created_at, updated_at)
SELECT pg_temp.demo_id('issue-' || d.key), t.id,
    CASE WHEN d.key = 'HOLD' THEN 'DEMO-FAB：膜厚抽检超上限' ELSE 'DEMO-FAB：片级失败，审核后同站重试' END,
    '虚构质量异常，用于展示处置链路',
    CASE WHEN d.key = 'HOLD' THEN 'Open' ELSE 'Closed' END, true, d.order_id, d.lot_id,
    v.step_id, c.admin_id, c.admin_id, d.base + interval '22 minutes',
    CASE WHEN d.key = 'RETRY' THEN d.base + interval '23 minutes' END,
    CASE WHEN d.key = 'RETRY' THEN d.base + interval '24 minutes' END,
    CASE WHEN d.key = 'RETRY' THEN d.base + interval '27 minutes' END,
    CASE WHEN d.key = 'HOLD' THEN d.qty ELSE 1 END,
    CASE WHEN d.key = 'RETRY' THEN '演示夹具定位偏差' END,
    CASE WHEN d.key = 'RETRY' THEN '隔离样片并复核夹具' END,
    CASE WHEN d.key = 'RETRY' THEN 'Retry' END,
    CASE WHEN d.key = 'RETRY' THEN c.admin_id END,
    CASE WHEN d.key = 'RETRY' THEN d.base + interval '24 minutes' END,
    CASE WHEN d.key = 'RETRY' THEN '批准同站重试' END,
    d.base + interval '22 minutes', c.anchor
FROM demo_lots d JOIN demo_visits v ON v.lot_id = d.lot_id AND v.visit_no = 1
CROSS JOIN demo_context c CROSS JOIN quality.issue_types t
WHERE d.key IN ('HOLD', 'RETRY') AND t.code = 'SYS-QUALITY-CHECK-FAILED';

INSERT INTO quality.issue_actions
    (id, issue_id, action_type, status, title, description, assigned_to_id, due_date,
     completed_at, completed_by_id, verified_at, verified_by_id, created_at, updated_at)
SELECT pg_temp.demo_id('action-' || d.key), pg_temp.demo_id('issue-' || d.key), 'Corrective',
    CASE WHEN d.key = 'HOLD' THEN 'Open' ELSE 'Verified' END,
    CASE WHEN d.key = 'HOLD' THEN '复核演示量测设备' ELSE '完成并复核夹具清洁点检' END,
    '演示整改记录', c.admin_id, c.today + 1,
    CASE WHEN d.key = 'RETRY' THEN d.base + interval '23 minutes 10 seconds' END,
    CASE WHEN d.key = 'RETRY' THEN c.admin_id END,
    CASE WHEN d.key = 'RETRY' THEN d.base + interval '23 minutes 30 seconds' END,
    CASE WHEN d.key = 'RETRY' THEN c.admin_id END, d.base + interval '23 minutes', c.anchor
FROM demo_lots d CROSS JOIN demo_context c WHERE d.key IN ('HOLD', 'RETRY');

INSERT INTO quality.hold_records
    (id, lot_id, issue_id, reason_code_id, hold_scope, held_at, held_by_id,
     released_at, released_by_id, release_reason, created_at, updated_at)
SELECT pg_temp.demo_id('hold-' || d.key), d.lot_id, pg_temp.demo_id('issue-' || d.key), r.id, 'Lot',
    d.base + interval '22 minutes', c.admin_id,
    CASE WHEN d.key = 'RETRY' THEN d.base + interval '25 minutes' END,
    CASE WHEN d.key = 'RETRY' THEN c.admin_id END,
    CASE WHEN d.key = 'RETRY' THEN '量测复核完成，解除冻结' END,
    d.base + interval '22 minutes', c.anchor
FROM demo_lots d CROSS JOIN demo_context c CROSS JOIN quality.reason_codes r
WHERE d.key IN ('HOLD', 'RETRY') AND r.code = 'SYS-QUALITY-CHECK-FAILED';

INSERT INTO quality.lot_releases
    (id, lot_id, issue_id, step_execution_id, process_run_id, release_type,
     recovery_action, reason, released_at, released_by_id, command_id, created_at, updated_at)
SELECT pg_temp.demo_id('quality-retry'), d.lot_id, pg_temp.demo_id('issue-RETRY'), v.step_id,
    v.run_id, 'Quality', 'Retry', '演示审核准予重新进入当前站', d.base + interval '26 minutes',
    c.admin_id, pg_temp.demo_id('quality-retry-command'), d.base + interval '26 minutes', c.anchor
FROM demo_lots d JOIN demo_visits v ON v.lot_id = d.lot_id AND v.visit_no = 1
CROSS JOIN demo_context c WHERE d.key = 'RETRY';

INSERT INTO quality.quality_checks
    (id, lot_id, quality_check_template_id, issue_id, production_quantity, all_passed,
     checked_at, checked_by_id, notes, created_at, updated_at)
SELECT pg_temp.demo_id('inspection-' || d.key), d.lot_id, pg_temp.demo_id('inspection-template'),
    CASE WHEN d.key = 'HOLD' THEN pg_temp.demo_id('issue-HOLD') END, d.qty, d.key <> 'HOLD',
    d.base + interval '22 minutes', c.admin_id, '虚构膜厚抽样', d.base + interval '22 minutes', c.anchor
FROM demo_lots d CROSS JOIN demo_context c WHERE d.key IN ('HOLD', 'QUEUE');

INSERT INTO quality.measurements
    (id, lot_id, quality_check_id, specification_version_id, parameter_code, parameter_type, sample_number,
     sample_point, value_numeric, unit, spec_min, spec_max, is_passed, recorded_by_id, measured_at, created_at, updated_at)
SELECT pg_temp.demo_id('measure-' || d.key), d.lot_id, pg_temp.demo_id('inspection-' || d.key),
    pg_temp.demo_id('thickness-spec'), 'DEMO-FAB-THICKNESS', 'decimal', 1, 'Center',
    CASE WHEN d.key = 'HOLD' THEN 108.5 ELSE 100.2 END, 'nm', 95, 105, d.key <> 'HOLD',
    c.admin_id, d.base + interval '22 minutes', d.base + interval '22 minutes', c.anchor
FROM demo_lots d CROSS JOIN demo_context c WHERE d.key IN ('HOLD', 'QUEUE');

INSERT INTO quality.scrap_entries
    (id, work_order_id, step_execution_id, reason_code_id, quantity, notes, reported_by_id,
     reported_at, created_at, updated_at)
SELECT pg_temp.demo_id('scrap-entry'), d.order_id, v.step_id, pg_temp.demo_id('scrap-reason'), 1,
    '演示外观复核后逐片报废', c.admin_id, d.base + interval '36 minutes', d.base + interval '36 minutes', c.anchor
FROM demo_lots d JOIN demo_visits v ON v.lot_id = d.lot_id CROSS JOIN demo_context c WHERE d.key = 'SCRAP';

INSERT INTO quality.wafer_dispositions
    (id, wafer_id, lot_id_at_event, disposition_type, reason_code_id, scrap_entry_id,
     approved_by_id, performed_by_id, occurred_at, created_at, updated_at)
SELECT pg_temp.demo_id('scrap-disposition'), w.wafer_id, w.lot_id, 'Scrap', pg_temp.demo_id('scrap-reason'),
    pg_temp.demo_id('scrap-entry'), c.admin_id, c.admin_id,
    w.base + interval '36 minutes', w.base + interval '36 minutes', c.anchor
FROM demo_wafers w CROSS JOIN demo_context c WHERE w.key = 'SCRAP' AND w.n = 1;

-- Equipment and resources.
INSERT INTO organization.shifts (id, code, name, fab_id, line_id, start_time, end_time, days_of_week,
    sort_order, is_active, created_at, updated_at)
SELECT pg_temp.demo_id('shift'), 'DEMO-FAB-SHIFT', '演示全日班', c.fab_id, c.line_id,
    '00:00', '23:59:59', '[1,2,3,4,5,6,7]', 10, true, c.anchor - interval '4 days', c.anchor
FROM demo_context c;

INSERT INTO equipment.chambers (id, equipment_id, code, name, is_active, created_at, updated_at)
SELECT pg_temp.demo_id('chamber'), pg_temp.demo_id('history-tool'), 'DEMO-FAB-CH-A', '演示 A 腔', true, c.anchor, c.anchor
FROM demo_context c;

INSERT INTO equipment.equipment_states (id, equipment_id, state, source, started_at, ended_at, created_at, updated_at)
SELECT pg_temp.demo_id('state-' || v.key), pg_temp.demo_id(v.tool), v.state, v.source,
    c.anchor - v.start_age, c.anchor - v.end_age, c.anchor - v.start_age, c.anchor
FROM demo_context c CROSS JOIN (VALUES
    ('old-idle', 'history-tool', 'Idle', 'Manual', interval '4 days', interval '1 day 10 hours'),
    ('closed-down', 'history-tool', 'Down', 'Manual', interval '1 day 10 hours', interval '1 day 9 hours'),
    ('current-idle', 'history-tool', 'Idle', 'Manual', interval '1 day 9 hours', NULL),
    ('sim-idle', 'sim-tool', 'Idle', 'Manual', interval '4 days', interval '45 minutes'),
    ('open-down', 'sim-tool', 'Down', 'Simulator', interval '45 minutes', NULL)
) v(key, tool, state, source, start_age, end_age);

INSERT INTO equipment.downtimes (id, equipment_id, equipment_state_id, line_id, shift_id, started_at, ended_at,
    reason_code_id, notes, needs_reason, reported_by_id, classified_by_id, classified_at, created_at, updated_at)
SELECT pg_temp.demo_id(v.key), e.equipment_id, e.id, c.line_id, pg_temp.demo_id('shift'),
    e.started_at, e.ended_at, pg_temp.demo_id(v.reason), v.note, v.reason IS NULL,
    c.admin_id, CASE WHEN v.reason IS NOT NULL THEN c.admin_id END, e.ended_at, e.started_at, c.anchor
FROM demo_context c CROSS JOIN (VALUES
    ('closed-down', 'down-reason', '演示传感器复核已归因'),
    ('open-down', NULL, '演示连接报文触发停机，待归因')
) v(key, reason, note)
JOIN equipment.equipment_states e ON e.id = pg_temp.demo_id('state-' || v.key);

INSERT INTO equipment.tools (id, fab_id, equipment_id, equipment_type_id, code, name, status, is_active,
    next_service_at, created_at, updated_at)
SELECT pg_temp.demo_id('resource-tool'), c.fab_id, pg_temp.demo_id('history-tool'), c.equipment_type_id,
    'DEMO-FAB-GAUGE', '演示量测校验工具', 'Available', true, c.anchor + interval '7 days',
    c.anchor - interval '4 days', c.anchor
FROM demo_context c;

INSERT INTO equipment.maintenance_schedules (id, equipment_id, event_type, frequency, interval_value,
    is_active, name, next_due_at, last_executed_at, assigned_to_id, created_at, updated_at)
SELECT pg_temp.demo_id(v.key), pg_temp.demo_id(v.tool), 'Preventive', 'Days', 7,
    true, v.name, c.anchor + v.next_due, CASE WHEN v.key = 'pm-cell' THEN c.anchor - interval '1 day' END,
    c.admin_id, c.anchor - interval '4 days', c.anchor
FROM demo_context c CROSS JOIN (VALUES
    ('pm-cell', 'history-tool', 'DEMO-FAB 工艺机台周保养', interval '6 days'),
    ('pm-sim', 'sim-tool', 'DEMO-FAB 模拟机台到期保养', interval '-1 hour')
) v(key, tool, name, next_due);

INSERT INTO equipment.maintenance_events (id, schedule_id, equipment_id, event_type, status, title, assigned_to_id,
    scheduled_at, started_at, completed_at, released_at, released_by_id, resolution_notes, created_at, updated_at)
SELECT pg_temp.demo_id(v.key), pg_temp.demo_id(v.plan), pg_temp.demo_id(v.tool), 'Preventive', v.status, v.label,
    c.admin_id, c.anchor - v.age,
    CASE WHEN v.status = 'Released' THEN c.anchor - interval '1 day 10 hours' END,
    CASE WHEN v.status = 'Released' THEN c.anchor - interval '1 day 9 hours 5 minutes' END,
    CASE WHEN v.status = 'Released' THEN c.anchor - interval '1 day 9 hours' END,
    CASE WHEN v.status = 'Released' THEN c.admin_id END,
    CASE WHEN v.status = 'Released' THEN '演示维护完成并复机' END, c.anchor - v.age, c.anchor
FROM demo_context c CROSS JOIN (VALUES
    ('pm-history', 'pm-cell', 'history-tool', 'Released', 'DEMO-FAB 工艺机台保养已复机', interval '1 day 11 hours'),
    ('pm-due', 'pm-sim', 'sim-tool', 'Planned', 'DEMO-FAB 模拟机台待维护', interval '1 hour')
) v(key, plan, tool, status, label, age);

INSERT INTO equipment.equipment_qualifications (id, equipment_id, operation_id, recipe_version_id,
    qualification_status, qualified_by_id, valid_from, valid_to, created_at, updated_at)
SELECT pg_temp.demo_id('equipment-qual'), pg_temp.demo_id('history-tool'), s.operation_id, s.recipe_version_id,
    'Qualified', c.admin_id, c.anchor - interval '4 days', c.anchor + interval '20 days', c.anchor, c.anchor
FROM demo_steps s CROSS JOIN demo_context c WHERE s.sequence_no = 10;

INSERT INTO equipment.worker_qualifications (id, worker_id, operation_id, recipe_version_id,
    qualification_status, qualified_by_id, valid_from, valid_to, created_at, updated_at)
SELECT pg_temp.demo_id('worker-qual'), w.id, s.operation_id, s.recipe_version_id,
    'Qualified', c.admin_id, c.anchor - interval '4 days', c.anchor + interval '20 days', c.anchor, c.anchor
FROM demo_steps s CROSS JOIN demo_context c
JOIN LATERAL (SELECT id FROM organization.workers WHERE fab_id = c.fab_id AND is_active
    AND code LIKE 'DEMO-%' ORDER BY code LIMIT 1) w ON true
WHERE s.sequence_no = 10;

-- The enabled gate is scoped to a dedicated stand-alone operation.
INSERT INTO engineering.operations (id, code, name, segment_type, equipment_type_id, is_active, created_at, updated_at)
SELECT pg_temp.demo_id('qualification-op'), 'DEMO-FAB-QUAL', '演示资格与资源门禁', 'Process',
    c.equipment_type_id, true, c.anchor, c.anchor FROM demo_context c;

INSERT INTO equipment.operation_policies (id, operation_id, require_equipment_qualification,
    require_worker_qualification, required_reticle_layer, created_at, updated_at)
SELECT pg_temp.demo_id('qualification-policy'), pg_temp.demo_id('qualification-op'),
    true, true, 'DEMO-L1', c.anchor, c.anchor FROM demo_context c;

INSERT INTO equipment.material_requirements (id, operation_id, material_id, created_at, updated_at)
SELECT pg_temp.demo_id('material-requirement'), pg_temp.demo_id('qualification-op'), pg_temp.demo_id('consumable'),
    c.anchor, c.anchor FROM demo_context c;

INSERT INTO equipment.reticles (id, fab_id, code, name, revision, layer_code, status, is_active,
    max_usage_count, usage_count, usable_until, created_at, updated_at)
SELECT pg_temp.demo_id(v.key), c.fab_id, v.code, v.label, 'A', 'DEMO-L1', 'Available', true,
    10, v.used, c.anchor + interval '15 days', c.anchor - interval '4 days', c.anchor
FROM demo_context c CROSS JOIN (VALUES
    ('reticle', 'DEMO-FAB-R01', '演示在用光罩', 3),
    ('reticle-spent', 'DEMO-FAB-R02', '演示寿命耗尽光罩', 10)
) v(key, code, label, used);

INSERT INTO equipment.reticle_mounts (id, equipment_id, reticle_id, loaded_at, created_at, updated_at)
SELECT pg_temp.demo_id('reticle-mount'), pg_temp.demo_id('history-tool'), pg_temp.demo_id('reticle'),
    c.anchor - interval '4 days', c.anchor - interval '4 days', c.anchor FROM demo_context c;

INSERT INTO equipment.reticle_usages (id, process_run_id, reticle_id, reticle_mount_id, loaded_at,
    unloaded_at, created_at, updated_at)
SELECT pg_temp.demo_id('reticle-run-' || v.key), v.run_id, pg_temp.demo_id('reticle'), pg_temp.demo_id('reticle-mount'),
    v.track_in, v.track_out, v.track_in, v.track_out
FROM demo_visits v WHERE v.sequence_no = 10 AND v.status = 'Completed';

INSERT INTO equipment.material_mounts (id, equipment_id, material_id, material_lot_id, mounted_at,
    opened_at, usable_until, unit, usage_quantity, created_at, updated_at)
SELECT pg_temp.demo_id('filter-mount'), pg_temp.demo_id('history-tool'), pg_temp.demo_id('consumable'),
    pg_temp.demo_id('filter-lot'), c.anchor - interval '4 days', c.anchor - interval '4 days',
    c.anchor + interval '10 days', 'ea', 0, c.anchor - interval '4 days', c.anchor FROM demo_context c;

INSERT INTO equipment.material_run_usages (id, material_mount_id, process_run_id, created_at, updated_at)
SELECT pg_temp.demo_id('filter-run-' || v.key), pg_temp.demo_id('filter-mount'), v.run_id, v.track_in, v.track_in
FROM demo_visits v WHERE v.sequence_no = 10 AND v.status = 'Completed';

-- The same processed wafer can be found by carrier, equipment, incoming lot and order.
INSERT INTO production.carriers (id, fab_id, carrier_code, carrier_type, slot_capacity, status,
    allowed_wafer_size_mm, created_at, updated_at)
SELECT pg_temp.demo_id('carrier'), c.fab_id, 'DEMO-FAB-FOUP', 'FOUP', 25, 'Available',
    300, c.anchor - interval '4 days', c.anchor FROM demo_context c;

INSERT INTO production.carrier_slots (id, carrier_id, slot_no, created_at, updated_at)
SELECT pg_temp.demo_id('slot-' || n), pg_temp.demo_id('carrier'), n, c.anchor - interval '4 days', c.anchor
FROM demo_context c CROSS JOIN generate_series(1, 25) n;

INSERT INTO production.carrier_loads (id, wafer_id, carrier_slot_id, loaded_at, unloaded_at, created_at, updated_at)
SELECT pg_temp.demo_id('load-' || w.wafer_code), w.wafer_id, pg_temp.demo_id('slot-' || (w.n + CASE WHEN w.key = 'DONE' THEN 5 ELSE 0 END)),
    w.base + interval '3 minutes', CASE WHEN w.key = 'DONE' THEN w.base + interval '70 minutes' END,
    w.base + interval '3 minutes', c.anchor
FROM demo_wafers w CROSS JOIN demo_context c WHERE w.key IN ('DONE', 'PLAN');

-- Workbench facts and user-scoped alert examples.
INSERT INTO workbench.equipment_ideal_rates (id, fab_id, equipment_id, effective_on, ideal_seconds)
SELECT pg_temp.demo_id('rate'), c.fab_id, pg_temp.demo_id('history-tool'), c.today - 4, 110 FROM demo_context c;

INSERT INTO workbench.production_schedule_blocks
    (id, fab_id, equipment_id, step_execution_id, starts_at, ends_at, cancelled_at)
SELECT pg_temp.demo_id('calendar-v2-' || v.key), c.fab_id, pg_temp.demo_id('history-tool'), v.step_id,
    (c.today + 1 + p.starts) AT TIME ZONE c.timezone,
    (c.today + 1 + p.ends) AT TIME ZONE c.timezone,
    CASE WHEN v.key = 'QUEUE' THEN c.anchor END
FROM demo_visits v CROSS JOIN demo_context c
JOIN (VALUES ('PLAN', time '09:00', time '10:00'), ('QUEUE', time '10:30', time '11:00')) p(key, starts, ends)
    ON p.key = v.key;

INSERT INTO workbench.subscriptions (id, fab_id, user_id, kind, enabled, enabled_at)
SELECT pg_temp.demo_id('subscription-' || k.kind), c.fab_id, c.admin_id, k.kind,
    true, c.anchor - interval '4 days'
FROM demo_context c CROSS JOIN (VALUES ('issue'), ('hold'), ('downtime'), ('disposition')) k(kind)
ON CONFLICT (fab_id, user_id, kind) DO NOTHING;

INSERT INTO workbench.alert_receipts (id, fab_id, user_id, kind, source_id, acknowledged_at, silenced_until)
SELECT pg_temp.demo_id(v.key), c.fab_id, c.admin_id, v.kind, pg_temp.demo_id(v.source),
    CASE WHEN v.kind = 'downtime' THEN c.anchor END,
    CASE WHEN v.kind = 'issue' THEN c.anchor + interval '8 hours' END
FROM demo_context c CROSS JOIN (VALUES
    ('receipt-down', 'downtime', 'open-down'), ('receipt-issue', 'issue', 'issue-HOLD')
) v(key, kind, source)
ON CONFLICT (fab_id, user_id, kind, source_id) DO NOTHING;

-- Simulator input and its accepted/rejected evidence.
INSERT INTO integration.machine_connections
    (id, equipment_id, name, protocol, status, is_active, last_connected_at, created_at, updated_at)
SELECT pg_temp.demo_id('sim-connection'), pg_temp.demo_id('sim-tool'), 'DEMO-FAB-SIM-CONN',
    'Simulator', 'Online', true, c.anchor - interval '55 minutes', c.anchor - interval '2 hours', c.anchor
FROM demo_context c;

CREATE TEMP TABLE demo_messages ON COMMIT DROP AS
SELECT v.*, c.anchor - v.age AS received,
    jsonb_build_object('kind', v.kind, 'state', v.state, 'reason', v.reason,
        'occurredAt', c.anchor - v.event_age) AS parsed, c.anchor - v.event_age AS occurred
FROM demo_context c CROSS JOIN (VALUES
    ('ONLINE', 'Connection', 'Unknown', 'Online', '演示连接上线',
        interval '55 minutes', interval '55 minutes', NULL),
    ('DOWN', 'State', 'Idle', 'Down', '演示传感器报警',
        interval '45 minutes', interval '45 minutes', NULL),
    ('STALE', 'State', 'Down', 'Idle', '演示乱序复机报文',
        interval '40 minutes', interval '50 minutes', '乱序或重复时点，不覆盖当前状态')
) v(key, kind, prior, state, reason, age, event_age, rejection);

INSERT INTO equipment.equipment_events
    (id, equipment_id, machine_connection_id, event_type, source, source_event_id, state_from, state_to,
     event_timestamp, received_at, is_simulated, processing_status, rejection_reason, correlation_id,
     payload, created_at, updated_at)
SELECT pg_temp.demo_id('event-' || m.key), pg_temp.demo_id('sim-tool'), pg_temp.demo_id('sim-connection'),
    m.kind, 'Simulator', 'DEMO-FAB-MSG-' || m.key, m.prior, m.state, m.occurred, m.received,
    true, CASE WHEN m.rejection IS NULL THEN 'Applied' ELSE 'Rejected' END,
    m.rejection, pg_temp.demo_id('corr-' || m.key), m.parsed, m.received, m.received
FROM demo_messages m;

INSERT INTO integration.machine_messages
    (id, machine_connection_id, source_message_id, topic, raw_payload, parsed_data, processing_status,
     processing_error, equipment_event_id, received_at, created_at, updated_at)
SELECT pg_temp.demo_id('message-' || m.key), pg_temp.demo_id('sim-connection'), 'DEMO-FAB-MSG-' || m.key,
    'simulator', m.parsed::text, m.parsed, e.processing_status, m.rejection, e.id, m.received, m.received, m.received
FROM demo_messages m JOIN equipment.equipment_events e ON e.id = pg_temp.demo_id('event-' || m.key);

-- CSV tasks are visible to the existing local admin because the list is actor-scoped.
INSERT INTO production.work_orders (id, fab_id, order_no, product_id, product_revision_id, planned_qty,
    produced_qty, status, priority, snapshot_version, description, created_at, updated_at)
SELECT pg_temp.demo_id('csv-order'), c.fab_id, 'DEMO-FAB-WO-CSV', c.product_id, c.product_revision_id,
    6, 0, 'Draft', 50, 0, '演示 CSV 已导入的草稿', c.anchor, c.anchor FROM demo_context c;

INSERT INTO integration.import_jobs (id, fab_id, user_id, entity, original_filename, status, dry_run,
    total_rows, successful_rows, failed_rows, input_data, error_log, result_data,
    started_at, completed_at, created_at, updated_at)
SELECT pg_temp.demo_id('csv-' || v.key), c.fab_id, c.admin_id, 'WorkOrder', 'demo-fab-' || v.key || '.csv',
    v.status, v.status <> 'Imported', 1, CASE WHEN v.status = 'Imported' THEN 1 ELSE 0 END,
    CASE WHEN v.status = 'Rejected' THEN 1 ELSE 0 END,
    jsonb_build_object('mapping', '{"orderNo":"单号","productCode":"产品","revisionCode":"修订","plannedQty":"数量"}'::jsonb,
        'rows', CASE WHEN v.status = 'Rejected' THEN '[]'::jsonb ELSE jsonb_build_array(jsonb_build_object(
            'row', 2, 'orderNo', v.order_no, 'productCode', p.code, 'revisionCode', rev.revision_code,
            'productId', c.product_id, 'productRevisionId', c.product_revision_id,
            'plannedQty', 6, 'priority', 50, 'dueDate', NULL, 'plannedStartAt', NULL,
            'plannedEndAt', NULL, 'description', '演示 CSV')) END),
    CASE WHEN v.status = 'Rejected' THEN '[{"row":2,"field":"plannedQty","message":"计划数量必须为正整数"}]'::jsonb END,
    CASE WHEN v.status = 'Imported' THEN jsonb_build_array(
        jsonb_build_object('row', 2, 'id', pg_temp.demo_id('csv-order'), 'orderNo', v.order_no)) END,
    c.anchor - v.age, c.anchor - v.age, c.anchor - v.age, c.anchor - v.age
FROM demo_context c JOIN engineering.products p ON c.product_id = p.id
JOIN engineering.product_revisions rev ON c.product_revision_id = rev.id
CROSS JOIN (VALUES
    ('imported', 'Imported', 'DEMO-FAB-WO-CSV', interval '3 minutes'),
    ('ready', 'Ready', 'DEMO-FAB-WO-CSV-NEXT', interval '2 minutes'),
    ('rejected', 'Rejected', NULL, interval '1 minute')
) v(key, status, order_no, age);

-- Audit examples explicitly identify their provenance. Outbox rows use the real refresh contract.
INSERT INTO audit.audit_logs (id, fab_id, user_id, module, entity_type, entity_id, action, source_channel,
    correlation_id, occurred_at, before_state, after_state, created_at, updated_at)
SELECT pg_temp.demo_id('audit-' || v.key), c.fab_id, c.admin_id, v.module, v.entity,
    pg_temp.demo_id(v.entity_key), v.action, 'DatabaseSeed', pg_temp.demo_id('corr-' || v.key),
    c.anchor - v.age, v.before, v.after, c.anchor - v.age, c.anchor - v.age
FROM demo_context c CROSS JOIN (VALUES
    ('DOWN', 'simulator', 'MachineMessage', 'message-DOWN', 'DemoMessageApplied',
        interval '45 minutes', NULL::jsonb, '{"sourceMessageId":"DEMO-FAB-MSG-DOWN","status":"Applied"}'::jsonb),
    ('quality-hold', 'quality', 'QualityCheck', 'inspection-HOLD', 'DemoQualityChecked',
        interval '68 minutes', NULL, '{"lotNumber":"DEMO-FAB-HOLD","allPassed":false}'),
    ('imported', 'imports', 'ImportJob', 'csv-imported', 'DemoWorkOrderCsvConfirmed',
        interval '3 minutes', '{"status":"Ready"}', '{"status":"Imported","successfulRows":1}')
) v(key, module, entity, entity_key, action, age, before, after);

INSERT INTO audit.audit_logs (id, fab_id, user_id, module, entity_type, entity_id, action, source_channel,
    correlation_id, occurred_at, after_state, created_at, updated_at)
SELECT pg_temp.demo_id('audit-calendar-' || v.key), c.fab_id, c.admin_id, 'workbench', 'ProductionScheduleBlock',
    p.id, CASE WHEN p.cancelled_at IS NULL THEN 'ScheduleCreated' ELSE 'ScheduleCancelled' END,
    'DatabaseSeed', pg_temp.demo_id('corr-calendar-' || v.key), c.anchor,
    jsonb_build_object('id', p.id, 'stepExecutionId', v.step_id, 'lotId', v.lot_id, 'lotNumber', v.lot_no,
        'step', v.name, 'equipmentId', p.equipment_id, 'startsAt', p.starts_at, 'endsAt', p.ends_at,
        'status', CASE WHEN p.cancelled_at IS NULL THEN 'Planned' ELSE 'Cancelled' END, 'etag', ''),
    c.anchor, c.anchor
FROM demo_visits v JOIN workbench.production_schedule_blocks p ON p.id = pg_temp.demo_id('calendar-v2-' || v.key)
CROSS JOIN demo_context c;

WITH events AS (
    INSERT INTO audit.business_events (id, event_type, entity_type, entity_id, user_id,
        payload, occurred_at, correlation_id, created_at, updated_at)
    SELECT pg_temp.demo_id('business-' || v.key), 'WorkbenchRefreshRequested', 'Fab', c.fab_id, c.admin_id,
        '{"schemaVersion":1,"sources":["execution.read"]}', c.anchor, pg_temp.demo_id('corr-' || v.key),
        c.anchor, c.anchor
    FROM demo_context c CROSS JOIN (VALUES ('outbox-delivered'), ('outbox-retry'), ('outbox-dead')) v(key)
    RETURNING *
)
INSERT INTO audit.outbox_messages (id, business_event_id, event_type, aggregate_type, aggregate_id, payload,
    occurred_at, correlation_id, published_at, next_attempt_at, dead_lettered_at, attempts,
    last_error, created_at, updated_at)
SELECT pg_temp.demo_id(v.key), e.id, e.event_type, e.entity_type, e.entity_id, e.payload,
    e.occurred_at, e.correlation_id, CASE WHEN v.key = 'outbox-delivered' THEN e.occurred_at END,
    CASE WHEN v.key = 'outbox-retry' THEN CURRENT_TIMESTAMP + interval '24 hours' END,
    CASE WHEN v.key = 'outbox-dead' THEN e.occurred_at END,
    v.attempts, CASE WHEN v.attempts > 1 THEN 'DeliveryFailed' END, e.occurred_at, e.occurred_at
FROM events e JOIN (VALUES ('outbox-delivered', 1), ('outbox-retry', 2), ('outbox-dead', 5)) v(key, attempts)
    ON e.id = pg_temp.demo_id('business-' || v.key);

DO $$
BEGIN
    IF (SELECT count(*) FROM production.wafer_completions c JOIN demo_wafers w ON w.wafer_id = c.wafer_id) <> 15
        OR (SELECT count(*) FROM production.wafer_releases r JOIN demo_wafers w ON w.wafer_id = r.wafer_id) <> 32
        OR (SELECT count(*) FROM quality.wafer_dispositions WHERE id = pg_temp.demo_id('scrap-disposition')) <> 1
        OR (SELECT count(*) FROM quality.hold_records WHERE id = pg_temp.demo_id('hold-HOLD') AND released_at IS NULL) <> 1 THEN
        RAISE EXCEPTION '演示数据的投放、完工、冻结或报废数量不符。';
    END IF;
END $$;

SELECT c.today AS fab_day, c.today + 1 AS calendar_day, c.today - 3 AS history_from
FROM demo_context c;
\if :{?demo_rollback}
    ROLLBACK;
\else
    COMMIT;
\endif
