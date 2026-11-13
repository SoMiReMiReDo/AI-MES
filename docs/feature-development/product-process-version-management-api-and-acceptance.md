# F2-`ProductProcessVersionManagement` 接口与验收

日期：2026-11-11｜状态：设计验收基线；首版实现与实际测试覆盖见[实现说明](f2-f3-implementation.md)，未逐项覆盖的场景仍待验证。依据：[能力设计](product-process-version-management.md)。

## 通用契约

以下为接口设计基线，实际接口细化见实现说明。前缀 `/api/v1`，JSON 使用 camelCase，时间为 UTC ISO 8601；日期字段为 YYYY-MM-DD。列表参数 page/pageSize（默认 1/20，上限 100），返回 items/total/page/pageSize。详情返回 ETag；更新及状态动作要求 If-Match，缺失返回 428，版本过期返回 412。写入使用现有 Cookie、防伪与会话验证。

创建返回 201 和资源 ID；更新/动作返回 200 与新版本。401 未登录、403 无权限、404 对象不存在或不可见、409 状态/唯一性/幂等冲突、422 业务校验失败；沿用 ProblemDetails，含稳定错误码和 correlationId，不暴露数据库细节。

状态动作及编号分配要求 Idempotency-Key：作用域包含操作者、动作与目标，保存请求摘要和结果；同键同请求重放原结果，同键不同请求返回 409。幂等重放仍须检查当前访问权限。错误或事务回滚不得留下成功回执。

客户端不能写审计人、发布时间、完成量或数据库行版本。服务端校验字段长度、枚举、关联对象与范围；拒绝未经定义的写字段。页面按钮权限仅用于展示，所有规则在服务端再次执行。

## 接口分组

| 路径（前缀略） | 方法与用途 | 核心输入/规则 |
| --- | --- | --- |
| /engineering/products | GET/POST 产品列表/创建 | code/name/unitOfMeasure/category；编码唯一 |
| /engineering/products/{id} | GET/PUT 详情/编辑 | name/description/isActive；编码创建后固定 |
| /engineering/products/{id}/revisions | GET/POST 修订列表/草稿 | revisionCode/routeVersionId/有效期/说明 |
| /engineering/product-revisions/{id} | GET/PUT 草稿详情/修改 | 产品归属不可改；版本状态禁止直接写 |
| /engineering/product-revisions/{id}/impact | GET 影响工单/Lot | 分页、厂区过滤，不能据此直接迁移 |
| /engineering/operations | GET/POST 工序 | code/name/segmentType/equipmentTypeId |
| /engineering/operations/{id} | GET/PUT 工序详情/更新 | 启用状态及说明；不覆盖已发布步骤基线 |
| /engineering/routes | GET/POST 路线 | code/name/productId（可空） |
| /engineering/routes/{id} | GET/PUT 路线详情/更新 | 名称、说明、启用状态 |
| /engineering/routes/{id}/versions | GET/POST 版本 | version/有效期/changeReason |
| /engineering/route-versions/{id} | GET/PUT 版本详情/草稿 | 嵌套 steps/checklistItems/outputDefinitions，整体校验并原子保存 |
| /engineering/recipes | GET/POST Recipe | code/name/description |
| /engineering/recipes/{id} | GET/PUT Recipe 详情/更新 | 名称、说明、启用状态 |
| /engineering/recipes/{id}/versions | GET/POST 版本 | version/parameters/checksum |
| /engineering/recipe-versions/{id} | GET/PUT 版本详情/草稿 | 已发布参数不可改 |
| /engineering/{versionKind}/{id}/release、/obsolete、/copy | POST 发布/废止/复制 | versionKind 限 product-revisions/route-versions/recipe-versions；reason；复制需新版本号 |
| /engineering/recipe-applicabilities | GET/POST 适用关系 | recipeVersionId/routeStepId/equipmentId/effectiveFrom/effectiveTo |
| /engineering/recipe-applicabilities/{id} | PUT 调整适用关系 | If-Match；审计；禁止覆盖历史执行依据 |
| /engineering/recipe-candidates | GET 配方候选 | routeStepId/equipmentId/at，返回命中依据 |
| /engineering/number-sequences | GET/POST 编号规则 | name/purpose/productId/prefix/suffix/padSize/nextNumber |
| /engineering/number-sequences/{id} | GET/PUT 规则详情/更新 | 不得回拨或制造重号 |
| /engineering/number-sequences/{id}/preview、/allocate | POST 预览/分配 | allocate 必须幂等；业务内部直接复用同事务服务 |
| /engineering/documents | POST multipart 上传 | file/entityType/entityId/revision；服务端校验关联和文件 |
| /engineering/documents/{id}、/{id}/content | GET 元数据/受权下载 | 下载不暴露 StoragePath |
| /engineering/documents/{id}/release | POST 文档发布 | 摘要、文件存在性、关联对象验证 |

编号独立分配须保存不可重复使用的回执；业务创建失败可以保留断号，但不能重复分配。工程通用读写分别对应 engineering.read/manage，版本发布/废止对应 release，附件写对应 documents.manage，编号正式分配对应 numbers.allocate；全部仍检查部署厂区权限。

## 验收清单

实际执行覆盖见实现说明；未覆盖细项仍待验证，不能仅凭文档标通过。

| ID | 场景及期望 |
| --- | --- |
| F2-A01 | 重复产品编码、同产品重复修订号、同路线/Recipe 重复版本号被拒绝 |
| F2-A02 | 空路线、重复/非正步骤序号、失效工序、跨产品路线绑定发布失败 |
| F2-A03 | 同工序重复步骤合法，按独立步骤 ID 保存与展示 |
| F2-A04 | 发布后修改步骤/点检/输出/参数、删除版本、恢复草稿均被拒绝 |
| F2-A05 | 并发发布与编辑只能按顺序产生一致结果，不出现已发布半成品 |
| F2-A06 | 复制产生新版本及新步骤 ID，源版本保持不变，引用正确重映射 |
| F2-A07 | Recipe 按步骤/设备/有效期筛选；区间结束时刻不命中，无候选不伪造默认值 |
| F2-A08 | 废止阻止新使用，历史工单/Lot 基线仍可查 |
| F2-A09 | 并发分配不重号、预览不占号、同键重放一致、不同请求同键冲突 |
| F2-A10 | 必填输出、枚举选项、key 唯一性与非法类型验证正确 |
| F2-A11 | 越权附件下载、非法绑定、路径输入、超限文件被拒绝；发布文件不可覆盖 |
| F2-A12 | 不同角色/厂区的列表、详情、影响查询和写入均受服务端限制 |
| F2-A13 | 版本冲突返回 412；事务失败无部分数据/审计/成功回执 |
| F2-A14 | WebUI 完成产品→路线→修订发布及 Recipe 配置，错误有可操作提示 |

F5 实际配方记录及点检/输出执行不在本轮验收，见 GAP-06；数据库测试、HTTP 集成测试与浏览器验证分别留存证据。
