# F5-`DispatchAndProductionExecution` 派工、过站与生产执行

日期：2026-11-13｜范围：用户确认 F5-01～05 的文档、API、WebUI 与测试。本文保留实现设计；执行证据见[实现记录](dispatch-and-production-execution-implementation.md)，未覆盖项不计已验收。

## 1. 确认范围与依赖

| 需求 | 本轮能力 | 边界 |
| --- | --- | --- |
| F5-01 | 按设备组、步骤、优先级和等待时间查询待加工 Lot；可执行性、阻塞原因、人工选择与指派 | 指派不预占设备，Track-in 必须重新核验 |
| F5-02 | Track-in 固定步骤、设备、实际 Recipe 和全部有效成员片 | 一设备一个 Running Run、一次加工完整 Lot；不支持抽片或共炉 |
| F5-03 | 版本绑定的指令、点检、类型化输出、PDF 指导书确认、关键操作确认及责任时间 | 执行记录一次提交后不可覆盖；录错进入后续更正流程 |
| F5-04 | 按实际参与片逐片 Passed/Failed 收尾；作业与质量门禁；下一站或终点完成 | Failed 不等于报废，不增加 ScrapQty；终点只计唯一 Wafer |
| F5-05 | 独立权限中止 Run，记录原因与已有事实，释放设备占用，等待处置 | 失败、中止、加工中 Hold/停工/质量阻塞均不自动重试或放行 |
| F7 最小前置 | 设备类型/组/设备台账、人工状态、当前占用和状态履历 | 完整 F7、资格维护、真实设备协议登记于 GAP-12 |

F5-06 Q-time、07 返工、08 腔体/共炉不在本轮。F6 处置/放行/报废、Agent、实时通知及 Docker 继续后置。用户已确认：异常允许受控结束当前 Run，Lot 进入 AwaitingDisposition，不能启动下一步。F6/返工恢复能力登记于 GAP-13。

## 2. 实现架构

沿用模块化单体、现有会话/厂区授权入口、单 MesDbContext 与事务内全局写锁。新增独立 `DispatchAndProductionExecution/ExecutionBackend`，按查询、门禁、Run、作业记录、最小设备能力分文件。Application 定义白名单请求，Domain 定义逐片结果与数据值校验；API 仅映射，WebUI 调用同一应用入口。

| 协作方 | 事务内职责 |
| --- | --- |
| F1 | 当前会话、动作权限、部署 Fab 范围；每次请求重验 |
| F2 | 使用 Lot 已绑定 RouteVersion/RouteStep 的不可变定义；实际 Recipe 必须来自工单快照适用关系且当前仍有效，记录匹配关系 |
| F3 | 已下达工单门禁；进度从稳定 Wafer 归属、唯一投放/完成事实重算；关闭前复核全部关联对象 |
| F4 | 复用等待步骤创建与终点 `CompleteAtRouteEnd`；完成事实、晶圆生命周期、Lot 和工单缓存同事务提交 |
| F6 | 读取已有 Hold/停工/未完成质量任务；未实现的必需质量门禁返回 DependencyNotReady |
| 最小设备 | 校验设备/组/类型/区域/厂区有效性，设备类型适用、人工状态和 Running 占用；占用以 Run 为权威 |

所有生产关键事实、审计、BusinessEvent、回执一次提交。失败整体回滚；没有消费者的 Outbox/Worker 不提前引入。写锁保持现状，未宣称满足 Fab 规模吞吐。

## 3. 状态与数量

正常流：Ready + Waiting → Track-in → Running + Running Run → Track-out 全片通过 → 当前步骤 Completed → 下一站 Waiting/Ready；末站调用 F4 完工。

异常流：逐片 Failed 或有效 Hold/停工/质量任务阻塞 → Run Completed、步骤 AwaitingDisposition、Lot AwaitingDisposition；中止 → Run Aborted、步骤/Lot AwaitingDisposition。保留原步骤为未退出，防止绕过处置创建下一站。中止把参与片记为 Aborted，不伪造 Passed；已录作业数据保留。

正常 Track-out 必须匹配唯一 Running Run，参与片集合与 Track-in 固定集合、当前有效归属完全一致。PassedQty 是本站通过量，ScrapQty 不因失败而增加。存在异常时即使部分片 Passed 也不推进整批、不生成终点完成。已结束 Run 不能再次使用新命令结束。

