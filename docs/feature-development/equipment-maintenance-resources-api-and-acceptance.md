# F7-`EquipmentMaintenanceResources` 接口与验收

更新：2026-11-17｜状态：接口实现基线与本地验收场景；执行结果见实现记录。

## 1. 通用契约

前缀 `/api/v1/equipment`，认证、防伪、ProblemDetails、部署厂区授权沿用既有管线。所有写操作带 `Idempotency-Key`；修改/状态转换/装卸还带目标对象 `If-Match`。读取按权限及厂区过滤后分页，默认20、最大100，返回 `items/total/page/pageSize`。列表支持 q、适用时 equipmentId/status/needsReason；逐项版本由 etag 返回。所有时间为 UTC，前端显示厂区时区。

幂等哈希按属性排序并规范类型化 ID 字段，业务编码和来源编号保留原文。必填控制字段缺失返回400。同键同参返回原业务结果，同键异参/版本409；缺版本428、旧版本412；范围外对象404，缺权限403。任何失败回滚业务、资源计数、审计及回执。设备工艺适用关系与现有 F5 请求路径保留。

## 2. 台账与事件

| 方法/路径 | 权限 | 请求/结果 |
| --- | --- | --- |
| GET `/types,/groups,/tools,/chambers` | read | 台账分页；tools 为既有物理设备名称 |
| POST `/types,/groups` | manage | code、name |
| PUT `/types/{id},/groups/{id}` | manage | name、description、isActive；编码不可改 |
| POST `/tools` | manage | code、name、areaId、equipmentGroupId、equipmentTypeId、lineId? |
| PUT `/tools/{id}` | manage | name、isActive，可补完整位置参数；保留 F5 旧请求兼容 |
| GET `/tools/{id}` | read | equipment、currentRun/Lot、equipmentAvailable 基础可用性、states、capabilities、maintenance、装载资源；具体生产启动仍以 F5 为准 |
| POST `/chambers`；PUT `/chambers/{id}` | manage | 设备与代码/名称；修改名称/说明/启用 |
| POST `/tools/{id}/state` | state | state、reason、reasonCodeId?、shiftId? |
| POST `/events/{equipmentId}` | simulate | source、sourceEventId、occurredAt、kind=State/Connection、state、reason、可选分类；目标设备版本 |
| GET `/events,/states` | read | 厂区过滤的事件/状态分页，事件显示 Applied/Rejected |
| GET `/options` | read | 本厂及实例共享的允许引用项；仅返回简要身份字段 |

模拟事件发生时间用于判序，应用时间采用接收时刻。来源去重在设备/来源范围内；重复来源事件可跨 HTTP 幂等键重放，异内容拒绝。事件不能创建/完成 Run。

## 3. 停机、维护与资格

| 方法/路径 | 权限 | 请求/结果 |
| --- | --- | --- |
| GET `/downtimes` | read | 未分类及设备筛选，停机记录与状态区间绑定 |
| POST `/downtimes` | downtime | equipmentStateId、原因/班次/说明；目标设备版本，限未登记 Down 区间 |
| POST `/downtimes/{id}/classify` | downtime | reasonCodeId、notes、shiftId?；停机版本 |
| GET/POST `/reasons`；PUT `/reasons/{id}` | read/downtime | 仅 Downtime 类别原因 |
| GET/POST `/shifts` | read/downtime | 本厂班次；创建 code/name/startTime/endTime |
| GET `/statistics?from=&until=&equipmentId=` | read | 窗口裁剪后的状态、停机原因/班次秒数、asOf 与口径说明 |
| GET/POST `/resources`；PUT `/resources/{id}` | read/maintenance | 工具台账，创建时可绑定设备及类型，后续修改状态/启用/名称，可在无运行加工及未关闭维护时更换或解除设备绑定 |
| GET/POST `/maintenance-plans` | read/maintenance | 名称、类型、频率Hours/Days、周期、到期时间、设备或工具、负责人 |
| POST `/maintenance-plans/{id}/defer` | maintenance | until、reason，计划版本 |
| POST `/maintenance-plans/{id}/toggle` | maintenance | 启用/停用、原因，计划版本 |
| GET/POST `/maintenance` | read/maintenance | 维护事件：标题、类型、计划时间、目标/计划、负责人 |
| POST `/maintenance/{id}/start,/complete,/release,/cancel` | maintenance；release 独立 | reason，工单版本；执行状态前置校验 |
| POST `/maintenance/{id}/defer` | maintenance | until、reason；限 Planned |
| GET/POST `/equipment-qualifications` | read/qualify | equipmentId、operationId、recipeVersionId?、validFrom、validTo? |
| GET/POST `/worker-qualifications` | read/qualify | workerId、operationId、recipeVersionId?、validFrom、validTo? |
| POST `/equipment-qualifications/{id}/revoke,/worker-qualifications/{id}/revoke` | qualify | reason，资格版本；复认证新建记录 |
| GET/POST `/policies`；PUT `/policies/{id}` | read/qualify | operationId、设备/人员资格开关（两者必须相等）、requiredReticleLayer?、requiredMaterialIds；策略版本 |

