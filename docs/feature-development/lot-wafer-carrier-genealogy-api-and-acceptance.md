# F4-`LotWaferCarrierGenealogy` 接口与验收

日期：2026-11-11｜状态：接口与验收设计基线；2026-11-12 首版实现及实际测试覆盖见[实现与本地验收](lot-wafer-carrier-genealogy-implementation.md)，未覆盖场景仍待验证。依据：[功能设计](lot-wafer-carrier-genealogy.md)。

## 1. 通用契约

前缀 `/api/v1/production`；JSON camelCase，ID 为 UUID，时间为 UTC ISO 8601。页面按 Fab 时区展示。沿用 Cookie、写请求防伪、有效会话、动作权限及服务端对象范围校验。用户/厂区/事件时间/实际完成量由服务端确定，拒绝未知写字段。

列表参数 `page=1/pageSize=20`，pageSize 最大 100；返回 `items/total/page/pageSize`。排序白名单，始终补 ID 稳定次序；筛选、统计、详情、血缘与回执均在范围校验后返回。详情含 `etag`，单资源详情同时返回 HTTP ETag；ETag 为不透明值，不暴露 xmin 的实现编码约定。

创建成功 201，含资源 ID、新 ETag 和 Location；更新/动作成功 200，返回 commandId、业务事件 ID、受影响资源及各自新版本。装载等多对象动作不伪造一个通用 ETag。预览 POST 无业务写入，返回 200，不分配正式编号。

| 情况 | HTTP / 稳定错误码（拟定） | 页面处理 |
| --- | --- | --- |
| 未登录/会话失效 | 401 / SessionInvalid | 重新登录 |
| 无动作权限/无厂区范围 | 403 / PermissionDenied、FabScopeRequired | 显示拒绝，不能按钮绕过 |
| 对象不存在或不可见 | 404 / ResourceNotFound | 不泄露其它厂区内容 |
| 缺少/过期版本 | 428 / VersionRequired；412 / VersionConflict | 刷新对象并重新确认 |
| 状态、Hold、运行占用 | 409 / LotStateConflict、LotBlocked、ResourceInUse | 展示获授权的阻塞来源 |
| 槽位、片归属、编码竞争 | 409 / SlotOccupied、MembershipConflict、CodeConflict | 刷新占用/清单，不静默覆盖 |
| 同键异参 | 409 / IdempotencyConflict | 保留原命令，不换键自动重试 |
| 投放超额度 | 409 / WorkOrderQuantityExceeded | 展示计划/已投放/本次值 |
| 校验失败 | 422 / InvalidWaferSet、IncompatibleLots、CarrierSizeMismatch、InvalidBaseline | 定位字段或兼容项 |
| 必需协作能力未接通 | 409 / DependencyNotReady | 展示依赖；不可把缺失门禁当通过 |
| 请求过大 | 413 / RequestTooLarge | 提示缩小清单，不分多次提交同一原子动作 |

错误沿用当前 API 的 ProblemDetails 形状，含 `type/title/status/code/traceId`；拟增 `errors` 供字段/对象定位。内部 CorrelationId 与 traceId 关联，不新增另一个含义不同的请求编号。只有获授权对象才返回其阻塞细节。

生产写动作必须携带 1～128 字符 Idempotency-Key。作用域为用户/Fab/动作/目标；摘要覆盖规范化后的业务输入和期望版本。重放必须使用原始业务输入及原版本：同键同参返回原成功状态/结果并标记 `Idempotency-Replayed: true`；授权复核通过后先查回执，再检查是否已过期。改了任何业务参数或预期版本均为新意图，须重新确认并用新键。

回执不得比业务先提交；失败回滚无成功回执。客户端超时不能推断失败，应以原键重试。回执重放后的 ETag 是当时结果，页面还需重查最新状态。新建动作无 If-Match；对依赖工单仍在锁内校验状态和基线。

首版设计限制单次建片/装载/拆合批涉及 Wafer 总数不超过 100，合批源数 2～20，载具容量 1～100；这是可验证的演示请求边界，不是设备容量标准。保持现有普通请求 64 KiB 限制，超限整体拒绝；容量放大须同时验证请求、锁耗时和页面性能。

## 2. 资源与动作接口

