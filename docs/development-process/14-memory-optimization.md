# MESagent 内存优化开发文档

2026-11-24（Asia/Shanghai）｜状态：已测开发基线，优化待实施｜本项目专用，不属于 OpenMES 参考手册

先用现有交付模式测出不含热更新工具的运行基线，再优化登录后页面和高开销查询。内存限额不是优化手段，等测出业务峰值和恢复情况后再定。

## 1. 现状与原因

| 测量对象 | 已测结果 | 解释 |
| --- | --- | --- |
| 开发 API 容器 | 启动约 1 分钟：1.06 GiB；约 5 分钟：1.10–1.11 GiB | 热更新与构建工具合计约 805 MiB PSS，API 服务进程约 200–230 MiB PSS。优先处理 `dotnet watch`、Roslyn 和 MSBuild 的常驻开销。 |
| 开发 Web 容器 | 启动约 1 分钟：809 MiB；约 5 分钟：505–507 MiB | Vite、npm、esbuild 合计约 211 MiB PSS；容器值还计入部分文件缓存等开销。启动会运行 `npm ci`，但未单独测出它对启动读数的贡献。 |
| 开发 PostgreSQL 容器 | 启动约 1 分钟：74 MiB；约 5 分钟：102 MiB | 仅作对照，暂不调整数据库内存配置。 |
| Chrome 登录页 | Vite 16.3 MiB；现有静态构建 4.9 MiB | 隔离浏览器各测三次，记录 GC 后已用 JS 堆。静态构建对照不等于完整交付栈测试，也不代表标签页总内存。 |

容器采样期间未主动施加业务负载。`docker stats` 在 Linux cgroup v2 下扣除 `inactive_file`；PSS 把共享驻留页按进程分摊。两者不能直接相减来估算堆或节省量。当前数据也不足以判断是否泄漏。配置与历史记录见[开发 Compose](../../compose.yaml)和[项目记忆](../../MEMORY.md)。

## 2. 实施顺序

### P0：测量免热更新的交付模式

- 开发仍用 Vite 和 `dotnet watch`；演示、验收和日常使用优先走现有[交付 Compose](../../compose.app.yaml)。交付镜像使用 [Release API](../../deploy/Dockerfile.api) 和 [Nginx 静态前端](../../deploy/Dockerfile.web)，运行时不启用 SDK 构建工具或 Vite，无须新建一套部署方案。
- 按[交付操作说明](../../deploy/README.md)启动隔离实例。用同一份脱敏数据快照分别恢复到两个独立实例，一次只运行一套被测服务；不复用正在工作的开发卷，不停现有服务做试验。端口、实例名和权限保持隔离。
- 保留现有热更新和健康检查。若必须在低内存机器上开发，再单独试验依赖未变时跳过重复安装，或验证事件监听能否替代轮询；改动后复测绑定挂载下的 CSS 和 C# 更新。

### P1：减少浏览器首屏和切页开销

- [入口](../../web/src/main.tsx)静态导入多数业务页。先保留登录、会话和导航壳同步加载，将生产分析、追溯等页面按路由懒加载。确认权限、深链接、页面加载失败提示和首访延迟不退化。
- 先用堆快照区分查询数据、DOM 和订阅的保留量。仅对确认占用较大的筛选查询调整 [`gcTime`](../../web/src/queryClient.ts)，核对切页和退出登录后的清理、请求量及实时更新；不要为节省内存而统一禁用缓存。只有长表的 DOM 被测为热点时，再评估虚拟化。

### P2：限制后端单次请求的缓冲量

- [工单履历](../../src/MESagent.Infrastructure/ProductionAnalytics/Orders.cs)先物化候选工单，再在内存生成结果、汇总和分页。优先在 PostgreSQL 下推可等价的筛选、投影、聚合和稳定排序分页；仍在应用计算的口径要先验证等价，不能简单提前 `Take` 截断候选集。确认工单号搜索、缺历史时间、厂区时区和达计划时点等边界行为。
- 工单 CSV 在完整物化后检查 10,000 行上限，随后拼接整份文本并生成字节数组。先让上限校验不依赖完整物化，再评估有界分批或流式输出。必须保留同筛选、权限、行顺序、CSV 公式注入保护及审计。发送响应前完成鉴权与上限校验；明确断开、写入失败与审计的关系，不能把未完整交付记录成已成功下载。
- 对照数据库计划和延迟，防止只是把压力转移到 PostgreSQL。不先改 GC 模式、堆上限或容器限额，也不把回收后工作集仍未下降单独判成泄漏。

## 3. 复测与验收

统一记录提交版本、运行模式、Docker 资源、浏览器版本、数据行数、筛选、并发度和测试时长。先对照开发与交付，然后在**同一交付模式**下比较优化前后，每组至少重复三轮。

| 场景 | 采样和验收 |
| --- | --- |
| 启动与空闲 | 记录启动峰值、就绪时间、就绪后固定时点的三容器占用。确认交付容器没有开发工具常驻，空闲 API 和 Web 内存低于开发模式。 |
| WebUI 首访与多轮切页 | 冷开登录页、登录后打开工作台，再反复切换工单履历、OEE 和追溯的筛选与路由。记录首屏 JS 体积、响应时间、GC 后 JS 堆和 DOM；复测实时刷新、权限和直接访问。优化后首屏 JS 应减少，切页后的保留量应趋于稳定。 |
| 工单查询与 CSV 并发 | 覆盖普通页、宽日期窗、工单号搜索、接近导出上限与超限场景。保持请求序列和并发一致，对比 API 内存峰值、分配率、延迟 P95、错误率与 PostgreSQL 开销。优化后峰值应降低，不得以明显延迟或错误率退化换取；列表、汇总、CSV 与审计须对账。 |
| 负载结束与持续运行 | 比较多轮负载结束后的托管堆和浏览器 JS 堆，观察是否持续抬升；记录容器重启及 `memory.events` 的 `oom`、`oom_kill` 增量，均应为零。若持续增长，再用多次快照定位保留引用。 |

容器用 `docker stats` 同频采样；需要归因时，查 cgroup `memory.current`、`memory.stat` 和进程 `/proc/<pid>/smaps_rollup` 的 `Pss`，只记进程名和用量，不保存完整启动参数。API 在隔离环境用 `dotnet-counters` 采集工作集、分配率和 GC 堆；浏览器用 Chrome DevTools 堆快照对照。无改动的 A/B 先建立基线波动，再确认内存和延迟的量化门槛。完整交付基线、登录后页面和业务峰值目前均待测。

**参考**：[Docker 容器内存口径](https://docs.docker.com/reference/cli/docker/container/stats/)、[.NET 运行时计数器](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/available-counters)、[Chrome 堆快照](https://developer.chrome.com/docs/devtools/memory-problems/heap-snapshots/)、[EF Core 高效查询](https://learn.microsoft.com/en-us/ef/core/performance/efficient-querying)、[生产分析业务口径](13-production-analytics.md)。
