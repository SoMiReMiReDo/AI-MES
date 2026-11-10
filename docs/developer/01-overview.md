# 项目全景与阅读地图

OpenMES 将生产计划转成可执行工序，再把实际产量、物料、质量与设备记录回收到同一业务链。

## 使用逻辑

### 跟一张工单走一遍

以装配线计划生产 100 件为例。管理员准备产线、工位、产品、工艺及操作员权限。主管审核工单并接单，操作员选择产线，在队列打开工单，建一个目标 100 件的批次。第一个工位开始工序，按当前计数和流转设置记录执行结果。下游完成后，主管回到工单详情确认实产与缺口。这个例子可以缩成一件产品、两道工序来复现。

| 角色 | 从哪里进入 | 现场要确认什么 |
| --- | --- | --- |
| 管理员 | 用户、生产结构、产品配置 | 人员可访问正确产线，工艺可用于建单 |
| 主管 | 工单列表、计划看板 | 产品、产线、交期、计划开工时间和接单状态 |
| 操作员 | 选择产线、队列、工单详情 | 当前可执行步骤、已录入数量、待解决提示 |
| 质量或库存人员 | 按启用能力进入质检与仓库功能 | 检验任务、单据状态、实物流转 |
| 集成人员 | API、设备连接及计数通道 | 接口身份、读数归属、实际入账量 |

看到同一个工单号时，先确认界面在展示计划、执行还是统计。主管列表的计划数量、工位界面的待处理量、班次看板的实绩分别回答不同问题。

## 技术逻辑

### 系统边界

这是以 Laravel 为业务中心的多端仓库。Web 主界面通过 Inertia 接入 React，移动端调用 API，桌面端使用 Tauri；这些入口共享服务端领域逻辑。opcua-gateway 是协议边车，不是另一套生产系统。

| 层次 | 当前实现 | 阅读目的 |
| --- | --- | --- |
| 业务与持久化 | Laravel 12、Eloquent、PostgreSQL | 状态、事务、权限、领域服务 |
| Web | React 19、Inertia 3、Vite 7、Tailwind 4 | 页面解析、表单提交、实时集合 |
| 同步 | Reverb、Laravel Echo、TanStack DB | 快照、增量、断线补偿 |
| 多端 | Expo、Tauri、共享 UI 包 | 入口适配与能力复用 |
| 设备 | Modbus、MQTT、OPC UA | 原始读数与生产数量的边界 |

版本来自依赖声明，精确安装版本须看 lock 文件。Composer 的 PHP 约束为 ^8.2，platform 为 8.3.31；Docker Compose 使用 postgres:17-alpine。

### 先沿主链读，再展开旁路

1. WorkOrderService：工单创建、快照、建批。
2. BatchService 与 BatchStep：开工、报数、完工、顺序约束。
3. 两类控制器与 WorkOrderPolicy：谁能通过哪个入口执行。
4. 操作员 React 页面：动作入口及错误展示。
5. 物料、质量、仓库、设备：在主链哪个节点介入。
6. Sync 与模块系统：数据如何传播、功能如何扩展。

### 目录职责

| 目录 | 责任与边界 |
| --- | --- |
| backend/app/Http | Web/API 入口、请求校验、中间件 |
| backend/app/Services | 业务用例、跨模型协调、外部集成 |
| backend/app/Models | 关系、局部状态规则、模型事件；不能只读 Service |
| backend/database | 迁移、工厂、演示数据；模型字段说明不替代数据库约束 |
| backend/resources/js | React 页面、布局、同步客户端、模块加载 |
| backend/app/Sync | 可同步数据集合及广播投影 |
| backend/modules | Laravel 模块；以实际 ModuleManager 配置为准 |
| packages/ui、packages/module | 跨端组件与前端模块契约 |
| mobile、desktop、opcua-gateway | 移动端、桌面外壳、设备边车 |

### 文档可信度

原 docs/development.md 的 Blade/Livewire 主前端说明已落后于 app.jsx 与依赖声明。本体系按当前工作树解读。旧文档适合获取业务背景，实际入口、字段、状态和开关要回到源码确认。阅读源码与测试不等于已在工厂环境验证。

### 用症状定位系统层

| 现场症状 | 第一处核对 | 接下来追什么 |
| --- | --- | --- |
| 能在主管列表看到，操作员队列里没有 | 当前产线、工单状态、计划开工时间 | 操作员控制器的查询与可生产范围 |
| 看得到工序，开工却失败 | 失败消息与当前工位 | 路由身份、服务中的工位和领域条件 |
| 操作返回成功，另一窗口仍是旧状态 | 刷新后数据库结果是否出现 | 该页面用 Inertia 刷新、实时集合还是产线通知 |
| 页面显示机器有读数，成品没增加 | 通道绑定与读数状态 | 计数差值、实际应用量、是否为末道有效工序 |

