# OrganizationIdentityAccessControl 实现与本地开发

更新：2026-11-11｜状态：架构初始化与 组织结构、人员与场所、账号会话及角色权限 首版代码已完成；Docker 打包按用户要求后置。

## 1. 已实现范围

| 范围 | 当前能力 |
| --- | --- |
| 工程基础 | 新建 Application、ASP.NET Core API、React WebUI 和 HTTP 集成测试项目；保留原控制台、Domain、Infrastructure、Database 与数据库测试 |
| OrganizationStructure 组织 | 厂区维护，区域、产线、位置创建/查询/编辑/启停；编码规范化、父级有效性、停用影响检查、历史可读、ETag 冲突保护 |
| WorkforceAndTerminals 人员与场所 | 员工、工位、终端台账；员工与账号唯一关联；工位同区域、终端同厂校验；关联变更和停用撤销受影响会话 |
| AccountsAndSessions 身份 | 本地账号、Cookie、防伪、首登改密、本人资料、改密/重置、自动/手动锁定、启停、会话列表与撤销、初始化及本地管理员恢复 |
| RolesAndAuthorization 授权 | 角色、权限集合、用户角色与厂区范围；受保护管理员、个人直接授权不生效、服务端对象校验、审计与安全事件 |
| WebUI | 登录/改密、工作台、组织树与列表、员工/工位/终端、账号/授权、角色/权限、个人设置及审计查询；桌面与窄屏布局 |

ShiftsAndHandovers 班次交接、FederatedIdentityAndMultiFab 双因素/SSO/多厂保持扩展边界；Agent 仅保留架构位置。生产过站、工艺、质量等其它模块仍只有原有数据基础，不能据此认为已有业务功能。

## 2. 代码入口与依赖

| 入口 | 职责 |
| --- | --- |
| [Application/OrganizationIdentityAccessControl.cs](../../src/MESagent.Application/OrganizationIdentityAccessControl.cs) | 公共应用入口、当前请求身份、用例执行接口、错误类型、权限目录与会话配置 |
| [Application/Contracts.cs](../../src/MESagent.Application/Contracts.cs) | 公共请求/响应契约与组织读模型 |
| [Domain/OrganizationRules.cs](../../src/MESagent.Domain/OrganizationRules.cs) | 编码、文本、时区等无基础设施依赖的校验 |
| [Infrastructure/OrganizationIdentityAccessControl](../../src/MESagent.Infrastructure/OrganizationIdentityAccessControl) | 身份适配、组织及权限用例的持久化实现、数据库事务、审计、会话与范围解析 |
| [API/Program.cs](../../src/MESagent.Api/Program.cs) | 服务组合、Cookie、防伪、限流、错误映射、HTTP 路由、OpenAPI 与本地初始化/恢复入口 |
| [web/src](../../web/src) | 按组织、账号、角色、审计、个人设置等页面拆分；共用请求封装和查询缓存 |
| [API tests](../../tests/MESagent.Api.Tests/OrganizationIdentityAccessControlTests.cs) | 真实认证、HTTP 和 PostgreSQL 集成验证 |

编译依赖：Application → Domain；Infrastructure → Application/Domain；API 在组合入口注册 Infrastructure。API 不直接修改业务实体。当前 OrganizationIdentityAccessControl 的数据库事务编排集中在 Infrastructure 的用例适配实现中，Application 保持无 EF/Identity 依赖的统一入口及契约；以后新增模块继续抽取可独立测试的业务规则，避免将用例搬入控制器。

OrganizationIdentityAccessControl 管理写请求、登录和初始化共用数据库事务级 advisory lock，优先保证管理员保护、授权变更与组织启停的一致性。该锁用于单厂初期的低频管理工作，不作为未来高频生产命令的并发方案。普通查询仍在数据库中先限制范围，再统计和分页；组织审计也按来源对象校验 Fab。

## 3. 数据库变化

第 4 条迁移为 `20261110213648_F01IdentityAndOrganization`，已应用到本地数据库：

- Terminal 增加必填 FabId、厂内编码唯一键；已绑定旧终端可沿工位回填，存在无法归属的旧终端时迁移明确失败。
- MesUser.WorkerId 增加非空唯一索引。
- MesRole 增加 IsActive、IsSystem。
- 增加 `security.user_sessions` 与 `security.security_events`。
- 数据库触发器保护工位/产线同区域、终端/工位同厂关系，安全事件禁止改写或删除。

当前为 **116 张业务/支撑表、7 张 Identity 表、2 个派生视图**，另有迁移历史表。字段字典与 `database/mesagent.sql` 已重新导出。编码格式、父对象启用状态、管理员保护等规则仍由应用用例执行，不把数据库外键当作完整业务授权。

## 4. 本地启动

前置环境：.NET SDK 10.0.401、Node 24、现有本地 PostgreSQL。此处使用已有 PostgreSQL 容器作为开发数据库，不要求进行应用 Docker 打包。

首次准备环境：

