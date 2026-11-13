# MESagent 数据架构开发文档

更新：2026-11-13｜适用阶段：数据库、F1～F4 基础及 F5-01～05 已实现；完整质量处置、设备资源和 Docker 待后续。

本文说明当前工程如何组织、核心对象如何关联、数据库保护哪些规则，以及开发者如何扩展模型。字段全集见 [数据库字段字典](development-process/05-database-catalog.md)，实施与验证记录见 [数据库实施说明](development-process/06-database-implementation.md)。

## 1. 当前交付与边界

已完成 C# 数据实体、EF Core 映射、PostgreSQL 数据库、版本化迁移、命令行管理工具和真实数据库测试。数据库包含 119 张业务/支撑表、7 张 Identity 表、2 个派生视图，另有迁移历史表；已应用 7 条迁移。

当前已实现 组织结构、人员与场所、账号会话及角色权限 的 API、登录与管理 WebUI，详见 [F1-`OrganizationIdentityAccessControl` 实现说明](feature-development/organization-identity-access-control-implementation.md)。Agent、设备驱动及后台 Outbox 消费程序尚未实现。共炉、光罩、Q-time、返工、资格管理等对象已有结构，不能据此认为这些业务已经可用。

F2 工艺、F3 工单、F4 批次/载具/血缘及 [F5 执行](feature-development/dispatch-and-production-execution-implementation.md)已接通首版。实际 Run 驱动终点完成与工单进度，最小设备台账/人工状态/占用已实现；完整 F6 处置和 F7 资源规则仍待开发。

技术基线：.NET SDK 10.0.401；EF Core / Identity 10.0.12；Npgsql EF Provider 10.0.3；PostgreSQL 18.6。版本集中管理于 [Directory.Packages.props](../Directory.Packages.props)，NuGet 锁文件随项目保存。

## 2. 代码分层和依赖

| 项目 | 当前职责 | 关键入口 |
| --- | --- | --- |
| Domain | 纯 C# 实体与组织/工艺/片集/执行值规则，不依赖 EF Core | [Entities](../src/MESagent.Domain/Entities) |
| Infrastructure | DbContext、Identity、映射/迁移及身份、工艺/工单、生产、执行模块的持久化/事务用例 | [MesDbContext.cs](../src/MESagent.Infrastructure/Persistence/MesDbContext.cs) |
| Application | 共享应用入口、模块类型契约、权限目录与配置，不依赖 EF/Identity | [OrganizationIdentityAccessControl.cs](../src/MESagent.Application/OrganizationIdentityAccessControl.cs) |
| API / WebUI | 认证/防伪/HTTP 与 React 管理、派工和生产执行页面 | [API](../src/MESagent.Api/Program.cs) / [WebUI](../web/src/main.tsx) |
| Database | 建库/升级、迁移状态查询、字段字典导出 | [Program.cs](../src/MESagent.Database/Program.cs) |
| Database.Tests | 在隔离 PostgreSQL 数据库中验证约束、并发和迁移 | [SchemaTests.cs](../tests/MESagent.Database.Tests/SchemaTests.cs) |
| 原 MESagent 控制台 | 保留的启动骨架，不承载 MES 业务服务 | [Program.cs](../src/MESagent/Program.cs) |

项目依赖方向为 Application → Domain、Infrastructure → Application/Domain；API 在组合入口注册 Infrastructure；Database → Infrastructure。Infrastructure 通过 EF Core 的 Npgsql Provider 访问 PostgreSQL。

Application 与 API 已创建。F1-`OrganizationIdentityAccessControl` 提供统一应用入口，身份和数据库事务适配位于 Infrastructure/OrganizationIdentityAccessControl；后续模块继续复用授权边界并抽取独立领域规则。Agent 本轮仅预留位置。

## 3. 数据库模块

所有模块位于一个 PostgreSQL 数据库，使用 schema 区分职责，支持跨模块外键和事务。

| schema | 表数 | 主要职责与对象 |
| --- | ---: | --- |
| organization | 9 | Fab、区域、产线、位置、员工、工位、终端、班次、交接 |
| security | 13 | Identity 的 7 张基础表，以及权限、角色权限、用户直接权限、厂区范围、会话与安全事件 |
| engineering | 14 | 产品/版本、路线/版本/步骤、工序、Recipe/版本/适用关系、编号、输出、点检、文档、Q-time 规则 |
| production | 25 | 工单、Lot、Wafer、归属、载具、步骤执行、加工、Track、血缘、共炉与返工结构 |
| quality | 18 | 原因、异常、Hold、放行、检验、规格、量测、质检任务、报废与逐片处置 |
| equipment | 15 | 设备及腔体、状态、事件、停机、维护、资格、光罩、材料上机 |
| materials | 15 | 公司、物料、来源、物料批次、BOM、领用/耗用、仓库、库存及流水 |
| integration | 9 | 外部系统、设备连接/消息/测点、API Key、导入、标签、自定义字段定义 |
| audit | 5 | 审计、业务事件、附件、幂等命令回执、Outbox |

