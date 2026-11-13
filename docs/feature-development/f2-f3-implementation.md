# F2/F3 实现与本地验收

日期：2026-11-11｜范围：F2-01～05、F3-01～02 及初始下达基线。预留能力见[待补全清单](deferred-capabilities.md)。真实生产闭环尚未实现。

## 实现路径

沿用现有模块化单体，不替换认证、数据库或前端框架：

- Domain：`ManufacturingRules` 保存有效期与工单计划规则，沿用已有业务实体。
- Application：沿用现有服务入口及请求/响应契约，新增幂等键、文件响应与能力权限目录。历史 F1 类型名称保持兼容。
- Infrastructure：`ProductProcessVersionManagement/ManufacturingBackend` 及分文件实现工程与工单用例，复用现有事务、会话、厂区权限和数据库写锁；不从页面直接写数据库。
- API：扩展 `/api/v1/engineering/*`、`/api/v1/planning/*` 路由，继续使用 Cookie、防伪、ProblemDetails；发布等修改只允许 POST。
- Web：`manufacturing.tsx`、`engineeringExtras.tsx` 接入原导航，提供主数据/版本表单、发布与复制、配方适用性、编号、指导书及工单执行概况。

F3 的实现当前与 F2 共用制造用例后端及事务设施，代码按 `Planning.cs` 分隔职责；未引入微服务或第二个 DbContext。沿用原有全局写锁适用于当前单厂试点，尚未开展大规模吞吐压测。

## 已实现能力

| 范围 | 实现 |
| --- | --- |
| F2-01 | 产品及修订维护、修订发布/废止/复制、按厂区查询影响工单与 Lot |
| F2-02 | 工序、路线与版本；步骤及点检/输出定义；草稿路线嵌套保存及失败回滚；发布后冻结、复制步骤与定义 |
| F2-03 | Recipe 及版本、适用区间与设备类型校验、候选查询；实际使用记录仍由 F5 接入 |
| F2-04 | 固定前后缀与补零整数编号，预览、正式分配、同键重放、重号检查；工单不传编号时按产品专用/默认 WorkOrder 规则原子分配 |
| F2-05 | 点检、类型与枚举输出、PDF 上传/下载/发布；指导书必须关联草稿路线步骤，发布路线时不允许尚未发布的指导书 |
| F3-01 | 草稿创建编辑、合法版本下达、原子基线快照、无关联 Lot 的取消/关闭、终态保护 |
| F3-02 | 关联 Lot 查询、进度契约及明确缺失依赖；完工量等未知指标返回 null，无人工产量编辑入口 |

工单下达快照包含计划、产品修订、路线、步骤、点检、输出、文档摘要和配方适用配置；不把候选配方认定为实际采用版本。存在生产关联时关闭仍拒绝，等待 F4/F5 提供可信终结判定。

## 接口细化

能力设计和接口文档仍是需求基线；本轮新增以下具体适配：

- `route-steps`、`checklist-items`、`output-definitions` 提供独立 GET/POST/PUT 子资源接口，沿用 xmin；路线版本 PUT 同时支持 steps 内嵌 checklistItems/outputDefinitions 的事务保存。
- 路线版本 GET 包含步骤及子定义；子资源列表支持 routeVersionId/routeStepId 等归属筛选。
- `equipment-options`、`equipment-type-options` 为工程表单提供只读候选，不提供 F7 设备维护功能。
- 产品影响查询返回 workOrders/lots 两个分页对象；工单关联 Lot 返回分页对象。基线列表及按版本查询当前返回快照数组。
- 指导书支持 multipart 上传，也支持页面使用的 JSON/base64；文件本体最多 10 MiB，请求上限仅对上传端点放宽至 15 MiB。普通端点继续采用原 64 KiB 上限。
- 编号规则暂不接受 Pattern/ResetPeriod 写入；固定 None 重置策略，日期重置继续登记 GAP-07。

## 迁移与启动

第 5 条迁移 `20261111120000_ManufacturingPermissions` 已应用到本地数据库：新增工程/工单权限、补授既有系统管理员，并为工单快照添加禁止更新/删除的数据库保护。它是数据与触发器迁移，EF 实体模型无新增字段，因此没有改动模型快照。

本地更新命令：

```sh
python3 scripts/database.py migrate
python3 scripts/app.py provision
python3 scripts/app.py serve
npm --prefix web run dev
```

已更新 provision，只对工程表、工单和快照开放所需写权限；未开放 Lot/Wafer/过站业务写权限。普通角色需要管理员按职责配置新权限，角色名不自动授予业务能力。

指导书路径由 `MES_DOCUMENT_ROOT` 配置；本地脚本使用 Git 忽略的 `.local/engineering-documents`，文件按 SHA-256 命名。备份需同时包含数据库和该目录。事务失败可能留下无记录的摘要文件，属于可回收孤立文件，当前不自动删除共享文件；生产交付前需随 Docker/备份方案定义清理与恢复机制。

## 验证与限制

测试源：`tests/MESagent.Api.Tests/ManufacturingTests.cs`、既有数据库/身份测试，以及扩展后的 `web/tests/organization-identity-access-control.spec.ts`。

- 17 项真实 PostgreSQL 测试及 25 项 HTTP 集成测试通过；新增覆盖发布前置条件、冻结与复制、快照/幂等、未知进度、PDF 发布下载、嵌套保存回滚、并发编号、跨厂区拒绝和 GET 禁止发布。
- Chrome 完整流程通过：首登改密与 F1 回归、产品创建、工单下达、基线查看、真实进度待接通提示及退出。使用独立临时数据库，不向本地业务库写验收工单。
- 前端构建通过；仍有打包体积提示，未将性能优化或生产部署计入本轮验收。
- 未执行完整 Fab 生产流程、设备联机、负载压测及 Docker 恢复演练；文档中所有单项设计场景不因上述测试总数自动视为全部覆盖。

F4/F5 完成后必须返回 F3 完成 GAP-01；F2 执行绑定见 GAP-06。不得将表结构或测试夹具当作真实生产能力。
