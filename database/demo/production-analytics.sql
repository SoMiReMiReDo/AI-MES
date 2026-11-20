\set ON_ERROR_STOP on
BEGIN;
SET LOCAL TIME ZONE 'UTC';
SET LOCAL lock_timeout = '10s';
SELECT pg_advisory_xact_lock(78001001);

CREATE FUNCTION pg_temp.analytics_id(key text) RETURNS uuid LANGUAGE sql IMMUTABLE STRICT AS $$
    SELECT md5('mesagent-production-analytics-v1:' || key)::uuid;
$$;

CREATE TEMP TABLE context ON COMMIT DROP AS
SELECT w.fab_id, w.product_id, w.product_revision_id, l.route_version_id, l.id AS template_lot,
    w.process_snapshot, e.equipment_type_id, e.equipment_group_id, f.timezone, u.id AS operator_id,
    (CURRENT_TIMESTAMP AT TIME ZONE f.timezone)::date AS today,
    pg_temp.analytics_id('revision') AS second_revision, w.created_at AS template_created
FROM production.lots l
JOIN production.work_orders w ON w.id = l.work_order_id AND w.fab_id = l.fab_id
JOIN organization.fabs f ON f.id = w.fab_id
JOIN organization.areas a ON a.fab_id = w.fab_id AND a.code = 'DEMO-AREA'
JOIN equipment.equipment e ON e.area_id = a.id AND e.code = 'DEMO-TOOL-01'
JOIN security.user_fab_scopes scope ON scope.fab_id = w.fab_id
JOIN security.users u ON u.id = scope.user_id AND u.normalized_user_name = 'ADMIN' AND u.is_active
WHERE l.lot_number = 'DEMO-LOT-DONE' AND l.status = 'Completed' AND w.process_snapshot IS NOT NULL
    AND w.process_snapshot ? 'recipeApplicabilities' AND w.process_snapshot ? 'revision';

DO $$
BEGIN
    IF current_database() <> 'mesagent' OR (SELECT count(*) FROM context) <> 1 THEN
        RAISE EXCEPTION '需要本地 mesagent 开发库及唯一的 DEMO-LOT-DONE 基础样本';
    END IF;
END $$;

SELECT EXISTS (
    SELECT FROM organization.areas a JOIN context c ON c.fab_id = a.fab_id
    WHERE a.id = pg_temp.analytics_id('area')
) AS already_loaded \gset
\if :already_loaded
    \echo '生产分析示例数据已装载，保留原历史日期与记录。'
    ROLLBACK;
    \quit
\endif

CREATE TEMP TABLE stages ON COMMIT DROP AS
SELECT row_number() OVER (ORDER BY r.sequence_no)::int AS stage, r.id AS route_step, r.name, r.instruction,
    r.requires_confirmation, s.id AS template_step, pr.recipe_version_id
FROM context c JOIN production.step_executions s ON s.lot_id = c.template_lot
JOIN engineering.route_steps r ON r.id = s.route_step_id AND r.route_version_id = c.route_version_id
JOIN production.process_runs pr ON pr.step_execution_id = s.id AND pr.status = 'Completed';

DO $$
BEGIN
    IF (SELECT count(*) FROM stages) <> 3 OR (SELECT count(DISTINCT route_step) FROM stages) <> 3 THEN
        RAISE EXCEPTION '演示基线须有三个已完成站点';
    END IF;
END $$;

INSERT INTO organization.areas (id, fab_id, code, name, is_active, created_at, updated_at)
SELECT pg_temp.analytics_id('area'), fab_id, 'DEMO-AN-AREA', '分析演示区', true, today - 29, today - 29
FROM context;

CREATE TEMP TABLE lines ON COMMIT DROP AS
SELECT n, pg_temp.analytics_id('line-' || n) AS id,
    CASE n WHEN 1 THEN c.product_revision_id ELSE c.second_revision END AS revision_id,
    CASE n WHEN 1 THEN 'A' ELSE 'B' END AS suffix
FROM context c CROSS JOIN generate_series(1, 2) n;

