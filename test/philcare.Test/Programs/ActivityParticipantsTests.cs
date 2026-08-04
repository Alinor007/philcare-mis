using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using philcare.Api.Features.Programs.Activities.CreateActivity;
using philcare.Api.Features.Programs.AidPrograms.CreateProgram;
using philcare.Api.Features.Programs.Participants.CreateParticipant;
using philcare.Api.Features.Programs.Projects.CreateProject;
using philcare.Test.Common;
using Xunit;

namespace philcare.Test.Programs;

public class ActivityParticipantsTests : IClassFixture<TestWebAppFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly HttpClient _client;

    public ActivityParticipantsTests(TestWebAppFactory factory)
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

    private async Task<int> CreateActivityAsync()
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
            Budget = 1000m
        });
        activityResponse.EnsureSuccessStatusCode();
        var activity = await activityResponse.Content.ReadFromJsonAsync<CreateActivityResponse>(JsonOptions);
        return activity!.Id;
    }

    private async Task<int> CreateParticipantAsync()
    {
        var response = await _client.PostAsJsonAsync("/api/participants", new
        {
            FullName = $"Participant-{Guid.NewGuid():N}",
            ParticipantType = "ATTENDEE",
            BeneficiaryType = "INDIVIDUAL",
            Gender = "Unspecified",
            ConsentOnFile = false
        });
        response.EnsureSuccessStatusCode();
        var participant = await response.Content.ReadFromJsonAsync<CreateParticipantResponse>(JsonOptions);
        return participant!.Id;
    }

    [Fact]
    public async Task Enroll_ThenListRoster_ShowsParticipant()
    {
        await AuthenticateAsAdminAsync();
        var activityId = await CreateActivityAsync();
        var participantId = await CreateParticipantAsync();

        var enrollResponse = await _client.PostAsJsonAsync($"/api/activities/{activityId}/participants", new
        {
            ParticipantId = participantId,
            RoleInActivity = "Attendee",
            AttendanceStatus = "Present",
            ConsentRequired = false
        });
        Assert.Equal(HttpStatusCode.Created, enrollResponse.StatusCode);

        var rosterResponse = await _client.GetAsync($"/api/activities/{activityId}/participants");
        rosterResponse.EnsureSuccessStatusCode();
        var roster = await rosterResponse.Content.ReadFromJsonAsync<List<RosterRowDto>>(JsonOptions);

        Assert.Single(roster!);
        Assert.Equal(participantId, roster![0].ParticipantId);
    }

    [Fact]
    public async Task Enroll_SameParticipantTwice_ReturnsConflict()
    {
        await AuthenticateAsAdminAsync();
        var activityId = await CreateActivityAsync();
        var participantId = await CreateParticipantAsync();

        var first = await _client.PostAsJsonAsync($"/api/activities/{activityId}/participants", new
        {
            ParticipantId = participantId,
            ConsentRequired = false
        });
        first.EnsureSuccessStatusCode();

        var second = await _client.PostAsJsonAsync($"/api/activities/{activityId}/participants", new
        {
            ParticipantId = participantId,
            ConsentRequired = false
        });

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Enroll_UnknownParticipant_ReturnsNotFound()
    {
        await AuthenticateAsAdminAsync();
        var activityId = await CreateActivityAsync();

        var response = await _client.PostAsJsonAsync($"/api/activities/{activityId}/participants", new
        {
            ParticipantId = 999999,
            ConsentRequired = false
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Remove_EnrolledParticipant_RemovesFromRoster()
    {
        await AuthenticateAsAdminAsync();
        var activityId = await CreateActivityAsync();
        var participantId = await CreateParticipantAsync();

        var enrollResponse = await _client.PostAsJsonAsync($"/api/activities/{activityId}/participants", new
        {
            ParticipantId = participantId,
            ConsentRequired = false
        });
        enrollResponse.EnsureSuccessStatusCode();

        var removeResponse = await _client.DeleteAsync($"/api/activities/{activityId}/participants/{participantId}");
        Assert.Equal(HttpStatusCode.NoContent, removeResponse.StatusCode);

        var rosterResponse = await _client.GetAsync($"/api/activities/{activityId}/participants");
        rosterResponse.EnsureSuccessStatusCode();
        var roster = await rosterResponse.Content.ReadFromJsonAsync<List<RosterRowDto>>(JsonOptions);

        Assert.Empty(roster!);
    }

    [Fact]
    public async Task Remove_NotEnrolled_ReturnsNotFound()
    {
        await AuthenticateAsAdminAsync();
        var activityId = await CreateActivityAsync();
        var participantId = await CreateParticipantAsync();

        var response = await _client.DeleteAsync($"/api/activities/{activityId}/participants/{participantId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private sealed record LoginResponseDto(string AccessToken, string RefreshToken, DateTime RefreshTokenExpiresAt);

    private sealed record RosterRowDto(int ParticipantId, string ParticipantName, string ParticipantType, string? RoleInActivity, string? AttendanceStatus);
}
