# 生产主链与事务边界

从一次完成工序请求追到产量回写，分清入口授权、领域约束与数据库提交。

## 使用逻辑

### 在操作员界面复现一条生产链

选择一张分配给当前产线、已经接单且到达计划开工时间的工单，设定两道有明确顺序的步骤。用目标 10 件的独立批次复现时，记录工单 ID、批次 ID 和两道工序 ID，便于把页面变化和请求关联起来。

| 操作 | 要观察的结果 |
| --- | --- |
| 建批 | 出现新批次和按工单快照展开的工序，初始批次为 PENDING |
| 开始第一道工序 | 工序变为 IN_PROGRESS，批次首次进入生产。配置了开工物料时触发分配 |
| 尝试提前开启第二道 | 在整批流转且启用顺序检查时，应等待前序满足条件 |
| 完成第一道 | 该工序关闭并推进后续可执行步骤。继续观察批次和工单状态 |
| 完成末道 | 批次满足完成条件后结算，回到工单核对计划、实产和剩余量 |

遇到提示时，按所处阶段处理。工单尚未到开工时间，核对排程。工位不匹配，核对分派或登录工位。已经开始但无法关闭，逐项检查文件确认、清单、输出与实绩工时。打开质检任务时还要区分正在等待检验和检验失败后产生的异常单。

## 技术逻辑

### 请求执行链

```text
Web /operator/batch-step/{batchStep}/complete
或 API /api/v1/batch-steps/{batchStep}/complete
  → Controller / FormRequest：授权与输入形状
  → BatchService.completeStep：事务与生产规则
  → WorkOrder 行锁 → BatchStep 行锁 → 重新判断
  → 写工序结果 → 聚合批次 → 推进后续 READY 工序
  → 完批结算、物料耗用 → 工单状态汇总
  → 提交后实时广播 → 页面读模型更新
```

### 创建与接收

createWorkOrder 保存工单和工艺快照。接收动作的 Web 与 API 路径并不完全相同：主管 Web 使用服务的 applyTransition，API 控制器有自己的 transition。修改状态规则时，两条入口都需要核对。

createBatch 在工单行锁内刷新快照、分配批次序号并生成 BatchStep。同一 variant_group 选择默认步骤，否则选择较小 step_number，其余先标为 SKIPPED；随后 promoteReadySteps 推进符合前置条件的步骤。

### 开工 startStep

服务在事务中依次检查工单能否生产、transfer 阻塞、工位路由、阻塞质检及 canStart。若是批次首次开工且开启 hold_on_material_shortage，还检查物料短缺。

工序进入 IN_PROGRESS 后，服务更新批次状态，按配置分配开工或当前工序的物料，刷新工单状态，并在首次进入生产时触发质量任务。缺料阻塞开关默认关闭；不能把“系统存在库存”推导为“所有开工必然检查库存”。

### 完工 completeStep

| 检查 | 所保护的业务事实 |
| --- | --- |
| canComplete | 必须是进行中的工序 |
| completionBlocker | transfer 上游输入已关闭且本站无待处理数量 |
| blockingDocuments | 必须验证的文件已经确认 |
| 必填清单、类型输出、阅读确认 | 现场执行证据完整 |
| actual_setup + actual_run ≤ actual_elapsed | 人工确认工时内部一致 |

服务写入 DONE、时间、操作者和 passed_qty；然后更新批次并调用 finishBatchIfComplete。完批时分配 end 物料、结算产量、确认耗用并触发按单位数质检。

### 并发与重试

lockProductionStep 先锁工单再锁工序，避免并发报数时读到陈旧可用量与工单汇总。数据库事务负责多处写入的一致性，前端禁用按钮只能改善体验。

重复完成通常会因工序状态不再可完成而被拒绝，不应宣称整个 API 使用统一幂等键。不同动作的防重复策略不同：库存过账看状态，机器读数看 event_id，人工数量更正看 expected 值。

### 验证入口

优先读 BatchStepTest 与 BatchServiceTest，覆盖先后顺序、重复动作、跨产线、完批产量。测试的存在只能说明仓库提供这些行为契约；本次未运行 OpenMES 业务测试。

### 对照 Web 和 API 的返回

| 动作 | Web 的可观测结果 | API 的可观测结果 |
| --- | --- | --- |
| 完成工序 | 操作员控制器验证所选产线，回到原页并带成功或错误消息 | CompleteBatchStepRequest 检查工单访问权和参数形状，成功响应含更新后工序 |
| 服务拒绝完成 | 看页面 flash 与提交后的状态 | 控制器把捕获的异常变为 422，错误落在 errors.step |
| 校验或授权拒绝 | 查请求是否进入控制器与所选产线校验 | 查 Form Request 的 403 或字段校验 422，结合响应体判断失败层 |

验证一次回滚时，可构造首道工序开工而领料不足的场景，观察失败后工序、批次、库存分配是否一起保留原值。查请求日志时按操作时间和工序 ID 对齐。对「点击后无响应」的工位问题，先确认响应与实际写入，再判断界面是否缺少刷新。

## API 完工请求契约

普通 API 完工入口为 POST /api/v1/batch-steps/{batchStep}/complete，使用普通 v1 的认证路径。以下字段来自 CompleteBatchStepRequest；字段通过形状校验后，仍须满足服务内的工序状态、文件、清单、输出和工时约束。