schema 是逻辑边界，不等于独立服务，也不自动提供用户权限隔离。

## 4. 核心关系与读取方式

### 4.1 产品、路线与配方

Product 保存产品身份，ProductRevision 保存修订版。Route 与 RouteVersion 区分路线身份和版本；RouteStep 表示该版本中的一个具体步骤，并引用 Operation。同一 Operation 可以出现在不同步骤中，因此不能用工序名称代替步骤 ID。

Lot 保存 ProductRevision 和 RouteVersion 引用。RecipeVersion 保存配方版本，RecipeApplicability 描述版本对步骤、设备的适用区间；实际加工通过 ProcessRun 固定当次使用的 RecipeVersion。

已发布版本内容及 RouteStep 的点检/输出定义受到数据库保护。修改正式工艺应创建新版本；新版何时对哪些 Lot 生效，需由后续业务服务控制。

### 4.2 Lot、Wafer 与载具

WorkOrder 表示生产需求；Lot 表示在制批次；Wafer 表示永久追踪的单片身份。LotMembership 记录晶圆归属的有效时间区间：`valid_to IS NULL` 为当前归属，历史查询按事件时间选择对应区间。

Carrier 是物理载具，CarrierSlot 是槽位，CarrierLoad 是装载历史。载具身份、Lot 身份和 Wafer 身份分别保存，换载具不等于拆批。

拆合批使用 GenealogyEvent 记录动作，GenealogyDetail 记录每片晶圆的源 Lot 与目标 Lot，LotMembership 保存实际归属变化。仅有这些表并不保证拆合批守恒，未来命令必须在同一事务核对晶圆集合并更新相关记录。

### 4.3 执行与追溯

StepExecution 表示 Lot 对路线步骤的一次到访，`visit_no` 区分重复到访。ProcessRun 表示一次实际加工，关联设备、配方和 Track-in/out 时间。ProcessRunWafer 固定该次加工的参与晶圆集合，TrackEvent 记录操作事件及幂等键。

读取单片履历时，从 Wafer 查询 ProcessRunWafer，再连接 ProcessRun、StepExecution、RouteStep 和设备/配方版本。历史 Lot 归属从 LotMembership 的时间区间查询，不能拿当前归属解释以前的加工。

### 4.4 质量与数量

Issue 是异常管理对象，HoldRecord 是独立阻塞记录，LotRelease 是生产/质量放行记录。关闭异常、投放路线和解除 Hold 是不同动作。

Measurement 保存规格版本、单位、实测值、采样范围和判定；WaferDisposition 保存单片处置，ScrapEntry 保存报废登记。报废命令需要同步处理晶圆生命周期、有效归属、处置及审计。

| 数量/时长 | 当前来源 | 使用约定 |
| --- | --- | --- |
| Lot 当前片数 | `production.lot_quantities` 的有效归属计数 | 不再增加另一份可自由编辑的当前数量 |
| Lot 报废量 | 同一视图中的逐片报废处置计数 | 报废记录需与实际晶圆明细一致 |
| Lot / 工单完工量 | 对象中的完工快照字段 | 由未来终点完成命令维护，不累加各站通过量 |
| 设备状态时长 | `equipment.state_durations` | 根据起止时间计算；未结束区间随查询时间变化 |

## 5. 持久化开发约定

表名、列名使用 snake_case，C# 使用 PascalCase。业务主键采用 UUID；业务时间使用 UTC 的 DateTime，对应 PostgreSQL timestamptz。班次日期/时刻采用 DateOnly / TimeOnly。物料数量和测量数值使用 numeric(20,6)，具体工艺的范围与精度仍需验证。

JSONB 用于参数、载荷、扩展和快照；Lot、Wafer、版本、设备、Hold 等核心关联应继续使用显式外键。字段类型、长度和可空性由各模块 `FabModel.*.cs` 配置；跨对象复合外键及特殊索引集中在 [FabModel.Integrity.cs](../src/MESagent.Infrastructure/Persistence/FabModel.Integrity.cs)。

业务实体使用 PostgreSQL 系统列 `xmin` 作为 EF 乐观并发令牌。读取后再修改时，若记录已被其他事务改变，SaveChanges 抛出并发异常；F1-`OrganizationIdentityAccessControl` API 使用 ETag/If-Match 并返回 412 冲突；未来生产 API 同样不能直接覆盖。

MesDbContext 在常规 SaveChanges 路径更新 UpdatedAt；直接 SQL 或批量更新不会自动经过这一逻辑，开发者需显式处理时间及并发条件。DbContext 本身没有自动生成审计、Outbox 或完整业务事务。

## 6. 数据库保护与应用层责任

| 已有数据库保护 | 仍需应用层实现 |
| --- | --- |
| 外键、唯一键、部分数量/时间 CHECK | 状态机、输入语义与操作权限 |
| 晶圆归属、槽位占用和设备状态的区间互斥 | 实际晶圆集合与加工参与集合一致 |
| 同设备/同执行的 Running 唯一性 | 设备资格、Recipe 匹配、Hold 阻塞检查 |
| 幂等键唯一性 | 请求内容校验、重复响应重放 |
| xmin 冲突检测 | 冲突处理及事务重试策略 |
| 关键事实禁止修改/删除、发布版本保护 | 更正流程、版本审批与生效范围 |
| 多条 Hold 分别保存 | 解除权限、全部阻塞解除后的可执行判断 |

