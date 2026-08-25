using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using philcare.Api.Features.Finance.Domain;
using philcare.Api.Features.Finance.Donors.CreateDonor;
using philcare.Api.Features.Finance.Donors.UpdateDonor;
using philcare.Test.Common;
using Xunit;

namespace philcare.Test.Finance;

public class DonorsTests : IClassFixture<TestWebAppFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly HttpClient _client;

    public DonorsTests(TestWebAppFactory factory)
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

    [Fact]
    public async Task CreateDonor_WithoutAuthentication_ReturnsUnauthorized()
    {
        var response = await _client.PostAsJsonAsync("/api/donors", new
        {
            Name = "Should Not Be Created",
            Type = "Individual",
            RiskRating = "Low",
            PepFlag = false,
            PrivacyConsent = true
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateDonor_ValidRequest_DefaultsKydStatusToPending()
    {
        await AuthenticateAsAdminAsync();

        var response = await _client.PostAsJsonAsync("/api/donors", new
        {
            Name = $"Donor-{Guid.NewGuid():N}",
            Type = "Organization",
            Email = "donor@example.org",
            Country = "Philippines",
            RiskRating = "Medium",
            PepFlag = false,
            PrivacyConsent = true
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var donor = await response.Content.ReadFromJsonAsync<CreateDonorResponse>(JsonOptions);

        Assert.True(donor!.Id > 0);
        Assert.Equal(KydStatus.Pending, donor.KydStatus);
        Assert.True(donor.IsActive);
        Assert.Equal(RiskRating.Medium, donor.RiskRating);
    }

    [Fact]
    public async Task CreateDonor_EmptyName_ReturnsBadRequest()
    {
        await AuthenticateAsAdminAsync();

        var response = await _client.PostAsJsonAsync("/api/donors", new
        {
            Name = "",
            Type = "Individual",
            RiskRating = "Low",
            PepFlag = false,
            PrivacyConsent = true
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetDonorById_UnknownId_ReturnsNotFound()
    {
        await AuthenticateAsAdminAsync();

        var response = await _client.GetAsync("/api/donors/999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetDonors_WithoutAuthentication_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/donors");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetDonors_AfterCreate_ContainsTheNewDonor()
    {
        await AuthenticateAsAdminAsync();
        var uniqueName = $"Donor-{Guid.NewGuid():N}";

        var createResponse = await _client.PostAsJsonAsync("/api/donors", new
        {
            Name = uniqueName,
            Type = "Individual",
            RiskRating = "Low",
            PepFlag = false,
            PrivacyConsent = true
        });
        createResponse.EnsureSuccessStatusCode();

        var listResponse = await _client.GetAsync("/api/donors");
        listResponse.EnsureSuccessStatusCode();
        var donors = await listResponse.Content.ReadFromJsonAsync<List<DonorListItemDto>>(JsonOptions);

        Assert.Contains(donors!, d => d.Name == uniqueName);
    }

    [Fact]
    public async Task UpdateDonor_ClearsKycAndChangesRiskRating()
    {
        await AuthenticateAsAdminAsync();

        var createResponse = await _client.PostAsJsonAsync("/api/donors", new
        {
            Name = $"Donor-{Guid.NewGuid():N}",
            Type = "Individual",
            RiskRating = "Low",
            PepFlag = false,
            PrivacyConsent = true
        });
        createResponse.EnsureSuccessStatusCode();
        var donor = await createResponse.Content.ReadFromJsonAsync<CreateDonorResponse>(JsonOptions);

        var updateResponse = await _client.PutAsJsonAsync($"/api/donors/{donor!.Id}", new
        {
            Name = donor.Name,
            Type = "Individual",
            Country = "Philippines",
            IsActive = true,
            KydStatus = "Cleared",
            RiskRating = "High",
            PepFlag = true,
            PrivacyConsent = true
        });

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        var updated = await updateResponse.Content.ReadFromJsonAsync<UpdateDonorResponse>(JsonOptions);

        Assert.Equal(KydStatus.Cleared, updated!.KydStatus);
        Assert.Equal(RiskRating.High, updated.RiskRating);
        Assert.True(updated.PepFlag);
    }

    [Fact]
    public async Task UpdateDonor_UnknownId_ReturnsNotFound()
    {
        await AuthenticateAsAdminAsync();

        var response = await _client.PutAsJsonAsync("/api/donors/999999", new
        {
            Name = "Ghost",
            Type = "Individual",
            IsActive = true,
            KydStatus = "Pending",
            RiskRating = "Low",
            PepFlag = false,
            PrivacyConsent = true
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ReviewDonorKyd_ChangesStatus_Succeeds()
    {
        await AuthenticateAsAdminAsync();
        var donorId = await CreateDonorAsync($"Donor-{Guid.NewGuid():N}");

        var response = await _client.PostAsJsonAsync($"/api/donors/{donorId}/kyd-status", new { Status = "Review" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<ReviewKydResponseDto>(JsonOptions);
        Assert.Equal("Review", result!.KydStatus);

        var getResponse = await _client.GetAsync($"/api/donors/{donorId}");
        getResponse.EnsureSuccessStatusCode();
        var donor = await getResponse.Content.ReadFromJsonAsync<DonorDetailDto>(JsonOptions);
        Assert.Equal("Review", donor!.KydStatus);
    }

    /// <summary>Setting the same status twice is a no-op guard, not a silent success.</summary>
    [Fact]
    public async Task ReviewDonorKyd_SameStatus_ReturnsConflict()
    {
        await AuthenticateAsAdminAsync();
        var donorId = await CreateDonorAsync($"Donor-{Guid.NewGuid():N}");

        // New donors default to Pending.
        var response = await _client.PostAsJsonAsync($"/api/donors/{donorId}/kyd-status", new { Status = "Pending" });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task ReviewDonorKyd_UnknownDonor_ReturnsNotFound()
    {
        await AuthenticateAsAdminAsync();

        var response = await _client.PostAsJsonAsync("/api/donors/999999/kyd-status", new { Status = "Cleared" });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private async Task<int> CreateDonorAsync(string name)
    {
        var response = await _client.PostAsJsonAsync("/api/donors", new
        {
            Name = name,
            Type = "Individual",
            RiskRating = "Low",
            PepFlag = false,
            PrivacyConsent = true
        });
        response.EnsureSuccessStatusCode();
        var donor = await response.Content.ReadFromJsonAsync<CreateDonorResponse>(JsonOptions);
        return donor!.Id;
    }

    private sealed record LoginResponseDto(string AccessToken, string RefreshToken, DateTime RefreshTokenExpiresAt);

    private sealed record DonorListItemDto(int Id, string Name, string Type, string? Email, string? Phone, bool IsActive);

    private sealed record ReviewKydResponseDto(int Id, string KydStatus);

    private sealed record DonorDetailDto(int Id, string KydStatus);
}