INSERT INTO organization.lines (id, area_id, code, name, is_active, created_at, updated_at)
SELECT l.id, pg_temp.analytics_id('area'), 'DEMO-AN-LINE-' || l.suffix, '分析演示线 ' || l.suffix,
    true, c.today - 29, c.today - 29 FROM lines l CROSS JOIN context c;

INSERT INTO engineering.product_revisions
    (id, product_id, revision_code, description, lifecycle_status, route_version_id,
     effective_from, released_at, released_by_id, created_at, updated_at)
SELECT second_revision, product_id, 'DEMO-AN-B', '虚构分析样本修订', 'Released', route_version_id,
    today - 29, today - 29, operator_id, today - 29, today - 29 FROM context;

CREATE TEMP TABLE tools ON COMMIT DROP AS
SELECT l.*, s.*, pg_temp.analytics_id('tool-' || l.suffix || s.stage) AS tool_id,
    pg_temp.analytics_id('app-' || l.suffix || s.stage) AS application_id
FROM lines l CROSS JOIN stages s;

INSERT INTO equipment.equipment
    (id, code, name, area_id, line_id, equipment_type_id, equipment_group_id,
     operational_state, communication_state, is_active, created_at, updated_at)
SELECT t.tool_id, 'DEMO-AN-' || t.suffix || t.stage, '演示线 ' || t.suffix || ' · ' || t.name,
    pg_temp.analytics_id('area'), t.id, c.equipment_type_id, c.equipment_group_id, 'Idle', 'Unknown', true,
    c.today - 29, c.today - 29
FROM tools t CROSS JOIN context c;

INSERT INTO engineering.recipe_applicabilities
    (id, route_step_id, recipe_version_id, equipment_id, effective_from, created_at, updated_at)
SELECT t.application_id, t.route_step, t.recipe_version_id, t.tool_id,
    c.today - 29, c.today - 29, c.today - 29
FROM tools t CROSS JOIN context c;

CREATE TEMP TABLE shifts ON COMMIT DROP AS
SELECT l.id AS line_id, l.suffix, s.number, s.code, s.name, s.starts_at, s.ends_at,
    pg_temp.analytics_id('shift-' || l.suffix || s.code) AS id
FROM lines l CROSS JOIN (VALUES
    (0, 'DAY', '白班', time '06:00', time '18:00'),
    (1, 'NIGHT', '夜班', time '18:00', time '06:00')
) s(number, code, name, starts_at, ends_at);

INSERT INTO organization.shifts
    (id, fab_id, line_id, code, name, start_time, end_time, days_of_week, sort_order, is_active, created_at, updated_at)
SELECT s.id, c.fab_id, s.line_id, 'DEMO-AN-' || s.suffix || '-' || s.code,
    s.suffix || ' 线' || s.name, s.starts_at, s.ends_at, '[1,2,3,4,5,6,7]', s.number, true, c.today - 29, c.today - 29
FROM shifts s CROSS JOIN context c;

INSERT INTO workbench.equipment_ideal_rates (id, fab_id, equipment_id, effective_on, ideal_seconds)
SELECT pg_temp.analytics_id('rate-' || t.tool_id || '-' || v.age), c.fab_id, t.tool_id, c.today - v.age,
    198 + (t.stage - 1) * 9 + (t.n - 1) * 7 + v.adjustment
FROM tools t CROSS JOIN context c
CROSS JOIN LATERAL (VALUES (CASE WHEN t.suffix = 'B' AND t.stage = 3 THEN 24 ELSE 28 END, 0), (12, -10))
    v(age, adjustment);

INSERT INTO quality.reason_codes (id, code, name, category, is_active, created_at, updated_at)
SELECT pg_temp.analytics_id(v.code), v.code, v.name, v.category, true, c.today - 29, c.today - 29
FROM context c CROSS JOIN (VALUES
    ('DEMO-AN-S1', '颗粒污染', 'Scrap'),
    ('DEMO-AN-S2', '膜厚超限', 'Scrap'),
    ('DEMO-AN-S3', '边缘损伤', 'Scrap'),
    ('DEMO-AN-S4', '边缘损伤', 'Scrap'),
    ('DEMO-AN-D1', '真空复核', 'Downtime'),
    ('DEMO-AN-D2', '计划维护', 'Downtime'),
    ('DEMO-AN-D3', '传送卡滞', 'Downtime')
) v(code, name, category);