下表省略统一前缀。权限前缀均为 `production.`；查询关联主数据另需既有工程/工单读取权限。

| 方法与路径 | 请求/用途 | 权限及并发 |
| --- | --- | --- |
| GET /lots | lotNumber/workOrderId/status/routeStepId/hasHold 筛选；列表中未知统计标 null | lots.read |
| POST /lots | workOrderId、lotNumber?、priority、notes?、wafers[] | lots.create；幂等 |
| GET /lots/{id} | 基线、数量、当前步骤/Run、阻塞、allowedActions、coverage | lots.read；返回 ETag |
| PUT /lots/{id} | 仅 Draft 的 priority/notes | lots.manage；If-Match、幂等 |
| POST /lots/{id}/release | 空对象；服务端确定首站及片集 | lots.release；If-Match、幂等 |
| POST /lots/{id}/cancel | reason | lots.cancel；If-Match、幂等 |
| GET /lots/{id}/wafers | 当前片清单；includeHistory=true 时返回归属区间，不混算当前数量 | lots.read + wafers.read |
| GET /lots/{id}/genealogy | 直接父/子事件分页，支持 direction=ancestors/descendants 展开 | genealogy.read + lots.read |
| POST /lots/{id}/split-preview | waferIds/newLotNumber?/reason，与正式命令同形 | lots.split；不写、不保留资格 |
| POST /lots/{id}/split | waferIds/newLotNumber?/reason | lots.split；If-Match、幂等 |
| POST /lot-merges/preview | sources[{lotId,etag,waferIds}]/newLotNumber?/reason | lots.merge；不写 |
| POST /lot-merges | 同预览；所有源片完整合入一个新批 | lots.merge；各源版本、幂等 |
| GET /wafers | waferCode/workOrderId/lotId/lifecycleStatus | wafers.read |
| GET /wafers/{id} | 身份、稳定工单、来源、当前 Lot/槽位、首次投放与完成事实引用 | wafers.read |
| GET /wafers/{id}/history | type/from/to/page/pageSize；返回归属、血缘、装载及已接通加工来源 | wafers.read；各来源对象授权 |
| GET /carriers | carrierCode/status/locationId | carriers.read |
| POST /carriers | carrierCode/carrierType/slotCapacity/allowedWaferSizeMm/currentLocationId? | carriers.manage；幂等 |
| GET /carriers/{id} | 载具资料、槽位、逐片当前 Lot、版本、allowedActions | carriers.read；片详情需 wafers.read |
| PUT /carriers/{id} | status，停用须空载；不允许直接改位置/容量 | carriers.manage；If-Match、幂等 |
| POST /carrier-load-commands | carrierVersions[]、waferVersions[]、lotVersions[]、moves[]、reason | carriers.load；多对象版本、幂等 |
| POST /carriers/{id}/move | toLocationId/reason，当前位置由服务端确定 | carriers.move；If-Match、幂等 |
| GET /carriers/{id}/history | 装载/卸载/移位时间线及来源事件 ID | carriers.read；片详情按授权返回 |
| GET /genealogy-events/{id} | eventType/occurredAt/actor、逐片 From/To、来源步骤继承关联 | genealogy.read；所有关联 Lot 同厂可见 |

载具备注字段当前模型没有；若代码阶段增加备注维护，须同时补字段与契约，不能向未定义字段写入。载具槽位随创建一次生成，不提供独立删除/任意修改接口。Wafer 和 LotMembership 无通用写接口；F4 不开放 `/complete`、报废、Hold/Release 或直接状态覆盖。

血缘展开默认 depth=1，最大 10；每页最多 100 条事件，返回 `truncated/hasMore`。页面继续按事件/源批下钻，不能把截断结果标为完整血缘。时间线排序为 occurredAt + eventId，返回 sourceType/sourceId/sourceUrl；没有 F5 实际加工事实时明确标 coverage，不能生成推测步骤。

工单候选沿用 `/api/v1/planning/work-orders?status=Released` 及快照读取；Lot 编号内部复用 F2 编号服务，不要求操作员具备编号规则维护权。来源候选在 F8 具备授权查询后接入；F4 不另建绕过 F8 的材料 CRUD。

## 3. 关键载荷与返回

### 3.1 建批

