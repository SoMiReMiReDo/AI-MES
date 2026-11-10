# MESagent 项目记忆

更新：2026-11-11（Asia/Shanghai）｜阶段：规划｜版本：v0.1

## 已确认

- 建设前后端完备的 MES + Agent + WebUI 系统，后端使用 C#，以 Docker 打包交付。
- 面向**半导体制造：晶圆、批次与设备追踪**。
- 本轮只初始化记忆、梳理框架和规划技术选型，暂不开发。
- 沟通偏好：先总结重点，具体细节追问后展开。

## 当前事实

- 仅有 `src/MESagent` 控制台项目：`net10.0`，SDK 固定为 `10.0.401`，入口输出就绪消息。
- 尚无 ASP.NET Core API、业务 WebUI、Agent、数据库模型、Docker 配置和业务测试。
- `docs/` 既有内容是 OpenMES 的学习和开发参考资料，其 Laravel/React 实现不属于 MESagent。
- 已只读核查本地 OpenMES v0.25.0 后台并验证工单到成品批次的下钻；[沿用建议](docs/planning/01-current-state.md#4-本地-openmes-后台核查与沿用建议) 优先参考看板、追溯、异常和审计界面，晶圆执行规则由 C# 业务层设计，待确认。
- 已初始化 Git 并推送公开仓库 [SoMiReMiReDo/AI-MES](https://github.com/SoMiReMiReDo/AI-MES)：`main` 为默认分支，`dev/backend` 为后端开发分支。
- 本轮仅新增记忆及规划文档、更新根 README；未修改现有代码或启动系统。

## v0.1 建议，尚未冻结

- 后端：ASP.NET Core 10 Web API + EF Core 10 + Npgsql 10，模块化单体。
- 前端：React 19 + TypeScript + Vite + Ant Design 6；React Router、TanStack Query；SignalR 更新生产页面。
- Agent：优先评估 Microsoft Agent Framework（.NET），通过 Microsoft.Extensions.AI 接入模型；先做查询、解释及建议，受控写入后置。
- 数据：PostgreSQL 18；知识检索阶段再引入 pgvector；首版无需独立向量库或消息中间件。
- 交付：Docker 多阶段构建 + Compose，Web/API/Worker/PostgreSQL；Agent 初期作为后端模块。
- 业务起点：Lot/Wafer、Route/Recipe 版本、Track-in/Track-out、Hold/Release、受限拆批合批、设备履历与追溯。

## 待明确

- 晶圆制造或封测的具体工艺、试点产线与角色分工；暂以晶圆制造演示场景设计，不推断完整 Fab 需求。
- 单厂并发规模、可用性与备份恢复目标；暂按单企业单厂试点规划。
- 模型使用云端还是本地、可出域数据范围、预算与模型评测样例。
- 实际设备协议和 ERP 接口；SECS/GEM 联机、设备指令下发均不在首版演示范围。

## 文档入口与下一步

- [现状与差距](docs/planning/01-current-state.md)
- [半导体系统规划](docs/planning/02-system-plan.md)
- [技术选型 v0.1](docs/planning/03-tech-stack.md)

下一步仍是需求收敛：确定一条示例工艺、Lot/Wafer 状态与关键验收场景；用户要求进入开发后，再做依赖兼容性验证和工程初始化。

## 决策记录

| 日期 | 决定 | 状态 |
| --- | --- | --- |
| 2026-11-10 | C# + MES + Agent + WebUI + Docker | 用户确认 |
| 2026-11-10 | 聚焦半导体晶圆、批次与设备追踪 | 用户确认 |
| 2026-11-10 | 当前只规划，不开发 | 用户确认 |
| 2026-11-10 | .NET/React/Agent Framework/PostgreSQL 方案 | v0.1 建议，待验证 |
| 2026-11-11 | 公开仓库 AI-MES，`main` + `dev/backend` 分支 | 用户确认 |
