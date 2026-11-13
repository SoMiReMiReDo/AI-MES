# MESagent Fab 全功能清单 v0.1

更新于 2026-11-13。以单企业、单厂、前道晶圆制造试点为规划基线，具体工艺、晶圆尺寸、载具类型和角色分工待确认。当前已建立 119 张业务/支撑表、7 张 Identity 表和 2 个派生视图。以下是待交付功能清单，数据库对象表示可复用的数据基础。组织结构、人员与场所、账号会话及角色权限，以及 F2-01～05、F3 工单生命周期/初始快照和进度查询契约的 API 与 WebUI 已实现；F4 基础与 F5-01～05、最小设备前置、真实终点及 F3 进度也已实现；完整 F6/F7、Agent 和设备驱动仍待后续。

清单共 14 个模块、87 项功能，基于最初 114 个业务实体、Identity 能力和两个派生视图。建议首版 48 项、按需 30 项、扩展 9 项。

后续设计入口：[功能实现架构](08-feature-implementation-architecture.md) 与 [F1-`OrganizationIdentityAccessControl` 功能设计](../feature-development/organization-identity-access-control.md)。2026-11-11 已确认本轮先细化单厂 组织结构、人员与场所、账号会话及角色权限，05/06 写扩展边界，Agent 仅预留位置；下列全量清单保留长期功能范围，不表示这些功能已实现或全部进入本轮开发。

- **首版**指完整演示闭环，包括只读 Agent 与 Docker 交付。表中的优先级是实施建议。
- **按需**指已有数据基础或现场接口方向，按试点工艺、权责和业务前置条件启用。若实际工艺依赖某项门禁，该项须在对应工艺投用前交付。
- **扩展**指需要补充数据模型、外部契约或独立验收的能力，纳入前单独确认范围。
- 表中列出主要对象，查询可复用多个域的数据。功能 ID 用于拆任务和验收。字段、约束和关系见 [实际字段字典](05-database-catalog.md)、[数据库实施](06-database-implementation.md) 和 [数据架构](../MESagent-development-guide.md)。

主线为 **工艺发布 → 工单或授权建批 → Lot/Wafer 投放 → 派工 → Track-in → 点检与量测 → Track-out → 下一站或完工 → 正反向追溯**。Hold、报废与拆合批作为独立业务动作插入主线。异常关闭、Lot 投放、质量放行和解除 Hold 各自留痕。

F1-`OrganizationIdentityAccessControl` 最新实现与验证见 [实现记录](../feature-development/organization-identity-access-control-implementation.md)；Docker 打包按用户要求后置。

命名约定：模块使用“编号-`EnglishCapabilityName`”，例如 F1-`OrganizationIdentityAccessControl`；子功能使用模块编号加两位序号，例如 `F2-01`，并由所属模块标题明确英文能力名称。已有明确英文名的子功能可完整写为 F1-01-`OrganizationStructure`。编号用于文档追踪，代码类型和目录仍使用能力英文名。

## F1-`OrganizationIdentityAccessControl` 组织、身份与访问控制

| ID | 功能与验收要点 | 主要对象 | 阶段 |
| --- | --- | --- | --- |
| F1-01-`OrganizationStructure` | 维护厂区、区域、产线、位置及启停状态。统一 Fab 时区，历史记录可访问已停用主数据。 | `Fab`、`Area`、`Line`、`Location` | 首版 |
| F1-02-`WorkforceAndTerminals` | 管理员工、工位和登录终端，关联责任人员与操作场所。工位、终端和物理设备独立标识。 | `Worker`、`Workstation`、`Terminal` | 首版 |
| F1-03-`AccountsAndSessions` | 登录、退出、个人资料、修改与重置密码、账号锁定与停用、首登改密和会话失效。 | `MesUser`、ASP.NET Core Identity | 首版 |
| F1-04-`RolesAndAuthorization` | 管理角色、权限和厂区范围。操作员、工艺、质量、计划、设备、管理员及只读角色分权，查询、导出、写入与 Agent 工具均执行对象级授权。 | `MesRole`、`Permission`、`RolePermission`、`UserPermission`、`UserFabScope` | 首版 |
| F1-05-`ShiftsAndHandovers` | 配置班次与生产日，处理跨午夜；按班记录未完工批次、设备停机和异常，交接双方确认。 | `Shift`、`ShiftHandover` | 按需 |
| F1-06-`FederatedIdentityAndMultiFab` | 账号双因素按组织要求启用；企业 SSO 或多厂隔离另行验证身份映射、数据过滤和跨厂操作。 | Identity 登录、声明、令牌；`UserFabScope` | 按需 |

