using System.Text.Json;
using MESagent.Application;
using MESagent.Domain;
using MESagent.Domain.Entities;
using MESagent.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;

namespace MESagent.Infrastructure.OrganizationIdentityAccessControl;

public sealed partial class OrganizationIdentityAccessControlBackend
{
    private static readonly Dictionary<string, Type> OrgTypes = new()
    {
        ["fabs"] = typeof(Fab),
        ["areas"] = typeof(Area),
        ["lines"] = typeof(Line),
        ["locations"] = typeof(Location),
        ["workers"] = typeof(Worker),
        ["workstations"] = typeof(Workstation),
        ["terminals"] = typeof(Terminal)
    };

    private IQueryable<OrgRow> OrgQuery(string resource)
    {
        IQueryable<OrgRow> q = resource switch
        {
            "fabs" => db.Set<Fab>().Select(x => new OrgRow
            {
                Id = x.Id,
                Code = x.Code,
                Name = x.Name,
                Description = x.Description,
                IsActive = x.IsActive,
                FabId = x.Id,
                Timezone = x.Timezone,
                Version = x.RowVersion
            }),
            "areas" => db.Set<Area>().Select(x => new OrgRow
            {
                Id = x.Id,
                Code = x.Code,
                Name = x.Name,
                Description = x.Description,
                IsActive = x.IsActive,
                FabId = x.FabId,
                Version = x.RowVersion
            }),
            "workers" => db.Set<Worker>().Select(x => new OrgRow
            {
                Id = x.Id,
                Code = x.Code,
                Name = x.Name,
                Description = x.Description,
                IsActive = x.IsActive,
                FabId = x.FabId,
                Version = x.RowVersion
            }),
            "lines" => from x in db.Set<Line>()
                       join a in db.Set<Area>() on x.AreaId equals a.Id
                       select new OrgRow
                       {
                           Id = x.Id,
                           Code = x.Code,
                           Name = x.Name,
                           Description = x.Description,
                           IsActive = x.IsActive,
                           FabId = a.FabId,
                           AreaId = x.AreaId,
                           Version = x.RowVersion
                       },
            "locations" => from x in db.Set<Location>()
                           join a in db.Set<Area>() on x.AreaId equals a.Id
                           select new OrgRow
                           {
                               Id = x.Id,
                               Code = x.Code,
                               Name = x.Name,
                               Description = x.Description,
                               IsActive = x.IsActive,
                               FabId = a.FabId,
                               AreaId = x.AreaId,
                               LocationType = x.LocationType,
                               Version = x.RowVersion
                           },
            "workstations" => from x in db.Set<Workstation>()
                              join a in db.Set<Area>() on x.AreaId equals a.Id
                              select new OrgRow
                              {
                                  Id = x.Id,
                                  Code = x.Code,
                                  Name = x.Name,
                                  Description = x.Description,
                                  IsActive = x.IsActive,
                                  FabId = a.FabId,
                                  AreaId = x.AreaId,
                                  LineId = x.LineId,
                                  Version = x.RowVersion
                              },
            "terminals" => db.Set<Terminal>().Select(x => new OrgRow
            {
                Id = x.Id,
                Code = x.Code,
                Name = x.Name,
                Description = x.Description,
                IsActive = x.IsActive,
                FabId = x.FabId,
                WorkstationId = x.WorkstationId,
                DeviceUuid = x.DeviceUuid,
                LastSeenAt = x.LastSeenAt,
                Version = x.RowVersion
            }),
            _ => throw new AppException(404, "ResourceNotFound", "资源不存在")
        };
        return q.Where(x => x.FabId == options.DeploymentFabId);
    }

    private static string OrgPermission(string resource) => resource switch
    {
        "workers" => "org.workers",
        "workstations" => "org.workstations",
        "terminals" => "org.terminals",
        _ => "org.structure"
    };

