using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using philcare.Api.Common.Domain;
using philcare.Api.Features.Programs.Beneficiaries.CreateBeneficiary;
using philcare.Test.Common;
using Xunit;

namespace philcare.Test.Programs;

public class BeneficiariesTests : IClassFixture<TestWebAppFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly HttpClient _client;

    public BeneficiariesTests(TestWebAppFactory factory)
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

    private async Task AuthenticateAsViewerAsync()
    {
        // Registering a user with a specific role requires an Admin bearer token.
        await AuthenticateAsAdminAsync();

        var email = $"viewer-{Guid.NewGuid():N}@philcare.local";
        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            Email = email,
            Password = "Viewer@12345",
            Role = "Viewer"
        });
        registerResponse.EnsureSuccessStatusCode();

        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new { Email = email, Password = "Viewer@12345" });
        loginResponse.EnsureSuccessStatusCode();
        var body = await loginResponse.Content.ReadFromJsonAsync<LoginResponseDto>(JsonOptions);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body!.AccessToken);
    }

    [Fact]
    public async Task GetBeneficiaries_AsViewer_ReturnsForbidden()
    {
        await AuthenticateAsViewerAsync();

        var response = await _client.GetAsync("/api/beneficiaries");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreateBeneficiary_ValidRequest_DefaultsStatusToPending()
    {
        await AuthenticateAsAdminAsync();

        var response = await _client.PostAsJsonAsync("/api/beneficiaries", new
        {
            FullName = $"Beneficiary-{Guid.NewGuid():N}",
            BeneficiaryType = "INDIVIDUAL",
            Gender = "Female",
            VulnerabilityCategory = "WIDOW",
            ConsentOnFile = true
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var beneficiary = await response.Content.ReadFromJsonAsync<CreateBeneficiaryResponse>(JsonOptions);

        Assert.Equal("PENDING", beneficiary!.Status);
        Assert.Equal(Gender.Female, beneficiary.Gender);
        Assert.True(beneficiary.IsActive);
    }

    [Fact]
    public async Task CreateBeneficiary_EmptyName_ReturnsBadRequest()
    {
        await AuthenticateAsAdminAsync();

        var response = await _client.PostAsJsonAsync("/api/beneficiaries", new
        {
            FullName = "",
            BeneficiaryType = "INDIVIDUAL",
            Gender = "Male",
            ConsentOnFile = false
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateBeneficiary_ConsentNotOnFile_ReturnsBadRequest()
    {
        await AuthenticateAsAdminAsync();

        var response = await _client.PostAsJsonAsync("/api/beneficiaries", new
        {
            FullName = $"Beneficiary-{Guid.NewGuid():N}",
            BeneficiaryType = "INDIVIDUAL",
            Gender = "Male",
            ConsentOnFile = false
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsDto>(JsonOptions);
        Assert.Equal("Beneficiaries.ConsentRequired", problem!.Title);
    }

    [Fact]
    public async Task CreateBeneficiary_ElevatedSafeguardingCategory_SucceedsWithWarning()
    {
        await AuthenticateAsAdminAsync();

        var response = await _client.PostAsJsonAsync("/api/beneficiaries", new
        {
            FullName = $"Beneficiary-{Guid.NewGuid():N}",
            BeneficiaryType = "INDIVIDUAL",
            Gender = "Male",
            SafeguardingCategory = "HIGH_RISK",
            ConsentOnFile = true
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var beneficiary = await response.Content.ReadFromJsonAsync<CreateBeneficiaryResponse>(JsonOptions);

        Assert.True(beneficiary!.SafeguardingWarning);
        Assert.NotNull(beneficiary.SafeguardingMessage);
    }

    [Fact]
    public async Task GetBeneficiaries_FilteredByType_ReturnsOnlyMatching()
    {
        await AuthenticateAsAdminAsync();
        var uniqueType = $"TYPE-{Guid.NewGuid():N}"[..20];

        var createResponse = await _client.PostAsJsonAsync("/api/beneficiaries", new
        {
            FullName = $"Beneficiary-{Guid.NewGuid():N}",
            BeneficiaryType = uniqueType,
            Gender = "Unspecified",
            ConsentOnFile = true
        });
        createResponse.EnsureSuccessStatusCode();

        var listResponse = await _client.GetAsync($"/api/beneficiaries?beneficiaryType={uniqueType}");
        listResponse.EnsureSuccessStatusCode();
        var beneficiaries = await listResponse.Content.ReadFromJsonAsync<List<BeneficiaryListItemDto>>(JsonOptions);

        Assert.Single(beneficiaries!);
        Assert.Equal(uniqueType, beneficiaries![0].BeneficiaryType);
    }

    [Fact]
    public async Task GetBeneficiaryById_UnknownId_ReturnsNotFound()
    {
        await AuthenticateAsAdminAsync();

        var response = await _client.GetAsync("/api/beneficiaries/999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdateBeneficiary_ChangesStatusAndVulnerability()
    {
        await AuthenticateAsAdminAsync();

        var createResponse = await _client.PostAsJsonAsync("/api/beneficiaries", new
        {
            FullName = $"Beneficiary-{Guid.NewGuid():N}",
            BeneficiaryType = "INDIVIDUAL",
            Gender = "Male",
            ConsentOnFile = true
        });
        createResponse.EnsureSuccessStatusCode();
        var beneficiary = await createResponse.Content.ReadFromJsonAsync<CreateBeneficiaryResponse>(JsonOptions);

        var updateResponse = await _client.PutAsJsonAsync($"/api/beneficiaries/{beneficiary!.Id}", new
        {
            FullName = beneficiary.FullName,
            BeneficiaryType = "INDIVIDUAL",
            Gender = "Male",
            VulnerabilityCategory = "PWD",
            ConsentOnFile = true,
            Status = "VERIFIED",
            IsActive = true
        });

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        var getResponse = await _client.GetAsync($"/api/beneficiaries/{beneficiary.Id}");
        getResponse.EnsureSuccessStatusCode();
        var detail = await getResponse.Content.ReadFromJsonAsync<BeneficiaryDetailDto>(JsonOptions);

        Assert.Equal("VERIFIED", detail!.Status);
        Assert.Equal("PWD", detail.VulnerabilityCategory);
        Assert.True(detail.ConsentOnFile);
    }

    private sealed record LoginResponseDto(string AccessToken, string RefreshToken, DateTime RefreshTokenExpiresAt);

    private sealed record BeneficiaryListItemDto(int Id, string FullName, string BeneficiaryType, string Gender, string Status, bool IsActive);

    private sealed record BeneficiaryDetailDto(int Id, string FullName, string Status, string? VulnerabilityCategory, bool ConsentOnFile);

    private sealed record ProblemDetailsDto(string Title, string Detail);
}
