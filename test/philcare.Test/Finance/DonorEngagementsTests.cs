using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using philcare.Api.Features.Finance.DonorEngagements.CreateDonorEngagement;
using philcare.Test.Common;
using Xunit;

namespace philcare.Test.Finance;

public class DonorEngagementsTests : IClassFixture<TestWebAppFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly HttpClient _client;

    public DonorEngagementsTests(TestWebAppFactory factory)
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
        var donor = await response.Content.ReadFromJsonAsync<DonorDto>(JsonOptions);
        return donor!.Id;
    }

    private async Task<int> CreateEngagementAsync(int donorId, string engagementType = "CALL", bool followUpRequired = false, DateTime? followUpDate = null)
    {
        var response = await _client.PostAsJsonAsync("/api/donor-engagements", new
        {
            DonorId = donorId,
            EngagementType = engagementType,
            EngagementDate = DateTime.UtcNow,
            Subject = $"Subject-{Guid.NewGuid():N}",
            FollowUpRequired = followUpRequired,
            FollowUpDate = followUpDate
        });
        response.EnsureSuccessStatusCode();
        var engagement = await response.Content.ReadFromJsonAsync<CreateDonorEngagementResponse>(JsonOptions);
        return engagement!.Id;
    }

    [Fact]
    public async Task CreateDonorEngagement_WithoutAuthentication_ReturnsUnauthorized()
    {
        var response = await _client.PostAsJsonAsync("/api/donor-engagements", new
        {
            DonorId = 1,
            EngagementType = "CALL",
            EngagementDate = DateTime.UtcNow,
            Subject = "Should not be created",
            FollowUpRequired = false
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateDonorEngagement_ValidRequest_StampsCreatedByFromToken()
    {
        await AuthenticateAsAdminAsync();
        var donorId = await CreateDonorAsync();

        var response = await _client.PostAsJsonAsync("/api/donor-engagements", new
        {
            DonorId = donorId,
            EngagementType = "MEETING",
            EngagementDate = DateTime.UtcNow,
            Subject = "Quarterly check-in",
            FollowUpRequired = false
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var engagement = await response.Content.ReadFromJsonAsync<CreateDonorEngagementResponse>(JsonOptions);
        Assert.Equal("admin@philcare.local", engagement!.CreatedBy);
        Assert.Equal(donorId, engagement.DonorId);
    }

    [Fact]
    public async Task CreateDonorEngagement_UnknownEngagementType_ReturnsBadRequest()
    {
        await AuthenticateAsAdminAsync();
        var donorId = await CreateDonorAsync();

        var response = await _client.PostAsJsonAsync("/api/donor-engagements", new
        {
            DonorId = donorId,
            EngagementType = "NOT_A_REAL_TYPE",
            EngagementDate = DateTime.UtcNow,
            Subject = "Bad type",
            FollowUpRequired = false
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateDonorEngagement_UnknownDonor_ReturnsNotFound()
    {
        await AuthenticateAsAdminAsync();

        var response = await _client.PostAsJsonAsync("/api/donor-engagements", new
        {
            DonorId = 999999,
            EngagementType = "CALL",
            EngagementDate = DateTime.UtcNow,
            Subject = "Ghost donor",
            FollowUpRequired = false
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateDonorEngagement_FollowUpRequiredWithoutDate_ReturnsBadRequest()
    {
        await AuthenticateAsAdminAsync();
        var donorId = await CreateDonorAsync();

        var response = await _client.PostAsJsonAsync("/api/donor-engagements", new
        {
            DonorId = donorId,
            EngagementType = "CALL",
            EngagementDate = DateTime.UtcNow,
            Subject = "Needs follow-up",
            FollowUpRequired = true
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetDonorEngagements_FilteredByDonorId_ReturnsOnlyMatching()
    {
        await AuthenticateAsAdminAsync();
        var donorAId = await CreateDonorAsync();
        var donorBId = await CreateDonorAsync();
        var engagementAId = await CreateEngagementAsync(donorAId);
        await CreateEngagementAsync(donorBId);

        var response = await _client.GetAsync($"/api/donor-engagements?donorId={donorAId}");
        response.EnsureSuccessStatusCode();
        var engagements = await response.Content.ReadFromJsonAsync<List<DonorEngagementListItemDto>>(JsonOptions);

        Assert.Contains(engagements!, e => e.Id == engagementAId);
        Assert.All(engagements!, e => Assert.Equal(donorAId, e.DonorId));
    }

    [Fact]
    public async Task GetDonorEngagements_OrdersByEngagementDateDescending()
    {
        await AuthenticateAsAdminAsync();
        var donorId = await CreateDonorAsync();
        var olderId = await CreateEngagementAsync(donorId);
        var newerId = await CreateEngagementAsync(donorId);

        var response = await _client.GetAsync($"/api/donor-engagements?donorId={donorId}");
        response.EnsureSuccessStatusCode();
        var engagements = await response.Content.ReadFromJsonAsync<List<DonorEngagementListItemDto>>(JsonOptions);

        // Both were created with DateTime.UtcNow in quick succession, so the tie is broken by Id desc.
        var ids = engagements!.Select(e => e.Id).ToList();
        Assert.True(ids.IndexOf(newerId) < ids.IndexOf(olderId));
    }

    [Fact]
    public async Task GetDonorEngagements_FilteredByFollowUpRequired_ReturnsOnlyFlagged()
    {
        await AuthenticateAsAdminAsync();
        var donorId = await CreateDonorAsync();
        var followUpId = await CreateEngagementAsync(donorId, followUpRequired: true, followUpDate: DateTime.UtcNow.AddDays(7));
        await CreateEngagementAsync(donorId, followUpRequired: false);

        var response = await _client.GetAsync($"/api/donor-engagements?donorId={donorId}&followUpRequired=true");
        response.EnsureSuccessStatusCode();
        var engagements = await response.Content.ReadFromJsonAsync<List<DonorEngagementListItemDto>>(JsonOptions);

        Assert.Contains(engagements!, e => e.Id == followUpId);
        Assert.All(engagements!, e => Assert.True(e.FollowUpRequired));
    }

    [Fact]
    public async Task UpdateDonorEngagement_UnknownId_ReturnsNotFound()
    {
        await AuthenticateAsAdminAsync();

        var response = await _client.PutAsJsonAsync("/api/donor-engagements/999999", new
        {
            EngagementType = "CALL",
            EngagementDate = DateTime.UtcNow,
            Subject = "Ghost engagement",
            FollowUpRequired = false
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdateDonorEngagement_ValidRequest_PersistsChanges()
    {
        await AuthenticateAsAdminAsync();
        var donorId = await CreateDonorAsync();
        var engagementId = await CreateEngagementAsync(donorId);

        var updateResponse = await _client.PutAsJsonAsync($"/api/donor-engagements/{engagementId}", new
        {
            EngagementType = "EMAIL",
            EngagementDate = DateTime.UtcNow,
            Subject = "Updated subject",
            FollowUpRequired = false
        });
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        var listResponse = await _client.GetAsync($"/api/donor-engagements?donorId={donorId}");
        listResponse.EnsureSuccessStatusCode();
        var engagements = await listResponse.Content.ReadFromJsonAsync<List<DonorEngagementListItemDto>>(JsonOptions);

        var updated = engagements!.Single(e => e.Id == engagementId);
        Assert.Equal("EMAIL", updated.EngagementType);
        Assert.Equal("Updated subject", updated.Subject);
    }

    [Fact]
    public async Task UpdateDonorEngagement_UnknownEngagementType_ReturnsBadRequest()
    {
        await AuthenticateAsAdminAsync();
        var donorId = await CreateDonorAsync();
        var engagementId = await CreateEngagementAsync(donorId);

        var response = await _client.PutAsJsonAsync($"/api/donor-engagements/{engagementId}", new
        {
            EngagementType = "NOT_A_REAL_TYPE",
            EngagementDate = DateTime.UtcNow,
            Subject = "Bad type",
            FollowUpRequired = false
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private sealed record LoginResponseDto(string AccessToken, string RefreshToken, DateTime RefreshTokenExpiresAt);

    private sealed record DonorDto(int Id, string Name);

    private sealed record DonorEngagementListItemDto(
        int Id, int DonorId, string DonorName, string EngagementType, DateTime EngagementDate,
        string Subject, string? Notes, bool FollowUpRequired, DateTime? FollowUpDate,
        string? CreatedBy, DateTime CreatedAt);
}
