# OrganizationIdentityAccessControl 接口与验收清单

日期：2026-11-11｜版本：v0.1｜状态：契约与验收基线；对应 API 和自动测试已建立

本文配套 [OrganizationIdentityAccessControl 功能设计](organization-identity-access-control.md)，用于拆分开发任务与验收；字段最终生成 OpenAPI，不能以本文代替已运行接口。

## 1. 公共契约

基础路径 `/api/v1`，JSON 字段使用 camelCase，ID 为 UUID，时间返回带 UTC 标记的 ISO 8601。所有管理接口要求普通有效会话、动作权限及部署厂区范围；安全管理接口另要求受保护 SystemAdmin。本人入口仅操作当前身份，UserId 由服务端获取。

查询默认 `page=1&pageSize=20`，最大 100，关键字最长 100；明确排序白名单并追加 ID 稳定排序。响应统一为 `items/total/page/pageSize`，必须先按范围过滤后计算 total。

对象详情返回 ETag，修改及状态命令携带 `If-Match`。组织实体使用 xmin 的不透明编码，账号/角色使用对应 ConcurrencyStamp；API 不暴露数据库内部版本字段名称。集合替换以父用户/角色的版本检查，关系表和父版本同事务更新。

成功读取 200，创建 201 并返回 Location，成功命令按下表返回 200 或 204。所有 Cookie 写请求校验防伪头 `X-CSRF-TOKEN`；获取防伪令牌返回 `Cache-Control: no-store`，登录与改密响应同样不可缓存。

| HTTP | 示例业务 code | 前端行为 |
| --- | --- | --- |
| 400 | `ValidationFailed`、`CsrfInvalid` | 标记字段/提示刷新身份后重取防伪令牌 |
| 401 | `LoginFailed`、`SessionInvalid` | 匿名页统一提示；已登录页清缓存并转登录 |
| 403 | `PermissionDenied`、`PasswordChangeRequired`、`FabScopeRequired` | 展示缺权或强制改密入口；不重复自动提交 |
| 404 | `ResourceNotFound` | 对不存在及不可见对象一致处理 |
| 409 | `CodeConflict`、`ParentInactive`、`ActiveDependencies`、`WorkerAlreadyLinked`、`LastAdministrator` | 展示可授权读取的冲突原因和处理入口 |
| 412 | `VersionConflict` | 重新读取并比较，保留未提交表单 |
| 428 | `VersionRequired` | 客户端应先读取版本再提交 |
| 429 | `RateLimited` | 按 Retry-After 等待，不循环重试登录 |
| 503 | `DependencyUnavailable` | 显示暂不可用；身份验证失败关闭访问 |

ProblemDetails 示例（字段与值为契约示例）：

```json
{
  "type": "about:blank",
  "title": "对象存在未处理的依赖",
  "status": 409,
  "code": "ActiveDependencies",
  "traceId": "request-correlation-id",
  "errors": {},
  "dependencies": [{ "type": "Workstation", "id": "authorized-object-id" }]
}
```

影响预览只是帮助用户判断，不能作为写入许可；提交时重新检查范围、版本和依赖。客户端不能传入 CreatedAt、UpdatedAt、操作者、RowVersion 原始值、PasswordHash 或权限声明；未知受控字段拒绝处理。

## 2. 登录、本人资料与会话

下表路径省略 `/api/v1`。`auth/me` 在受限改密模式仅返回 userId、displayName 和 passwordChangeRequired；普通模式可返回当前角色、部署厂区、权限代码与 UI 能力，不返回敏感内部字段。

| 方法与路径 | 输入/输出 | 授权与规则 |
| --- | --- | --- |
| `GET /auth/csrf` | 200：requestToken | 匿名或有效会话可获取；同源、防缓存 |
| `POST /auth/login` | username、password、可空 terminalId；200：passwordChangeRequired | 防伪、限流、身份验证；终端先验证身份再按范围校验；成功写 Cookie/会话 |
| `POST /auth/logout` | 无 Body；204 | 防伪，撤销当前会话并清 Cookie；重复退出保持成功 |
| `GET /auth/me` | 200：本人最小信息/已授权能力；普通模式返回本人资料 ETag | 普通或受限会话；不接受用户 ID |
| `PATCH /account/profile` | displayName；200：本人资料/ETag | 普通会话，本人，If-Match |
| `POST /account/change-password` | currentPassword、newPassword；204 | 普通或受限会话；验证当前密码；成功撤销所有会话并清 Cookie |
| `GET /account/sessions` | 200：本人会话列表 | 普通会话，隐藏内部凭据，仅本人 |
| `POST /account/sessions/{id}/revoke` | 204 | 普通会话，确认会话属于本人；重复撤销无副作用 |
| `POST /account/sessions/revoke-all` | 204 | 普通会话，撤销本人全部会话并清 Cookie |