CREATE TEMP TABLE slots ON COMMIT DROP AS
SELECT c.today - back AS day, back, s.id AS shift_id, s.suffix, s.line_id, s.number,
    ((c.today - back) + s.starts_at) AT TIME ZONE c.timezone AS shift_start,
    slot.n, l.revision_id, 'DEMO-AN-' || to_char(c.today - back, 'YYYYMMDD') || '-' || s.suffix || s.number || slot.n AS key,
    back * 17 + l.n * 31 + s.number * 7 + slot.n * 13 AS pattern
FROM context c CROSS JOIN generate_series(1, 28) back CROSS JOIN shifts s
JOIN lines l ON l.id = s.line_id CROSS JOIN generate_series(0, 3) slot(n);

CREATE TEMP TABLE lots ON COMMIT DROP AS
SELECT s.*, pg_temp.analytics_id('lot-' || key) AS lot_id, pg_temp.analytics_id('wo-' || key) AS order_id,
    shift_start + make_interval(mins => 6 + n * 145) AS lot_start,
    19 + pattern % 7 AS qty,
    CASE WHEN pattern % 19 = 0 THEN 0 WHEN pattern % 7 = 0 THEN 1 + pattern % 3 ELSE -1 END AS scrap_stage,
    CASE WHEN pattern % 19 = 0 OR pattern % 7 = 0 THEN 1 + pattern % 3 ELSE 0 END AS scraps,
    CASE WHEN pattern % 19 = 0 THEN 'DEMO-AN-S4'
        WHEN pattern % 4 < 2 THEN 'DEMO-AN-S1'
        WHEN pattern % 4 = 2 THEN 'DEMO-AN-S2' ELSE 'DEMO-AN-S3' END AS reason
FROM slots s;

CREATE TEMP TABLE timing ON COMMIT DROP AS
SELECT l.*, t.stage, t.tool_id, t.route_step, t.recipe_version_id, t.application_id,
    t.name, t.instruction, t.requires_confirmation, t.template_step,
    pg_temp.analytics_id('step-' || l.key || '-' || t.stage) AS step_id,
    pg_temp.analytics_id('run-' || l.key || '-' || t.stage) AS run_id,
    l.lot_start + make_interval(mins => (t.stage - 1) * 120) AS track_in,
    l.lot_start + make_interval(mins => (t.stage - 1) * 120 + 72 + (l.pattern + 11 * t.stage) % 22
        + CASE WHEN l.suffix = 'A' AND t.stage = 2 AND l.back BETWEEN 3 AND 5 THEN 18 ELSE 0 END
        + CASE WHEN l.suffix = 'B' AND t.stage = 3 AND l.back IN (2, 6, 10) THEN 18 ELSE 0 END) AS track_out
FROM lots l JOIN tools t ON t.suffix = l.suffix;

CREATE TEMP TABLE visits ON COMMIT DROP AS
SELECT t.*, coalesce(lag(track_out) OVER (PARTITION BY lot_id ORDER BY stage), lot_start - interval '4 minutes') AS entered,
    track_out + CASE WHEN stage = scrap_stage THEN interval '5 minutes' ELSE interval '0' END AS exited,
    CASE WHEN stage = scrap_stage THEN track_out + interval '3 minutes'
        WHEN scrap_stage = 0 AND stage = 1 THEN track_in - interval '1 minute' END AS scrap_at
FROM timing t;

