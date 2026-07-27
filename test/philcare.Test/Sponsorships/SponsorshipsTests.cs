using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using philcare.Api.Features.Finance.Donors.CreateDonor;
using philcare.Api.Features.Programs.Participants.CreateParticipant;
using philcare.Api.Features.Sponsorships.CreateSponsorship;
using philcare.Test.Common;
using Xunit;

namespace philcare.Test.Sponsorships;

public class SponsorshipsTests : IClassFixture<TestWebAppFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly HttpClient _client;

    public SponsorshipsTests(TestWebAppFactory factory)
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

    private async Task<int> CreateDonorAsync()
    {
        var response = await _client.PostAsJsonAsync("/api/donors", new
        {
            Name = $"Donor-{Guid.NewGuid():N}",
            Type = "Individual",
            RiskRating = "Low",
            PepFlag = false,
            PrivacyConsent = true
        });
        response.EnsureSuccessStatusCode();
        var donor = await response.Content.ReadFromJsonAsync<CreateDonorResponse>(JsonOptions);
        return donor!.Id;
    }

    private async Task<int> CreateParticipantAsync()
    {
        var response = await _client.PostAsJsonAsync("/api/participants", new
        {
            FullName = $"Participant-{Guid.NewGuid():N}",
            ParticipantType = "BENEFICIARY",
            Gender = "Unspecified",
            ConsentOnFile = true
        });
        response.EnsureSuccessStatusCode();
        var participant = await response.Content.ReadFromJsonAsync<CreateParticipantResponse>(JsonOptions);
        return participant!.Id;
    }

    [Fact]
    public async Task CreateSponsorship_ValidRequest_DefaultsStatusToActive()
    {
        await AuthenticateAsAdminAsync();
        var donorId = await CreateDonorAsync();
        var participantId = await CreateParticipantAsync();

        var response = await _client.PostAsJsonAsync("/api/sponsorships", new
        {
            DonorId = donorId,
            ParticipantId = participantId,
            SponsorshipType = "CHILD",
            MonthlyAmountPhp = 1500m,
            StartDate = DateTime.UtcNow
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var sponsorship = await response.Content.ReadFromJsonAsync<CreateSponsorshipResponse>(JsonOptions);
        Assert.Equal("Active", sponsorship!.Status);
    }

    [Fact]
    public async Task CreateSponsorship_UnknownDonor_ReturnsNotFound()
    {
        await AuthenticateAsAdminAsync();
        var participantId = await CreateParticipantAsync();

        var response = await _client.PostAsJsonAsync("/api/sponsorships", new
        {
            DonorId = 999999,
            ParticipantId = participantId,
            SponsorshipType = "CHILD",
            MonthlyAmountPhp = 1500m,
            StartDate = DateTime.UtcNow
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateSponsorship_UnknownParticipant_ReturnsNotFound()
    {
        await AuthenticateAsAdminAsync();
        var donorId = await CreateDonorAsync();

        var response = await _client.PostAsJsonAsync("/api/sponsorships", new
        {
            DonorId = donorId,
            ParticipantId = 999999,
            SponsorshipType = "CHILD",
            MonthlyAmountPhp = 1500m,
            StartDate = DateTime.UtcNow
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateSponsorship_DuplicateActivePair_ReturnsConflict()
    {
        await AuthenticateAsAdminAsync();
        var donorId = await CreateDonorAsync();
        var participantId = await CreateParticipantAsync();

        var first = await _client.PostAsJsonAsync("/api/sponsorships", new
        {
            DonorId = donorId,
            ParticipantId = participantId,
            SponsorshipType = "CHILD",
            MonthlyAmountPhp = 1500m,
            StartDate = DateTime.UtcNow
        });
        first.EnsureSuccessStatusCode();

        var second = await _client.PostAsJsonAsync("/api/sponsorships", new
        {
            DonorId = donorId,
            ParticipantId = participantId,
            SponsorshipType = "FAMILY",
            MonthlyAmountPhp = 2000m,
            StartDate = DateTime.UtcNow
        });

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task ChangeStatus_ActiveToPausedToEnded_ThenEndedTransitionFails()
    {
        await AuthenticateAsAdminAsync();
        var donorId = await CreateDonorAsync();
        var participantId = await CreateParticipantAsync();

        var createResponse = await _client.PostAsJsonAsync("/api/sponsorships", new
        {
            DonorId = donorId,
            ParticipantId = participantId,
            SponsorshipType = "CHILD",
            MonthlyAmountPhp = 1500m,
            StartDate = DateTime.UtcNow
        });
        createResponse.EnsureSuccessStatusCode();
        var sponsorship = await createResponse.Content.ReadFromJsonAsync<CreateSponsorshipResponse>(JsonOptions);

        var pauseResponse = await _client.PostAsJsonAsync($"/api/sponsorships/{sponsorship!.Id}/status", new { Status = "Paused" });
        Assert.Equal(HttpStatusCode.OK, pauseResponse.StatusCode);

        var endResponse = await _client.PostAsJsonAsync($"/api/sponsorships/{sponsorship.Id}/status", new { Status = "Ended" });
        Assert.Equal(HttpStatusCode.OK, endResponse.StatusCode);

        var reactivateResponse = await _client.PostAsJsonAsync($"/api/sponsorships/{sponsorship.Id}/status", new { Status = "Active" });
        Assert.Equal(HttpStatusCode.Conflict, reactivateResponse.StatusCode);
    }

    [Fact]
    public async Task GetSponsorships_FilteredByParticipant_ReturnsOnlyThatParticipantsSponsorships()
    {
        await AuthenticateAsAdminAsync();
        var donorId = await CreateDonorAsync();
        var participantId = await CreateParticipantAsync();

        var createResponse = await _client.PostAsJsonAsync("/api/sponsorships", new
        {
            DonorId = donorId,
            ParticipantId = participantId,
            SponsorshipType = "CHILD",
            MonthlyAmountPhp = 1500m,
            StartDate = DateTime.UtcNow
        });
        createResponse.EnsureSuccessStatusCode();

        var response = await _client.GetAsync($"/api/sponsorships?participantId={participantId}");
        response.EnsureSuccessStatusCode();
        var sponsorships = await response.Content.ReadFromJsonAsync<List<SponsorshipListItemDto>>(JsonOptions);

        Assert.Single(sponsorships!);
        Assert.Equal(participantId, sponsorships![0].ParticipantId);
    }

    [Fact]
    public async Task GetSponsorshipSummary_ReturnsAggregatedRows()
    {
        await AuthenticateAsAdminAsync();
        var donorId = await CreateDonorAsync();
        var participantId = await CreateParticipantAsync();

        var createResponse = await _client.PostAsJsonAsync("/api/sponsorships", new
        {
            DonorId = donorId,
            ParticipantId = participantId,
            SponsorshipType = "STUDENT",
            MonthlyAmountPhp = 800m,
            StartDate = DateTime.UtcNow
        });
        createResponse.EnsureSuccessStatusCode();

        var response = await _client.GetAsync("/api/reports/sponsorship-summary");
        response.EnsureSuccessStatusCode();
        var rows = await response.Content.ReadFromJsonAsync<List<SponsorshipSummaryRowDto>>(JsonOptions);

        Assert.Contains(rows!, r => r.SponsorshipType == "STUDENT" && r.Status == "Active");
    }

    private sealed record LoginResponseDto(string AccessToken, string RefreshToken, DateTime RefreshTokenExpiresAt);

    private sealed record SponsorshipListItemDto(
        int Id, int DonorId, string DonorName, int ParticipantId, string ParticipantName, string SponsorshipType, decimal MonthlyAmountPhp, string Status);

    private sealed record SponsorshipSummaryRowDto(string SponsorshipType, string Status, int Count, decimal TotalMonthlyCommitmentPhp);
}