本人改密以当前密码校验和 Identity 并发控制为准，不强制 If-Match；会话撤销为幂等动作，不要求客户端保存过期会话版本。退出在 Cookie 已无效时仍清 Cookie，已识别的 SessionId 应安全撤销；匿名重复退出不暴露会话是否存在。

受限改密用户访问 `/account/profile`、会话列表、管理及业务入口一律 403 `PasswordChangeRequired`；不能只在前端路由重定向。

## 3. 组织、员工、工位与终端

### 3.1 端点模式

| 资源名 | 读取权限 | 修改权限 | 范围参数 |
| --- | --- | --- | --- |
| `fabs` | org.structure.read | org.structure.manage | 仅 DeploymentFabId |
| `areas`、`lines`、`locations` | org.structure.read | org.structure.manage | fabId 或 areaId；服务端校验归属 |
| `workers` | org.workers.read | org.workers.manage | fabId |
| `workstations` | org.workstations.read | org.workstations.manage | areaId、可空 lineId |
| `terminals` | org.terminals.read | org.terminals.manage | fabId、可空 workstationId |

对每种资源使用下列模式，全部位于 `/organization` 下：

| 方法与路径 | 行为 |
| --- | --- |
| `GET /organization/{resource}` | 范围内分页列表；支持 keyword、isActive 与上表筛选 |
| `GET /organization/{resource}/{id}` | 详情/ETag，停用对象仍可读取 |
| `POST /organization/{resource}` | 创建；201 + DTO + ETag；Fab 不提供此公开端点 |
| `PATCH /organization/{resource}/{id}` | 更新允许字段；If-Match；200 + 新 DTO/ETag |
| `GET /organization/{resource}/{id}/deactivation-impact` | 修改权限，返回可见阻塞依赖摘要；无写入副作用 |
| `POST /organization/{resource}/{id}/activate` | reason + If-Match；200 + 新 DTO/ETag |
| `POST /organization/{resource}/{id}/deactivate` | reason + If-Match；复核依赖；200 + 新 DTO/ETag |

不提供 DELETE、通用 SQL 过滤、自定义 include 联表、批量导出和跨厂迁移端点。普通 PATCH 不能改 IsActive，状态变化单独记录原因。

停用有关联账号的 Worker 另需 SystemAdmin 及 `security.users.manage`；不能只凭员工维护权限停用登录身份。新建/启用子项与停用父项遵循功能设计中的同一父节点加锁规则。

### 3.2 请求字段白名单

通用创建字段为 code、name、description；通用修改仅 name、description。启停通过独立动作。业务字段补充如下：

| 资源 | 创建时附加字段 | 后续可改字段/特别限制 |
| --- | --- | --- |
| Fab | 仅引导：timezone | 公开 API 仅 name、description；timezone/code 固定 |
| Area | fabId | 归属创建后固定 |
| Line | areaId | 归属创建后固定 |
| Location | areaId、locationType | locationType 可改并审计，不覆盖历史使用记录 |
| Worker | fabId | 不通过员工 PATCH 反向指定任意账号；绑定走账号管理 |
| Workstation | areaId、可空 lineId | lineId 可在同 Area 内调整；有运行依赖时拒绝，相关终端会话撤销 |
| Terminal | fabId、deviceUuid、可空 workstationId | 绑定走单独端点；deviceUuid/fabId/code 固定 |

终端绑定：`POST /organization/terminals/{id}/bind-workstation`，请求 `{workstationId, reason}`（workstationId 可 null），要求 org.terminals.manage、If-Match；校验同厂、有效工位及未结场所依赖，记录绑定前后值并撤销关联会话。成功 200 + 新 DTO/ETag。

终端编号/设备 UUID 的录入错误在尚未使用时也不允许直接覆盖历史身份，使用停用并新建的方式；重复 UUID 的错误台账需管理员执行独立受控修复，不提供任意解除唯一约束入口。

## 4. 账号管理

