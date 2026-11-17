# F6 接口与验收

更新：2026-11-17｜设计基线：[功能设计](quality-exception-disposition.md)。执行结果见实现记录。

## 1. 协议

根路径 `/api/v1/quality`。全部接口要求有效会话和厂区范围；写入防伪令牌及 `Idempotency-Key`，更新或 Lot 操作要求 `If-Match`。同键同参重放；异参409，缺版本428，过期412，业务冲突409，参数错误422，不存在/不可见404。查询支持 page/pageSize（20/最大100），status、lotId、q 等资源适用筛选；详情返回 etag。

| 资源 | 查询 | 写入 |
| --- | --- | --- |
| reason-codes | GET 列表/`{id}` | POST 创建、PUT `{id}` 更新名称/描述/启用（类别和代码固定） |
| issue-types | GET 列表/`{id}` | POST 创建、PUT `{id}` 更新名称/严重度/阻塞/启用 |
| issues | GET 列表/`{id}`（含整改、Hold） | POST 登记；POST `{id}/acknowledge`、`assign`、`resolve`、`close` |
| issues/{id}/actions | 随异常详情 | POST 新增整改 |
| actions/{id} | GET 详情 | POST `complete`、`verify` |
| lots | GET 列表/`{id}`（含 Hold、放行、当前片/Run） | POST `{id}/holds`、`release`、`scrap` |
| holds | GET 列表/`{id}` | POST `{id}/release` |
| specifications | GET 列表/`{id}` | POST 创建 Draft、PUT `{id}` 修改草稿、POST `{id}/release` |
| templates | GET 列表/`{id}` | POST 创建 Draft、PUT `{id}` 修改草稿、POST `{id}/release` |
| checks | GET 列表/`{id}`（含量测） | POST 提交完整检验 |
| scraps | GET 列表/`{id}`（含逐片处置） | 通过 Lot 报废命令创建 |
| assignees / routes / equipment | GET 有范围的选择项 | 无 |

具体请求类型以 `MESagent.Application/QualityExceptionDisposition.cs` 及生成 OpenAPI 为准。人员、权限、时间、规格快照和判定值均由服务端保存。检查和报废列表先按 Lot 解析厂区，再计数分页。

## 2. 验收场景

| ID | 场景与预期 |
| --- | --- |
| Q01 | 原因码按类别维护；停用/错类别不能用于新业务，历史仍可读 |
| Q02 | 异常至少一个生产/设备对象；跨厂与对象不一致拒绝 |
| Q03 | 异常 Open→确认→指派→处置→关闭；跳转、重复动作与旧版本拒绝 |
| Q04 | 指派仅限本厂有效人类账号，不能伪造操作者 |
| Q05 | 整改须先完成后复核；未全部复核或有效 Hold 时不能关闭异常 |
| Q06 | 阻塞异常原子创建 Hold；后改类型不回写既有阻塞性 |
| Q07 | 同一 Lot 两条 Hold，解除一条仍阻塞；全部解除不自动推进 |
| Q08 | Running 新增 Hold，不破坏 Run；F5 收尾后 AwaitingDisposition |
| Q09 | 未解除 Hold、停工、未完成任务/阻塞异常拒绝质量放行 |
| Q10 | 全 Passed 且仅阻塞未推进，审核 Continue 后进入下一站 |
| Q11 | 路线终点 Continue 创建真实 WaferCompletion，重复请求不重复记账 |
| Q12 | Failed/中止拒绝直接 Continue，Retry 生成同站 VisitNo+1 |
| Q13 | Retry 保留旧 Run/参与片结果；点检、输出、文档及确认重新完成 |
| Q14 | 多次重试编号连续，设备/Recipe 校验继续生效 |
| Q15 | 规格类型、精度、上下限及期望值校验；发布后不可改 |
| Q16 | 模板绑定具体已发布规格；发布后冻结，升级用新版本 |
| Q17 | 数值边界闭区间、单边限、布尔/文本期望值均由服务端判定 |
| Q18 | 缺样本/参数、重复样点、错单位/类型、外厂量测设备拒绝且无半条记录 |
| Q19 | Lot 检验及逐片检验成功，历史保存规格快照及设备/样点 |
| Q20 | 不合格原子创建异常/Hold；同键重放仅一套记录 |
| Q21 | 合格复检追加记录，不自动释放旧 Hold 或改历史结果 |
| Q22 | 报废数量与片集必须一致；空集、重复/跨 Lot/非 Active/已完工拒绝 |
| Q23 | 运行中拒绝报废；先受控收尾后允许 |
| Q24 | 部分报废关闭片归属和槽位，保留原 Lot/Run；其余片继续/重试后真实完工 |
| Q25 | 全报废关闭 Lot 为 AllScrapped，完工量为0，投放量不回减 |
| Q26 | 报废任一步骤失败时，处置、归属、装载及审计全部回滚 |
| Q27 | 同键异参409，旧版本412，缺版本428；并发报废仅一次成功 |
| Q28 | 最小质量角色可独立完成授权动作；无质量权限、无厂区、撤权后重放拒绝 |
| Q29 | F3 真实片进度、缓存重算/关闭与 F4 批次完工显示一致 |
| Q30 | 数据库冻结发布版本和历史；迁移回滚/重放、已有记录兼容 |
| Q31 | WebUI 完成异常整改、Hold/恢复、规格模板、检验、报废流程，空错/权限及冲突反馈有效 |

测试基于独立临时数据库；本地演示数据不作为可重置夹具，浏览器验收样本单独标记。