## F2-`ProductProcessVersionManagement` 产品、工艺与版本管理

| ID | 功能与验收要点 | 主要对象 | 阶段 |
| --- | --- | --- | --- |
| F2-01 | 管理产品和修订版，建立、发布、废止版本，关联有效 Route 版本并查受影响 Lot。已发布内容通过新版本变更。 | `Product`、`ProductRevision` | 首版 |
| F2-02 | 管理工序、路线及路线版本，编辑步骤顺序、设备适用要求和作业说明。发布校验步骤完整性，重复工序通过各自步骤 ID 区分。 | `Operation`、`Route`、`RouteVersion`、`RouteStep` | 首版 |
| F2-03 | 管理 Recipe 和修订、有效期、版本标识与适用步骤及设备；查询候选配方并记录实际采用版本。文件签名、设备端比对及下发见 F11-`DataImportEquipmentIntegration`。 | `Recipe`、`RecipeVersion`、`RecipeApplicability` | 首版 |
| F2-04 | 配置 Lot 等业务编号的规则和编号分配。预览格式，校验重号，保证并发分配安全；外部编码按映射保留。 | `NumberSequence` | 首版 |
| F2-05 | 随路线版本定义步骤点检、必填输出、数据类型与单位，关联作业指导书。步骤执行按绑定版本取定义。 | `ChecklistItem`、`OutputDefinition`、`EngineeringDocument` | 首版 |
| F2-06 | 工艺变更影响分析、审批和生效控制，识别在制 Lot 与新投批次，记录受控切换和原基线。多级审批或任意在制路线迁移须另定规则。 | `ProductRevision`、`RouteVersion`、`RecipeVersion`、`WorkOrderChangeRequest` | 按需 |

## F3-`WorkOrderPlanningExecution` 工单与计划执行

| ID | 功能与验收要点 | 主要对象 | 阶段 |
| --- | --- | --- | --- |
| F3-01 | 创建、查询、编辑草稿、下达、取消或关闭工单。记录产品版本、计划片数、优先级、交期，按状态限制动作。 | `WorkOrder` | 首版 |
| F3-02 | 工单关联多个 Lot，查看投放、在制、阻塞和完工进度。工单完工量只计归属明确且已终点完成的唯一 Wafer。 | `WorkOrder`、`Lot`、`Wafer`、`LotMembership` | 首版 |
| F3-03 | 维护下达基线和快照，处理变更申请、评估影响、审批、拒绝和生效。历史执行版本保持可解释。 | `WorkOrderSnapshot`、`WorkOrderChangeRequest` | 按需 |
| F3-04 | 工单或关联 Lot 停工与复工，记录原因、影响范围、操作人和时间。Lot Hold 仍按独立规则判断。 | `WorkOrderStop`、`Issue` | 按需 |
| F3-05 | 关联 Foundry 客户及客户订单号，查看交期和履约进度；排产日历、拖拽排程、有限产能和自动优化见 F10-`WorkbenchDashboardsAlertsScheduling`。 | `Company`、`WorkOrder` | 按需 |

## F4-`LotWaferCarrierGenealogy` Lot、Wafer、载具与血缘