CREATE TEMP TABLE order_inputs ON COMMIT DROP AS
SELECT l.*, v.exited AS finished,
    CASE WHEN l.pattern % 11 = 0 THEN NULL WHEN l.pattern % 5 = 0 THEN l.day - 1 ELSE l.day + 1 END AS due,
    CASE WHEN l.scraps > 0 OR l.pattern % 3 = 0 THEN 'Closed' ELSE 'Released' END AS order_status,
    c.process_snapshot || jsonb_build_object('generatedAt', l.lot_start - interval '8 minutes', 'userId', c.operator_id,
        'plan', jsonb_build_object('orderNo', l.key, 'plannedQty', l.qty, 'priority', 55 + l.pattern % 25,
            'dueDate', CASE WHEN l.pattern % 11 = 0 THEN NULL WHEN l.pattern % 5 = 0 THEN l.day - 1 ELSE l.day + 1 END),
        'revision', c.process_snapshot->'revision' || jsonb_build_object('id', l.revision_id,
            'revisionCode', CASE l.suffix WHEN 'A' THEN c.process_snapshot->'revision'->>'revisionCode' ELSE 'DEMO-AN-B' END),
        'recipeApplicabilities', (SELECT jsonb_agg(jsonb_build_object('id', t.application_id, 'routeStepId', t.route_step,
            'equipmentId', t.tool_id, 'recipeVersionId', t.recipe_version_id, 'effectiveFrom', c.today - 29))
            FROM tools t WHERE t.suffix = l.suffix)) AS snapshot
FROM lots l JOIN visits v ON v.lot_id = l.lot_id AND v.stage = 3 CROSS JOIN context c;

INSERT INTO production.work_orders
    (id, order_no, product_id, product_revision_id, fab_id, planned_qty, produced_qty, status, priority, due_date,
     process_snapshot, snapshot_version, completed_at, description, created_at, updated_at)
SELECT o.order_id, o.key, c.product_id, o.revision_id, c.fab_id, o.qty, o.qty - o.scraps, o.order_status,
    55 + o.pattern % 25, o.due, o.snapshot, 1, CASE WHEN o.scraps = 0 THEN o.finished END,
    '虚构生产分析样本', o.lot_start - interval '12 minutes', o.finished + interval '8 minutes'
FROM order_inputs o CROSS JOIN context c;

INSERT INTO production.work_order_snapshots
    (id, work_order_id, version, snapshot, effective_from, created_by_id, created_at, updated_at)
SELECT pg_temp.analytics_id('snapshot-' || o.key), o.order_id, 1, o.snapshot, o.lot_start - interval '8 minutes',
    c.operator_id, o.lot_start - interval '8 minutes', o.lot_start - interval '8 minutes'
FROM order_inputs o CROSS JOIN context c;

INSERT INTO production.lots
    (id, lot_number, lot_type, status, priority, target_qty, produced_qty, work_order_id, fab_id, product_revision_id,
     route_version_id, snapshot_version, lot_assigned_at, released_to_route_at, released_to_route_by_id,
     started_at, completed_at, closed_at, close_reason, created_at, updated_at)
SELECT o.lot_id, o.key, 'Production', 'Completed', 55 + o.pattern % 25, o.qty, o.qty - o.scraps, o.order_id, c.fab_id,
    o.revision_id, c.route_version_id, 1, o.lot_start - interval '6 minutes', o.lot_start - interval '5 minutes',
    c.operator_id, o.lot_start, o.finished, o.finished, 'Completed', o.lot_start - interval '6 minutes', o.finished
FROM order_inputs o CROSS JOIN context c;

CREATE TEMP TABLE wafers ON COMMIT DROP AS
SELECT o.lot_id, o.order_id, o.key, i, o.finished, o.lot_start, i > o.qty - o.scraps AS scrapped, v.scrap_at,
    o.key || '-W' || lpad(i::text, 2, '0') AS code, pg_temp.analytics_id('wafer-' || o.key || '-' || i) AS id
FROM order_inputs o CROSS JOIN LATERAL generate_series(1, o.qty) i
LEFT JOIN visits v ON v.lot_id = o.lot_id AND v.scrap_at IS NOT NULL;

CREATE INDEX ON wafers(lot_id);

INSERT INTO production.wafers
    (id, wafer_code, wafer_size_mm, fab_id, work_order_id, lifecycle_status, produced_at, created_at, updated_at)
SELECT w.id, w.code, 300, c.fab_id, w.order_id, CASE WHEN w.scrapped THEN 'Scrapped' ELSE 'Completed' END,
    CASE WHEN NOT w.scrapped THEN w.finished END, w.lot_start - interval '6 minutes',
    coalesce(CASE WHEN w.scrapped THEN w.scrap_at END, w.finished)
FROM wafers w CROSS JOIN context c;

INSERT INTO production.genealogy_events
    (id, event_type, idempotency_key, correlation_id, performed_by_id, reason, occurred_at, created_at, updated_at)
