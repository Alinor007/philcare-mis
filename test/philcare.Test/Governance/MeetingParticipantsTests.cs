using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using philcare.Api.Features.Governance.Assignments.CreateAssignment;
using philcare.Api.Features.Governance.Meetings.CreateMeeting;
using philcare.Api.Features.Governance.OrgBodies.CreateOrgBody;
using philcare.Api.Features.Governance.People.CreatePerson;
using philcare.Api.Features.Governance.Roles.CreateGovernanceRole;
using philcare.Test.Common;
using Xunit;

namespace philcare.Test.Governance;

public class MeetingParticipantsTests : IClassFixture<TestWebAppFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly HttpClient _client;

    public MeetingParticipantsTests(TestWebAppFactory factory)
    {
        _client = factory.CreateClient();
    }

    private async Task AuthenticateAsAdminAsync()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new { Email = "admin@philcare.local", Password = "Admin@12345" });
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<LoginResponseDto>(JsonOptions);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body!.AccessToken);
    }

    private async Task<int> CreateMeetingAsync()
    {
        var bodyResponse = await _client.PostAsJsonAsync("/api/governance/bodies", new { Name = $"Body-{Guid.NewGuid():N}", BodyType = "GOVERNANCE" });
        bodyResponse.EnsureSuccessStatusCode();
        var body = await bodyResponse.Content.ReadFromJsonAsync<CreateOrgBodyResponse>(JsonOptions);

        var meetingResponse = await _client.PostAsJsonAsync("/api/governance/meetings", new
        {
            OrgBodyId = body!.Id,
            MeetingType = "BOARD_STRATEGIC",
            MeetingDate = DateTime.UtcNow,
            Mode = "IN_PERSON"
        });
        meetingResponse.EnsureSuccessStatusCode();
        var meeting = await meetingResponse.Content.ReadFromJsonAsync<CreateMeetingResponse>(JsonOptions);
        return meeting!.Id;
    }

    private async Task<int> CreatePersonAsync()
    {
        var response = await _client.PostAsJsonAsync("/api/governance/people", new
        {
            FullName = $"Person-{Guid.NewGuid():N}",
            PersonCategory = "BOARD",
            DefaultVotingRights = true
        });
        response.EnsureSuccessStatusCode();
        var person = await response.Content.ReadFromJsonAsync<CreatePersonResponse>(JsonOptions);
        return person!.Id;
    }

    [Fact]
    public async Task AddBeneficiary_ThenListRoster_ShowsPerson()
    {
        await AuthenticateAsAdminAsync();
        var meetingId = await CreateMeetingAsync();
        var personId = await CreatePersonAsync();

        var addResponse = await _client.PostAsJsonAsync($"/api/governance/meetings/{meetingId}/participants", new
        {
            PersonId = personId,
            AttendanceStatus = "PRESENT",
            VotingRight = true,
            CountsForQuorum = true
        });
        Assert.Equal(HttpStatusCode.Created, addResponse.StatusCode);

        var rosterResponse = await _client.GetAsync($"/api/governance/meetings/{meetingId}/participants");
        rosterResponse.EnsureSuccessStatusCode();
        var roster = await rosterResponse.Content.ReadFromJsonAsync<List<RosterRowDto>>(JsonOptions);

        Assert.Single(roster!);
        Assert.Equal(personId, roster![0].PersonId);
    }

    [Fact]
    public async Task AddBeneficiary_SamePersonTwice_ReturnsConflict()
    {
        await AuthenticateAsAdminAsync();
        var meetingId = await CreateMeetingAsync();
        var personId = await CreatePersonAsync();

        var first = await _client.PostAsJsonAsync($"/api/governance/meetings/{meetingId}/participants", new
        {
            PersonId = personId,
            AttendanceStatus = "PRESENT",
            VotingRight = false,
            CountsForQuorum = false
        });
        first.EnsureSuccessStatusCode();

        var second = await _client.PostAsJsonAsync($"/api/governance/meetings/{meetingId}/participants", new
        {
            PersonId = personId,
            AttendanceStatus = "PRESENT",
            VotingRight = false,
            CountsForQuorum = false
        });

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task AddBeneficiary_AssignmentBelongingToDifferentPerson_ReturnsBadRequest()
    {
        await AuthenticateAsAdminAsync();
        var meetingId = await CreateMeetingAsync();
        var personId = await CreatePersonAsync();
        var otherPersonId = await CreatePersonAsync();

        var bodyResponse = await _client.PostAsJsonAsync("/api/governance/bodies", new { Name = $"Body-{Guid.NewGuid():N}", BodyType = "GOVERNANCE" });
        bodyResponse.EnsureSuccessStatusCode();
        var body = await bodyResponse.Content.ReadFromJsonAsync<CreateOrgBodyResponse>(JsonOptions);

        var roleResponse = await _client.PostAsJsonAsync("/api/governance/roles", new { Name = $"Role-{Guid.NewGuid():N}", RoleCategory = "BOARD" });
        roleResponse.EnsureSuccessStatusCode();
        var role = await roleResponse.Content.ReadFromJsonAsync<CreateGovernanceRoleResponse>(JsonOptions);

        var assignmentResponse = await _client.PostAsJsonAsync("/api/governance/assignments", new
        {
            PersonId = otherPersonId,
            OrgBodyId = body!.Id,
            GovernanceRoleId = role!.Id,
            StartDate = DateTime.UtcNow,
            IsPrimary = false,
            VotingRights = true,
            IsTemporary = false
        });
        assignmentResponse.EnsureSuccessStatusCode();
        var assignment = await assignmentResponse.Content.ReadFromJsonAsync<CreateAssignmentResponse>(JsonOptions);

        var response = await _client.PostAsJsonAsync($"/api/governance/meetings/{meetingId}/participants", new
        {
            PersonId = personId,
            AssignmentId = assignment!.Id,
            AttendanceStatus = "PRESENT",
            VotingRight = true,
            CountsForQuorum = true
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task RemoveBeneficiary_RemovesFromRoster()
    {
        await AuthenticateAsAdminAsync();
        var meetingId = await CreateMeetingAsync();
        var personId = await CreatePersonAsync();

        var addResponse = await _client.PostAsJsonAsync($"/api/governance/meetings/{meetingId}/participants", new
        {
            PersonId = personId,
            AttendanceStatus = "PRESENT",
            VotingRight = true,
            CountsForQuorum = true
        });
        addResponse.EnsureSuccessStatusCode();

        var removeResponse = await _client.DeleteAsync($"/api/governance/meetings/{meetingId}/participants/{personId}");
        Assert.Equal(HttpStatusCode.NoContent, removeResponse.StatusCode);

        var rosterResponse = await _client.GetAsync($"/api/governance/meetings/{meetingId}/participants");
        rosterResponse.EnsureSuccessStatusCode();
        var roster = await rosterResponse.Content.ReadFromJsonAsync<List<RosterRowDto>>(JsonOptions);

        Assert.Empty(roster!);
    }

    [Fact]
    public async Task RemoveBeneficiary_NotABeneficiary_ReturnsNotFound()
    {
        await AuthenticateAsAdminAsync();
        var meetingId = await CreateMeetingAsync();
        var personId = await CreatePersonAsync();

        var response = await _client.DeleteAsync($"/api/governance/meetings/{meetingId}/participants/{personId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private sealed record LoginResponseDto(string AccessToken, string RefreshToken, DateTime RefreshTokenExpiresAt);
    private sealed record RosterRowDto(int PersonId, string PersonFullName, string? RoleInMeeting, string AttendanceStatus, bool VotingRight, bool CountsForQuorum);
}
