# 两种流转与数量账本

whole_batch 控制整批顺序，transfer 让工序按已转出的数量重叠执行。两者的完工与计产时点不同。

## 使用逻辑

### 用 100 件生产任务核对每一笔

下面假设两道连续工序、人工计数、启用顺序校验，且没有阻塞项。数量是执行后的累计结果。页面每次录入新增数量时，先看本站待处理量。

| 现场动作 | 第一站通过 / 报废 / 待处理 | 第二站通过 / 报废 / 待处理 | 批次成品 |
| --- | --- | --- | --- |
| 批次开始，第一站尚未报数 | 0 / 0 / 100 | 0 / 0 / 0 | 0 |
| 第一站报通过 60、报废 5 | 60 / 5 / 35 | 0 / 0 / 60 | 0 |
| 第二站开工并报通过 55、报废 2 | 60 / 5 / 35 | 55 / 2 / 3 | 55 |
| 第一站再报通过 35 并关闭 | 95 / 5 / 0 | 55 / 2 / 38 | 55 |
| 第二站再报通过 38 并关闭 | 95 / 5 / 0 | 93 / 2 / 0 | 93 |

若工单目标也是 100 件，这批结束后留下 7 件差额。第二站在第三行已经有 55 件成品，当时仍有 3 件待处理，且上游还可能继续来料。只有末站清空、上游关闭和其他完工条件均满足，才能完成第二道工序。

把同一任务换到 whole_batch 时，启用顺序控制的下游先等前序结束。最后结算时核对当前入口实际传了什么完工产量。例如在支持提交完工产量的入口传入 93，才应按该输入校验汇总结果。默认回退值是批次目标量。

## 技术逻辑

### 模式对照

| 规则 | whole_batch | transfer |
| --- | --- | --- |
| 下游可启动条件 | 前序 DONE / SKIPPED 等前置条件满足 | 已有可流入数量且满足其他约束 |
| 数量写入 | 完成步骤会转出未报废剩余量 | 运行期间 recordQuantity 分次报数 |
| 结算时点 | 完批结算 | 末有效工序通过时实时汇总 |
| 工序关闭 | 完成规则满足 | 还必须无待处理量、无未来流入 |
| 工单结束 | 达到完成条件 | 达到完成条件且不再有进行中批次 |

production_flow_mode 是系统设置，默认 whole_batch。ProductionFlow 缓存使用 scoped 容器并设置短期过期，长驻进程也要观察配置变化；它不是每张工单独立选项。

### 数量如何在有效工序间传递

首道有效工序从批次目标量取输入，其余工序从最近一个未跳过的前序取得累计通过量。每站只扣自己的通过和报废，所以第一站报废的 5 件已经从传往第二站的数量中排除。

```text
输入量 incomingQty
  − 已通过 passed_qty
  − 本站报废 scrap_qty
  = 可处理 availableQty（以模型的实际实现为准）
```

最后有效工序不包含 SKIPPED 步骤。更改可选路线会影响数量归属，因此已有本步骤或下游数量时，服务会阻止相关跳过与路线改动。

### 更正不是覆盖数据库

correctGoodQuantity 仅用于满足开关条件的运行中人工计数步骤。它检查操作者或主管权限、expected 旧值、输入上限以及下游已耗用量，记录 AuditLog 后重新汇总。若下游已经加工，先处理下游更正，不能把上游改小到破坏数量守恒。

### 常见误读

- 全报废可能使一个批次结束，但工单仍需要补产。
- 已达到计划量不代表 transfer 的最后批次已经关闭。
- whole_batch 的默认完批产量与逐工序报废账本要分开核对。
- 设备累计寄存器值既不是工序增量，也不是工单成品数。

### 三类边界怎样落在代码里

1. **上游全报废。** 在顺序校验开启时，prerequisitesMet 对 transfer 使用「上游关闭或本站待处理量大于零」。上游全报废且已关闭时，下游仍可开工并在满足条件时关闭，避免批次卡在没有来料的工序。
2. **报数和关闭。** recordQuantity 在事务内锁定并重算可用量，要求步骤正在运行。超量时 Web 返回 good_qty 字段错误，操作者可修正输入再提交。completionBlocker 单独要求上游已关闭和本站待处理量归零。
3. **请求重发。** 数量上限能挡住超量重发。只要仍有余量，同样的人工增量可能是又一次合法报数。提交结果不明时先查工序累计值。更正入口另带预期旧值和原因，避免覆盖别人刚写的结果。

## 数量字典与计算边界

| 名称 | 数据归属 | 能否直接用于工单成品统计 |
| --- | --- | --- |
| planned_qty | 工单计划 | 否，属于需求 |
| target_qty | 批次目标 | 否，属于本批投入目标 |
| incomingQty() | 工序计算值 | 否，包含本站尚未处理的输入 |
| passed_qty | 工序累计通过 | 只有按模式处理末有效工序时才参与成品汇总 |
| scrap_qty | 工序报废 | 不应在每个下游重复扣除 |
| availableQty() | 工序待处理量 | 用于本次报数上限与关闭检查 |
| produced_qty | 批次或工单 | 必须先确认所读对象及其汇总时点 |

