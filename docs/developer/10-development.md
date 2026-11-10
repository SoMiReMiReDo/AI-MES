# 开发、验证与问题定位

用现有测试约束修改范围；区分文档站验证、业务测试和生产环境验证。

## 使用逻辑

### 用一个改动串起开发和验收

假设需求是「工序还有待处理量时，完工错误消息要显示剩余数量」。先在隔离数据里准备两道工序、transfer 模式和能操作该产线的账号。完成上游并在下游只报一部分。点击完工，记录工序 ID、当前剩余量、提交内容、返回消息和提交后状态。处理剩余量后再点完工，检查错误消失并完成工序。这样同时拿到一个失败样本和一个成功样本。

| 提交前要回答的问题 | 可复核产物 |
| --- | --- |
| 用户在什么页面、什么状态触发 | 操作顺序、角色、配置和最小数据 |
| 拒绝来自哪个层 | Web flash 或字段错误、API 响应、服务规则 |
| 成功后有哪些状态与数量变化 | 工序、批次和工单的前后值 |
| 别的角色与入口是否受影响 | 对象权限用例以及 Web/API 对照 |
| 是否涉及实际部署能力 | 锁、WebSocket、设备或 ERP 的环境要求 |

准备可复用数据时，按产品、工单、批次和工序记录标识，避免只保存截图。对支持不同流转模式的逻辑，记录测试时启用的模式和设置。

## 技术逻辑

### 本地开发

从 OpenMES 的 backend 目录准备环境与依赖：

```bash
composer install
npm ci
# 按 .env.example 配置本地数据库与应用环境
php artisan key:generate
php artisan migrate
npm run build
composer run dev
```

上述命令是操作说明，本次没有执行数据库迁移或启动 OpenMES。composer run dev 启动 Laravel、队列监听、日志与 Vite。容器部署从仓库根使用 docker compose up -d；环境变量与端口以当前 Compose 为准。

### 按变化选择测试

| 修改范围 | 首先验证 |
| --- | --- |
| 工序规则 | Feature/Api/BatchStepTest、Unit/Services/BatchServiceTest |
| transfer 与数量 | Feature/Web/Operator/StepQuantityFlowTest |
| 设备计数 | Feature/Machine/MachineCounterTest |
| 仓储过账 | Feature/Warehouse/StockDocumentServiceTest |
| ERP 导入 | Feature/Api/V1/Erp 下对应测试 |
| 实时同步 | Feature/Sync 下投影、提交后广播与权限测试 |
| 前端逻辑 | Vitest colocated 测试；需要浏览器行为时再跑 E2E |

```bash
# backend 目录；Inertia feature tests 前先构建资源
npm run build
php artisan test --filter=BatchStepTest
npm test
# 仓库根目录；先有正在运行的隔离测试实例
E2E_BASE_URL=http://localhost:8080 npx playwright test
```

根目录 npm test 是占位失败命令。phpunit.xml 默认 SQLite :memory:、sync 队列、null 广播；它不能充分证明 PostgreSQL 行锁、长驻 worker 和实际 WebSocket 的正确性。并发问题需要补 PostgreSQL 场景验证。Playwright 配置为单 worker。

### 故障定位表

| 现象 | 优先核对 |
| --- | --- |
| 不能开工 | 工单计划时间 / hold、前序、工位、阻塞质检、缺料开关 |
| 工单数量偏大 | 中间工序是否误当成品、设备重复事件、绑定与基线 |
| 完批但未完单 | 计划短缺、报废、其他进行中批次、阻塞状态 |
| 现场页面没更新 | 快照 HTTP 状态、session、频道授权、Reverb、提交后事件 |
| 完工但没有入库 | 仓储开关、自动草稿条件、生成日志、单据是否过账 |
| 模块直接刷新白屏 | moduleAssets、页面注册顺序、provider 与构建产物 |

### 文档维护流程

