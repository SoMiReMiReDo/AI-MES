# 领域对象与数据归属

先区分配置、计划、执行和事实数据，才能理解为什么同一工艺会在多个对象中出现。

## 使用逻辑

### 配置变更时判断影响范围

假设工单 A 已用「组装 → 测试」建立了批次。工程人员把模板改为「组装 → 校准 → 测试」时，先查 A 自己的工艺快照、批次版本和已开始的工序。原批次仍保留生成时的执行对象。后续工单能否采用新工艺，取决于建单时的产品、模板和版本选择。

| 需求 | 从哪个对象入手 | 要核对的结果 |
| --- | --- | --- |
| 修改以后建单用的标准工艺 | 工艺模板与步骤 | 新建工单的快照包含新工序 |
| 修改已下达工单的工艺 | 工单的 BOM 选择或工程变更入口 | 是否具备应用条件、哪个版本何时生效 |
| 查询已经生产的批次 | 批次、工序、已记录数量 | 保留当时采用的版本与执行事实 |
| 解释某物料的用途 | 快照内 BOM、分配和批次追溯 | 区分配方要求、实际分配和消耗 |

看到批次 DONE 时，可继续查末道步骤和工单累计产量。一张目标 100 件的工单可以分批生产。完成一批只表示该次执行结束，工单仍可能等待余量补产。

## 技术逻辑

### 四层数据

| 层次 | 核心对象 | 保存什么 |
| --- | --- | --- |
| 配置 | ProductType、ProcessTemplate、TemplateStep、BOM | 可复用生产配置 |
| 计划 | WorkOrder | 产品、目标数量、产线、工艺快照及版本 |
| 执行 | Batch、BatchStep | 一次生产批及其实际执行工序 |
| 事实 | ScrapEntry、物料耗用、检验、MachineCounterReading | 实际发生的数量、使用和异常 |

关系主干为 ProcessTemplate → WorkOrder.process_snapshot → Batch → BatchStep。工单有多个批次，批次有多个工序；批次的 snapshot_version 记录它采用哪个配置版本。

### 为什么保留快照

createBatch 不直接把当前模板当作执行事实，而从工单的 process_snapshot 建工序。因此模板日后变化不会自动改写已有批次。专门的 BOM 选择或变更请求仍可能更新工单配置，不能把快照理解为“永远不可修改”。阅读 WorkOrderSnapshotService、ChangeRequestService 时，应核对变更范围、停产条件和版本归属。

### 状态归属

| 对象 | 重要状态 | 谁推进 |
| --- | --- | --- |
| 工单 | PENDING、ACCEPTED、IN_PROGRESS、BLOCKED、PAUSED、CHANGE_HOLD、DONE、REJECTED、CANCELLED | 显式状态动作与 WorkOrderService 汇总 |
| 批次 | PENDING、IN_PROGRESS、DONE、CANCELLED | BatchService 聚合工序结果 |
| 工序 | PENDING、READY、IN_PROGRESS、DONE、SKIPPED | 前置条件、开工、完工、可选路线 |

这些是状态集合，不代表任意相邻状态都可自由互转。工序 canStart 还会检查前序；批次完成与工单完成也不是同一事件。

### 数量归属

- target_qty 是批次目标，produced_qty 是批次产出，两者不能直接等同。
- passed_qty 与 scrap_qty 归属具体工序；中间工序通过量不是成品数量。
- transfer 模式下，批次成品取最后有效工序通过量，工单汇总非取消批次。
- whole_batch 模式在完批时结算，使用提交的 produced_qty 或批次目标量，再汇总 DONE 批次。分析报废时要核对调用端是否显式传入完工量。

### 读数据库的方法

先读模型关系和 casts，再检索对应迁移。字段约束、外键、唯一性和后续迁移共同构成真实结构。演示 seeder 只描述一种场景，不应当作生产规则。

### 快照如何落到执行数据

建单的事务先按产品与 BOM 选择组装 process_snapshot，还可并入产品版本及工程资料。建批读取工单中的快照，写入 snapshot_version，再把快照步骤展开为 BatchStep。步骤编号、工位、可选属性等进入执行记录。遇到「产品页面已变，工序页面没变」，要对照这三份数据各自保存的内容。

### 已下达工单的变更

ChangeRequestService::apply 会锁定变更申请和关联工单，检查申请状态、工单是否停工或尚未开始、拟修改数量是否低于已生产量，并校验生效点。应用时记录变更前值、创建快照版本，再按生效点处理尚未开始的执行数据。排查时可按「变更申请 → 生效点 → 工单快照版本 → 批次版本 → 实际步骤」逐层比对。

数据库排查可从 work_orders.id 关联 batches.work_order_id，再用 batch_steps.batch_id 找执行记录。只按工单号和步骤名比对容易混入多个批次。检查实际模板与执行差异时，应保留三个对象的 ID。

## 核心字段与数据契约

下面是开发常用字段索引，不是完整表结构。类型、默认值、索引和外键最终以迁移为准；模型 fillable 只说明允许批量赋值的字段，不能证明该字段可被所有接口写入。

