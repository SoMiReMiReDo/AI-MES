using System.Text.Json;
using MESagent.Application;
using MESagent.Domain;
using MESagent.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MESagent.Infrastructure.QualityExceptionDisposition;

public sealed partial class QualityBackend
{
    private const string AutomaticCode = "SYS-QUALITY-CHECK-FAILED";

    private async Task<T> Find<T>(Guid id) where T : class =>
        await db.Set<T>().FindAsync(id) ?? throw Error("ResourceNotFound", "配置不存在", 404);

    private async Task<OrganizationIdentityAccessControlResult> Configuration(
        OrganizationIdentityAccessControlRequest r, string[] p)
    {
        var create = p.Length == 2 && r.Method == "POST";
        var update = p.Length == 3 && r.Method == "PUT";
        var release = p.Length == 4 && p[3] == "release" && r.Method == "POST";
        if (!create && !update && !release)
            throw Error("ResourceNotFound", "接口不存在", 404);
        object row;
        if (p[1] == "reason-codes" && !release)
        {
            var input = Body<SaveQualityReason>(r);
            var code = Text(input.Code, 64).ToUpperInvariant();
            var value = create ? new ReasonCode { Code = code, Category = input.Category }
                : await Find<ReasonCode>(Id(p[2]));
            if (!create)
                Match(r.Version, value.RowVersion);
            if (code == AutomaticCode || value.Code == AutomaticCode)
                throw Error("ReservedConfiguration", "系统检验原因码不可修改", 409);
            if (input.Category is not ("Hold" or "Scrap" or "Disposition")
                || value.Code != code || value.Category != input.Category)
                throw Error("InvalidRequest", "原因类别无效或尝试修改固定代码/类别", 422);
            value.Name = Text(input.Name, 200);
            value.Description = Text(input.Description, 2000, false);
            value.IsActive = input.IsActive;
            row = value;
        }
        else if (p[1] == "issue-types" && !release)
        {
            var input = Body<SaveQualityIssueType>(r);
            var code = Text(input.Code, 64).ToUpperInvariant();
            var value = create ? new IssueType { Code = code } : await Find<IssueType>(Id(p[2]));
            if (!create)
                Match(r.Version, value.RowVersion);
            if (code == AutomaticCode || value.Code == AutomaticCode)
                throw Error("ReservedConfiguration", "系统检验异常类型不可修改", 409);
            if (input.Severity is not ("Minor" or "Major" or "Critical") || value.Code != code)
                throw Error("InvalidRequest", "严重度无效或尝试修改固定代码", 422);
            value.Name = Text(input.Name, 200);
            value.Description = Text(input.Description, 2000, false);
            value.Severity = input.Severity;
            value.IsBlocking = input.IsBlocking;
            value.IsActive = input.IsActive;
            row = value;
        }
        else if (p[1] == "specifications")
        {
            var value = create ? new SpecificationVersion { LifecycleStatus = "Draft" }
                : await Find<SpecificationVersion>(Id(p[2]));
            if (!create)
                Match(r.Version, value.RowVersion);
            if (value.LifecycleStatus != "Draft")
                throw Error("VersionFrozen", "发布规格不可修改，请创建新版本");
            if (release)
            {
                Text(Body<ReasonRequest>(r).Reason, 500);
                if (!QualityRules.ValidSpecification(value.ValueType, value.SpecMin, value.SpecMax,
                    value.ExpectedBoolean, value.ExpectedText))
                    throw Error("InvalidSpecification", "规格尚未完整", 422);
                value.LifecycleStatus = "Released";
                value.ReleasedAt = Now;
                value.ReleasedById = actor.UserId;
            }
            else
            {
                var input = Body<SaveQualitySpecification>(r);
                var code = Text(input.ParameterCode, 64).ToUpperInvariant();
                if (input.Version < 1 || !QualityRules.ValidSpecification(input.ValueType, input.SpecMin,
                    input.SpecMax, input.ExpectedBoolean, input.ExpectedText)
                    || !create && (value.ParameterCode != code || value.Version != input.Version))
                    throw Error("InvalidSpecification", "规格类型、上下限、期望值或固定版本无效", 422);
                value.ParameterCode = code;
                value.Version = input.Version;
                value.ValueType = input.ValueType;
                value.Unit = string.IsNullOrWhiteSpace(input.Unit) ? null : Text(input.Unit, 32);
                value.SpecMin = input.SpecMin;
                value.SpecMax = input.SpecMax;
                value.ExpectedBoolean = input.ExpectedBoolean;
                value.ExpectedText = input.ExpectedText;
            }
            row = value;
        }
        else if (p[1] == "templates")
        {
            var value = create ? new QualityCheckTemplate { LifecycleStatus = "Draft" }
                : await Find<QualityCheckTemplate>(Id(p[2]));
            if (!create)
                Match(r.Version, value.RowVersion);
            if (value.LifecycleStatus != "Draft")
                throw Error("VersionFrozen", "发布模板不可修改，请创建新版本");
            if (release)
            {
                Text(Body<ReasonRequest>(r).Reason, 500);
                await TemplateSpecifications(value, true);
                value.LifecycleStatus = "Released";
                value.ReleasedAt = Now;
                value.ReleasedById = actor.UserId;
            }
            else
            {
                var input = Body<SaveQualityTemplate>(r);
                var name = Text(input.Name, 200);
                if (input.Version < 1 || input.SamplesPerCheck is < 1 or > 100
                    || input.SpecificationVersionIds == null || input.SpecificationVersionIds.Length is < 1 or > 50
                    || input.SpecificationVersionIds.Distinct().Count() != input.SpecificationVersionIds.Length
                    || !create && (value.RouteVersionId != input.RouteVersionId
                        || value.Name != name || value.Version != input.Version))
                    throw Error("InvalidTemplate", "模板版本、样本数、规格清单或固定标识无效", 422);
                var route = await Find<RouteVersion>(input.RouteVersionId);
                if (input.RouteStepId != null && !await db.Set<RouteStep>().AnyAsync(x => x.Id == input.RouteStepId
                    && x.RouteVersionId == route.Id))
                    throw Error("InvalidTemplate", "模板步骤与路线不一致", 422);
                value.RouteVersionId = route.Id;
                value.RouteStepId = input.RouteStepId;
                value.Name = name;
                value.Version = input.Version;
                value.SamplesPerCheck = input.SamplesPerCheck;
                value.MinChecksPerBatch = 0;
                value.Parameters = JsonSerializer.Serialize(input.SpecificationVersionIds.Order(), Json);
                await TemplateSpecifications(value, false);
            }
            row = value;
        }
        else
            throw Error("ResourceNotFound", "接口不存在", 404);
        if (create)
            db.Add(row);
        return await Saved(row, release ? "ConfigurationReleased" : "ConfigurationSaved",
            new { resource = p[1], create }, create ? 201 : 200);
    }

    private async Task<List<SpecificationVersion>> TemplateSpecifications(QualityCheckTemplate template,
        bool requireReleased)
    {
        Guid[] ids;
        try
        {
            ids = JsonSerializer.Deserialize<Guid[]>(template.Parameters ?? "[]", Json) ?? [];
        }
        catch (JsonException)
        {
            throw Error("InvalidTemplate", "旧模板参数尚未转换为版本化规格清单", 409);
        }
        var specifications = await db.Set<SpecificationVersion>().Where(x => ids.Contains(x.Id)).ToListAsync();
        if (ids.Length == 0 || specifications.Count != ids.Length
            || specifications.Select(x => x.ParameterCode).Distinct().Count() != specifications.Count
            || requireReleased && specifications.Any(x => x.LifecycleStatus != "Released"))
            throw Error("InvalidTemplate", "规格缺失、重复参数或尚未发布", 422);
        return specifications;
    }
}