    private static (int Page, int Size, string Keyword) Page(OrganizationIdentityAccessControlRequest r)
    {
        int page = r.Query.TryGetValue("page", out var p) && int.TryParse(p, out var pi) ? pi : 1;
        int size = r.Query.TryGetValue("pageSize", out var s) && int.TryParse(s, out var si) ? si : 20;
        var keyword = r.Query.GetValueOrDefault("keyword", "");
        if (page < 1 || page > 100000 || size < 1 || size > 100 || keyword.Length > 100)
            Fail(400, "ValidationFailed", "分页或搜索参数超出范围");
        return (page, size, keyword);
    }

    private async Task<OrganizationIdentityAccessControlResult> Organization(OrganizationIdentityAccessControlRequest r)
    {
        var parts = r.Path.Split('/');
        var resource = parts[1];
        if (!OrgTypes.TryGetValue(resource, out var type))
            throw new AppException(404, "ResourceNotFound", "资源不存在");
        Require(OrgPermission(resource) + (r.Method == "GET" && parts.Length < 4 ? ".read" : ".manage"));
        var q = OrgQuery(resource);
        foreach (var key in new[] { "fabId", "areaId", "lineId", "workstationId" })
        {
            if (!r.Query.TryGetValue(key, out var raw) || string.IsNullOrEmpty(raw))
                continue;
            if (!Guid.TryParse(raw, out var filter))
                throw new AppException(400, "ValidationFailed", "无效 ID");
            q = key switch
            {
                "fabId" => q.Where(x => x.FabId == filter),
                "areaId" => q.Where(x => x.AreaId == filter),
                "lineId" => q.Where(x => x.LineId == filter),
                _ => q.Where(x => x.WorkstationId == filter)
            };
        }
        if (parts.Length == 2 && r.Method == "GET")
        {
            var (page, size, keyword) = Page(r);
            if (keyword != "")
                q = q.Where(x => x.Code.Contains(keyword.ToUpper()) || x.Name.Contains(keyword));
            if (r.Query.TryGetValue("isActive", out var raw) && bool.TryParse(raw, out var active))
                q = q.Where(x => x.IsActive == active);
            return new(new
            {
                items = await q
                    .OrderBy(x => x.Code)
                    .ThenBy(x => x.Id)
                    .Skip((page - 1) * size)
                    .Take(size)
                    .ToListAsync(),
                total = await q.CountAsync(),
                page,
                pageSize = size
            });
        }
        object entity;
        object? before = null;
        string action;
        if (parts.Length == 2 && r.Method == "POST")
        {
            if (resource == "fabs")
                Fail(403, "PermissionDenied", "厂区仅通过初始化命令创建");
            var extra = resource switch
            {
                "areas" or "workers" => new[] { "fabId" },
                "lines" => ["areaId"],
                "locations" => ["areaId", "locationType"],
                "workstations" => ["areaId", "lineId"],
                "terminals" => ["fabId", "deviceUuid", "workstationId"],
                _ => Array.Empty<string>()
            };
            Fields(r.Body, ["code", "name", "description", .. extra]);
            entity = Activator.CreateInstance(type)!;
            Set(entity, "Code", OrganizationRules.Code(S(r.Body, "code")));
            Set(entity, "Name", OrganizationRules.Text(S(r.Body, "name")));
            Set(entity, "Description", Description(r.Body));
            foreach (var field in extra)
                Set(entity, char.ToUpperInvariant(field[0]) + field[1..],
                    field == "locationType" ? LocationType(r.Body) : G(r.Body, field));
            await ValidateOrgParents(entity, true);
            db.Add(entity);
            action = "Created";
        }
        else
        {
            if (parts.Length < 3 || !Guid.TryParse(parts[2], out var id))
                throw new AppException(404, "ResourceNotFound", "对象不存在");
            var row = await q.SingleOrDefaultAsync(x => x.Id == id)
                ?? throw new AppException(404, "ResourceNotFound", "对象不存在或无权访问");
            if (parts.Length == 3 && r.Method == "GET")
                return new(row, row.Etag);
            if (parts.Length == 4 && parts[3] == "deactivation-impact" && r.Method == "GET")
                return new(new { dependencies = await Dependencies(type, id) });
            entity = (await db.FindAsync(type, id))!;
            before = row;
            Version(r.Version, row.Version);
            if (parts.Length == 3 && r.Method == "PATCH")
            {
                Fields(r.Body, resource == "locations"
                    ? ["name", "description", "locationType"]
                    : resource == "workstations"
                        ? ["name", "description", "lineId"]
                        : ["name", "description"]);
                if (r.Body.TryGetProperty("name", out _))
                    Set(entity, "Name", OrganizationRules.Text(S(r.Body, "name")));
                if (r.Body.TryGetProperty("description", out _))
                    Set(entity, "Description", Description(r.Body));
                if (resource == "locations" && r.Body.TryGetProperty("locationType", out _))
                    Set(entity, "LocationType", LocationType(r.Body));
                if (resource == "workstations" && r.Body.TryGetProperty("lineId", out _))
                {
                    Set(entity, "LineId", G(r.Body, "lineId"));
                    await RevokeTerminalSessions(null, id);
                }
                await ValidateOrgParents(entity, false);
                action = "Updated";
            }
            else if (parts.Length == 4 && r.Method == "POST" && parts[3] is "activate" or "deactivate")
            {
                Fields(r.Body, "reason");
                Reason(r.Body);
                var activate = parts[3] == "activate";
                if (activate)
                    await ValidateOrgParents(entity, true);
                else
                {
                    var deps = await Dependencies(type, id);
                    if (deps.Count > 0)
                        Fail(409, "ActiveDependencies", "存在未处理的依赖，请先查看停用影响");
                    if (entity is Worker)
                    {
                        var linked = await db.Users.SingleOrDefaultAsync(x => x.WorkerId == id);
                        if (linked is not null)
                        {
                            Require("security.users.manage");
                            await ProtectAdmin(linked);
                            linked.IsActive = false;
                            await Revoke(linked, "WorkerDeactivated");
                            Audit("User", linked.Id, "DeactivatedByWorker",
                                after: new { reason = Reason(r.Body) });
                        }
                    }
                    if (entity is Terminal)
                        await RevokeTerminalSessions(id, null);
                    if (entity is Workstation)
                        await RevokeTerminalSessions(null, id);
                }
                Set(entity, "IsActive", activate);
                action = activate ? "Activated" : "Deactivated";
            }
            else if (resource == "terminals" && parts.Length == 4
                && parts[3] == "bind-workstation" && r.Method == "POST")
            {
                Fields(r.Body, "workstationId", "reason");
                Reason(r.Body);
                Set(entity, "WorkstationId", G(r.Body, "workstationId"));
                await ValidateOrgParents(entity, true);
                await RevokeTerminalSessions(id, null);
                action = "WorkstationBound";
            }
            else
                throw new AppException(404, "ResourceNotFound", "接口不存在");
        }
        await db.SaveChangesAsync();
        var entityId = (Guid)entity.GetType().GetProperty("Id")!.GetValue(entity)!;
        var after = await OrgQuery(resource).SingleAsync(x => x.Id == entityId);
        Audit(type.Name, entityId, action, before, new { data = after, reason = S(r.Body, "reason") });
        return new(after, after.Etag, action == "Created" ? 201 : 200);
    }

