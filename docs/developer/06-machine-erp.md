# 设备信号与 ERP 集成

外部系统只提供输入；OpenMES 仍要决定读数属于谁、是否新鲜、是否可计入生产。

## 使用逻辑

### 设备接入验收

先选择一台设备、一个工位和一道运行中的工序。将计数通道绑定到这道工序，确认工单采用机器计数。给累计通道依次发送带时间戳的 500、503，并在两次读数之间检查读数明细与工序结果。假设来源未变且本站有至少 3 件可处理，500 建立基线，503 产生 3 件差值。若本站只有 2 件可处理，验收时应对照差值 3、应用量 2 和部分应用状态。

| 读数现象 | 应查看什么 | 如何继续 |
| --- | --- | --- |
| 通道有读数，应用量为零 | 是否只建立基线、未绑定、工序未开工或处于阻塞 | 核对 reading 状态和实际工序 |
| 累计值从 503 降到 0 | 通道重置状态与设备是否实际清零 | 确认来源和原因，再通过受控重建基线流程继续 |
| 出现重复上报 | 事件 ID、观察时间和读取记录 | 对照相同事件的原记录与实际入账量 |
| 应用量小于差值 | 工序可处理量与输入来源 | 检查上游交料或批次目标，保留未应用读数供复核 |

### ERP 交换验收

先核对产线和产品编码，在隔离环境导入一张可识别的工单，再模拟一批含错误编码的请求。记录每行创建、更新、跳过或出错的结果。重复导入前明确选用更新、跳过还是报错策略。出站验收另选一张已过账单据，在 ERP 落账后保存其单号，使用 ACK 写回 erp_reference。

## 技术逻辑

### 设备信号路径

Modbus / OPC UA 的 tag 读数进入 MachineSignalIngestor，按信号类型分派。MQTT 的消息解析与动作还应沿 Connectivity 的 MqttMessageParser、ActionExecutor 阅读，不能假设所有 MQTT 消息都经过同一 tag 入口。

| 信号 | 分派与语义 |
| --- | --- |
| state | WorkstationStateMachine；状态未变化时不重复写转换事件 |
| good_count / reject_count / cycle_complete | MachineCounterService；按通道解释读数 |
| telemetry | 工位元数据与遥测事件 |
| alarm | 记录告警事件；这里不直接建立所有业务 Issue |

### 计数器三层概念

原始值 → 基线与差值 → 目标工序产量。MachineCounterService 把基线、绑定、去重、来源配置与生产应用放在受锁保护的流程内。

- 已配置通道检查明确的工位和工序归属。
- event_id 重复时返回已有 reading；不能假设所有输入都自带该字段。
- 累计读数首次建立基线，不直接把整段历史记成产量。
- 回退、配置变化、过时读数等需要按 reading.status 判断；重建基线后的下一条新读数才建立新基线。
- MachineSignalIngestor 中仍有 configured_at 为空时进入 LegacyMachineCounting 的兼容分支。新通道规则不能被泛化到全部旧配置。

### ERP 路由边界

ERP 路由为 /api/v1/erp，使用 module:erp、auth.apikey、scope 和限流；普通 /api/v1 主要使用 auth:sanctum。两套身份方式不可混用。

| 方向 | 端点示例 | 责任 |
| --- | --- | --- |
| 入站工单 | POST /api/v1/erp/work-orders/import | 外部工单导入 |
| 主数据 | POST /api/v1/erp/products/import | 产品同步；材料、BOM 有各自入口 |
| 库存 | POST /api/v1/erp/stock/import | 外部库存同步 |
| 出站生产 | GET /api/v1/erp/production/completions | 生产完成读取 |
| 单据确认 | POST /api/v1/erp/stock-documents/{stockDocument}/ack | ERP 对已提供单据的接收确认 |

请求字段和错误契约应结合实际控制器、Request 与 ERP 测试读取，原 API 文档作检索起点。本章不是未经验证的完整接口规范。

