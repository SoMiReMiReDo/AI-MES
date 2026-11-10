# MESagent

面向半导体制造的 MES + Agent + WebUI 项目，采用 C# 后端并以 Docker 交付。已启动数据库开发，建立 PostgreSQL 18 + EF Core 10 数据模型、迁移和真实数据库测试；业务 API、WebUI 与 Agent 尚未实现。

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

首版围绕 Lot/Wafer、工艺与配方版本、设备履历、生产执行、质量处置和追溯建设。当前数据库包含 114 张业务表、7 张 Identity 表、2 个派生视图，另有迁移历史表；按需模块已有结构，业务流程尚待实现。

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
