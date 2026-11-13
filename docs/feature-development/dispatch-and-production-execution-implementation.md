# F5 实现与本地验收

日期：2026-11-13｜范围：F5-01～05、最小设备前置、F2 执行绑定及 F3 真实进度回补。依据：[功能设计](dispatch-and-production-execution.md) · [API 与验收](dispatch-and-production-execution-api-and-acceptance.md)。

## 1. 实现结果

沿用单体、既有 Cookie/CSRF/会话/厂区授权、单 Context 和事务内全局写锁。新增独立 ExecutionBackend；F4 提供等待步骤与终点收尾，F3 进度从唯一片事实读取。普通只读请求使用 RepeatableRead，避免进度中多个查询跨越不同提交；写命令继续在 ReadCommitted 事务中先取全局锁再加载当前授权。

| 代码 | 已实现职责 |
| --- | --- |
| `src/MESagent.Application/DispatchAndProductionExecution.cs` | 类型化指派、Track-in/out、作业记录及设备请求；进度响应 |
| `src/MESagent.Domain/ExecutionRules.cs` | 完整唯一片集、逐片结果、既有工程数据类型/精度/枚举校验 |
| `src/MESagent.Infrastructure/DispatchAndProductionExecution/` | 权限、规范化幂等、门禁、设备/配方候选、Run/参与片/Track、作业记录、设备台账与人工状态历史、执行下载 |
| `src/MESagent.Infrastructure/LotWaferCarrierGenealogy/Lifecycle.cs` | 等待步骤共享入口；终点真实 Run 驱动唯一完成、Lot/Wafer 与工单缓存同步 |
| `src/MESagent.Infrastructure/LotWaferCarrierGenealogy/ProductionProgress.cs` | 稳定归属/首次投放/完成事实统计、已知质量阻塞去重及缺失历史提示 |
| `src/MESagent.Infrastructure/ProductProcessVersionManagement/Planning.cs` | 真实进度、缓存重算、生产关联终结后关闭工单 |
| `web/src/execution.tsx` | 派工筛选/指派、加工详情、类型化作业记录、逐片结果、中止、设备台账与状态履历 |
| `web/src/manufacturing.tsx` | 工单真实投放/在制/阻塞/完工/报废指标，重算入口 |

实际请求类型为工程端既有 `string/decimal/integer/boolean/datetime/enum`，单位必须与定义一致。每项输出/点检只能新增一次，已录值无普通覆盖入口；错误更正后续按 GAP-11 设计。绑定 PDF 取下达快照与已保存 StepDocument，执行权限可以下载本批历史文档，不需要工程管理权限。

加工中 Hold/停工/质量阻塞可保存作业事实并受控结束 Run，Lot 留在 AwaitingDisposition；失败不计 ScrapQty 或终点产出。中止独立授权、原因必填。没有公开“改回 Ready”或人工完工捷径。完整 F6/F7、Q-time、返工及共炉仍按 GAP-12～14 保留。

## 2. 数据库与本地操作

第 7 条迁移 `20261113040741_DispatchAndProductionExecution` 已应用本地数据库：步骤指派设备/Recipe、Run 匹配适用关系、单步骤输出唯一键、Run/参与片及输出历史保护、执行与设备权限。仍为 119 张业务/支撑表 + 7 张 Identity 表，未增加表数。

迁移前 Lot/Run/OutputValue 数量均为 0；备份在 Git 忽略目录 `.local/backups/pre-f5-20261113.dump`。迁移不补造旧 Run 的适用关系。若已有重复步骤输出，唯一索引迁移会失败，需先核验，不自动删除或猜测历史。备份恢复演练仍随 Docker 后置。

运行角色使用新增的增量命令：

```sh
python3 scripts/database.py migrate
python3 scripts/app.py provision-execution
python3 scripts/app.py serve
```