以下为结构示例，尖括号中的 ID/编码须替换成真实值：

```json
{
  "workOrderId": "<released-work-order-id>",
  "lotNumber": "LOT-DEMO-001",
  "priority": 50,
  "notes": "演示批",
  "wafers": [
    {
      "waferCode": "WAFER-DEMO-001",
      "waferSizeMm": 300,
      "originMaterialLotId": null,
      "supplierWaferCode": null
    }
  ]
}
```

priority 为 0～100；WaferSizeMm 正值，同批相同，精度沿用现有 numeric(20,6)。notes 最大 2000 字符；reason 去空白后 1～1000 字符。来源为空返回 `sourceCoverage=Unregistered`。返回 lotId、etag、waferIds、genealogyEventId、commandId；TargetQty 由清单长度确定，禁止手填数量代替清单。

Lot 详情至少包含 `workOrderId/snapshotVersion/productRevisionId/routeVersionId/currentRouteStepId/status/etag`，以及 `quantities`、`activeHoldCount/blockingReasons`、`allowedActions`、`coverage/asOf/missingDependencies`。allowedActions 是当前提示，提交仍重新授权和校验。

### 3.2 拆批与合批

拆批发送源 Lot 的 If-Match，body 为 `waferIds/newLotNumber?/reason`。预览返回迁移片、源余片数、新批数、继承位置与拒绝原因；不返回可永久使用的“通过令牌”。源批修改后原版本提交返回 412。

合批发送所有源 `lotId + etag + waferIds`，waferIds 必须恰为各源当前完整集合。缺少任一版本返回 428，版本过期返回 412，片集合不符返回 409 MembershipConflict。服务端排序锁对象，但不接受客户端通过调整源顺序绕过规范化摘要。

成功返回 `commandId/genealogyEventId/newLot{ id,etag }/sourceLots[{id,etag,status,currentWaferCount}]`。同键重放必须仍指向同一个新 Lot 和事件，不重新分配编号。新批不得重新累计工单投放量。

### 3.3 装载命令

```json
{
  "carrierVersions": [{ "id": "<carrier-id>", "etag": "<etag>" }],
  "waferVersions": [{ "id": "<wafer-id>", "etag": "<etag>" }],
  "lotVersions": [{ "id": "<current-lot-id>", "etag": "<etag>" }],
  "moves": [
    {
      "waferId": "<wafer-id>",
      "fromSlotId": null,
      "toSlotId": "<target-slot-id>"
    }
  ],
  "reason": "首次装载"
}
```

版本集合必须覆盖所有源/目标载具、全部变更片和其当前 Lot，不接受遗漏、重复或无关资源。每片恰出现一次；目标非空槽不可重复。原槽与实际当前装载不符返回 409，不能把错误原槽解释为“直接覆盖”。交换槽位允许目标当前占用者同样在本命令中明确迁出。

成功返回各载具及相关 Lot/Wafer 新版本、关闭/新建的 loadIds 与事件 ID。只要片装载或归属影响页面操作前提，就推进相关版本；Lot 拆合批也更新受影响 Wafer 和在载 Carrier 的版本，使旧页面无法继续使用旧 Lot 信息。

### 3.4 F5/F6 内部协作

| 能力 | 调用方输入 | 同事务结果/限制 |
| --- | --- | --- |
| EnterWaitingStep | Lot、绑定 RouteStep、工单快照、服务端时间 | 唯一未退出步骤；复制定义引用，不复制已完成检查记录 |
| InheritWaitingPosition | 血缘事件、源等待步骤集、目标 Lot | 新等待步骤及来源关联；源合批步骤退出为 Transferred |
| CompleteAtRouteEnd | 已授权 F5 上下文、最终 Run、实际参与且成功的 Wafer 集 | 校验终点和门禁后唯一完成事实、片/批状态、工单缓存；禁止独立提交事务 |
| CloseMembershipForScrap | 已授权 F6 上下文、逐片处置、原 Lot/Run | 关闭归属/装载，与处置/报废事实及审计一起提交；全片报废才 Closed |

内部调用仍验证操作者、厂区与调用用例所需权限；不设普通用户可伪造的 `trusted=true` 或 `skipValidation` 参数。F5/F6 未实现时相关能力标待集成，不暴露占位成功响应。

