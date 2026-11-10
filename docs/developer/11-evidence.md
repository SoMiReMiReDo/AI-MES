# 来源基线与已知文档差异

结论绑定当前本地工作树，并保留路径、行号和内容摘要，便于之后复核。

## 使用逻辑

### 用源码解决一条相互矛盾的说明

操作员指南的「开始批次」段落把建批和进入生产连在一起。实际排查可在测试环境做两个动作并分别记录结果。先创建批次，核对新批次是 PENDING、相关步骤已经生成。随后开始首道工序，再核对工序与批次进入生产。这两个操作对应不同控制器方法和服务。更新说明时应把「创建批次」和「开始工序」拆成两行，保留对应源码证据。

| 正在查什么 | 首选证据 | 需要补哪一层 |
| --- | --- | --- |
| 页面按钮和操作顺序 | 角色指南、React 页面与 Web 路由 | 同角色、同设置的实际页面 |
| 工序为何拒绝或允许 | 服务、模型和请求校验 | 失败分支测试及真实请求结果 |
| 数据表如何约束字段 | 迁移、模型、数据库约束 | 实际部署版本和迁移状态 |
| 未来计划是否已经交付 | 路线图、RFC 与对应入口 | 找到可达的实现和可执行验证 |

在站内打开源码链接时，用目标行定位方法，再读它的调用点和前后分支。该行能帮助定位论据，操作条件通常分布在多个方法和设置中。

## 技术逻辑

### 阅读基线

- 源码：/Users/seal/Desktop/myLog/program/SKhynix/OpenMes
- Git HEAD：6048facedb99b112caecd9f60089e753a53ca89d
- 梳理日期：2026-11-09（Asia/Shanghai）
- 工作树包含已修改和未跟踪内容，因此不能只用 HEAD 重建本次阅读材料。
- baseline.json 保存被引用文件的 SHA-256 与源工作树状态，便于识别资料变化。

### 原文档与代码的差异

| 主题 | 原说明或容易产生的误解 | 当前核对结果 |
| --- | --- | --- |
| 主 Web 技术栈 | development.md 写 Blade / Livewire | app.jsx 使用 React / Inertia；仍可存在 Blade 宿主模板 |
| 数据库版本 | 技术文档写 PostgreSQL 14+ | Compose 当前镜像为 PostgreSQL 17；不等于证明不兼容 14 |
| 模块发现 | 只按目录扫描 provider 理解 | ModuleManager 使用 manifest provider 与启用状态 |
| 设备信号 | 统一入口注释容易被理解为每次都落事件 | 相同 state 返回；计数还有 Legacy 分支，应按分派函数判断 |
| 工单完成 | 达到数量即一律结束 | transfer 仍等待进行中批次结束 |
| 仓库 | 完工事件即入库成功 | 先生成草稿，过账才移动库存；监听失败记录日志 |

### 文档来源分组

docs/index.md 用作入口；development.md 用作旧架构对照；production-flow-review.md 和 production-flow-deployment.md 用作数量及上线注意事项；machine-connectivity.md、mqtt-connectivity.md 用作协议背景；warehouse-erp-rollout.md 用作集成部署背景。源码是行为判断的优先依据。

原 docs 下其他角色指南、培训、历史评审与 RFC 也列在 source-docs.md 清单中。该清单按文件标题建立导航，不表示所有细节均完成逐条代码核验。RFC / roadmap 是设计或规划，不能据其标题宣称已实现。

### 范围与维护

本体系覆盖架构、核心生产链及主要旁路，不替代完整数据库字典或穷举 API 规范。未进行运行时行为、设备真机或生产部署验收。新增结论应附源码位置，遇到文档与实现冲突时记录差异，不静默混合两种描述。

站内源码页面是本地生成的阅读快照；修改 OpenMES 不会自动更新它们。更新文档后重新构建，检查 baseline 中引用文件哈希，并保留版本差异。

### 如何复核版本和更新这套文档

1. 检查 baseline.json 的 git_head、worktree_status 和相关文件哈希，确定对比的是哪份源码。相同 Git HEAD 的两个工作树可能有不同未提交修改。
2. 从路由和用户动作开始，找到服务入口并验证方法仍然被调用。涉及状态时核对模型规则、设置与数据库写入。
3. 更新章节正文、源码链接及 chapters.json 中对应的文件行号。新增证据标明它说明的是界面动作、当前实现、仓库测试还是本次实际验证。
4. 重新生成 portal 和全文搜索索引，运行 check_portal.py。链接和哈希通过用于确认产物与源文件的对应关系。业务结论还要依靠代码复核或隔离实例验证。

