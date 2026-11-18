using System.Text.Json;
using MESagent.Application;
using MESagent.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MESagent.Infrastructure.EquipmentMaintenanceResources;

public sealed partial class EquipmentBackend
{
    private async Task ValidateClassification(Guid? reasonId, Guid? shiftId, Guid? lineId)
    {
        if (reasonId != null && !await db.Set<ReasonCode>().AnyAsync(x => x.Id == reasonId
            && x.IsActive && x.Category == "Downtime"))
            throw Error("InvalidReason", "请选择启用的停机原因", 422);
        if (shiftId != null && !await db.Set<Shift>().AnyAsync(x => x.Id == shiftId && x.FabId == fabId
            && x.IsActive && (x.LineId == null || x.LineId == lineId)))
            throw Error("InvalidShift", "班次不存在、停用或不属于该设备产线", 422);
    }

    private async Task SetState(Equipment value, ChangeEquipmentState input, string source)
    {
        if (input.State is not ("Idle" or "Down" or "Maintenance" or "Offline"))
            throw Error("InvalidEquipmentState", "状态应为 Idle、Down、Maintenance 或 Offline", 422);
        var reason = Text(input.Reason, 500);
        if (value.OperationalState == input.State)
            throw Error("EquipmentStateConflict", "设备已处于该状态");
        await ValidateClassification(input.ReasonCodeId, input.ShiftId, value.LineId);
        db.Add(new EquipmentEvent
        {
            EquipmentId = value.Id,
            EventType = "State",
            StateFrom = value.OperationalState,
            StateTo = input.State,
            EventTimestamp = Now,
            ReceivedAt = Now,
            Source = source,
            CorrelationId = actor.CorrelationId,
            Payload = JsonSerializer.Serialize(new { reason, actor.UserId }, Json)
        });
        value.OperationalState = input.State;
        await EquipmentResources.RefreshState(db, value, Now, actor.UserId, source,
            input.ReasonCodeId, input.ShiftId, reason);
    }

    internal async Task<EquipmentEvent> ApplySimulatedEvent(
        Equipment equipment,
        SimulateEquipmentEvent input,
        string source,
        string sourceId,
        string payload,
        Guid? connectionId = null)
    {
        if (input.OccurredAt.Kind != DateTimeKind.Utc || input.OccurredAt > Now.AddSeconds(5))
            throw Error("InvalidEventTime", "事件时间必须为 UTC 且不能晚于接收时间", 422);
        // PostgreSQL timestamps have microsecond precision.
        var occurredAt = input.OccurredAt.AddTicks(-(input.OccurredAt.Ticks % 10));
        Text(input.Kind, 32);
        Text(input.State, 32);
        Text(input.Reason, 500);
        var latest = await db.Set<EquipmentEvent>().Where(x => x.EquipmentId == equipment.Id
            && x.ProcessingStatus == "Applied" && (x.EventType == input.Kind
                || input.Kind == "State" && x.EventType == "ManualState"))
            .MaxAsync(x => (DateTime?)x.EventTimestamp);
        var from = input.Kind == "Connection" ? equipment.CommunicationState : equipment.OperationalState;
        var invalid = input.Kind switch
        {
            "Connection" => input.State is not ("Online" or "Offline" or "Unknown"),
            "State" => input.State is not ("Idle" or "Down" or "Maintenance" or "Offline"),
            _ => true
        };
        var rejection = invalid ? "无效事件类型或状态"
            : occurredAt < equipment.CreatedAt ? "事件早于设备登记时间"
            : latest != null && occurredAt <= latest ? "乱序或重复时点，不覆盖当前状态"
            : from == input.State ? "当前状态相同，无需转换" : null;
        if (rejection == null)
        {
            await ValidateClassification(input.ReasonCodeId, input.ShiftId, equipment.LineId);
            if (input.Kind == "Connection")
                equipment.CommunicationState = input.State;
            else
            {
                equipment.OperationalState = input.State;
                await EquipmentResources.RefreshState(db, equipment, Now, actor.UserId, "Simulator",
                    input.ReasonCodeId, input.ShiftId, input.Reason);
            }
            Touch(equipment);
        }
        var value = new EquipmentEvent
        {
            EquipmentId = equipment.Id,
            MachineConnectionId = connectionId,
            Source = source,
            SourceEventId = sourceId,
            EventType = input.Kind,
            StateFrom = from,
            StateTo = input.State,
            EventTimestamp = occurredAt,
            ReceivedAt = Now,
            IsSimulated = true,
            ProcessingStatus = rejection == null ? "Applied" : "Rejected",
            RejectionReason = rejection,
            CorrelationId = actor.CorrelationId,
            Payload = payload
        };
        db.Add(value);
        return value;
    }

