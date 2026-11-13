# MESagent 项目记忆

更新：2026-11-13（Asia/Shanghai）｜阶段：F5 首版及 F3 真实进度/F2 执行绑定完成，完整 F6/F7 待后续｜版本：v1.0

## 2026-11-13 F1～F5 本地演示数据

- 用户授权在当前本地环境建立演示数据，并明确要求命令批量写入；已复用现有业务服务与有效管理员会话，保留既有管理员/凭据。统一 `DEMO-` 标识，演示账号使用 `demo.*`。
- 已建立组织/人员/终端、4 角色与4账号、2 产品/三站路线/Recipe 与版本/点检/PDF、5 台设备、8 工单、11 批次、34 片、3 载具及13次真实 Run；含拆2+4合回、待投放/待加工/加工中/完工/失败/中止/取消状态。
- 逐片核对：首次投放28、完工14、在制14（含等待处置4）、报废0。Chrome 已验证显示；当前浏览器停在生产管理。展示入口、对照及限制见 [演示说明](docs/development-process/09-f1-f5-demo-data.md)。临时工具/记录/凭据在 Git 忽略的 `.local/demo-seed/`，密钥不入文档。
- 待修正：F4 批次详情查询/UI 仍保留完工量 null/“待执行模块接通”占位；真实完工可在 F3 执行概况/F5查看，本轮只建立数据未修改应用代码。下一步按授权修复此显示遗漏，并继续 F6/F7。

## 2026-11-13 F5 实现阶段

- 用户确认本轮 F5-01～05 完整首版：功能文档、后端、WebUI 与测试；补最小设备台账、人工状态及占用校验，完整 F7 登记 GAP-12 后续开发。
- 用户确认 Failed/中止/加工中 Hold 可受控关闭当前 Run，批次进入等待处置，禁止启动下一步；F6 处置与返工保留待集成（GAP-13/14）。Docker 继续后置。
- [F5 设计](docs/feature-development/dispatch-and-production-execution.md)、接口验收与[实现记录](docs/feature-development/dispatch-and-production-execution-implementation.md)已编写。独立 ExecutionBackend 复用认证/厂区授权、单事务和全局写锁，普通只读使用一致性快照。
- 已实现派工/指派、设备/Recipe/片集门禁、Run/Track、类型化点检输出/PDF/确认、逐片退出及中止；真实终点调用 F4，F3 指标/缓存重算/关闭门禁及 F2 执行绑定已回补，完整 F6 口径继续标缺。
- 第 7 条迁移已应用，表数保持 126；迁移前备份在 Git 忽略目录，未写本地生产演示数据。使用 `provision-execution` 只补执行/最小设备表 INSERT/UPDATE，未改账号密码、未增加 DDL/DELETE；DDL/字典/OpenAPI 已同步。
- 验证：17 项数据库 + 57 项 API/协作测试通过，共 74 项；后端构建零警告、Web 构建/格式检查通过。Chrome 已通过 F1～F5 全流程，包含设备台账/人工状态、5 片 3 站及工单实际完工量。完整 F7、F6 处置/返工见 GAP-12～14；GAP-01/08/09 保留尚缺质量/存量专项。

## 已确认