BatchStep.availableQty 的实现为 max(0, round(incomingQty − passed_qty − scrap_qty, 2))，SKIPPED 返回 0。这种钳制保护展示与判断，但不能拿“结果非负”证明历史账本完全正确；排查异常要同时看未经钳制的三个原始数。

## 关闭条件与零产出场景

transfer 的 completionBlocker 分两次检查：上游是否还可能来料，以及本站是否还有待处理量。第一道有效工序没有前序，所以输入天然关闭，但仍要处理本批目标量。下游本站余量为 0，也可能因为上游还在运行而不能关闭。

例如第一站输入 10，全部报废且关闭；第二站输入 0，在顺序检查启用时仍可满足前序条件。按要求完成其他证据后关闭第二站，批次可以结束，但 0 件成品不能满足目标 10 件的工单。这是需要补产的结果，而不是必须把第二站数量补成 10。

## 人工数量更正的操作规程

更正前保存工序当前 passed_qty、scrap_qty、下游已处理量和工单汇总。调用更正入口时携带该入口要求的 expected 旧值和原因；如果别的操作者已经改动，应重新读取并重新判断，不能无条件覆盖。

假设上游已通过 60，下游已通过 50 且报废 5。此时下游已经消耗 55，上游改成 54 会破坏输入关系。应先调查下游记录是否确有错误，再按业务更正流程处理；不应为了让上游更正成功而随意减少下游数量。

人工更正服务还限制运行状态、人工计数条件及权限。机器历史读数需要第 06 章的复核流程，不能用人工更正绕过来源与审计。

## 流转模式与计数源的组合

whole_batch / transfer 决定推进和结算方式，operator / machine / both 决定计数来源。两个维度必须一起记录，不能把“采用 transfer”理解为“只能人工计数”。both 也不代表可以把人工结果和设备历史累计值直接相加。

修改模式相关逻辑前，应分别走人工报数、明确配置的机器通道、Legacy 兼容通道以及完批入口。transfer 下旧机器通道兼容检查可能直接阻塞生产；现场切换模式前需要验证已有通道和运行中批次，而不只是切换页面设置。

## 数量验收表

| 用例 | 输入或操作 | 期望 |
| --- | --- | --- |
| 额度内分次报数 | 剩余 10，先报 4 再报 6 | 累计 10，余量 0 |
| 超出额度 | 剩余 10，提交良品与报废合计 11 | 拒绝，不留下部分数量 |
| 同请求重发 | 第一次增量已成功，仍有余量 | 可能再次计数；调用端先核实第一次结果 |
| 下游先关闭 | 本站余量 0，但上游未关闭 | transfer 拒绝关闭 |
| 路线切换 | 本站或下游已有数量 | 核对服务拒绝条件，不能丢失已有归属 |
| 整批默认值 | whole_batch 完批未传实产 | 核对默认回退及计数源，不能据报废表自行推算返回值 |

建议把第一个 100 件算例保存成回归数据，并比较每一步后的三层数量。只有最终工单数量正确，仍不能排除中间站重复计数或错误归属。

## 源码依据

- [backend/app/Support/ProductionFlow.php:19](/Users/seal/Desktop/myLog/program/SKhynix/OpenMes/backend/app/Support/ProductionFlow.php:19)
- [backend/app/Models/BatchStep.php:349](/Users/seal/Desktop/myLog/program/SKhynix/OpenMes/backend/app/Models/BatchStep.php:349)
- [backend/app/Services/WorkOrder/BatchService.php:319](/Users/seal/Desktop/myLog/program/SKhynix/OpenMes/backend/app/Services/WorkOrder/BatchService.php:319)
- [backend/app/Services/WorkOrder/BatchService.php:253](/Users/seal/Desktop/myLog/program/SKhynix/OpenMes/backend/app/Services/WorkOrder/BatchService.php:253)
- [backend/app/Services/WorkOrder/BatchService.php:608](/Users/seal/Desktop/myLog/program/SKhynix/OpenMes/backend/app/Services/WorkOrder/BatchService.php:608)
- [backend/tests/Feature/Web/Operator/StepQuantityFlowTest.php:1](/Users/seal/Desktop/myLog/program/SKhynix/OpenMes/backend/tests/Feature/Web/Operator/StepQuantityFlowTest.php:1)
- [前序条件与全报废分支](/Users/seal/Desktop/myLog/program/SKhynix/OpenMes/backend/app/Models/BatchStep.php:299)
- [Web 报数与字段错误](/Users/seal/Desktop/myLog/program/SKhynix/OpenMes/backend/app/Http/Controllers/Web/Operator/BatchController.php:164)
- [数量安全回归用例](/Users/seal/Desktop/myLog/program/SKhynix/OpenMes/backend/tests/Feature/Web/Operator/StepQuantitySafetyTest.php:27)
