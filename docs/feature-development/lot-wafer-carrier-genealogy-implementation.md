# F4-`LotWaferCarrierGenealogy` 实现与本地验收

日期：2026-11-12｜范围：F4-01～05 首版 API/WebUI、F4-06 取消/合批耗尽及内部完工/报废收尾能力。F5/F6 真实业务入口尚未接通，完整生产闭环与 F3 真实进度仍待回补。

需求依据：[功能设计](lot-wafer-carrier-genealogy.md) · [接口与 42 项验收场景](lot-wafer-carrier-genealogy-api-and-acceptance.md)。

## 1. 架构判断及调整

**采用微调，不更换现有架构。** 保留模块化单体、单 MesDbContext、认证/厂区授权、同事务全局写锁及现有前端框架；新增独立生产模块，避免继续向工程后端堆入生产流程。

| 位置 | 已实现职责 |
| --- | --- |
| [Application 契约](../../src/MESagent.Application/LotWaferCarrierGenealogy.cs) | 类型化建批、拆合批、载具与装载请求及模块接口；OpenAPI 导出请求 schema |
| [Domain 规则](../../src/MESagent.Domain/LotRules.cs) | 严格子集、终态、微秒精度下的正时间区间；[生产事实](../../src/MESagent.Domain/Entities/ProductionFacts.cs)保存投放、完成与步骤继承 |
| [ProductionBackend](../../src/MESagent.Infrastructure/LotWaferCarrierGenealogy/ProductionBackend.cs) | 在已认证的现有事务内分派命令，动作/厂区授权、版本、规范化幂等、审计与业务事件 |
| [Lots](../../src/MESagent.Infrastructure/LotWaferCarrierGenealogy/Lots.cs)、[Genealogy](../../src/MESagent.Infrastructure/LotWaferCarrierGenealogy/Genealogy.cs) | 固定工单基线建片、投放、取消、受限拆合批、等待步骤与文档引用 |
| [Carriers](../../src/MESagent.Infrastructure/LotWaferCarrierGenealogy/Carriers.cs)、[Queries](../../src/MESagent.Infrastructure/LotWaferCarrierGenealogy/Queries.cs) | 槽位、装卸/交换、移位、范围内列表/详情与血缘履历 |
| [Lifecycle](../../src/MESagent.Infrastructure/LotWaferCarrierGenealogy/Lifecycle.cs) | F5/F6 内部协作：根据真实 Run 或逐片 Scrap 凭据核验并收尾；无公开完成/报废 HTTP 捷径 |
| [API](../../src/MESagent.Api/Program.cs) | `/api/v1/production/*`、类型契约元数据、重放标识、ETag、错误处理 |
| [生产 WebUI](../../web/src/production.tsx) | 批次/晶圆/载具页面、建批、投放、拆合批预览、逐片装卸、履历及并发反馈；沿用当前主题能力 |

未另建 Worker、消息中间件或微服务。当前写锁适合单厂试点，吞吐及多节点方案未压测；F2/F3 的历史请求摘要不在本轮重写，F4 单独采用规范化摘要，避免破坏旧回执。

## 2. 实现范围

- **F4-01**：仅已下达工单建批；新 Wafer 身份、稳定工单/厂区、来源/尺寸、手工或 F2 自动 Lot 编号；逐片 Create 血缘。允许来源未登记并显式标记，不实现 F8 库存/来料验收。
- **F4-02**：核对工单快照和有效版本，投放首站 Waiting；唯一首次投放事实；工单锁内防超投。当前成员、在制、Hold 和等待步骤可查；完成指标保持待 F5 集成，工单原进度接口仍标缺。
- **F4-03**：载具容量/尺寸、自动槽位、装卸、两片交换槽位、跨载具转入、人工移位、停用及历史。装载改变不等于生产归属变化，不控制设备或搬运。
- **F4-04/05**：已投放 Ready/Waiting 批次，按片严格部分拆出、多个完整源批合入新批；同厂/工单/产品/路线/快照/位置/尺寸，逐片守恒。新步骤保存继承关系；源合批步骤 Transferred，源批 Merged，不产生完成量。
- **F4-06**：Draft 取消关闭装载/归属，保留身份；合批耗尽独立收尾。内部终点完成检查已结束 Run、实际逐片 Passed、必要路线加工、作业记录、Hold/停工/质量任务，再唯一计完成；报废收尾校验逐片处置和 ScrapEntry，全部报废标 Closed/AllScrapped。**内部能力已测试，不等于 F5/F6 用户流程已实现。**