- 建设前后端完备的 MES + Agent + WebUI 系统，后端使用 C#，以 Docker 打包交付。
- 面向**半导体制造：晶圆、批次与设备追踪**。
- 2026-11-11 用户明确要求按 Fab 字段清单与文档技术栈建立数据库，已启动数据库开发；随后授权完成架构初始化与 F1-`OrganizationIdentityAccessControl` 业务 API/WebUI，Agent 保持预留。
- 沟通偏好：先总结重点，具体细节追问后展开。
- 代码格式：优先可读性，具体规则见下方[代码格式规范](#代码格式规范)；本轮按该规范统一仓库内手写代码格式，只调整空白，不改代码内容。
- 2026-11-11 确认本轮编写实现架构与 F1-`OrganizationIdentityAccessControl` 文档：单企业单厂落地并保留厂区校验；组织结构、人员与场所、账号会话及角色权限 完整设计，05/06 仅扩展边界；Agent 仅预留位置，不展开细节。
- F1-`OrganizationIdentityAccessControl` 采用本地账号密码、同源 Cookie、管理员建号/重置、首登改密，不开放自行注册；角色授权与厂区范围，暂不开放个人直接授权。
- 2026-11-11 用户明确要求 Docker 打包后置；本轮完成代码、本地开发与测试，不把部署草案计为正式交付。

## 代码格式规范

适用范围：`src/`、`tests/`、`web/src/`、`web/tests/`、`scripts/` 及根目录手写配置；`docs/` 为参考资料不纳入。生成物（`src/MESagent.Infrastructure/Persistence/Migrations/**` 含 Designer/Snapshot、`web/src/generated/**`、`*.lock.json`、`*.sln`）由工具生成，不手工格式化。

格式调整只允许改空白字符：不增删任何字符，不改标识符、字面量、运算符、括号、语句顺序、`using`/`import` 与注释文字。

### 通用规则

- 文件：UTF-8（无 BOM）、LF 换行、文件末尾恰好一个换行、无行尾空格；空格缩进，禁用 Tab。
- 缩进：C#/TS/TSX/CSS/Python/Shell/HTML 每级 4 空格；JSON/YAML/XML 每级 2 空格。
- 行宽：C# ≤120；TS/TSX/CSS ≤80（Prettier 默认）；Python ≤100。注释、字符串字面量、URL、SQL 等不可拆分内容可超宽。
- 每行只写一条语句或一个声明，禁止 `a(); b();` 式并排语句。
- 控制流（`if`/`else`/`for`/`foreach`/`while`/`do`/`try`/`catch`/`finally`/`switch`）的语句体换行并缩进；不新增或删除花括号。
- 空行：文件内连续空行不超过 1 行；逻辑相关语句成组，组与组之间空一行；Python 顶层定义之间空 2 行。
- 注释 `//`、`///` 后保留一个空格；注释文字不改写。
- 复杂条件、链式调用、对象/集合初始化器和回调按逻辑分行展开，续行缩进 4 空格；方法参数超过 3 个或单行超宽时每行一个。
- 集合/数组字面量：一行放得下时保持一行；超宽或展开为多行时每行一个元素。

### C#

- `using` 组之后、file-scoped `namespace` 声明之后各空一行。
- 类型成员（方法、嵌套类型、字段/自动属性组）之间空一行；连续的单行字段/自动属性可作为一个字段组。
- 方法体内部按校验、查询、状态变更、返回等逻辑段空一行。
- 超过 120 字符的表达式在运算符、逗号或 `.` 前换行；EF 链式配置按 `.` 断行；不得拆分字符串字面量或插值字符串。
- 关键字 `if (`、`for (`、`while (`、`switch (`、`catch (` 与括号之间一个空格；方法调用名与 `(` 之间不留空格；逗号后一个空格、逗号前不留空格；二元运算符两侧各一个空格（由 `dotnet format whitespace` 保证）。

### TypeScript / TSX

- 遵循 Prettier：`printWidth 80`、`tabWidth 4`（由 `web/.prettierrc.json` 固定，`.editorconfig` 同步）、双引号、分号、尾随逗号。
- `import` 组之后空一行；顶层组件/函数/类型之间空一行；组件内 hooks 组与 `return` 之间空一行；多属性 JSX 每行一个属性。

### Python

- PEP 8 风格，4 空格缩进；`import` 组后空一行；顶层函数/类之间空 2 行；函数内逻辑段空一行；`if __name__ == "__main__":` 前空 2 行。
- 不使用 black 自动改写（会增删括号、改引号），按本规范人工调整；原有行尾冗余分号属于非空白字符，按“不增删字符”约束保留，不单独删除。

### 校验方式

- C#：`dotnet format whitespace` 只做空白规范化；空行与断行按上述规则人工补齐。
- Web：`npx prettier --check "src/**/*.{ts,tsx,css}"`。
- 内容不变验证：格式化前后逐文件删除全部空白字符后内容必须完全一致；C# 用词法 token 序列、TS 用 AST、Python 用 `tokenize` 与基线逐项比对；再执行 `dotnet build`、`python3 scripts/app.py test`、`npm run build`。

## 当前事实

- 保留原控制台；已有 Domain/Infrastructure/Database，并新增 Application、ASP.NET Core API、React WebUI 和 API 测试；SDK 仍为 `10.0.401`。
- 已建立 PostgreSQL 18.6 数据库：119 张业务/支撑表、7 张 Identity 表、2 个派生视图及 EF 迁移历史表；7 条迁移已应用，字段字典与 SQL 已同步导出。
- 本地 Docker 服务 `mesagent-postgres-1`，数据库 `mesagent`，仅绑定 `127.0.0.1:55432`，使用独立卷；密码在 Git 忽略的 `.env`，不记入记忆或仓库。未修改 OpenMES 参考服务。
- 已验证外键、数量/时间、归属/槽位互斥、幂等键、xmin 并发、历史与发布版本保护、事务及迁移回滚；具体操作与验证见数据库实施说明。
- 已实现组织/身份/权限、F2 工艺、F3 工单与真实片进度、F4 建批/投放/拆合批/载具、F5 首版派工过站及真实终点；F1 扩展、完整质量报废/处置、完整设备资源、Agent 尚待后续，见阶段记录。
- `docs/` 既有内容是 OpenMES 的学习和开发参考资料，其 Laravel/React 实现不属于 MESagent。
- 已只读核查本地 OpenMES v0.25.0 后台并验证工单到成品批次的下钻；[沿用建议](docs/development-process/01-current-state.md#4-本地-openmes-后台核查与沿用建议) 优先参考看板、追溯、异常和审计界面，晶圆执行规则由 C# 业务层设计，待确认。
- 已初始化 Git 并推送公开仓库 [SoMiReMiReDo/AI-MES](https://github.com/SoMiReMiReDo/AI-MES)：`main` 为默认分支，`dev/mes-backend` 为后端开发分支。
- [Fab 字段保留清单](docs/development-process/04-fab-data-field-retention.md) 已作为数据库基础；按需对象先建结构，工艺细节仍待场景收敛。
- F1-`OrganizationIdentityAccessControl` 第 4 条迁移补齐终端厂区、员工唯一绑定、角色状态、会话/安全事件及关联约束；已初始化本地管理员，凭据仅保存在 Git 忽略的本地文件，首登须改密。
- 验证：17 项数据库测试 + 17 项 HTTP 集成测试通过，前端构建通过；Chrome 完整流程通过一次。页面拆分后的最终浏览器复测被自动审批额度上限阻止，需后续补跑；Docker/恢复演练延期。
- 2026-11-11 已按代码格式规范统一手写代码格式：47 个文件、净增 2335 行（展开压缩语句），手写 C# 6374→8455 行；改动经去空白校验与 C# token / TS AST / Python token 级比对，均为纯空白差异；`dotnet build` 0 警告、34 项测试通过、Web 构建与 Prettier 检查通过。EF 迁移等生成物与 `docs/` 未改动。

## 技术选择与建议

- 数据库已采用 EF Core / Identity 10.0.12 + Npgsql EF Provider 10.0.3，集中锁版本；ASP.NET Core 10 Web API 与模块化单体已开始落地。
- 前端：React 19 + TypeScript + Vite + Ant Design 6；React Router、TanStack Query；SignalR 留待生产页面阶段。
- Agent：历史建议为优先评估 Microsoft Agent Framework（.NET）；本轮功能设计仅保留扩展位置，暂不展开。
- 数据：PostgreSQL 18.6 已运行，Docker 镜像固定摘要；pgvector、独立向量库或消息中间件尚未引入。
- 交付：Docker 多阶段构建 + Compose 为后续方案；应用部署文件暂为草案。现阶段使用本地 API/Vite 与既有 PostgreSQL。
- 业务起点：Lot/Wafer、Route/Recipe 版本、Track-in/Track-out、Hold/Release、受限拆批合批、设备履历与追溯。

## 待明确

- 晶圆制造或封测的具体工艺、试点产线与角色分工；暂以晶圆制造演示场景设计，不推断完整 Fab 需求。
- 单厂并发规模、可用性与备份恢复目标；暂按单企业单厂试点规划。
- 模型使用云端还是本地、可出域数据范围、预算与模型评测样例。
- 实际设备协议和 ERP 接口；SECS/GEM 联机、设备指令下发均不在首版演示范围。

## 文档入口与下一步

- [数据架构开发文档](docs/MESagent-development-guide.md)：当前分层、数据关系、开发约定、迁移与测试入口。
- 开发过程目录已由 `docs/planning` 改名为 `docs/development-process`，原六份文档内容不变；外部引用及字段字典生成路径已更新。

- [现状与差距](docs/development-process/01-current-state.md)
- [半导体系统规划](docs/development-process/02-system-plan.md)
- [技术选型 v0.1](docs/development-process/03-tech-stack.md)
- [Fab 数据字段沿用与保留](docs/development-process/04-fab-data-field-retention.md)
- [数据库实际字段字典](docs/development-process/05-database-catalog.md)
- [数据库实施与操作](docs/development-process/06-database-implementation.md)
- [Fab 全功能清单](docs/development-process/07-fab-function-catalog.md)：基于最初 114 个业务实体整理 14 个模块、87 项功能；当前 F1/F2/F3 及 F4 基础能力已实现，完整生产执行尚未接通。
- [功能实现架构](docs/development-process/08-feature-implementation-architecture.md)：模块职责、共享应用层、事务/授权、WebUI 与交付顺序。
- [F1-`OrganizationIdentityAccessControl` 功能设计](docs/feature-development/organization-identity-access-control.md) · [接口与验收](docs/feature-development/organization-identity-access-control-api-and-acceptance.md)：设计规则与 32 项验收场景；执行证据见 [F1-`OrganizationIdentityAccessControl` 实现与启动](docs/feature-development/organization-identity-access-control-implementation.md)。

下一步：按后续授权推进 F6 真实质量处置/报废/恢复及 GAP-12 完整 F7；F3 实际进度、F2 执行绑定和 F4 终点已随 F5 接通，质量口径/部分报废联调与复杂存量专项仍保留。Docker 继续后置；新任务开始前先沟通计划。

## 决策记录

| 日期 | 决定 | 状态 |
| --- | --- | --- |
| 2026-11-10 | C# + MES + Agent + WebUI + Docker | 用户确认 |
| 2026-11-10 | 聚焦半导体晶圆、批次与设备追踪 | 用户确认 |
| 2026-11-10 | 当前只规划，不开发 | 用户确认 |
| 2026-11-10 | .NET/React/Agent Framework/PostgreSQL 方案 | v0.1 建议，待验证 |
| 2026-11-11 | 公开仓库 AI-MES，`main` + `dev/mes-backend` 分支 | 用户确认 |
| 2026-11-11 | 按 Fab 清单与技术栈启动数据库开发 | 用户确认；数据库已建立并验证 |
| 2026-11-11 | 单厂 组织结构、人员与场所、账号会话及角色权限 完整设计，05/06 扩展；本地账号/Cookie、角色及厂区授权，Agent 仅预留 | 用户确认范围；设计文档已编写，业务待实现 |
| 2026-11-11 | 代码格式规范写入 MEMORY.md，按规范统一手写代码格式（C# 120 / TS 80 / Python 100，仅空白不改内容） | 用户确认并已执行 |

| 2026-11-11 | 实施架构初始化与 组织结构、人员与场所、账号会话及角色权限；Docker 打包后置 | 用户授权；代码及本地测试完成，部署未验收 |

- 2026-11-11 命名约定：使用具体能力英文 `OrganizationIdentityAccessControl`（文件使用 kebab-case），不以功能编号命名代码；已应用迁移与密钥用途标识保持兼容。命名调整已通过前后端构建及文档链接检查。

- 2026-11-11 文档命名修订：模块使用编号加英文能力名，如 F1-`OrganizationIdentityAccessControl`、F2-`ProductProcessVersionManagement`；代码类型与目录继续使用英文能力名，历史迁移标识保持兼容。

## 2026-11-11 F2/F3 文档阶段

- 用户确认先完成任务一：F2-01～05、F3-01～02 及初始下达基线的能力/接口验收文档，已写入 `docs/feature-development/`，当时仅完成文档；后续实现见下方实现阶段记录。
- 新增 [功能空缺与待补全清单](docs/feature-development/deferred-capabilities.md)，记录 F2-06、F3-03/04/05 等预留项。
- F3 真实生产进度须在 F4/F5 完成后返回补全（GAP-01）；当前 Wafer 无直接工单归属，不能凭当前 Lot 简单统计，未知指标应明确标缺。
- 用户随后确认任务二，要求沿用原有架构继续开发；遇到问题及时沟通。Docker 仍后置。

## 2026-11-11 F2/F3 实现阶段

- 已沿用现有架构实现 F2-01～05、F3-01～02 查询契约及初始下达快照：产品/路线/Recipe、发布冻结与复制、编号、PDF 指导书、工单生命周期和 WebUI；复用会话、厂区权限、审计、xmin 与事务锁。
- 第 5 条迁移补充新权限、既有管理员授权与工单快照不可变保护；本地迁移及运行角色授权已应用。PDF 持久目录为 `.local/engineering-documents`（Git 忽略），未改管理员凭据。
- 最终验证：17 项数据库测试 + 25 项 HTTP 测试通过，包含并发编号和嵌套路线保存回滚；Chrome 完整流程通过，已补跑此前 F1 浏览器回归；Web 构建通过，仍有包体积提示。
- F3 未实现真实完工统计：未知指标返回 null，带生产关联的关闭继续拒绝；F2-06、F3-03/04/05 等仍按空缺清单预留。测试通过不代表完整 Fab 流程或 Docker 验收。
- [实现与本地验收](docs/feature-development/f2-f3-implementation.md) 记录代码路径、接口细化、迁移与运行限制；后续开发仍先沟通路径和计划。

## 2026-11-11 F4 文档阶段

- 用户确认本轮仅架构与功能文档：F4-01～06 完整设计，F4-07/无工单工程批预留；建批必须关联已下达工单；拆合批限非加工中且无有效 Hold，合批同厂、同工单、同产品修订、同路线版本和同工艺位置。
- 已更新 [实现架构 v0.2](docs/development-process/08-feature-implementation-architecture.md)，新增 [F4 功能设计](docs/feature-development/lot-wafer-carrier-genealogy.md)与[接口及 42 项验收场景](docs/feature-development/lot-wafer-carrier-genealogy-api-and-acceptance.md)；本轮无代码/数据库变更，验收场景尚未执行。
- 方案沿用现有模块化单体、共享事务及全局写锁；需补稳定 Wafer 工单/厂区、首次投放、终点完成及步骤继承事实。现有归属数量不等于 WIP；拆合批不重复投产，合批耗尽不计完工。
- 更细的状态、仅 Ready 拆合批、投放限额、载具规格及请求上限属于设计方案，非现场已确认政策。GAP-08～11 记录数据/收尾待实现及特殊批种/更正预留；GAP-01/06 仍未完成。

## 2026-11-12 F4 实现阶段

- 用户授权判断架构并开发 F4；仅微调为独立 ProductionBackend、类型化请求、共享领域规则，保留现有认证、单 Context/事务和全局写锁。
- F4-01～05 首版 API/WebUI 已完成：工单建批、固定基线投放、逐片归属、载具装卸/换槽/移位、受限拆合批和血缘。F4-06 取消/合批耗尽及内部完工/报废收尾能力已实现；F5/F6 用户流程尚未接入，不能称完整生产闭环。
- 第 6 条迁移已应用，新增首次投放、终点完成及步骤继承表；本地迁移前生产表为空，备份留在 Git 忽略目录。运行账号权限已配置，serve 改用 mesagent_app；无凭据变更。字段字典、DDL、OpenAPI 类型已同步。
- 验证：后端构建 0 警告/错误；17 项数据库 + 39 项 API/协作测试通过（F4 新增 14 项，含最小权限角色）；Chrome 完成建批→投放→2+3 拆合→装载→履历及原有模块回归；Web 构建通过，仍有包体积提示。
- 详见 [F4 实现与验收](docs/feature-development/lot-wafer-carrier-genealogy-implementation.md)。GAP-08 结构与内部能力落地但真实终点/重算仍待验收；GAP-09、GAP-01/06 未关闭。工程批、复杂更正及 Docker 继续后置。
