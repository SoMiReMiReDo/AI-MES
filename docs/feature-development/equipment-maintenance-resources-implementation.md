# F7-`EquipmentMaintenanceResources` 实现与本地验收

更新：2026-11-17（Asia/Shanghai）｜状态：F7-01～07 已实现并通过本地验收。

## 1. 交付内容与确认

用户确认 F7-01～07 的文档、后端、WebUI 和测试，以及以下规则：

- 资格按工序配置，默认关闭；启用时设备与当前操作员人员资格必须同时有效。
- 光罩按启动 Run 次数计寿命；中止不返还，重放不重复计数。耗材按开封/上机截止校验，库存和精确耗用留给 F8。
- 支持小时/天维护计划，到期阻止新加工；完成后由独立权限确认复机，延期须说明并留审计。
- 加工中设备 Down 保留当前 Run，占用不释放；F5 受控收尾，F6 处置后继续或重试。

[功能设计](equipment-maintenance-resources.md)与[接口及验收](equipment-maintenance-resources-api-and-acceptance.md)是本轮基线。跨模块设计更新到[架构第14节](../development-process/08-feature-implementation-architecture.md#14-f7-设备与生产资源架构)。

## 2. 代码组织与协作

| 位置 | 本轮实现 |
| --- | --- |
| `src/MESagent.Application/EquipmentMaintenanceResources.cs` | 台账、模拟事件、停机、维护、资格/门禁、资源装卸类型化请求与模块接口 |
| `src/MESagent.Domain/EquipmentRules.cs`、`Entities/Equipment*.cs` | 资格/寿命边界与资源事实模型 |
| `src/MESagent.Infrastructure/EquipmentMaintenanceResources/` | 独立 EquipmentBackend；Registry/Events/Maintenance/Qualifications/Consumables/Queries；共享 ResourceGates 与 EquipmentStates |
| `src/MESagent.Infrastructure/DispatchAndProductionExecution/` | 候选、指派、Track-in 共用资源门禁；Run 实际使用与有效设备区间；异常设备收尾进入等待处置 |
| `src/MESagent.Infrastructure/Persistence/FabModel.EquipmentResources.cs` | 4张新增表及原表增量映射、互斥/唯一关系 |
| `Persistence/Sql/EquipmentResources.*.sql` | 增量权限、历史兼容、状态/装卸关闭保护、资格撤销与维护转换保护 |
| `src/MESagent.Api/Program.cs` | 公开类型化端点与 OpenAPI；保留旧设备请求兼容 |
| `web/src/equipment.tsx` | 七组设备与资源工作台；原 execution.tsx 中的设备台账提取到此 |
| `tests/MESagent.Api.Tests/EquipmentTests.cs` | 共享既有 ExecutionTests 的生产样本，验证设备与执行跨模块规则 |
| `database/equipment-runtime-grants.sql`、`scripts/app.py` | 可重复执行的增量最小运行授权；不改密码 |

沿用单 MesDbContext、认证应用入口、事务内全局写锁和只读一致性快照。设备模块不启动独立事务；状态、停机、资源计次/关联、Run、审计、业务事件与回执共同提交。故障、维护、资格及资源不通过 UI 按钮绕过服务端检查。

物理设备保持 `/equipment/tools` 旧路径；实际工具为 `/equipment/resources`。旧 F5 的设备名称/启用修改载荷仍可用，新增完整设备位置编辑仅限无工艺/加工关联的设备。工具绑定支持在无运行加工和未关闭维护时调整；类型必须兼容。腔体只提供台账，不启用腔体调度或共炉。

## 3. 数据库与运行权限

新增第9条迁移 `20261117095908_EquipmentMaintenanceResources`，应用表数126→130（123业务/支撑 + 7 Identity），派生视图仍为2个。新表为：

- `equipment.operation_policies`
- `equipment.material_requirements`
- `equipment.reticle_mounts`
- `equipment.material_run_usages`

设备新增独立通信状态，事件新增来源编号/处理结果，停机绑定唯一 Down 区间，维护保存独立复机，资源增加明确厂区、光罩寿命和耗材精确截止/实际物料身份。光罩/工具来源不明的旧记录保留空厂区，不推断归属；已有资源关联不回填虚构 Run。

迁移从启用时点校准当前 Running 设备的有效状态；更早区间沿用原人工状态，不据此宣称历史真实运行时长已经补齐。Downtime 的时间来自状态区间，补登记只允许尚未绑定的 Down 区间，统计窗口裁剪后汇总，不重复加总停机时间。

本地迁移前已备份到 Git 忽略目录，备份权限600。升级后已核对账号、工单、Lot、Wafer、Run、成员、载具、首次投放和终点完成数量，均与备份前一致。新增权限只补充 SystemAdmin，其他角色由管理员授权；未改账号密码。运行角色无新增 DDL 或生产/资源历史 DELETE；仅可编辑的工序耗材要求关系有 DELETE 权限。

```bash
python3 scripts/database.py migrate
python3 scripts/app.py provision-equipment
python3 scripts/app.py serve
```

首个命令使用既有所有者连接，运行 API 使用 `mesagent_app`；不要为升级重复 bootstrap 或重置管理员。字段字典、幂等 DDL 与生成的 OpenAPI 类型随本轮同步。

## 4. 验证证据

| 验证 | 结果与覆盖 |
| --- | --- |
| 后端构建 | 0警告、0错误；未升级依赖 |
| 数据库测试 | 17项通过；含空库迁移、重复迁移、约束与全迁移回滚/重建 |
| API/协作 | 原70项 + 本轮18项，合计88项，全部通过 |
| 关键业务 | Running/Down/恢复与占用、停机归因及统计一致、模拟事件去重/乱序与通信独立、维护到期/完成/复机、资格缺失/撤销/复认证、专项资格过期、资源寿命与参与片影响 |
| 边界与可靠性 | 缺/旧版本、必填控制字段、文本编码幂等哈希、跨厂/权限拒绝、耗材有效期/Hold、光罩中止计次与幂等、实际运行角色无DDL/历史DELETE；启动与光罩卸下并发专项通过 |
| Web | TypeScript/Vite 构建通过，保留已有大包提示；Prettier 按本项目4空格规范检查 |
| Chrome | F1～F7 真实流程、历史停机补登记及响应丢失后的原键重试通过；稳定截图已复核 |
| DDL | 导出脚本在隔离空库连续执行两次，130张表一致；模型与迁移快照无差异 |

浏览器测试由 `python3 scripts/e2e.py` 自动构建后运行，在独立数据库与5081/5174测试端口执行并清理；不向本地演示库写测试生产数据。流程还注入耗材上机响应丢失，确认同表单重试沿用原设备版本与幂等键、只生成一条记录；另用明确历史来源的测试样本完成 Down 区间页面补登记。流程包括 F1 身份权限、F2/F3/F4 生产基础、F5 五片三站、F6 质量，以及本轮停机归因/模拟通信/维护复机/资格/光罩/耗材→真实启动→受控中止→参与晶圆影响查询。

稳定截图在 Git 忽略的 `web/test-results/equipment-downtime.png` 与 `equipment-resource-impact.png`。完整场景定义见接口文档 E01～E30；自动测试是代表性场景证据，不等于实机协议、现场工艺或压力验收。

## 5. 已知边界

- 仍为单厂试点、单设备一次一个 Lot；腔体台账不代表腔体调度/多 Lot 共炉。
- 模拟事件始终标记为模拟；通信在线不等于可加工，不连接真实设备或下发指令。
- 资源影响只覆盖实际记录的 Run 关联；启用前没有关联的历史不推断完整覆盖。
- 停机班次由人工指定，未指定独立归类；不推断自动排班和跨班拆时，不计算 OEE。
- 耗材登记不建立采购/库存账本，不扣库存或伪造单片精确耗用；后续由 F8 扩展。
- 独立复机使用独立权限，不强制与完成人为不同账号；不自动恢复 Down 或发放工艺资格。
- Docker、实机联接、自动质检、跨路线返工与完整 F9/F10 继续后置。
