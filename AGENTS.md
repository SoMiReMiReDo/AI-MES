# MESagent 工作约定

- 使用中文沟通，对详细内容进行取舍并总结；用户追问后再展开。
- 开始工作先读 [项目记忆](MEMORY.md)，再按需读 `docs/development-process/`。
- 项目目标：C# 后端的半导体 MES + Agent + 完整 WebUI，最终以 Docker 交付。
- 用户于 2026-11-11 明确启动数据库开发；当前已建立数据库模型、迁移、本地 PostgreSQL 服务和测试。后续按用户授权范围推进，不把预留表结构当作已实现业务流程。
- 区分“已实现”“用户已确认”“建议方案”“待验证”。建议选型不等于用户最终确认。
- `src/MESagent` 保留原 .NET 控制台骨架；数据库实现位于 Domain、Infrastructure、Database 项目。`docs/developer`、`docs/portal` 和课程是 OpenMES 参考资料，不代表本项目已有功能。
- 保留既有学习材料；本项目规划写入 `docs/development-process/`，不要混写进 OpenMES 开发手册。
- 后续开发需让 WebUI 与 Agent 共用服务端权限和业务规则；模型输出不得直接改生产数据或控制设备。
- 每次形成重要决定或阶段进展后，更新 `MEMORY.md` 中的状态、决定和下一步，保持记忆简短，不保存密钥和生产敏感数据。