`provision-execution` 要求既有非超级用户 `mesagent_app`，只补 F5 执行/最小设备表的 INSERT/UPDATE；不创建/重置账号、不改密码、不授 DDL/DELETE。完整初始化仍保留旧 provision，新模块升级使用增量入口。现有管理员通过迁移获得新增权限，其余角色需管理员显式分配。没有修改管理员凭据，也没有在本地业务库写入演示 Lot。

字段字典、`database/mesagent.sql` 和 `web/src/generated/api.d.ts` 已同步。

## 3. 验证证据

- 后端构建：0 警告、0 错误。
- 数据库 17 项 + API/协作 57 项通过，共 74 项；新增 ExecutionTests 18 项（含 3 个类型化理论用例）。全部使用自动创建/清理的隔离数据库。
- 前端生产构建通过；保留既有包体积提示，不作为性能验收。
- Chrome 全流程通过：F1～F4 回归、设备台账/人工状态、派工指派、5 片 3 站、作业记录和工单真实完工量；单次完整浏览器用例约 54 秒。已查看作业记录与进度截图，未见遮挡或字段溢出问题。

| 验收条件 | 实际覆盖与边界 |
| --- | --- |
| E01/E15 | 真实 HTTP 5 片 3 站完工；首站完成后拆 2+3，两批竞争同设备、各过中间站、末站前合回，仍只计 5 条完成 |
| E03/E05/E07 | 非可用设备、错误配方、缺版本、设备状态改变后的旧版本及双批竞争；未逐项自动穷举所有停用父对象组合 |
| E06/E14 | 同键并发重放仅一个 Run、异参冲突、撤销 Fab 后重放拒绝、跨厂对象不可见；原 F4 撤权/规范化用例同时回归 |
| F2 执行绑定 | Track-in 固定适用关系 ID、当时有效区间与版本；后续适用关系终止/Recipe 废止仍能正常收尾，历史 Track 中的依据保留 |
| E08/E09 | 缺必填阻止 Track-out；点检/指导书/关键确认/decimal 输出与单位校验，错误载荷整体回滚；绑定 PDF 下载及未知文档 404 |
| E10/E11/E12/E13 | 非法片集回滚、Failed 不报废/不推进、中止原因与权限、加工中 Hold 收尾、两条 Hold 独立阻塞、活动 Q-time 明确拒绝；Hold 由测试夹具写入，不代表 F6 UI 已实现 |
| E16/E17 | 去重实际进度、完成移出在制、损坏缓存从事实重算、在制工单拒绝关闭、全部完成后关闭；关闭与启动的专门并发用例尚未单独执行，代码共用全局写锁 |
| E18 | 全迁移降级/重建、终结 Run 历史保护、临时限权角色真实终点过站，无 CREATE/历史 DELETE 权限 |
| E02/E19 | Chrome 实际完成台账、人工状态、派工/指派、作业记录、逐片过站与工单进度，并回归 F1～F4；筛选全部组合和所有上限边界不标为逐项通过 |

日志保存在 Git 忽略的 `.local/f5-tests.log`、`.local/f5-e2e.log`。本地操作与隔离测试分开，测试成功不表示真实 Fab 投产、F6 处置或 Docker 已验收。

截图在 Git 忽略的 `web/test-results/execution-work.png`、`execution-completed.png`、`execution-progress.png`。验证命令为后端单进程构建、带本地测试环境的 `dotnet test MESagent.sln --no-build --no-restore -m:1`、`npm --prefix web run build`、Web Prettier 检查及 `python3 scripts/e2e.py`。

## 4. 后续

GAP-01 的执行事实统计与缓存回补已落地，完整质量口径仍依赖 F6；GAP-06 的实际 Recipe、点检/输出/指导书执行绑定已落地。GAP-08/09 已接通真实 F5 终点，报废后的联调和复杂存量核验仍待后续。完整设备/资源能力按用户要求登记于 GAP-12，异常恢复与按需执行能力分别见 GAP-13/14。
