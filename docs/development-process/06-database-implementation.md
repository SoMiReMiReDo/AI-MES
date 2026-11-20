# Fab 数据库实施说明

更新：2026-11-19（Asia/Shanghai）。当前结构以字段字典和最新迁移为准。

## 2026-11-19 网站设置增量更新

第 14 条迁移 `20261119113118_WebsiteSettings` 新增 `settings.site_appearance` 和 `settings.fab_schedule_preferences`，在已有库中保留原站点品牌并加入设置权限。已有站点或厂区保存记录时，迁移会阻止降级。

本地开发库备份后执行 `python3 scripts/database.py migrate`，再用 `python3 scripts/app.py provision-settings` 增量授予现有运行角色设置表的读取和必要写入权限。Docker 交付会随迁移完成授权。见 [网站设置开发计划](11-website-settings-plan.md)。

## 2026-11-18 F12 增量更新

第 13 条迁移 `20261118133408_ApplicationAuditReliability` 为审计记录增加厂区和来源模块，为 Outbox 增加业务事件关联、待发索引与死信状态。应用表数保持 134。先执行 `python3 scripts/database.py migrate`，再通过 `python3 scripts/app.py provision-reliability` 给本地运行角色增加队列读取、写入及投递状态字段的更新权限。见 [F12 功能设计](../feature-development/application-audit-reliability.md)。

本地开发库已备份后升级，迁移前后的账号、生产、质量、集成和审计事实数量一致。运行角色增量授权后 API 恢复就绪。全量幂等 DDL 已在隔离空库连续执行两次，模型与迁移快照一致。

## 2026-11-18 F11 增量更新

第 12 条迁移 `20261118111241_DataImportEquipmentIntegration` 为模拟报文增加设备事件关联和保护，为工单导入任务补充厂区、快照及结果。应用表数保持 134。运行角色通过 `python3 scripts/app.py provision-integration` 获取报文追加、连接通信字段及导入任务所需权限。见 [F11 功能设计](../feature-development/data-import-equipment-integration.md)。

本地库已备份后升级，既有账号、工单、批次、Run、设备和导入记录的数量核对一致。空库连续执行两次全量幂等 DDL 通过，迁移快照与模型一致。

## 2026-11-18 F10 增量更新

第 11 条迁移 `20261118072845_WorkbenchDashboardsAlertsScheduling` 新增 `workbench` 的设备产速、计划块、个人订阅和告警回执四张表，并为系统管理员配置排程与产速权限。存在工作台业务记录时会阻止降级。

本地升级前已备份，升级后账号、生产、设备、质量和物料事实数量核对一致。通过 `python3 scripts/app.py provision-workbench` 为现有运行角色增量授权，未修改凭据。全量幂等 SQL 在独立空库连续执行两次通过。见 [F10 功能设计](../feature-development/workbench-dashboards-alerts-scheduling.md)。

## 2026-11-18 F08-01 增量更新

第 10 条迁移 `20261118052836_WaferMaterialOrigins` 给 `materials.material_sources` 增加手工来源厂区、模式互斥约束及唯一索引。表和视图数量保持 130 和 2。既有集成来源保留原外键。已有手工来源时，降级会阻止迁移回退。

本地先备份再升级，账号、工单、批次、晶圆、Run 和物料来源数量核对一致。通过 `python3 scripts/app.py provision-materials` 为现有运行角色增量授权登记写入。全量幂等 SQL 在空库连续运行两次，迁移数均为 10。见 [F08-01 功能设计](../feature-development/material-consumption-inventory.md)。

## 2026-11-17 F7 增量更新

已应用第9条迁移 `20261117095908_EquipmentMaintenanceResources`，新增设备工序策略、耗材要求、光罩装卸及耗材 Run 关联4张表；当前123业务/支撑 + 7 Identity，共130张应用表，派生视图仍2个。事件来源/去重、停机区间绑定、复机、资格与资源历史保护详见 [F7 实现记录](../feature-development/equipment-maintenance-resources-implementation.md)。

本地升级前已备份到 Git 忽略目录，升级后账号及生产事实数量核对一致。`python3 scripts/app.py provision-equipment` 仅增量授权既有运行角色，无密码修改、DDL或生产/资源历史 DELETE；工序耗材要求作为可编辑配置允许删除关系。API 已恢复使用 `mesagent_app`。105项数据库/API测试与 Chrome 全流程通过；幂等 DDL 在隔离空库连续执行两次通过，模型/迁移快照一致。历史有效运行状态仅从本轮启用时点开始完整记录，不追补虚构历史。