    private static void Set(object obj, string name, object? value) =>
        obj.GetType().GetProperty(name)!.SetValue(obj, value);

    private static string? Description(JsonElement b)
    {
        var s = S(b, "description").Trim();
        if (s.Length > 2000)
            Fail(400, "ValidationFailed", "说明最长 2000 字符");
        return s.Length == 0 ? null : s;
    }

    private static string LocationType(JsonElement b)
    {
        var s = S(b, "locationType");
        if (s is not ("Storage" or "Buffer" or "Process" or "Other"))
            Fail(400, "ValidationFailed", "请选择有效位置类型");
        return s;
    }

    private async Task ValidateOrgParents(object e, bool active)
    {
        Guid fab = options.DeploymentFabId;
        Guid? area = e switch
        {
            Line x => x.AreaId,
            Location x => x.AreaId,
            Workstation x => x.AreaId,
            _ => null
        };
        if (area is Guid areaId)
        {
            var a = await db.Set<Area>().SingleOrDefaultAsync(x => x.Id == areaId && x.FabId == fab)
                ?? throw new AppException(404, "ResourceNotFound", "区域不存在或无权访问");
            if (active && !a.IsActive)
                Fail(409, "ParentInactive", "父区域已停用");
        }
        var direct = e switch
        {
            Area x => x.FabId,
            Worker x => x.FabId,
            Terminal x => x.FabId,
            Fab x => x.Id,
            _ => fab
        };
        if (direct != fab)
            Fail(404, "ResourceNotFound", "厂区不存在或无权访问");
        if (e is not Fab && active && !await db.Set<Fab>().AnyAsync(x => x.Id == fab && x.IsActive))
            Fail(409, "ParentInactive", "厂区已停用");
        if (e is Workstation w && w.LineId is Guid line)
        {
            var l = await db.Set<Line>().SingleOrDefaultAsync(x => x.Id == line && x.AreaId == w.AreaId)
                ?? throw new AppException(400, "ValidationFailed", "产线必须属于同一区域");
            if (active && !l.IsActive)
                Fail(409, "ParentInactive", "产线已停用");
        }
        if (e is Terminal t)
        {
            if (t.DeviceUuid == Guid.Empty)
                Fail(400, "ValidationFailed", "终端设备 UUID 必填");
            if (t.WorkstationId is Guid ws)
            {
                var station = await db.Set<Workstation>().SingleOrDefaultAsync(x => x.Id == ws)
                    ?? throw new AppException(404, "ResourceNotFound", "工位不存在");
                await ValidateOrgParents(station, active);
                if (active && !station.IsActive)
                    Fail(409, "ParentInactive", "工位已停用");
            }
        }
    }

