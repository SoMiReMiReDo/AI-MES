# Fab 场景数据字段沿用与保留清单 v0.1

日期：2026-11-11｜状态：规划建议，待业务确认，未实现。

**实施更新：**用户随后已明确要求按本清单建立数据库，现已形成 [实际字段字典](05-database-catalog.md) 与 [数据库实施说明](06-database-implementation.md)。下文保留原始取舍依据；具体落地表名、类型和约束以当前 EF Core 模型及迁移为准，业务流程仍待实现。

## 1. 结论与适用范围

建议保留 OpenMES 的**身份权限、产品版本、工艺定义、生产履历、质量异常、物料追溯、设备事件和审计**数据语义；围绕 Lot / Wafer 重组执行模型，并补齐 Recipe、载具槽位、拆合批和 Hold 历史。

范围按**前道晶圆制造 Fab**理解，不默认包含封装、Die 级测试、完整仓储或商业财务。这里的“保留”是沿用字段表达的业务事实，不是照搬 Laravel 表结构或直接迁移生产数据。现有项目仍是 .NET 控制台骨架。

依据：

- 原始字段来自 [OpenMES 完整数据清单](/Users/seal/.codex/visualizations/2026/11/10/01a1c6de-e029-74f3-ba61-97ac09a09680/OpenMES-完整数据清单.md)，该文档标注基线为提交 `6048face`、132 张表、1,759 列。本次依据该清单筛选，未重新核验全部迁移、字段类型、约束和运行逻辑。
- 项目边界见 [半导体系统规划](02-system-plan.md)。单片追溯、完整血缘和配方版本管理也有 [Siemens 半导体 MES 官方说明](https://www.siemens.com/en-us/products/opcenter/execution/semiconductor/)作为行业方向参考；下文具体字段是本项目设计建议，不是标准强制字段。

| 标记 | 含义 |
| --- | --- |
| 核心保留 | 建议作为首版数据基础；不等于每条记录每个字段都必填 |
| 改造保留 | 保留业务含义，调整对象、关联或核算方法 |
| 按需保留 | 对实际 Fab 有用，但启用取决于工艺和实施范围 |
| 新增建议 | 原清单没有完整表达，需要独立设计；不是 OpenMES 现有字段 |
| 暂不纳入 | 不进入当前 Fab 执行主线；不删除原资料或已有历史数据 |

以下第 3、4 节的“原字段”列只列原清单存在的字段；改名和新增统一写在说明或第 5 节。未逐一列出的字段不表示可以删除，需要迁移时另做逐列核对。

## 2. 通用字段保留规则

| 数据类型 | 原字段中建议保留的部分 | Fab 处理原则 |
| --- | --- | --- |
| 身份与主数据 | `id`、`code`、`name`、`description`、`is_active` | 在原表存在时保留；业务编码与内部主键分开，停用后历史仍可引用 |
| 创建与修改 | `created_at`、`updated_at`、`created_by_id` | 仅按各原表实际列沿用；新对象统一补齐创建来源及必要审计 |
| 业务时间与责任人 | `started_at`、`completed_at`、`recorded_at` 及各类 `*_by_id` | 保留原始事实；业务发生时间与接收时间分开，统一存 UTC、按厂区时区展示 |
| 归属范围 | `tenant_id`、`line_id`、`area_id` | 租户、厂区、区域、产线分别建模；`tenant_id` 不等于厂区，`line_id` 也不能直接当设备组 |
| 外部编码 | `external_code`、`external_system`、`external_ref` | 有 ERP 或外部主数据时保留，不能覆盖 MES 内部身份 |
| 扩展数据 | `custom_fields`、`extra_data`、`parameters`、`config`、`payload` | 按需保留并定义结构版本；Lot、Wafer、Recipe、Hold 等关键关系采用显式字段/关系表 |
| 删除信息 | `deleted_at`、`deleted_by_id` | 主数据可停用或软删除；已发生的过站、报废、拆合批和审计采用追加更正或冲销，不能靠删除改历史 |
| 汇总字段 | 数量、时长、良率、OEE 等 | 可作受控汇总或快照；需记录口径，能回溯事实记录，不能多处自由编辑 |

第 3、4 节省略重复的主键、通用时间和删除字段；关系表保留其实际组合键，不强行为每张表增加独立 `id`。

## 3. 核心沿用字段

### 3.1 身份、组织、产品与工艺

| 业务对象 / 原表 | 建议保留的原字段 | 处理方式与用途 |
| --- | --- | --- |
| 用户 `users` | `username`、`name`、`email`、`worker_id`、`account_type`、`last_login_at`、`force_password_change` | 核心保留：操作责任人；账号状态与认证模型另行设计，密码和双因素字段按 C# 身份组件重建 |
| 角色 `roles`；权限 `permissions` | 各自的 `name` | 核心保留：重新定义 Fab 操作权限；`guard_name` 不作为业务字段继承 |
| 授权关系 `model_has_roles`；`model_has_permissions`；`role_has_permissions` | 分别为 `role_id`、`model_id`；`permission_id`、`model_id`；`permission_id`、`role_id` | 改造保留：改成明确的用户/角色关系；Laravel 的 `model_type` 多态结构不照搬；直接授权按需启用 |
| 组织 `lines` | `code`、`name`、`description`、`area_id`、`is_active` | 改造保留：保留组织定位；新增 Fab/Area 层级，`area_id` 引用对象需核实，不能仅凭列名认定已有完整区域主数据 |
| 人员 `workers` | `code`、`name`、`is_active` | 核心保留：员工身份；班组、联系方式与技能资格按需扩展，工资字段不进入执行核心 |
| 设备类型参考 `workstation_types` | `code`、`name`、`description`、`is_active` | 改造保留：参考建立设备类别；设备组与工艺资格单独建模 |
| 设备参考 `workstations` | `code`、`name`、`line_id`、`workstation_type_id`、`is_active` | 改造保留：区分物理设备、操作工位和登录终端，原工位不能自动视作真实设备 |
| 产品 `product_types` | `code`、`name`、`description`、`unit_of_measure`、`category`、`is_active`、`external_code`、`external_system` | 核心保留：产品型号与外部标识；Lot/Wafer 的生产数量按片，物料单位另定 |
| 产品版本 `product_revisions` | `product_type_id`、`revision_code`、`description`、`lifecycle_status`、`process_template_id`、`change_reason`、`external_ref`、`effective_from`、`effective_to`、`released_at`、`obsolete_at`、`released_by_id` | 核心保留：发布版本绑定 Route 版本；历史 Lot 不随主数据修改漂移 |
| 工艺路线参考 `process_templates` | `product_type_id`、`name`、`version`、`is_active` | 改造保留：拆分路线身份与不可变发布版本；`is_active` 不能代替版本审批和生命周期 |
| 工序参考 `process_segments` | `code`、`name`、`description`、`segment_type`、`workstation_type_id`、`standard_instruction`、`parameters`、`is_active` | 改造保留：定义 Operation 主数据；参数需要类型、单位和版本 |
| 路线步骤 `template_steps` | `process_template_id`、`process_segment_id`、`step_number`、`name`、`instruction`、`workstation_type_id`、`requires_confirmation`、`parameters` | 改造保留：建立 RouteStep 身份；同一工序在路线中重复出现时分别识别，不能只靠名称定位 |
| 编号规则 `lot_sequences` | `name`、`product_type_id`、`prefix`、`suffix`、`next_number`、`pad_size`、`pattern`、`reset_period`、`last_reset_key`、`purpose` | 核心保留规则语义：Lot/Wafer/载具编号分别定义；编号分配需并发安全 |

### 3.2 工单、Lot、Wafer 与执行履历

| 业务对象 / 原表 | 建议保留的原字段 | 处理方式与用途 |
| --- | --- | --- |
| 工单 `work_orders` | `order_no`、`product_type_id`、`product_revision_id`、`planned_qty`、`produced_qty`、`status`、`priority`、`due_date`、`description`、`planned_start_at`、`planned_end_at`、`completed_at` | 改造保留：表示生产需求与计划；一张工单可对应多个 Lot，工单不能代替在制 Lot |
| 工艺基线 `work_orders`；`work_order_snapshots` | 前者 `process_snapshot`、`snapshot_version`；后者 `work_order_id`、`version`、`snapshot`、`effective_from`、`effective_from_batch_id`、`change_request_id`、`created_by_id` | 改造保留：保留投放基线与变更关联；以显式 Route/Recipe 版本为主，快照为辅助，不能只存不可解释 JSON |
| Lot 参考 `batches` | `work_order_id`、`batch_number`、`lot_number`、`lot_assigned_at`、`target_qty`、`produced_qty`、`scrap_qty`、`status`、`started_at`、`completed_at`、`notes`、`snapshot_version` | 改造保留：建立生产 Lot；`batch_number` / `lot_number` 需选择唯一主业务编号并保留旧编号映射；目标量、在制量和完成量分开 |
| Wafer 参考 `serial_units` | `serial_no`、`work_order_id`、`batch_id`、`material_id`、`status`、`produced_at` | 改造保留：`serial_no` 映射晶圆标识，`batch_id` 仅能参考当前 Lot 归属；来料身份、建片时间和完工时间分开，必须另存归属历史 |
| 执行参考 `batch_steps` | `batch_id`、`step_number`、`name`、`instruction`、`status`、`started_at`、`completed_at`、`started_by_id`、`completed_by_id`、`workstation_id`、`confirmed_at`、`confirmed_by`、`requires_confirmation`、`passed_qty`、`scrap_qty`、`assigned_by_id`、`assigned_at` | 改造保留：拆出步骤实例、加工批次和 Track 事件；开始/完成时间须经语义验证后映射 Track-in/out，不覆盖返工的前次记录 |
| 单片过站 `unit_step_history` | `serial_unit_id`、`batch_step_id`、`workstation_id`、`operator_id`、`parameters`、`result`、`notes`、`processed_at` | 改造保留：保留 Wafer 级执行事实，补充实际加工、路线步骤、Recipe 版本和事件类型关联 |
| 过程确认 `process_confirmations` | `batch_id`、`batch_step_id`、`confirmed_by`、`confirmed_at`、`confirmation_type`、`notes`、`value` | 核心保留：关键操作的确认责任；不能单靠确认记录代替权限校验或完整审批 |
| 输出项定义 `template_step_outputs` | `process_template_id`、`template_step_id`、`key`、`label`、`value_type`、`unit`、`options`、`is_required`、`sort_order` | 核心保留：作业数据采集定义，需要版本化 |
| 输出值 `batch_step_output_values` | `batch_step_id`、`output_id`、`value_text`、`value_number`、`value_boolean`、`value_date`、`recorded_by_id`、`recorded_at` | 核心保留：执行时采集事实；量测还须关联具体 Wafer/采样点及规格版本 |

**数量口径：**生产 Lot 的当前片数从有效 Wafer 归属计算；工单完成量按定义的终点完成事件统计；工序通过量只用于该次执行。禁止把每站 `passed_qty` 累加成工单产量，也不以设备脉冲计数自动创建 Wafer。

### 3.3 质量、Hold、报废与追溯

| 业务对象 / 原表 | 建议保留的原字段 | 处理方式与用途 |
| --- | --- | --- |
| 异常类型 `issue_types` | `code`、`name`、`severity`、`is_blocking`、`is_active` | 核心保留：原因、严重度与阻塞策略；具体阻塞对象需明确 |
| 异常 `issues` | `work_order_id`、`batch_step_id`、`issue_type_id`、`title`、`description`、`status`、`reported_by_id`、`assigned_to_id`、`reported_at`、`acknowledged_at`、`resolved_at`、`closed_at`、`resolution_notes`、`disposition`、`non_conforming_qty`、`root_cause`、`containment_action`、`disposition_by_id`、`disposition_at` | 核心保留：补 Lot/Wafer/设备影响范围；关闭异常不自动解除全部 Hold |
| 停工参考 `work_order_stops` | `work_order_id`、`batch_id`、`type`、`reason`、`issue_id`、`stopped_by_id`、`stopped_at`、`resumed_by_id`、`resumed_at`、`resume_notes`、`context` | 改造保留：参考事件、原因和责任人语义；工单停工与 Lot Hold 分别建模 |
| 物料冻结参考 `material_lots` | `issue_id`、`hold_reason`、`held_at`、`held_by_id`、`released_at`、`released_by_id` | 改造保留：保留物料冻结事实；单组冻结字段不能表达并存的多条 Hold，不能直接作为生产 Lot Hold 模型 |
| 批次放行参考 `batches` | `released_at`、`released_by`、`release_type` | 改造保留：先确认是投产、质量放行还是解除冻结；三种动作不能共用一个含义不明的时间字段 |
| 过程检验定义 `quality_check_templates` | `process_template_id`、`name`、`min_checks_per_batch`、`samples_per_check`、`parameters` | 改造保留：明确路线步骤、抽样对象及规格版本；每日频次由实际工艺决定 |
| 过程检验 `quality_checks` | `batch_id`、`quality_check_template_id`、`checked_by`、`checked_at`、`production_quantity`、`all_passed`、`notes` | 改造保留：关联本次 ProcessRun、采样范围和规格；托盘字段不当载具字段使用 |
| 量测样本 `quality_check_samples` | `quality_check_id`、`sample_number`、`parameter_name`、`parameter_type`、`value_numeric`、`value_boolean`、`is_passed` | 改造保留：增加 Wafer、采样点、单位和规格快照；样本序号不能代替 Wafer ID |
| 规格和判定参考 `inspection_results` | `inspection_id`、`criterion_name`、`criterion_type`、`required`、`unit`、`spec_min`、`spec_max`、`value_numeric`、`value_boolean`、`value_text`、`is_passed`、`notes` | 改造保留：沿用规格/实测/判定语义；原表属于来料检验，过程量测另建关联，不混淆来源 |
| 报废原因 `scrap_reasons` | `code`、`name`、`description`、`category`、`is_active` | 核心保留：统一原因码 |
| 报废事实 `scrap_entries` | `work_order_id`、`batch_step_id`、`scrap_reason_id`、`quantity`、`shift_id`、`notes`、`reported_by`、`reported_at` | 改造保留：增加被报废 Wafer 清单和授权处置关联；数量与清单一致，保留报废前归属及历史 |

### 3.4 设备状态与审计

| 业务对象 / 原表 | 建议保留的原字段 | 处理方式与用途 |
| --- | --- | --- |
| 设备事件 `machine_events` | `workstation_id`、`machine_connection_id`、`event_type`、`state_from`、`state_to`、`payload`、`event_timestamp`、`correlation_id` | 改造保留：改为明确的设备关联，保留来源和事件时间；模拟事件需明确标识 |
| 状态区间 `workstation_states` | `workstation_id`、`state`、`started_at`、`ended_at`、`duration_seconds`、`source`、`metadata` | 改造保留：设备状态履历；运行、可用、维护和连接状态的口径需区分 |
| 停机原因 `downtime_reasons` | `code`、`name`、`kind`、`is_active` | 核心保留原因字典；分类映射待工厂确认 |
| 停机事实 `production_downtimes` | `line_id`、`workstation_id`、`downtime_reason_id`、`shift_id`、`started_at`、`ended_at`、`duration_minutes`、`notes`、`reported_by`、`needs_reason`、`classified_at`、`classified_by_id` | 保留字段语义；首版可由设备状态历史提供基础数据，避免两套时间重复计量 |
| 审计 `audit_logs` | `user_id`、`entity_type`、`entity_id`、`action`、`before_state`、`after_state`、`ip_address`、`user_agent`、`created_at` | 核心保留：敏感信息脱敏；增补请求关联与操作来源，覆盖 WebUI、Agent 和集成入口 |
| 业务事件 `event_logs` | `event_type`、`entity_type`、`entity_id`、`user_id`、`payload`、`created_at` | 核心保留：与具体业务事实关联；通用日志不能替代 Wafer 归属或加工履历表 |
| 附件 `attachments` | `entity_type`、`entity_id`、`original_name`、`storage_path`、`mime_type`、`file_size`、`uploaded_by_id` | 保留事实证据的索引与文件本体；路径不能代替文件内容，需补校验值和访问控制 |

## 4. 按工艺与实施范围保留的字段

这些模块不是“不需要”，而是尚未确认为首版。涉及真实生产时，设备资格、材料寿命、人员资格等可能成为投产前置条件。

| 原表 / 模块 | 建议保留的原字段 | 适用条件与取舍 |
| --- | --- | --- |
| `shifts`；`shift_handovers` | 前者 `code`、`name`、`start_time`、`end_time`、`days_of_week`、`line_id`、`is_active`；后者 `shift_id`、`line_id`、`business_date`、`shift_start`、`shift_end`、`discrepancies`、`notes`、`confirmed_by`、`confirmed_at` | 班次统计与交接；包装、托盘汇总不进入 Fab 核心，跨午夜班次定义生产日 |
| `companies`；`work_orders` | 前者 `code`、`name`、`type`、`is_active`；后者 `customer_id`、`customer_order_no` | Foundry 客户与供应商关联；不能把 `companies` 主键直接写入原本引用 `customers` 的字段，需统一主数据映射 |
| `materials`；`material_types` | 前者 `code`、`name`、`description`、`material_type_id`、`unit_of_measure`、`tracking_type`、`supplier_code`、`external_code`、`external_system`、`is_active`；后者 `code`、`name` | 硅片、化学品、气体等主数据；物料类别和跟踪粒度由工艺确认 |
| `material_lots` | `lot_number`、`material_id`、`source_id`、`quantity_received`、`quantity_available`、`unit_of_measure`、`received_at`、`manufacturing_date`、`expiry_date`、`status`、`supplier_lot_no`、`supplier_reference`、`inspection_id`、`source_container_no`、`warehouse_id` | 来料与化学品批次；与生产 Lot 分开；化学品开封寿命/上机寿命需要新增字段 |
| `bom_items` | `process_template_id`、`template_step_id`、`material_id`、`quantity_per_unit`、`per`、`scrap_percentage`、`notes` | 参考工序用料要求；消耗基数可按片、加工批、时间等确认，不能统一套装配件 BOM |
| `material_allocations`；`allocation_lot_picks` | 前者 `batch_id`、`batch_step_id`、`material_id`、`work_order_id`、`allocated_qty`、`returned_qty`、`expected_qty`、`consumed_qty`、`adjustment_qty`、`scrap_qty`、`status`、`allocated_by`、`allocated_at`、`consumed_at`、`consumption_warehouse_id`；后者 `material_allocation_id`、`material_lot_id`、`picked_qty`、`picking_strategy`、`consumption_warehouse_id` | 领料、退料与来源批次；采用此模块时三者一起保留才能追到实际物料批次 |
| `batch_step_lot_consumption` | `batch_step_id`、`material_lot_id`、`sublot_id`、`quantity_consumed`、`consumed_at`、`recorded_by_id` | 工序耗料追溯；补单位及 ProcessRun 关联；设备共用化学品需另记上机时间区间，不能伪造每片精确消耗 |
| `warehouses`；`warehouse_stocks`；`stock_movements` | 分别为 `code`、`name`、`kind`、`erp_code`、`is_active`；`warehouse_id`、`material_id`、`product_type_id`、`material_lot_id`、`quantity`、`unit_of_measure`；`material_id`、`warehouse_id`、`movement_type`、`quantity`、`balance_after`、`source_type`、`source_id`、`reason`、`performed_by`、`performed_at` | MES 管理库存时采用；库存流水需补物料批次和单位，ERP 管库存时明确主从关系 |
| `stock_documents`；`stock_document_lines` | 前者 `document_no`、`type`、`status`、`warehouse_id`、`work_order_id`、`batch_id`、`posted_at`、`posted_by_id`、`erp_reference`、`erp_synced_at`；后者 `stock_document_id`、`material_id`、`product_type_id`、`material_lot_id`、`lot_number`、`quantity`、`unit_of_measure` | 需要出入库过账或 ERP 对账时采用，单据与流水避免重复记账 |
| `inspection_plans`；`inspections` | 前者 `name`、`material_id`、`material_type_id`、`criteria`、`version`、`published_at`、`root_id`、`is_active`；后者 `inspection_plan_id`、`material_id`、`lot_number`、`supplier_lot_ref`、`quantity_received`、`inspector_id`、`started_at`、`completed_at`、`status`、`disposition`、`disposition_notes`、`disposition_by_id`、`disposition_at`、`plan_version`、`issue_id` | 来料检验；逐项结果沿用第 3 节 `inspection_results`，补物料批次主键关联 |
| `quality_control_triggers`；`quality_control_tasks` | 前者 `name`、`trigger_type`、`quality_check_template_id`、`workstation_id`、`product_type_id`、`threshold_n`、`downtime_min_minutes`、`is_blocking`、`is_active`；后者 `quality_control_trigger_id`、`status`、`batch_id`、`workstation_id`、`due_reason`、`quality_check_id`、`issue_id`、`fired_at`、`completed_at`、`completed_by_id` | 首件、维护后验证或定期抽检；规则需版本化，不能直接等同 SPC |
| `issue_actions` | `issue_id`、`type`、`title`、`description`、`assigned_to_id`、`due_date`、`status`、`completed_at`、`completed_by_id`、`verified_at`、`verified_by_id`、`notes` | 异常整改闭环 |
| `work_order_change_requests` | `code`、`work_order_id`、`reason`、`status`、`proposed`、`previous_values`、`impact`、`effective_from`、`effective_from_batch_id`、`requested_by_id`、`submitted_at`、`approved_by_id`、`approved_at`、`rejected_by_id`、`rejected_at`、`rejection_reason`、`applied_by_id`、`applied_at`、`resulting_snapshot_version` | 工程变更审批；必须明确适用 Lot/Wafer 集合与生效步骤，不用数量阈值替代具体对象 |
| `template_step_checklist_items`；`batch_step_checklist_completions` | 前者 `template_step_id`、`label`、`is_required`、`sort_order`；后者 `batch_step_id`、`checklist_item_id`、`checked_by_id`、`checked_at` | 作业前点检与完成证明；定义与执行记录需版本一致 |
| `engineering_documents`；`batch_step_documents` | 前者 `entity_type`、`entity_id`、`original_filename`、`document_type`、`mime_type`、`file_size`、`revision`、`checksum`、`storage_path`、`lifecycle_status`、`effective_from`、`effective_to`、`released_at`、`released_by_id`、`uploaded_by_id`；后者 `batch_step_id`、`name`、`reference`、`file_path`、`is_mandatory`、`requires_validation`、`validated_at`、`validated_by_id` | SOP、工程文件与执行证据；历史执行绑定当时的文档版本 |
| `tools`；`maintenance_schedules`；`maintenance_events` | 分别为 `code`、`name`、`workstation_type_id`、`status`、`next_service_at`；`name`、`tool_id`、`workstation_id`、`event_type`、`assigned_to_id`、`frequency`、`interval_value`、`last_executed_at`、`next_due_at`、`is_active`；`title`、`event_type`、`status`、`tool_id`、`workstation_id`、`schedule_id`、`assigned_to_id`、`scheduled_at`、`started_at`、`completed_at`、`resolution_notes` | 工装与 PM 参考；光罩、设备、腔体分别管理，保养完成不自动表示工艺资格恢复 |
| `machine_connections`；`machine_messages` | 前者 `name`、`protocol`、`is_active`、`status`、`status_message`、`last_connected_at`；后者 `machine_connection_id`、`topic`、`raw_payload`、`parsed_data`、`processing_status`、`processing_error`、`received_at` | 真机集成时启用；原始报文留存与业务事实分开，连接状态不能代替设备可加工状态 |
| `machine_tags` | `machine_connection_id`、`workstation_id`、`name`、`address`、`signal_type`、`data_type`、`transform`、`unit`、`is_active` | 按实际协议配置测点；MQTT/Modbus/OPC UA 子表按协议选用，SECS/GEM 需要另行设计 |
| `integration_configs`；`material_sources` | 前者 `system_type`、`system_name`、`is_active`；后者 `material_id`、`integration_config_id`、`external_code`、`external_name`、`unit_mapping`、`conversion_factor`、`last_synced_at`、`sync_enabled` | 对接 ERP/物料主数据；连接配置与凭据分离 |
| `api_keys` | `integration_config_id`、`name`、`key_prefix`、`key_hash`、`scopes`、`ip_allowlist`、`is_active`、`last_used_at`、`expires_at` | 采用 API Key 集成时保留语义；权限沿用服务端规则，不迁移明文密钥 |
| `csv_import_mappings`；`csv_imports` | 前者 `name`、`mapping_config`、`entity`；后者 `user_id`、`entity`、`original_filename`、`file_path`、`status`、`total_rows`、`successful_rows`、`failed_rows`、`error_log`、`started_at`、`completed_at`、`dry_run` | 批量导入主数据时保留；先校验再提交，导入也需通过业务约束 |
| `label_templates`；`custom_field_definitions` | 前者 `name`、`type`、`size`、`fields_config`、`barcode_format`、`is_active`；后者 `entity_type`、`key`、`label`、`type`、`config`、`required`、`position`、`is_active` | Lot/载具标签和非核心扩展；不沿用医疗 UDI 语义，不用自定义字段替代领域关系 |

## 5. Fab 场景必须补齐或预留的数据

**本节所有对象和字段名都是新增建议**，用于说明原数据模型的缺口。即使借用了类似列名，也不表示原表已有该对象。首版必需与实际工艺启用项分列如下。

| 建议对象 | 新增建议字段（不重复通用主键/审计） | 优先级与规则 |
| --- | --- | --- |
| 厂区与区域 | Fab：`fab_code`、`name`、`timezone`；Area：`fab_id`、`area_code`、`name` | 首版：明确数据范围，生产对象能可靠归属到 Fab |
| 设备组与设备 | 组：`fab_id`、`equipment_group_code`；设备：`area_id`、`equipment_group_id`、`equipment_code`、`equipment_type`、`operational_state` | 首版：定义设备身份和所在区域；资格、通信状态与运行状态分别处理 |
| Lot 补充 | `fab_id`、`product_revision_id`、`route_version_id`、`current_route_step_id`、`lot_type`、`priority`、`released_to_route_at`、`current_wafer_count` | 首版：当前片数为受控派生值，投放工艺不等同解除 Hold |
| Wafer 补充 | `wafer_code`、`origin_material_lot_id`、`supplier_wafer_code`、`wafer_size_mm`、`lifecycle_status` | 首版身份与来源；尺寸用于已选工艺，供应商标识可空；不能用槽位号作晶圆永久身份 |
| Wafer 与 Lot 归属历史 | `wafer_id`、`lot_id`、`valid_from`、`valid_to`、`change_event_id` | 首版：同一 Wafer 的有效归属区间不能重叠，拆合批后仍能查询原 Lot |
| Carrier 与槽位历史 | 载具：`carrier_code`、`carrier_type`、`slot_capacity`、`status`、`current_location_id`；装载：`carrier_id`、`slot_no`、`wafer_id`、`loaded_at`、`unloaded_at` | 首版：按工艺选 FOUP/卡匣等类型；有效槽位不能重复占用，Wafer 同时最多占一个槽位；载具不等于 Lot |
| Route / RouteVersion / RouteStep | 路线：`route_code`；版本：`route_id`、`version`、`lifecycle_status`、`effective_from`、`effective_to`、`released_by_id`、`released_at`；步骤：`route_version_id`、`operation_id`、`sequence_no` | 首版：发布版本固定；同工序可多次出现，每个步骤独立身份 |
| RecipeVersion 与适用关系 | 配方：`recipe_code`、`version`、`lifecycle_status`、`checksum`、`released_at`、`released_by_id`；适用关系：`recipe_version_id`、`route_step_id`、`equipment_id`、`effective_from`、`effective_to` | 首版：只管理版本引用和匹配关系；校验值在有文件时填写，设备配方下载后置 |
| 步骤执行实例 | `lot_id`、`route_step_id`、`visit_no`、`status`、`entered_at`、`exited_at` | 首版：区分路线位置与实际到访；重复经过同一步骤新增实例 |
| ProcessRun 与参与晶圆 | 加工：`step_execution_id`、`equipment_id`、`recipe_version_id`、`track_in_at`、`track_out_at`、`track_in_by_id`、`track_out_by_id`、`status`；参与表：`process_run_id`、`wafer_id`、`result` | 首版：固定实际加工的晶圆集合，不能事后根据当前 Lot 归属反推 |
| TrackEvent | `process_run_id`、`event_type`、`occurred_at`、`recorded_at`、`actor_id`、`source`、`correlation_id`、`idempotency_key` | 首版：每次动作留痕，重复提交不重复过站/计数；有中止/更正时保留原事件 |
| HoldRecord | `lot_id`、`reason_code_id`、`issue_id`、`hold_scope`、`held_at`、`held_by_id`、`released_at`、`released_by_id`、`release_reason` | 首版 Lot Hold；每个原因独立一条，全部适用阻塞解除后才允许继续；单片 Hold 后续用关联表扩展 |
| 拆合批事件与逐片明细 | 事件：`event_type`、`reason`、`occurred_at`、`performed_by_id`；明细：`event_id`、`wafer_id`、`from_lot_id`、`to_lot_id` | 首版：能表达多源/多目标关系；不得只给 Lot 增加一个父批字段，必须逐片核对守恒 |
| 量测关联与规格版本 | `process_run_id`、`lot_id`、`wafer_id`、`sample_point`、`parameter_code`、`spec_version_id`、`unit`、`spec_min`、`spec_max`、`measured_at`、`measurement_equipment_id` | 首版基础量测：明确整批或单片粒度；无采样点时可空；规格上下限与 SPC 控制限分开 |
| Wafer 处置明细 | `wafer_id`、`lot_id_at_event`、`process_run_id`、`disposition_type`、`reason_code_id`、`issue_id`、`approved_by_id`、`performed_by_id`、`occurred_at` | 首版报废：记录具体晶圆及处置前上下文；返工与报废不同，不靠减数量表示报废 |
| 请求与审计关联 | `correlation_id`、`idempotency_key`、`row_version`、`source_channel`、`actor_user_id`、`reason_code_id` | 首版：分别用于关联、幂等、并发和责任追踪；按对象适用，不机械加到所有表 |
| 设备/人员资格 | `equipment_id` 或 `worker_id`、`operation_id`、`recipe_version_id`、`qualification_status`、`valid_from`、`valid_to`、`qualified_by_id` | 按实际生产范围启用：岗位授权不能代替工艺资格，资格过期应影响执行校验 |
| 腔体与共炉加工 | 腔体：`equipment_id`、`chamber_code`；共炉：`process_batch_id`、`process_run_id`、`lot_id`、`wafer_id`、`position` | 后续：首版一设备一次加工一个 Lot；启用多 Lot 共炉时必须增加批处理及成员模型 |
| 光罩与使用记录 | `reticle_code`、`revision`、`layer_code`、`status`、`usage_count`；使用：`reticle_id`、`process_run_id`、`loaded_at`、`unloaded_at` | 含光刻工艺时评估，不能仅用通用工具名称描述 |
| Q-time 与返工 | Q-time：`start_route_step_id`、`end_route_step_id`、`limit_minutes`、`started_at`、`deadline_at`、`violated_at`；返工：`original_step_execution_id`、`rework_route_version_id`、`return_route_step_id`、`reason_code_id`、`approved_by_id` | 按工艺启用：开始/结束触发点需进一步明确；超时处置和返工审批均走服务端规则 |
| 材料上机与寿命 | `material_lot_id`、`equipment_id`、`opened_at`、`mounted_at`、`unmounted_at`、`usable_until`、`usage_quantity`、`unit` | 化学品等实际工艺启用，按使用区间追溯受影响加工记录 |

Agent 会话与工具审计按已有系统规划单独设计，并通过 `correlation_id` 关联 MES 业务记录；不把聊天记录当生产履历，也不让模型输出直接写生产数据或下发设备指令。

## 6. 暂不纳入 Fab 执行核心的数据

| 原数据 | 处理建议 |
| --- | --- |
| `udi_code`、`udi_readable` | 医疗 UDI 专用语义不沿用；产品/批次/载具标识按 Fab 编码体系设计 |
| `unit_cartons`、`pallets`、`packaging_scan_logs`、`packaging_checklists`、`work_order_eans`、`carton_id`、`pallet_id`、`packed_qty` | 包装出货业务后置；不能把箱号/托盘号改名就当 FOUP，也不能把装箱关系当槽位历史 |
| `pallet_movements` | 位置变更、经手人和时间的思想可参考；载具移动与设备装卸历史另建模型 |
| `subassemblies`、`serial_unit_components` | 装配件和零件绑定关系不作为前道晶圆主线；物料消耗、晶圆血缘与光罩使用分别建模 |
| `material_sublots` | 可用于物料分装；其父子关系不等同生产 Lot 的拆合批血缘 |
| `pay_type`、`pay_rate`、`pay_currency`、`wage_group_id`、`cost_sources`、`additional_costs` | 薪资与成本核算后置，保留执行人员和工时事实即可 |
| 客户 `payment_score`、`total_orders`、`total_revenue` 及工单 `unit_price` | CRM/财务侧数据，执行核心只保留必要客户和订单引用 |
| `oee_records`、`ideal_rate_per_hour`、`machine_counters`、`machine_counter_readings` | 原始状态/计数有参考价值；OEE、产速与晶圆数量重新定义口径，首版不沿用通用件数算法 |
| `week_number`、`month_number`、`production_year`、`priority_score` | 时间分组和派工分值可按规则计算；需要历史复现时保存规则版本与计算时间 |
| `line_statuses`、`line_status_id`、`is_optional`、`variant_group`、`skip_reason` | 展示状态和可选步骤配置不能直接控制 Fab 过站；跳站、分支或返工需受控路线规则及授权履历 |
| `view_templates`、`line_view_columns`、`dashboard_widgets`、`operator_screens` | 可参考页面配置能力，按新 WebUI 设计；不作为制造事实或权限来源 |
| `sessions`、`cache`、`cache_locks`、`jobs`、`job_batches`、`failed_jobs`、`migrations`、`system_updates` | 由 .NET、任务组件和 Docker 交付方式重建，不迁移 Laravel 运行时状态 |
| `remember_token`、`password_reset_tokens`、`personal_access_tokens`、`two_factor_secret`、`two_factor_recovery_codes`、协议连接密码/私钥 | 按新身份及密钥体系处理，不当普通业务列复制；保留账号身份和授权意图即可 |

## 7. 首版范围与确认项

首版字段基线建议按 **身份权限 → 厂区/设备 → 产品与 Route/Recipe 版本 → Lot/Wafer/载具 → 加工履历 → Hold/量测/报废 → 拆合批与审计** 收敛。物料全库存、自动搬运、真机协议、SPC/FDC/APC、复杂返工和商业财务按后续场景展开。

进入数据库设计前，需要业务确认：

1. 试点工艺及晶圆/载具类型，是否涉及光刻、共炉、Q-time 或单片加工。
2. Lot/Wafer 编码、来料转生产 Lot 的方式，以及拆合批、返工和报废规则。
3. Track-in/out 对应的实际业务时点、加工参与片清单，以及量测抽样粒度。
4. 哪些参数决定能否过站、哪些资格或材料寿命是实际投产前置条件。
5. 主数据来源、工艺版本变更的生效范围，以及履历/原始报文/附件的保留期限。

本次仅完成字段取舍与差距分析；字段类型、必填性、索引、唯一约束、保留年限及真实数据迁移方案待范围确认后设计。
