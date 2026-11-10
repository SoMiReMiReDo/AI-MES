# 物料、质量与仓储闭环

生产会触发分配、耗用和检验；仓库草稿与正式库存过账属于另外的业务阶段。

## 使用逻辑

### 一次生产涉及四次业务确认

假设本批需要 20 个零件。主管先核对工单快照里的 BOM、消耗时点和可用量。首次开工时，若开启批号追踪，操作员可在支持的入口查看建议领用批号，并确认或指定实际批号。发生质量检验时，质量人员记录样本结果。工单结束后，仓库人员检查对应库存单据，再按流程过账。

| 当前看到的状态 | 应检查的证据 | 现场动作 |
| --- | --- | --- |
| 可用量不足 | 物料、需求量、库存与已保留量、允许负库存或缺料停工设置 | 核对库存和领用批号，解决短缺后重试 |
| 质检待处理 | 任务对应批次、触发规则、是否阻塞 | 录入样本并查看判定 |
| 质检已结束但生产仍被阻塞 | 质量结果与关联异常单 | 按权限处理异常和处置流程 |
| 工单已完、仓库显示草稿 | 单据行、仓库、物料批号、单据状态 | 核实后过账，再与外部系统对账 |

在追查某个批次的用料时，保留物料编码与批号。两个批号的同一种物料可能有不同的库存与追溯结果。

## 技术逻辑

### 物料在何时介入

| 时点 | BatchService 调用 | 含义 |
| --- | --- | --- |
| 批次第一次进入生产 | allocateForBatch | 开工时点或默认 BOM 行 |
| 指定工序开始 | allocateForStep | consumed_at=during 的行 |
| 批次完成 | allocateForBatchEnd | consumed_at=end 的行 |
| 完批结算 | consumeForBatch | 将相关分配确认为耗用 |

MaterialAllocationService 协调分配与耗用，LotPickingService 负责选批逻辑，StockMovementService 负责物料库存变动。追溯不仅要知道“用了哪种材料”，还要关联物料批、生产批和工序。

### 质量如何阻塞生产

QualityControlTask 的未完成阻塞任务会阻止相应开工；transfer 的 productionBlocker 也会参与数量与执行校验。QualityTriggerService 在进入生产和完批数量等节点创建任务。来料检验、物料 hold、异常处置由 Quality 下的独立服务处理。

分析一个“不能开工”问题时，分别检查工单 hold、阻塞式质量任务、工序前置条件、工位路由和可选缺料阻塞。页面禁用提示不是最终规则所在。

### 完工到库存

```text
WorkOrderCompleted
 → GenerateWorkOrderStockDocuments
 → 仓储模块 / 自动生成开关 / 仓库条件
 → WorkOrderStockDocumentService 生成草稿
 → StockDocumentService.post 过账
 → 仓库余额 + 物料库存流水 + 批次数量
 → ERP 拉取与确认
```

监听器异常被捕获并写警告，避免库存草稿生成失败直接破坏工单完工。由此可能出现“工单已完成，但库存草稿缺失”，排障需要查日志和补偿路径。

### 库存写入规则

StockDocumentService 把草稿、过账、取消分开。post 在锁住单据后检查状态，已过账的单据直接返回，避免重复扣增库存。取消已过账单据通过反向操作处理，而不是手改余额。

库存草稿不是已入库；ERP ack 也不应被当成生产完成动作。是否启用仓储、是否自动生成以及单据是否过账，都影响最终结果。

### 领用失败的事务范围

allocateMatching 从工单快照读取 BOM，按当前时点筛选，用批次目标量计算需求。分配时产生库存变动和保留量，再建立 MaterialAllocation。批号追踪开启后，服务使用指定批号或自动选批。如果批号校验失败，事务回滚相关库存与分配记录。完批处理后，具备工序归属与已选批号的分配可写入 BatchStepLotConsumption 供追溯查询。

### 检验结果与库存结果的落点

performTask 对质检任务行加锁，复核任务仍开放，再创建质量检查结果。结果失败时可关联异常单，任务本身仍记录为 DONE。因此排查停工要同时看任务终态与异常单是否还在阻塞。

库存单据过账则由 StockDocumentService::post 锁住单据并检查状态。验收时用同一个单据 ID 对照草稿、首次过账、重复提交、取消后的余额与流水。ERP 确认发生在单据流程上，应与质量任务和 MES 完工记录分别核对。

## 四本账如何对齐

| 账本 | 回答的问题 | 对账主键或维度 |
| --- | --- | --- |
| BOM / 工艺快照 | 理论上需要什么、何时消耗 | 工单版本、物料、消耗时点 |
| MaterialAllocation | 本批实际分配与耗用什么 | 批次、物料、所选物料批 |
| 检验与异常 | 检验发生了什么、是否仍阻塞 | 任务、结果、关联异常单 |
| StockDocument / 库存流水 | 哪张单据导致库存变化 | 单据 ID、仓库、物料或产品、批号 |

四本账之间有关联，但状态不等价。质量任务 DONE 只表示检验任务处理完；工单 DONE 不表示仓库过账；分配记录存在也不能单独证明 ERP 已确认入账。

