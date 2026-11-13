# F3-`WorkOrderPlanningExecution` 工单与计划执行

日期：2026-11-11｜状态：范围已确认，业务设计待代码阶段验证；首版代码已实现，实际接口与验证见[实现说明](f2-f3-implementation.md)。

## 1. 范围与代码依据

本轮覆盖 F3-01 工单生命周期、F3-02 关联 Lot 与进度查询，包含下达所需的初始基线快照。F3-03 完整变更审批、F3-04 停复工、F3-05 客户履约专题预留，见[待补全清单](deferred-capabilities.md)。配套见[接口与验收](work-order-planning-execution-api-and-acceptance.md)。

已阅读 Production.cs 中 WorkOrder、WorkOrderSnapshot、WorkOrderChangeRequest、Lot、Wafer，以及 FabModel.Production.cs 的映射。当前已有字段、外键、非负数量与时间约束；尚无本模块业务 API/UI。WorkOrder.ProducedQty 和 Lot.ProducedQty 字段存在不代表生产统计已实现。

## 2. 工单规则

创建草稿需指定部署厂区、唯一工单号、产品及其修订、正整数计划片数、优先级与可选计划时间/交期；计划结束不得早于开始。建议优先级 0～100，数值越大越优先，作为本轮设计约定在代码阶段验证。产品与修订必须同属；工单号由 F2 编号服务或手工唯一编码提供。

| 状态/动作 | 条件与结果 |
| --- | --- |
| Draft 编辑 | 可改计划字段与产品修订，禁止写完成量、快照版本、完成时间；不提供物理删除 |
| Draft → Released | 产品、修订、路线处于启用/已发布且当前有效；原子写入第 1 版快照、工单状态和审计 |
| Draft/Released → Cancelled | 必填原因，无首次投放，关联批次/成员/执行/质量均已终结；取消不可恢复 |
| Released → Closed | 必填原因，所有关联 Lot/步骤/Run 终结，无 Active/Registered 片及有效质量阻塞，历史归属可核验 |
| Closed/Cancelled | 终态只读，不能修改计划或重新下达 |

Released 不允许直接修改计划或版本，变更留待 F3-03。首版不增加自动 Completed 状态：生产完成事实与行政关闭分开；CompletedAt 仅由后续可信生产完成判定写入，关闭时间记审计，必要时扩展独立字段。空工单允许有原因的关闭，不因此视为生产完成。

下达快照保存 schemaVersion、工单计划、产品修订、路线版本、步骤及定义的 ID/版本和必要内容、生成时间与操作者；Recipe 保存候选配置基线，不提前认定实际采用版本。WorkOrderSnapshot 为历史基线权威记录，ProcessSnapshot 如保留则为同事务镜像，SnapshotVersion 指向同一版本。历史快照不覆盖。

## 3. F3 真实生产进度依赖：F4/F5 完成后必须回补

早期只提供查询契约。2026-11-13 已随 F5 返回补全真实执行统计、缓存重算及关闭门禁；F6 完整质量处置仍待集成。覆盖与未验证项跟踪 [GAP-01](deferred-capabilities.md)及 [F5 验收](dispatch-and-production-execution-implementation.md)。

| 指标 | 目标口径 | 必要来源 |
| --- | --- | --- |
| 投放量 | 属于工单、去重后的已投放 Wafer 数；拆合批不重复累计 | F4 的稳定工单归属、成员历史与血缘 |
| 在制量 | 已投放且未完成/报废等终结的唯一 Wafer 数 | F4 归属与终结规则、F5 执行事实 |
| 阻塞量 | 在制中因门禁无法继续的唯一 Wafer 数，为在制子集，不与在制相加 | F5 门禁；质量 Hold 的完整语义还需 F6 |
| 完工量 | 归属明确且已完成绑定路线终点的唯一 Wafer 数 | F4 归属、F5 终点完成事实与幂等处理 |

不得累加过站数量或直接相加 Lot.ProducedQty；历史成员与当前成员混合联表必须去重，跨工单合批须由 F4 先定义归属规则。若无法确定归属，标注异常并阻止认定完成。返工重过终点不得重复计数；完成撤销规则待 F5 定义。

2026-11-13 F5 已回补真实进度：coverage=ProductionFacts 表示使用逐片实际投放/终点完成事实；历史关联或事实不完整返回 IncompleteHistory，指标为 null。missingDependencies 继续标注完整 F6 质量处置，不以该 coverage 宣称全部质量语义已实现。ProducedQty 不开放人工编辑，终点同步缓存并提供独立授权的重算入口。详情见 [F5 实现](dispatch-and-production-execution-implementation.md)。原 PendingIntegration/Verified 为早期设计状态，已由上述实际契约替换。

## 4. 服务、权限与页面

建议权限：planning.work-orders.read/manage/release/cancel/close；所有查询、分页、统计及按 ID 访问均校验工单 FabId，沿用 F1 会话与授权。主数据选择调用 F2 只读能力，工单服务不改工艺版本。WebUI 与未来 Agent 共享同一服务端校验。

写入要求 xmin/If-Match；下达、取消、关闭使用幂等键，状态、快照、审计、回执在同事务提交。取消/关闭与未来投批命令必须共享工单锁和状态校验，避免检查后产生关联 Lot。代码阶段需与 F4 的后续写入约定一并记录。

页面包括工单列表筛选（编号、产品、状态、交期）、草稿表单、工单详情（计划、基线、关联 Lot、进度、审计）。已下达字段只读；依赖未接通时明确展示待补全说明，不显示虚假完成率。

## 5. 数据差异与实施路径

已有 PlannedQty 数据库约束仅为非负，应用创建规则收紧为正数；Status 尚为自由字符串，需业务状态机。Wafer 没有直接 WorkOrderId，因此稳定完工归属不能仅凭当前 Lot 推断；F4/F5 阶段评估完成事实与归属快照的必要迁移。快照保护、幂等回执和工单终态约束在代码阶段核查并补齐。

建议顺序：复用 F2 有效版本查询 → 工单草稿 → 原子下达快照 → 取消/关闭 → 进度契约与测试夹具 → 页面与权限验收。真实进度集成另在 F4/F5 完成后回补。代码任务开始前单独确认计划。