| 对象 / 字段 | 含义 | 修改时的约束 |
| --- | --- | --- |
| WorkOrder.order_no | 对外可识别工单号 | 与数据库 ID 分开；导入匹配先读策略 |
| planned_qty / produced_qty | 计划量与已汇总实产 | 计划修改不能替代报数；实产沿生产服务更新 |
| line_id / product_type_id | 生产归属与产品 | 同时影响授权、工艺、设备和筛选 |
| process_snapshot / snapshot_version | 工单采用的工艺内容及版本 | 通过快照和工程变更流程维护 |
| planned_start_at / planned_end_at | 计划时间窗口 | 参与排程和开工门槛，区别于实际时间 |
| counting_source | operator / machine / both | 与生产流转模式是两个独立维度 |
| Batch.target_qty / produced_qty | 本批目标与结算产量 | 不等于工单目标，也不等于每站通过量之和 |
| Batch.batch_number / lot_number | 批次序号与批号 | 批号、数据库主键、单件序列号不能互换 |
| Batch.snapshot_version | 建批采用的工单版本 | 用来区分变更前后的生产记录 |
| BatchStep.passed_qty / scrap_qty | 本工序累计通过与报废 | 下游输入依赖上游通过，不能任意覆盖 |
| BatchStep.step_number / status | 顺序和执行状态 | 参与有效前序、可开工及完批判断 |

Batch 的 target_qty、produced_qty 和 scrap_qty 在模型中使用 decimal:2。客户端不要依赖 JSON 数字一定是 JavaScript number，应检查接口实际序列化形状；展示精度和业务计算精度也应分开。

## 状态的层级与解释

| 层级 | 代码中的状态 | 开发时如何解释 |
| --- | --- | --- |
| 工单 | PENDING、ACCEPTED、IN_PROGRESS | 待处理、已接收与执行中；具体转换看入口和服务 |
| 工单 | BLOCKED、PAUSED、CHANGE_HOLD | 阻塞、暂停、工程变更等待；恢复条件并不相同 |
| 工单 | DONE、REJECTED、CANCELLED | 模型定义的终态集合 |
| 批次 | PENDING、IN_PROGRESS、DONE、CANCELLED | 一次生产执行的生命周期 |
| 工序 | PENDING、READY、IN_PROGRESS、DONE、SKIPPED | 前置条件、执行与路线选择的状态 |

READY 表示前序条件允许进入候选，并不保证点击开工一定成功；服务还会复核工单、工位、物料和质量条件。批次所有步骤处于 DONE 或 SKIPPED 也不一定足够：allStepsComplete 要求每个 variant_group 至少有一个 DONE，避免把整组路线全部跳过当成完成。

不要由这张状态清单推导任意两个状态之间都允许转换。变更状态必须使用对应业务动作，并核对 Web 与 API 的转换入口。

## 工艺变更的开发场景

以工单版本 v1 的两道工序为例，批次 A 已开始，批次 B 尚未创建。若新工艺增加一道校准步骤，应先判断需求是修改后续新工单的模板，还是变更当前工单，再检查工程变更服务的允许状态和应用条件。

验收时保留三份证据：变更前工单快照、应用后的工单版本、A 与新建 B 各自的步骤及 snapshot_version。不能通过更新模板后刷新页面来证明已生效，也不能批量重建历史 BatchStep 来“统一版本”。

## 新增字段的实现步骤

1. 确定字段属于标准配置、工单快照、执行状态还是事件事实。历史必须重现的配置需要考虑快照复制。
2. 新增迁移并考虑既有行的空值或回填；不要重命名已合并迁移来改变历史。
3. 更新模型 casts、关系与必要的 fillable，随后更新真正接收输入的 Request。
4. 在领域服务中实现跨字段约束，核对多个入口是否复用同一逻辑。
5. 更新详情、列表、实时集合、导出和审计中实际需要该字段的路径。
6. 验证旧数据读取、新数据创建、修改拒绝、跨租户访问和历史快照。

常见遗漏是只修改数据库和表单，导致实时页面在首次加载有值、收到广播后丢值；这个问题要沿第 07 章同时检查两份投影。

## 源码依据

- [backend/app/Models/WorkOrder.php:37](/Users/seal/Desktop/myLog/program/SKhynix/OpenMes/backend/app/Models/WorkOrder.php:37)
- [backend/app/Models/Batch.php:13](/Users/seal/Desktop/myLog/program/SKhynix/OpenMes/backend/app/Models/Batch.php:13)
- [backend/app/Models/BatchStep.php:349](/Users/seal/Desktop/myLog/program/SKhynix/OpenMes/backend/app/Models/BatchStep.php:349)
- [backend/app/Services/WorkOrder/WorkOrderService.php:695](/Users/seal/Desktop/myLog/program/SKhynix/OpenMes/backend/app/Services/WorkOrder/WorkOrderService.php:695)
- [backend/app/Services/WorkOrder/WorkOrderSnapshotService.php:23](/Users/seal/Desktop/myLog/program/SKhynix/OpenMes/backend/app/Services/WorkOrder/WorkOrderSnapshotService.php:23)
- [backend/app/Services/WorkOrder/ChangeRequestService.php:28](/Users/seal/Desktop/myLog/program/SKhynix/OpenMes/backend/app/Services/WorkOrder/ChangeRequestService.php:28)
- [建单如何构造工艺快照](/Users/seal/Desktop/myLog/program/SKhynix/OpenMes/backend/app/Services/WorkOrder/WorkOrderService.php:380)
- [变更应用、锁与快照版本](/Users/seal/Desktop/myLog/program/SKhynix/OpenMes/backend/app/Services/WorkOrder/ChangeRequestService.php:216)
- [工单变更 API 测试](/Users/seal/Desktop/myLog/program/SKhynix/OpenMes/backend/tests/Feature/Api/V1/WorkOrderChangeRequestApiTest.php:19)
- [backend/app/Models/WorkOrder.php:104](/Users/seal/Desktop/myLog/program/SKhynix/OpenMes/backend/app/Models/WorkOrder.php:104)
- [backend/app/Models/Batch.php:100](/Users/seal/Desktop/myLog/program/SKhynix/OpenMes/backend/app/Models/Batch.php:100)
