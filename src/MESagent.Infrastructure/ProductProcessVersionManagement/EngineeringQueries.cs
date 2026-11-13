using System.Text.Json;
using MESagent.Application;
using MESagent.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MESagent.Infrastructure.ProductProcessVersionManagement;

public sealed partial class ManufacturingBackend
{
    private async Task<OrganizationIdentityAccessControlResult?> EngineeringQuery(OrganizationIdentityAccessControlRequest r, string[] p)
    {
        if (p.Length == 3 && p[1] == "route-versions" && r.Method == "GET" && Guid.TryParse(p[2], out var routeId))
        {
            var route = await Get<RouteVersion>(routeId);
            var result = (Dictionary<string, JsonElement>)View(route);
            var steps = await db.Set<RouteStep>().Where(x => x.RouteVersionId == routeId).OrderBy(x => x.SequenceNo).ToListAsync();
            var definitions = new List<object>();
            foreach (var step in steps)
            {
                var view = (Dictionary<string, JsonElement>)View(step);
                view["checklistItems"] = JsonSerializer.SerializeToElement(await db.Set<ChecklistItem>().Where(x => x.RouteStepId == step.Id).OrderBy(x => x.SortOrder).ToListAsync(), Json);
                view["outputDefinitions"] = JsonSerializer.SerializeToElement(await db.Set<OutputDefinition>().Where(x => x.RouteStepId == step.Id).OrderBy(x => x.SortOrder).ToListAsync(), Json);
                definitions.Add(view);
            }
            result["steps"] = JsonSerializer.SerializeToElement(definitions, Json);
            return new(result, Tag(route.RowVersion));
        }
        if (p.Length == 2 && p[1] == "equipment-options" && r.Method == "GET")
        {
            var rows = await (from equipment in db.Set<Equipment>()
                              join area in db.Set<Area>() on equipment.AreaId equals area.Id
                              where area.FabId == fabId
                              select new { equipment.Id, equipment.Code, equipment.Name,
                                  equipment.EquipmentTypeId }).ToListAsync();
            return new(new { items = rows, total = rows.Count });
        }
        if (p.Length == 2 && p[1] == "equipment-type-options" && r.Method == "GET")
        {
            var rows = await db.Set<EquipmentType>().Select(x => new { x.Id, x.Code, x.Name }).ToListAsync();
            return new(new { items = rows, total = rows.Count });
        }
        if (p.Length == 2 && p[1] == "recipe-candidates" && r.Method == "GET")
        {
            if (!r.Query.TryGetValue("routeStepId", out var stepText) || !Guid.TryParse(stepText, out var step)
                || !r.Query.TryGetValue("equipmentId", out var equipmentText) || !Guid.TryParse(equipmentText,
                    out var equipment))
                throw Error("必须提供步骤和设备");
            var device = await Get<Equipment>(equipment);
            if ((await Get<Area>(device.AreaId)).FabId != fabId)
                throw Error("设备不可访问", 404);
            await Get<RouteStep>(step);
            var at = r.Query.TryGetValue("at", out var time) ? DateTime.Parse(time).ToUniversalTime() : Now;
            var rows = await (from a in db.Set<RecipeApplicability>()
                              join v in db.Set<RecipeVersion>() on a.RecipeVersionId equals v.Id
                              join recipe in db.Set<Recipe>() on v.RecipeId equals recipe.Id
                              where a.RouteStepId == step && a.EquipmentId == equipment
                                  && a.EffectiveFrom <= at && (a.EffectiveTo == null || a.EffectiveTo > at)
                                  && v.LifecycleStatus == "Released" && recipe.IsActive
                              select new { applicability = a, version = v }).ToListAsync();
            return new(rows);
        }
        if (p.Length != 4 || !Guid.TryParse(p[2], out var id))
            return null;
        if (p[1] == "product-revisions" && p[3] == "impact" && r.Method == "GET")
            return new(new
            {
                workOrders = await Page(db.Set<WorkOrder>().Where(x => x.ProductRevisionId == id
                    && x.FabId == fabId), r),
                lots = await Page(db.Set<Lot>().Where(x => x.ProductRevisionId == id && x.FabId == fabId), r)
            });
        if (p[3] != "copy" || r.Method != "POST")
            return null;
        Fields(r.Body, ["version", "reason"]);
        var version = Text(r.Body, "version");
        if (string.IsNullOrWhiteSpace(version) || version.Length > 64)
            throw Error("需要新版本号");
        object entity;
        if (p[1] == "route-versions")
        {
            var source = await Get<RouteVersion>(id);
            var target = new RouteVersion { RouteId = source.RouteId, Version = version, LifecycleStatus = "Draft",
                ChangeReason = Text(r.Body, "reason") };
            db.Add(target);
            foreach (var step in await db.Set<RouteStep>().AsNoTracking().Where(x => x.RouteVersionId == id).ToListAsync())
            {
                var old = step.Id;
                step.Id = Guid.CreateVersion7();
 step.RouteVersionId = target.Id;
 step.RowVersion = 0;
                db.Add(step);
                foreach (var doc in await db.Set<EngineeringDocument>().AsNoTracking().Where(x => x.EntityType == "RouteStep" && x.EntityId == old).ToListAsync())
                {
                    doc.Id = Guid.CreateVersion7();
 doc.EntityId = step.Id;
 doc.RowVersion = 0;
                    doc.CreatedAt = Now;
 doc.UpdatedAt = Now;
                    db.Add(doc);
                }
                foreach (var item in await db.Set<ChecklistItem>().AsNoTracking().Where(x => x.RouteStepId == old).ToListAsync())
                {
                    item.Id = Guid.CreateVersion7();
 item.RouteStepId = step.Id;
 item.RowVersion = 0;
 db.Add(item);
                }
                foreach (var item in await db.Set<OutputDefinition>().AsNoTracking().Where(x => x.RouteStepId == old).ToListAsync())
                {
                    item.Id = Guid.CreateVersion7();
 item.RouteStepId = step.Id;
 item.RowVersion = 0;
 db.Add(item);
                }
            }
            entity = target;
        }
        else if (p[1] == "product-revisions")
        {
            var source = await Get<ProductRevision>(id);
            entity = new ProductRevision { ProductId = source.ProductId, RevisionCode = version,
                RouteVersionId = source.RouteVersionId, LifecycleStatus = "Draft", Description = source.Description,
                    ChangeReason = Text(r.Body, "reason") };
            db.Add(entity);
        }
        else if (p[1] == "recipe-versions")
        {
            var source = await Get<RecipeVersion>(id);
            entity = new RecipeVersion { RecipeId = source.RecipeId, Version = version,
                Parameters = source.Parameters, Checksum = source.Checksum, LifecycleStatus = "Draft" };
            db.Add(entity);
        }
        else return null;
        Audit(entity, "Copied", new { sourceId = id });
        await db.SaveChangesAsync();
        return new(View(entity), Status: 201);
    }
}
