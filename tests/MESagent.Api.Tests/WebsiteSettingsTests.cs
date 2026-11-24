using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using MESagent.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;
using static MESagent.Api.Tests.OrganizationIdentityAccessControlFixture;

namespace MESagent.Api.Tests;

public sealed class WebsiteSettingsTests(OrganizationIdentityAccessControlFixture fixture)
    : IClassFixture<OrganizationIdentityAccessControlFixture>
{
    [Fact]
    public async Task AppearanceIsPublic_AndVersionedChangesAreAudited()
    {
        using var anonymous = fixture.Client();
        var initial = await Get(anonymous, "site/appearance");
        Assert.Equal(new[] { "defaultTheme", "name", "subtitle" },
            initial.EnumerateObject().Select(x => x.Name).Order().ToArray());
        Assert.Equal("MESagent", initial.GetProperty("name").GetString());
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await anonymous.GetAsync("/api/v1/settings/system")).StatusCode);

        var admin = fixture.Admin;
        var info = await Get(admin, "settings/system");
        Assert.True(info.GetProperty("canManageAppearance").GetBoolean());
        Assert.Equal("Asia/Shanghai", info.GetProperty("timezone").GetString());
        Assert.Equal(60, info.GetProperty("idleMinutes").GetInt32());
        var current = await Get(admin, "settings/appearance");
        var etag = current.GetProperty("etag").GetString();
        var input = new { name = " 晶圆工作台 ", subtitle = "FAB", defaultTheme = "dark" };
        Assert.Equal((HttpStatusCode)428, (await Send(admin, "settings/appearance", input, "PUT")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Send(admin, "settings/appearance",
            new { name = "品牌\n", subtitle = "", defaultTheme = "dark" }, "PUT", etag)).StatusCode);
        var attempts = await Task.WhenAll(
            Send(admin, "settings/appearance", input, "PUT", etag),
            Send(admin, "settings/appearance", input, "PUT", etag));
        Assert.Contains(attempts, x => x.StatusCode == HttpStatusCode.PreconditionFailed);
        var saved = Assert.Single(attempts, x => x.IsSuccessStatusCode);
        Assert.NotEqual(etag, saved.Headers.ETag?.Tag);
        var publicAppearance = await Get(anonymous, "site/appearance");
        Assert.Equal("晶圆工作台", publicAppearance.GetProperty("name").GetString());
        Assert.Equal("dark", publicAppearance.GetProperty("defaultTheme").GetString());

        var darkEtag = (await Get(admin, "settings/appearance")).GetProperty("etag").GetString();
        Assert.Equal(HttpStatusCode.BadRequest, (await Send(admin, "settings/appearance",
            new { name = "晶圆工作台", subtitle = "FAB", defaultTheme = "auto" }, "PUT", darkEtag)).StatusCode);
        var followSystem = await Send(admin, "settings/appearance",
            new { name = "晶圆工作台", subtitle = "FAB", defaultTheme = "system" }, "PUT", darkEtag);
        Assert.True(followSystem.IsSuccessStatusCode, await followSystem.Content.ReadAsStringAsync());
        Assert.Equal("system",
            (await Get(anonymous, "site/appearance")).GetProperty("defaultTheme").GetString());
        var restored = await Send(admin, "settings/appearance",
            new { name = "晶圆工作台", subtitle = "FAB", defaultTheme = "dark" },
            "PUT", (await Get(admin, "settings/appearance")).GetProperty("etag").GetString());
        Assert.True(restored.IsSuccessStatusCode, await restored.Content.ReadAsStringAsync());
        Assert.Equal("dark",
            (await Get(anonymous, "site/appearance")).GetProperty("defaultTheme").GetString());

        var traceId = saved.Headers.GetValues("X-Trace-Id").Single();
        var audit = await Get(admin, "reliability/audit-logs?correlationId=" + traceId);
        var change = Assert.Single(audit.GetProperty("items").EnumerateArray());
        Assert.Equal("SiteAppearanceUpdated", change.GetProperty("action").GetString());
        Assert.Equal(SiteAppearance.SingletonId, change.GetProperty("entityId").GetGuid());
        Assert.Equal("MESagent", change.GetProperty("beforeState").GetProperty("name").GetString());
        Assert.Equal("晶圆工作台", change.GetProperty("afterState").GetProperty("name").GetString());
        Assert.Equal((await Get(admin, "auth/me")).GetProperty("userId").GetGuid(),
            change.GetProperty("userId").GetGuid());
    }

    [Fact]
    public async Task AppearanceNeedsAllScopes_AndSchedulePreferenceIsLocal()
    {
        var (viewer, _, _) = await fixture.NewUser();
        using var reader = viewer;
        Assert.Equal(HttpStatusCode.Forbidden,
            (await reader.GetAsync("/api/v1/settings/appearance")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await Send(reader, "settings/schedule",
            new { defaultView = "day" }, "PUT")).StatusCode);
        var (noScope, _, _) = await fixture.NewUser("SystemAdmin", scope: false);
        using var unscoped = noScope;
        Assert.Equal(HttpStatusCode.Forbidden,
            (await unscoped.GetAsync("/api/v1/settings/system")).StatusCode);

        await using var db = fixture.Database.CreateContext();
        var other = new Fab { Code = "OTHER", Name = "Other Fab", Timezone = "America/Los_Angeles" };
        db.Add(other);
        var otherPreference = new FabSchedulePreference { FabId = other.Id, DefaultView = "week" };
        db.Add(otherPreference);
        await db.SaveChangesAsync();
        try
        {
            var admin = fixture.Admin;
            var info = await Get(admin, "settings/system");
            Assert.False(info.GetProperty("canManageAppearance").GetBoolean());
            Assert.True(info.GetProperty("canManageSchedule").GetBoolean());
            var site = await Get(admin, "settings/appearance");
            var result = await Send(admin, "settings/appearance",
                new { name = "Denied", subtitle = "", defaultTheme = "light" },
                "PUT", site.GetProperty("etag").GetString());
            Assert.Equal(HttpStatusCode.Forbidden, result.StatusCode);
            Assert.Equal("AllFabScopesRequired",
                (await result.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());

            var schedule = await Get(admin, "settings/schedule");
            Assert.Equal("week", schedule.GetProperty("defaultView").GetString());
            var etag = schedule.GetProperty("etag").GetString();
            var saved = await Send(admin, "settings/schedule", new { defaultView = "day" }, "PUT", etag);
            Assert.True(saved.IsSuccessStatusCode, await saved.Content.ReadAsStringAsync());
            Assert.Equal(HttpStatusCode.PreconditionFailed, (await Send(admin, "settings/schedule",
                new { defaultView = "week" }, "PUT", etag)).StatusCode);
            var options = await Get(admin, "workbench/options");
            Assert.Equal("day", options.GetProperty("defaultScheduleView").GetString());
            Assert.Equal("week", await db.Set<FabSchedulePreference>().Where(x => x.FabId == other.Id)
                .Select(x => x.DefaultView).SingleAsync());
        }
        finally
        {
            db.Remove(otherPreference);
            db.Remove(other);
            await db.SaveChangesAsync();
        }
    }

    private static Task<JsonElement> Get(HttpClient client, string path) =>
        client.GetFromJsonAsync<JsonElement>("/api/v1/" + path);
}