    private async Task<OrganizationIdentityAccessControlResult> Events(
        OrganizationIdentityAccessControlRequest r, string[] p)
    {
        if (p.Length == 2 && r.Method == "POST" && p[1] == "reasons")
        {
            var input = Body<CreateEquipmentReference>(r);
            var value = new ReasonCode
            {
                Code = Text(input.Code, 64),
                Name = Text(input.Name, 200),
                Category = "Downtime"
            };
            db.Add(value);
            return await Saved(value, "DowntimeReasonCreated", input, true);
        }
        if (p.Length == 2 && r.Method == "POST" && p[1] == "shifts")
        {
            var input = Body<RegisterEquipmentShift>(r);
            if (input.StartTime == input.EndTime)
                throw Error("InvalidShift", "班次起止时间不能相同", 422);
            var value = new Shift
            {
                Code = Text(input.Code, 64),
                Name = Text(input.Name, 200),
                FabId = fabId,
                StartTime = input.StartTime,
                EndTime = input.EndTime,
                DaysOfWeek = "[1,2,3,4,5,6,7]"
            };
            db.Add(value);
            return await Saved(value, "ShiftCreated", input, true);
        }
        if (p.Length == 3 && r.Method == "PUT" && p[1] == "reasons")
        {
            var input = Body<UpdateEquipmentReference>(r);
            var row = await db.Set<ReasonCode>().SingleOrDefaultAsync(x => x.Id == Id(p[2]) && x.Category == "Downtime")
                ?? throw Error("ResourceNotFound", "原因不存在", 404);
            Match(r.Version, row.RowVersion);
            row.Name = Text(input.Name, 200);
            row.IsActive = input.IsActive;
            return await Saved(row, "DowntimeReasonUpdated", input);
        }
        if (p.Length == 3 && p[1] == "events" && r.Method == "POST")
        {
            var equipment = await Equipment(Id(p[2]));
            var input = Body<SimulateEquipmentEvent>(r);
            var source = Text(input.Source, 64);
            var sourceId = Text(input.SourceEventId, 128);
            Text(input.Kind, 32);
            Text(input.State, 32);
            var hash = Canonical(r.Body);
            var prior = await db.Set<EquipmentEvent>().SingleOrDefaultAsync(x => x.EquipmentId == equipment.Id
                && x.MachineConnectionId == null && x.Source == source && x.SourceEventId == sourceId);
            if (prior != null)
            {
                if (prior.Payload == null || Canonical(JsonSerializer.Deserialize<JsonElement>(prior.Payload)) != hash)
                    throw Error("SourceEventConflict", "来源事件编号已用于不同内容");
                return new(View(prior), Replayed: true);
            }
            Match(r.Version, equipment.RowVersion);
            var value = await ApplySimulatedEvent(equipment, input, source, sourceId, hash);
            return await Saved(value, "SimulatedEventReceived", input, true);
        }
        if (p.Length == 2 && p[1] == "downtimes" && r.Method == "POST")
        {
            var input = Body<RegisterDowntime>(r);
            var state = await db.Set<EquipmentState>().SingleOrDefaultAsync(x => x.Id == input.EquipmentStateId)
                ?? throw Error("ResourceNotFound", "状态区间不存在", 404);
            var equipment = await Equipment(state.EquipmentId);
            Match(r.Version, equipment.RowVersion);
            if (state.State != "Down" || await db.Set<Downtime>().AnyAsync(x => x.EquipmentStateId == state.Id))
                throw Error("DowntimeConflict", "只能为尚未登记的 Down 区间补登记停机");
            await ValidateClassification(input.ReasonCodeId, input.ShiftId, equipment.LineId);
            var value = new Downtime
            {
                EquipmentId = equipment.Id,
                EquipmentStateId = state.Id,
                LineId = equipment.LineId,
                StartedAt = state.StartedAt,
                EndedAt = state.EndedAt,
                ReportedById = actor.UserId,
                ReasonCodeId = input.ReasonCodeId,
                ShiftId = input.ShiftId,
                Notes = Text(input.Notes, 2000, false),
                NeedsReason = input.ReasonCodeId == null,
                ClassifiedAt = input.ReasonCodeId == null ? null : Now,
                ClassifiedById = input.ReasonCodeId == null ? null : actor.UserId
            };
            db.Add(value);
            return await Saved(value, "DowntimeRegistered", input, true);
        }
        if (p.Length == 4 && p[1] == "downtimes" && p[3] == "classify" && r.Method == "POST")
        {
            var value = await db.Set<Downtime>().SingleOrDefaultAsync(x => x.Id == Id(p[2]))
                ?? throw Error("ResourceNotFound", "停机不存在", 404);
            var equipment = await Equipment(value.EquipmentId);
            Match(r.Version, value.RowVersion);
            var input = Body<ClassifyDowntime>(r);
            await ValidateClassification(input.ReasonCodeId, input.ShiftId, equipment.LineId);
            value.ReasonCodeId = input.ReasonCodeId;
            value.ShiftId = input.ShiftId;
            value.Notes = Text(input.Notes, 2000);
            value.NeedsReason = false;
            value.ClassifiedAt = Now;
            value.ClassifiedById = actor.UserId;
            return await Saved(value, "DowntimeClassified", input);
        }
        throw Error("ResourceNotFound", "接口不存在", 404);
    }
}
