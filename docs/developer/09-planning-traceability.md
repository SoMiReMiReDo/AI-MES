# 排程、绩效与追溯旁路

这些子系统围绕生产主链提供计划、解释和追查能力；应按业务问题选择入口。

## 使用逻辑

### 计划、绩效和追溯的三个现场问题

| 谁在问 | 问题 | 查看路径 |
| --- | --- | --- |
| 计划员 | 同一条线明早能否安排两张单 | 查看分钟排程的开始和结束，再结合班次能力、占用与物料提示 |
| 班组长 | 昨天某班产量偏低，是停机多还是速度慢 | 对照同一产线、日期及班次的停机、产量、报废和节拍 |
| 质量人员 | 供应商反馈某个物料批有问题，哪些产出受影响 | 以物料批号做正向追溯，再用生产批号或成品标识逆查投入 |

例如计划员把开工时间从 08:00 拖到 10:00，应分别核对计划开工和交期。操作员在 09:30 能否开工还要看计划开工门槛、选线和现场条件。追溯时先明确手里的编号是物料批、生产批、托盘还是单件序列号，再选查询方向。

## 技术逻辑

### 排程与能力

SchedulePlannerService 是排程阅读入口，CapacityService 提供能力相关计算。调整计划与实际开工要分开：修改计划时间不应被直接理解为已经发生生产。

WorkOrder 的 plannedStartBlocker / assertProductionAvailable 参与生产入口约束。排程页面显示可安排，不等于当前操作员已经满足工位、物料和质量开工条件。

### OEE 与工时

OeeCalculationService 汇总绩效相关数据；WorkstationStateMachine、DowntimeService 管理工位状态与停机事实。阅读计算时先确认统计窗口、工位/产线范围、班次和数据来源，再解释指标。

BatchService 同时记录系统测得 duration_minutes 和人工确认的 actual_elapsed / actual_setup / actual_run。两者分别保存，人工确认值需满足工时和约束；报表选择哪组数据必须追到具体计算函数。

### 追溯与包装

| 业务问题 | 源码入口 |
| --- | --- |
| 该生产批消耗了哪些材料批 | MaterialAllocationService、TraceabilityService |
| 某个序列号经过哪些过程 | SerialTraceService |
| 测试日志如何解析与导入 | Traceability/TestLog/TestLogParser、TestLogImporter |
| 单件如何打包 | Packaging/UnitPackingService |
| 托盘如何移动 | Logistics/PalletMovementService |
| 批号如何生成与放行 | Lot/LotService、BatchReleaseService |

本章提供旁路定位，不宣称已逐一验证全部格式、计算公式或现场配置。具体扩展应先选择一个用例，沿入口、服务、模型、测试追踪闭环。

### 变更影响分析

改工序数量需同时关注产量、报废、OEE、物料耗用、质量触发和追溯。改排程需关注计划开工阻塞、产线分配与可见性。改批号需关注打印、追溯和外部接口，不只改显示文本。

### 分钟冲突与放行

minuteConflictExists 在相应产线范围内检查其它活跃工单的计划时间重叠，条件为「已有开始早于候选结束，且已有结束晚于候选开始」。08:00–09:00 和 09:00–10:00 在这个判断中只相接，不重叠。工单是否可生产则由 planned_start_at 与系统时间比较。没有计划开始时间的既有工单仍可通过该时间筛选。

### 用一组数字复算 OEE

以产线 OeeCalculationService 为例，假设班次 480 分钟，计划停机 30 分钟，损失停机 45 分钟，总产 300 件、报废 15 件、理想节拍每件 1.2 分钟。净计划 450 分钟，运行 405 分钟，可用率 90%，性能率约 88.89%，质量率 95%，乘积为 76%。源码对性能率封顶 100%，分母条件不满足时相应指标可能为空。这个算例针对产线计算入口，复核工位指标时应读相应计算路径。

### 从执行事实查正反向追溯

batchGenealogy 把 BatchStepLotConsumption 按工序分组，并读入工位、完成者和输出批。正向查物料批的受影响范围，反向查生产批的实际输入；遇到缺失关系时，先确认批号追踪、选批与实际消耗是否记录。单件路线则看 SerialTraceService 的扫码绑定与工序历史。例如 bindComponent 对同一绑定的重复扫码返回已有记录。

## 排程数据的输入与验收

排程涉及 line_id、due_date、week_number、shift_number、end_date、end_shift_number、planned_start_at 和 planned_end_at 等 placement 字段。WorkOrder 模型把这些字段明确归为位置或时间变更；新增导入入口若也修改这些字段，同样要考虑排程事件与撤销记录。

| 场景 | 示例 | 检查结果 |
| --- | --- | --- |
| 时间相接 | A 08:00–09:00，B 09:00–10:00 | 分钟重叠判断不冲突 |
| 时间重叠 | A 08:00–09:00，B 08:30–09:30 | 检查同产线活跃单冲突分支 |
| 结束不晚于开始 | 已有工单改成 10:00–09:00 | 模型保存校验拒绝 |
| 未开工订单排到未来 | 当前 09:00，计划 10:00 | 操作员生产入口受计划门槛限制 |
| 已开工订单推迟到未来 | 已有开工或实产记录 | 模型保护不允许借排程隐藏已发生生产 |