    private async Task RevokeTerminalSessions(Guid? terminal, Guid? workstation)
    {
        var ids = terminal is Guid t
            ? new[] { t }
            : await db.Set<Terminal>().Where(x => x.WorkstationId == workstation).Select(x => x.Id).ToArrayAsync();
        foreach (var s in await db.Set<UserSession>()
            .Where(x => x.TerminalId != null && ids.Contains(x.TerminalId.Value) && x.RevokedAt == null)
            .ToListAsync())
        {
            s.RevokedAt = Now;
            s.RevokeReason = "TerminalContextChanged";
        }
    }

    private async Task<List<object>> Dependencies(Type parent, Guid id)
    {
        var result = new List<object>();
        // Metadata identifiers are model-owned; the object ID remains a bound SQL parameter.
        foreach (var entity in db.Model.GetEntityTypes().Where(x => x.ClrType.Namespace == typeof(Fab).Namespace))
            foreach (var fk in entity.GetForeignKeys()
                .Where(x => x.PrincipalEntityType.ClrType == parent && x.Properties.Count == 1))
            {
                var prop = fk.Properties[0];
                string? predicate = null;
                if (entity.FindProperty("IsActive") is not null)
                    predicate = "is_active = TRUE";
                else if (entity.ClrType == typeof(Carrier) && prop.Name == "CurrentLocationId")
                    predicate = "TRUE";
                else if (entity.ClrType == typeof(Downtime))
                    predicate = "ended_at IS NULL";
                else if (entity.ClrType == typeof(Lot))
                    predicate = "status NOT IN ('Completed','Cancelled','Consumed')";
                else if (entity.ClrType == typeof(WorkOrder))
                    predicate = "status NOT IN ('Completed','Cancelled','Closed')";
                if (predicate is null)
                    continue;
                var schema = entity.GetSchema();
                var table = entity.GetTableName();
                var column = prop.GetColumnName();
                var sql =
                    $"SELECT id AS \"Value\" FROM \"{schema}\".\"{table}\" WHERE \"{column}\" = {{0}} AND {predicate} LIMIT 20";
                var ids = await db.Database.SqlQueryRaw<Guid>(sql, id).ToListAsync();
                if (ids.Count > 0)
                    result.Add(new { type = entity.ClrType.Name, ids });
            }
        return result;
    }
}
