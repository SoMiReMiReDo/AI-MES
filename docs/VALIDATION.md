# 验证记录

验证日期：2026-11-09。范围为本次新增开发文档与本地 portal；未修改 OpenMES 源码，未运行 OpenMES 业务测试或数据库迁移。

## 本轮逐章开发手册扩充验证

日期：2026-11-09（Asia/Shanghai）。11 个章节全部扩充，新增正文约 19,606 字符（含 Markdown 标记）；补充字段与状态契约、请求示例、事务与重试、设备读数状态、ERP 部分成功、环境搭建、开发步骤及验收矩阵。同步更新首页描述、章节目录、搜索索引和来源快照。

- `python3 docs/tools/build_portal.py`：11 章、102 份来源快照生成成功。
- `python3 docs/tools/check_portal.py`：115 个 HTML 页面、32,391 个本地链接/锚点、11 章、102 份源文件 SHA-256 通过。链接计数包含源码逐行锚点。
- `node --check docs/portal/assets/app.js` 与 `node --check docs/portal/assets/search-data.js`：语法检查通过。
- 逐章检查搜索正文与 Markdown 完全一致，49 张 Markdown 表格列数一致，代码围栏成对、章节标题进入 HTML、页面 ID 无重复。
- 搜索数据抽查：207、rebaseline 命中设备集成章节；Octane 命中开发章节；数量验收表命中数量章节。此项为索引数据检查，不是浏览器交互测试。
- 浏览器视觉复核未完成：浏览器工具安全策略拒绝打开 file:// 协议；未绕过该限制，也未把历史浏览器检查当作本轮结果。
- 未运行 OpenMES 业务测试、迁移、设备接入或 ERP 联调。正文验收场景与命令为后续开发使用的说明，不代表本轮已执行。

## 双视角扩写后的验证

- 重建 11 章 HTML 和全文搜索索引，逐章核对使用逻辑、技术逻辑、源码依据及章内导航。
- 检查 Markdown 表格列数、前端脚本语法和检索正文一致性。
- 111 个 HTML 页面、32,036 个本地链接和锚点、98 份来源文件哈希检查通过。链接计数包含源码逐行锚点。
- 本轮验证范围为文档产物和源码引用，业务场景作为复现说明。

## 初版浏览器与构建验证

- `python3 tools/build_portal.py`：生成 11 章、原文档索引、首页及 85 份带行号来源快照。
- `python3 tools/check_portal.py`：98 个 HTML 页面、28,379 个本地链接/锚点引用、11 章与 85 份文件哈希检查通过。链接数量包含源码每行的行号锚点，并非 28,379 条独立业务引用。
- `node --check portal/assets/app.js`：前端脚本语法检查通过。
- 本机 HTTP 浏览器验证：首页、窄屏导航展开、搜索 MachineCounter 得到 3 个章节、搜索结果进入设备集成章节。
- 点击 MachineCounterService.php:113 后，URL 定位 #L113，目标内容为 ingest 方法；页面目标位置约为视口顶部 95px。
- 首页 transfer 切换展示数量流转说明与成品 55 的例子；默认 whole_batch 说明正常。
- 检查桌面 1440px 和默认窄屏布局；手机宽度检查 document.scrollWidth 与 clientWidth 相等，未出现页面级横向溢出。
- 页面预览保存为 `portal-preview.jpg`。

## 验证限制

- 浏览器工具禁止 file: 协议，因此未在工具中直接运行双击 HTML 的场景。页面采用本地 CSS/JS、内嵌搜索数据，不需要 fetch、CDN 或构建依赖；可由用户直接打开，或使用 README 的 HTTP 命令。
- 未执行 PHP、Vitest、Playwright 业务测试，不能将文档站通过当成 MES 业务验收。
- 未验证外部设备、ERP、生产数据库与跨端应用。

## 后续复验

```bash
cd /Users/seal/Desktop/myNote/MESagent/docs
python3 tools/build_portal.py
python3 tools/check_portal.py
node --check portal/assets/app.js
```

Markdown 是正文来源，HTML 是生成结果。构建器仅支持本项目使用的标题、段落、列表、表格、链接及代码块，不是通用 Markdown 实现。源码快照和 baseline.json 在构建时更新；源码变化时还须人工重新核对正文结论及 chapters.json 的行号。