SELECT pg_temp.analytics_id('create-' || key), 'Create', 'analytics-create-' || key,
    pg_temp.analytics_id('trace-create-' || key), c.operator_id, '示例建批',
    o.lot_start - interval '6 minutes', o.lot_start - interval '6 minutes', o.lot_start - interval '6 minutes'
FROM order_inputs o CROSS JOIN context c;

INSERT INTO production.genealogy_details (id, event_id, to_lot_id, wafer_id, created_at, updated_at)
SELECT pg_temp.analytics_id('genealogy-' || code), pg_temp.analytics_id('create-' || key),
    lot_id, id, lot_start - interval '6 minutes', lot_start - interval '6 minutes' FROM wafers;

INSERT INTO production.lot_memberships (id, lot_id, wafer_id, change_event_id, valid_from, valid_to, created_at, updated_at)
SELECT pg_temp.analytics_id('member-' || code), lot_id, id, pg_temp.analytics_id('create-' || key),
    lot_start - interval '6 minutes', CASE WHEN scrapped THEN scrap_at END,
    lot_start - interval '6 minutes', coalesce(CASE WHEN scrapped THEN scrap_at END, finished)
FROM wafers;

INSERT INTO production.wafer_releases
    (id, wafer_id, work_order_id, initial_lot_id, route_version_id, snapshot_version, released_at, released_by_id, command_id)
SELECT pg_temp.analytics_id('release-' || code), id, order_id, lot_id, c.route_version_id, 1,
    lot_start - interval '5 minutes', c.operator_id, pg_temp.analytics_id('release-command-' || key)
FROM wafers CROSS JOIN context c;

INSERT INTO production.step_executions
    (id, lot_id, route_step_id, route_version_id, visit_no, status, name, instruction, requires_confirmation,
     entered_at, exited_at, passed_qty, scrap_qty, assigned_equipment_id, assigned_recipe_version_id,
     assigned_by_id, assigned_at, confirmed_at, confirmed_by_id, created_at, updated_at)
SELECT v.step_id, v.lot_id, v.route_step, c.route_version_id, 1, 'Completed', v.name, v.instruction,
    v.requires_confirmation, v.entered, v.exited, v.qty - CASE WHEN v.stage >= v.scrap_stage THEN v.scraps ELSE 0 END,
    CASE WHEN v.stage = greatest(1, v.scrap_stage) THEN v.scraps ELSE 0 END, v.tool_id, v.recipe_version_id,
    c.operator_id, v.entered, v.track_in + interval '1 minute', c.operator_id, v.entered, v.exited
FROM visits v CROSS JOIN context c;

INSERT INTO production.process_runs
    (id, step_execution_id, equipment_id, recipe_version_id, recipe_applicability_id,
     status, track_in_at, track_in_by_id, track_out_at, track_out_by_id, created_at, updated_at)
SELECT v.run_id, v.step_id, v.tool_id, v.recipe_version_id, v.application_id, 'Completed',
    v.track_in, c.operator_id, v.track_out, c.operator_id, v.track_in, v.track_out
FROM visits v CROSS JOIN context c;

INSERT INTO production.process_run_wafers
    (id, process_run_id, wafer_id, operator_id, processed_at, result, notes, created_at, updated_at)
SELECT pg_temp.analytics_id('participant-' || v.run_id || '-' || w.id), v.run_id, w.id, c.operator_id,
    v.track_out, CASE WHEN w.scrapped AND v.stage = v.scrap_stage THEN 'Failed' ELSE 'Passed' END,
    '虚构逐片过站', v.track_in, v.track_out
FROM visits v JOIN wafers w ON w.lot_id = v.lot_id AND (NOT w.scrapped OR v.stage <= v.scrap_stage)
CROSS JOIN context c;

INSERT INTO production.track_events
    (id, process_run_id, event_type, occurred_at, recorded_at, source, actor_id, idempotency_key,
     correlation_id, payload, created_at, updated_at)
