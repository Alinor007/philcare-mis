using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using philcare.Api.Features.Programs.Activities.CreateActivity;
using philcare.Api.Features.Programs.AidPrograms.CreateProgram;
using philcare.Api.Features.Programs.Projects.CreateProject;
using philcare.Api.Features.HumanResources.Volunteers.CreateVolunteer;
using philcare.Test.Common;
using Xunit;

namespace philcare.Test.HumanResources.Volunteers;

public class ActivityVolunteersTests : IClassFixture<TestWebAppFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly HttpClient _client;

    public ActivityVolunteersTests(TestWebAppFactory factory)
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

    private async Task<int> CreateActivityAsync(string? safeguardingRisk = null)
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

        var activityResponse = await _client.PostAsJsonAsync("/api/activities", new
        {
            ProjectId = project!.Id,
            Name = $"Activity-{Guid.NewGuid():N}",
            ActivityType = "OUTREACH",
            Budget = 1000m,
            SafeguardingRisk = safeguardingRisk
        });
        activityResponse.EnsureSuccessStatusCode();
        var activity = await activityResponse.Content.ReadFromJsonAsync<CreateActivityResponse>(JsonOptions);
        return activity!.Id;
    }

    private async Task<int> CreateVolunteerAsync(bool orientationCompleted = false)
    {
        var response = await _client.PostAsJsonAsync("/api/volunteers", new
        {
            FullName = $"Volunteer-{Guid.NewGuid():N}",
            Gender = "Unspecified",
            OrientationCompleted = orientationCompleted,
            CodeOfConductSigned = false,
            PoliceClearanceOnFile = false
        });
        response.EnsureSuccessStatusCode();
        var volunteer = await response.Content.ReadFromJsonAsync<CreateVolunteerResponse>(JsonOptions);
        return volunteer!.Id;
    }

    [Fact]
    public async Task Enroll_ThenListRoster_ShowsVolunteer()
    {
        await AuthenticateAsAdminAsync();
        var activityId = await CreateActivityAsync();
        var volunteerId = await CreateVolunteerAsync();

        var enrollResponse = await _client.PostAsJsonAsync($"/api/activities/{activityId}/volunteers", new
        {
            VolunteerId = volunteerId,
            RoleInActivity = "Logistics"
        });
        Assert.Equal(HttpStatusCode.Created, enrollResponse.StatusCode);

        var rosterResponse = await _client.GetAsync($"/api/activities/{activityId}/volunteers");
        rosterResponse.EnsureSuccessStatusCode();
        var roster = await rosterResponse.Content.ReadFromJsonAsync<List<RosterRowDto>>(JsonOptions);

        Assert.Single(roster!);
        Assert.Equal(volunteerId, roster![0].VolunteerId);
    }

    [Fact]
    public async Task Enroll_SameVolunteerTwice_ReturnsConflict()
    {
        await AuthenticateAsAdminAsync();
        var activityId = await CreateActivityAsync();
        var volunteerId = await CreateVolunteerAsync();

        var first = await _client.PostAsJsonAsync($"/api/activities/{activityId}/volunteers", new { VolunteerId = volunteerId });
        first.EnsureSuccessStatusCode();

        var second = await _client.PostAsJsonAsync($"/api/activities/{activityId}/volunteers", new { VolunteerId = volunteerId });

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Enroll_IntoSafeguardingRiskActivity_WithoutOrientation_ReturnsBadRequest()
    {
        await AuthenticateAsAdminAsync();
        var activityId = await CreateActivityAsync(safeguardingRisk: "CHILD");
        var volunteerId = await CreateVolunteerAsync(orientationCompleted: false);

        var response = await _client.PostAsJsonAsync($"/api/activities/{activityId}/volunteers", new { VolunteerId = volunteerId });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Enroll_IntoSafeguardingRiskActivity_WithOrientation_Succeeds()
    {
        await AuthenticateAsAdminAsync();
        var activityId = await CreateActivityAsync(safeguardingRisk: "CHILD");
        var volunteerId = await CreateVolunteerAsync(orientationCompleted: true);

        var response = await _client.PostAsJsonAsync($"/api/activities/{activityId}/volunteers", new { VolunteerId = volunteerId });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Remove_EnrolledVolunteer_RemovesFromRoster()
    {
        await AuthenticateAsAdminAsync();
        var activityId = await CreateActivityAsync();
        var volunteerId = await CreateVolunteerAsync();

        var enrollResponse = await _client.PostAsJsonAsync($"/api/activities/{activityId}/volunteers", new { VolunteerId = volunteerId });
        enrollResponse.EnsureSuccessStatusCode();

        var removeResponse = await _client.DeleteAsync($"/api/activities/{activityId}/volunteers/{volunteerId}");
        Assert.Equal(HttpStatusCode.NoContent, removeResponse.StatusCode);

        var rosterResponse = await _client.GetAsync($"/api/activities/{activityId}/volunteers");
        rosterResponse.EnsureSuccessStatusCode();
        var roster = await rosterResponse.Content.ReadFromJsonAsync<List<RosterRowDto>>(JsonOptions);

        Assert.Empty(roster!);
    }

    private sealed record LoginResponseDto(string AccessToken, string RefreshToken, DateTime RefreshTokenExpiresAt);

    private sealed record RosterRowDto(int VolunteerId, string VolunteerName, string? RoleInActivity, string? AttendanceStatus, decimal? HoursServed);
}
