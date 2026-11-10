# MESagent

面向半导体制造的 MES + Agent + WebUI 项目，计划采用 C# 后端并以 Docker 交付。当前处于规划阶段，代码仍是 .NET 10 最小控制台骨架。

## 项目记忆与规划

- [项目记忆](MEMORY.md)：已确认目标、当前事实、建议选型与待明确事项。
- [工作约定](AGENTS.md)：后续协作与记忆维护入口。
- [当前框架与差距](docs/planning/01-current-state.md)。
- [半导体系统规划 v0.1](docs/planning/02-system-plan.md)。
- [技术选型 v0.1](docs/planning/03-tech-stack.md)。

首版围绕 Lot/Wafer、工艺与配方版本、设备履历、生产执行、质量处置和追溯规划。本轮只编写文档，未开始业务开发。

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

以上命令只构建和运行当前控制台，不会启动 MES WebUI。尚未添加数据库、Web 服务、Agent、Docker 配置或测试项目。
