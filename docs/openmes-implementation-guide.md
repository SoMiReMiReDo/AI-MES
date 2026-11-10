# OpenMES 项目地图与核心调用链

源码位于 `/Users/seal/Desktop/myLog/program/SKhynix/OpenMes`。阅读基线为本地 `main` 的 `6048facedb99b112caecd9f60089e753a53ca89d`（2026-09-29），以该目录当时的工作树为准，其中已有本地修改和未跟踪文件。仓库地址见 [OpenMES](https://github.com/Mes-Open/OpenMes)。

OpenMES 把产品工艺与生产计划转成车间可执行的工序，再记录实际产量、物料和质量结果。先抓住三个层次。

| 对象 | 负责回答的问题 |
| --- | --- |
| 工单 `WorkOrder` | 要生产什么、计划多少、按哪份工艺执行？ |
| 批次 `Batch` | 这次执行归属哪张工单、目标数量是多少？ |
| 工序 `BatchStep` | 在这批中哪一步可以开始、哪一步已完成、数量怎样流转？ |

```text
产品 / BOM / 工艺配置 → 工单的工艺快照 → 批次及可执行工序
                                      ← 批次产量与完成状态 ← 工序执行结果
```

## 项目地图与阅读顺序

先用 [README 的功能总览](/Users/seal/Desktop/myLog/program/SKhynix/OpenMes/README.md:45) 建立术语，再按 1–3 顺着主链读。4–7 用于定位旁路和扩展。

| 顺序 | 区域 | 读到什么程度 |
| --- | --- | --- |
| 1 | 入口与身份 | 从 [Web 路由](/Users/seal/Desktop/myLog/program/SKhynix/OpenMes/backend/routes/web.php:266) 和 [API 路由](/Users/seal/Desktop/myLog/program/SKhynix/OpenMes/backend/routes/api.php:153) 找到操作员入口。Web 用 Laravel 会话和 Inertia，移动端通过 Expo 客户端调用 Sanctum API。看 [工单权限](/Users/seal/Desktop/myLog/program/SKhynix/OpenMes/backend/app/Policies/WorkOrderPolicy.php:32) 如何限制产线可见性 |
| 2 | 生产与存储 | `backend/app/Models/` 定位工单、批次、工序关系，`backend/database/migrations/` 查表结构。重点跟 [建单和建批](/Users/seal/Desktop/myLog/program/SKhynix/OpenMes/backend/app/Services/WorkOrder/WorkOrderService.php:173) 与 [工序执行](/Users/seal/Desktop/myLog/program/SKhynix/OpenMes/backend/app/Services/WorkOrder/BatchService.php:35) |
| 3 | 现场界面与测试 | 从 [操作员工单详情](/Users/seal/Desktop/myLog/program/SKhynix/OpenMes/backend/resources/js/Pages/operator/WorkOrderDetail.jsx:904) 和 [移动端工序请求](/Users/seal/Desktop/myLog/program/SKhynix/OpenMes/mobile/api/batchSteps.ts) 对照请求。先读工序测试名称，再挑一个成功和一个拒绝场景 |
| 4 | 排程与绩效 | [排程服务](/Users/seal/Desktop/myLog/program/SKhynix/OpenMes/backend/app/Services/Schedule/SchedulePlannerService.php:38) 处理工单时间、产线和冲突。班次看板和 [OEE 计算](/Users/seal/Desktop/myLog/program/SKhynix/OpenMes/backend/app/Services/Production/OeeCalculationService.php:24) 消费生产数据 |
| 5 | 物料、质量与 ERP | `Services/Material/` 处理分配、耗用和库存变化。`Services/Quality/` 有来料检验和生产质检。`Services/Warehouse/` 管理库存单据，ERP 通过 [单独的 API 路由组](/Users/seal/Desktop/myLog/program/SKhynix/OpenMes/backend/routes/api.php:113) 交换工单、主数据和库存数据 |
| 6 | 设备与实时数据 | `opcua-gateway/` 是 OPC UA 边车，MQTT 和 Modbus 由 Laravel 命令接入。设备值进入 [信号处理服务](/Users/seal/Desktop/myLog/program/SKhynix/OpenMes/backend/app/Services/Machine/MachineSignalIngestor.php:31)。网页实时读模型从 `app/Sync/` 和 `resources/js/lib/realtimeCollection.js` 看起 |
| 7 | 分发与扩展 | `mobile/` 是 Expo 客户端，`desktop/` 是 Tauri 外壳。`packages/ui/` 提供共享组件，`packages/module/` 提供前端模块契约。部署看 `docker-compose.yml`，服务端扩展看 `app/Extension/`、`app/Support/ModuleRegistry.php` 与 `backend/modules/` |

## 核心调用链

1. **建工单，接收工单。** API 的 `POST /api/v1/work-orders` 经过 `StoreWorkOrderRequest` 和控制器授权，进入 `WorkOrderService::createWorkOrder()`，保存待接收工单和工艺快照。主管 Web 的建单共用该服务。接收动作有两条路径，主管 Web 用 `WorkOrderService::applyTransition()`，API 用 `Api\V1\WorkOrderController::transition()`。读状态规则时按入口核对。[API 建单](/Users/seal/Desktop/myLog/program/SKhynix/OpenMes/backend/app/Http/Controllers/Api/V1/WorkOrderController.php:64) · [主管 Web 接收](/Users/seal/Desktop/myLog/program/SKhynix/OpenMes/backend/app/Http/Controllers/Web/Supervisor/WorkOrderController.php:225) · [API 状态变更](/Users/seal/Desktop/myLog/program/SKhynix/OpenMes/backend/app/Http/Controllers/Api/V1/WorkOrderController.php:190)
2. **从快照生成执行批次。** Web `POST /operator/batch` 与 API `POST /api/v1/work-orders/{workOrder}/batches` 汇合到 `WorkOrderService::createBatch()`。它在工单锁内读取快照、创建批次和工序，再将符合前置条件的工序置为 `READY`。快照保存当时的生产配置，源码还支持专门的 BOM 或配置变更路径。[建批服务](/Users/seal/Desktop/myLog/program/SKhynix/OpenMes/backend/app/Services/WorkOrder/WorkOrderService.php:695)
3. **操作员开始和完成工序。** Web 的 `/operator/batch-step/{batchStep}/start|complete` 与 API 的 `/api/v1/batch-steps/{batchStep}/start|complete` 各自处理请求，最终调用 `BatchService::startStep()` 或 `completeStep()`。服务在事务内锁定并复查生产状态，按场景检查工序、工位、文件、质检和物料等约束，写入执行结果。[Web 控制器](/Users/seal/Desktop/myLog/program/SKhynix/OpenMes/backend/app/Http/Controllers/Web/Operator/BatchController.php:63) · [API 控制器](/Users/seal/Desktop/myLog/program/SKhynix/OpenMes/backend/app/Http/Controllers/Api/V1/BatchStepController.php:24) · [完成工序](/Users/seal/Desktop/myLog/program/SKhynix/OpenMes/backend/app/Services/WorkOrder/BatchService.php:129)
4. **推进工序并回写产量。** 完成工序后，`BatchService` 更新批次、推进后续就绪工序。整批流转（`whole_batch`）在批次完成时结算产量，工单汇总已完成批次的产量。按量流转（`transfer`）记录工序间的数量，末工序通过量可在批次关闭前实时汇总。工单状态由产量、批次状态和生产阻塞等条件决定。[流转模式](/Users/seal/Desktop/myLog/program/SKhynix/OpenMes/backend/app/Support/ProductionFlow.php) · [批次结算](/Users/seal/Desktop/myLog/program/SKhynix/OpenMes/backend/app/Services/WorkOrder/BatchService.php:608) · [工单状态汇总](/Users/seal/Desktop/myLog/program/SKhynix/OpenMes/backend/app/Services/WorkOrder/WorkOrderService.php:798)

### 三条旁路

- **物料与 ERP。** 工序执行按时点触发物料分配或耗用。工单进入 `DONE` 后可由 `WorkOrderCompleted` 监听器在仓储启用时生成库存草稿，过账后的单据可供 ERP 拉取并确认。ERP 入站工单则走自己的导入服务。[物料服务](/Users/seal/Desktop/myLog/program/SKhynix/OpenMes/backend/app/Services/Material/MaterialAllocationService.php) · [完工单据监听器](/Users/seal/Desktop/myLog/program/SKhynix/OpenMes/backend/app/Listeners/GenerateWorkOrderStockDocuments.php) · [ERP 导入入口](/Users/seal/Desktop/myLog/program/SKhynix/OpenMes/backend/app/Http/Controllers/Api/V1/Erp/WorkOrderImportController.php:19)
- **设备。** OPC UA 边车把订阅值送到 `MachineGatewayController::ingest()`，再交给 `MachineSignalIngestor`。后者按信号类型分派工位状态、计数、遥测和告警，计数是否进产量还要看通道配置和归属。[边车入口](/Users/seal/Desktop/myLog/program/SKhynix/OpenMes/opcua-gateway/index.js:43) · [信号分派](/Users/seal/Desktop/myLog/program/SKhynix/OpenMes/backend/app/Services/Machine/MachineSignalIngestor.php:31)
- **实时界面。** 浏览器从 Web 路由 `GET /api/collections/{name}` 取快照，再用 Reverb 接收 `CollectionChanged` 增量。广播在事务提交后派发。[快照入口](/Users/seal/Desktop/myLog/program/SKhynix/OpenMes/backend/routes/web.php:181) · [提交后广播](/Users/seal/Desktop/myLog/program/SKhynix/OpenMes/backend/app/Sync/CollectionBroadcaster.php:198)

## 源码索引

先用这些路径定位行为。重建时保留相应源码，等当前里程碑通过再对照实现。

| 练习 | 对照源码 | 现有行为用例 |
| --- | --- | --- |
| M1 工单与接收 | `WorkOrder`、`WorkOrderPolicy`、两个入口的工单控制器 | [WorkOrderTest](/Users/seal/Desktop/myLog/program/SKhynix/OpenMes/backend/tests/Feature/Api/WorkOrderTest.php) · [WorkOrderTransitionsTest](/Users/seal/Desktop/myLog/program/SKhynix/OpenMes/backend/tests/Feature/Api/WorkOrderTransitionsTest.php) |
| M2 快照与建批 | `WorkOrderService::createWorkOrder()`、`buildProcessSnapshot()`、`createBatch()` | [建单生成快照](/Users/seal/Desktop/myLog/program/SKhynix/OpenMes/backend/tests/Feature/Api/WorkOrderTest.php:68) · [WorkOrderServiceTest](/Users/seal/Desktop/myLog/program/SKhynix/OpenMes/backend/tests/Unit/Services/WorkOrderServiceTest.php) |
| M3–M4 工序、结算和入口 | `BatchStep`、`Batch`、`BatchService`、工序路由与控制器 | [BatchStepTest](/Users/seal/Desktop/myLog/program/SKhynix/OpenMes/backend/tests/Feature/Api/BatchStepTest.php) |
| M5–M7 按量流转、物料和质量 | `BatchService::recordQuantity()`、`MaterialAllocationService`、`QualityTriggerService` | [BatchServiceTest](/Users/seal/Desktop/myLog/program/SKhynix/OpenMes/backend/tests/Unit/Services/BatchServiceTest.php) · [物料批次追溯](/Users/seal/Desktop/myLog/program/SKhynix/OpenMes/backend/tests/Feature/Api/V1/MaterialLotGenealogyTest.php) · [QualityControlTriggerTest](/Users/seal/Desktop/myLog/program/SKhynix/OpenMes/backend/tests/Feature/Quality/QualityControlTriggerTest.php) |

## 检查问题

1. 为什么工单保存工艺快照，批次还要保存自己的工序？
2. 第二道工序提前开始时，谁负责授权，谁决定前置条件是否满足？
3. 先完成一道工序、再完成整个批次时，分别有哪些状态和数量发生变化？
4. 为什么实时看板应在事务提交后接收增量？如果一次完工请求重发，哪里能判断是否已处理？

## 最小验证

有已运行的本地 OpenMES 实例时，可执行 `curl -fsS http://127.0.0.1/api/health`，按实际监听地址修改 URL。预期 HTTP 200，JSON 中 `status` 为 `ok`，并带有 `timestamp`。这一步只验证 HTTP 入口。

最小的生产行为验证选现有 `BatchStepTest`，覆盖先后顺序、批次完工、工单计产与跨产线访问。运行时使用隔离的测试环境，按仓库 CI 准备 PHP 8.3、Composer 依赖和应用测试配置，确认数据库为 `sqlite` / `:memory:`，并且没有旧的配置缓存或环境变量覆盖。仓库的测试环境定义见 [phpunit.xml](/Users/seal/Desktop/myLog/program/SKhynix/OpenMes/backend/phpunit.xml:21) 和 [CI 配置](/Users/seal/Desktop/myLog/program/SKhynix/OpenMes/.github/workflows/tests.yml:13)。

```bash
cd /Users/seal/Desktop/myLog/program/SKhynix/OpenMes/backend
php artisan test tests/Feature/Api/BatchStepTest.php --stop-on-failure
```

本次核验了源码与启动和测试入口。HTTP 验证跳过，本轮仅整理文档，未启动实例。PHPUnit 跳过，当前 shell 找不到 PHP 或 Composer，且 `backend/vendor/autoload.php` 缺失。继续学习见 [重建里程碑与互动规则](LEARNING_PATH.md)。