### 排障顺序

设备有值但不计产：协议连接 → tag/topic 解码 → 配置指纹 → reading 状态与基线 → 工位/工序绑定 → 工序状态及阻塞 → 最后有效工序汇总。不要从设备累计值直接推算丢失产量。

### 读数为什么只有部分入账

MachineCounterService 在事务里锁通道，先按 event_id 查重复，再校验数值、时间、配置指纹和计数模式。累计模式用新旧读数求差，增量或脉冲还要求有效事件 ID，脉冲值必须为 1。应用阶段锁定工单与工序，复核租户、产线、工位、计数源和生产状态。良品增量按本站 availableQty 截取。明确通道的非良品 kind 走 quality_unknown 分支。阅读明细时分开查看 raw_value、delta、applied_qty 和 status。

### ERP 的部分成功和确认

入站由 importErp 逐行复用工单导入逻辑。合法行和失败行可以出现在同一个结果中，应按 errors 的行号、字段和消息修正，再按策略重送。出站库存单据默认筛选已过账状态，支持仓库、更新时间及仅未同步筛选，按游标分页。ACK 在 API key 建立租户上下文后才查单据，避免提前绑定模型时失去租户限制。

## 设备读数状态速查

以下状态来自明确配置通道的 MachineCounterService.ingest / apply；Legacy 路径需单独核对。

| 状态 | 语义 | 排查动作 |
| --- | --- | --- |
| invalid_value | 非法、负数、超范围或精度不合要求 | 检查协议解码与单位倍率 |
| timestamp_required / future_reading / out_of_order | 缺时间、超前或顺序不合法 | 对齐设备时钟、采集时间及 configured_at |
| event_id_required / invalid_pulse | 增量或脉冲缺有效 ID；脉冲值不是 1 | 修正发送端事件生成规则 |
| baseline / unchanged | 建立累计基线或没有正增量 | 不应解释为漏计历史总数 |
| source_changed / reset_required | 来源变化或累计回退 | 复核配置并按流程重新建立基线 |
| unassigned / blocked | 无归属或目标不能接收 | 核对绑定、运行状态、租户、产线与阻塞 |
| quality_unknown | 通道不是明确良品 kind | 不直接当作良品入账 |
| partial / applied | 部分或全部应用增量 | 比较 delta 与 applied_qty |

实现会拒绝超过服务端当前时间 5 秒的读数；累计模式相同时间戳也被视为过时。时间检查与 event_id 去重不是同一种机制。不要通过替换 ID 或时间戳重发原事件来绕过检查。

## 部分应用和人工复核

读数 delta=3、applied_qty=2，意味着还有 1 件未应用，但系统不会因此自动认定应补到任意工序。review 只允许未复核且状态在 unassigned、blocked、partial、quality_unknown 范围内的记录。选择 apply 时还必须是明确 good，目标能接收完整剩余量且量大于 0。

复核会标记原读数的复核信息，再追加 reconciled 或 dismissed 记录，保留原始归属。rebaseline 则清空基线，下一条新鲜累计读数只建立基线。两者一个处理未应用增量，一个处理计数起点，不能互相替代。

## ERP 导入的 HTTP 与业务结果

| 响应 | 含义 | 客户端动作 |
| --- | --- | --- |
| 200 | 本次批处理没有行错误 | 仍区分 imported、updated、skipped |
| 207 | 批处理完成，但至少一行失败 | 读取 data.errors，按一基行号修复失败行 |
| 422 | 请求形状不符合 Request | 修正整体或字段格式后再提交 |
| 401 / 403 | 身份或权限不满足，具体看中间件 | 核对 key、IP、scope 和模块状态 |

WorkOrderImportService 支持 update_or_create、skip_existing、error_on_duplicate 策略。重试前必须选择业务需要的策略；HTTP 207 不代表整批回滚，也不能将成功行不加区分地再创建一次。

