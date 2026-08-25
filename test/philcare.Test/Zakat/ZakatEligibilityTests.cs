using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using philcare.Api.Features.Programs.Beneficiaries.CreateBeneficiary;
using philcare.Api.Features.Zakat.CreateZakatEligibility;
using philcare.Test.Common;
using Xunit;

namespace philcare.Test.Zakat;

public class ZakatEligibilityTests : IClassFixture<TestWebAppFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly HttpClient _client;

    public ZakatEligibilityTests(TestWebAppFactory factory)
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

    private async Task<int> CreateBeneficiaryAsync()
    {
        var response = await _client.PostAsJsonAsync("/api/beneficiaries", new
        {
            FullName = $"Beneficiary-{Guid.NewGuid():N}",
            BeneficiaryType = "INDIVIDUAL",
            Gender = "Unspecified",
            ConsentOnFile = true
        });
        response.EnsureSuccessStatusCode();
        var beneficiary = await response.Content.ReadFromJsonAsync<CreateBeneficiaryResponse>(JsonOptions);
        return beneficiary!.Id;
    }

    private async Task<CreateZakatEligibilityResponse> CreateEligibilityAsync(int beneficiaryId)
    {
        var response = await _client.PostAsJsonAsync("/api/zakat-eligibilities", new
        {
            BeneficiaryId = beneficiaryId,
            AsnafCategory = "FUQARA",
            MonthlyIncomePhp = 3000m,
            HouseholdSize = 4,
            AssessmentDate = DateTime.UtcNow,
            AssessedBy = "Case Worker A"
        });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CreateZakatEligibilityResponse>(JsonOptions))!;
    }

    [Fact]
    public async Task CreateEligibility_ValidRequest_StartsAsDraft()
    {
        await AuthenticateAsAdminAsync();
        var beneficiaryId = await CreateBeneficiaryAsync();

        var eligibility = await CreateEligibilityAsync(beneficiaryId);

        Assert.Equal("Draft", eligibility.Status);
    }

    [Fact]
    public async Task UpdateEligibility_WhileDraft_Succeeds()
    {
        await AuthenticateAsAdminAsync();
        var beneficiaryId = await CreateBeneficiaryAsync();
        var eligibility = await CreateEligibilityAsync(beneficiaryId);

        var response = await _client.PutAsJsonAsync($"/api/zakat-eligibilities/{eligibility.Id}", new
        {
            AsnafCategory = "MASAKIN",
            MonthlyIncomePhp = 2500m,
            HouseholdSize = 5,
            AssessmentDate = DateTime.UtcNow,
            AssessedBy = "Case Worker B"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task UpdateEligibility_AfterSubmit_ReturnsConflict()
    {
        await AuthenticateAsAdminAsync();
        var beneficiaryId = await CreateBeneficiaryAsync();
        var eligibility = await CreateEligibilityAsync(beneficiaryId);

        var submitResponse = await _client.PostAsync($"/api/zakat-eligibilities/{eligibility.Id}/submit", null);
        submitResponse.EnsureSuccessStatusCode();

        var updateResponse = await _client.PutAsJsonAsync($"/api/zakat-eligibilities/{eligibility.Id}", new
        {
            AsnafCategory = "MASAKIN",
            AssessmentDate = DateTime.UtcNow
        });

        Assert.Equal(HttpStatusCode.Conflict, updateResponse.StatusCode);
    }

    [Fact]
    public async Task SubmitThenApprove_AsAdmin_SetsValidUntil()
    {
        await AuthenticateAsAdminAsync();
        var beneficiaryId = await CreateBeneficiaryAsync();
        var eligibility = await CreateEligibilityAsync(beneficiaryId);

        var submitResponse = await _client.PostAsync($"/api/zakat-eligibilities/{eligibility.Id}/submit", null);
        submitResponse.EnsureSuccessStatusCode();

        var validUntil = DateTime.UtcNow.Date.AddMonths(6);
        var decisionResponse = await _client.PostAsJsonAsync($"/api/zakat-eligibilities/{eligibility.Id}/decision", new
        {
            Approve = true,
            DecidedBy = "Admin Reviewer",
            ValidUntil = validUntil
        });

        Assert.Equal(HttpStatusCode.OK, decisionResponse.StatusCode);
        var decision = await decisionResponse.Content.ReadFromJsonAsync<DecisionDto>(JsonOptions);
        Assert.Equal("Approved", decision!.Status);
        Assert.Equal(validUntil, decision.ValidUntil);
    }

    [Fact]
    public async Task Reject_WithoutReason_ReturnsBadRequest()
    {
        await AuthenticateAsAdminAsync();
        var beneficiaryId = await CreateBeneficiaryAsync();
        var eligibility = await CreateEligibilityAsync(beneficiaryId);

        var submitResponse = await _client.PostAsync($"/api/zakat-eligibilities/{eligibility.Id}/submit", null);
        submitResponse.EnsureSuccessStatusCode();

        var decisionResponse = await _client.PostAsJsonAsync($"/api/zakat-eligibilities/{eligibility.Id}/decision", new
        {
            Approve = false
        });

        Assert.Equal(HttpStatusCode.BadRequest, decisionResponse.StatusCode);
    }

    [Fact]
    public async Task Reject_WithReason_Succeeds()
    {
        await AuthenticateAsAdminAsync();
        var beneficiaryId = await CreateBeneficiaryAsync();
        var eligibility = await CreateEligibilityAsync(beneficiaryId);

        var submitResponse = await _client.PostAsync($"/api/zakat-eligibilities/{eligibility.Id}/submit", null);
        submitResponse.EnsureSuccessStatusCode();

        var decisionResponse = await _client.PostAsJsonAsync($"/api/zakat-eligibilities/{eligibility.Id}/decision", new
        {
            Approve = false,
            RejectionReason = "Household income exceeds asnaf threshold."
        });

        Assert.Equal(HttpStatusCode.OK, decisionResponse.StatusCode);
        var decision = await decisionResponse.Content.ReadFromJsonAsync<DecisionDto>(JsonOptions);
        Assert.Equal("Rejected", decision!.Status);
    }

    [Fact]
    public async Task Decision_AsProgramRoleOnly_ReturnsForbidden()
    {
        await AuthenticateAsAdminAsync();
        var beneficiaryId = await CreateBeneficiaryAsync();
        var eligibility = await CreateEligibilityAsync(beneficiaryId);

        var submitResponse = await _client.PostAsync($"/api/zakat-eligibilities/{eligibility.Id}/submit", null);
        submitResponse.EnsureSuccessStatusCode();

        var programEmail = $"program-{Guid.NewGuid():N}@philcare.local";
        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            Email = programEmail,
            Password = "Program@12345",
            Role = "Program"
        });
        registerResponse.EnsureSuccessStatusCode();

        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new { Email = programEmail, Password = "Program@12345" });
        loginResponse.EnsureSuccessStatusCode();
        var loginBody = await loginResponse.Content.ReadFromJsonAsync<LoginResponseDto>(JsonOptions);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginBody!.AccessToken);

        var decisionResponse = await _client.PostAsJsonAsync($"/api/zakat-eligibilities/{eligibility.Id}/decision", new
        {
            Approve = true
        });

        Assert.Equal(HttpStatusCode.Forbidden, decisionResponse.StatusCode);
    }

    [Fact]
    public async Task Submit_SecondCaseForSameBeneficiary_WhileFirstApproved_ReturnsConflict()
    {
        await AuthenticateAsAdminAsync();
        var beneficiaryId = await CreateBeneficiaryAsync();

        var first = await CreateEligibilityAsync(beneficiaryId);
        var firstSubmit = await _client.PostAsync($"/api/zakat-eligibilities/{first.Id}/submit", null);
        firstSubmit.EnsureSuccessStatusCode();
        var firstDecision = await _client.PostAsJsonAsync($"/api/zakat-eligibilities/{first.Id}/decision", new { Approve = true });
        firstDecision.EnsureSuccessStatusCode();

        var second = await CreateEligibilityAsync(beneficiaryId);
        var secondSubmit = await _client.PostAsync($"/api/zakat-eligibilities/{second.Id}/submit", null);

        Assert.Equal(HttpStatusCode.Conflict, secondSubmit.StatusCode);
    }

    [Fact]
    public async Task Approve_AfterPreviousApprovalExpired_Succeeds()
    {
        await AuthenticateAsAdminAsync();
        var beneficiaryId = await CreateBeneficiaryAsync();

        var first = await CreateEligibilityAsync(beneficiaryId);
        var firstSubmit = await _client.PostAsync($"/api/zakat-eligibilities/{first.Id}/submit", null);
        firstSubmit.EnsureSuccessStatusCode();
        var firstDecision = await _client.PostAsJsonAsync($"/api/zakat-eligibilities/{first.Id}/decision", new
        {
            Approve = true,
            ValidUntil = DateTime.UtcNow.Date.AddDays(-1) // already expired
        });
        firstDecision.EnsureSuccessStatusCode();

        var second = await CreateEligibilityAsync(beneficiaryId);
        var secondSubmit = await _client.PostAsync($"/api/zakat-eligibilities/{second.Id}/submit", null);
        secondSubmit.EnsureSuccessStatusCode();

        var secondDecision = await _client.PostAsJsonAsync($"/api/zakat-eligibilities/{second.Id}/decision", new { Approve = true });

        Assert.Equal(HttpStatusCode.OK, secondDecision.StatusCode);
        var decision = await secondDecision.Content.ReadFromJsonAsync<DecisionDto>(JsonOptions);
        Assert.Equal("Approved", decision!.Status);
    }

    private sealed record LoginResponseDto(string AccessToken, string RefreshToken, DateTime RefreshTokenExpiresAt);

    private sealed record DecisionDto(int Id, string Status, DateTime? ValidUntil, string? RejectionReason);
}