路径位于 `/security/users`。读取仅返回 ID、username、displayName、worker 摘要、isActive、lockoutEnd、forcePasswordChange、角色、厂区范围、lastLoginAt 与 ETag；不得序列化 MesUser 全部字段。

| 方法与路径 | 请求/结果 | 权限与约束 |
| --- | --- | --- |
| `GET /security/users` | 分页、keyword/status/roleId | security.users.read；单厂实例管理员可看到无范围的待配置账号 |
| `GET /security/users/{id}` | 详情/ETag | security.users.read |
| `POST /security/users` | username、displayName、workerId?、temporaryPassword、roleIds、fabIds | security.users.manage；包含分配角色/范围时还需 assignments.manage；原子创建；201 |
| `PATCH /security/users/{id}` | displayName、workerId?；200 + ETag | security.users.manage；If-Match；变更员工绑定需撤销会话 |
| `POST /security/users/{id}/activate` | reason；200 + ETag | security.users.manage；If-Match；关联员工停用时拒绝 |
| `POST /security/users/{id}/deactivate` | reason；200 + ETag | 同上；禁止停用自己/最后管理员；撤销会话 |
| `POST /security/users/{id}/lock` | until、reason；200 + ETag | 同上；until 必须未来且有限；禁止锁定自己/最后管理员 |
| `POST /security/users/{id}/unlock` | reason；200 + ETag | 同上；清失败计数和锁定，不改变停用状态 |
| `POST /security/users/{id}/reset-password` | temporaryPassword、reason；204 | security.users.reset-password；If-Match；首登改密并撤销会话；自身重置使用本人改密路径 |
| `GET /security/users/{id}/sessions` | 200：目标会话列表 | security.sessions.revoke；对目标对象授权 |
| `POST /security/users/{id}/sessions/revoke-all` | reason；204 | security.sessions.revoke；不改账号其他状态；可重复 |
| `PUT /security/users/{id}/access` | 完整 roleIds、fabIds、reason；200 + ETag | security.assignments.manage；If-Match；同事务替换角色/范围并撤销会话 |

fabIds 在单厂部署中只允许空集合或包含 DeploymentFabId 的集合；不能授予数据库中任意其它厂。员工已绑定时移除其所属 Fab 范围需先停用/解绑或同时通过完整授权用例处理，不能产生有现场角色却指向越权员工的状态。

建号 POST 不自动重试；用户名唯一约束避免重复。响应丢失后按用户名查询配置结果，若不能确定临时密码状态则显式重置。密码端点不保存请求哈希/响应回执；其他创建接口可利用唯一编码核对，首期不强制通用幂等回执。

## 5. 角色、权限与审计

| 方法与路径 | 请求/响应 | 规则 |
| --- | --- | --- |
| `GET /security/permissions` | 200：按模块分组的已交付权限定义 | security.roles.read；不提供权限创建/删除接口 |
| `GET /security/roles` | 分页角色及状态 | security.roles.read |
| `GET /security/roles/{id}` | 角色、permissionCodes、成员统计、ETag | security.roles.read |
| `POST /security/roles` | name、permissionCodes；201 + ETag | security.roles.manage；角色名规范化唯一，不允许冒用保留系统名；禁止安全管理权限授予普通角色 |
| `PATCH /security/roles/{id}` | name；200 + ETag | security.roles.manage、If-Match；系统角色拒绝；普通角色更名推进版本并撤销成员会话 |
| `PUT /security/roles/{id}/permissions` | 完整 permissionCodes、reason | security.roles.manage、If-Match；200 + ETag；原子替换并撤销受影响会话 |
| `POST /security/roles/{id}/activate` | reason | security.roles.manage、If-Match；200 + ETag |
| `POST /security/roles/{id}/deactivate` | reason | 同上；系统角色拒绝；普通角色停用撤销成员会话 |
| `GET /security/audit-logs` | 分页，actorId/action/entityType/entityId/from/to | security.audit.read；只提供 OrganizationIdentityAccessControl 白名单对象审计，不泄露其它域审计 |
| `GET /security/events` | 分页，eventType/outcome/from/to/userId | security.audit.read；受限安全事件，不回显原始凭据/请求 |

审计查询默认最近 7 天，单次时间范围建议最大 90 天；查询历史更早记录仍可按较小时间窗翻查，查询窗口不等于留存期限。禁止前端下载整张用户或审计表；若未来增加导出，应定义任务与专门权限。