有效 Hold、停工、未结束 Run 与未完成质量任务直接阻塞适用动作。若路线配置了尚未实现的活动 Q-time 或产品相关阻塞型自动质量触发器，投放/拆合批返回 DependencyNotReady，不静默跳过。未来 F5/F6 接入时必须保持同一事务/写锁，并授权 `execution.track-out`、`quality.scrap`；这两项未来模块权限当前没有公开路由，也没有作为 F4 普通角色动作发布。

## 3. 数据库与运行权限

第 6 条迁移 [20261112033140_LotWaferCarrierGenealogy](../../src/MESagent.Infrastructure/Persistence/Migrations/20261112033140_LotWaferCarrierGenealogy.cs) 已应用到本地数据库。当前为 **119 张业务/支撑表 + 7 张 Identity 表 = 126 张应用表**，另有 2 个视图及迁移历史表。

新增 `wafer_releases`、`wafer_completions`、`step_execution_inheritances`；Wafer 稳定厂区/工单、Carrier 尺寸及 Lot 关闭时间/原因字段。SQL 加固管理批次的基线、稳定身份、成员/装载同厂归属、区间只可关闭一次及追加事实不可修改。实际模型/DDL见[字段字典](../development-process/05-database-catalog.md)和[导出 SQL](../../database/mesagent.sql)。

兼容性说明：Wafer 的两项稳定字段仍为数据库可空字段，便于保留旧结构与隔离测试夹具；F4 创建时必填且不允许修改，未归属的旧片不进入 F4 查询/命令。迁移仅允许从完整历史唯一证明工单/厂区后回填；存量归属不明会中止迁移。不会从当前 Lot 猜造投放或完成事实。具有工单/快照的管理批次受新增基线保护，老的无工单结构不因此获得首版工程批入口。

本地迁移前查询 Lot/Wafer/Carrier 均为 0；已在 Git 忽略的 `.local/backups/pre-f4-20261112.dump` 保存迁移前备份。没有改动管理员凭据，也没有向本地业务库写入演示生产数据。空库迁移、全迁移回滚及重新迁移沿用数据库测试验证；备份恢复演练仍随 Docker 后置，不以保存备份代替恢复验收。

`scripts/app.py provision` 已增量授予 F4 所需表权限；`serve` 改用 `mesagent_app` 运行账号，迁移/初始化仍用所有者。运行角色可执行 F4 写用例，但无生产 schema CREATE 或血缘 DELETE 权限，已用临时限权角色验证。

## 4. 接口与页面适配

- F4 回执作用域按用户/Fab/方法/规范化目标路径生成摘要，以适配现有 128 字符字段；业务载荷规范化对象字段、集合顺序、ID 和数字。成功重放先于旧版本校验，但每次重新验证当前权限/厂区；血缘内部键使用命令 ID。
- 创建返回资源及 ETag；拆合批返回新批与各源版本；装载返回各资源版本；重放响应带 `Idempotency-Replayed: true`。普通请求仍受 64 KiB 限制。
- 请求 schema 已由 OpenAPI 导出到 [api.d.ts](../../web/src/generated/api.d.ts)。单次最多 100 片、合批 2～20 源；多对象装载必须提交全部相关版本和预期原槽。
- 当前排序固定为稳定 ID；时间线按事件时间/ID排序，暂无通用自选排序参数。血缘最大 10 层，每层最多读取 1000 明细并显式标记截断；履历每类最多 1000 条，`truncated` 不冒充完整历史。列表使用服务端分页；页面候选最多 100 页，海量候选检索需后续扩展。
- 详情显示业务编号、状态和时间，血缘可展开逐片 From/To；载具当前槽位以表格呈现，图形化槽位面板尚未提供。详情历史页面目前取前 100 条，更多历史可由分页 API 获取，完整导出属于后续追溯模块。
- WebUI 保留请求参数和幂等键供网络失败重试；版本/状态冲突要求刷新及重新核对。自动化浏览器验收修复了首次打开表单的默认值问题。