编辑 developer/*.md，然后运行 python3 tools/build_portal.py。生成站点是派生产物，应优先修改 Markdown。源码更新后重新核对相关证据和 baseline.json；新基线不是只替换日期。

本次验证记录见 VALIDATION.md。原学习课程与重建里程碑继续独立保留，不作为本次开发文档中心的强制阅读顺序。

### 让每类测试覆盖一个风险

PHPUnit Feature 用于校验请求、角色和数据库结果，Unit 可定位领域计算及分支。前端测试适合验证状态映射和错误显示。Playwright 的现场意义是验证浏览器路径和操作反馈。例如跨窗口实时列表用例依赖运行中的应用，其用例还通过 Docker 查询和更改测试记录，复跑前需要提供相应容器和隔离数据库。

项目 CI 以 PHP 8.3 和 Node 20 安装依赖、构建资源、执行前端测试，再准备应用密钥并跑 Laravel 测试。复核失败时先确认失败发生在构建、环境引导还是断言阶段。涉及 PostgreSQL 行锁时，在对应数据库中用两个独立连接构造竞争，再断言最终状态与写入次数。

### 接手故障时收集可对照的上下文

按「用户和产线 → 工单/批次/工序 → 操作和时间 → 页面/接口响应 → 提交后的状态」记录证据。设备和 ERP 问题额外记录通道 ID、事件 ID 或外部单据号。复现后先运行覆盖该分支的测试，再跑受影响的相邻入口。

## 环境初始化的可复现步骤

以下命令面向 OpenMES 源码目录，而不是 MESagent。先检查 PHP、Composer、Node 与 npm 已安装，并使用锁文件安装依赖。PHP 约束及 Composer platform 见第 01 章；精确依赖以当前 lock 文件为准。

```bash
cd /Users/seal/Desktop/myLog/program/SKhynix/OpenMes/backend
php --version
composer --version
node --version
npm --version
composer install
npm ci
# 仅首次创建本地配置，不覆盖已有 .env
[ -f .env ] || cp .env.example .env
```

在继续前编辑本地 .env。示例 DB_HOST=postgres、REVERB_HOST=reverb 是容器网络中的服务名，直接在宿主机运行时应改为实际可访问地址。设置数据库连接、APP_URL、会话、广播和浏览器连接参数，确认它们属于本地隔离实例。

```bash
# 首次初始化才生成 APP_KEY，已有实例保留原密钥
php artisan key:generate
php artisan migrate
npm run build
composer run dev
```

composer run dev 包含应用、队列监听、日志与 Vite，不包含 Reverb 服务。需要实时同步时，在匹配配置的另一个终端运行 php artisan reverb:start，或使用 Compose 的 reverb 服务。空数据库完成迁移并不等于已具备角色、产品、工艺等实验数据，应按安装流程和第 01 章准备最小数据。

## 容器开发与长驻进程

从 OpenMES 根目录使用基础 Compose 与开发覆盖文件：

```bash
docker compose -f docker-compose.yml -f docker-compose.dev.yml up -d
docker compose -f docker-compose.yml -f docker-compose.dev.yml ps
```

开发覆盖文件挂载 PHP 与前端源码，frontend 服务执行 npm ci 和 npm run watch。后端使用 RoadRunner Octane，修改 PHP 后需要重新加载长驻应用；只刷新浏览器不保证新 PHP 已执行。

```bash
docker compose -f docker-compose.yml -f docker-compose.dev.yml exec backend php artisan octane:reload
docker compose -f docker-compose.yml -f docker-compose.dev.yml restart reverb
```

第二条用于修改广播鉴权或相关 PHP 后重启 Reverb。依赖清单发生变化时还需在对应运行环境重新安装依赖；挂载了 composer.json 不代表镜像里的 vendor 自动同步。端口、数据库和安装初始化按当前 Compose 配置检查，不把本地预览文档的 8765 当作业务端口。

## 分层验证命令

```bash
# OpenMES/backend；运行受影响的业务测试
npm run build
php artisan test --filter=BatchServiceTest --stop-on-failure
php artisan test --filter=StepQuantitySafetyTest --stop-on-failure
npm test
# 针对改动的 PHP 文件执行格式化或检查
./vendor/bin/pint --test app/Services/WorkOrder/BatchService.php
```

Playwright 从 OpenMES 根目录安装和运行，不是 backend。第一次使用需要 npm ci 和 npx playwright install chromium，再提供 E2E_BASE_URL 指向已启动的隔离实例。部分用例通过 Docker 直接修改测试数据，运行前应阅读目标 spec 的准备逻辑。

验收记录应写出命令、运行目录、结果与环境。测试未执行、执行被依赖阻塞、断言失败是三种不同状态，不应统一写成“已验证”。

## 从缺陷报告到修复提交

1. 收集角色、租户、产线、模式、对象 ID、操作时间和响应，缩小到一条可重放的路径。
2. 用 Route → Controller / Request → Service → Model 定位首次偏离预期的位置。
3. 对业务风险补一条能区分正确与错误行为的测试，避免只镜像实现细节。
4. 在领域规则所在地修复，并核对 Web、API、设备或导入的相邻入口。
5. 运行相关测试、前端构建与必要的浏览器场景；数据库并发另在 PostgreSQL 验证。
6. 提交说明描述触发条件、修复后行为、验证与未覆盖的环境，并更新相应章节。

数据库变更另需检查既有数据、空值、索引和回填。涉及长驻进程的配置或 PHP 改动，交付说明应写清需要重新加载的服务。恢复时区分应用代码回退与数据补偿，不能假设回退代码会自动撤销已过账库存或外部 ERP 单据。

## 文档站维护命令与验收

```bash
cd /Users/seal/Desktop/myNote/MESagent/docs
python3 tools/build_portal.py
python3 tools/check_portal.py
node --check portal/assets/app.js
node --check portal/assets/search-data.js
```

Markdown 是唯一正文源稿；不要只编辑 portal 中生成的 HTML。新增章节更新 chapters.json，新增证据同时补源码链接和 sources 清单。构建会刷新来源快照和 baseline，因此构建后仍要复核是否有源码变化导致旧文字失效。

浏览器验收覆盖首页入口、每章目录、长表格横向滚动、代码块、搜索命中、来源行号、上一章/下一章和窄屏导航。使用 HTTP 预览时可在 docs 目录运行 python3 -m http.server 8765 --bind 127.0.0.1，再打开 /portal/。

## 源码依据

- [backend/composer.json:66](/Users/seal/Desktop/myLog/program/SKhynix/OpenMes/backend/composer.json:66)
- [backend/phpunit.xml:26](/Users/seal/Desktop/myLog/program/SKhynix/OpenMes/backend/phpunit.xml:26)
- [playwright.config.ts:8](/Users/seal/Desktop/myLog/program/SKhynix/OpenMes/playwright.config.ts:8)
- [backend/tests/Feature/Warehouse/StockDocumentServiceTest.php:1](/Users/seal/Desktop/myLog/program/SKhynix/OpenMes/backend/tests/Feature/Warehouse/StockDocumentServiceTest.php:1)
- [CI 运行环境与测试顺序](/Users/seal/Desktop/myLog/program/SKhynix/OpenMes/.github/workflows/tests.yml:18)
- [数量和状态边界测试](/Users/seal/Desktop/myLog/program/SKhynix/OpenMes/backend/tests/Feature/Web/Operator/StepQuantitySafetyTest.php:27)
- [实时列表 E2E 的运行依赖](/Users/seal/Desktop/myLog/program/SKhynix/OpenMes/tests/e2e/work-orders-live-list.spec.ts:14)
- [docker-compose.dev.yml:1](/Users/seal/Desktop/myLog/program/SKhynix/OpenMes/docker-compose.dev.yml:1)
- [backend/.env.example:1](/Users/seal/Desktop/myLog/program/SKhynix/OpenMes/backend/.env.example:1)
- [backend/package.json:1](/Users/seal/Desktop/myLog/program/SKhynix/OpenMes/backend/package.json:1)
- [package.json:1](/Users/seal/Desktop/myLog/program/SKhynix/OpenMes/package.json:1)
