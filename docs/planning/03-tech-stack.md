# 技术选型 v0.1

日期：2026-11-10｜状态：推荐草案。C#、半导体方向、完整前后端及 Docker 已由用户确认，其余是当前建议；未安装或联调以下组件。

## 1. 推荐组合

| 层次 | 推荐 | 本项目选择理由 |
| --- | --- | --- |
| 后端 | C# / .NET 10、ASP.NET Core 10 Web API | 延续现有工程；统一承载业务、权限和 Agent 接口 |
| 架构 | 模块化单体 + 独立 Worker | 先让跨模块事务、部署和排查保持简单 |
| ORM | EF Core 10 + Npgsql EF Provider 10 | 统一迁移、关系映射、事务与并发处理 |
| 前端 | React 19 + TypeScript + Vite | 与现有 React 学习资料衔接；独立 SPA 适合内部操作台 |
| UI | Ant Design 6 | 采用表格、表单、树、弹窗等组件建设管理与生产界面 |
| 状态与路由 | TanStack Query + React Router；局部状态用 React | 区分服务端数据缓存、页面导航和临时 UI 状态 |
| 实时 | ASP.NET Core SignalR | 生产状态通知；Agent 文本流首版也可复用同一传输 |
| 认证 | ASP.NET Core Identity + Cookie + Policy 授权 | 单站点登录、角色与操作权限；企业 SSO 后续接入 |
| Agent | Microsoft Agent Framework（.NET）优先候选 | 复用 C# 工具与依赖注入，减少独立 Python 服务的维护面 |
| 模型抽象 | Microsoft.Extensions.AI | 通过接口隔离模型服务；具体模型供应商/型号待定 |
| 数据库 | PostgreSQL 18 | 作为业务事务、履历、审计和会话的统一存储 |
| 知识检索 | PostgreSQL + pgvector，P3 后按需加入 | SOP 知识检索可与现有数据库协同；生产状态始终查业务工具 |
| 异步任务 | PostgreSQL Outbox + .NET Worker | 事务写入待发事件；提交后发送通知，按至少一次投递设计幂等 |
| 打包 | Docker 多阶段构建 + Docker Compose | 统一 Web/API/Worker/数据库的开发与单机交付 |
| 可观测与测试 | 结构化日志、OpenTelemetry；xUnit、Vitest、Playwright | 跟踪 HTTP/工具/任务，验证领域、前端交互与完整业务链 |