2026-11-11 本轮确认并完成 [F4-01～06 功能设计](../feature-development/lot-wafer-carrier-genealogy.md)及[接口与验收文档](../feature-development/lot-wafer-carrier-genealogy-api-and-acceptance.md)，2026-11-12 首版实现与边界见[实现记录](../feature-development/lot-wafer-carrier-genealogy-implementation.md)，F5/F6 完整联调待完成。首版必须关联已下达工单，工程批与 F4-07 预留；拆合批限非加工中、无有效 Hold，合批要求同厂/工单/产品修订/路线版本/工艺位置。下表保留长期功能边界，授权无工单场景不进入当前首版。

| ID | 功能与验收要点 | 主要对象 | 阶段 |
| --- | --- | --- | --- |
| F4-01 | 按工单或获准的工程场景建批，创建或登记 Wafer 清单、来源与规格。独立管理 Lot 编号、Wafer ID 和物料来源批号。 | `Lot`、`Wafer`、`LotMembership`、`MaterialLot` | 首版 |
| F4-02 | 投放 Lot 时核对产品/路线版本、片清单和适用门禁，绑定工艺版本并进入首站。查询当前步骤、生命周期、有效片数、Hold 和历史归属。 | `Lot`、`LotMembership`、`StepExecution`、`production.lot_quantities` | 首版 |
| F4-03 | 建立载具、槽位与位置台账；装载、卸载、换槽、换载具和人工移位。校验容量、同片和同槽占用，保留时间历史。 | `Carrier`、`CarrierSlot`、`CarrierLoad`、`CarrierMovement`、`Location` | 首版 |
| F4-04 | 按指定 Wafer 拆批，记录源/目标和继承的工艺位置；同一事务关闭旧归属、创建新归属并核对片集合守恒。 | `GenealogyEvent`、`GenealogyDetail`、`LotMembership` | 首版 |
| F4-05 | 受限合批。建议首版仅处理同厂、同工单（或同一已授权无工单场景）、同产品修订、同 Route 版本及同工艺位置的非加工中 Lot；校验适用 Recipe、有效 Hold 与逐片归属，保留源批历史。 | `Lot`、`GenealogyEvent`、`GenealogyDetail`、`LotMembership` | 首版 |
| F4-06 | 批次完工、取消或合批耗尽等收尾；分别核算完工、转移与报废。更正通过授权命令和追加事实，保留原历史。 | `Lot`、`Wafer`、`WorkOrder`、`BusinessEvent` | 首版 |
| F4-07 | 特殊批种、跨工单合批或试验批流转；按现场规则明确所有权、工艺兼容、产量归属及审批。 | `Lot`、`LotMembership`、`GenealogyDetail`；规则待定 | 扩展 |

## F5-`DispatchAndProductionExecution` 派工、过站与生产执行

2026-11-13 用户确认并实现 F5-01～05 与最小设备前置；已接通 F2 执行绑定、F4 真实终点和 F3 进度。详见[功能设计](../feature-development/dispatch-and-production-execution.md)与[实现验收](../feature-development/dispatch-and-production-execution-implementation.md)。06～08、完整 F7 和异常恢复留在待补全清单。

