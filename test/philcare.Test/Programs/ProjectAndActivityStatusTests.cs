using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using philcare.Api.Features.Programs.Activities.ChangeActivityStatus;
using philcare.Api.Features.Programs.Activities.CreateActivity;
using philcare.Api.Features.Programs.AidPrograms.CreateProgram;
using philcare.Api.Features.Programs.Projects.ChangeProjectStatus;
using philcare.Api.Features.Programs.Projects.CreateProject;
using philcare.Test.Common;
using Xunit;

namespace philcare.Test.Programs;

public class ProjectAndActivityStatusTests : IClassFixture<TestWebAppFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly HttpClient _client;

    public ProjectAndActivityStatusTests(TestWebAppFactory factory)
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

    private async Task<int> CreateProjectAsync()
    {
        var programResponse = await _client.PostAsJsonAsync("/api/programs", new
        {
            Name = $"Program-{Guid.NewGuid():N}",
            Category = "RELIEF"
        });
        programResponse.EnsureSuccessStatusCode();
        var program = await programResponse.Content.ReadFromJsonAsync<CreateProgramResponse>(JsonOptions);

        var projectResponse = await _client.PostAsJsonAsync("/api/projects", new
        {
            ProgramId = program!.Id,
            Name = $"Project-{Guid.NewGuid():N}",
            TotalBudget = 100000m
        });
        projectResponse.EnsureSuccessStatusCode();
        var project = await projectResponse.Content.ReadFromJsonAsync<CreateProjectResponse>(JsonOptions);
        return project!.Id;
    }

    private async Task<int> CreateActivityAsync(int projectId)
    {
        var response = await _client.PostAsJsonAsync("/api/activities", new
        {
            ProjectId = projectId,
            Name = $"Activity-{Guid.NewGuid():N}",
            ActivityType = "OUTREACH",
            Budget = 1000m
        });
        response.EnsureSuccessStatusCode();
        var activity = await response.Content.ReadFromJsonAsync<CreateActivityResponse>(JsonOptions);
        return activity!.Id;
    }

    [Fact]
    public async Task ChangeActivityStatus_PlannedToOngoing_Succeeds()
    {
        await AuthenticateAsAdminAsync();
        var projectId = await CreateProjectAsync();
        var activityId = await CreateActivityAsync(projectId);

        var response = await _client.PostAsJsonAsync($"/api/activities/{activityId}/status", new { Status = "ONGOING" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<ChangeActivityStatusResponse>(JsonOptions);
        Assert.Equal("ONGOING", result!.ImplementationStatus);
    }

    [Fact]
    public async Task ChangeActivityStatus_PlannedDirectlyToCompleted_ReturnsBadRequest()
    {
        await AuthenticateAsAdminAsync();
        var projectId = await CreateProjectAsync();
        var activityId = await CreateActivityAsync(projectId);

        var response = await _client.PostAsJsonAsync($"/api/activities/{activityId}/status", new { Status = "COMPLETED" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsDto>(JsonOptions);
        Assert.Equal("Activities.InvalidStatusTransition", problem!.Title);
    }

    [Fact]
    public async Task ChangeActivityStatus_ToCompleted_CapturesActuals()
    {
        await AuthenticateAsAdminAsync();
        var projectId = await CreateProjectAsync();
        var activityId = await CreateActivityAsync(projectId);

        var ongoingResponse = await _client.PostAsJsonAsync($"/api/activities/{activityId}/status", new { Status = "ONGOING" });
        ongoingResponse.EnsureSuccessStatusCode();

        var completedResponse = await _client.PostAsJsonAsync($"/api/activities/{activityId}/status", new
        {
            Status = "COMPLETED",
            ActualBeneficiaries = 42
        });

        Assert.Equal(HttpStatusCode.OK, completedResponse.StatusCode);
        var result = await completedResponse.Content.ReadFromJsonAsync<ChangeActivityStatusResponse>(JsonOptions);
        Assert.Equal("COMPLETED", result!.ImplementationStatus);
        Assert.Equal(42, result.ActualBeneficiaries);
        Assert.NotNull(result.ActualEndDate);
    }

    [Fact]
    public async Task ChangeActivityStatus_FromTerminalState_ReturnsBadRequest()
    {
        await AuthenticateAsAdminAsync();
        var projectId = await CreateProjectAsync();
        var activityId = await CreateActivityAsync(projectId);

        (await _client.PostAsJsonAsync($"/api/activities/{activityId}/status", new { Status = "ONGOING" })).EnsureSuccessStatusCode();
        (await _client.PostAsJsonAsync($"/api/activities/{activityId}/status", new { Status = "COMPLETED" })).EnsureSuccessStatusCode();

        var response = await _client.PostAsJsonAsync($"/api/activities/{activityId}/status", new { Status = "ONGOING" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ChangeActivityStatus_UnknownId_ReturnsNotFound()
    {
        await AuthenticateAsAdminAsync();

        var response = await _client.PostAsJsonAsync("/api/activities/999999/status", new { Status = "ONGOING" });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ChangeProjectStatus_ToCompletedWithOpenActivity_ReturnsBadRequest()
    {
        await AuthenticateAsAdminAsync();
        var projectId = await CreateProjectAsync();
        await CreateActivityAsync(projectId); // left PLANNED — still "open"

        var response = await _client.PostAsJsonAsync($"/api/projects/{projectId}/status", new { Status = "ONGOING" });
        response.EnsureSuccessStatusCode();

        var closeResponse = await _client.PostAsJsonAsync($"/api/projects/{projectId}/status", new { Status = "COMPLETED" });

        Assert.Equal(HttpStatusCode.BadRequest, closeResponse.StatusCode);
        var problem = await closeResponse.Content.ReadFromJsonAsync<ProblemDetailsDto>(JsonOptions);
        Assert.Equal("Projects.HasOpenActivities", problem!.Title);
    }

    [Fact]
    public async Task ChangeProjectStatus_PlannedDirectlyToCompleted_ReturnsBadRequest()
    {
        await AuthenticateAsAdminAsync();
        var projectId = await CreateProjectAsync(); // no activities at all — must fail on the
                                                     // transition table, not the open-activities guard

        var response = await _client.PostAsJsonAsync($"/api/projects/{projectId}/status", new { Status = "COMPLETED" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsDto>(JsonOptions);
        Assert.Equal("Projects.InvalidStatusTransition", problem!.Title);
    }

    [Fact]
    public async Task ChangeProjectStatus_ToCompletedAfterActivitiesClosed_Succeeds()
    {
        await AuthenticateAsAdminAsync();
        var projectId = await CreateProjectAsync();
        var activityId = await CreateActivityAsync(projectId);

        (await _client.PostAsJsonAsync($"/api/activities/{activityId}/status", new { Status = "ONGOING" })).EnsureSuccessStatusCode();
        (await _client.PostAsJsonAsync($"/api/activities/{activityId}/status", new { Status = "COMPLETED" })).EnsureSuccessStatusCode();
        (await _client.PostAsJsonAsync($"/api/projects/{projectId}/status", new { Status = "ONGOING" })).EnsureSuccessStatusCode();

        var response = await _client.PostAsJsonAsync($"/api/projects/{projectId}/status", new { Status = "COMPLETED" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<ChangeProjectStatusResponse>(JsonOptions);
        Assert.Equal("COMPLETED", result!.ImplementationStatus);
        Assert.NotNull(result.ClosedAt);
    }

    [Fact]
    public async Task ChangeProjectStatus_UnknownId_ReturnsNotFound()
    {
        await AuthenticateAsAdminAsync();

        var response = await _client.PostAsJsonAsync("/api/projects/999999/status", new { Status = "ONGOING" });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private sealed record LoginResponseDto(string AccessToken, string RefreshToken, DateTime RefreshTokenExpiresAt);

    private sealed record ProblemDetailsDto(string Title, string Detail);
}
