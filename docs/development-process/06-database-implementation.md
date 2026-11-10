# Fab 数据库实施说明

日期：2026-11-11（Asia/Shanghai）。用户已明确启动数据库开发；本次交付数据库结构、迁移与验证，不代表 MES 业务接口、WebUI 或 Agent 已实现。

## 1. 已建立的结构

技术组合：.NET SDK **10.0.401**、EF Core / Identity **10.0.12**、Npgsql EF Provider **10.0.3**、PostgreSQL **18.6**。NuGet 精确版本在根目录 `Directory.Packages.props` 管理，各项目提交 `packages.lock.json`；本地 `dotnet-ef` 固定为 10.0.12。数据库镜像在 `compose.yaml` 固定版本及 SHA256 摘要。

共 **121 张应用表**：114 张业务表 + 7 张 Identity 表；另有 EF 迁移历史表和 2 个派生视图。

| schema | 表数 | 主要对象 |
| --- | ---: | --- |
| organization | 9 | Fab、Area、Line、位置、员工、工位、终端、班次、交接 |
| security | 11 | Identity 用户/角色/授权基础 7 表，操作权限、角色权限、直接授权、用户厂区范围 |
| engineering | 14 | 产品/版本、路线/版本/步骤、工序、Recipe/版本/适用关系、编号、输出定义、点检、文档、Q-time 规则 |
| production | 25 | 工单/快照/变更/停工、Lot、Wafer、归属、载具槽位与移动、加工/逐片参与/事件、拆合批、返工与共炉结构 |
| quality | 18 | 原因、异常及整改、Lot/物料 Hold、独立放行、来料检验、规格版本、量测、质检触发/任务、报废及逐片处置 |
| equipment | 15 | 设备分类/组/设备/腔体、事件/状态/停机、工具/保养、设备和人员资格、光罩/使用、材料上机 |
| materials | 15 | 公司、物料分类/主数据/来源/批次/分装、BOM、分配/拣选/消耗、仓库/库存/流水/单据 |
| integration | 9 | 外部系统、设备连接/消息/测点、API Key、导入映射/任务、标签、自定义字段定义 |
| audit | 5 | 审计、业务事件、附件、幂等请求回执、Outbox |

完整字段、类型、可空性与外键目标见 [数据库字段字典](05-database-catalog.md)。筛选依据仍是 [Fab 字段保留清单](04-fab-data-field-retention.md)，不是把 OpenMES 的 132 张表全部照搬。

## 2. 字段落地的主要调整

| 清单中的来源 / 对象 | 实际落地 |
| --- | --- |
| `batches` | `production.lots`；`lot_number` 是业务批号，原批号保留为 `legacy_batch_number`；完工量 `produced_qty` 是待业务命令维护的受控快照，不能累加每站通过量 |
| `serial_units` | `production.wafers`；归属独立放入 `lot_memberships`，不再保存另一个可自由修改的当前批次字段；工单由对应时点的 Lot 追溯 |
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