| ID | 功能与验收要点 | 主要对象 | 阶段 |
| --- | --- | --- | --- |
| F5-01 | 操作台按设备组、步骤、优先级和等待时间查询待加工 Lot，展示可执行性与阻塞原因，支持人工选择或指派。 | `Lot`、`StepExecution`、`RouteStep`、`EquipmentGroup` | 首版 |
| F5-02 | Track-in 校验权限、Fab、当前步骤、有效 Hold、设备可用/占用、适用 Recipe、当前片清单和已启用门禁。锁定一次加工的实际设备、配方和参与晶圆。 | `StepExecution`、`ProcessRun`、`ProcessRunWafer`、`TrackEvent` | 首版 |
| F5-03 | 加工中显示指令、点检、输出、作业文档和关键操作确认；按配置校验必填项及单位，保存责任人和时间。 | `ChecklistCompletion`、`OutputValue`、`StepDocument`、`ProcessConfirmation` | 首版 |
| F5-04 | Track-out 匹配未结束 Run，录入逐片结果并按策略校验输出和量测门禁，原子关闭 Run 和步骤、释放占用并推进或等待处置。重复提交返回同一结果。 | `ProcessRun`、`ProcessRunWafer`、`TrackEvent`、`StepExecution`、`Lot` | 首版 |
| F5-05 | 加工中止及受控收尾。保留已发生的加工事实和原因，授权结束当前 Run，明确等待处置或允许重试；加工中 Hold 阻止启动下一步。 | `ProcessRun`、`TrackEvent`、`Issue`、`HoldRecord` | 首版 |
| F5-06 | Q-time 定义起止事件、时限、预警和违规处置；服务端计算窗口并去重触发，规定拆合批、停机和返工对时钟的影响。 | `QTimeRule`、`QTimeWindow`、`Issue`、`HoldRecord` | 按需 |
| F5-07 | 受控返工。指定原因、原步骤、返工路线版本与返回位置，经授权后生成新步骤到访和加工记录。跳站、分支及豁免规则按工艺确认。 | `ReworkRecord`、`RouteVersion`、`StepExecution` | 按需 |
| F5-08 | 腔体级执行、多 Lot 共炉、批内位置和局部失败处理。定义成员兼容、容量和关闭规则，验证后调整现有单设备 Running 约束。 | `Chamber`、`ProcessBatch`、`ProcessBatchMember`、`ProcessRun` | 按需 |

## F6-`QualityExceptionDisposition` 质量、异常与处置

| ID | 功能与验收要点 | 主要对象 | 阶段 |
| --- | --- | --- | --- |
| F6-01 | 维护异常类型和按类别区分的原因码；登记、确认、指派、处置、关闭异常及整改，关联受影响工单、Lot、Wafer、步骤或设备。 | `ReasonCode`、`IssueType`、`Issue`、`IssueAction` | 首版 |
| F6-02 | 手动或按已配置质量规则冻结 Lot，记录阻塞原因、来源和责任人；授权逐条解除并复核其余 Hold。质量放行独立记录，Lot 投放见 F4-02。 | `HoldRecord`、`LotRelease`、`Issue` | 首版 |
| F6-03 | 管理检验模板与规格版本；按 Lot 或 Wafer 录入参数、单位、样点、测量设备和实际结果，后端依据绑定规格判定并保存当时上下限。 | `QualityCheckTemplate`、`SpecificationVersion`、`QualityCheck`、`Measurement` | 首版 |
| F6-04 | 授权逐片报废。核对 Wafer 清单与报废量，在事务内处理生命周期、有效归属、载具占用和审计；保留处置前 Lot 和加工上下文。 | `ScrapEntry`、`WaferDisposition`、`LotMembership`、`CarrierLoad` | 首版 |
| F6-05 | 按步骤、设备、产品、频次或停机条件触发质检任务，跟踪执行、逾期、阻塞和复检，触发重复时保持任务唯一。 | `QualityTrigger`、`QualityTask`、`QualityCheck` | 按需 |
| F6-06 | 来料检验计划版本、检验任务、结果、拒收或条件放行；物料批次可多条冻结，逐条授权释放并联动可用性。 | `InspectionPlan`、`IncomingInspection`、`InspectionResult`、`MaterialHold`、`MaterialLot` | 按需 |
| F6-07 | 单片 Hold、偏差许可和复核审批。明确适用对象、允许继续的条件、有效期和可撤销边界。 | `HoldRecord`、`WaferDisposition`；单片 Hold 与审批关系待补 | 扩展 |
| F6-08 | SPC 控制图、超控规则、FDC 异常分析和 APC 接口；分别定义规格限、控制限、数据来源和处置策略。 | `Measurement`、`EquipmentEvent`；规则、时序及分析模型待补 | 扩展 |

## F7-`EquipmentMaintenanceResources` 设备、停机、维护与生产资源

