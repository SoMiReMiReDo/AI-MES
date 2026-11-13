# 功能开发文档

本目录保存 MESagent 各功能的需求与实现设计。文档完成不代表业务已实现；代码与数据库现状见 [项目记忆](../../MEMORY.md) 和 [数据架构](../MESagent-development-guide.md)。

建议阅读顺序：[Fab 功能目录](../development-process/07-fab-function-catalog.md) → [功能实现架构](../development-process/08-feature-implementation-architecture.md) → 对应功能文档。

| 功能 | 文档 | 状态 |
| --- | --- | --- |
| F1-`OrganizationIdentityAccessControl` 组织、身份与访问控制 | [功能设计](organization-identity-access-control.md)、[接口与验收](organization-identity-access-control-api-and-acceptance.md)、[实现与启动](organization-identity-access-control-implementation.md) | 01～04 首版代码已完成；05/06 扩展；Docker 后置 |

后续文档继续使用功能目录 ID，明确已确认范围、设计方案、模型差异、接口与验收。跨模块架构写在 `docs/development-process/`；OpenMES 的 `docs/developer`、`docs/portal` 和课程保持为参考材料。

## F2/F3 本轮文档

| 功能 | 文档 | 状态 |
| --- | --- | --- |
| F2-`ProductProcessVersionManagement` | [能力设计](product-process-version-management.md)、[接口与验收](product-process-version-management-api-and-acceptance.md) | 01～05 首版代码已实现；见下方实现说明 |
| F3-`WorkOrderPlanningExecution` | [能力设计](work-order-planning-execution.md)、[接口与验收](work-order-planning-execution-api-and-acceptance.md) | 01～02 与初始基线已实现；真实生产进度须在 F4/F5 完成后回补 |

[功能空缺与待补全清单](deferred-capabilities.md) 记录预留能力、依赖和回补验收条件。代码任务开始前需与用户确认开发路径和计划。

[F2/F3 实现与本地验收](f2-f3-implementation.md)：代码路径、接口细化、迁移、启动与测试证据。

## F4 本轮文档

| 功能 | 文档 | 状态 |
| --- | --- | --- |
| F4-`LotWaferCarrierGenealogy` | [功能设计](lot-wafer-carrier-genealogy.md)、[接口与验收](lot-wafer-carrier-genealogy-api-and-acceptance.md)、[实现与本地验收](lot-wafer-carrier-genealogy-implementation.md) | 01～05 首版已实现；06 真实终点已随 F5 接通，质量报废仍待 F6；07 与工程批预留 |

2026-11-11 用户确认：建批必须关联已下达工单；拆合批仅非加工中、无有效 Hold，合批同厂/工单/产品修订/路线版本/工艺位置。其余细化规则为设计方案。跨模块实现架构见 [08 第 11 节](../development-process/08-feature-implementation-architecture.md#11-f4-生产功能的实现架构)；F4/F5 完成后必须回补 F3 真实生产进度。

## F5 本轮交付

| 功能 | 文档 | 状态 |
| --- | --- | --- |
| F5-`DispatchAndProductionExecution` | [功能设计](dispatch-and-production-execution.md)、[API 与验收](dispatch-and-production-execution-api-and-acceptance.md)、[实现与本地验收](dispatch-and-production-execution-implementation.md) | 01～05、最小设备前置、F2 执行绑定与 F3 真实进度已实现；74 项数据库/API 测试与 Chrome 流程通过；完整 F7/F6 处置及返工登记 GAP-12～14 |
