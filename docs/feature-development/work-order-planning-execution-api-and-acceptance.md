# F3-`WorkOrderPlanningExecution` 接口与验收

日期：2026-11-11｜状态：设计验收基线；首版实现与实际测试覆盖见[实现说明](f2-f3-implementation.md)，未逐项覆盖的场景仍待验证。依据：[能力设计](work-order-planning-execution.md)。

## 通用契约

以下为接口设计基线，实际接口细化见实现说明。前缀 `/api/v1`，JSON 使用 camelCase，时间为 UTC ISO 8601；日期字段为 YYYY-MM-DD。列表参数 page/pageSize（默认 1/20，上限 100），返回 items/total/page/pageSize。详情返回 ETag；更新及状态动作要求 If-Match，缺失返回 428，版本过期返回 412。写入使用现有 Cookie、防伪与会话验证。

创建返回 201 和资源 ID；更新/动作返回 200 与新版本。401 未登录、403 无权限、404 对象不存在或不可见、409 状态/唯一性/幂等冲突、422 业务校验失败；沿用 ProblemDetails，含稳定错误码和 correlationId，不暴露数据库细节。

状态动作及编号分配要求 Idempotency-Key：作用域包含操作者、动作与目标，保存请求摘要和结果；同键同请求重放原结果，同键不同请求返回 409。幂等重放仍须检查当前访问权限。错误或事务回滚不得留下成功回执。

客户端不能写审计人、发布时间、完成量或数据库行版本。服务端校验字段长度、枚举、关联对象与范围；拒绝未经定义的写字段。页面按钮权限仅用于展示，所有规则在服务端再次执行。

## 接口与权限

| 路径（前缀略） | 方法/权限 | 输入与结果 |
| --- | --- | --- |
| /planning/work-orders | GET / read | 编号、状态、产品、交期筛选；分页前施加工单厂区过滤 |
| /planning/work-orders | POST / manage | fabId/orderNo 或编号规则、productId/productRevisionId/plannedQty/priority/dueDate/plannedStartAt/plannedEndAt/description；生成 Draft |
| /planning/work-orders/{id} | GET / read | 计划、状态、基线版本及允许动作 |
| /planning/work-orders/{id} | PUT / manage | 仅 Draft 更新计划字段，工单号与厂区创建后固定 |
| /planning/work-orders/{id}/release | POST / release | 原子生成初始快照并下达，返回 snapshotVersion |
| /planning/work-orders/{id}/cancel | POST / cancel | reason 必填；无首次投放且关联批次/成员/执行/质量均已终结 |
| /planning/work-orders/{id}/close | POST / close | reason 必填；允许全部生产关联终结的工单，仍有在制/阻塞则拒绝 |
| /planning/work-orders/{id}/snapshots | GET / read | 基线版本列表 |
| /planning/work-orders/{id}/snapshots/{version} | GET / read | 不可变历史快照 |
| /planning/work-orders/{id}/lots | GET / read | 关联 Lot 分页；不提供投批或绑定写接口 |
| /planning/work-orders/{id}/progress | GET / read | coverage/asOf/missingDependencies/plannedQty/releasedQty/inProcessQty/blockedQty/completedQty/scrappedQty |
| /planning/work-orders/{id}/recalculate | POST / recalculate | reason；从唯一完成事实重算 ProducedQty 缓存，不改历史事实 |

权限完整名称为 planning.work-orders.{动作}。只读接口不触发重算写入。2026-11-13 真实进度已接通：coverage=ProductionFacts；无法核验历史时 IncompleteHistory 且未知指标为 null，missingDependencies 保留完整 F6 质量处置。asOf 是一致性快照查询时间。release/cancel/close/recalculate 均要求 If-Match 和 Idempotency-Key。关闭已允许全部生产关联终结的工单，取消仍禁止已有首次投放的工单。

## 本轮验收

| ID | 场景及期望（执行覆盖见实现说明） |
| --- | --- |
| F3-A01 | 正常草稿创建编辑；零/负数量、非法时间、重复编号、产品修订不匹配被拒绝 |
| F3-A02 | 未发布、过期、废止路线/产品修订禁止下达，合法版本生成唯一第 1 版快照 |
| F3-A03 | 下达与工艺废止并发时按锁与事务得到一致结果；快照不混用版本 |
| F3-A04 | 下达重试不重复快照，同键不同内容冲突，事务失败完整回滚 |
| F3-A05 | 下达后不可直接改计划/版本；快照始终可解释且不可编辑 |
| F3-A06 | 空工单可取消或有原因关闭；有 Lot 时禁止取消；未集成生产终结判定时不允许带生产事实关闭 |
| F3-A07 | 终态不能恢复、改计划或再次下达；无原因的终态动作被拒绝 |
| F3-A08 | 越权列表/聚合/详情/快照访问被阻止；过期 ETag 返回 412 |
| F3-A09 | 进度缺依赖返回 null 与明确标识；UI 不把未知展示成零或 100% |
| F3-A10 | 隔离夹具覆盖多 Lot、重复成员与重复终点记录，统计按唯一 Wafer；此项仅验证规则，不代表真实闭环 |
| F3-A11 | 页面完成草稿→下达→查基线与关联 Lot；不可执行动作解释原因 |

## F4/F5 完成后的强制回补验收

以下是早期列出的依赖验收。2026-11-13 已回补的实际覆盖见 [F5 实现记录](dispatch-and-production-execution-implementation.md)，未覆盖的质量/返工场景继续跟踪 [GAP-01](deferred-capabilities.md)，不能据此将全表标为通过。

| ID | 回补场景与通过条件 |
| --- | --- |
| F3-R01 | 真实投批、过站到路线终点后，工单投放/在制/完成量与唯一 Wafer 对账一致 |
| F3-R02 | 拆批、合批、历史成员变动不重复计数；跨工单归属不明时标异常 |
| F3-R03 | 返工、终点请求重试和完成撤销按 F5 最终规则处理，不累计虚假完工量 |
| F3-R04 | 投批与取消/关闭并发遵守同一工单锁，不产生终态下新增执行 |
| F3-R05 | 数据聚合与 ProducedQty 缓存一致，重算可恢复；禁止人工覆盖统计结果 |
| F3-R06 | 实际浏览器流程验证进度；有证据后将 coverage 标记 Verified，质量阻塞未接 F6 时继续单独标缺 |

完成回补后同步能力文档、待补全清单及 MEMORY.md，并记录测试环境、执行结果和证据位置。
