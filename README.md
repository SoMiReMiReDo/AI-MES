# MESagent

面向半导体制造的 MES + Agent + WebUI 项目。当前 C# 后端和 React WebUI 已实现 F1～F12 的首版范围，支持 Docker 单机交付。Agent 验收随 F13 补齐。

Docker 空实例、演示数据和备份恢复见 [部署操作](deploy/README.md)。

## 本地网页启动与验证

先打开 Docker Desktop。在已完成数据库和管理员初始化的开发环境中，从项目根目录运行：

```sh
docker compose up -d --wait
```

在 Docker Desktop 的 `mesagent` 项目中统一管理 `postgres`、`api` 和 `web`。浏览器打开 [网页工作台](http://127.0.0.1:5173)，API 可通过 [就绪检查](http://127.0.0.1:5080/health/ready) 验证。保存前端代码会触发 Vite 热更新，保存 C# 代码会触发 `dotnet watch` 热更新或重启。

```sh
docker compose ps
docker compose logs -f api web
docker compose stop
```

开发 Compose 沿用现有 PostgreSQL 数据卷、`.env` 中的 API 运行账号和厂区配置，以及 `.local` 下的登录密钥和工艺文档。数据库迁移和运行账号授权仍通过现有脚本执行。首次切换时，先关闭占用 `5080` 和 `5173` 的本机开发服务。

### 在宿主机运行 API 和 Web

需要在宿主机调试时，先执行 `docker compose stop api web` 释放端口，再用两个终端启动：

**终端 1：启动数据库和后端**

```sh
cd /Users/seal/Desktop/myNote/MESagent
python3 scripts/database.py up
python3 scripts/app.py serve
```

**终端 2：启动网页**

```sh
cd /Users/seal/Desktop/myNote/MESagent
npm run dev --prefix web
```

浏览器使用同一网页入口。两个终端保持运行，停止服务时分别按 `Ctrl+C`。

本地 F5 迁移与增量权限已应用，重启后端即可加载新代码。其他已有环境升级时运行 `python3 scripts/database.py migrate` 和 `python3 scripts/app.py provision-execution`；详细步骤见 [F5 实现与本地验收](docs/feature-development/dispatch-and-production-execution-implementation.md)。

初始账号保存在本地 `.local/initial-admin.txt`（Git 忽略）。首次登录需要修改密码；若已经修改，使用新密码。改密后删除初始凭据文件。

建议依次验证：登录与改密 → 组织结构 → 员工、工位与终端 → 账号授权 → 角色权限 → 审计记录。

若提示缺少前端依赖，在项目根目录运行：

```sh
npm ci --prefix web
```

全新环境的数据库、管理员初始化及测试步骤见 [F1-`OrganizationIdentityAccessControl` 实现与开发说明](docs/feature-development/organization-identity-access-control-implementation.md)。

## 项目记忆与规划

- [MESagent 数据架构开发文档](docs/MESagent-development-guide.md)：代码分层、核心数据关系、持久化约定与开发流程。

- [项目记忆](MEMORY.md)：已确认目标、当前事实、建议选型与待明确事项。
- [工作约定](AGENTS.md)：后续协作与记忆维护入口。
- [当前框架与差距](docs/development-process/01-current-state.md)。
- [半导体系统规划 v0.1](docs/development-process/02-system-plan.md)。
- [技术选型 v0.1](docs/development-process/03-tech-stack.md)。
- [Fab 字段沿用与保留清单](docs/development-process/04-fab-data-field-retention.md)。
- [数据库字段字典](docs/development-process/05-database-catalog.md)。
- [数据库实施与连接说明](docs/development-process/06-database-implementation.md)。

首版围绕 Lot/Wafer、工艺与配方版本、设备履历、生产执行、质量处置和追溯建设。当前数据库包含 119 张业务/支撑表、7 张 Identity 表、2 个派生视图，已应用 7 条迁移；F1～F4 基础及 F5-01～05 已实现，后续范围见 [待补全清单](docs/feature-development/deferred-capabilities.md)。

`docs/` 中原有 [OpenMES 开发手册](docs/README.md)、门户与课程仍作为参考材料保存，其 Laravel/React 功能不属于本项目已实现能力。

## 环境

- .NET SDK 10.0.401（`global.json` 允许同一功能带内的更新补丁）。
- VS Code 和 C# Dev Kit / C# 扩展。
- 项目入口：`src/MESagent/Program.cs`。

## 构建与运行

```sh
cd ~/Desktop/myNote/MESagent
dotnet restore MESagent.sln
dotnet build MESagent.sln
dotnet run --project src/MESagent
```

## 编辑与调试

在终端运行 `code .` 打开整个目录。按 `Cmd+Shift+B` 构建；在 C# 文件中设置断点后，按 `F5` 并选择 `Debug MESagent` 开始调试。

以上运行命令只启动原有控制台，不会启动 MES WebUI。

## 数据库

```sh
dotnet restore MESagent.sln --locked-mode
dotnet tool restore
python3 scripts/database.py init
python3 scripts/database.py up
python3 scripts/database.py migrate
python3 scripts/database.py test
```

数据库：`127.0.0.1:55432/mesagent`；账号 `mesagent_owner`，随机密码保存在 Git 忽略的本地 `.env`。Docker 使用独立持久卷，与 OpenMES 参考环境分离。详细约束、迁移及操作说明见 [数据库实施说明](docs/development-process/06-database-implementation.md)。