例如上游只重命名或移动一个方法，行号检查仍可能通过，但旧链接已经落到别的语句。复核时应按类名和方法名重新定位，再更新行号。验证记录中也应写清具体运行了什么。

## 证据等级与结论写法

| 等级 | 可以支持的结论 | 不能据此宣称 |
| --- | --- | --- |
| 实现阅读 | 某方法当前按这些条件执行 | 现场所有配置都经过验证 |
| 仓库测试阅读 | 仓库为某边界提供了测试契约 | 本次测试已经通过 |
| 本地文档检查 | 页面、链接、锚点、哈希与搜索产物一致 | Laravel 业务逻辑正确 |
| 隔离实例验证 | 指定版本、配置和输入下观察到结果 | 真机或生产部署普遍成立 |
| 现场验收 | 指定设备与业务流程已验证 | 其他设备、租户或模式自动兼容 |

正文中的验收矩阵是建议执行的场景，不是已执行结果。源码直接支持的字段与分支可作为实现说明；建议的日志字段、外部去重和交付步骤属于开发实践。新增内容时保持这种区分，避免把建议误写成已有功能。

## 来源清单的使用方式

chapters.json 维护章节标题、摘要与显式引用文件；baseline.json 保存阅读根目录、git_head、worktree_status 和文件 SHA-256；portal/sources 保存转义后的文本及行号。这三者共同帮助从结论追到当时的源文件。

哈希相同说明文件内容一致，不能证明选择的行支持该结论。行号存在只说明链接有效，方法移动后仍需人工复核。Git HEAD 一致也不能代替文件哈希，因为本地工作树可能包含尚未提交的修复或演示代码。

## 更新基线的标准流程

1. 先保留当前 baseline 与需要比较的章节，明确本次更新涉及哪个源码工作树。
2. 检查被引用文件的内容差异，列出受影响的行为、字段和入口。
3. 对每条受影响说明追到调用链，核对 Request、Service、Model 及测试。
4. 修改正文、源码定位与引用清单，明确新旧行为差异。
5. 重新构建离线门户，再检查链接、锚点、来源哈希、搜索和表格。
6. 在 VALIDATION.md 记录本次实际执行内容；未运行的业务或设备测试继续标记未执行。

不能仅运行构建并用新哈希覆盖旧基线，就认为所有章节已跟进新源码。生成器可以刷新文本快照，不能自动判断中文解释是否仍正确。

## 文档与实现冲突的处理模板

```text
主题：例如批次何时进入生产
旧说明：建批即开工
当前实现：createBatch 创建 PENDING；首次 startStep 推进执行
依据：类名、方法名、文件与行号、读取基线
影响：操作指南、按钮文案、状态验收
验证：静态阅读 / 指定测试 / 隔离实例（分别填写）
处理：修订当前章节，保留原指南作为历史来源
```

对于尚未覆盖的分支，明确写出待验证条件，例如某个机器协议、某种 BOM 配置或某条移动端入口。不能因为主链验证通过，就把旁路接口描述成同等成熟。

## 完整性的边界与后续维护

这套手册覆盖从开发环境、领域对象、生产执行、数量、物料质量仓库、外部集成、前端同步、权限模块，到排程追溯及交付验证的工作链。它提供核心字段和接口示例，但不取代全量数据库字典、所有 API 的机器可读规范或工厂设备作业标准。

新增业务功能的文档完成条件是：用户知道从哪里触发，开发者知道规则归属，联调者知道输入与错误，测试者知道如何判断成功，维护者能追到证据。每次业务变更按这个标准更新对应章节，比单纯增加篇幅更能保持手册可用。

## 源码依据

- [docs/development.md:9](/Users/seal/Desktop/myLog/program/SKhynix/OpenMes/docs/development.md:9)
- [docs/production-flow-review.md:3](/Users/seal/Desktop/myLog/program/SKhynix/OpenMes/docs/production-flow-review.md:3)
- [docs/warehouse-erp-rollout.md:1](/Users/seal/Desktop/myLog/program/SKhynix/OpenMes/docs/warehouse-erp-rollout.md:1)
- [backend/app/Services/ModuleManager.php:23](/Users/seal/Desktop/myLog/program/SKhynix/OpenMes/backend/app/Services/ModuleManager.php:23)
- [旧指南 · 开始批次段落](/Users/seal/Desktop/myLog/program/SKhynix/OpenMes/docs/operator-guide.md:67)
- [建批时的状态与工序生成](/Users/seal/Desktop/myLog/program/SKhynix/OpenMes/backend/app/Services/WorkOrder/WorkOrderService.php:695)
- [首道工序开工的状态推进](/Users/seal/Desktop/myLog/program/SKhynix/OpenMes/backend/app/Services/WorkOrder/BatchService.php:35)