工单 releasedQty 取唯一 WaferRelease；completedQty 取唯一 WaferCompletion；scrappedQty 取已投放且生命周期 Scrapped 的片；inProcessQty 为已投放、未完成/报废的片，包含排队/Running/待处置。blockedQty 是在制子集，按当前 Lot 的 Hold/停工/质量任务/待处置事实去重。缺失稳定归属或投放事实时 coverage 标记不完整，不猜造历史。完整 F6 门禁另标 missingDependencies。缓存重算不修改历史事实。关闭工单要求全部关联 Lot 终结、无 Active/Registered 片及未结束 Run/步骤/质量阻塞；取消只能无首次投放且关联 Lot 已取消。

## 4. 门禁和版本绑定

- Track-in 仅 Ready/Waiting，工单 Released；Lot/Step 与已绑定路线一致，全部成员 Active 且存在同工单投放事实；权限、厂区、Lot 与设备 ETag 全部校验。
- 设备人工状态仅 Idle 允许启动。Run 占用独立于人工状态，界面显示加工占用；人工状态可在加工中标 Down/Maintenance/Offline，不能把设备标 Idle 清除真实 Run。首版不把通信在线当可加工。
- RouteStep/Operation 声明设备类型时必须匹配。RecipeVersion 必须 Released、Recipe 启用；匹配工单下达快照中的 RecipeApplicability，并复核当前相同关系有效期。Track-in 固定版本、匹配关系 ID 和参与片，后续废止不回写已发生事实，也不阻止受控结束。
- 工单快照中的 PDF 引用随新步骤创建，使用当时文档 ID/校验和。已绑定历史 PDF 可按执行权限下载；全部绑定指导书需阅读确认。指令/点检/输出来自冻结步骤定义。
- 点检必须来自本步骤；输出沿用 string/decimal/integer/boolean/datetime/enum 类型，校验最多六位小数、长度、选项、单位与必填。仅加工中可录入；关键确认保存 ProcessConfirmation 及步骤确认时间。
- 正常 Track-out 必须完成必需点检、输出、PDF 和关键确认。中止不要求补齐正常作业项。加工中出现 Hold 可保存过程数据并收尾，但不得推进。
- 活动 Q-time 或阻塞自动质量触发器尚未接通时拒绝启动；已有质量阻塞读取生效。人员/设备资格、载具强制、物料寿命尚无启用配置，不宣称门禁已交付；工艺存在未解释的参数策略时拒绝执行并提示依赖待实现。

## 5. 权限、幂等和审计

权限：`execution.read`、`execution.dispatch`、`execution.track-in`、`execution.record`、`execution.track-out`、`execution.abort`；最小设备 `equipment.read`/`equipment.manage`/`equipment.state`；进度重算 `planning.work-orders.recalculate`。管理员迁移补齐权限，其余角色由管理员显式分配，不自动扩大角色。

厂区从 Lot/Step/Run 或设备 Area/Group 服务端解析；不可见对象 404，缺动作/范围 403。执行下载不要求额外 engineering.read，不能枚举非本批已绑定文件。

全部写操作携带 Idempotency-Key，变更对象带 If-Match；Track-in 另带设备 ETag。作用域含用户/Fab/方法/目标，摘要规范化对象、ID、数值及集合顺序。同键同参返回原结果，同键异参 409；当前权限先于重放校验；旧版本重试原成功命令允许重放。命令 ID 用作全局 TrackEvent 键。子记录写入推进 Run/Lot/Step 聚合版本。

## 6. 数据增量与页面

StepExecution 增加可空 AssignedEquipmentId/AssignedRecipeVersionId，兼容旧步骤；ProcessRun 增加可空 RecipeApplicabilityId，旧历史不补造。增加数据库历史保护与所需权限，保留现有设备/步骤 Running 唯一约束。不改变已应用迁移。

WebUI 增加“生产执行”与“设备台账”：派工筛选、阻塞说明、候选设备/配方、指派/Track-in、运行详情、指令/PDF/点检/输出/确认、逐片结果和中止原因；失败留表单，版本冲突要求刷新。工单详情显示真实指标及 F6 覆盖边界。时间按 Fab 时区显示，历史记录带对象编号和来源。

接口与可执行验收条件见 [API 与验收](dispatch-and-production-execution-api-and-acceptance.md)。