SELECT pg_temp.analytics_id(e.kind || '-' || v.run_id), v.run_id, e.kind, e.event_at, e.event_at,
    'DatabaseSeed', c.operator_id, 'analytics-' || e.kind || '-' || v.run_id,
    pg_temp.analytics_id('trace-' || e.kind || '-' || v.run_id),
    CASE WHEN e.kind = 'TrackIn' THEN jsonb_build_object('waferIds', p.wafer_ids)
        ELSE jsonb_build_object('advance', v.scrap_stage <> v.stage, 'wafers', p.results) END, e.event_at, e.event_at
FROM visits v CROSS JOIN context c
CROSS JOIN LATERAL (VALUES ('TrackIn', v.track_in), ('TrackOut', v.track_out)) e(kind, event_at)
CROSS JOIN LATERAL (SELECT jsonb_agg(x.wafer_id ORDER BY x.wafer_id) AS wafer_ids,
    jsonb_agg(jsonb_build_object('waferId', x.wafer_id, 'result', x.result) ORDER BY x.wafer_id) AS results
    FROM production.process_run_wafers x WHERE x.process_run_id = v.run_id) p;

INSERT INTO production.checklist_completions
    (id, checklist_item_id, step_execution_id, checked_at, checked_by_id, created_at, updated_at)
SELECT pg_temp.analytics_id('checklist-' || v.step_id || '-' || i.checklist_item_id), i.checklist_item_id,
    v.step_id, v.track_in + interval '1 minute', c.operator_id, v.track_in, v.track_in
FROM visits v JOIN production.checklist_completions i ON i.step_execution_id = v.template_step CROSS JOIN context c;

INSERT INTO production.output_values
    (id, output_id, step_execution_id, value_number, value_boolean, value_text, value_date, recorded_at,
     recorded_by_id, created_at, updated_at)
SELECT pg_temp.analytics_id('output-' || v.step_id || '-' || vo.output_id), vo.output_id, v.step_id,
    CASE WHEN def.key = 'wafer_count' THEN v.qty - CASE WHEN v.stage > v.scrap_stage THEN v.scraps ELSE 0 END
        ELSE vo.value_number END, vo.value_boolean, vo.value_text,
    CASE WHEN vo.value_date IS NOT NULL THEN v.track_in + interval '1 minute' END,
    v.track_in + interval '1 minute', c.operator_id, v.track_in, v.track_in
FROM visits v JOIN production.output_values vo ON vo.step_execution_id = v.template_step
JOIN engineering.output_definitions def ON def.id = vo.output_id CROSS JOIN context c;

INSERT INTO production.process_confirmations
    (id, lot_id, step_execution_id, confirmation_type, value, notes, confirmed_at, confirmed_by_id, created_at, updated_at)
SELECT pg_temp.analytics_id('confirm-' || v.step_id || '-' || p.id), v.lot_id, v.step_id, p.confirmation_type,
    p.value, '示例确认', v.track_in + interval '1 minute', c.operator_id, v.track_in, v.track_in
FROM visits v JOIN production.process_confirmations p ON p.step_execution_id = v.template_step CROSS JOIN context c;

INSERT INTO production.step_documents
    (id, engineering_document_id, step_execution_id, name, reference, requires_validation, is_mandatory,
     validated_at, validated_by_id, created_at, updated_at)
SELECT pg_temp.analytics_id('doc-' || v.step_id || '-' || doc.id), doc.engineering_document_id,
    v.step_id, doc.name, doc.reference, doc.requires_validation, doc.is_mandatory,
    v.track_in + interval '1 minute', c.operator_id, v.entered, v.exited
FROM visits v JOIN production.step_documents doc ON doc.step_execution_id = v.template_step
CROSS JOIN context c WHERE doc.engineering_document_id IS NOT NULL;

INSERT INTO quality.scrap_entries
    (id, work_order_id, step_execution_id, reason_code_id, quantity, notes, reported_by_id,
     reported_at, created_at, updated_at)
SELECT pg_temp.analytics_id('scrap-' || v.key), v.order_id, v.step_id, pg_temp.analytics_id(v.reason), v.scraps,
    '虚构质量复核与逐片报废', c.operator_id, v.scrap_at, v.scrap_at, v.scrap_at
FROM visits v CROSS JOIN context c WHERE v.scrap_at IS NOT NULL;