### 早期阶段记录

2026-11-17 曾应用第8条迁移 `QualityExceptionDisposition`，补齐质量首版的版本、快照与恢复凭据，当时表数126；见 [F6 实现记录](../feature-development/quality-exception-disposition-implementation.md)。

2026-11-13 应用第7条迁移 `DispatchAndProductionExecution`，补充设备/配方指派、Run 适用关系与执行历史保护，当时为119业务/支撑 + 7 Identity。通过 `provision-execution` 增量授权，未改密码或增加 DDL/DELETE；见 [F5 实现记录](../feature-development/dispatch-and-production-execution-implementation.md)。

## 1. 已建立的结构

技术组合：.NET SDK **10.0.401**、EF Core / Identity **10.0.12**、Npgsql EF Provider **10.0.3**、PostgreSQL **18.6**。NuGet 精确版本在根目录 `Directory.Packages.props` 管理，各项目提交 `packages.lock.json`；本地 `dotnet-ef` 固定为 10.0.12。数据库镜像在 `compose.yaml` 固定版本及 SHA256 摘要。

共 **136 张应用表**，包括 129 张业务与支撑表、7 张 Identity 表。另有 EF 迁移历史表和 2 个派生视图。

| schema | 表数 | 主要对象 |
| --- | ---: | --- |
| organization | 9 | Fab、Area、Line、位置、员工、工位、终端、班次、交接 |
| security | 13 | Identity 用户/角色/授权基础 7 表，操作权限、角色权限、直接授权、用户厂区范围、会话与安全事件 |
| engineering | 14 | 产品/版本、路线/版本/步骤、工序、Recipe/版本/适用关系、编号、输出定义、点检、文档、Q-time 规则 |
| production | 28 | 工单/快照/变更/停工、Lot、Wafer、归属、载具槽位与移动、加工/逐片参与/事件、拆合批、返工与共炉结构、首次投放/终点完成/步骤继承 |
| quality | 18 | 原因、异常及整改、Lot/物料 Hold、独立放行、来料检验、规格版本、量测、质检触发/任务、报废及逐片处置 |
| equipment | 19 | 设备分类/组/设备/腔体、事件/状态/停机、工具/保养、设备和人员资格、光罩/使用、材料上机 |
| materials | 15 | 公司、物料分类/主数据/来源/批次/分装、BOM、分配/拣选/消耗、仓库/库存/流水/单据 |
| integration | 9 | 外部系统、设备连接/消息/测点、API Key、导入映射/任务、标签、自定义字段定义 |
| audit | 5 | 审计、业务事件、附件、幂等请求回执、Outbox |
| workbench | 4 | 理想产速、生产计划块、订阅与回执 |
| settings | 2 | 站点外观、厂区排程默认视图 |

完整字段、类型、可空性与外键目标见 [数据库字段字典](05-database-catalog.md)。筛选依据仍是 [Fab 字段保留清单](04-fab-data-field-retention.md)，不是把 OpenMES 的 132 张表全部照搬。

## 2. 字段落地的主要调整

| 清单中的来源 / 对象 | 实际落地 |
| --- | --- |
| `batches` | `production.lots`；`lot_number` 是业务批号，原批号保留为 `legacy_batch_number`；完工量 `produced_qty` 是待业务命令维护的受控快照，不能累加每站通过量 |
| `serial_units` | `production.wafers`；归属独立放入 `lot_memberships`，不再保存另一个可自由修改的当前批次字段；F4 新片额外固定工单/厂区，历史批次仍按对应时点归属追溯 |
| Lot 当前片数 / 报废量 | `production.lot_quantities` 从有效归属、逐片报废事实计算；当前片数按有效归属计数，报废操作未来必须在同一事务关闭归属并写处置 |
| `batch_steps` / `unit_step_history` | `step_executions` + `process_runs` + `process_run_wafers` + `track_events`，保存步骤到访、加工、实际晶圆集合及操作责任人 |
| `process_templates` / `template_steps` | `routes` / `route_versions` / `route_steps`，同一工序的不同路线位置分别有身份 |
| `workstations` | 物理设备为 `equipment.equipment`，人工操作工位和登录终端为 `organization.workstations` / `terminals` |
| `released_at` 等混合语义 | 投放路线记录在 Lot；解除冻结记录在 Hold；生产/质量放行记录在 `quality.lot_releases` |
| 停机与报废原因 | 统一 `quality.reason_codes`，用 `category` 区分；原因类别与具体操作匹配由后续业务服务校验 |
| 单条物料冻结字段 | 保留来源字段以便参考映射；多条冻结以 `material_holds` 为准，旧摘要字段不能作为是否可用的唯一依据 |
| 库存 | `warehouse_stocks` 明确只能属于一种物料或产品；物料库存可定位物料批次，物料批次与物料主数据受复合外键约束 |
| 时长字段 | 设备状态时长由 `equipment.state_durations` 计算；其他起止时间可计算的时长不再独立录入 |
| Laravel 账号和运行时表 | 身份信息使用 ASP.NET Core Identity；会话、缓存、队列、框架迁移等不搬入新业务模型 |
| 集成协议与凭据 | 连接保留协议、配置 JSON 和 `secret_reference`；未建立 SECS/GEM 协议实现，不把设备密码/私钥写在普通配置里 |
| 公共创建人 | 原清单明确要求的业务责任人以用户外键保存；通用 `created_by_id` 只在需要它的对象设置，不用它代替事件操作者 |