未来写命令建议按“确认用户范围 → 查幂等回执 → 读取并校验当前状态 → 在事务中修改业务事实并写审计/Outbox → 提交”组织。幂等唯一键冲突后应核对请求摘要和既有结果，不能把所有数据库冲突当作成功。

例如拆批命令需要锁定或并发校验源/目标 Lot，确认未加工、工艺位置兼容、晶圆集合合法，然后同步写血缘、关闭旧归属、创建新归属及审计。数据库的区间互斥提供最后一层保护，但不会替代完整业务检查。

模型输出和设备原始消息不得直接成为生产数据写入指令；后续 Agent、WebUI 与设备适配入口均应调用受控应用服务。

## 7. 本地开发入口

在仓库根目录执行：

```sh
dotnet restore MESagent.sln --locked-mode
dotnet tool restore
python3 scripts/database.py init
python3 scripts/database.py up
python3 scripts/database.py migrate
python3 scripts/database.py status
```

连接地址为 `127.0.0.1:55432/mesagent`，本地迁移账号为 `mesagent_owner`，密码位于 Git 忽略的 `.env`。脚本不会覆盖已有 `.env`。数据库由 [compose.yaml](../compose.yaml) 管理，使用独立持久卷；`stop` 停止容器但保留数据。

当前账号用于本地建库和迁移。F1-`OrganizationIdentityAccessControl` 已提供独立运行角色配置草案及应用层厂区过滤；当前本地开发命令仍使用开发连接，生产最小权限账号联调随交付阶段验证。未实现数据库 RLS。

## 8. 修改模型与迁移流程

1. 在 Domain 对应模块修改实体；同步修改 Infrastructure 的字段映射、外键、索引和 CHECK。
2. 对新业务不变量增加真实 PostgreSQL 测试；需要数据库专用保护时新增迁移 SQL，不修改已应用资源。
3. 构建并生成增量迁移，检查生成内容是否有意外删除、类型转换或必填字段回填问题。
4. 在本地或隔离测试库应用迁移，运行测试，再导出 SQL 和字段字典。

```sh
dotnet build MESagent.sln --no-restore
dotnet ef migrations add ChangeName --project src/MESagent.Infrastructure --startup-project src/MESagent.Database --output-dir Persistence/Migrations
python3 scripts/database.py migrate
python3 scripts/database.py test
dotnet ef migrations script --idempotent --project src/MESagent.Infrastructure --startup-project src/MESagent.Database --output database/mesagent.sql
python3 scripts/database.py catalog
```

迁移和模型快照是结构变更的版本记录；[database/mesagent.sql](../database/mesagent.sql) 是导出物，不手工修改后当作新的模型来源。`catalog` 今后输出到 `docs/development-process/05-database-catalog.md`。本次目录调整仅搬迁原文件，没有重新生成或修改原有六份文档。

## 9. 测试与问题定位

已记录 17 项真实 PostgreSQL 测试通过，覆盖约束、区间冲突、幂等、xmin、回滚、发布保护、审计、库存和派生数量；新增 17 项 F1-`OrganizationIdentityAccessControl` HTTP 集成测试通过。测试在随机命名的 `mesagent_test_*` 数据库运行并清理，缺少测试连接时会失败，不用内存数据库代替。本轮 F1-`OrganizationIdentityAccessControl` 开发已执行上述 34 项测试；浏览器与部署验证边界见 F1-`OrganizationIdentityAccessControl` 实现记录。

| 现象 | 优先检查 |
| --- | --- |
| 连接失败 | Docker 状态、55432 端口、`.env`；修改密码变量不会自动修改已有卷内角色密码 |
| 外键或 CHECK 拒绝写入 | 实际对象关联、版本归属、时间顺序和数量范围 |
| 唯一键或区间冲突 | 重复幂等键、未关闭的归属/装载/加工记录、重叠历史区间 |
| EF 并发异常 | 是否使用旧读数更新；重新读取后由业务规则决定重试 |
| 发布版本/历史修改被拒绝 | 应创建新版本或追加更正事实，而非覆盖原记录 |
| 模型与库不一致 | 迁移是否生成并应用，是否绕过迁移手工修改数据库 |

容器重启和空业务库的结构备份恢复已验证；带生产数据、附件和角色权限的完整恢复演练仍待实施。

## 10. 文档维护

`docs/development-process/` 保存需求、技术选型、字段取舍及实施阶段记录；本开发文档描述当前架构和日常开发入口。OpenMES 的 `docs/developer`、`docs/portal` 和课程继续保留为参考，不写入本项目实现结论。

重要进展同步到根目录 MEMORY.md。未来修改数据结构时，同时更新迁移、测试、导出 SQL、字段字典和本开发文档的相关章节，并注明哪些是实际实现、哪些仍为建议。