INSERT INTO quality.wafer_dispositions
    (id, wafer_id, lot_id_at_event, process_run_id, scrap_entry_id, disposition_type, reason_code_id,
     approved_by_id, performed_by_id, occurred_at, created_at, updated_at)
SELECT pg_temp.analytics_id('scrap-' || w.code), w.id, w.lot_id, CASE WHEN v.scrap_stage > 0 THEN v.run_id END,
    pg_temp.analytics_id('scrap-' || v.key), 'Scrap', pg_temp.analytics_id(v.reason), c.operator_id, c.operator_id,
    v.scrap_at, v.scrap_at, v.scrap_at
FROM wafers w JOIN visits v ON v.lot_id = w.lot_id AND v.scrap_at IS NOT NULL CROSS JOIN context c WHERE w.scrapped;

INSERT INTO quality.lot_releases
    (id, lot_id, step_execution_id, process_run_id, release_type, recovery_action, reason, released_at,
     released_by_id, command_id, created_at, updated_at)
SELECT pg_temp.analytics_id('quality-release-' || v.key), v.lot_id, v.step_id, v.run_id, 'Quality', 'Continue',
    '示例剔除失败片后继续通过片', v.exited, c.operator_id, pg_temp.analytics_id('quality-command-' || v.key),
    v.exited, v.exited FROM visits v CROSS JOIN context c WHERE v.scrap_at IS NOT NULL AND v.scrap_stage > 0;

INSERT INTO production.wafer_completions
    (id, wafer_id, lot_id_at_completion, work_order_id, process_run_id, route_version_id,
     route_step_id, completed_at, completed_by_id, command_id)
SELECT pg_temp.analytics_id('completed-' || w.code), w.id, w.lot_id, w.order_id, v.run_id, c.route_version_id,
    v.route_step, v.exited, c.operator_id, pg_temp.analytics_id('complete-command-' || v.key)
FROM wafers w JOIN visits v ON v.lot_id = w.lot_id AND v.stage = 3 CROSS JOIN context c WHERE NOT w.scrapped;

CREATE TEMP TABLE stops ON COMMIT DROP AS
SELECT pg_temp.analytics_id('down-' || s.day || '-' || t.suffix || t.stage || s.number) AS id,
    s.shift_id, s.line_id, t.tool_id, stop_at,
    stop_at + make_interval(mins => 16 + s.pattern % 22
        + CASE WHEN t.suffix = 'A' AND t.stage = 2 AND s.back BETWEEN 3 AND 5 THEN 14 ELSE 0 END) AS up_at,
    CASE WHEN t.suffix = 'A' AND t.stage = 2 AND s.back BETWEEN 3 AND 5 THEN 'DEMO-AN-D1'
        WHEN s.pattern % 2 = 0 THEN 'DEMO-AN-D2' ELSE 'DEMO-AN-D3' END AS reason
FROM slots s JOIN tools t ON t.suffix = s.suffix
CROSS JOIN LATERAL (SELECT s.shift_start + make_interval(mins =>
    CASE t.stage WHEN 1 THEN 580 WHEN 2 THEN 52 ELSE 174 END)) tm(stop_at)
WHERE s.n = 0 AND (s.pattern + t.stage) % 3 <> 0;

INSERT INTO equipment.equipment_states (id, equipment_id, state, source, started_at, ended_at, created_at, updated_at)
SELECT pg_temp.analytics_id('state-' || v.run_id), v.tool_id, 'Running', 'Manual',
    v.track_in, v.track_out, v.track_in, v.track_out FROM visits v
UNION ALL
SELECT id, tool_id, 'Down', 'Manual', stop_at, up_at, stop_at, up_at FROM stops
UNION ALL
SELECT pg_temp.analytics_id('idle-' || tool_id), tool_id, 'Idle', 'Manual', max(ended), NULL,
    max(ended), max(ended) FROM (
        SELECT tool_id, track_out AS ended FROM visits
        UNION ALL SELECT tool_id, up_at FROM stops
    ) timeline GROUP BY tool_id;

INSERT INTO equipment.downtimes
    (id, equipment_id, equipment_state_id, line_id, shift_id, started_at, ended_at, reason_code_id, needs_reason,
     notes, reported_by_id, classified_by_id, classified_at, created_at, updated_at)
