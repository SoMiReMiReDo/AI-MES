using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace MESagent.Api.Tests;

public sealed class ManufacturingTests : IClassFixture<OrganizationIdentityAccessControlFixture>
{
    private readonly OrganizationIdentityAccessControlFixture fixture;
    public ManufacturingTests(OrganizationIdentityAccessControlFixture fixture) => this.fixture = fixture;
    private async Task<JsonElement> Create(string path, object body)
    {
        var response = await OrganizationIdentityAccessControlFixture.Send(fixture.Admin, path, body);
        Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }
    private async Task<HttpResponseMessage> Action(string path, JsonElement row, string key, object? body = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/" + path)
        {
            Content = JsonContent.Create(body ?? new { reason = "集成测试" })
        };
        request.Headers.TryAddWithoutValidation("If-Match", row.GetProperty("etag").GetString());
        request.Headers.Add("Idempotency-Key", key);
        return await fixture.Admin.SendAsync(request);
    }
    [Fact]
    public async Task PublishAndReleasePreservesSnapshotAndReportsEmptyProductionFacts()
    {
        var suffix = Guid.NewGuid().ToString("N");
        var product = await Create("engineering/products", new
        {
            code = suffix,
            name = "测试产品",
            unitOfMeasure = "wafer"
        });
        var route = await Create("engineering/routes", new
        {
            code = suffix,
            name = "路线",
            productId = product.GetProperty("id")
        });
        var version = await Create("engineering/route-versions", new
        {
            routeId = route.GetProperty("id"),
            version = "1"
        });
        var path = "engineering/route-versions/" + version.GetProperty("id").GetString();
        var invalid = await Action(path + "/release", version, Guid.NewGuid().ToString());
        Assert.Equal(HttpStatusCode.UnprocessableEntity, invalid.StatusCode);
        var operation = await Create("engineering/operations", new
        {
            code = suffix,
            name = "光刻",
            segmentType = "Process"
        });
        await Create("engineering/route-steps", new
        {
            routeVersionId = version.GetProperty("id"),
            operationId = operation.GetProperty("id"),
            sequenceNo = 10,
            name = "首站"
        });
        var published = await Action(path + "/release", version, Guid.NewGuid().ToString());
        Assert.True(published.IsSuccessStatusCode, await published.Content.ReadAsStringAsync());
        var revision = await Create("engineering/product-revisions", new
        {
            productId = product.GetProperty("id"),
            routeVersionId = version.GetProperty("id"),
            revisionCode = "A"
        });
        var revisionRelease = await Action("engineering/product-revisions/" + revision.GetProperty("id").GetString() + "/release", revision, Guid.NewGuid().ToString());
        Assert.True(revisionRelease.IsSuccessStatusCode, await revisionRelease.Content.ReadAsStringAsync());
        var order = await Create("planning/work-orders", new
        {
            orderNo = suffix,
            productId = product.GetProperty("id"),
            productRevisionId = revision.GetProperty("id"),
            plannedQty = 25
        });
        var orderPath = "planning/work-orders/" + order.GetProperty("id").GetString();
        var key = Guid.NewGuid().ToString();
        var released = await Action(orderPath + "/release", order, key);
        Assert.True(released.IsSuccessStatusCode, await released.Content.ReadAsStringAsync());
        var replay = await Action(orderPath + "/release", order, key);
        Assert.True(JsonElement.DeepEquals(await released.Content.ReadFromJsonAsync<JsonElement>(),
            await replay.Content.ReadFromJsonAsync<JsonElement>()));
        var snapshots = await fixture.Admin.GetFromJsonAsync<JsonElement>("/api/v1/" + orderPath + "/snapshots");
        Assert.Equal(1, snapshots.GetArrayLength());
        var progress = await fixture.Admin.GetFromJsonAsync<JsonElement>("/api/v1/" + orderPath + "/progress");
        Assert.Equal("ProductionFacts", progress.GetProperty("coverage").GetString());
        Assert.Equal(0, progress.GetProperty("completedQty").GetInt32());
    }
    [Fact]
    public async Task NumberAllocationIsIdempotentAndRejectsStaleVersion()
    {
        var seq = await Create("engineering/number-sequences", new
        {
            name = Guid.NewGuid().ToString(),
            purpose = "Lot",
            prefix = "L",
            padSize = 4,
            nextNumber = 1
        });
        var path = "engineering/number-sequences/" + seq.GetProperty("id").GetString();
        var key = Guid.NewGuid().ToString();
        var first = await Action(path + "/allocate", seq, key, new { });
        Assert.True(first.IsSuccessStatusCode, await first.Content.ReadAsStringAsync());
        var replay = await Action(path + "/allocate", seq, key, new { });
        Assert.True(JsonElement.DeepEquals(await first.Content.ReadFromJsonAsync<JsonElement>(),
            await replay.Content.ReadFromJsonAsync<JsonElement>()));
        var stale = await Action(path + "/allocate", seq, Guid.NewGuid().ToString(), new { });
        Assert.Equal(HttpStatusCode.PreconditionFailed, stale.StatusCode);
    }
    [Fact]
    public async Task ViewerCannotReadOrWriteManufacturingWithoutPermission()
    {
        var user = await fixture.NewUser();
        var result = await user.Client.GetAsync("/api/v1/engineering/products");
        Assert.Equal(HttpStatusCode.Forbidden, result.StatusCode);
        result = await OrganizationIdentityAccessControlFixture.Send(user.Client, "planning/work-orders", new { });
        Assert.Equal(HttpStatusCode.Forbidden, result.StatusCode);
    }
    [Fact]
    public async Task ReleasedRecipeRejectsMutationAndAllowsCopy()
    {
        var recipe = await Create("engineering/recipes", new { code = Guid.NewGuid().ToString(), name = "Recipe" });
        var draft = await Create("engineering/recipe-versions", new
        {
            recipeId = recipe.GetProperty("id"),
            version = "A",
            parameters = "{}"
        });
        var path = "engineering/recipe-versions/" + draft.GetProperty("id").GetString();
        var unsafeRead = new HttpRequestMessage(HttpMethod.Get, "/api/v1/" + path + "/release")
        {
            Content = JsonContent.Create(new { reason = "禁止通过读取修改" })
        };
        unsafeRead.Headers.TryAddWithoutValidation("If-Match", draft.GetProperty("etag").GetString());
        Assert.Equal(HttpStatusCode.NotFound, (await fixture.Admin.SendAsync(unsafeRead)).StatusCode);
        var response = await Action(path + "/release", draft, Guid.NewGuid().ToString());
        Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());
        var published = await response.Content.ReadFromJsonAsync<JsonElement>();
        var edit = await OrganizationIdentityAccessControlFixture.Send(fixture.Admin, path,
            new { parameters = "{\"temperature\":100}" }, "PUT", published.GetProperty("etag").GetString());
        Assert.Equal(HttpStatusCode.Conflict, edit.StatusCode);
        var copy = await Create(path + "/copy", new { version = "B", reason = "实验" });
        Assert.Equal("Draft", copy.GetProperty("lifecycleStatus").GetString());
        Assert.NotEqual(draft.GetProperty("id").GetGuid(), copy.GetProperty("id").GetGuid());
    }

    [Fact]
    public async Task DocumentsRequireDraftStepAndPublishedFileBeforeRouteRelease()
    {
        var suffix = Guid.NewGuid().ToString();
        var route = await Create("engineering/routes", new { code = suffix, name = "路线" });
        var version = await Create("engineering/route-versions", new
        {
            routeId = route.GetProperty("id"),
            version = "A"
        });
        var operation = await Create("engineering/operations", new
        {
            code = suffix,
            name = "工序",
            segmentType = "Process"
        });
        var step = await Create("engineering/route-steps", new
        {
            routeVersionId = version.GetProperty("id"),
            operationId = operation.GetProperty("id"),
            sequenceNo = 1,
            name = "步骤"
        });
        var doc = await Create("engineering/documents", new
        {
            entityType = "RouteStep",
            entityId = step.GetProperty("id"),
            revision = "1",
            filename = "instruction.pdf",
            content = Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes("%PDF-1.4\n%%EOF"))
        });
        var routePath = "engineering/route-versions/" + version.GetProperty("id").GetString();
        var refused = await Action(routePath + "/release", version, Guid.NewGuid().ToString());
        Assert.Equal(HttpStatusCode.UnprocessableEntity, refused.StatusCode);
        var docPath = "engineering/documents/" + doc.GetProperty("id").GetString();
        var published = await Action(docPath + "/release", doc, Guid.NewGuid().ToString());
        Assert.True(published.IsSuccessStatusCode, await published.Content.ReadAsStringAsync());
        var downloaded = await fixture.Admin.GetAsync("/api/v1/" + docPath + "/content");
        Assert.Equal("application/pdf", downloaded.Content.Headers.ContentType?.MediaType);
        var released = await Action(routePath + "/release", version, Guid.NewGuid().ToString());
        Assert.True(released.IsSuccessStatusCode, await released.Content.ReadAsStringAsync());
        var edit = await OrganizationIdentityAccessControlFixture.Send(fixture.Admin,
            "engineering/route-steps/" + step.GetProperty("id").GetString(), new { name = "不可变" }, "PUT",
                step.GetProperty("etag").GetString());
        Assert.False(edit.IsSuccessStatusCode);
    }

    [Fact]
    public async Task ConcurrentAllocationsNeverRepeatNumbers()
    {
        var suffix = Guid.NewGuid().ToString("N");
        var sequence = await Create("engineering/number-sequences", new
        {
            name = suffix,
            purpose = "Lot",
            prefix = suffix,
            padSize = 4,
            nextNumber = 1
        });
        var path = "engineering/number-sequences/" + sequence.GetProperty("id").GetString();
        var results = await Task.WhenAll(Enumerable.Range(0, 6).Select(async _ =>
        {
            for (var attempt = 0;
 attempt < 20;
 attempt++)
            {
                var latest = await fixture.Admin.GetFromJsonAsync<JsonElement>("/api/v1/" + path);
                var response = await Action(path + "/allocate", latest, Guid.NewGuid().ToString(), new { });
                if (response.StatusCode == HttpStatusCode.PreconditionFailed) continue;
                Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());
                return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("number").GetString();
            }
            throw new InvalidOperationException("Allocation retries exhausted");
        }));
        Assert.Equal(6, results.Distinct().Count());
    }

    [Fact]
    public async Task WorkOrderCreationRejectsForeignFabBeforeWriting()
    {
        var response = await OrganizationIdentityAccessControlFixture.Send(fixture.Admin, "planning/work-orders",
            new { fabId = Guid.NewGuid() });
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task RouteDraftSavesNestedDefinitionsAtomically()
    {
        var suffix = Guid.NewGuid().ToString();
        var route = await Create("engineering/routes", new { code = suffix, name = "嵌套路由" });
        var version = await Create("engineering/route-versions", new
        {
            routeId = route.GetProperty("id"),
            version = "1"
        });
        var operation = await Create("engineering/operations", new
        {
            code = suffix,
            name = "工序",
            segmentType = "Process"
        });
        var path = "engineering/route-versions/" + version.GetProperty("id").GetString();
        var invalid = await OrganizationIdentityAccessControlFixture.Send(fixture.Admin, path, new
        {
            steps = new[] { new { operationId = operation.GetProperty("id"), sequenceNo = 1, name = "站点",
                outputDefinitions = new[] { new { key = "x", label = "值", valueType = "invalid" } } } }
        }, "PUT", version.GetProperty("etag").GetString());
        Assert.Equal(HttpStatusCode.UnprocessableEntity, invalid.StatusCode);
        var unchanged = await fixture.Admin.GetFromJsonAsync<JsonElement>("/api/v1/" + path);
        Assert.Equal(0, unchanged.GetProperty("steps").GetArrayLength());
        var saved = await OrganizationIdentityAccessControlFixture.Send(fixture.Admin, path, new
        {
            steps = new[] { new { operationId = operation.GetProperty("id"), sequenceNo = 1, name = "站点",
                outputDefinitions = new[] { new { key = "x", label = "温度", valueType = "decimal", unit = "C" } } } }
        }, "PUT", unchanged.GetProperty("etag").GetString());
        Assert.True(saved.IsSuccessStatusCode, await saved.Content.ReadAsStringAsync());
        var detail = await fixture.Admin.GetFromJsonAsync<JsonElement>("/api/v1/" + path);
        Assert.Equal(1, detail.GetProperty("steps")[0].GetProperty("outputDefinitions").GetArrayLength());
    }

}
