# OpenMES 开发文档中心

从 [前端阅读页面](portal/index.html) 开始，或直接阅读 [项目全景](developer/01-overview.md)。

11 章覆盖环境搭建、领域字段与状态、请求契约、事务与数量、物料质量仓储、设备与 ERP、实时页面、权限模块、排程追溯以及交付验证。每章从业务场景进入实现，补充开发步骤、边界条件、排障和验收矩阵，并附可跳转行号的源码依据。前端支持全文检索与章内跳转。

本手册描述 OpenMES（Laravel / React），文档所在的 MESagent .NET 控制台是独立项目。开发命令应在各自正确的工作目录执行。

## 章节

- [项目全景与阅读地图](developer/01-overview.md) — OpenMES 将生产计划转成可执行工序，再把实际产量、物料、质量与设备记录回收到同一业务链。
- [领域对象与数据归属](developer/02-domain.md) — 先区分配置、计划、执行和事实数据，才能理解为什么同一工艺会在多个对象中出现。
- [生产主链与事务边界](developer/03-production.md) — 从一次完成工序请求追到产量回写，分清入口授权、领域约束与数据库提交。
- [两种流转与数量账本](developer/04-quantity.md) — whole_batch 控制整批顺序，transfer 让工序按已转出的数量重叠执行。两者的完工与计产时点不同。
- [物料、质量与仓储闭环](developer/05-material-quality.md) — 生产会触发分配、耗用和检验；仓库草稿与正式库存过账属于另外的业务阶段。
- [设备信号与 ERP 集成](developer/06-machine-erp.md) — 外部系统只提供输入；OpenMES 仍要决定读数属于谁、是否新鲜、是否可计入生产。
- [前端入口与实时读模型](developer/07-frontend-sync.md) — React 页面负责呈现与交互，Laravel 写业务数据，实时集合让多个页面看到提交后的结果。
- [权限、租户与扩展模块](developer/08-permissions-modules.md) — 界面是否可见、用户是否有权操作、数据是否属于当前租户，是三层不同约束。
- [排程、绩效与追溯旁路](developer/09-planning-traceability.md) — 这些子系统围绕生产主链提供计划、解释和追查能力；应按业务问题选择入口。
- [开发、验证与问题定位](developer/10-development.md) — 用现有测试约束修改范围；区分文档站验证、业务测试和生产环境验证。
- [来源基线与已知文档差异](developer/11-evidence.md) — 结论绑定当前本地工作树，并保留路径、行号和内容摘要，便于之后复核。

## 打开与维护

直接用浏览器打开 portal/index.html，无需安装依赖，也无需启动 OpenMES。可选 HTTP 预览：

```bash
cd /Users/seal/Desktop/myNote/MESagent/docs
python3 -m http.server 8765 --bind 127.0.0.1
# 浏览器打开 http://127.0.0.1:8765/portal/
```

修改 developer/*.md 后，执行 python3 tools/build_portal.py；验证执行 python3 tools/check_portal.py。

- [源码文档目录](developer/source-docs.md)
- [验证记录](VALIDATION.md)
- [已有交互课程](openmes-course/index.html)
- [已有学习路线](LEARNING_PATH.md)
- [已有实现导读](openmes-implementation-guide.md)

原有学习材料保持不变；本次新增开发文档体系与独立 portal。页面仅为本地文档阅读，不连接或改写 MES 业务数据库。