## 4. 验收清单

以下为完整目标场景；已执行覆盖以实现记录为准，未逐项覆盖不得标通过。DB 表示真实 PostgreSQL 约束/事务测试，API 表示 HTTP 集成测试，UI 表示浏览器。测试通过必须另附实现版本、命令、结果和日期；文档本身不构成验收证据。

| ID | 覆盖 | 场景及期望 | 层级/依赖 |
| --- | --- | --- | --- |
| F4-A01 | 01 | 已下达工单建 5 片：1 Lot、5 Wafer、5 初始归属及 1 Create 事件，固定工单快照 | DB/API/UI |
| F4-A02 | 01 | Draft/Cancelled/Closed 工单、无工单及跨厂工单建批均拒绝 | API |
| F4-A03 | 01 | 重复片号、空清单、重复输入、非正/混合尺寸、伪造数量/厂区/状态被拒 | API |
| F4-A04 | 01 | Lot 自动编号并发不重号；任一片创建失败，Lot/片/归属/审计/成功回执全回滚 | DB/API |
| F4-A05 | 01 | 来源批号与 WaferCode 独立；未登记来源明确显示，非法/不可见来源拒绝 | API/UI；F8 来源协作 |
| F4-A06 | 01 | 稳定 Wafer 工单/厂区不可直接修改；跨工单现有片不能通过建批转入 | DB/API |
| F4-A07 | 02 | 投放固定工单基线及路线首站，创建唯一 Waiting 步骤和逐片首次投放，StartedAt 不提前写 | DB/API/UI |
| F4-A08 | 02 | 工单快照缺失、版本废止、空路线、片状态不符或有效 Hold 阻止投放 | API；F6 门禁联调 |
| F4-A09 | 02 | 并发投放争夺最后工单额度，仅符合上限的命令成功；报废后不自动补额度 | DB/API；含 F6 联调 |
| F4-A10 | 02 | 投放与工单取消/关闭、工艺废止竞争，有确定提交顺序，不出现终态工单新投批 | DB/API |
| F4-A11 | 02 | currentWaferCount 与 WIP 分开；未接通完成来源返回 null/PendingIntegration | API/UI |
| F4-A12 | 03 | 创建载具原子生成连续槽位；非法容量、重复编码、跨厂位置或尺寸不匹配拒绝 | DB/API/UI |
| F4-A13 | 03 | 并发同片装两槽、两片装同槽，只成功一项且无重叠区间 | DB/API |
| F4-A14 | 03 | 两片交换槽、跨载具移动正确；任一映射失败整批装载命令回滚 | DB/API/UI |
| F4-A15 | 03 | 错误原槽、遗漏版本、重复片/目标槽、已报废/取消片装载拒绝 | API |
| F4-A16 | 03 | 有在载片不能停用；容量/类型/编码不能直接改；历史槽位不可删除 | DB/API |
| F4-A17 | 03 | 人工移位同时写位置及 Movement；原地移位拒绝；移动中涉及运行/阻塞片拒绝 | API/UI；F5/F6 联调 |
| F4-A18 | 03 | 同载具多 Lot、同 Lot 多载具可查询；拆合批不自动改变槽位 | DB/API/UI |
| F4-A19 | 04 | 5 片拆出 2 片，源余 3 片；集合无交集、并集不变，逐片历史可查 | DB/API/UI |
| F4-A20 | 04 | 空选片、全部片、外来片、重复片、非 Ready 或无等待步骤拒绝 | API |
| F4-A21 | 04 | 子批继承快照/等待位置，建立来源步骤关联；不复制加工、点检、量测成功事实 | DB/API |
| F4-A22 | 05 | 同位置 2+3 片合成新批；两个源 Merged 且无有效成员，新批 5 片 | DB/API/UI |
| F4-A23 | 05 | 不同工单/产品修订/路线/快照/步骤/尺寸、重复源、自环或互斥配方约束合批拒绝 | API |
| F4-A24 | 04/05 | 有 Hold、Run、待处置、活动 Q-time 或不支持的返工上下文，拆合批拒绝 | API；F5/F6 集成 |
| F4-A25 | 04/05 | 拆合批与 Track-in、Hold、报废、装载竞争时整体串行生效，不丢片不双归属 | DB/API；F5/F6 集成 |
| F4-A26 | 04/05 | 合批源 Waiting 步骤退出为 Transferred，目标等待步骤唯一；不产生虚假过站量 | DB/API |
| F4-A27 | 06 | 未投放取消原子关闭装载/归属，身份保留；取消不算报废/完成、不回收片号 | DB/API/UI |
| F4-A28 | 06 | 已投放、Running 或带 Hold 取消被拒；无手工覆盖状态/完成量入口 | API |
| F4-A29 | 06 | 三步路线非终点不得完成；最终 Run 成功后每片只记一次完成与工单产量 | DB/API/UI；F5/GAP-01 |
| F4-A30 | 06 | 完成片保留最终 Lot/载具记录但不计 WIP；Merged 不计完成；全报废 Closed 不填 CompletedAt | DB/API；F5/F6 |
| F4-A31 | 06 | 部分报废后仅余片终点完成，完成/报废分别可追溯且无重复计数 | DB/API/UI；F5/F6 |
| F4-A32 | 通用 | 同键同参（含 JSON 字段顺序变化）重放同一结果；同键异参返回 409 | API |
| F4-A33 | 通用 | 不同用户/动作相同客户端键不冲突；血缘内部键唯一；并发同键只产生一次事实 | DB/API |
| F4-A34 | 通用 | 成功后响应丢失，以原键和原 ETag 重试成功；撤权/撤销范围后旧回执不可读取 | API/UI |
| F4-A35 | 通用 | 缺单/多对象版本返回 428，过期返回 412；预览后资源变化提交重验 | API/UI |
| F4-A36 | 通用 | 对列表/总数/片历史/血缘/载具/回执越权均拒绝或先过滤；间接关联不泄露他厂 | API |
| F4-A37 | 通用 | 事务各写入阶段注入失败，事实、状态、审计、事件及回执均不部分落库 | DB/API |
| F4-A38 | 通用 | 历史归属/装载只可一次关闭；血缘/完成/投放事实禁止改删；快速动作无零长区间 | DB/API |
| F4-A39 | 页面 | 加载/空数据/失败/并发冲突可解释；重复点击不双写，成功后所有关联页面刷新 | UI |
| F4-A40 | 回补 | 拆合批后 F3 投放仍 5，完成去重；工单缓存重算与事实一致、关闭不与新投批竞争 | DB/API/UI；F5/GAP-01 |
| F4-A41 | 迁移 | 旧数据归属不明不猜测回填；新迁移、运行账号最小权限及回退在隔离库验证 | DB |
| F4-A42 | 边界 | 100 片/20 源边界及超限拒绝；血缘深度截断显式标记；分页统计不重复 | API/UI |