| ID | 功能与验收要点 | 主要对象 | 阶段 |
| --- | --- | --- | --- |
| F7-01 | 维护设备类型、设备组和设备台账，设置所属区域、可选产线和启用状态，查看能力和当前加工。腔体台账随相关工艺启用。 | `EquipmentType`、`EquipmentGroup`、`Equipment`、`Chamber` | 首版 |
| F7-02 | 接收人工或模拟设备事件，维护运行状态与区间履历，查看当前 Lot、停机原因及影响。区分通信在线、设备运行状态与是否允许加工。 | `EquipmentEvent`、`EquipmentState`、`Downtime`、`ReasonCode`、`equipment.state_durations` | 首版 |
| F7-03 | 登记停机起止、补录原因、查看未分类停机及班次统计。统一设备状态时长与停机的统计口径。 | `Downtime`、`EquipmentState`、`Shift` | 按需 |
| F7-04 | 维护工具台账，建立周期性维护计划与事件，完成、延期和复机确认，维护阻塞接入派工。 | `Tool`、`MaintenanceSchedule`、`MaintenanceEvent` | 按需 |
| F7-05 | 管理设备、人员的工序或配方资格、生效、到期、失效及复认证；启用后纳入 Track-in 校验。 | `EquipmentQualification`、`WorkerQualification` | 按需 |
| F7-06 | 光罩版本、层次、使用寿命、装卸及加工关联，查询受影响晶圆。 | `Reticle`、`ReticleUsage`、`ProcessRun` | 按需 |
| F7-07 | 化学品或耗材上机、卸下和寿命校验，按设备及使用区间定位受影响 Run。 | `MaterialMount`、`MaterialLot`、`Equipment`、`ProcessRun` | 按需 |

## F8-`MaterialConsumptionInventory` 物料、耗用与库存

| ID | 功能与验收要点 | 主要对象 | 阶段 |
| --- | --- | --- | --- |
| F8-01 | 登记演示用硅片来源、物料及来料批号；建片时支持引用该来源并从 Wafer 追溯。 | `Material`、`MaterialSource`、`MaterialLot`、`Wafer` | 首版 |
| F8-02 | 管理供应商、物料类别、单位及追踪粒度，登记到货与分装批次，查询供应商批号、失效日期和冻结/检验状态；可用量按明确的库存主系统核算。 | `Company`、`MaterialType`、`Material`、`MaterialSource`、`MaterialLot`、`MaterialSublot`、`IncomingInspection`、`MaterialHold` | 按需 |
| F8-03 | 按路线版本或步骤维护 BOM/用料要求，按片、加工批或其他已确认基数算需求。 | `BomItem`、`RouteVersion`、`RouteStep` | 按需 |
| F8-04 | 物料预留、拣选、领用、退料、调整和实际耗用记录。可从物料批追到 Run，处理并发防超领与受控更正。 | `MaterialAllocation`、`AllocationLotPick`、`MaterialConsumption`、`ProcessRun` | 按需 |
| F8-05 | 仓库与库存余额、出入库及转移，单据过账、冲销、流水和对账。启用前确认 MES/ERP 账本主从，保证余额与流水一致、重试不重复记账。 | `Warehouse`、`WarehouseStock`、`StockMovement`、`StockDocument`、`StockDocumentLine` | 按需 |

## F9-`TraceabilityImpactAnalysis` 多维追溯与影响分析

| ID | 功能与验收要点 | 主要对象 | 阶段 |
| --- | --- | --- | --- |
| F9-01 | 按工单、Lot、Wafer、载具或设备编号定位并下钻。历史按当时归属查询，可查看当前状态和完整时间线。 | `WorkOrder`、`Lot`、`Wafer`、`Carrier`、`Equipment`、`LotMembership` | 首版 |
| F9-02 | 正反查拆合批、逐片加工、实际设备与 Recipe、人员、量测、Hold 和处置。重复到访分别展示，来源可打开。 | `GenealogyDetail`、`StepExecution`、`ProcessRunWafer`、`ProcessRun`、`Measurement`、`HoldRecord`、`WaferDisposition` | 首版 |
| F9-03 | 按时间段、设备、配方版本、物料批、光罩或异常反查影响 Lot/Wafer。已启用的关联域给出证据范围，缺失记录在结果中标注。 | `ProcessRun`、`RecipeVersion`、`MaterialConsumption`、`MaterialMount`、`ReticleUsage`、`Issue` | 按需 |
| F9-04 | 追溯结果按授权导出，标注查询范围、生成时间和关键记录 ID；按大数据量需求提供后台导出。 | 追溯读模型、`AuditLog`；后台导出任务结构按需 | 按需 |