组件能力依据：[Npgsql 10](https://www.npgsql.org/efcore/release-notes/10.0.html)、[React 版本](https://react.dev/versions)、[Vite](https://vite.dev/guide/)、[Ant Design](https://ant.design/docs/react/introduce/)、[TanStack Query](https://tanstack.com/query/latest/docs/framework/react/overview)、[SignalR](https://learn.microsoft.com/en-us/aspnet/core/signalr/introduction?view=aspnetcore-10.0)、[Identity](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/identity?view=aspnetcore-10.0)。选择理由是对本项目的判断，不是官方推荐组合。

## 2. 版本策略

- `.NET 10` 是 LTS，官方支持周期至 2028-11-14；延续本地 SDK `10.0.401` 作为现状基线，开发启动时再核对补丁，不在本轮升级。见 [.NET 支持策略](https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core)。
- EF Core 与 Npgsql Provider 使用兼容的 10.x 组合，NuGet 包版本集中管理并锁定；发布前实际验证迁移、事务、并发及数据库连接。
- 前端选定 React 19 / Ant Design 6 主线；Vite、TypeScript、路由及 Query 的精确版本在 P1 验证后锁入 lockfile。Node 24 系列作为拟议构建环境，使用 npm 与 `npm ci` 保持简单。
- PostgreSQL 18 为当前方案，官方支持表列为受支持版本。OpenMES 参考环境的 17 不构成新项目的版本约束。见 [PostgreSQL 版本策略](https://www.postgresql.org/support/versioning/)。
- Agent Framework 的具体 .NET 包、模型连接器和工作流组件分别核实稳定性；不能把框架总体说明视为所有集成都已稳定。当前官方概览中的 Foundry 安装示例仍使用 `--prerelease`，因此本轮不锁定该连接器。
- 最终交付记录 SDK、NuGet/npm 依赖、数据库镜像和应用镜像摘要；本表不冒充已经验证的完整依赖清单。

## 3. Agent 框架比较与边界

| 候选 | 适配判断 | v0.1 决定 |
| --- | --- | --- |
| Microsoft Agent Framework | 提供 .NET Agent、工具、会话与工作流能力，符合统一 C# 技术栈 | 优先验证，只引入首版所需功能 |
| Microsoft.Extensions.AI + 显式 C# 流程 | 是模型客户端抽象与辅助组件，不是完整 Agent 编排框架 | 若目标连接器或框架版本不满足要求，用于首版有限工具调用的退路 |
| Semantic Kernel | 官方 Agent Framework 将其企业特性与 AutoGen 抽象整合进新框架 | 本项目无既有 SK 资产，不同时维护两套编排层 |
| LangGraph（Python 独立服务） | 强调持久状态、显式图流程与人工介入；需增加服务部署和跨语言契约 | 有复杂长任务或 Python 专用依赖时再评估 |

依据：[Agent Framework 概览](https://learn.microsoft.com/en-us/agent-framework/overview/)、[官方仓库](https://github.com/microsoft/agent-framework)、[Microsoft.Extensions.AI](https://learn.microsoft.com/en-us/dotnet/ai/microsoft-extensions-ai)、[LangGraph](https://docs.langchain.com/oss/python/langgraph/overview)。

首版一个领域助手即可，通过显式工具查询 MES；不把“多 Agent”当成必需功能。应用层负责状态规则、权限、人工确认和审计，框架升级不应改变这些约束。云端/本地模型选择依据数据可出域要求、中文能力、工具调用可靠性和成本评测再确定。

## 4. 主要替代方案与采用条件

| 选择点 | 当前建议 | 何时重新评估 |
| --- | --- | --- |
| ASP.NET Core / 大型应用平台 | 直接使用 ASP.NET Core | 需要成熟 SaaS 租户管理且团队熟悉相应平台时，再比较 ABP 等方案 |
| React / Vue / Blazor | React | 团队已有 Vue 或 Blazor 主力经验时，重新比较维护成本 |
| SPA / 服务端渲染 | SPA | 出现面向公众的 SEO 或服务端渲染需求时再评估 |
| PostgreSQL / SQL Server | PostgreSQL | 工厂已规定 SQL Server、运维体系或供应商接口要求时调整 |
| 自建有限工具流程 / Agent Framework | Agent Framework 优先验证 | 核心所需能力只有预览实现或兼容性不达标时采用有限流程 |
| Compose / Kubernetes | Compose 单机试点 | 明确多节点、高可用与集群运维能力后再升级部署方案 |

首版暂不加入 Redis、RabbitMQ/Kafka、独立向量数据库和 Kubernetes。增加组件应有容量、吞吐或隔离需求证明。知识检索启用 pgvector 时再验证扩展版本、索引和权限过滤，见 [pgvector 官方说明](https://github.com/pgvector/pgvector)。

Docker Compose 适用于多容器配置和单机部署场景，见 [官方用途说明](https://docs.docker.com/compose/intro/features-uses/)。它不替代数据库备份、密钥管理或高可用设计；具体交付职责见 [系统规划](02-system-plan.md)。

## 5. 开发启动后的选型验证

1. .NET/EF/Npgsql/PostgreSQL：完成真实数据库迁移、并发冲突、重复请求和回滚验证。
2. React/UI/实时：一个真实 CRUD 页面完成登录、字段错误、越权、SignalR 重连与缓存更新验证。
3. Agent：用选定模型验证工具 schema、流式返回、中文场景、超时/取消、身份传递和审计；失败时验证降级路径。
4. Docker：从空环境构建启动，完成初始化、健康检查、重启保留数据以及备份恢复。

以上验证均未执行。本轮的完成标准是文档可追溯、方案与现状分离、后续开发有清晰入口。
