using MESagent.Application;
using MESagent.Domain.Entities;
using MESagent.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;

namespace MESagent.Infrastructure.EquipmentMaintenanceResources;

public sealed partial class EquipmentBackend
{
    private async Task<Guid?> ValidateTarget(Guid? equipmentId, Guid? toolId)
    {
        if ((equipmentId == null) == (toolId == null))
            throw Error("InvalidMaintenanceTarget", "设备或工具必须恰选一项", 422);
        if (equipmentId != null)
        {
            await Equipment(equipmentId.Value);
            return equipmentId;
        }
        var tool = await Tools.SingleOrDefaultAsync(x => x.Id == toolId)
            ?? throw Error("ResourceNotFound", "工具不存在", 404);
        if (tool.EquipmentId != null)
            await Equipment(tool.EquipmentId.Value);
        return tool.EquipmentId;
    }

    private async Task Assignee(Guid? userId)
    {
        if (userId != null && !await db.Users.AnyAsync(x => x.Id == userId && x.IsActive && x.AccountType == "Human"
            && db.Set<UserFabScope>().Any(s => s.UserId == x.Id && s.FabId == fabId)))
            throw Error("InvalidAssignee", "负责人必须为本厂启用的人类账号", 422);
    }

    private static void Utc(DateTime time)
    {
        if (time.Kind != DateTimeKind.Utc)
            throw Error("InvalidTime", "时间必须为 UTC", 422);
    }

