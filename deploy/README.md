# 部署草案（后置）

用户于 2026-11-11 明确要求本轮先完成架构与 OrganizationIdentityAccessControl 功能开发，暂不完成 Docker 打包。此目录、`compose.app.yaml`、`compose.prebuilt.yaml` 和 `scripts/publish-local.sh` 保留为此前已编写的草案，不作为已验收交付使用。

应用容器启动、健康检查、HTTPS/代理、运行账号、重启、密钥与业务数据恢复均应在后续交付阶段重新联调。当前开发启动方式见 [OrganizationIdentityAccessControl 实现说明](../docs/feature-development/organization-identity-access-control-implementation.md)。现有 PostgreSQL 开发容器继续使用原 `compose.yaml`。
