using MESagent.Application;
using MESagent.Domain;
using MESagent.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MESagent.Infrastructure.DispatchAndProductionExecution;

public sealed partial class ExecutionBackend
{
    private async Task<OrganizationIdentityAccessControlResult> EquipmentCommand(
        OrganizationIdentityAccessControlRequest r, string[] p)
    {
        if (p.Length == 2 && r.Method == "POST")
        {
            object created;
            if (p[1] is "types" or "groups")
            {
                var input = Body<CreateEquipmentReference>(r);
                var code = Text(input.Code, 64);
                var name = Text(input.Name, 200);
                created = p[1] == "types" ? new EquipmentType { Code = code, Name = name }
                    : new EquipmentGroup { Code = code, Name = name, FabId = fabId };
            }
            else if (p[1] == "tools")
            {
                var input = Body<CreateExecutionEquipment>(r);
                if (!await db.Set<Area>().AnyAsync(x => x.Id == input.AreaId && x.FabId == fabId && x.IsActive)
                    || !await db.Set<EquipmentGroup>().AnyAsync(x => x.Id == input.EquipmentGroupId
                        && x.FabId == fabId && x.IsActive)
                    || !await db.Set<EquipmentType>().AnyAsync(x => x.Id == input.EquipmentTypeId && x.IsActive)
                    || input.LineId != null && !await db.Set<Line>().AnyAsync(x => x.Id == input.LineId
                        && x.AreaId == input.AreaId && x.IsActive))
                    throw Error("InvalidEquipmentScope", "设备区域、产线、组或类型不存在、停用或归属不匹配", 422);
                var equipment = new Equipment
                {
                    Code = Text(input.Code, 64), Name = Text(input.Name, 200), AreaId = input.AreaId,
                    LineId = input.LineId, EquipmentGroupId = input.EquipmentGroupId,
                    EquipmentTypeId = input.EquipmentTypeId, OperationalState = "Idle"
                };
                created = equipment;
                db.Add(new EquipmentState
                {
                    EquipmentId = equipment.Id, State = "Idle", StartedAt = Now, Source = "Manual"
                });
            }
            else
                throw Error("ResourceNotFound", "接口不存在", 404);
            db.Add(created);
            Record(created, "EquipmentCreated", new { });
            await db.SaveChangesAsync();
            var values = View(created);
            return new(values, (string?)values["etag"], 201);
        }
        if (p.Length is 3 or 4 && p[1] == "tools")
        {
            var equipment = await Equipment(Id(p[2]));
            Match(r.Version, equipment.RowVersion);
            if (p.Length == 3 && r.Method == "PUT")
            {
                var input = Body<UpdateExecutionEquipment>(r);
                if (!input.IsActive && await db.Set<ProcessRun>().AnyAsync(x => x.EquipmentId == equipment.Id
                    && x.Status == "Running"))
                    throw Error("EquipmentOccupied", "加工中设备不能停用，请先受控收尾");
                equipment.Name = Text(input.Name, 200);
                equipment.IsActive = input.IsActive;
                Record(equipment, "EquipmentUpdated", input);
            }
            else if (p.Length == 4 && p[3] == "state" && r.Method == "POST")
            {
                var input = Body<ChangeExecutionEquipmentState>(r);
                if (input.State is not ("Idle" or "Down" or "Maintenance" or "Offline"))
                    throw Error("InvalidEquipmentState", "状态应为 Idle、Down、Maintenance 或 Offline", 422);
                var reason = Text(input.Reason, 500);
                if (input.State == equipment.OperationalState)
                    throw Error("EquipmentStateConflict", "设备已处于该人工状态");
                var states = await db.Set<EquipmentState>().Where(x => x.EquipmentId == equipment.Id
                    && x.EndedAt == null).ToListAsync();
                var time = LotRules.NextEventTime(Now, states.Select(x => x.StartedAt));
                foreach (var state in states)
                    state.EndedAt = time;
                // Close the prior interval before inserting its replacement (exclusion constraint).
                await db.SaveChangesAsync();
                db.Add(new EquipmentEvent
                {
                    EquipmentId = equipment.Id, EventType = "ManualState", StateFrom = equipment.OperationalState,
                    StateTo = input.State, EventTimestamp = time, ReceivedAt = time,
                    CorrelationId = actor.CorrelationId, Payload = System.Text.Json.JsonSerializer.Serialize(
                        new { reason, actor.UserId }, Json)
                });
                db.Add(new EquipmentState
                {
                    EquipmentId = equipment.Id, State = input.State, StartedAt = time, Source = "Manual"
                });
                equipment.OperationalState = input.State;
                Record(equipment, "EquipmentStateChanged", input);
            }
            else
                throw Error("ResourceNotFound", "接口不存在", 404);
            Touch(equipment);
            await db.SaveChangesAsync();
            return new(View(equipment), Tag(equipment.RowVersion));
        }
        throw Error("ResourceNotFound", "接口不存在", 404);
    }
}
