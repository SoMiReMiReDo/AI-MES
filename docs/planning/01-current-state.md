# 当前框架与差距

核查日期：2026-11-10。结论：MESagent 是 C# 起步工程，完整前后端尚待建设；已有 OpenMES 文档可供业务学习。

## 1. MESagent 实际状态

| 范围 | 已存在 | 尚缺 |
| --- | --- | --- |
| C# 工程 | `MESagent.sln`、一个 `net10.0` 控制台项目 | Web API、领域与应用层、持久化 |
| 开发配置 | SDK `10.0.401`、Nullable、EditorConfig、VS Code 调试任务 | 前后端联调、自动检查与 CI |
| WebUI | 静态文档门户及课程 | 登录、生产页面、API 客户端、实时订阅 |
| Agent | 无运行时代码或依赖 | 模型适配、工具、会话、评测及审计 |
| 数据与部署 | 无数据库配置或容器编排 | 表结构、迁移、备份、Dockerfile、Compose |
| 版本管理 | `.gitignore` | Git 仓库尚未初始化 |

本机命令实测：`.NET SDK 10.0.401`、`Node v24.18.0`、`Docker CLI 29.8.1`。只确认命令可用，未验证 Docker daemon、数据库或容器运行状态；未执行构建、安装依赖和业务测试。

依据：[项目文件](../../src/MESagent/MESagent.csproj)、[程序入口](../../src/MESagent/Program.cs)、[SDK 配置](../../global.json)。项目文件使用 `Microsoft.NET.Sdk`，入口只有一行输出，没有 Web Host 和包引用。

## 2. 已有 OpenMES 参考框架

参考源码位于 `/Users/seal/Desktop/myLog/program/SKhynix/OpenMes`，不属于本项目代码。此次只读核对其 `backend/composer.json`、`backend/package.json`、`docker-compose.yml`；其他业务行为依据已有手册，未重新运行或完整审计外部项目。

| 层次 | 依赖声明或资料记载 | 对新系统的参考价值 |
| --- | --- | --- |
| 后端 | Laravel `^12.0`、Eloquent、Sanctum | 工单/批次、事务、权限和数量边界 |
| 前端 | React `^19.2.6`、Inertia `^3.3.0`、Vite `^7.0.7`、Tailwind `^4.0.0` | 生产页面组织、表单和组件思路 |
| 实时 | Reverb、Echo、TanStack DB | 提交后推送、断线重取快照 |
| 数据库 | Compose 声明 `postgres:17-alpine` | PostgreSQL 部署和数据关系参考 |
| 扩展 | 手册记录 Expo、Tauri、设备协议边车 | 后续多端与设备接入的边界参考 |

表中版本是声明约束，不是已安装版本。既有 [项目全景](../developer/01-overview.md) 与 [前端同步说明](../developer/07-frontend-sync.md) 可继续阅读。

## 3. 如何转成 MESagent 的规划

- 以 C# 重建业务服务，保留工单、执行履历、事务、权限等概念；半导体 Lot/Wafer 模型需单独设计。
- React 作为候选前端基础；Inertia 与 Laravel 的页面协议不直接搬入 ASP.NET Core，改用独立 SPA + HTTP API。
- 实时方案采用 SignalR 通知与 API 重查，先不复制 OpenMES 的集合同步基础设施。
- OpenMES 的整批/按量流转只作为参考；晶圆身份、拆合批血缘、重复工序与 Recipe 版本必须由半导体需求定义。
- 本轮不复制外部业务代码，不改写现有参考手册或生成门户。

目标边界见 [系统规划](02-system-plan.md)，具体推荐与替代方案见 [技术选型](03-tech-stack.md)。

## 4. 本地 OpenMES 后台核查与沿用建议

2026-11-10 只读查看 [OpenMES v0.25.0 后台](http://localhost:8080/admin/dashboard)，当前数据以 CNC 加工为主。工单列表及详情、排产、班次监控、设备看板、产品修订、工艺段、物料批次、异常处置、维护及审计等页面可打开。实测 [追溯](http://localhost:8080/admin/traceability) 可按工单号查到成品批次并进入批次详情，抽样批次没有投料及工步记录。[来料质检入口](http://localhost:8080/inspections) 返回 404，设备连接概览受浏览器访问限制。此轮未验证写操作、权限执行和实时设备接入。

| 优先级 | 建议沿用的设计 | MESagent 落点 |
| --- | --- | --- |
| 首版 | 总览卡片下钻、列表筛选、设备卡片、编号追溯、异常及审计界面 | WIP/Hold/设备状态 → Lot/Wafer/设备详情；显示当前工序、加工记录、Recipe 版本、拆合批关系和处置记录 |
| 首版适配 | 工艺段、产品版本、异常阻塞及原因码 | 在 C# 领域层定义 Route/Recipe 版本、Track-in/out、Hold/Release 与单片身份；WebUI 和只读 Agent 共用查询与权限 |
| 后续按需 | 拖拽排产、OEE、保养计划、仓储、客户及包装 | 首版先有待加工 Lot 列表和设备状态；按试点范围决定是否引入其他功能 |
