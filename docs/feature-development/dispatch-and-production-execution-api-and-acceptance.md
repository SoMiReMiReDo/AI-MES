# F5 接口与验收

日期：2026-11-13｜设计基线：[功能设计](dispatch-and-production-execution.md)。以下均为本轮验收条件，执行结果由实现记录逐项说明。

## 1. API

统一前缀 `/api/v1`，复用 Cookie、CSRF、ProblemDetails、ETag、幂等与厂区授权。列表分页默认 20、最大 100，稳定排序。

| 方法/路径 | 业务契约 |
| --- | --- |
| GET execution/dispatch | Ready/Waiting；设备组、步骤、最低优先级、最少等待分钟、lotNumber；优先级降序/等待时间/ID，含阻塞与候选数 |
| GET execution/lots/{id} | Lot/当前步骤、成员、指令、定义/已录记录、绑定文件、候选设备配方、最近 Run |
| POST execution/lots/{id}/assign | equipmentId、recipeVersionId；If-Match=Lot，保存人工指派，不预占 |
| POST execution/lots/{id}/track-in | equipmentId、equipmentEtag、recipeVersionId；If-Match=Lot，返回 Run |
| GET execution/runs | Running/历史列表，支持 status/lotId |
| GET execution/runs/{id} | 实际设备/配方/参与片、Track 与作业数据、阻塞 |
| POST execution/runs/{id}/records | checklistItemIds、outputs[{outputId,unit,valueText/valueNumber/valueBoolean/valueDate}]、documentIds、confirm、notes；If-Match=Run，一次提交新增事实 |
| POST execution/runs/{id}/track-out | wafers[{waferId,result:Passed/Failed,notes}]；If-Match=Run，片集必须完整唯一 |
| POST execution/runs/{id}/abort | reason；If-Match=Run，需独立 abort 权限 |
| GET execution/lots/{id}/documents/{documentId}/content | 只下载该批已绑定文件，验证完整性 |
| GET/POST equipment/types、equipment/groups、equipment/tools | 分页台账/新建；Group 固定部署 Fab，Tool 需 Area/Group/Type |
| GET equipment/tools/{id} | 详情、当前占用、状态履历 |
| PUT equipment/tools/{id} | name、isActive；不提供随意迁移归属 |
| POST equipment/tools/{id}/state | state:Idle/Down/Maintenance/Offline、reason；人工状态历史 |
| GET planning/work-orders/{id}/progress | 唯一片进度、coverage、missingDependencies、asOf |
| POST planning/work-orders/{id}/recalculate | reason；If-Match=工单，重算 ProducedQty 缓存 |

新建类型：code/name；设备组：code/name；设备：code/name/areaId/equipmentGroupId/equipmentTypeId/可选 lineId，初始 Idle。设备写入同样需要幂等，更新需要版本。不接受外部指定 Fab、人员、时间、状态快照或产出数量。

错误：400 格式/未知字段，403 权限，404 不存在或不可见，409 状态/占用/门禁/幂等冲突，412 版本过期，428 缺版本，422 片集或类型值无效。网络失败可保留同一命令键重试；修改表单必须生成新键。

## 2. 验收条件

| ID | 场景与预期 |
| --- | --- |
| E01 | 5 片、3 站全流程经真实 API 完成，15 条参与片、3 对 Track、5 条唯一完成，Lot Completed，工单完成量 5 |
| E02 | 设备组/步骤/优先级/等待时间筛选、分页和稳定排序；显示阻塞原因，指派后仍复核占用 |
| E03 | 错误设备类型、跨厂/停用设备、非 Idle、运行占用、错误配方/未生效/快照外配方均拒绝，零副作用 |
| E04 | 当前步骤或片集变化、非 Active、缺首次投放、有效 Hold/停工/质量任务阻止 Track-in |
| E05 | 并发两批争同设备仅一批成功；同批并发启动最多一个 Run |
| E06 | 正常请求重放不重复事实，字段顺序/数值等价不影响回执；同键异参拒绝；旧 ETag 同键重放成功 |
| E07 | 缺少/过期 Lot、Run、设备版本得到 428/412；过程记录推进相关聚合版本 |
| E08 | 点检/输出/PDF/关键确认缺失不能正常结束；错误类型、选项、单位及外站定义拒绝并回滚 |
| E09 | 绑定作业定义和 PDF 可查看，执行最小权限可下载本批历史文件，外批/非绑定文件拒绝 |
| E10 | 逐片结果遗漏、重复、外片、非法结果拒绝；Failed 不计报废/完成，Run 可关闭且 Lot 待处置 |
| E11 | 中止需权限和原因，Run Aborted、片 Aborted、释放占用，保留过程事实且不得启动下一步 |
| E12 | 加工中新 Hold 可受控结束但不推进；两条 Hold 释放一条仍阻塞；停工/质量任务同理 |
| E13 | 启用尚未实现 Q-time/质量触发或未知工艺策略必须拒绝启动，不显示全部门禁通过 |
| E14 | 撤销动作权限/Fab 后旧回执不可重放；查询、下载、详情及列表不泄露跨厂对象 |
| E15 | 中间站后拆成 2+3 并合回，随后完成仍只计 5 唯一片，前站加工从逐片历史验证 |
| E16 | 进度包含排队/Running/待处置，按片去重；完成不再是 WIP；重算恢复缓存且不造历史 |
| E17 | 关闭工单与启动竞争受同锁保护；仍有有效生产关联拒绝；全部终结后可关闭 |
| E18 | 迁移空库建立、全降级/重建通过；历史身份、已结束 Run 和已录事实保护；最小运行角色可执行 |
| E19 | WebUI 完成设备维护、派工、执行记录、逐片退出与真实进度；原 F1～F4 回归通过 |

F6 质量处置、真实设备协议和资格等不计本轮验收通过；测试夹具插入 Hold 不等于已实现 F6 用户入口。
