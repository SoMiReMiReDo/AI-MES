# F6 实现与本地验收

更新：2026-11-17｜F6-01～04 首版文档、代码、本地数据库及浏览器验收完成。

设计入口：[功能设计](quality-exception-disposition.md)、[接口与验收](quality-exception-disposition-api-and-acceptance.md)。本文仅记录实际实现，长期需求仍见功能目录。

## 1. 实现范围

- 独立 `QualityBackend` 通过既有认证应用入口执行，复用 Cookie/防伪、权限与厂区范围重载、一致性查询、单事务及全局写锁；写请求使用类型化白名单、版本和规范化幂等回执，撤权后不得重放。
- 原因码/异常类型、异常确认/指派/处置/关闭、整改完成/复核；阻塞属性按登记时快照保存。阻塞异常原子建立 Hold，关联对象校验同厂和生产上下文。
- Lot 多条 Hold 与逐条解除；解除之后仍有独立质量放行门禁。完整 Passed 的等待处置可 Continue；Failed/中止可 Retry，同站生成 VisitNo+1 并重新完成作业记录，保留原 Run/Track/结果。
- 版本化规格及模板，草稿编辑、发布冻结和复制新版本；数值闭区间/单边限、布尔与文本期望值均由服务端判定。检验支持 Lot/逐片、样本/样点、单位、量测设备，历史保存规格快照。失败检验与异常/Hold 同事务提交，合格复检不自动放行。
- 报废核对片集与数量并保存原 Lot/Run，通过 F4 关闭归属及槽位；全报废单独关闭，部分报废后的剩余 Passed 片可继续终点。F3 真实进度/关闭及 F4 批次完工显示同步回补，移除旧的“待执行模块接通”占位。
- React 质量管理含异常与整改、冻结/放行、检验、逐片报废和质量配置；按钮按权限显示，失败保留输入，提交仍由后端重新校验。OpenAPI 请求契约已生成并用于质量表单；详情显示晶圆号、冻结原因、责任人及规格期望值。

## 2. 主要代码

| 层次 | 文件/目录 |
| --- | --- |
| Domain | `QualityRules.cs`、`Entities/Quality.cs` |
| Application | `QualityExceptionDisposition.cs`；质量权限目录及生产内部协作契约 |
| Infrastructure | `QualityExceptionDisposition/`（入口、查询、配置、异常、检验、处置、共享门禁） |
| 跨模块 | `DispatchAndProductionExecution/QualityRecovery.cs`、F4 `Lifecycle.cs`/步骤生成、`ProductionProgress.cs`、F3 关闭门禁 |
| API | `Program.cs` 的 `/api/v1/quality` 路由及类型化契约 |
| Web | `quality.tsx`、`qualityConfiguration.tsx`、`qualityInspection.tsx`、`qualityShared.tsx` |
| 测试 | `QualityTests.cs`、`web/tests/quality-flow.ts`；既有 F1～F5 回归保留 |

## 3. 数据库与本地升级

第 8 条迁移 `20261117060236_QualityExceptionDisposition` 已应用到本地数据库，表数保持 119 业务/支撑 + 7 Identity。新增异常阻塞快照、模板生命周期、规格期望值、检验关联异常、质量放行恢复上下文和命令唯一索引；增加检验/放行不可变、Hold 单次释放、异常上下文保护。既有已使用模板冻结，旧参数 JSON 不猜测转换。

迁移前已在 Git 忽略的 `.local/backups/` 建立完整备份。迁移前后账号、Lot、Wafer、首次投放、终点完成、Run、报废记录数量核对一致。新增两个系统质量配置用于不合格自动冻结，前台拒绝修改保留代码；其他原因码/类型均由用户维护。

`python3 scripts/app.py provision-quality` 仅增量授予既有 `mesagent_app` 的质量表 INSERT/UPDATE；不可变表只授 INSERT。未修改密码、原角色授权，未增加 DDL/DELETE。新装环境的 `provision` 同步包含质量权限。权限目录迁移只给既有 SystemAdmin 添加新动作，普通角色由管理员显式授权。

字段字典 `docs/development-process/05-database-catalog.md`、幂等 DDL `database/mesagent.sql` 和 `web/src/generated/api.d.ts` 已同步。

## 4. 验证证据

- 后端完整构建：零警告、零错误。前端构建通过；保留现有包体积提示，未升级依赖。
- 17 项真实数据库测试 + 70 项 API/协作测试通过，共 **87 项**；F6 新增13项。覆盖异常/整改状态、复合 Hold、终点 Continue、Failed/Aborted 重试及新到访作业门禁、三种量测类型和版本冻结、失败检验去重、长模板名称/数值极值校验、跨厂拒绝、部分/全报废、并发/幂等/版本、审计失败回滚、最小质量角色/撤权，以及非超级运行角色无 DDL/DELETE 的真实收尾。
- 完整导出幂等 DDL 已在隔离空库连续执行两次通过（8 条迁移）。数据库测试包含全迁移回滚/重建；测试在随机临时库运行并清理，与用户演示库分离。
- Chrome 浏览器全流程通过：保留 F1～F5 回归，并完成质量原因码、规格/模板发布、手工 Hold/质量放行、失败检验自动冻结、异常整改完成/复核、处置/解除/关闭、逐片报废及后续真实生产；样本最终首次投放3、完工2、报废1、在制0。

日志位于本机 `/private/tmp/mesagent-f6-*.log`，未纳入仓库。截图在 Git 忽略的 `web/test-results/`。

## 5. 启动与边界

本地 API 已重启并通过健康与 F6 路由检查。验收期间原 PostgreSQL 容器停止，已使用原持久卷恢复并复核服务。

本地沿用 `python3 scripts/app.py serve` 与 `npm --prefix web run dev`；网页新增“质量管理”，新授权需刷新会话信息。Docker 应用打包仍后置，不把已有 PostgreSQL 容器作为完整交付。

跨路线返工、自动质检任务/触发、来料检验、单片 Hold、偏差审批、SPC/FDC/APC、完整 F7 及复杂存量更正仍待后续；首版不提供已完工晶圆报废、历史量测覆盖或绕过门禁的状态编辑入口。