组合键引用用于校验产品版本归属、Lot/步骤路线版本一致性；所有业务外键限制级联删除，保留历史引用。历史来源标识及具体外部数据迁移映射需在导入真实数据时再定义，本次没有导入 OpenMES 数据。

## 3. 已由数据库保护的规则

- Wafer 当前只能属于一个 Lot；包括历史在内的归属时间区间不允许重叠，允许首尾相接。
- Wafer 同时只能占一个载具槽位；同一槽位的装载区间不允许重叠；槽号不能超过载具容量。
- 首版同一设备、同一步骤执行只能存在一个 `Running` 加工；同一 Lot 只能有一个未退出的步骤执行。共炉结构已预留，但这条首版限制没有放开。
- Lot 当前步骤及步骤执行必须属于记录的 Route 版本；数量非负、关键时间先后关系、数据值互斥按各表 CHECK 约束执行。
- 多个 Hold 分别保留释放记录；解除其中一条不改动其他记录。
- TrackEvent、血缘事件及命令回执有幂等唯一键；库存唯一键对 NULL 批号同样去重。
- 业务实体的 EF 并发检查使用 PostgreSQL `xmin`，冲突抛出 `DbUpdateConcurrencyException`。
- 审计、业务事件、Track、血缘事件/逐片明细、量测、逐片处置、报废、库存流水、耗料、设备事件、过程确认和点检完成记录禁止 UPDATE/DELETE，需通过新事实更正。
- 已发布/废止的 Route、Recipe、产品和规格版本不能改内容；Route 步骤及其点检/输出定义也受发布保护。废止状态及适用终止时间允许受控变更，不能回到草稿。

EF 可表达的约束进入模型快照；时间区间排斥约束、触发器和视图放在 `FabIntegrityGuards` 迁移的嵌入 SQL 资源中。已应用的迁移及其资源不再改写，后续通过增量迁移升级。

## 4. 本地启动和连接

```sh
dotnet restore MESagent.sln --locked-mode
dotnet tool restore
python3 scripts/database.py init
python3 scripts/database.py up
python3 scripts/database.py migrate
python3 scripts/database.py status
```

`init` 只在 `.env` 不存在时生成随机密码，文件权限 600，Git 忽略该文件；不会覆盖已有配置。

| 连接项 | 值 |
| --- | --- |
| 主机 | `127.0.0.1` |
| 端口 | `55432`，可在首次启动前通过 `.env` 修改 |
| 数据库 | `mesagent` |
| 本地迁移账号 | `mesagent_owner` |
| 密码 | 项目根目录 `.env` 中的 `MES_DB_PASSWORD` |
| 容器 / 卷 | `mesagent-postgres-1` / `mesagent_postgres_data` |

PostgreSQL 18 卷挂载 `/var/lib/postgresql`。停止使用 `python3 scripts/database.py stop`，重新 `up` 保留已有数据。初始化密码变量只在空数据卷初始化时生效；修改 `.env` 不会自动修改已有数据库角色密码。

本地迁移角色是 PostgreSQL 初始化管理账号，仅用于当前开发环境；未来 API/Worker 需配置独立最小权限运行账号。用户权限及厂区范围表已建，但授权策略、数据过滤和 RLS 尚未实现。