所有敏感管理动作要求 reason（去空白后 1～500 字符），只保存必要差异。角色/权限集合替换中出现未知、停用或未实现权限时整体拒绝，不悄悄丢弃部分项。

## 6. 关键用例验收

具体执行证据与未验证项见 [实现记录](organization-identity-access-control-implementation.md#6-验证结果与边界)。本表保持需求验收场景，不将每个条目一概标为通过。既有 17 项数据库测试结果不证明以下业务场景已通过。测试使用隔离真实 PostgreSQL，跨范围用例即使部署单厂，也应构造第二厂恶意数据检验拒绝路径；这不表示已交付多厂能力。

| 验收 ID | 需求 | 操作/条件 | 预期结果 |
| --- | --- | --- | --- |
| AC01 | OrganizationStructure | 管理员创建区域、产线、位置并刷新 | 层级准确；Location 位于 Area 下；实际数据持久化 |
| AC02 | OrganizationStructure | 同作用域重复编码、不同区域相同 Line 编码 | 同范围 409；不同合法范围可创建；大小写规范化不能绕过唯一约束 |
| AC03 | OrganizationStructure | 创建到停用父节点；停用有有效子对象的区域；并发新增子项与停用父项 | 409 或按锁顺序有一方完成后另一方被拒；不存在停用父项下新增有效子项，无部分更新 |
| AC04 | OrganizationStructure | 停用无未结依赖的位置后查询历史；启用但父区域停用 | 历史可读且标停用；新绑定拒绝；重新启用拒绝 |
| AC05 | OrganizationStructure | UTC 事件跨本地午夜；非法时区及普通 API 改时区 | 日期筛选与 Fab 时区一致；非法/不允许变更被拒绝 |
| AC06 | OrganizationStructure/04 | 两人使用同一 ETag 修改名称 | 一人成功；另一人 412；不覆盖成功结果 |
| AC07 | WorkforceAndTerminals | 工位绑定不同 Area 的 Line；终端绑定其它 Fab 的工位 | 服务端拒绝；数据库加固约束同步验证 |
| AC08 | WorkforceAndTerminals | 登记无工位终端；越权查询；重复 UUID/厂内编码 | 有明确 Fab 范围；越权 404；重复 409 |
| AC09 | WorkforceAndTerminals | 两个账号绑定同一员工；跨厂绑定员工 | 拒绝，数据库唯一约束和服务端校验均有效 |
| AC10 | WorkforceAndTerminals/03 | 停用员工，再启用员工；仅有员工维护权限时停用有关联账号的员工 | 关联账号及会话原子停用/撤销；启用员工不自动启用账号；缺少安全管理权限的请求被拒绝 |
| AC11 | WorkforceAndTerminals | 终端停用/换绑或工位产线变更，旧 Cookie 再访问 | 受影响会话无效；旧审计场所不变；手工 terminalId 不绕过登录 |
| AC12 | AccountsAndSessions | 管理员建号，临时密码登录后直接调用业务 API | 仅允许受限白名单；业务读写均 403，而非只靠页面跳转 |
| AC13 | AccountsAndSessions | 首登改密成功、响应丢失后分别尝试新旧密码 | 旧密码及旧会话无效；新密码可登录，ForcePasswordChange 已清除 |
| AC14 | AccountsAndSessions | 连续错误 5 次、锁定期登录、到期或管理员解锁 | 计数及期限正确；匿名提示一致；解锁不改变 IsActive |
| AC15 | AccountsAndSessions | 已登录期间攻击者触发自动锁定；管理员再手动锁定 | 自动锁定只阻止新登录；手动锁定撤销已有会话 |
| AC16 | AccountsAndSessions | 退出当前会话后重放复制的旧 Cookie；同账号另一个会话 | 已退出会话失效；另一会话按原状态有效；全部退出时全部失效 |
| AC17 | AccountsAndSessions | 重置密码、停用账号后旧 Cookie 请求；随后只启用账号 | 下一次授权检查拒绝旧会话；启用不恢复旧会话或清除改密要求 |
| AC18 | AccountsAndSessions | 空闲/绝对超时、受限超时、后台轮询 | 按测试时钟到期；后台轮询不无限续期；超时需要登录 |
| AC19 | AccountsAndSessions | 首次登录缺防伪头、登录后跨站写、修改 localStorage 模拟管理员 | 写请求拒绝；客户端状态不能增加服务端权限 |
| AC20 | AccountsAndSessions | 未知用户/错密码/停用/锁定；超限登录 | 对匿名响应不泄露账户状态；限流返回 429；事件记录无密码 |
| AC21 | RolesAndAuthorization | Viewer 直接调用组织修改、账号列表及导出猜测路径 | 写入与人员/安全读取拒绝；不存在未授权导出后门 |
| AC22 | RolesAndAuthorization | 用户有动作权限无 Fab 范围；请求伪造 FabId 或子对象 ID | 列表/计数/详情/写入均不泄露越权数据；关联校验同样拒绝 |
| AC23 | RolesAndAuthorization | 删除角色权限、停用角色、移除用户角色/范围后调用旧请求 | 下一次检查使用新权限或拒绝旧会话；角色 Cookie 内容不能维持旧授权 |
| AC24 | RolesAndAuthorization | 插入 UserPermission、UserClaim 或提交 `*` 权限 | 不能获得额外动作；UI 不提供个人直接授权；未知权限整体拒绝 |
| AC25 | RolesAndAuthorization | 普通自定义角色请求安全管理权限；受保护角色被编辑 | 服务端拒绝，无部分角色关系变化 |
| AC26 | RolesAndAuthorization | 两管理员并发移除/锁定对方；员工停用关联最后管理员 | 管理锁及不变量保证至少一名可用管理员；隐蔽路径也被拦截 |
| AC27 | RolesAndAuthorization | 普通角色多成员，修改角色权限时模拟事务中途失败 | 权限、成员会话/安全戳及审计全部回滚；不存在部分撤权 |
| AC28 | 组织结构、人员与场所、账号会话及角色权限 | 审计落库失败、唯一冲突、授权集合版本冲突 | 业务写入整体回滚；错误码准确，不返回假成功 |
| AC29 | 组织结构、人员与场所、账号会话及角色权限 | 请求 ID 贯穿 API、审计、安全事件 | 可关联定位；审计中无密码、Cookie、哈希、安全戳及全量请求体 |
| AC30 | AccountsAndSessions/04 | 数据库断开时携带仍未过期 Cookie 请求 | 认证失败关闭访问，返回可解释暂不可用；不信任过期缓存 |
| AC31 | OrganizationIdentityAccessControl/F14 | 空环境迁移并引导；已有管理员时再次引导 | 初始角色/Fab/账号正确；不重复或覆盖人工授权；无固定默认密码 |
| AC32 | OrganizationIdentityAccessControl/F14 | 容器重启；持久化密钥与会话；恢复演练 | 未到期会话可验证，已撤销会话不复活；恢复流程明确恢复点后会话统一撤销策略 |

## 7. 开发验证分层

| 层次 | 必验内容 | 交付证据 |
| --- | --- | --- |
| 领域/应用 | 归属、有效性、权限并集、受限白名单、管理员保护、时区与超时 | 用例测试与明确规则代码；测试不依赖真实等待 |
| 真实 PostgreSQL | 迁移前数据检查、唯一/外键、xmin/Identity 并发、授权集合事务、审计失败回滚、管理锁竞争 | 隔离测试库结果；不得用内存数据库代替 |
| HTTP 集成 | Cookie、防伪、401/403/404、ETag、凭据字段脱敏、角色/厂区越权 | 使用真实认证及数据库的 API 测试 |
| 浏览器端到端 | 管理员建号 → 首登改密 → 角色工作台 → 组织维护 → 越权拒绝 → 撤权退出 | Playwright 或等价流程证据，涵盖加载/空数据/冲突/失效 |
| Docker | 空环境启动、迁移/初始化、密钥与会话持久化、重启和恢复 | 可重复部署说明、镜像/依赖版本及验收记录 |

建议试点夹具：一个部署 Fab、两个 Area、一条 Line、两个 Location、两个 Worker、一个 Workstation、两个 Terminal（一个未绑定）、两个管理员、一个操作员与一个 Viewer。第二 Fab 仅用于负向隔离测试，不出现在正常管理 UI。

OrganizationIdentityAccessControl 完成记录须逐项标注通过/失败/不适用及证据，不能仅写“页面可打开”。ShiftsAndHandovers/06 不进入本轮通过统计；迁移、API、UI 和初始化流程都完成后再更新 MEMORY 中的实现状态。
