# F1～F12 演示数据与展示任务清单

2026-11-18 已将 `DEMO-FAB-` 数据直接写入本地开发库 `mesagent`。打开 [本地 MES 工作台](http://127.0.0.1:5173/)，使用现有管理员查看完整菜单、站内通知和本人导入任务。示例沿用原有演示组织、三站工艺和 PDF，新增 10 张工单、9 个批次、32 片晶圆和 11 次 Run。32 片已全部投放，其中 15 片完工、16 片在制（含等待处置及冻结）、1 片报废。

## 功能覆盖清单

| 状态 | 模块与入口 | 样本和验收结果 |
| --- | --- | --- |
| [x] | F1 组织结构、员工、工位、终端、账号与访问、角色与权限 | 复用 `DEMO-AREA`、`DEMO-WS-01`、`DEMO-TERM-01` 和四个 `demo.*` 账号。核对人员与终端关联、启用/停用、首登改密和角色权限。 |
| [x] | F2 工艺管理 | `DEMO-P300`、`DEMO-ROUTE-300`、各站 Recipe、发布/草稿/废止版本、编号、点检、类型化输出和指导书 PDF。新增 `DEMO-FAB-CELL` 三站配方适用关系。 |
| [x] | F3 工单管理 → 执行概况 | 打开 `DEMO-FAB-WO-DONE`，应见计划 5、投放 5、完工 5。打开 `DEMO-FAB-WO-SCRAP`，应见投放 4、在制 3、报废 1。原样本覆盖取消和关闭，`DEMO-FAB-WO-CSV` 是导入草稿。 |
| [x] | F4 生产管理 → 批次、晶圆、载具 | `DEMO-LOT-MERGED` 保留拆合批血缘。新载具 `DEMO-FAB-FOUP` 的 1～4 槽装有待排程片，6～10 槽有已完工片装卸履历。原载具保留换槽和移位样本。 |
| [x] | F5 生产执行 → 待加工派工、加工与历史 | `DEMO-FAB-QUEUE` 待派工，`DEMO-LOT-RUNNING` 保留加工中，`DEMO-FAB-DONE` 显示三站完成、实际 Recipe、逐片结果及作业记录。对照 `DEMO-FAB-FAIL` 混合通过/失败和 `DEMO-LOT-ABORT` 受控中止。 |
| [x] | F6 质量管理 → 五个页签 | `DEMO-FAB-HOLD` 膜厚 108.5 nm 超过 105 nm 上限，可下钻未关闭异常、整改和有效 Hold。`DEMO-FAB-QUEUE` 抽检通过，`DEMO-FAB-RETRY` 保留关闭异常、解除 Hold、质量放行及同站第二次到访。`DEMO-FAB-SCRAP-W01` 报废后其余 3 片保留。 |
| [x] | F7 设备与资源 → 台账、状态与事件、停机与统计、工具与维护、资格与门禁、光罩、耗材 | `DEMO-FAB-CELL` 空闲、有已归因历史停机、保养复机和资格记录。`DEMO-FAB-SIM` 停机待归因并有到期维护。`DEMO-FAB-R01` 和 `DEMO-FAB-FILTER-01` 可下钻关联 Run，`DEMO-FAB-R02` 展示寿命耗尽。门禁配置见 `DEMO-FAB-QUAL`，该工序供配置展示。 |
| [x] | F8 硅片来料 | `DEMO-FAB-IN-01` 关联 32 片，`DEMO-FAB-IN-02` 当前未绑定晶圆，两批均可作为建批来源。打开 `DEMO-FAB-DONE-W01` 可反查供应商批号。 |
| [x] | F9 生产追溯 | 分别按工单 `DEMO-FAB-WO-DONE`、Lot `DEMO-FAB-DONE`、Wafer `DEMO-FAB-DONE-W01`、载具 `DEMO-FAB-FOUP`、设备 `DEMO-FAB-CELL` 检索并打开履历。用 `DEMO-LOT-MERGED` 展示拆合批上下游，用 `DEMO-FAB-HOLD` 查看质量原始记录。最近查询与查看随浏览操作生成。 |
| [x] | F10 工作台 | 总览可下钻待加工、在制、运行、Hold、停机和完工。历史趋势选 **11 月 15～18 日**，看三个日期的完工、等待/周转、失败、检验和报废。班次与 OEE 选 `DEMO-FAB-CELL` 及同一日期范围。排程切到 **11 月 19 日**，查看 `DEMO-FAB-PLAN` 的 09:00 计划、变更记录、已取消样本及待排队列。待办与告警查看停机已确认、冻结待处理和异常静音。 |
| [x] | F11 数据导入与集成 | `DEMO-FAB-SIM-CONN` 下有上线、停机和乱序报文，可对照已应用/已拒绝并下钻设备事件。工单导入用 `demo-fab-ready.csv`、`demo-fab-rejected.csv`、`demo-fab-imported.csv` 展示预检、行错误和草稿链接。 |
| [x] | F12 审计与安全 | 业务审计按来源渠道 `DatabaseSeed` 筛选，可查看模拟报文、质量、导入和排程快照。事件投递有已投递、等待重试和死信样本，死信提供重放入口。 |
| [x] | F14 交付展示 | F14 的空环境初始化、重建和备份恢复使用独立 Docker 演示实例和原验收流程。[Docker 交付验收](../feature-development/docker-delivery-acceptance.md) |

`[x]` 表示展示样本已就绪。F13 Agent、实机通信、库存账本等后续范围见 [Fab 功能清单](07-fab-function-catalog.md)。

## 推荐演示顺序

1. 先在工作台看总览，切到上述历史和排程日期，再打开 `DEMO-FAB-WO-DONE` 核对 5 片完工。
2. 从 `DEMO-FAB-IN-01` 下钻 `DEMO-FAB-DONE-W01`，查看载具装卸、三站加工、光罩与耗材影响及工艺 PDF。
3. 用 `DEMO-FAB-HOLD`、`DEMO-FAB-RETRY`、`DEMO-FAB-SCRAP` 连续展示异常、放行和报废，再用模拟报文、CSV 和 Outbox 展示集成及可靠性。

## 数据脚本与复核

[直写 SQL](../../database/demo/fab-showcase.sql) 依赖已装载的 [F1～F5 演示基线](09-f1-f5-demo-data.md)。在项目根目录用以下命令执行，脚本校验三站基线，在单个事务内写入，已装载时自动跳过。加 `-v demo_rollback=1` 可对尚未装载的实例做回滚演练。日期按首次装载时的厂区时区计算，历史、排程和投递状态随后按实际日期演进。

```sh
docker exec -i mesagent-postgres-1 psql -X -U mesagent_owner -d mesagent < database/demo/fab-showcase.sql
```

数据库实查确认原 8 张工单及核心完工数 5、6、3 保持一致。使用 API 运行角色在只读事务中完成 40 项后端查询核验，并比对工艺 PDF 摘要。浏览器管理员会话过期，逐页渲染待复核。

F11 导入任务和 F10 通知按管理员账号可见。开工、质量解除、确认导入、重放等操作会改变相应样本的初始状态。