## 从 BOM 到实际耗用的核对方法

对一个简单无损耗系数的示例，单位成品需要物料 M 两件，批次目标 10，则理论需求为 20 件。先在工单快照核对单位用量和 consumed_at，再观察对应开工、指定工序或完批动作产生的分配。实际项目若有单位换算、替代物料或其他需求规则，应按服务结果复算，不能直接套这个示例。

选批时同时记录物料 ID 和 lot ID，确认批号属于正确物料及租户，且符合可用性条件。自动建议的批号与现场实际投料不同，必须通过支持的指定批号流程修正，不能仅把显示文本改成实际批号。完成后再查耗用和 BatchStepLotConsumption 是否建立了对应关系。

## 质量任务与异常处置的生命周期

1. 配置触发条件和是否阻塞，确认作用范围覆盖测试工单或批次。
2. 触发生产节点后查任务是否创建，保留任务与批次关联。
3. 执行检验，记录样本、结果和判定，观察任务进入 DONE。
4. 若失败，继续查产生的异常单及其阻塞状态。
5. 按实际处置流程解决异常，重新验证生产入口能否继续。

开发新的质量触发器时，要区分“每次调用都触发”与“只在首次进入生产时触发”。重复点击开工、事务重试和重跑任务都可能造成重复检验，需要沿已有触发条件核对，而不是在前端去重。

## 仓储过账与补偿操作

验收从一张草稿开始，记录每行物料、数量、仓库和批号。首次 post 后检查单据状态、余额、流水；第二次对同一 ID post，应验证已有状态保护不会再生成同等库存变化。取消已过账单据要通过服务的反向处理，并保留原单与反向记录用于对账。

如果工单完成却没有草稿，先查仓储模块、自动生成设置、仓库条件及监听器警告。确认确实未生成后，再使用系统已有的补偿入口或经过验证的业务服务处理。直接重做生产完工可能重复触发其他副作用；直接插入单据也可能遗漏行、关联和审计。

## 开发与验收矩阵

| 修改 | 至少覆盖的场景 | 对照证据 |
| --- | --- | --- |
| 选批规则 | 自动选批、指定批号、无足够可用量 | 分配记录和库存变化一致 |
| 消耗时点 | start、during、end | 只有对应阶段的 BOM 行被处理 |
| 缺料停工 | 开关开 / 关 | 区分可选开工阻塞与真正分配失败 |
| 质量规则 | 合格、不合格、重复提交 | 任务、检查、异常单关系不混淆 |
| 库存服务 | 首次过账、重复过账、取消 | 余额与流水能解释每次变化 |
| 外部同步 | 已过账未 ACK、ACK 后、ACK 失败 | ERP 接收记录与 MES 单据可逐条核对 |

排查库存差异时固定同一单位和同一仓库范围，先列出完整流水再解释余额；不能拿全厂物料总量直接与某一个仓库或物料批余额比较。

## 源码依据

- [backend/app/Services/Material/MaterialAllocationService.php:18](/Users/seal/Desktop/myLog/program/SKhynix/OpenMes/backend/app/Services/Material/MaterialAllocationService.php:18)
- [backend/app/Services/Material/LotPickingService.php:21](/Users/seal/Desktop/myLog/program/SKhynix/OpenMes/backend/app/Services/Material/LotPickingService.php:21)
- [backend/app/Services/Quality/QualityTriggerService.php:32](/Users/seal/Desktop/myLog/program/SKhynix/OpenMes/backend/app/Services/Quality/QualityTriggerService.php:32)
- [backend/app/Listeners/GenerateWorkOrderStockDocuments.php:23](/Users/seal/Desktop/myLog/program/SKhynix/OpenMes/backend/app/Listeners/GenerateWorkOrderStockDocuments.php:23)
- [backend/app/Services/Warehouse/StockDocumentService.php:99](/Users/seal/Desktop/myLog/program/SKhynix/OpenMes/backend/app/Services/Warehouse/StockDocumentService.php:99)
- [backend/tests/Feature/Warehouse/WorkOrderStockDocumentTest.php:1](/Users/seal/Desktop/myLog/program/SKhynix/OpenMes/backend/tests/Feature/Warehouse/WorkOrderStockDocumentTest.php:1)
- [backend/tests/Feature/Quality/QualityControlTriggerTest.php:1](/Users/seal/Desktop/myLog/program/SKhynix/OpenMes/backend/tests/Feature/Quality/QualityControlTriggerTest.php:1)
- [物料分配事务与选批](/Users/seal/Desktop/myLog/program/SKhynix/OpenMes/backend/app/Services/Material/MaterialAllocationService.php:637)
- [质检完成与失败异常](/Users/seal/Desktop/myLog/program/SKhynix/OpenMes/backend/app/Services/Quality/QualityTriggerService.php:230)
- [物料分配与消耗测试](/Users/seal/Desktop/myLog/program/SKhynix/OpenMes/backend/tests/Unit/Services/MaterialAllocationServiceTest.php:22)
