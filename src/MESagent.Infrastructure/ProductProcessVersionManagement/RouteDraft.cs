using System.Text.Json;
using MESagent.Application;
using MESagent.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MESagent.Infrastructure.ProductProcessVersionManagement;

public sealed partial class ManufacturingBackend
{
    private async Task<OrganizationIdentityAccessControlResult> SaveRouteDraft(
        OrganizationIdentityAccessControlRequest request, Guid id)
    {
        var route = await Get<RouteVersion>(id);
        Match(request.Version, route.RowVersion);
        if (route.LifecycleStatus != "Draft") throw Error("路线已冻结", 409);
        Fields(request.Body, ["routeId", "version", "effectiveFrom", "effectiveTo", "changeReason", "steps"]);
        var steps = request.Body.GetProperty("steps");
        if (steps.ValueKind != JsonValueKind.Array || steps.GetArrayLength() > 500) throw Error("步骤必须为不超过 500 项的数组");
        var originals = await db.Set<RouteStep>().Where(x => x.RouteVersionId == id).ToListAsync();
        var before = JsonSerializer.SerializeToElement(originals, Json);
        var requestedIds = steps.EnumerateArray().Where(x => x.TryGetProperty("id",
            out _)).Select(x => x.GetProperty("id").GetGuid()).ToArray();
        if (requestedIds.Distinct().Count() != requestedIds.Length
            || requestedIds.Any(x => originals.All(y => y.Id != x)))
            throw Error("步骤 ID 重复或不属于当前路线");
        foreach (var removed in originals.Where(x => !requestedIds.Contains(x.Id)))
        {
            if (await db.Set<EngineeringDocument>().AnyAsync(x => x.EntityType == "RouteStep"
                && x.EntityId == removed.Id))
                throw Error("已关联指导书的步骤不能移除", 409);
            db.RemoveRange(await db.Set<ChecklistItem>().Where(x => x.RouteStepId == removed.Id).ToListAsync());
            db.RemoveRange(await db.Set<OutputDefinition>().Where(x => x.RouteStepId == removed.Id).ToListAsync());
            db.Remove(removed);
        }
        // Move retained steps out of the destination range before exchanging sequence numbers.
        var maximum = originals.Select(x => x.SequenceNo).Concat(steps.EnumerateArray().Select(x => x.GetProperty("sequenceNo").GetInt32())).DefaultIfEmpty().Max();
        if ((long)maximum + originals.Count >= int.MaxValue) throw Error("步骤顺序号过大");
        foreach (var retained in originals.Where(x => requestedIds.Contains(x.Id))) retained.SequenceNo = ++maximum;
        await db.SaveChangesAsync();
        var root = request.Body.Deserialize<Dictionary<string, JsonElement>>(Json)!;
        root.Remove("steps");
        await Engineering(request with { Body = JsonSerializer.SerializeToElement(root) }, ["engineering",
            "route-versions", id.ToString()]);
        foreach (var input in steps.EnumerateArray())
        {
            var body = input.Deserialize<Dictionary<string, JsonElement>>(Json)!;
            var existing = body.Remove("id", out var stepId) ? originals.Single(x => x.Id == stepId.GetGuid()) : null;
            var hasChecklist = body.Remove("checklistItems", out var checklist);
            var hasOutputs = body.Remove("outputDefinitions", out var outputs);
            body["routeVersionId"] = JsonSerializer.SerializeToElement(id);
            var path = existing is null ? "engineering/route-steps" : "engineering/route-steps/" + existing.Id;
            var saved = await Engineering(request with
            {
                Path = path, Method = existing is null ? "POST" : "PUT",
                Version = existing is null ? null : Tag(existing.RowVersion),
                    Body = JsonSerializer.SerializeToElement(body)
            }, path.Split('/'));
            var savedId = JsonSerializer.SerializeToElement(saved.Data, Json).GetProperty("id").GetGuid();
            if (hasChecklist) await SaveDefinitions<ChecklistItem>(request, savedId, checklist, "checklist-items");
            if (hasOutputs) await SaveDefinitions<OutputDefinition>(request, savedId, outputs, "output-definitions");
        }
        route.UpdatedAt = Now;
        Audit(route, "RouteDraftSaved", before);
        await db.SaveChangesAsync();
        return new(View(route), Tag(route.RowVersion));
    }

    private async Task SaveDefinitions<T>(OrganizationIdentityAccessControlRequest request,
        Guid stepId, JsonElement inputs, string kind) where T : class
    {
        if (inputs.ValueKind != JsonValueKind.Array
            || inputs.GetArrayLength() > 200) throw Error("步骤定义必须为不超过 200 项的数组");
        var existing = await db.Set<T>().Where(x => EF.Property<Guid>(x, "RouteStepId") == stepId).ToListAsync();
        Guid Identity(T item) => (Guid)typeof(T).GetProperty("Id")!.GetValue(item)!;
        var ids = inputs.EnumerateArray().Where(x => x.TryGetProperty("id",
            out _)).Select(x => x.GetProperty("id").GetGuid()).ToArray();
        if (ids.Distinct().Count() != ids.Length
            || ids.Any(x => existing.All(y => Identity(y) != x))) throw Error("定义 ID 无效");
        db.RemoveRange(existing.Where(x => !ids.Contains(Identity(x))));
        await db.SaveChangesAsync();
        foreach (var input in inputs.EnumerateArray())
        {
            var body = input.Deserialize<Dictionary<string, JsonElement>>(Json)!;
            var current = body.Remove("id",
                out var value) ? existing.Single(x => Identity(x) == value.GetGuid()) : null;
            body["routeStepId"] = JsonSerializer.SerializeToElement(stepId);
            var path = "engineering/" + kind + (current is null ? "" : "/" + Identity(current));
            await Engineering(request with
            {
                Path = path, Method = current is null ? "POST" : "PUT",
                Version = current is null ? null : Tag((uint)typeof(T).GetProperty("RowVersion")!.GetValue(current)!),
                Body = JsonSerializer.SerializeToElement(body)
            }, path.Split('/'));
        }
    }
}
