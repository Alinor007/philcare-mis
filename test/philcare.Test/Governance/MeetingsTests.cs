using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using philcare.Api.Features.Governance.Meetings.CreateMeeting;
using philcare.Api.Features.Governance.OrgBodies.CreateOrgBody;
using philcare.Api.Features.Governance.People.CreatePerson;
using philcare.Test.Common;
using Xunit;

namespace philcare.Test.Governance;

public class MeetingsTests : IClassFixture<TestWebAppFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly HttpClient _client;

    public MeetingsTests(TestWebAppFactory factory)
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

    private async Task<int> CreateBodyAsync(string? quorumRule = null, string? decisionThreshold = null)
    {
        var response = await _client.PostAsJsonAsync("/api/governance/bodies", new
        {
            Name = $"Body-{Guid.NewGuid():N}",
            BodyType = "GOVERNANCE",
            QuorumRule = quorumRule,
            DecisionThreshold = decisionThreshold
        });
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<CreateOrgBodyResponse>(JsonOptions);
        return body!.Id;
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
    public async Task CreateMeeting_SnapshotsBodyQuorumPolicyAndDefaultsPublicationDeadline()
    {
        await AuthenticateAsAdminAsync();
        var bodyId = await CreateBodyAsync(quorumRule: "50% + 1", decisionThreshold: "Simple majority");
        var meetingDate = new DateTime(2026, 3, 25, 0, 0, 0, DateTimeKind.Utc);

        var response = await _client.PostAsJsonAsync("/api/governance/meetings", new
        {
            OrgBodyId = bodyId,
            MeetingType = "BOARD_STRATEGIC",
            MeetingDate = meetingDate,
            Mode = "IN_PERSON"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<CreateMeetingResponse>(JsonOptions);
        Assert.Equal("50% + 1", created!.QuorumRequired);
        Assert.Equal("Simple majority", created.DecisionThreshold);

        var detailResponse = await _client.GetAsync($"/api/governance/meetings/{created.Id}");
        detailResponse.EnsureSuccessStatusCode();
        var detail = await detailResponse.Content.ReadFromJsonAsync<MeetingDetailDto>(JsonOptions);
        Assert.Equal(meetingDate.AddDays(10), detail!.PublicationDeadline);
    }

    [Fact]
    public async Task CreateMeeting_UnknownBody_ReturnsNotFound()
    {
        await AuthenticateAsAdminAsync();

        var response = await _client.PostAsJsonAsync("/api/governance/meetings", new
        {
            OrgBodyId = 999999,
            MeetingType = "BOARD_STRATEGIC",
            MeetingDate = DateTime.UtcNow,
            Mode = "IN_PERSON"
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateMeeting_UnknownChairPerson_ReturnsNotFound()
    {
        await AuthenticateAsAdminAsync();
        var bodyId = await CreateBodyAsync();

        var response = await _client.PostAsJsonAsync("/api/governance/meetings", new
        {
            OrgBodyId = bodyId,
            MeetingType = "BOARD_STRATEGIC",
            MeetingDate = DateTime.UtcNow,
            Mode = "IN_PERSON",
            ChairPersonId = 999999
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetMeetings_FilteredByBody_ReturnsOnlyThatBodysMeetings()
    {
        await AuthenticateAsAdminAsync();
        var bodyId = await CreateBodyAsync();

        var createResponse = await _client.PostAsJsonAsync("/api/governance/meetings", new
        {
            OrgBodyId = bodyId,
            MeetingType = "BOARD_STRATEGIC",
            MeetingDate = DateTime.UtcNow,
            Mode = "IN_PERSON"
        });
        createResponse.EnsureSuccessStatusCode();

        var response = await _client.GetAsync($"/api/governance/meetings?bodyId={bodyId}");
        response.EnsureSuccessStatusCode();
        var meetings = await response.Content.ReadFromJsonAsync<List<MeetingListItemDto>>(JsonOptions);

        Assert.Single(meetings!);
        Assert.Equal(bodyId, meetings![0].OrgBodyId);
    }

    [Fact]
    public async Task GetMeetingQuorum_CountsPresentAndQuorumEligibleBeneficiaries()
    {
        await AuthenticateAsAdminAsync();
        var bodyId = await CreateBodyAsync(quorumRule: "50% + 1");

        var createResponse = await _client.PostAsJsonAsync("/api/governance/meetings", new
        {
            OrgBodyId = bodyId,
            MeetingType = "BOARD_STRATEGIC",
            MeetingDate = DateTime.UtcNow,
            Mode = "IN_PERSON"
        });
        createResponse.EnsureSuccessStatusCode();
        var meeting = await createResponse.Content.ReadFromJsonAsync<CreateMeetingResponse>(JsonOptions);

        var presentPersonId = await CreatePersonAsync();
        var absentPersonId = await CreatePersonAsync();

        var addPresent = await _client.PostAsJsonAsync($"/api/governance/meetings/{meeting!.Id}/participants", new
        {
            PersonId = presentPersonId,
            AttendanceStatus = "PRESENT",
            VotingRight = true,
            CountsForQuorum = true
        });
        addPresent.EnsureSuccessStatusCode();

        var addAbsent = await _client.PostAsJsonAsync($"/api/governance/meetings/{meeting.Id}/participants", new
        {
            PersonId = absentPersonId,
            AttendanceStatus = "ABSENT",
            VotingRight = true,
            CountsForQuorum = true
        });
        addAbsent.EnsureSuccessStatusCode();

        var quorumResponse = await _client.GetAsync($"/api/governance/meetings/{meeting.Id}/quorum");
        quorumResponse.EnsureSuccessStatusCode();
        var quorum = await quorumResponse.Content.ReadFromJsonAsync<QuorumDto>(JsonOptions);

        Assert.Equal(2, quorum!.EligibleCount);
        Assert.Equal(1, quorum.PresentCount);
        Assert.Equal(1, quorum.CountsForQuorumPresentCount);
        Assert.Equal("50% + 1", quorum.QuorumRequired);
        Assert.Equal(50.0, quorum.PresentPercentage);
    }

    private sealed record LoginResponseDto(string AccessToken, string RefreshToken, DateTime RefreshTokenExpiresAt);
    private sealed record MeetingListItemDto(int Id, int OrgBodyId, string OrgBodyName, string MeetingType, DateTime MeetingDate, string Status, bool HasMinutes);
    private sealed record MeetingDetailDto(
        int Id, int OrgBodyId, string OrgBodyName, string MeetingType, DateTime MeetingDate, string Mode, string? CalledBy,
        int? ChairPersonId, string? ChairPersonName, int? SecretaryPersonId, string? SecretaryPersonName, string? QuorumRequired,
        string? DecisionThreshold, string Status, DateTime? PublicationDeadline, string? Notes, int BeneficiaryCount, bool HasMinutes);
    private sealed record QuorumDto(
        int MeetingId, int EligibleCount, int PresentCount, int CountsForQuorumPresentCount, double? PresentPercentage,
        string? QuorumRequired, string? DecisionThreshold);
}
