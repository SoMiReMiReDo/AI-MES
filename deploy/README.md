# MESagent Docker 单机交付

交付包含 Nginx WebUI、ASP.NET Core API 及同进程 Outbox Worker、PostgreSQL、一次性迁移和授权任务。需要 Docker Engine 或 Docker Desktop、Docker Compose v2（支持 `up --wait`）、Python 3.9+ 和镜像仓库访问。脚本在 macOS、Linux 或可调用 Docker 的 WSL 环境运行。以下命令从仓库根目录执行。

## 空实例启动

```sh
python3 scripts/delivery.py --instance pilot init --port 5088
python3 scripts/delivery.py --instance pilot up
python3 scripts/delivery.py --instance pilot bootstrap --username admin
python3 scripts/delivery.py --instance pilot status
```

浏览器打开 [本地 WebUI](http://127.0.0.1:5088)，首次登录修改密码。引导命令交互读取密码，已有管理员的实例会拒绝重复引导。库和 API 走容器网络，宿主只开放回环 Web 端口。首次构建需要拉取固定镜像并恢复锁定依赖。数据和配置使用独立作用域 `mesagent-delivery-pilot` 和 `.local/delivery/pilot/`。配置包含数据库凭据，目录和文件由脚本限制权限，Docker 宿主访问权限等同于部署运维权限。

更新前先备份。再次运行 `up` 时先构建镜像，再停止正在运行的 Web 与 API。Compose 完成增量迁移和授权后启动新版，等待 API 与 Web 就绪。Web 同源的 `/health/live` 检查 API 存活，`/health/ready` 检查数据库查询是否可用。排查日志时使用实例配置和完整 Compose 项目名。

```sh
docker compose -p mesagent-delivery-pilot --env-file .local/delivery/pilot/compose.env \
    -f compose.app.yaml logs --tail 100 postgres migrate provision api web
```

### HTTPS 正式入口

建立正式实例时在 `init`（或 `restore`）加 `--https-proxy`，应用使用 Production 与 Secure Cookie。宿主 HTTPS 代理终止 TLS，将请求转至该实例的回环 Web 端口，保留原始 Host（含非默认端口）和 WebSocket 升级。外部 HTTP 在宿主入口跳转 HTTPS。容器内 Web 代理根据部署模式写入 HTTPS scheme，API 只接收私有容器网络中的代理请求。以下为宿主 Nginx 的 HTTPS 站点转发段。

```nginx
location / {
    proxy_pass http://127.0.0.1:5088;
    proxy_http_version 1.1;
    proxy_set_header Host $http_host;
    proxy_set_header Upgrade $http_upgrade;
    proxy_set_header Connection $mes_upgrade;
    proxy_read_timeout 110s;
    client_max_body_size 15m;
}
```

在宿主 Nginx 的 `http` 块中定义 `map $http_upgrade $mes_upgrade { default upgrade; '' close; }`，在对应 TLS 站点配置证书和证书私钥。将正式实例的回环 Web 端口保留为宿主代理上游。

## 演示与验收

本机装好 Chrome 后执行。完整验收另外使用与 `global.json` 匹配的 .NET SDK。

```sh
npm --prefix web ci
python3 scripts/delivery.py --instance demo demo --port 5089
python3 scripts/delivery.py acceptance --port 5090 --keep
```

`demo` 从全新实例创建示例数据，成功后将随机登录凭据写入该实例的 `demo-login.txt`。浏览器主流程中生成的 `EXEC-*` 工单和 Lot 包含五片三站完工记录，追溯页可按编号查询。重新演示时使用新的实例名与空闲端口。`acceptance` 随机命名隔离项目，运行浏览器、数据库和 API 测试，演练故障与恢复。成功后清理临时恢复实例，`--keep` 保留源演示实例。只有测试阶段临时发布源数据库的回环端口。

已有锁定宿主 SDK 时，可以运行 `sh scripts/publish-local.sh` 生成便携发布产物，再给 `up`、`demo`、`acceptance` 或 `restore` 加 `--prebuilt`，使用相同运行镜像打包。标准路径由 Docker 构建阶段完成编译。

## 备份与恢复

```sh
mkdir -p .local/delivery-backups
python3 scripts/delivery.py --instance pilot backup .local/delivery-backups/pilot-copy
python3 scripts/delivery.py --instance restored restore .local/delivery-backups/pilot-copy --port 5091
```

备份短暂停止应用和后台写入，保留源数据库运行。成功生成数据库归档、工艺 PDF 归档、Cookie 密钥归档和校验清单，原本运行的应用随后恢复。请将完整备份存入加密、独立且受访问控制的存储。恢复保留原厂区 ID，为新实例名和端口重新生成数据库凭据。若数据或文件恢复中断，脚本将阻止启动该目标实例，请保留原备份并使用新的实例名重试。只恢复由受信部署环境生成并保管的备份，回切前先用对应版本验证完整性。更多范围和验收条件见 [F14 功能文档](../docs/feature-development/docker-delivery-acceptance.md)。
