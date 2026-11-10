# 前端入口与实时读模型

React 页面负责呈现与交互，Laravel 写业务数据，实时集合让多个页面看到提交后的结果。

## 使用逻辑

### 用两个窗口观察同步

在隔离实例中，用有权限的账号打开两个管理端工单列表窗口。在窗口 A 新建测试工单，在窗口 B 检查它是否自动出现。然后更新同一工单的计划量，再检查两边显示的数量。验收时记下工单 ID、列表使用的集合和网络请求。用筛选过的集合验收时，选择一条可退出该筛选范围的记录，再观察它是否从列表移除。

| 现象 | 观察方法 | 优先核对 |
| --- | --- | --- |
| 打开列表就没数据 | 检查集合 GET 请求与响应的 rows | 会话、角色、集合名和查询筛选 |
| 刷新后能看到，停在页面看不到 | 检查 WebSocket 连接、订阅和事件 | Reverb、频道授权、事件是否在提交后发送 |
| 列表消失一行，工单详情仍能打开 | 对照集合过滤条件 | 是否发出了退出集合的 delete |
| 跳转后实时更新停止 | 对照组件卸载与重建时间 | 共享订阅是否仍有消费者 |

工位、操作员队列和管理列表的刷新来源可能不同。复现前先定位页面使用的 props、集合或频道，便于解释两个界面的刷新时点差异。

## 技术逻辑

### Web 启动

app.jsx 使用 createInertiaApp，核心页面来自 Pages/**/*.jsx，模块页面来自 backend/modules 下的构建期 glob，同时支持运行时注册页面。首次页面解析前等待 loadModules，避免直接刷新模块页面时尚未注册。

setup 加载 locale、timezone、tenant 信息与共享 UI 标签。导航事件同步 CSRF meta，解决会话刷新后部分 fetch 提交仍使用旧令牌的问题。

### 页面阅读入口

| 页面 | 关注点 |
| --- | --- |
| operator/Queue.jsx | 操作员可执行工单入口 |
| operator/WorkOrderDetail.jsx | 批次与工序动作、请求及错误反馈 |
| operator/Workstation.jsx | 工位现场上下文 |
| lib/pageResolver.js | 核心与模块页面解析 |
| lib/realtimeCollection.js | 同步集合协议与生命周期 |

改页面时先确定它使用 Inertia props、普通请求还是实时集合，避免把所有状态复制到组件本地后失去同步来源。

### 快照与增量

```text
浏览器 GET /api/collections/{name}
  → 服务端返回 rows 与 channel
  → Echo 订阅该 channel
  → CollectionChanged：upsert / delete
  → TanStack 集合提交变化
  → 使用集合的表格与页面刷新
```

这些 collection 快照路由位于 web.php，即使 URL 含 /api，也不能据此推断它与普通 token API 使用同一身份路径。

realtimeCollection 首次加载快照，订阅确认及重连时再次取快照，覆盖订阅前的写入窗口。集合维护 key 集，upsert 根据已有 key 转为插入或更新；快照也会删除已不属于当前集合的旧行。

### 服务端广播

CollectionBroadcaster 监听模型变更，映射到具体集合与成员条件，并在事务提交后广播。离开过滤集合的行发 delete；例如完成工单可离开 work_orders_active，但不等于数据库被删除。

修改同步字段时，同时核对 ShapeRegistry / Shapes 的读取投影与 CollectionBroadcaster 的写侧投影，以及租户/产线权限。新增字段不是只给 React 表格加一列。

### 多端边界

mobile 的 API 请求和 Tauri 外壳应单独验证。共享 packages/ui 不代表 Web、Expo 与桌面具备完全相同的路由、权限状态或离线能力。

### 快照与广播怎样约定数据形状

CollectionController 根据 ShapeRegistry 找表、列和过滤条件。它使用 DB::table，所以对支持软删除的表显式过滤 deleted_at，再按表结构与用户 tenant_id 决定租户过滤和 channel 名。客户端使用服务返回的 channel。channels.php 分别定义管理集合 col 与操作员产线 operator-line 的授权。排查跨角色行为时需同时看 HTTP 请求权限和频道权限。

### 为什么回滚不能广播

模型事件可以早于数据库事务提交。CollectionBroadcaster 推迟发送到提交后，事务回滚的记录不会进入浏览器。广播失败时写日志，数据库写入仍可成功。回归用例分别验证「回滚后无记录也无事件」和「事务内尚无事件、提交后有事件」。浏览器用例还覆盖同一页面重挂载与多个窗口接收更新，适合验证真实 Reverb 和前端订阅生命周期。

## 新增页面或字段的完整路径

1. 从 routes/web.php 找到入口和中间件，定位返回 Inertia 页面名的控制器。
2. 核对页面名能被 pageResolver 解析，模块页面还要确认加载顺序和资源发布。
3. 明确页面的数据来源：初始 props、普通请求或实时集合；选择一个权威来源维护同一字段。
4. 表单通过对应业务端点提交，展示字段错误和业务拒绝信息。
5. 若使用集合，同时修改初始 Shape 列与广播投影，确认插入、更新、删除都携带正确 key。
6. 用首次访问、页面内导航、直接刷新、断网重连和第二窗口修改验证。