    private async Task<OrganizationIdentityAccessControlResult> Maintenance(
        OrganizationIdentityAccessControlRequest r, string[] p)
    {
        if (p[1] == "resources" && p.Length == 2 && r.Method == "POST")
        {
            var input = Body<RegisterEquipmentTool>(r);
            var equipment = input.EquipmentId == null ? null : await Equipment(input.EquipmentId.Value);
            if (equipment != null && input.EquipmentTypeId != null
                && equipment.EquipmentTypeId != input.EquipmentTypeId)
                throw Error("ToolTypeMismatch", "工具适用设备类型与绑定设备不一致", 422);
            if (input.EquipmentTypeId != null && !await db.Set<EquipmentType>().AnyAsync(x =>
                x.Id == input.EquipmentTypeId && x.IsActive))
                throw Error("InvalidEquipmentType", "设备类型不存在或停用", 422);
            var value = new Tool
            {
                Code = Text(input.Code, 64),
                Name = Text(input.Name, 200),
                FabId = fabId,
                EquipmentId = input.EquipmentId,
                EquipmentTypeId = input.EquipmentTypeId,
                Description = Text(input.Description, 2000, false),
                Status = "Available"
            };
            db.Add(value);
            return await Saved(value, "ToolRegistered", input, true);
        }
        if (p[1] == "resources" && p.Length == 3 && r.Method == "PUT")
        {
            var value = await Tools.SingleOrDefaultAsync(x => x.Id == Id(p[2]))
                ?? throw Error("ResourceNotFound", "工具不存在", 404);
            Match(r.Version, value.RowVersion);
            if (value.EquipmentId != null)
                await IdleResource(value.EquipmentId.Value);
            var input = Body<EditEquipmentTool>(r);
            if (input.Status is not ("Available" or "Blocked"))
                throw Error("InvalidToolStatus", "工具状态应为 Available 或 Blocked", 422);
            if (r.Body.TryGetProperty("equipmentId", out _) && input.EquipmentId != value.EquipmentId)
            {
                if (await MaintenanceWorks.AnyAsync(x => x.ToolId == value.Id
                    && x.Status != "Released" && x.Status != "Cancelled"))
                    throw Error("ToolMaintenanceOpen", "工具有未关闭维护，不能更换绑定设备");
                if (input.EquipmentId != null)
                {
                    var equipment = await Equipment(input.EquipmentId.Value);
                    if (value.EquipmentTypeId != null && equipment.EquipmentTypeId != value.EquipmentTypeId)
                        throw Error("ToolTypeMismatch", "工具适用设备类型与绑定设备不一致", 422);
                    await IdleResource(input.EquipmentId.Value);
                }
                value.EquipmentId = input.EquipmentId;
            }
            value.Name = Text(input.Name, 200);
            value.Status = input.Status;
            value.IsActive = input.IsActive;
            value.Description = Text(input.Description, 2000, false);
            return await Saved(value, "ToolUpdated", input);
        }
        if (p[1] == "maintenance-plans" && p.Length == 2 && r.Method == "POST")
        {
            var input = Body<CreateMaintenancePlan>(r);
            await ValidateTarget(input.EquipmentId, input.ToolId);
            await Assignee(input.AssignedToId);
            Utc(input.NextDueAt);
            if (input.Frequency is not ("Hours" or "Days") || input.IntervalValue < 1 || input.IntervalValue > 36500)
                throw Error("InvalidMaintenancePeriod", "周期应为 Hours/Days 和 1～36500 的正整数", 422);
            var value = new MaintenanceSchedule
            {
                Name = Text(input.Name, 200),
                EventType = Text(input.EventType, 32),
                Frequency = input.Frequency,
                IntervalValue = input.IntervalValue,
                NextDueAt = input.NextDueAt,
                EquipmentId = input.EquipmentId,
                ToolId = input.ToolId,
                AssignedToId = input.AssignedToId
            };
            db.Add(value);
            return await Saved(value, "MaintenancePlanCreated", input, true);
        }
        if (p[1] == "maintenance-plans" && p.Length == 4 && r.Method == "POST")
        {
            var value = await Plans.SingleOrDefaultAsync(x => x.Id == Id(p[2]))
                ?? throw Error("ResourceNotFound", "维护计划不存在", 404);
            Match(r.Version, value.RowVersion);
            if (p[3] == "defer")
            {
                var input = Body<DeferMaintenance>(r);
                Utc(input.Until);
                var reason = Text(input.Reason, 500);
                if (input.Until <= Now || input.Until <= value.NextDueAt
                    || await MaintenanceWorks.AnyAsync(x => x.ScheduleId == value.Id && x.Status == "InProgress"
                        || x.ScheduleId == value.Id && x.Status == "Completed"))
                    throw Error("MaintenanceDeferConflict", "只能延期到更晚的未来时间，且不能有已开始或待复机维护");
                var prior = value.NextDueAt;
                value.NextDueAt = input.Until;
                return await Saved(value, "MaintenancePlanDeferred", new { prior, input.Until, reason });
            }
            if (p[3] == "toggle")
            {
                var input = Body<ToggleMaintenancePlan>(r);
                var reason = Text(input.Reason, 500);
                value.IsActive = input.IsActive;
                return await Saved(value, "MaintenancePlanToggled", new { input.IsActive, reason });
            }
        }
        if (p[1] == "maintenance" && p.Length == 2 && r.Method == "POST")
        {
            var input = Body<CreateMaintenanceWork>(r);
            var equipmentId = input.EquipmentId;
            var toolId = input.ToolId;
            if (input.ScheduleId != null)
            {
                var plan = await Plans.SingleOrDefaultAsync(x => x.Id == input.ScheduleId && x.IsActive)
                    ?? throw Error("ResourceNotFound", "有效维护计划不存在", 404);
                if (equipmentId != null && equipmentId != plan.EquipmentId || toolId != null && toolId != plan.ToolId)
                    throw Error("MaintenanceTargetConflict", "工单与计划的目标不一致");
                equipmentId = plan.EquipmentId;
                toolId = plan.ToolId;
                if (await MaintenanceWorks.AnyAsync(x => x.ScheduleId == plan.Id
                    && x.Status != "Released" && x.Status != "Cancelled"))
                    throw Error("MaintenanceAlreadyOpen", "计划已有未关闭维护工单");
            }
            await ValidateTarget(equipmentId, toolId);
            await Assignee(input.AssignedToId);
            Utc(input.ScheduledAt);
            var value = new MaintenanceEvent
            {
                Title = Text(input.Title, 200),
                EventType = Text(input.EventType, 32),
                Status = "Planned",
                ScheduledAt = input.ScheduledAt,
                ScheduleId = input.ScheduleId,
                EquipmentId = equipmentId,
                ToolId = toolId,
                AssignedToId = input.AssignedToId
            };
            db.Add(value);
            return await Saved(value, "MaintenanceCreated", input, true);
        }
        if (p[1] == "maintenance" && p.Length == 4 && r.Method == "POST")
        {
            var value = await MaintenanceWorks.SingleOrDefaultAsync(x => x.Id == Id(p[2]))
                ?? throw Error("ResourceNotFound", "维护工单不存在", 404);
            Match(r.Version, value.RowVersion);
            var equipmentId = await ValidateTarget(value.EquipmentId, value.ToolId);
            if (p[3] == "defer")
            {
                var input = Body<DeferMaintenance>(r);
                Utc(input.Until);
                var reason = Text(input.Reason, 500);
                if (value.Status != "Planned" || input.Until <= Now || input.Until <= value.ScheduledAt)
                    throw Error("MaintenanceStateConflict", "只允许将未开始工单延期到更晚的未来时间");
                var prior = value.ScheduledAt;
                value.ScheduledAt = input.Until;
                return await Saved(value, "MaintenanceDeferred", new { prior, input.Until, reason });
            }
            var note = Text(Body<ReasonRequest>(r).Reason, 500);
            if (p[3] == "start" && value.Status == "Planned")
            {
                if (equipmentId != null)
                    await IdleResource(equipmentId.Value);
                value.Status = "InProgress";
                value.StartedAt = Now;
            }
            else if (p[3] == "complete" && value.Status == "InProgress")
            {
                value.Status = "Completed";
                value.CompletedAt = Now;
                value.ResolutionNotes = note;
            }
            else if (p[3] == "release" && value.Status == "Completed")
            {
                value.Status = "Released";
                value.ReleasedAt = Now;
                value.ReleasedById = actor.UserId;
                if (value.ScheduleId != null)
                {
                    var plan = await Plans.SingleAsync(x => x.Id == value.ScheduleId);
                    plan.LastExecutedAt = Now;
                    plan.NextDueAt = plan.Frequency == "Hours" ? Now.AddHours(plan.IntervalValue)
                        : Now.AddDays(plan.IntervalValue);
                    Touch(plan);
                }
            }
            else if (p[3] == "cancel" && value.Status == "Planned")
                value.Status = "Cancelled";
            else
                throw Error("MaintenanceStateConflict", "维护状态不允许该操作");
            return await Saved(value, "Maintenance" + p[3], new { reason = note });
        }
        throw Error("ResourceNotFound", "接口不存在", 404);
    }
}