| 字段 | 请求约束 | 业务解释 |
| --- | --- | --- |
| produced_qty | 可空、数值、至少 0 | 完工产量输入；实际计产取决于模式、计数源与完批分支 |
| actual_elapsed_minutes | 可空、非负整数 | 操作员确认的总历时 |
| actual_setup_minutes | 可空、非负整数 | 操作员确认的准备时间 |
| actual_run_minutes | 可空、非负整数 | 操作员确认的运行时间 |

下面是字段示例，不是自动创建生产对象的脚本；替换 ID 前要先准备一条满足完成条件的运行中工序。

```json
{
  "produced_qty": 10,
  "actual_elapsed_minutes": 25,
  "actual_setup_minutes": 5,
  "actual_run_minutes": 20
}
```

成功控制器响应包含 message 和 data，data 加载 completedBy 与 batch.workOrder。服务内异常由该控制器返回 422，响应包含 message 与 errors.step。FormRequest 的字段错误则按字段返回；不要让客户端只识别一个固定英文错误字符串。JSON 中把 run 改成 21，可用于构造准备时间与运行时间合计超过总历时的失败场景。

## 一次动作应检查哪些写入

| 动作 | 首要写入对象 | 联动检查 |
| --- | --- | --- |
| 建批 | Batch、BatchStep | 批次序号、工单快照版本、路线选择、初始 READY |
| 首次开工 | BatchStep、Batch | 操作者与开始时间、物料分配、质量触发、工单状态 |
| 分次报数 | BatchStep 数量 | 下游可用量、末站汇总、工单阻塞与上限 |
| 完成步骤 | BatchStep 状态与结果 | 下一步可执行性、是否已满足完批条件 |
| 批次结算 | Batch、WorkOrder | 物料耗用、质量任务、工单完成及库存草稿旁路 |

这张表用于建立验收断言，不意味着每种开关下都会触发全部副作用。调查某次请求时，要以实际经过的分支和提交结果为准。

## 事务、锁顺序与外部副作用

生产写路径先锁工单再锁工序，在锁内重读可用量并判断。新增一个会修改同一数量的入口时，必须对齐已有锁顺序；在加锁前读取的关系或可用量可能已经过时。仅给步骤加锁却在锁外更新工单汇总，仍可能丢失并发写入。

事务保证受其覆盖的数据库写入一起提交或回滚，不能自动撤销 HTTP 请求、设备命令或已发送通知。扩展生产服务时，先确认现有事件何时派发、监听器是否延迟到提交后、失败是否被捕获。新增外部调用的可靠投递与补偿是设计工作，不应假设项目已有通用消息事务机制。

## 关键回归场景

| 场景 | 准备 | 必须观察的结果 |
| --- | --- | --- |
| 重复完成 | 同一步骤第一次成功后再次提交 | 不再次结算数量或耗用，返回不可完成原因 |
| 并发报数 | 两个连接同时消费同一剩余量 | 总通过与报废不超过输入量 |
| 未到计划时间 | 工单 planned_start_at 在未来 | 相关生产入口拒绝，执行记录不被推进 |
| 必填证据缺失 | 缺文件确认、清单或类型输出之一 | 完工失败，补齐对应项后才可继续 |
| 分配中失败 | 构造触发分配失败的领料条件 | 事务覆盖的状态和分配无部分残留 |
| 提交后通知失败 | 数据已提交但广播不可用 | 通过重新读取确认事实，另查广播日志 |

回滚验证不能只断言 HTTP 失败；至少比较工序状态、批次状态、工单数量及该场景关联的分配记录。并发验证需要两个真实数据库连接，第 10 章说明默认 SQLite 测试环境的局限。

## 源码依据

- [backend/app/Http/Controllers/Api/V1/BatchStepController.php:48](/Users/seal/Desktop/myLog/program/SKhynix/OpenMes/backend/app/Http/Controllers/Api/V1/BatchStepController.php:48)
- [backend/app/Http/Requests/Api/V1/CompleteBatchStepRequest.php:15](/Users/seal/Desktop/myLog/program/SKhynix/OpenMes/backend/app/Http/Requests/Api/V1/CompleteBatchStepRequest.php:15)
- [backend/app/Services/WorkOrder/BatchService.php:35](/Users/seal/Desktop/myLog/program/SKhynix/OpenMes/backend/app/Services/WorkOrder/BatchService.php:35)
- [backend/app/Services/WorkOrder/BatchService.php:704](/Users/seal/Desktop/myLog/program/SKhynix/OpenMes/backend/app/Services/WorkOrder/BatchService.php:704)
- [backend/app/Services/WorkOrder/WorkOrderService.php:695](/Users/seal/Desktop/myLog/program/SKhynix/OpenMes/backend/app/Services/WorkOrder/WorkOrderService.php:695)
- [backend/tests/Feature/Api/BatchStepTest.php:1](/Users/seal/Desktop/myLog/program/SKhynix/OpenMes/backend/tests/Feature/Api/BatchStepTest.php:1)
- [backend/tests/Unit/Services/BatchServiceTest.php:1](/Users/seal/Desktop/myLog/program/SKhynix/OpenMes/backend/tests/Unit/Services/BatchServiceTest.php:1)
- [Web 建批 · 输入与选线](/Users/seal/Desktop/myLog/program/SKhynix/OpenMes/backend/app/Http/Controllers/Web/Operator/BatchController.php:467)
- [Web 完工 · 跳转与反馈](/Users/seal/Desktop/myLog/program/SKhynix/OpenMes/backend/app/Http/Controllers/Web/Operator/BatchController.php:126)
- [计划开工边界测试](/Users/seal/Desktop/myLog/program/SKhynix/OpenMes/backend/tests/Feature/WorkOrder/PlannedStartTest.php:21)