可审阅完整 DDL 文件：`database/mesagent.sql`，由 EF Core 生成的幂等迁移脚本。使用脚本与使用 `migrate` 是同一套迁移历史，不要另行手工建同名表。

## 5. 继续开发与验证命令

```sh
dotnet build MESagent.sln --no-restore
python3 scripts/database.py test
python3 scripts/database.py catalog

# 修改模型后新增迁移（不要修改已经应用的迁移）
dotnet ef migrations add ChangeName --project src/MESagent.Infrastructure --startup-project src/MESagent.Database --output-dir Persistence/Migrations
python3 scripts/database.py migrate
dotnet ef migrations script --idempotent --project src/MESagent.Infrastructure --startup-project src/MESagent.Database --output database/mesagent.sql
```

测试使用随机命名的 `mesagent_test_*` 独立数据库，测试结束删除，仅需要本地测试管理连接。缺少连接信息时测试直接失败，不伪装成已通过，也不退回内存数据库。业务数据库不植入测试账号或演示晶圆。

当前验证记录见本节末尾；命令行迁移标识使用 UTC 时间，因此其日期前缀可能比上海日期早一天。

## 6. 已建结构与未实现业务的边界

本次已完成数据库与持久化工程，不包含 API、登录页面、WebUI、设备驱动、调度或 Agent。建表不意味着工厂可投产。

Track-in 权限、Hold 阻塞、设备/Recipe 资格匹配、发布状态校验、加工参与晶圆集合与 Lot 归属核对、拆合批守恒、报废时关闭归属、库存记账、自动写审计/Outbox 等跨对象规则，需后续应用服务以事务实现。唯一键只提供去重保护，完整的幂等响应重放仍需业务命令处理器。

光罩、Q-time、材料寿命、共炉、返工和资格表先保留结构；具体工艺枚举、状态机、精度及规则版本需要结合工厂样本细化。关联载具、设备与 Lot 的跨厂区一致性尚不作为完整多厂隔离承诺。现阶段仍按单厂试点建模。

不创建包装/财务业务表、SPC/FDC/APC 算法表、向量数据库或 Laravel 运行时表。身份认证已具备数据模型，未创建默认管理员和通用密码。数据保留期限及生产级备份、恢复和高可用策略仍需后续运维方案。

## 7. 验证记录

- 构建通过，0 警告、0 错误；依赖采用集中版本与锁文件。
- **17 项真实 PostgreSQL 测试全部通过**：全量建库与重复迁移、外键、负数量、当前及历史归属、槽位占用/容量、路线版本关联、多 Hold、设备占用、幂等、xmin 并发、事务回滚、发布保护、审计不可改、产品库存去重、派生数量及全量迁移降级/重建。
- 本地数据库已应用 3 条迁移，无待执行迁移；实际为 122 张表（含 1 张迁移历史表）、2 个视图、301 个外键、4 个时间区间排斥约束。
- EF 导出的 `database/mesagent.sql` 在独立空库连续执行两次成功，结构统计与 EF 迁移建库一致。
- 对当前空业务库执行 `pg_dump -Fc` 并恢复到独立临时库成功，核对表/视图/外键/迁移/排斥约束数量一致。这是结构恢复验证，尚不是带生产数据和附件的恢复演练。
- Docker 容器重启后数据库和迁移记录仍在，持久卷生效；测试库和恢复验证库均已清理。

## 8. F4 增量迁移（2026-11-12）

第 6 条迁移 `20261112033140_LotWaferCarrierGenealogy` 已应用；新增三个不可变生产事实表及 Wafer 稳定归属、载具尺寸、Lot 关闭语义，并保护历史区间只关闭一次。迁移前本地 Lot/Wafer/Carrier 均为空，备份已保存于 Git 忽略的本地目录；未向业务库写测试生产数据。

`production.lot_quantities` 仍为当前成员/处置时 Lot 报废数，不是 WIP 或工单完工量。F4 的内部完工/报废收尾能力已具备，F5/F6 真正执行入口及工单进度仍待联调。历史未实现描述保留为初始建库边界，当前证据以 [F4 实现记录](../feature-development/lot-wafer-carrier-genealogy-implementation.md) 为准。

已验证 17 项数据库测试（含全迁移回滚/重建）和 39 项 API/协作测试；最小权限角色可执行建批、投放与装载，无生产 DDL/血缘删除权限。`scripts/app.py serve` 使用 `mesagent_app`，迁移/初始化使用所有者；DDL 与字典已重新导出。