对接记录建议保存外部工单号、请求批次标识、发送时间、策略、各行结果和 MES 对象 ID。不要保存明文 API key 到日志或错误工单。

## 出站库存交换与 ACK

读取库存单据使用 erp:stock:read，ACK 使用 erp:stock:write。导出默认只选已过账单据，可按 since、type、warehouse 和 unsynced_only 筛选，并使用游标分页。客户端应跟随响应的游标信息，不应把它当页码递增接口。

1. 拉取待同步的已过账单据，保存 MES 单据 ID、document_no、行项目及更新时间。
2. ERP 根据自己的去重约定建立对应单据，确认落账成功。
3. 调用 ACK 写回 ERP 单号 erp_reference。
4. 如果 ACK 网络失败，先用 MES ID 查询 ERP 已有单据，再补确认，避免重复入账。
5. 定期对照 MES 过账单据与 ERP 结果；取消或后续修改需要单独定义同步处理，不能只依赖“未同步”过滤。

步骤 2 的外部去重和步骤 5 的对账是集成方实施要求，不代表 OpenMES 已提供跨系统分布式事务。

## 集成回归数据包

设备至少覆盖：首次基线、正常增量、重复事件、乱序时间、累计清零、绑定切换、工序阻塞、超可用量和复核重复提交。ERP 至少覆盖：合法行与错误行混批、重复单号的三种策略、跨租户对象、分页遍历以及落账成功但 ACK 失败。每个用例同时检查响应与最终业务数量，协议连通只完成了第一层验收。

## 源码依据

- [backend/app/Services/Machine/MachineSignalIngestor.php:31](/Users/seal/Desktop/myLog/program/SKhynix/OpenMes/backend/app/Services/Machine/MachineSignalIngestor.php:31)
- [backend/app/Services/Machine/MachineCounterService.php:113](/Users/seal/Desktop/myLog/program/SKhynix/OpenMes/backend/app/Services/Machine/MachineCounterService.php:113)
- [backend/app/Services/Connectivity/MqttMessageParser.php:1](/Users/seal/Desktop/myLog/program/SKhynix/OpenMes/backend/app/Services/Connectivity/MqttMessageParser.php:1)
- [backend/app/Services/Connectivity/ActionExecutor.php:1](/Users/seal/Desktop/myLog/program/SKhynix/OpenMes/backend/app/Services/Connectivity/ActionExecutor.php:1)
- [backend/routes/api.php:113](/Users/seal/Desktop/myLog/program/SKhynix/OpenMes/backend/routes/api.php:113)
- [backend/tests/Feature/Machine/MachineCounterTest.php:1](/Users/seal/Desktop/myLog/program/SKhynix/OpenMes/backend/tests/Feature/Machine/MachineCounterTest.php:1)
- [backend/tests/Feature/Api/V1/Erp/ErpWorkOrderImportTest.php:1](/Users/seal/Desktop/myLog/program/SKhynix/OpenMes/backend/tests/Feature/Api/V1/Erp/ErpWorkOrderImportTest.php:1)
- [机器增量截取与生产复核](/Users/seal/Desktop/myLog/program/SKhynix/OpenMes/backend/app/Services/Machine/MachineCounterService.php:237)
- [ERP 逐行导入结果](/Users/seal/Desktop/myLog/program/SKhynix/OpenMes/backend/app/Services/CsvImport/WorkOrderImportService.php:92)
- [单据导出、游标与确认](/Users/seal/Desktop/myLog/program/SKhynix/OpenMes/backend/app/Http/Controllers/Api/V1/Erp/StockDocumentExportController.php:28)
- [backend/app/Http/Controllers/Api/V1/Erp/WorkOrderImportController.php:27](/Users/seal/Desktop/myLog/program/SKhynix/OpenMes/backend/app/Http/Controllers/Api/V1/Erp/WorkOrderImportController.php:27)