## F10-`WorkbenchDashboardsAlertsScheduling` WebUI、看板、告警与排程

| ID | 功能与验收要点 | 主要对象 | 阶段 |
| --- | --- | --- | --- |
| F10-01 | 角色工作台与菜单搜索；管理、工艺、计划、操作员、质量、设备、追溯和审计入口，复用列表分页、筛选、排序、状态与错误反馈。 | F1-`OrganizationIdentityAccessControl`–F9-`TraceabilityImpactAnalysis` 查询与权限 | 首版 |
| F10-02 | 生产总览显示 WIP、待加工、运行、Hold、设备及完工统计；按厂区、区域、工序、设备组过滤并下钻。 | `Lot`、`StepExecution`、`HoldRecord`、`ProcessRun`、`EquipmentState` | 首版 |
| F10-03 | 待办和告警视图显示当前阻塞、异常、设备停机等来源记录。授权实时刷新，断线重连后重取快照。 | `Issue`、`HoldRecord`、`Downtime`、`BusinessEvent`、`OutboxMessage` | 首版 |
| F10-04 | 按产品、步骤、日期和设备查历史产出、等待时间、周期、报废及不合格趋势，启用班次后增加班次维度。区分过站通过片次与终点完工片数。 | `WorkOrder`、`StepExecution`、`ProcessRun`、`ScrapEntry`、`Shift` | 首版 |
| F10-05 | 班次监控、OEE、瓶颈及损失分析；确认计划时长、理想产速、合格量与缺失值口径后上线。 | `Shift`、`EquipmentState`、`Downtime`、`ProcessRunWafer`；指标读模型待设计 | 按需 |
| F10-06 | 拖拽排产、维护冲突校验、日历容量、优先级规则和自动派工；保留排程变更及人工干预。 | `WorkOrder`、`Lot`、`MaintenanceEvent`；计划块、日历、规则及快照待补 | 扩展 |
| F10-07 | 个人告警订阅、确认、静默、升级和通知渠道；只读来源待办可先运行。 | `BusinessEvent`、`OutboxMessage`；订阅与告警生命周期待补 | 扩展 |

## F11-`DataImportEquipmentIntegration` 数据导入、设备及外部系统集成

| ID | 功能与验收要点 | 主要对象 | 阶段 |
| --- | --- | --- | --- |
| F11-01 | 模拟设备事件入口，保留来源、发生及接收时间，去重并处理乱序或无效转换。设备事件经应用规则进入状态履历。 | `MachineConnection`、`MachineMessage`、`EquipmentEvent`、`EquipmentState` | 首版 |
| F11-02 | 管理外部系统、连接、测点和信号映射；查看连接健康、原报文、解析结果、错误及重放，使用密钥引用与限权 API Key。 | `IntegrationConfig`、`MachineConnection`、`MachineTag`、`MachineMessage`、`ApiKey` | 按需 |
| F11-03 | 批量导入主数据或工单。字段映射、预检、错误定位、确认导入和幂等重跑，复用正常业务校验。 | `ImportMapping`、`ImportJob`、`CommandReceipt` | 按需 |
| F11-04 | Fab 标签与扫码，管理 Lot/载具标签模板和打印，扫码用于选定对象并核对当前身份；打印驱动和现场编码待定。 | `LabelTemplate`、`NumberSequence`、`Lot`、`Carrier` | 按需 |
| F11-05 | 自定义非核心字段，定义类型、验证和适用对象，支持显示与检索；按对象现有扩展字段能力决定是否补值模型。 | `CustomFieldDefinition` | 按需 |
| F11-06 | ERP、WMS、QMS 数据交换和签名 Webhook；确认主数据权威源、映射、ACK、重试、对账与外部错误处理。 | `IntegrationConfig`、`ApiKey`、`OutboxMessage`；外部契约和投递记录待定 | 按需 |
| F11-07 | 真机接入。按机台确认 SECS/GEM、GEM300 或其他协议范围，覆盖连接恢复、事件采集、载具与晶圆跟踪、加工任务联调。 | `MachineConnection`、`MachineMessage`、`EquipmentEvent`；驱动、联机状态和任务协议待设计 | 扩展 |
| F11-08 | 设备端 Recipe 校验/下发、AMHS/MCS 搬运及授权设备命令，按工艺启用确认、握手、超时、互锁和人工恢复。 | `RecipeVersion`、`CarrierMovement`；配方文件、搬运与命令状态待补 | 扩展 |

