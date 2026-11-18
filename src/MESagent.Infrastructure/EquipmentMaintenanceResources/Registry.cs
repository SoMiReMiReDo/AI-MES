using MESagent.Application;
using MESagent.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MESagent.Infrastructure.EquipmentMaintenanceResources;

public sealed partial class EquipmentBackend
{
    private async Task ValidateLocation(Guid areaId, Guid groupId, Guid typeId, Guid? lineId)
    {
        if (!await db.Set<Area>().AnyAsync(x => x.Id == areaId && x.FabId == fabId && x.IsActive)
            || !await db.Set<EquipmentGroup>().AnyAsync(x => x.Id == groupId && x.FabId == fabId && x.IsActive)
            || !await db.Set<EquipmentType>().AnyAsync(x => x.Id == typeId && x.IsActive)
            || lineId != null && !await db.Set<Line>().AnyAsync(x => x.Id == lineId && x.AreaId == areaId && x.IsActive))
            throw Error("InvalidEquipmentScope", "区域、产线、设备组或类型停用、不存在或归属不符", 422);
    }

    private async Task IdleResource(Guid equipmentId)
    {
        if (await db.Set<ProcessRun>().AnyAsync(x => x.EquipmentId == equipmentId && x.Status == "Running"))
            throw Error("EquipmentOccupied", "设备正在加工，请先按 F5 受控收尾");
    }

    private async Task<OrganizationIdentityAccessControlResult> Registry(
        OrganizationIdentityAccessControlRequest r, string[] p)
    {
        if (p.Length == 2 && r.Method == "POST")
        {
            object value;
            if (p[1] is "types" or "groups")
            {
                var input = Body<CreateEquipmentReference>(r);
                value = p[1] == "types"
                    ? new EquipmentType { Code = Text(input.Code, 64), Name = Text(input.Name, 200) }
                    : new EquipmentGroup { Code = Text(input.Code, 64), Name = Text(input.Name, 200), FabId = fabId };
            }
            else if (p[1] == "tools")
            {
                var input = Body<CreateExecutionEquipment>(r);
                await ValidateLocation(input.AreaId, input.EquipmentGroupId, input.EquipmentTypeId, input.LineId);
                var equipment = new Equipment
                {
                    Code = Text(input.Code, 64),
                    Name = Text(input.Name, 200),
                    AreaId = input.AreaId,
                    LineId = input.LineId,
                    EquipmentGroupId = input.EquipmentGroupId,
                    EquipmentTypeId = input.EquipmentTypeId,
                    OperationalState = "Idle"
                };
                value = equipment;
                db.Add(new EquipmentState
                {
                    EquipmentId = equipment.Id,
                    State = "Idle",
                    StartedAt = Now,
                    Source = "Manual"
                });
            }
            else
            {
                var input = Body<RegisterEquipmentChamber>(r);
                var equipmentId = input.EquipmentId;
                await Equipment(equipmentId);
                value = new Chamber
                {
                    EquipmentId = equipmentId,
                    Code = Text(input.Code, 64),
                    Name = Text(input.Name, 200)
                };
            }
            db.Add(value);
            return await Saved(value, "Registered", r.Body, true);
        }
        if (p.Length is 3 or 4 && p[1] == "tools")
        {
            var value = await Equipment(Id(p[2]));
            Match(r.Version, value.RowVersion);
            if (p.Length == 4 && p[3] == "state" && r.Method == "POST")
            {
                var input = Body<ChangeEquipmentState>(r);
                await SetState(value, input, "Manual");
                return await Saved(value, "StateChanged", input);
            }
            if (p.Length != 3 || r.Method != "PUT")
                throw Error("ResourceNotFound", "接口不存在", 404);
            // Preserve the F5 update contract while accepting full F7 location editing.
            var inputEdit = r.Body.TryGetProperty("areaId", out _) ? Body<EditEquipment>(r) : null;
            var simple = inputEdit == null ? Body<UpdateExecutionEquipment>(r) : null;
            await IdleResource(value.Id);
            if (inputEdit != null)
            {
                await ValidateLocation(inputEdit.AreaId, inputEdit.EquipmentGroupId,
                    inputEdit.EquipmentTypeId, inputEdit.LineId);
                if ((value.AreaId != inputEdit.AreaId || value.LineId != inputEdit.LineId
                        || value.EquipmentTypeId != inputEdit.EquipmentTypeId
                        || value.EquipmentGroupId != inputEdit.EquipmentGroupId)
                    && (await db.Set<ProcessRun>().AnyAsync(x => x.EquipmentId == value.Id)
                        || await db.Set<RecipeApplicability>().AnyAsync(x => x.EquipmentId == value.Id)))
                    throw Error("EquipmentIdentityFrozen", "设备已有工艺或加工关联，归属及类型不可改写");
                value.AreaId = inputEdit.AreaId;
                value.LineId = inputEdit.LineId;
                value.EquipmentTypeId = inputEdit.EquipmentTypeId;
                value.EquipmentGroupId = inputEdit.EquipmentGroupId;
                value.Description = Text(inputEdit.Description, 2000, false);
            }
            value.Name = Text(inputEdit?.Name ?? simple!.Name, 200);
            value.IsActive = inputEdit?.IsActive ?? simple!.IsActive;
            return await Saved(value, "Updated", r.Body);
        }
        if (p.Length == 3 && r.Method == "PUT")
        {
            var input = Body<UpdateEquipmentReference>(r);
            object value;
            if (p[1] == "types")
            {
                var row = await db.Set<EquipmentType>().SingleOrDefaultAsync(x => x.Id == Id(p[2]))
                    ?? throw Error("ResourceNotFound", "类型不存在", 404);
                Match(r.Version, row.RowVersion);
                row.Name = Text(input.Name, 200);
                row.Description = Text(input.Description, 2000, false);
                row.IsActive = input.IsActive;
                value = row;
            }
            else if (p[1] == "groups")
            {
                var row = await db.Set<EquipmentGroup>().SingleOrDefaultAsync(x => x.Id == Id(p[2]) && x.FabId == fabId)
                    ?? throw Error("ResourceNotFound", "设备组不存在", 404);
                Match(r.Version, row.RowVersion);
                row.Name = Text(input.Name, 200);
                row.Description = Text(input.Description, 2000, false);
                row.IsActive = input.IsActive;
                value = row;
            }
            else
            {
                var row = await db.Set<Chamber>().SingleOrDefaultAsync(x => x.Id == Id(p[2]))
                    ?? throw Error("ResourceNotFound", "腔体不存在", 404);
                await Equipment(row.EquipmentId);
                await IdleResource(row.EquipmentId);
                Match(r.Version, row.RowVersion);
                row.Name = Text(input.Name, 200);
                row.Description = Text(input.Description, 2000, false);
                row.IsActive = input.IsActive;
                value = row;
            }
            return await Saved(value, "Updated", input);
        }
        throw Error("ResourceNotFound", "接口不存在", 404);
    }
}