跨日、跨班和时区应补独立样本。页面显示的工厂时间、服务端解析时间、设备 observed_at 和报表统计日期需要能相互对应；不要直接对浏览器格式化后的字符串做冲突判断。

## OEE 计算契约与缺失值

对产线 calculateForDate 入口，可用率按运行时间除以扣除计划停机后的计划时间计算；性能率按总产乘理想节拍再除以运行时间计算，并封顶 100%；质量率按良品除以总产计算。记录中分别保存分项百分比及最终 OEE。

| 输入不足 | 当前计算的表现 | 页面应如何解释 |
| --- | --- | --- |
| 计划分钟不大于 0 | 返回 null，不产生本次计算记录 | 无可计算计划窗口 |
| 净计划时间不大于 0 | 可用率为空 | 分母不成立 |
| 运行时间或理想节拍不大于 0 | 性能率为空 | 缺时间或节拍依据 |
| 总产不大于 0 | 质量率为空 | 无产出供计算 |
| 任一分项为空 | 最终 OEE 为空 | 不能擅自转换为 0% |

同一产线、工位为空、班次与记录日期构成该入口 updateOrCreate 的定位条件。重新计算可能更新记录，因此导出报告应说明数据截至时间。工位 OEE 或其他报表要另读其查询，不能因名称相同就复用产线公式和数据窗口。

## 追溯查询的输入输出

| 手里已有的标识 | 查询目标 | 应核对的关系 |
| --- | --- | --- |
| 物料批号 | 找出影响的生产批和产出 | 实际耗用 → 工序 → 批次 |
| 生产批 ID / lot_number | 解释其投入与执行 | 工单快照、步骤、物料批、操作者、质量记录 |
| 单件序列号 | 查询该件过程与组件绑定 | 扫码事件、工序历史、组件关系 |
| 托盘或包装标识 | 解释装箱及物流位置 | 包装成员、批次与移动记录 |

同一个文本编号在不同类别中含义不同，接口与界面应明确标识类型。查不到关系可能是当时没有启用批号追踪、未记录实际耗用或查错标识，不应直接解释为“没有使用该物料”。

## 跨系统追溯实验

在隔离环境准备物料批 L1 和 L2，分别投入两个生产批，记录实际选批和工序消耗。完成后从 L1 正查，再从对应生产批逆查，验证两条方向指向同一消耗事实。若启用单件和包装，再把一件绑定到包装，验证序列号、生产批与包装之间能连起来。

扩展追溯导出时，保留原始 ID、业务编号、事件时间和关系类型，避免仅输出人类可读名称。供应商问题的影响范围需要区分已投料、已产出、已包装与已发运，各范围依赖不同事实，不能仅凭 BOM 推导所有产品都实际受影响。

## 源码依据

- [backend/app/Services/Schedule/SchedulePlannerService.php:1](/Users/seal/Desktop/myLog/program/SKhynix/OpenMes/backend/app/Services/Schedule/SchedulePlannerService.php:1)
- [backend/app/Services/Schedule/CapacityService.php:1](/Users/seal/Desktop/myLog/program/SKhynix/OpenMes/backend/app/Services/Schedule/CapacityService.php:1)
- [backend/app/Services/Production/OeeCalculationService.php:1](/Users/seal/Desktop/myLog/program/SKhynix/OpenMes/backend/app/Services/Production/OeeCalculationService.php:1)
- [backend/app/Services/Traceability/TraceabilityService.php:1](/Users/seal/Desktop/myLog/program/SKhynix/OpenMes/backend/app/Services/Traceability/TraceabilityService.php:1)
- [backend/app/Services/Traceability/SerialTraceService.php:1](/Users/seal/Desktop/myLog/program/SKhynix/OpenMes/backend/app/Services/Traceability/SerialTraceService.php:1)
- [backend/app/Services/Packaging/UnitPackingService.php:1](/Users/seal/Desktop/myLog/program/SKhynix/OpenMes/backend/app/Services/Packaging/UnitPackingService.php:1)
- [分钟排程交叠条件](/Users/seal/Desktop/myLog/program/SKhynix/OpenMes/backend/app/Services/Schedule/SchedulePlannerService.php:580)
- [计划开工门槛](/Users/seal/Desktop/myLog/program/SKhynix/OpenMes/backend/app/Models/WorkOrder.php:85)
- [产线 OEE 的计算与落表](/Users/seal/Desktop/myLog/program/SKhynix/OpenMes/backend/app/Services/Production/OeeCalculationService.php:24)
- [生产批工序与实际物料批关联](/Users/seal/Desktop/myLog/program/SKhynix/OpenMes/backend/app/Services/Traceability/TraceabilityService.php:403)
- [扫码组件绑定](/Users/seal/Desktop/myLog/program/SKhynix/OpenMes/backend/app/Services/Traceability/SerialTraceService.php:410)