SELECT pg_temp.analytics_id('stop-' || s.id), s.tool_id, s.id, s.line_id, s.shift_id, s.stop_at, s.up_at,
    pg_temp.analytics_id(s.reason), false, '虚构停机记录', c.operator_id, c.operator_id, s.up_at, s.stop_at, s.up_at
FROM stops s CROSS JOIN context c;

CREATE TEMP TABLE terminal_only ON COMMIT DROP AS
SELECT 'DEMO-AN-TERMINAL-' || i AS key, pg_temp.analytics_id('terminal-' || i) AS id, i,
    CASE WHEN i % 2 = 0 THEN 'Closed' ELSE 'Cancelled' END AS status,
    ((c.today - (i * 2)) + time '11:30') AT TIME ZONE c.timezone AS at
FROM context c CROSS JOIN generate_series(1, 10) i;

INSERT INTO production.work_orders
    (id, order_no, product_id, product_revision_id, fab_id, planned_qty, produced_qty, status,
     priority, snapshot_version, description, created_at, updated_at)
SELECT t.id, t.key, c.product_id, c.product_revision_id, c.fab_id, 20, 0, t.status, 50, 0,
    '虚构零产出终态工单', t.at - interval '1 day', t.at
FROM terminal_only t CROSS JOIN context c;

INSERT INTO audit.audit_logs
    (id, fab_id, user_id, module, entity_type, entity_id, action, source_channel, occurred_at, correlation_id,
     after_state, created_at, updated_at)
SELECT pg_temp.analytics_id('audit-' || q.id), c.fab_id, c.operator_id, 'planning', 'WorkOrder', q.id,
    CASE q.status WHEN 'Closed' THEN 'close' ELSE 'cancel' END, 'DatabaseSeed', q.at,
    pg_temp.analytics_id('audit-trace-' || q.id),
    jsonb_build_object('id', q.id, 'fabId', c.fab_id, 'status', q.status, 'description', '虚构示例'), q.at, q.at
FROM context c CROSS JOIN (
    SELECT order_id AS id, order_status AS status, finished + interval '8 minutes' AS at FROM order_inputs
    WHERE order_status = 'Closed'
    UNION ALL SELECT id, status, at FROM terminal_only WHERE i <> 10
) q;

DO $$
BEGIN
    IF (SELECT count(*) FROM order_inputs) <> 448 OR (SELECT count(*) FROM visits) <> 1344
        OR EXISTS (SELECT 1 FROM visits v JOIN visits w ON v.tool_id = w.tool_id AND v.run_id < w.run_id
            WHERE v.track_in < w.track_out AND w.track_in < v.track_out)
        OR EXISTS (SELECT 1 FROM stops s JOIN visits v ON s.tool_id = v.tool_id
            WHERE s.stop_at < v.track_out AND v.track_in < s.up_at)
        OR EXISTS (SELECT 1 FROM wafers w JOIN production.wafer_releases r ON r.wafer_id = w.id
            LEFT JOIN production.wafer_completions c ON c.wafer_id = w.id
            LEFT JOIN quality.wafer_dispositions d ON d.wafer_id = w.id
            WHERE (c.id IS NULL) = (d.id IS NULL)) THEN
        RAISE EXCEPTION '生产分析样本数量、时间窗或逐片结案不一致';
    END IF;
END $$;

SELECT (SELECT min(day) FROM slots) AS first_shift_date, (SELECT max(day) FROM slots) AS last_shift_date,
    (SELECT count(*) FROM order_inputs) + (SELECT count(*) FROM terminal_only) AS work_orders,
    (SELECT count(*) FROM lots) AS lots, (SELECT count(*) FROM visits) AS runs, (SELECT count(*) FROM wafers) AS wafers,
    (SELECT count(*) FROM wafers WHERE NOT scrapped) AS completed, (SELECT count(*) FROM wafers WHERE scrapped) AS scrapped,
    (SELECT count(*) FROM stops) AS downtimes;

\if :{?commit_demo}
    \if :commit_demo
        COMMIT;
    \else
        ROLLBACK;
    \endif
\else
    ROLLBACK;
\endif
