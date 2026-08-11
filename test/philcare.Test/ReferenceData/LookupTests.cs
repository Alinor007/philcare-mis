using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using philcare.Api.Common.Persistence;
using philcare.Api.Features.ReferenceData.Domain;
using philcare.Api.Features.ReferenceData.GetLookups;
using philcare.Api.Infrastructure.Seed;
using philcare.Test.Common;
using Xunit;

namespace philcare.Test.ReferenceData;

public class LookupTests : IClassFixture<TestWebAppFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly TestWebAppFactory _factory;
    private readonly HttpClient _client;

    public LookupTests(TestWebAppFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task AuthenticateAsAdminAsync()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new { Email = "admin@philcare.local", Password = "Admin@12345" });
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<LoginResponseDto>(JsonOptions);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body!.AccessToken);
    }

    /// <summary>Re-runs boot-time seeding against the already-seeded test database.</summary>
    private async Task ReseedAsync()
    {
        using var scope = _factory.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<DbSeeder>().SeedAsync();
    }

    private async Task<LookupItem> GetRawAsync(string category, string code)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await db.LookupItems.AsNoTracking().SingleAsync(l => l.Category == category && l.Code == code);
    }

    private async Task<List<LookupItemResponse>> GetCategoryAsync(string category)
    {
        var response = await _client.GetAsync($"/api/lookups/{category}");
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<List<LookupItemResponse>>(JsonOptions))!;
    }

    [Fact]
    public async Task GetLookups_WithoutAuthentication_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/lookups");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Seed_NewGovernanceCategories_AreSeededFromExcelVocabulary()
    {
        await AuthenticateAsAdminAsync();

        var calledBy = await GetCategoryAsync(LookupCategory.CalledBy);
        var participationMode = await GetCategoryAsync(LookupCategory.ParticipationMode);

        Assert.Equal(11, calledBy.Count);
        Assert.Equal(8, participationMode.Count);
        // "General Manager" is not in the workbook's dropdown but does appear in real
        // Meetings_Register rows — dropping it would make historical data unreadable.
        Assert.Contains(calledBy, l => l.Code == "GENERAL_MANAGER");
        Assert.Contains(participationMode, l => l.Code == "IN_PERSON");
    }

    [Theory]
    // Hardcoded in C# comparisons and/or written by a shipped migration. Renaming or removing
    // any of these silently breaks quorum counting, report totals, or entity defaults.
    [InlineData("implementation_status", "PLANNED")]
    [InlineData("beneficiary_type", "INDIVIDUAL")]
    [InlineData("person_status", "ACTIVE")]
    [InlineData("beneficiary_status", "PENDING")]
    [InlineData("attendance_status", "PRESENT")]
    [InlineData("attendance_status", "ONLINE_PRESENT")]
    [InlineData("decision_status", "OPEN")]
    [InlineData("decision_status", "IN_PROGRESS")]
    public async Task Seed_FrozenCodes_SurviveTheExcelRealignment(string category, string code)
    {
        await AuthenticateAsAdminAsync();

        var items = await GetCategoryAsync(category);

        Assert.Contains(items, l => l.Code == code);
    }

    [Fact]
    public async Task Seed_RelabelledCode_KeepsItsCodeAndGainsTheExcelLabel()
    {
        await AuthenticateAsAdminAsync();

        var items = await GetCategoryAsync(LookupCategory.ImplementationStatus);
        var planned = items.Single(l => l.Code == "PLANNED");

        Assert.Equal("Planning", planned.Label);
    }

    [Fact]
    public async Task Reseed_SeedOwnedLabel_IsReconciledBackToTheJson()
    {
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var item = await db.LookupItems.SingleAsync(l => l.Category == "meeting_mode" && l.Code == "HYBRID");
            item.Label = "Drifted label";
            // The audit interceptor stamps "system" here (no HTTP user on this scope), which is
            // exactly the seed-owned state a boot-time write leaves behind.
            await db.SaveChangesAsync();
        }

        await ReseedAsync();

        var reconciled = await GetRawAsync("meeting_mode", "HYBRID");
        Assert.Equal("Hybrid", reconciled.Label);
    }

    [Fact]
    public async Task Reseed_AdminEditedLabel_IsPreserved()
    {
        await AuthenticateAsAdminAsync();
        var target = await GetRawAsync("meeting_mode", "ONLINE");

        var update = await _client.PutAsJsonAsync(
            $"/api/lookups/{target.Id}",
            new { Label = "Video conference", SortOrder = target.SortOrder, IsActive = true });
        update.EnsureSuccessStatusCode();

        await ReseedAsync();

        var afterReseed = await GetRawAsync("meeting_mode", "ONLINE");
        Assert.Equal("Video conference", afterReseed.Label);
    }

    [Fact]
    public async Task Reseed_DeactivatedItem_IsNotReactivated()
    {
        await AuthenticateAsAdminAsync();
        var target = await GetRawAsync("meeting_role", "TIMEKEEPER");

        var deactivate = await _client.DeleteAsync($"/api/lookups/{target.Id}");
        deactivate.EnsureSuccessStatusCode();

        await ReseedAsync();

        var afterReseed = await GetRawAsync("meeting_role", "TIMEKEEPER");
        Assert.False(afterReseed.IsActive);
    }

    [Fact]
    public async Task Seed_HasNoDuplicateCategoryCodePairs()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var keys = await db.LookupItems.AsNoTracking().Select(l => new { l.Category, l.Code }).ToListAsync();

        Assert.Equal(keys.Count, keys.Distinct().Count());
    }

    private sealed record LoginResponseDto(string AccessToken, string RefreshToken, DateTime RefreshTokenExpiresAt);
}
