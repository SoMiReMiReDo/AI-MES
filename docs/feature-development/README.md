# 功能开发文档

本目录保存 MESagent 各功能的需求与实现设计。文档完成不代表业务已实现；代码与数据库现状见 [项目记忆](../../MEMORY.md) 和 [数据架构](../MESagent-development-guide.md)。

建议阅读顺序：[Fab 功能目录](../development-process/07-fab-function-catalog.md) → [功能实现架构](../development-process/08-feature-implementation-architecture.md) → 对应功能文档。

| 功能 | 文档 | 状态 |
| --- | --- | --- |
| OrganizationIdentityAccessControl 组织、身份与访问控制 | [功能设计](organization-identity-access-control.md)、[接口与验收](organization-identity-access-control-api-and-acceptance.md)、[实现与启动](organization-identity-access-control-implementation.md) | 01～04 首版代码已完成；05/06 扩展；Docker 后置 |

后续文档继续使用功能目录 ID，明确已确认范围、设计方案、模型差异、接口与验收。跨模块架构写在 `docs/development-process/`；OpenMES 的 `docs/developer`、`docs/portal` 和课程保持为参考材料。