```sh
dotnet restore MESagent.sln --locked-mode
dotnet tool restore
npm ci --prefix web
python3 scripts/database.py init
python3 scripts/database.py up
python3 scripts/database.py migrate
python3 scripts/app.py configure
```

仅在尚未初始化管理员的新环境执行：

```sh
python3 scripts/app.py bootstrap
```

命令交互读取用户名及临时密码，不把密码放在命令行参数中。也可执行 `bootstrap-generated` 生成随机临时密码，仅写入 Git 忽略的 `.local/initial-admin.txt`，文件权限为 600。已有管理员时拒绝重复初始化。本次本地开发环境已初始化，无须再次执行；首登须改密，改密后删除初始凭据文件。

分别在两个终端运行：

```sh
python3 scripts/app.py serve
```

```sh
npm run dev --prefix web
```

访问 `http://127.0.0.1:5173`。Vite 将 `/api` 代理到 `127.0.0.1:5080`，形成同源开发入口。开发 Cookie 允许本地 HTTP；生产环境需要 HTTPS 与持久化 Data Protection 密钥，生产部署验证留待后续阶段。

本机管理员失去访问能力时，拥有宿主机与数据库管理权限的人员可以运行：

```sh
python3 scripts/app.py recover-admin
```

该入口仅恢复已有 SystemAdmin，输入新临时密码和原因，撤销全部旧会话并要求改密；没有对应匿名 HTTP 后门。

## 5. 接口契约与开发约定

开发环境 OpenAPI：`http://127.0.0.1:5080/openapi/v1.json`。API 启动后执行：

```sh
npm run generate:api --prefix web
```

类型写入 `web/src/generated/api.d.ts`，前端引用生成的登录、当前用户、组织与分页契约。修改 API 字段后重新生成；资源专用字段限制仍由后端白名单校验，不能只依赖通用组织创建 schema。

- 写请求提交 `X-CSRF-TOKEN`；编辑及状态命令提交 `If-Match`，冲突返回 412，缺少版本返回 428。
- 普通管理事务同时提交业务与审计；审计失败会回滚授权、会话撤销和业务修改。
- 密码请求不进入通用回执或请求体日志；客户端不自动重试写请求。
- 每次请求验证服务端会话及账号状态，权限变更撤销受影响会话。自动密码失败锁定只阻止新登录，管理员锁定同时撤销已有会话。
- 单企业单厂由 `Mes__DeploymentFabId` 明确配置；没有范围不等于全厂可见。
- `GET` 查询只读业务；获取本人信息会按交互策略更新会话活动时间，不改变生产事实。

## 6. 验证结果与边界

| 验证 | 结果 |
| --- | --- |
| .NET 编译 | 已通过，0 警告、0 错误 |
| 既有 PostgreSQL 测试 | 17 项通过，包含迁移回滚/重应用、约束、并发与历史保护 |
| OrganizationIdentityAccessControl HTTP 集成测试 | 17 项通过；真实数据库、Cookie 和防伪；覆盖首登、撤权、跨厂拒绝、员工/终端绑定、锁定、限流、审计事务回滚、审计范围及本地恢复 |
| TypeScript/Vite 构建 | 已通过；当前 UI bundle 较大，后续可按页面继续拆分加载 |
| Chrome 完整流程 | 已通过一次：首登改密 → 区域创建 → 只读账号拒绝越权 → 停用账号后会话失效 → 退出；完成桌面与移动截图检查 |
| 最新版本浏览器复测 | 最后一次复测未执行：自动审批服务额度达到上限。前述 Chrome 结果来自页面文件拆分前，不冒充最终源码完整回归 |
| Docker 应用交付与恢复演练 | 用户明确后置，不计入本轮完成项；部署文件仅保留草案 |

可复现命令：

```sh
python3 scripts/app.py test
npm run build --prefix web
python3 scripts/e2e.py
```

浏览器测试使用本机 Chrome 与随机 `mesagent_test_browser_*` 数据库，结束后清理，不向实际开发库写入验收样本。测试截图及失败 trace 位于 Git 忽略的 `web/test-results/`。

[原 32 项验收场景](organization-identity-access-control-api-and-acceptance.md) 是需求验收清单，不能与 34 个自动测试简单按编号对应。AC31 的应用 Docker 初始化、AC32 的容器重启/完整恢复随交付延期；夏令时、实际现场终端可信度、完整异常网络组合及容量/性能验收仍需现场阶段验证。本地安全事件暂保存用户、会话、动作、结果和请求关联，不采集 IP/UserAgent，留存清理策略尚未启用。

## 7. 下一步

根据后续授权进入 F02 工艺基础及相关业务模块。保持同一服务端权限与事务入口；后续生产模块引用组织对象时必须重新检查有效性和所属厂区。班次、SSO、多厂和 Agent 不随本轮代码完成自动进入实施范围。

命名调整（2026-11-11）：代码模块统一采用 `OrganizationIdentityAccessControl`，文档和浏览器测试文件采用 `organization-identity-access-control`。已应用迁移 ID、数据库约束名称和既有 Cookie Data Protection 用途保持原值，避免影响迁移历史与已签发会话。
