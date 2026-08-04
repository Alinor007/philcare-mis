using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using philcare.Api.Features.Governance.Decisions.CreateDecision;
using philcare.Api.Features.Governance.Meetings.CreateMeeting;
using philcare.Api.Features.Governance.Minutes.CreateMinutes;
using philcare.Api.Features.Governance.OrgBodies.CreateOrgBody;
using philcare.Test.Common;
using Xunit;

namespace philcare.Test.Governance;

public class MinutesAndDecisionsTests : IClassFixture<TestWebAppFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly HttpClient _client;

    public MinutesAndDecisionsTests(TestWebAppFactory factory)
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

    private async Task<int> CreateMeetingAsync(bool held)
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

        if (held)
        {
            var updateResponse = await _client.PutAsJsonAsync($"/api/governance/meetings/{meeting!.Id}", new
            {
                MeetingType = "BOARD_STRATEGIC",
                MeetingDate = DateTime.UtcNow,
                Mode = "IN_PERSON",
                Status = "Held"
            });
            updateResponse.EnsureSuccessStatusCode();
        }

        return meeting!.Id;
    }

    [Fact]
    public async Task CreateMinutes_ForScheduledNotHeldMeeting_ReturnsBadRequest()
    {
        await AuthenticateAsAdminAsync();
        var meetingId = await CreateMeetingAsync(held: false);

        var response = await _client.PostAsJsonAsync($"/api/governance/meetings/{meetingId}/minutes", new { });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateMinutes_ForHeldMeeting_Succeeds()
    {
        await AuthenticateAsAdminAsync();
        var meetingId = await CreateMeetingAsync(held: true);

        var response = await _client.PostAsJsonAsync($"/api/governance/meetings/{meetingId}/minutes", new { Summary = "The 2026-2028 Strategic Plan approved." });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var minutes = await response.Content.ReadFromJsonAsync<CreateMinutesResponse>(JsonOptions);
        Assert.Equal("Draft", minutes!.PublicationStatus);
    }

    [Fact]
    public async Task CreateMinutes_DuplicateForSameMeeting_ReturnsConflict()
    {
        await AuthenticateAsAdminAsync();
        var meetingId = await CreateMeetingAsync(held: true);

        var first = await _client.PostAsJsonAsync($"/api/governance/meetings/{meetingId}/minutes", new { });
        first.EnsureSuccessStatusCode();

        var second = await _client.PostAsJsonAsync($"/api/governance/meetings/{meetingId}/minutes", new { });

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task AddDecision_ThenListDecisions_ShowsDecision()
    {
        await AuthenticateAsAdminAsync();
        var meetingId = await CreateMeetingAsync(held: true);

        var minutesResponse = await _client.PostAsJsonAsync($"/api/governance/meetings/{meetingId}/minutes", new { });
        minutesResponse.EnsureSuccessStatusCode();
        var minutes = await minutesResponse.Content.ReadFromJsonAsync<CreateMinutesResponse>(JsonOptions);

        var decisionResponse = await _client.PostAsJsonAsync($"/api/governance/minutes/{minutes!.Id}/decisions", new
        {
            DecisionText = "Approve the 2026-2028 Strategic Plan.",
            DecisionStatus = "OPEN"
        });
        Assert.Equal(HttpStatusCode.Created, decisionResponse.StatusCode);

        var listResponse = await _client.GetAsync($"/api/governance/minutes/{minutes.Id}/decisions");
        listResponse.EnsureSuccessStatusCode();
        var decisions = await listResponse.Content.ReadFromJsonAsync<List<DecisionListItemDto>>(JsonOptions);

        Assert.Single(decisions!);
        Assert.Equal("OPEN", decisions![0].DecisionStatus);
    }

    [Fact]
    public async Task UpdateMinutes_ToPublished_ThenEditAttempt_ReturnsConflict()
    {
        await AuthenticateAsAdminAsync();
        var meetingId = await CreateMeetingAsync(held: true);

        var minutesResponse = await _client.PostAsJsonAsync($"/api/governance/meetings/{meetingId}/minutes", new { });
        minutesResponse.EnsureSuccessStatusCode();
        var minutes = await minutesResponse.Content.ReadFromJsonAsync<CreateMinutesResponse>(JsonOptions);

        var publishResponse = await _client.PutAsJsonAsync($"/api/governance/meetings/{meetingId}/minutes", new
        {
            Summary = "Final summary.",
            PublicationStatus = "Published"
        });
        Assert.Equal(HttpStatusCode.OK, publishResponse.StatusCode);

        var editAttempt = await _client.PutAsJsonAsync($"/api/governance/meetings/{meetingId}/minutes", new
        {
            Summary = "Trying to edit after publish.",
            PublicationStatus = "Published"
        });

        Assert.Equal(HttpStatusCode.Conflict, editAttempt.StatusCode);
    }

    [Fact]
    public async Task AddDecision_ToPublishedMinutes_ReturnsConflict()
    {
        await AuthenticateAsAdminAsync();
        var meetingId = await CreateMeetingAsync(held: true);

        var minutesResponse = await _client.PostAsJsonAsync($"/api/governance/meetings/{meetingId}/minutes", new { });
        minutesResponse.EnsureSuccessStatusCode();
        var minutes = await minutesResponse.Content.ReadFromJsonAsync<CreateMinutesResponse>(JsonOptions);

        var publishResponse = await _client.PutAsJsonAsync($"/api/governance/meetings/{meetingId}/minutes", new
        {
            PublicationStatus = "Published"
        });
        publishResponse.EnsureSuccessStatusCode();

        var decisionResponse = await _client.PostAsJsonAsync($"/api/governance/minutes/{minutes!.Id}/decisions", new
        {
            DecisionText = "Too late to add this.",
            DecisionStatus = "OPEN"
        });

        Assert.Equal(HttpStatusCode.Conflict, decisionResponse.StatusCode);
    }

    private sealed record LoginResponseDto(string AccessToken, string RefreshToken, DateTime RefreshTokenExpiresAt);
    private sealed record DecisionListItemDto(int Id, string DecisionText, string? ActionPoints, int? ResponsiblePersonId, string? ResponsiblePersonName, DateTime? DueDate, string DecisionStatus);
}