资格专项和通用记录的优先级、维护是否到期即阻塞，以确认后的功能设计为准。策略是运行准入规则，不覆盖发布路线或已执行事实。

## 4. 光罩与耗材

| 方法/路径 | 权限 | 请求/结果 |
| --- | --- | --- |
| GET/POST `/reticles`；PUT `/reticles/{id}` | read/reticles | 创建code/name/revision/layerCode/maxUsageCount/usableUntil?；修改名称/状态/启用 |
| POST `/reticle-mounts/{equipmentId}` | reticles | reticleId；设备版本；光罩不可跨设备重复装载 |
| POST `/reticle-mounts/{id}/unload` | reticles | reason；装载记录版本，设备无加工 |
| GET `/reticle-mounts` | read | 历史及当前装载 |
| GET `/reticles/{id}/impact` | read | 分页实际 Run、Lot、参与 Wafer 与结果 |
| GET/POST `/consumables` | read/materials | 最小耗材物料定义code/name/unit |
| GET/POST `/consumable-lots` | read/materials | materialId、lotNumber、expiryAt?；本厂登记来源 |
| POST `/material-mounts/{equipmentId}` | materials | materialLotId、openedAt、usableUntil；设备版本 |
| POST `/material-mounts/{id}/unload` | materials | reason；上机版本，设备无加工 |
| GET `/material-mounts` | read | 上机/卸下区间与有效截止 |
| GET `/material-mounts/{id}/impact` | read | 分页实际 Run/Lot/Wafer 关联，coverage 明示事实范围 |

## 5. 验收场景

| 编号 | 场景及预期 |
| --- | --- |
| E01 | 设备类型/组创建与停用、重复编码冲突；旧版本修改412、缺版本428 |
| E02 | 跨厂区域/组、错产线、停用类型拒绝；对象查询无跨厂泄漏 |
| E03 | 设备台账新增/修改、腔体台账；已有加工的类型/归属不可覆盖 |
| E04 | 设备详情返回真实能力和当前 Lot；加工中禁止停用/搬迁 |
| E05 | Track-in→Running 区间，Track-out/中止→Idle；历史区间无重叠 |
| E06 | 加工中 Down 保留 Run，占用不释放；恢复 Idle 仍显示 Running；按 F5 收尾 |
| E07 | 模拟 Connection 事件与运行状态分离；通信 Online 不自动允许加工 |
| E08 | 来源编号同参重放、异参409；HTTP 重试仅一份事件、停机与审计 |
| E09 | 乱序、同点、无效状态留 Rejected；当前条件与区间不变；未来时刻拒绝 |
| E10 | Down 自动建停机、退出同步关闭；无原因进未分类，补分类保留审计 |
| E11 | 历史 Down 补登记唯一；其他状态或重复区间拒绝 |
| E12 | 窗口裁剪、开放区间、原因/班次统计与 Down 状态秒数一致，不重复计算 |
| E13 | 工具台账绑定与范围；维护计划目标恰选一，计划与工单目标不一致拒绝 |
| E14 | 计划/工单到期影响派工；未来工单不提前阻塞；延期原因及版本必填 |
| E15 | 加工中不能开始维护；开始→完成仍阻塞→独立复机；原 Down 不被自动修改 |
| E16 | 复机推进周期且幂等不重复；取消未开始工单不取消已到期计划门禁 |
| E17 | 设备/人员资格缺失、生效、到期、撤销及复认证；专项资格优先 |
| E18 | Worker 未绑定、停用、跨厂拒绝；有权限但无工艺资格仍不能启动 |
| E19 | 按工序配置门禁；指派/候选/Track-in 同口径；并发修改后启动重新校验 |
| E20 | 光罩版本层次唯一身份、装卸互斥、加工中卸载拒绝 |
| E21 | 光罩层次不符、寿命/有效期/停用拒绝启动；成功仅消费一次，中止不返还 |
| E22 | 光罩影响查询保留拆合批前实际参与 Wafer、原 Run、Recipe；分页及范围有效 |
| E23 | 耗材最小登记本厂可用；冻结/过期/单位异常/跨厂上机拒绝 |
| E24 | 开封与截止时间顺序、批次有效期上界、同物料重复上机互斥 |
| E25 | 必需耗材缺失/过期阻止；成功记录 Run 关联，卸下后仍可查询实际参与片 |
| E26 | 并发 Track-in/故障/维护/卸载只能产生符合最新状态的一次结果，无半成功 |
| E27 | 各细分权限、撤权、无厂区、范围外ID、幂等回执均不能绕过服务端授权 |
| E28 | 最小运行角色无 DDL/DELETE，能够完成所有 F7 正常流程 |
| E29 | 数据迁移不改账号、生产事实或既有演示数据，空库与升级/回滚检查通过 |
| E30 | WebUI 完成台账→模拟故障→停机归因→维护复机→资格/资源→启动→影响查询；加载/空/错误及版本冲突清晰 |

以上是验收要求，实际执行范围和证据必须在实现记录逐项登记。