## F12-`ApplicationAuditReliability` 应用服务、审计与可靠性

| ID | 功能与验收要点 | 主要对象 | 阶段 |
| --- | --- | --- | --- |
| F12-01 | 建设共享 Application 命令与查询、版本化 API 和契约，统一校验、分页、追踪 ID、错误及并发冲突反馈。 | 各业务实体、Identity、EF Core `xmin` | 首版 |
| F12-02 | 高风险写命令按权限、Fab、状态、幂等键及请求摘要校验。并发冲突返回可恢复反馈，失败回滚；同键异参拒绝。 | `CommandReceipt`、`TrackEvent`、`GenealogyEvent`、`xmin` | 首版 |
| F12-03 | 同一事务提交业务事实、审计和待发事件。审计可按对象、操作人、时间及来源查找，关联请求和脱敏前后值。 | `AuditLog`、`BusinessEvent`、`OutboxMessage` | 首版 |
| F12-04 | Worker 拉取 Outbox、授权通知、失败重试、重复消费保护、失败查询与受控重放；提交后才通知客户端。 | `OutboxMessage`、`BusinessEvent` | 首版 |
| F12-05 | 作业文件、异常证据和附件上传、查看、授权下载及备份，保留关联与完整性校验。 | `Attachment`、`EngineeringDocument`、`StepDocument` | 按需 |
| F12-06 | 系统日志、请求关联、健康检查、依赖状态与运行指标；主数据停用与历史事实更正统一走授权入口。 | 运行日志、`AuditLog`、`BusinessEvent` | 首版 |

## F13-`AgentWorkbench` Agent 工作台

| ID | 功能与验收要点 | 主要对象 | 阶段 |
| --- | --- | --- | --- |
| F13-01 | WebUI 对话和对象侧栏，流式返回、取消、会话历史与失败反馈；允许从 Lot、Wafer、设备详情带入当前对象。 | Lot/Wafer/设备查询；Agent 会话与消息待建 | 首版 |
| F13-02 | 提供 Lot 状态、Wafer 履历、设备履历、Hold 清单等只读工具。答案带数据时间、业务来源链接并区分已知事实与解释。 | F6-`QualityExceptionDisposition`、F7-`EquipmentMaintenanceResources`、F9-`TraceabilityImpactAnalysis` 查询；工具调用记录待建 | 首版 |
| F13-03 | 以当前用户和 Fab 范围执行工具，限制参数、调用次数及超时，记录模型版本、工具摘要、耗时和来源关联。模型不可用时常规 MES 界面可继续使用。 | `UserFabScope`、`AuditLog`；模型配置与工具审计待建 | 首版 |
| F13-04 | 评测只读查询正确性、引用可回溯性、越权拒绝和故障降级；验证模型输出无法直接写生产数据或控制设备。 | 评测样例、Agent 审计、应用服务 | 首版 |
| F13-05 | 按权限检索 SOP 和历史知识，引用已授权版本，处理文档更新和撤回。 | `EngineeringDocument`、`Attachment`；索引及文档权限设计待补 | 扩展 |
| F13-06 | 生成建议单及参数预览，授权人确认后复核当前状态和权限，由同一应用命令执行并留痕。 | `AuditLog`、`CommandReceipt`；建议单、确认与审批记录待补 | 扩展 |

