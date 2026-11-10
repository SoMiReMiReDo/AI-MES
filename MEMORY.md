# MESagent 项目记忆

更新：2026-11-11（Asia/Shanghai）｜阶段：数据库基础已实现｜版本：v0.2

## 已确认

- 建设前后端完备的 MES + Agent + WebUI 系统，后端使用 C#，以 Docker 打包交付。
- 面向**半导体制造：晶圆、批次与设备追踪**。
- 2026-11-11 用户明确要求按 Fab 字段清单与文档技术栈建立数据库，已启动数据库开发；WebUI、业务 API 与 Agent 尚未进入实现。
- 沟通偏好：先总结重点，具体细节追问后展开。

## 当前事实

- 保留原 `src/MESagent` 控制台；新增 Domain、Infrastructure、Database 迁移工具及真实 PostgreSQL 测试项目，SDK 仍为 `10.0.401`。
- 已建立 PostgreSQL 18.6 数据库：114 张业务表、7 张 Identity 表、2 个派生视图及 EF 迁移历史表；3 条迁移已应用，17 项真实数据库测试通过。
- 本地 Docker 服务 `mesagent-postgres-1`，数据库 `mesagent`，仅绑定 `127.0.0.1:55432`，使用独立卷；密码在 Git 忽略的 `.env`，不记入记忆或仓库。未修改 OpenMES 参考服务。
- 已验证外键、数量/时间、归属/槽位互斥、幂等键、xmin 并发、历史与发布版本保护、事务及迁移回滚；具体操作与验证见数据库实施说明。
- 尚无 ASP.NET Core API、WebUI 或 Agent。表结构不代表过站权限、拆合批守恒、受控报废等应用层规则已实现。
- `docs/` 既有内容是 OpenMES 的学习和开发参考资料，其 Laravel/React 实现不属于 MESagent。
- 已只读核查本地 OpenMES v0.25.0 后台并验证工单到成品批次的下钻；[沿用建议](docs/development-process/01-current-state.md#4-本地-openmes-后台核查与沿用建议) 优先参考看板、追溯、异常和审计界面，晶圆执行规则由 C# 业务层设计，待确认。
- 已初始化 Git 并推送公开仓库 [SoMiReMiReDo/AI-MES](https://github.com/SoMiReMiReDo/AI-MES)：`main` 为默认分支，`dev/mes-backend` 为后端开发分支。
- [Fab 字段保留清单](docs/development-process/04-fab-data-field-retention.md) 已作为数据库基础；按需对象先建结构，工艺细节仍待场景收敛。

## 技术选择与建议

- 数据库已采用 EF Core / Identity 10.0.12 + Npgsql EF Provider 10.0.3，集中锁版本；ASP.NET Core 10 Web API 与模块化单体仍按规划推进。
- 前端：React 19 + TypeScript + Vite + Ant Design 6；React Router、TanStack Query；SignalR 更新生产页面。
- Agent：优先评估 Microsoft Agent Framework（.NET），通过 Microsoft.Extensions.AI 接入模型；先做查询、解释及建议，受控写入后置。
- 数据：PostgreSQL 18.6 已运行，Docker 镜像固定摘要；pgvector、独立向量库或消息中间件尚未引入。
- 交付：Docker 多阶段构建 + Compose，Web/API/Worker/PostgreSQL；Agent 初期作为后端模块。
- 业务起点：Lot/Wafer、Route/Recipe 版本、Track-in/Track-out、Hold/Release、受限拆批合批、设备履历与追溯。

## 待明确

- 晶圆制造或封测的具体工艺、试点产线与角色分工；暂以晶圆制造演示场景设计，不推断完整 Fab 需求。
- 单厂并发规模、可用性与备份恢复目标；暂按单企业单厂试点规划。
- 模型使用云端还是本地、可出域数据范围、预算与模型评测样例。
- 实际设备协议和 ERP 接口；SECS/GEM 联机、设备指令下发均不在首版演示范围。

## 文档入口与下一步

- [数据架构开发文档](docs/MESagent-development-guide.md)：当前分层、数据关系、开发约定、迁移与测试入口。
- 开发过程目录已由 `docs/planning` 改名为 `docs/development-process`，原六份文档内容不变；外部引用及字段字典生成路径已更新。

- [现状与差距](docs/development-process/01-current-state.md)
- [半导体系统规划](docs/development-process/02-system-plan.md)
- [技术选型 v0.1](docs/development-process/03-tech-stack.md)
- [Fab 数据字段沿用与保留](docs/development-process/04-fab-data-field-retention.md)
- [数据库实际字段字典](docs/development-process/05-database-catalog.md)
- [数据库实施与操作](docs/development-process/06-database-implementation.md)

下一步：基于实际数据库确认示例工艺与状态规则，再建设共享的应用服务和 API，落实 Track-in/out、Hold、拆合批及报废的事务边界；现阶段不把预留表结构描述为完整 MES 功能。

## 决策记录

| 日期 | 决定 | 状态 |
| --- | --- | --- |
| 2026-11-10 | C# + MES + Agent + WebUI + Docker | 用户确认 |
| 2026-11-10 | 聚焦半导体晶圆、批次与设备追踪 | 用户确认 |
| 2026-11-10 | 当前只规划，不开发 | 用户确认 |
| 2026-11-10 | .NET/React/Agent Framework/PostgreSQL 方案 | v0.1 建议，待验证 |
| 2026-11-11 | 公开仓库 AI-MES，`main` + `dev/mes-backend` 分支 | 用户确认 |
| 2026-11-11 | 按 Fab 清单与技术栈启动数据库开发 | 用户确认；数据库已建立并验证 |