## 5. 端到端样例与完成定义

准备一个已下达工单（计划 5 片）、已发布三步演示路线和兼容 Recipe、两个同尺寸载具及厂区位置。步骤名称及 300 mm 示例仅作软件测试数据，不代表现场工艺确认。

1. 建批 5 片，载具装载，投放首站；核对稳定工单、首次投放与等待步骤。
2. F5 完成第一步并进入第二步等待；拆出 2 片，保留 3 片，逐片看来源步骤和载具不变。
3. F6 给一源批叠加两条 Hold；解除一条仍禁止合批，全部解除后才允许重新校验合批。
4. 合为新批 5 片，继续三步终点完成；重放最后一次 Track-out，工单完成量仍为 5。
5. 从任一 Wafer 查原批、拆批、合批、实际 Run/Recipe、装载历史和完成事实；F3 展示去重后的真实进度。

另用独立样本验证未投放取消、部分/全部报废、跨厂/越权、同片同槽竞争及失败回滚，不能通过改动上述成功样本历史制造错误场景。

F4 本地阶段可以验收建批、投放等待、载具和拆合批基础；F5/F6 未接通的场景标“待集成”，数据库夹具仅证明约束。只有相关 DB/API/UI 场景执行且证据齐全，才能宣称对应子功能已实现；F4-06 完整验收及 F3 GAP-01 回补须有真实 F5/F6 流程。Outbox 通知、Agent、真机、完整 Docker 交付不计入本轮文档完成标准。
