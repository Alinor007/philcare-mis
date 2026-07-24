using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using philcare.Api.Features.Programs.Activities.CreateActivity;
using philcare.Api.Features.Programs.AidPrograms.CreateProgram;
using philcare.Api.Features.Programs.Projects.CreateProject;
using philcare.Test.Common;
using Xunit;

namespace philcare.Test.Programs;

public class ProgramsTests : IClassFixture<TestWebAppFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly HttpClient _client;

    public ProgramsTests(TestWebAppFactory factory)
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

    private async Task<int> CreateProgramAsync(string name)
    {
        var response = await _client.PostAsJsonAsync("/api/programs", new { Name = name, Category = "RELIEF" });
        response.EnsureSuccessStatusCode();
        var program = await response.Content.ReadFromJsonAsync<CreateProgramResponse>(JsonOptions);
        return program!.Id;
    }

    [Fact]
    public async Task CreateProgram_WithoutAuthentication_ReturnsUnauthorized()
    {
        var response = await _client.PostAsJsonAsync("/api/programs", new { Name = "Should Not Save", Category = "RELIEF" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task FullChain_ProgramToProjectToActivity_Succeeds()
    {
        await AuthenticateAsAdminAsync();
        var programId = await CreateProgramAsync($"Program-{Guid.NewGuid():N}");

        var projectResponse = await _client.PostAsJsonAsync("/api/projects", new
        {
            ProgramId = programId,
            Name = "Emergency Relief 2026",
            TotalBudget = 500000m
        });
        Assert.Equal(HttpStatusCode.Created, projectResponse.StatusCode);
        var project = await projectResponse.Content.ReadFromJsonAsync<CreateProjectResponse>(JsonOptions);

        var activityResponse = await _client.PostAsJsonAsync("/api/activities", new
        {
            ProjectId = project!.Id,
            Name = "Food Pack Distribution - Batch 1",
            ActivityType = "RELIEF_DISTRIBUTION",
            Budget = 100000m
        });
        Assert.Equal(HttpStatusCode.Created, activityResponse.StatusCode);
        var activity = await activityResponse.Content.ReadFromJsonAsync<CreateActivityResponse>(JsonOptions);

        Assert.Equal(project.Id, activity!.ProjectId);
        Assert.Equal("PLANNED", activity.ImplementationStatus);

        var programDetail = await _client.GetAsync($"/api/programs/{programId}");
        programDetail.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task CreateProject_UnderMissingProgram_ReturnsNotFound()
    {
        await AuthenticateAsAdminAsync();

        var response = await _client.PostAsJsonAsync("/api/projects", new
        {
            ProgramId = 999999,
            Name = "Orphan Project",
            TotalBudget = 1000m
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateActivity_UnderMissingProject_ReturnsNotFound()
    {
        await AuthenticateAsAdminAsync();

        var response = await _client.PostAsJsonAsync("/api/activities", new
        {
            ProjectId = 999999,
            Name = "Orphan Activity",
            ActivityType = "OUTREACH",
            Budget = 500m
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeactivateProgram_RequiresAdminRole()
    {
        await AuthenticateAsAdminAsync();
        var programId = await CreateProgramAsync($"Program-{Guid.NewGuid():N}");

        var programEmail = $"program-{Guid.NewGuid():N}@philcare.local";
        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            Email = programEmail,
            Password = "Program@12345",
            Role = "Program"
        });
        registerResponse.EnsureSuccessStatusCode();

        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new { Email = programEmail, Password = "Program@12345" });
        loginResponse.EnsureSuccessStatusCode();
        var programToken = await loginResponse.Content.ReadFromJsonAsync<LoginResponseDto>(JsonOptions);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", programToken!.AccessToken);

        var deactivateResponse = await _client.DeleteAsync($"/api/programs/{programId}");

        Assert.Equal(HttpStatusCode.Forbidden, deactivateResponse.StatusCode);
    }

    [Fact]
    public async Task DeactivateProgram_AsAdmin_Succeeds()
    {
        await AuthenticateAsAdminAsync();
        var programId = await CreateProgramAsync($"Program-{Guid.NewGuid():N}");

        var response = await _client.DeleteAsync($"/api/programs/{programId}");
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var getResponse = await _client.GetAsync($"/api/programs?includeInactive=true");
        getResponse.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task UpdateProject_UnknownId_ReturnsNotFound()
    {
        await AuthenticateAsAdminAsync();

        var response = await _client.PutAsJsonAsync("/api/projects/999999", new
        {
            Name = "Ghost",
            TotalBudget = 0m,
            ImplementationStatus = "PLANNED",
            IsActive = true
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private sealed record LoginResponseDto(string AccessToken, string RefreshToken, DateTime RefreshTokenExpiresAt);
}
