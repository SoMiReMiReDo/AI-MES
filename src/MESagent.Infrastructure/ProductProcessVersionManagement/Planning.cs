using System.Text.Json;
using MESagent.Application;
using MESagent.Domain;
using MESagent.Domain.Entities;
using MESagent.Infrastructure.LotWaferCarrierGenealogy;
using Microsoft.EntityFrameworkCore;

namespace MESagent.Infrastructure.ProductProcessVersionManagement;

public sealed partial class ManufacturingBackend
{
    private async Task<OrganizationIdentityAccessControlResult> Planning(OrganizationIdentityAccessControlRequest r,
        string[] p)
    {
        if (p.Length < 2 || p[1] != "work-orders")
            throw Error("接口不存在", 404);
        if (r.Method == "POST" && p.Length == 2 && r.Body.TryGetProperty("fabId", out var requestedFab)
            && requestedFab.GetGuid() != fabId) throw Error("厂区不可访问", 404);
        if (p.Length == 2 && r.Method == "POST" && string.IsNullOrWhiteSpace(Text(r.Body, "orderNo")))
        {
            var productId = r.Body.GetProperty("productId").GetGuid();
            var sequences = await db.Set<NumberSequence>().Where(x => x.Purpose == "WorkOrder"
                && (x.ProductId == productId || x.ProductId == null)).ToListAsync();
            var preferred = sequences.Where(x => x.ProductId == productId).ToList();
            if (preferred.Count == 0) preferred = sequences.Where(x => x.ProductId == null).ToList();
            if (preferred.Count != 1)
                throw Error("需要唯一的 WorkOrder 编号规则或手工工单号");
            var sequence = preferred[0];
            var number = sequence.Prefix + sequence.NextNumber.ToString().PadLeft(sequence.PadSize,
                '0') + sequence.Suffix;
            sequence.NextNumber = checked(sequence.NextNumber + 1);
            Audit(sequence, "AllocatedForWorkOrder");
            var values = r.Body.Deserialize<Dictionary<string, JsonElement>>(Json)!;
            values["orderNo"] = JsonSerializer.SerializeToElement(number);
            r = r with { Body = JsonSerializer.SerializeToElement(values) };
        }
        if (p.Length <= 3)
            return await Resource<WorkOrder>(r, p,
                "orderNo fabId productId productRevisionId plannedQty priority dueDate plannedStartAt plannedEndAt description",
                async x =>
                {
                    if (x.FabId != fabId)
                        throw Error("厂区不可访问", 404);
                    if (!ManufacturingRules.ValidWorkOrderPlan(x.PlannedQty, x.Priority, x.PlannedStartAt,
                        x.PlannedEndAt))
                        throw Error("计划数量、优先级或时间无效");
                    if ((await Get<ProductRevision>(x.ProductRevisionId)).ProductId != x.ProductId)
                        throw Error("产品修订不匹配");
                }, x => { x.Status = "Draft";
 x.FabId = fabId;
 });
        if (!Guid.TryParse(p[2], out var id))
            throw Error("无效工单", 404);
        var order = await db.Set<WorkOrder>().SingleOrDefaultAsync(x => x.Id == id && x.FabId == fabId)
            ?? throw Error("工单不存在", 404);
        if (r.Method == "GET")
        {
            if (p[3] == "progress")
                return new(await ProductionProgress.Read(db, order, Now));
            if (p[3] == "lots")
                return new(await Page(db.Set<Lot>().Where(x => x.WorkOrderId == id && x.FabId == fabId), r));
            if (p[3] == "snapshots")
            {
                var query = db.Set<WorkOrderSnapshot>().Where(x => x.WorkOrderId == id);
                if (p.Length == 5 && int.TryParse(p[4],
                    out var version)) query = query.Where(x => x.Version == version);
                return new(await query.OrderBy(x => x.Version).ToListAsync());
            }
            throw Error("接口不存在", 404);
        }
        if (r.Method != "POST" || p.Length != 4)
            throw Error("接口不存在", 404);
        Fields(r.Body, ["reason"]);
        Match(r.Version, order.RowVersion);
        if (p[3] == "recalculate")
        {
            if (string.IsNullOrWhiteSpace(Text(r.Body, "reason")))
                throw Error("必须填写重算原因");
            var progress = await ProductionProgress.Read(db, order, Now);
            if (progress.CompletedQty == null)
                throw Error("历史事实尚未完成核验，不能覆盖缓存", 409);
            order.ProducedQty = progress.CompletedQty.Value;
            order.CompletedAt = order.ProducedQty == order.PlannedQty
                ? await db.Set<WaferCompletion>().Where(x => x.WorkOrderId == order.Id)
                    .MaxAsync(x => (DateTime?)x.CompletedAt) : null;
        }
        else if (p[3] == "release")
        {
            if (order.Status != "Draft")
                throw Error("只能下达草稿", 409);
            var revision = await Get<ProductRevision>(order.ProductRevisionId);
            if (!ManufacturingRules.IsEffective(revision.LifecycleStatus, revision.EffectiveFrom,
                revision.EffectiveTo, Now))
                throw Error("产品修订未生效");
            await ReleasedProduct(revision);
            var route = await Get<RouteVersion>(revision.RouteVersionId!.Value);
            var steps = await db.Set<RouteStep>().Where(x => x.RouteVersionId == route.Id).OrderBy(x => x.SequenceNo).ToListAsync();
            var stepIds = steps.Select(x => x.Id).ToArray();
            order.ProcessSnapshot = JsonSerializer.Serialize(new
            {
                schemaVersion = 1, plan = new { order.OrderNo, order.PlannedQty, order.Priority, order.DueDate },
                revision, route, steps,
                checklist = await db.Set<ChecklistItem>().Where(x => stepIds.Contains(x.RouteStepId)).ToListAsync(),
                outputs = await db.Set<OutputDefinition>().Where(x => stepIds.Contains(x.RouteStepId)).ToListAsync(),
                documents = await db.Set<EngineeringDocument>().Where(x => x.EntityType == "RouteStep"
                    && stepIds.Contains(x.EntityId)).Select(x => new { x.Id, x.EntityId, x.Revision,
                        x.Checksum }).ToListAsync(),
                recipeApplicabilities = await db.Set<RecipeApplicability>().Where(x => stepIds.Contains(x.RouteStepId)).ToListAsync(),
                generatedAt = Now, actor.UserId
            }, Json);
            order.SnapshotVersion = 1;
            db.Add(new WorkOrderSnapshot { WorkOrderId = id, Version = 1, Snapshot = order.ProcessSnapshot,
                EffectiveFrom = Now, CreatedById = actor.UserId });
            order.Status = "Released";
        }
        else if (p[3] is "cancel" or "close")
        {
            if (string.IsNullOrWhiteSpace(Text(r.Body, "reason")))
                throw Error("必须填写原因");
            if (order.Status != "Released" && !(p[3] == "cancel" && order.Status == "Draft"))
                throw Error("工单状态不允许该动作", 409);
            var lots = db.Set<Lot>().Where(x => x.WorkOrderId == id);
            if (await lots.AnyAsync(x => x.Status != "Completed" && x.Status != "Merged"
                    && x.Status != "Closed" && x.Status != "Cancelled")
                || await db.Set<Wafer>().AnyAsync(x => x.WorkOrderId == id
                    && (x.LifecycleStatus == "Registered" || x.LifecycleStatus == "Active"))
                || await db.Set<StepExecution>().AnyAsync(x => lots.Select(l => l.Id).Contains(x.LotId)
                    && x.ExitedAt == null)
                || await db.Set<HoldRecord>().AnyAsync(x => lots.Select(l => l.Id).Contains(x.LotId)
                    && x.ReleasedAt == null)
                || await db.Set<WorkOrderStop>().AnyAsync(x => x.WorkOrderId == id && x.ResumedAt == null)
                || await db.Set<QualityTask>().AnyAsync(x => lots.Select(l => l.Id).Contains(x.LotId!.Value)
                    && x.CompletedAt == null)
                || await (from run in db.Set<ProcessRun>()
                          join step in db.Set<StepExecution>() on run.StepExecutionId equals step.Id
                          where lots.Select(l => l.Id).Contains(step.LotId) && run.TrackOutAt == null
                          select run.Id).AnyAsync())
                throw Error("存在未终结生产或质量关联，不能关闭/取消", 409);
            if (p[3] == "cancel" && await db.Set<WaferRelease>().AnyAsync(x => x.WorkOrderId == id))
                throw Error("已有首次投放事实，不能取消工单", 409);
            var progress = await ProductionProgress.Read(db, order, Now);
            if (progress.CompletedQty == null)
                throw Error("历史生产关联未核验，不能关闭工单", 409);
            order.ProducedQty = progress.CompletedQty.Value;
            order.Status = p[3] == "cancel" ? "Cancelled" : "Closed";
        }
        else throw Error("接口不存在", 404);
        order.UpdatedAt = Now;
        Audit(order, p[3], new { reason = Text(r.Body, "reason") });
        await db.SaveChangesAsync();
        return new(View(order), Tag(order.RowVersion));
    }
}