读代码时可以从一条 URL 找到 Route，再追 Controller、Service、Model，最后回到响应或广播。设备、ERP 和普通 Web 操作的入口各自不同。共用的领域方法及持久化对象，是检查跨入口行为是否一致的落点。

## 文档范围与开发工作区

本门户是 OpenMES 的中文开发手册，Markdown 源稿位于 MESagent/docs/developer，HTML 是可离线阅读的构建产物。MESagent/src 中的 .NET 控制台是独立起步项目，不是本手册描述的 Laravel 后端。运行 dotnet run 不会启动 OpenMES；在文档目录执行 npm install 也不会安装业务系统。

| 工作对象 | 本机位置 | 在这里做什么 |
| --- | --- | --- |
| OpenMES 阅读基线 | /Users/seal/Desktop/myLog/program/SKhynix/OpenMes | 核对业务代码、配置、迁移与测试 |
| 当前开发工作树 | /Users/seal/.codex/worktrees/6a9f/OpenMes | 开发时先确认 HEAD 与未提交变更，再选择使用 |
| 文档工程 | /Users/seal/Desktop/myNote/MESagent/docs | 编辑章节、重建门户和检查来源 |
| 离线门户 | docs/portal/index.html | 搜索、阅读章节与带行号源码快照 |

两个 OpenMES 工作树不能仅因仓库名相同就视作内容一致。本手册来源基线仍指向表中的阅读基线，具体哈希见第 11 章。

## 从需求到交付的工作路线

| 开发任务 | 建议阅读顺序 | 完成时应能交付 |
| --- | --- | --- |
| 首次接手项目 | 01 → 02 → 03 → 10 | 一份启动记录、一条可解释的生产请求 |
| 修改工序或数量规则 | 02 → 03 → 04 → 05 → 10 | 状态与数量前后对照、失败分支回归 |
| 接入设备或 ERP | 06 → 08 → 04 → 11 | 字段映射、重试规则、对账样本 |
| 新增实时页面 | 07 → 08 → 02 → 10 | 页面、读取投影、广播与授权验证 |
| 排程与报表开发 | 09 → 02 → 04 → 10 | 时间口径、数据来源、可复算结果 |

第一次阅读建议只跟一张工单，不同时展开所有模块。先能解释“请求在哪里被允许、哪次写入改变事实、页面如何读到结果”，再扩展到质量、设备与仓库。

## 最小业务实验的准备清单

在隔离实例准备一个产品、一条产线、两个工位、含两道顺序工序的模板，以及可访问该产线的主管和操作员。创建目标 10 件的工单，记录当前流转模式、计数源、顺序开关及计划开工时间。第一轮不接真实设备，先验证人工执行；随后再按第 06 章替换为机器计数场景。

1. 创建工单，确认产品、产线与快照中的两道工序。
2. 接单并创建批次，保存工单、批次、工序的 ID。
3. 开始首道工序，检查工序和批次状态变化。
4. 按所选流转模式报数和完成；不要混用两套计产时点。
5. 核对末站通过量、批次产量和工单剩余量。
6. 开第二个窗口观察更新；启用仓储后再检查单据草稿和过账。

实验结果用“操作前 → 输入 → 响应 → 操作后”记录。成功提示只能证明入口返回成功，数量、库存与跨窗口更新要分别观察。

## 跨模块协作的责任边界

核心生产服务维护执行和数量事实；质量服务解释检验与阻塞；仓储服务维护单据和库存动作；同步层把已提交的数据投影给页面。一个需求可能穿过多个边界，例如新增工序产出字段，需要同时考虑持久化、输入校验、读模型、广播、报表和导出。

建议在编码前写下三句话：本次改变哪个业务事实；允许谁在什么状态修改；哪些下游必须看到新结果。若无法回答，先回第 02 章确定字段归属，而不是先往多个表和页面复制字段。

## 源码依据

- [backend/composer.json:13](/Users/seal/Desktop/myLog/program/SKhynix/OpenMes/backend/composer.json:13)
- [backend/package.json:21](/Users/seal/Desktop/myLog/program/SKhynix/OpenMes/backend/package.json:21)
- [backend/resources/js/app.jsx:65](/Users/seal/Desktop/myLog/program/SKhynix/OpenMes/backend/resources/js/app.jsx:65)
- [docker-compose.yml:3](/Users/seal/Desktop/myLog/program/SKhynix/OpenMes/docker-compose.yml:3)
- [操作员指南 · 选线和队列](/Users/seal/Desktop/myLog/program/SKhynix/OpenMes/docs/operator-guide.md:34)
- [主管指南 · 工单操作](/Users/seal/Desktop/myLog/program/SKhynix/OpenMes/docs/supervisor-guide.md:37)
- [操作员队列 · 产线与可生产查询](/Users/seal/Desktop/myLog/program/SKhynix/OpenMes/backend/app/Http/Controllers/Web/Operator/WorkOrderController.php:28)
