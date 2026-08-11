using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using philcare.Api.Features.Programs.Activities.CreateActivity;
using philcare.Api.Features.Programs.AidPrograms.CreateProgram;
using philcare.Api.Features.HumanResources.Staff.CreateStaffMember;
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

    private async Task<int> CreateStaffMemberAsync()
    {
        var response = await _client.PostAsJsonAsync("/api/staff", new
        {
            FullName = $"Staff-{Guid.NewGuid():N}",
            Position = "Field Officer",
            EmploymentType = "FULL_TIME"
        });
        response.EnsureSuccessStatusCode();
        var staffMember = await response.Content.ReadFromJsonAsync<CreateStaffMemberResponse>(JsonOptions);
        return staffMember!.Id;
    }

    [Fact]
    public async Task Assign_ThenListRoster_ShowsStaffMember()
    {
        await AuthenticateAsAdminAsync();
        var activityId = await CreateActivityAsync();
        var staffMemberId = await CreateStaffMemberAsync();

        var enrollResponse = await _client.PostAsJsonAsync($"/api/activities/{activityId}/participants", new
        {
            StaffMemberId = staffMemberId,
            RoleInActivity = "Attendee",
            AttendanceStatus = "PRESENT",
            ConsentRequired = false
        });
        Assert.Equal(HttpStatusCode.Created, enrollResponse.StatusCode);

        var rosterResponse = await _client.GetAsync($"/api/activities/{activityId}/participants");
        rosterResponse.EnsureSuccessStatusCode();
        var roster = await rosterResponse.Content.ReadFromJsonAsync<List<RosterRowDto>>(JsonOptions);

        Assert.Single(roster!);
        Assert.Equal(staffMemberId, roster![0].StaffMemberId);
    }

    [Fact]
    public async Task Assign_SameStaffMemberTwice_ReturnsConflict()
    {
        await AuthenticateAsAdminAsync();
        var activityId = await CreateActivityAsync();
        var staffMemberId = await CreateStaffMemberAsync();

        var first = await _client.PostAsJsonAsync($"/api/activities/{activityId}/participants", new
        {
            StaffMemberId = staffMemberId,
            ConsentRequired = false
        });
        first.EnsureSuccessStatusCode();

        var second = await _client.PostAsJsonAsync($"/api/activities/{activityId}/participants", new
        {
            StaffMemberId = staffMemberId,
            ConsentRequired = false
        });

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Assign_UnknownStaffMember_ReturnsNotFound()
    {
        await AuthenticateAsAdminAsync();
        var activityId = await CreateActivityAsync();

        var response = await _client.PostAsJsonAsync($"/api/activities/{activityId}/participants", new
        {
            StaffMemberId = 999999,
            ConsentRequired = false
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Remove_AssignedStaffMember_RemovesFromRoster()
    {
        await AuthenticateAsAdminAsync();
        var activityId = await CreateActivityAsync();
        var staffMemberId = await CreateStaffMemberAsync();

        var enrollResponse = await _client.PostAsJsonAsync($"/api/activities/{activityId}/participants", new
        {
            StaffMemberId = staffMemberId,
            ConsentRequired = false
        });
        enrollResponse.EnsureSuccessStatusCode();

        var removeResponse = await _client.DeleteAsync($"/api/activities/{activityId}/participants/{staffMemberId}");
        Assert.Equal(HttpStatusCode.NoContent, removeResponse.StatusCode);

        var rosterResponse = await _client.GetAsync($"/api/activities/{activityId}/participants");
        rosterResponse.EnsureSuccessStatusCode();
        var roster = await rosterResponse.Content.ReadFromJsonAsync<List<RosterRowDto>>(JsonOptions);

        Assert.Empty(roster!);
    }

    [Fact]
    public async Task Assign_AfterPriorRemoval_ReactivatesRoster()
    {
        await AuthenticateAsAdminAsync();
        var activityId = await CreateActivityAsync();
        var staffMemberId = await CreateStaffMemberAsync();

        var firstEnroll = await _client.PostAsJsonAsync($"/api/activities/{activityId}/participants", new
        {
            StaffMemberId = staffMemberId,
            ConsentRequired = false
        });
        firstEnroll.EnsureSuccessStatusCode();

        var removeResponse = await _client.DeleteAsync($"/api/activities/{activityId}/participants/{staffMemberId}");
        Assert.Equal(HttpStatusCode.NoContent, removeResponse.StatusCode);

        var reEnroll = await _client.PostAsJsonAsync($"/api/activities/{activityId}/participants", new
        {
            StaffMemberId = staffMemberId,
            RoleInActivity = "Re-enrolled",
            ConsentRequired = false
        });

        Assert.Equal(HttpStatusCode.Created, reEnroll.StatusCode);

        var rosterResponse = await _client.GetAsync($"/api/activities/{activityId}/participants");
        rosterResponse.EnsureSuccessStatusCode();
        var roster = await rosterResponse.Content.ReadFromJsonAsync<List<RosterRowDto>>(JsonOptions);

        Assert.Single(roster!);
        Assert.Equal("Re-enrolled", roster![0].RoleInActivity);
    }

    [Fact]
    public async Task Assign_InvalidAttendanceStatus_ReturnsBadRequest()
    {
        await AuthenticateAsAdminAsync();
        var activityId = await CreateActivityAsync();
        var staffMemberId = await CreateStaffMemberAsync();

        var response = await _client.PostAsJsonAsync($"/api/activities/{activityId}/participants", new
        {
            StaffMemberId = staffMemberId,
            AttendanceStatus = "NOT_A_REAL_STATUS",
            ConsentRequired = false
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateActivityParticipant_ChangesAttendanceStatus()
    {
        await AuthenticateAsAdminAsync();
        var activityId = await CreateActivityAsync();
        var staffMemberId = await CreateStaffMemberAsync();

        var enrollResponse = await _client.PostAsJsonAsync($"/api/activities/{activityId}/participants", new
        {
            StaffMemberId = staffMemberId,
            ConsentRequired = false
        });
        enrollResponse.EnsureSuccessStatusCode();

        var updateResponse = await _client.PutAsJsonAsync($"/api/activities/{activityId}/participants/{staffMemberId}", new
        {
            AttendanceStatus = "LATE",
            ConsentRequired = false
        });

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        var rosterResponse = await _client.GetAsync($"/api/activities/{activityId}/participants");
        rosterResponse.EnsureSuccessStatusCode();
        var roster = await rosterResponse.Content.ReadFromJsonAsync<List<RosterRowDto>>(JsonOptions);

        Assert.Equal("LATE", roster!.Single().AttendanceStatus);
    }

    [Fact]
    public async Task Remove_NotAssigned_ReturnsNotFound()
    {
        await AuthenticateAsAdminAsync();
        var activityId = await CreateActivityAsync();
        var staffMemberId = await CreateStaffMemberAsync();

        var response = await _client.DeleteAsync($"/api/activities/{activityId}/participants/{staffMemberId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private sealed record LoginResponseDto(string AccessToken, string RefreshToken, DateTime RefreshTokenExpiresAt);

    private sealed record RosterRowDto(int StaffMemberId, string StaffMemberName, string Position, string? RoleInActivity, string? AttendanceStatus);
}
