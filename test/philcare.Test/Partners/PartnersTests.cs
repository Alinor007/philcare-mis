using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using philcare.Api.Features.Partners.CreatePartner;
using philcare.Api.Features.Programs.Activities.CreateActivity;
using philcare.Api.Features.Programs.AidPrograms.CreateProgram;
using philcare.Api.Features.Programs.Projects.CreateProject;
using philcare.Test.Common;
using Xunit;

namespace philcare.Test.Partners;

public class PartnersTests : IClassFixture<TestWebAppFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly HttpClient _client;

    public PartnersTests(TestWebAppFactory factory)
    {
        _client = factory.CreateClient();
    }

    private async Task AuthenticateAsAdminAsync()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            Email = "admin@philcare.local",
            Password = "Admin@12345"
        });

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<LoginResponseDto>(JsonOptions);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body!.AccessToken);
    }

    private async Task<int> CreatePartnerAsync(string? name = null)
    {
        var response = await _client.PostAsJsonAsync("/api/partners", new
        {
            Name = name ?? $"Partner-{Guid.NewGuid():N}",
            PartnerType = "IMPLEMENTING"
        });
        response.EnsureSuccessStatusCode();
        var partner = await response.Content.ReadFromJsonAsync<CreatePartnerResponse>(JsonOptions);
        return partner!.Id;
    }

    private async Task<int> CreateProjectAsync()
    {
        var programResponse = await _client.PostAsJsonAsync("/api/programs", new { Name = $"Program-{Guid.NewGuid():N}", Category = "RELIEF" });
        programResponse.EnsureSuccessStatusCode();
        var program = await programResponse.Content.ReadFromJsonAsync<CreateProgramResponse>(JsonOptions);

        var projectResponse = await _client.PostAsJsonAsync("/api/projects", new
        {
            ProgramId = program!.Id,
            Name = $"Project-{Guid.NewGuid():N}",
            TotalBudget = 10000m
        });
        projectResponse.EnsureSuccessStatusCode();
        var project = await projectResponse.Content.ReadFromJsonAsync<CreateProjectResponse>(JsonOptions);
        return project!.Id;
    }

    [Fact]
    public async Task CreatePartner_ValidRequest_ReturnsCreated()
    {
        await AuthenticateAsAdminAsync();

        var response = await _client.PostAsJsonAsync("/api/partners", new
        {
            Name = $"Al-Ihsan Foundation-{Guid.NewGuid():N}",
            PartnerType = "IMPLEMENTING",
            ContactPerson = "Juan Dela Cruz"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var partner = await response.Content.ReadFromJsonAsync<CreatePartnerResponse>(JsonOptions);
        Assert.True(partner!.IsActive);
    }

    [Fact]
    public async Task CreatePartner_DuplicateName_ReturnsConflict()
    {
        await AuthenticateAsAdminAsync();
        var name = $"Duplicate-Partner-{Guid.NewGuid():N}";

        var first = await _client.PostAsJsonAsync("/api/partners", new { Name = name, PartnerType = "IMPLEMENTING" });
        first.EnsureSuccessStatusCode();

        var second = await _client.PostAsJsonAsync("/api/partners", new { Name = name, PartnerType = "DONOR" });

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task DeactivatePartner_AsAdmin_Succeeds()
    {
        await AuthenticateAsAdminAsync();
        var partnerId = await CreatePartnerAsync();

        var response = await _client.DeleteAsync($"/api/partners/{partnerId}");
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var getResponse = await _client.GetAsync($"/api/partners/{partnerId}");
        getResponse.EnsureSuccessStatusCode();
        var partner = await getResponse.Content.ReadFromJsonAsync<PartnerDetailDto>(JsonOptions);
        Assert.False(partner!.IsActive);
    }

    [Fact]
    public async Task CreateActivity_WithImplementingPartnerId_LinksPartner()
    {
        await AuthenticateAsAdminAsync();
        var projectId = await CreateProjectAsync();
        var partnerId = await CreatePartnerAsync();

        var response = await _client.PostAsJsonAsync("/api/activities", new
        {
            ProjectId = projectId,
            Name = $"Activity-{Guid.NewGuid():N}",
            ActivityType = "OUTREACH",
            Budget = 1000m,
            ImplementingPartnerId = partnerId
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task CreateActivity_WithUnknownImplementingPartnerId_ReturnsNotFound()
    {
        await AuthenticateAsAdminAsync();
        var projectId = await CreateProjectAsync();

        var response = await _client.PostAsJsonAsync("/api/activities", new
        {
            ProjectId = projectId,
            Name = $"Activity-{Guid.NewGuid():N}",
            ActivityType = "OUTREACH",
            Budget = 1000m,
            ImplementingPartnerId = 999999
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateActivity_WithInactiveImplementingPartnerId_ReturnsBadRequest()
    {
        await AuthenticateAsAdminAsync();
        var projectId = await CreateProjectAsync();
        var partnerId = await CreatePartnerAsync();

        var deactivateResponse = await _client.DeleteAsync($"/api/partners/{partnerId}");
        deactivateResponse.EnsureSuccessStatusCode();

        var response = await _client.PostAsJsonAsync("/api/activities", new
        {
            ProjectId = projectId,
            Name = $"Activity-{Guid.NewGuid():N}",
            ActivityType = "OUTREACH",
            Budget = 1000m,
            ImplementingPartnerId = partnerId
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private sealed record LoginResponseDto(string AccessToken, string RefreshToken, DateTime RefreshTokenExpiresAt);

    private sealed record PartnerDetailDto(
        int Id, string Name, string PartnerType, string? ContactPerson, string? Email, string? Phone, string? Address,
        string? City, string? Province, string? Region, string? MouReference, DateTime? MouStartDate, DateTime? MouEndDate,
        string? AccreditationNotes, string? Notes, bool IsActive, int ActivityCount);
}