## 5. 已执行验证与证据

验证命令：`dotnet build MESagent.sln --no-restore -m:1 -p:UseSharedCompilation=false`、通过 `scripts/database.py` 的环境配置执行全量 `dotnet test`、`npm --prefix web run build`、`python3 scripts/e2e.py`。测试均使用自动创建并清理的隔离数据库。

- 后端构建：0 警告、0 错误。
- 数据库 **17 项**、HTTP/协作 **39 项**通过，共 **56 项**；其中 [ProductionTests](../../tests/MESagent.Api.Tests/ProductionTests.cs)新增 **14 项**。
- Chrome 浏览器流程通过：包含既有 F1/F2/F3 回归及 [F4 页面流程](../../web/tests/production-flow.ts)。实际完成 5 片建批 → 投放 → 拆出 2 片/源余 3 片 → 合为新批 5 片 → 载具装载 → 晶圆履历查询。
- 前端生产构建通过；仍有既有包体积提示，未作为性能验收。页面截图留在 Git 忽略的 `web/test-results/production-genealogy.png`、`production-carrier.png`；本地测试日志在 `.local/`。

| 场景映射 | 本轮证据与实际边界 |
| --- | --- |
| A01/A07/A19/A21/A22/A26 | API 验证固定基线、首次投放、2+3 拆合、步骤继承及成员守恒；浏览器验证操作流程 |
| A03/A04/A23 | 验证空片、未知字段、跨工单合批、重复批号失败回滚；未逐一自动覆盖全部非法组合 |
| A09 | 并发竞争最后投放额度只能成功一方；报废补投政策未开放 |
| A13～16/A18/A27 | API 验证槽位竞争、交换、停用占用拒绝、取消关闭装载/归属；浏览器验证装载；其它边界仍需扩展用例 |
| A08/A24 | 两条 Hold 解除一条仍拒绝；活动 Q-time/阻塞触发配置有代码拒绝，全部规则组合未逐项自动测试 |
| A29/A30/A38 | 内部能力测试验证 Running Run 不可完工、可信终点唯一计数、保留成品归属、全报废不计完成及历史保护；使用隔离夹具，不是 F5/F6 真实 UI 流程 |
| A32～37 | API 验证规范化重放、旧 ETag 重试、同键异参、撤销厂区后拒绝回执、越权/不可见对象及拆批失败回滚；未覆盖所有故障注入点 |
| A39/A41 | 浏览器基础闭环、迁移回滚/重建及最小权限角色用例通过；存量多工单歧义数据的迁移演练未单独执行 |
| A05/A06/A10/A11/A12/A17/A20/A25/A28/A31/A40/A42 | 实现中的部分规则有间接覆盖，但不标整项通过；特别是 F5/F6 并发、部分报废后完工、完整统计回补和全部上限边界仍待专门验收 |

## 6. 下一步

GAP-08 的结构、稳定归属、首次投放及内部完成能力已落地；真实终点集成与统计重算仍未关闭。GAP-09 待 F5/F6 真实流程联调；F4/F5 完成后必须返回 F3 完成 GAP-01，并在 F5 回补 F2 的 GAP-06。F4-07、无工单工程批、复杂更正/返工、Agent 与 Docker 继续按原边界预留。
