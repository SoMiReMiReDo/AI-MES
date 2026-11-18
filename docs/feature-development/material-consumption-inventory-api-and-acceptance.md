# F08-01 接口与验收

接口前缀为 `/api/v1`。查询采用 `items/total/page/pageSize`，默认每页 20、最大 100。登记提交防伪令牌和 `Idempotency-Key`，成功返回 201；同键同内容重放，异内容返回 409。

| 方法与路径 | 请求或用途 |
| --- | --- |
| GET /materials/wafer-materials | 启用硅片物料，支持 `search` 检索代码和名称 |
| POST /materials/wafer-materials | `code`（64）、`name`（200） |
| GET /materials/wafer-sources | 本厂手工来源，支持 `materialId` 和 `search` |
| POST /materials/wafer-sources | `materialId`、`code`（128）、`name`（200） |
| GET /materials/wafer-lots | 本厂手工来料批次，支持 `search` 检索批号、供应方批号、物料和来源代码 |
| POST /materials/wafer-lots | `sourceId`、`lotNumber`（64）、`supplierLotNo`（128）、可选 `expiryDate`（当地日历 YYYY-MM-DD） |
| GET /materials/wafer-lots/{id} | 本厂批次详情，含有效性与来源 |
| GET /materials/wafer-lots/{id}/wafers | 反查本厂晶圆，逐片返回稳定标识、生命周期和按权限显示的当前生产批 |
| GET /materials/wafer-origin-options | 建批可选批次，支持 `search`，只返回可用批次的精简标识 |
| POST /production/lots | 延用逐片 `originMaterialLotId` 和可选 `supplierWaferCode` |
| GET /production/wafers/{id} | 返回原来源 FK、来源覆盖状态和授权范围内的硅片来料摘要 |

缺失或越厂的来源返回 404，权限不足返回 403，冻结、停用及过期返回 409。文本去除首尾空格并按数据库现有大小写规则判重。输入验证失败时回滚整个命令，响应遵循现有错误契约。

## 验收

| 场景 | 预期 |
| --- | --- |
| 物料 → 来源 → 来料批 → 建批并投放 | 批号可检索；晶圆保留永久来源；双向链接可用 |
| 建片不选来源，或查询旧片 | 显示来源未登记或既有引用，存量事实保持可读 |
| 重试、重复批号及同键异参 | 精确重放，冲突无重复登记、审计或事件 |
| 过期、冻结、停用、ID 跨厂 | 不提供可选项；绕过 UI 的非法建片或投放被服务端拒绝 |
| 拆批后反查 | 同一晶圆仍关联原来料，当前生产批随实际归属更新 |
| 读、登记、建片、晶圆追溯权限分离 | 未授权入口拒绝，跨厂对象和数量不暴露 |
| 对照库存和 F7 | 无记账；登记批不出现在 F7 上机耗材选项，既有耗材流程正常 |