## F14-`DockerDeliveryAcceptance` Docker 交付与验收

| ID | 功能与验收要点 | 主要对象或组件 | 阶段 |
| --- | --- | --- | --- |
| F14-01 | Docker 交付 Web/API/Worker/PostgreSQL，同源访问、独立迁移任务、运行账号、持久卷、密钥配置和健康检查。 | 现有数据库迁移与 Compose；应用镜像及运行配置待建 | 首版 |
| F14-02 | 演示数据和初始化流程，账号、设备、工艺、Lot 与追溯数据可重复部署；在隔离环境执行端到端验收。 | 组织、工艺、执行、设备、质量与审计对象 | 首版 |
| F14-03 | 覆盖数据库事务与并发、API 权限、UI 过站和 Agent 工具测试；演练重启、备份及业务数据恢复，覆盖附件和密钥。 | 现有数据库测试；应用、UI 与恢复演练待补 | 首版 |
| F14-04 | 按部署目标确认性能、留存期限、可用性及 RPO/RTO；执行压测、归档和多节点恢复，覆盖启用的外部接口。 | 运行环境、指标与备份体系 | 按需 |

## 开工顺序与闭环验收

| 交付顺序 | 验收样例 |
| --- | --- |
| 1. 应用基础与工艺 | 两个不同权限账号可登录并获得正确数据范围；发布一套产品/路线/配方，旧批次继续使用原版本。 |
| 2. 批次、载具与过站 | 建一个 5 片、3 步演示 Lot；分配载具槽位并依序 Track-in/out。错误步骤、错误设备/配方和设备已占用均得到可解释的拒绝。重复提交与并发竞争只产生一次业务结果。 |
| 3. 质量与拆合批 | 在途 Lot 叠加两条 Hold，解除一条仍阻塞；完成受控收尾。将 Lot 拆成 2 片和 3 片，兼容位置合回，核对逐片血缘；另验证报废后有效归属、槽位与数量一致。 |
| 4. 看板、追溯与 Agent | 用同一个 Wafer 从原批、拆合批查到实际设备、配方、质量及处置。看板可下钻到同一事实；Agent 给出来源链接，越权查询被服务端拒绝。 |
| 5. 完整交付 | 在新环境启动镜像、完成迁移和演示闭环，验证重启及恢复后的历史和统计一致。 |

首版实现前需收敛三项规则。第一，选择具体工序、量测及载具样本，确定 Track-in/out 的现场时点和设备可用条件。第二，定稿 Lot/Wafer/Run 状态转换、数量口径、拆合批兼容矩阵、报废及加工中 Hold 收尾。第三，定义操作角色、独立审批、设备/人员资格及物料门禁，明确哪些按需项需要前移。

## OpenMES 沿用与范围取舍

- 沿用的界面组织包括总览卡片下钻、可筛选的工单与 WIP 列表、设备看板、按编号追溯、异常处置和审计。工艺段、产品修订、班次、维护和库存页面可参考其信息布局。见 [本地后台核查](01-current-state.md#4-本地-openmes-后台核查与沿用建议)。
- 对应关系为工单 → 生产需求，生产批次 → Lot，单件序列 → Wafer，工艺模板 → RouteVersion/RouteStep，产线终端 → 工位/终端与物理设备分开。沿用的是操作思路，Fab 过站、Recipe 匹配、晶圆身份及血缘按本项目规则实现。见 [Fab 字段取舍](04-fab-data-field-retention.md)。
- 范围外包括通用包装/EAN/托盘出货、工资、成本、客户营收和医疗 UDI。封测、Die 级 Wafer Map 与良率、跨厂协同按独立场景评估。单片血缘、配方管理及联机能力的行业对照见 [Siemens 半导体 MES](https://www.siemens.com/en-us/products/opcenter/execution/semiconductor/) 和 [SEMI 设备通信说明](https://www.semi.org/en/standards-watch-2022-Sept/intro-to-semi-communication-standards)。
