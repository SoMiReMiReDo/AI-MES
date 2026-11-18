# F11-`DataImportEquipmentIntegration` 数据导入与设备集成

更新 2026-11-18。用户确认的 F11-01 和 F11-03 CSV 工单草稿流程已通过本地验收。

| 功能 | 本轮交付 |
| --- | --- |
| F11-01 | 为已登记设备创建模拟连接，接收模拟报文，查看原文、解析结果、拒绝原因及关联设备事件。 |
| F11-03 | 上传 CSV，映射列名，逐行预检，查看历史任务并确认整批创建工单草稿。 |
| 后续范围 | F11-02 完整连接、测点和 API Key 管理，F11-03 其他导入对象和共享映射模板，F11-04/05 标签及自定义字段，F11-06/07/08 外部系统、真实设备及设备命令。 |

## 模拟连接与报文

- 连接仅绑定部署厂区内已启用设备，协议固定 `Simulator`。模拟报文用操作者的 Cookie 和防伪令牌提交。读须有 `integration.simulator.read` 和 `equipment.read`，登记和投递另须 `equipment.simulate`。
- 投递外层包含 `sourceMessageId` 和 `rawPayload`。原文是最多 8 KiB 的 JSON 文本，支持 `kind`（`Connection` 或 `State`）、`state`、以 `Z` 结尾的 UTC `occurredAt` 和 `reason`。发生时间可比接收时间晚最多 5 秒。F7 支持的分类和班次关联也可传入。保留原文及服务端接收时间。无效载荷进入报文履历，外层输入无效直接返回错误。
- 同连接、同来源编号、同原文返回原结果。编号相同而原文字节变化返回 409。按连接隔离编号，投递与 F7 人工状态操作共享事务和写锁；设备状态的时序按设备和状态类别判定，跨连接也适用。
- 解析成功后调用 F7 模拟状态用例，沿用非法、乱序、同状态判定以及 Down/停机登记和 Run 占用规则。报文状态为 `Applied` 或 `Rejected`。F7 生成事件时记录报文与事件的关联，解析失败也能查看报文及原因。区间按接收并应用时刻推进。`Connection` 事件更新通信状态，`State` 事件更新运行条件。

## CSV 工单草稿

从「数据导入与集成 → 工单导入」选取 UTF-8 CSV，手动填写源表头映射或使用同名表头。文件最多 256 KiB、100 条数据记录，接受 UTF-8 BOM、双引号转义和引号内换行。表头去除首尾空白并按大小写匹配，列名须非空且唯一。行号为 CSV 记录编号，表头为第 1 条，空行略过。

| 目标字段 | 规则 |
| --- | --- |
| `orderNo` | 必填，最长 64 字符。文件内重复、与已有工单号重复均报错。 |
| `productCode`、`revisionCode` | 必填，以代码和修订精确匹配已有产品及关联修订。草稿沿用 F3 的修订规则，工艺生效在下达时校验。 |
| `plannedQty` | 必填，正整数。 |
| `priority` | 可选，整数 0～100，默认 0。 |
| `dueDate` | 可选，`yyyy-MM-dd`。 |
| `plannedStartAt`、`plannedEndAt` | 可选，ISO 8601 时间需带 `Z` 或偏移，保存为 UTC。结束时间须大于或等于开始时间。 |
| `description` | 可选，支持 CSV 引号中的逗号、引号及换行。 |

预检生成 `Ready` 或 `Rejected` 任务，保存映射、规范化预览及行、字段、错误信息。任务只对当前厂区的提交者开放，使用 `integration.work-orders.import` 和 `planning.work-orders.manage`。原始 CSV 在预检后释放，任务保存文件名及结构化结果。

对 `Ready` 任务确认时提交预检版本和幂等键。系统在共享事务内复核工单编号及当前产品修订，逐行调用 F3 工单创建用例。任一行复核不通过，任务标记 `Rejected`，显示错误且创建数为零。通过后同一事务提交全部 `Draft` 工单、业务审计、创建结果和 `Imported` 状态。重试返回同一批结果。工单下达从既有工单页面执行。

## 接口与持久化

所有端点前缀为 `/api/v1/integration`。列表使用现有分页契约，详情返回具体的报文或任务。工单结果携带工单 ID 和编号，可进入现有工单查询。

| 方法和路径 | 契约 |
| --- | --- |
| `GET /connections`、`GET /connections/{id}`、`POST /connections` | 查询模拟连接或登记 `equipmentId` 与 `name`，登记需 `Idempotency-Key`。 |
| `GET /connections/{id}/messages`、`GET /connections/{id}/messages/{messageId}` | 查报文摘要及带原文的详情。 |
| `POST /connections/{id}/messages` | 接收模拟报文，以连接和来源编号去重，并返回处理详情。 |
| `GET /work-order-imports`、`GET /work-order-imports/{id}` | 查询自己在当前厂区的导入任务及预检、确认详情。 |
| `POST /work-order-imports` | 预检 `filename`、`csv` 和 `mapping`，需 `Idempotency-Key`。 |
| `POST /work-order-imports/{id}/confirm` | 复核并确认，需要 `If-Match` 和 `Idempotency-Key`。 |

复用 `MachineConnection`、`MachineMessage` 和 `ImportJob`，为报文补充设备事件链接，为任务补充厂区及结构化的输入与结果字段。旧任务保留原字段及文件路径。现有 `ImportMapping` 继续保留作共享模板扩展。连接的厂区从设备和区域/设备组解析；任务使用显式 `FabId`。回执按操作者、厂区、操作和目标作用域保存，读取或重试均重新验证权限。

## 验收清单

- 登记模拟连接，投递 Down 后从报文详情看到原文、时间和关联事件，并通过设备页看到有效状态及停机区间。通信在线与设备 Down 可同时成立。
- 同报文并发投递只保留一个报文和一个事件，原文变化冲突。不同连接可独立使用同编号。乱序、无效及无法解析的报文均可查询拒绝原因。
- 自定义中文表头的 CSV 可预检再确认，草稿可在 F3 查到，数量、日期、时间和带引号的描述正确。确认前草稿数为零，确认重试只创建一次。
- 混合有效和无效行、文件内重复、产品与修订不匹配、预检后出现重复工单号、权限撤销及过期版本均阻止整批创建。任务限制在提交者与厂区范围内，连接限制在关联设备的厂区范围内。

## 实施与验证

集成模块位于 `src/MESagent.Infrastructure/DataImportEquipmentIntegration/`，WebUI 入口为「生产准备 → 数据导入与集成」。交付含类型化 API、生成的 Web 客户端契约、第 12 条迁移及运行角色增量授权。[数据库升级记录](../development-process/06-database-implementation.md#2026-11-18-f11-增量更新)

17 项数据库测试、101 项 API 测试及 Chrome F1～F11 全流程回归通过。Chrome 专项在独立数据库以运行角色完成设备事件下钻、工单预检与确认、工单查询及 390 像素浅深主题验收。测试数据随临时数据库清理。