服务端未写入成功时，页面不能仅凭本地乐观状态宣布生产完成。若需要乐观显示，应保留待确认状态并在失败后恢复，与真实产量清晰区分。

## 快照协议与订阅生命周期

CollectionController 返回 rows、channel 和 at。channel 由服务端决定，租户表可能使用租户频道，全局表使用 g；浏览器不应自行拼接别的租户频道。at 是响应时间信息，不应未经核实当成可恢复所有历史事件的日志游标。

实时更新覆盖当前集合成员。一个工单离开 active 集合时，客户端收到 delete 应只移除这份列表中的成员；不能把所有详情缓存都当成该工单已删除。重新获取快照时，也要删除本地已不在 rows 中的旧行。

组件挂载、共享集合消费者计数与取消订阅需要一起检查。只验证首次打开页面正常，容易漏掉路由来回跳转后不再收到事件的问题。

## 五段式实时故障定位

| 层次 | 观察点 | 能排除或确认的问题 |
| --- | --- | --- |
| 写请求 | 状态码、错误体、工序或工单 ID | 请求是否真的成功 |
| 持久化 | 刷新或重新查询后的状态 | 数据是否已提交 |
| 快照 | rows、channel、HTTP 权限 | 投影、筛选与租户是否正确 |
| 广播 | 提交后日志、Reverb 与频道授权 | 事件是否发送到允许订阅的频道 |
| 消费端 | WebSocket 帧、集合 key、组件订阅 | 收到事件但未正确更新或渲染 |

“刷新后恢复”通常提示写入存在而增量链路有问题，但仍须检查快照是否用了不同过滤。401/403 先处理身份或授权；连接成功却没有事件再追广播和订阅，不要一律清空所有缓存。

## 实时一致性的验收矩阵

| 操作 | 窗口 A | 窗口 B | 重载后的结果 |
| --- | --- | --- | --- |
| 新建对象 | 看到成功与新行 | 收到新成员 | 与两窗一致 |
| 修改实时字段 | 显示新值 | 同一行更新 | 字段不回退、不消失 |
| 对象退出过滤 | 退出当前列表 | 收到 delete 并移除 | 不再返回该成员 |
| 事务回滚 | 显示失败 | 不出现未提交变化 | 保留原状态 |
| 断线期间修改 | 网络不可用 | 另一端正常更新 | 重连快照恢复一致 |
| 两租户同名对象 | 仅各自可见 | 不串租户 | 快照与频道同样隔离 |

以上是开发验收要求；默认 PHPUnit 的 null 广播不能替代真实 WebSocket 验收。使用跨窗口 E2E 时还需满足用例依赖的 Docker 与隔离数据条件。

## 性能与可维护性

新增投影字段时控制集合体积，避免把大块工艺快照、附件内容或不必要的敏感字段广播到整个列表。关联查询在服务端明确加载，防止每行触发一次查询。页面内的格式化、标签和派生值尽量从同一份集合数据计算，避免 props、组件 state 和实时集合各保存一份无法自动对齐的数量。

## 源码依据

- [backend/resources/js/app.jsx:28](/Users/seal/Desktop/myLog/program/SKhynix/OpenMes/backend/resources/js/app.jsx:28)
- [backend/resources/js/lib/realtimeCollection.js:21](/Users/seal/Desktop/myLog/program/SKhynix/OpenMes/backend/resources/js/lib/realtimeCollection.js:21)
- [backend/app/Sync/CollectionBroadcaster.php:206](/Users/seal/Desktop/myLog/program/SKhynix/OpenMes/backend/app/Sync/CollectionBroadcaster.php:206)
- [backend/routes/web.php:181](/Users/seal/Desktop/myLog/program/SKhynix/OpenMes/backend/routes/web.php:181)
- [backend/resources/js/Pages/operator/WorkOrderDetail.jsx:1](/Users/seal/Desktop/myLog/program/SKhynix/OpenMes/backend/resources/js/Pages/operator/WorkOrderDetail.jsx:1)
- [backend/resources/js/lib/pageResolver.js:1](/Users/seal/Desktop/myLog/program/SKhynix/OpenMes/backend/resources/js/lib/pageResolver.js:1)
- [backend/tests/Feature/Sync/CollectionBroadcastAfterCommitTest.php:1](/Users/seal/Desktop/myLog/program/SKhynix/OpenMes/backend/tests/Feature/Sync/CollectionBroadcastAfterCommitTest.php:1)
- [快照列、软删除与租户过滤](/Users/seal/Desktop/myLog/program/SKhynix/OpenMes/backend/app/Http/Controllers/Api/CollectionController.php:24)
- [管理集合频道授权](/Users/seal/Desktop/myLog/program/SKhynix/OpenMes/backend/routes/channels.php:27)
- [工单列表跨窗口 E2E](/Users/seal/Desktop/myLog/program/SKhynix/OpenMes/tests/e2e/work-orders-live-list.spec.ts:65)
