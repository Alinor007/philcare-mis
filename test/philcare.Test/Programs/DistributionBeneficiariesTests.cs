using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using philcare.Api.Features.Finance.Donors.CreateDonor;
using philcare.Api.Features.Programs.Activities.CreateActivity;
using philcare.Api.Features.Programs.AidPrograms.CreateProgram;
using philcare.Api.Features.Programs.Distributions.CreateDistribution;
using philcare.Api.Features.Programs.Beneficiaries.CreateBeneficiary;
using philcare.Api.Features.Programs.Projects.CreateProject;
using philcare.Api.Features.Zakat.CreateZakatEligibility;
using philcare.Test.Common;
using Xunit;

namespace philcare.Test.Programs;

/// <summary>
/// The distribution reach roster. The load-bearing assertion in here is
/// <see cref="RosterMutations_DoNotMoveMoney"/>: a roster row records who received aid, never how
/// much it cost, so no roster operation may touch a funding bucket.
/// </summary>
public class DistributionBeneficiariesTests : IClassFixture<TestWebAppFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly HttpClient _client;

    public DistributionBeneficiariesTests(TestWebAppFactory factory)
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

    private async Task DeactivateBeneficiaryAsync(int beneficiaryId)
    {
        // Consent cannot be withdrawn through the API (UpdateBeneficiaryHandler rejects it the same
        // way registration does), so "inactive" is the only reachable rejected-beneficiary state.
        var response = await _client.PutAsJsonAsync($"/api/beneficiaries/{beneficiaryId}", new
        {
            FullName = $"Deactivated-{Guid.NewGuid():N}",
            BeneficiaryType = "INDIVIDUAL",
            Gender = "Unspecified",
            ConsentOnFile = true,
            Status = "VERIFIED",
            IsActive = false
        });
        response.EnsureSuccessStatusCode();
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
            TotalBudget = 100000m
        });
        projectResponse.EnsureSuccessStatusCode();
        var project = await projectResponse.Content.ReadFromJsonAsync<CreateProjectResponse>(JsonOptions);

        var activityResponse = await _client.PostAsJsonAsync("/api/activities", new
        {
            ProjectId = project!.Id,
            Name = $"Activity-{Guid.NewGuid():N}",
            ActivityType = "RELIEF_DISTRIBUTION",
            Budget = 50000m
        });
        activityResponse.EnsureSuccessStatusCode();
        var activity = await activityResponse.Content.ReadFromJsonAsync<CreateActivityResponse>(JsonOptions);
        return activity!.Id;
    }

    private async Task FundBucketAsync(string fundCode, decimal amount)
    {
        var donorResponse = await _client.PostAsJsonAsync("/api/donors", new
        {
            Name = $"Donor-{Guid.NewGuid():N}",
            Type = "Individual",
            RiskRating = "Low",
            PepFlag = false,
            PrivacyConsent = true
        });
        donorResponse.EnsureSuccessStatusCode();
        var donor = await donorResponse.Content.ReadFromJsonAsync<CreateDonorResponse>(JsonOptions);

        var donationResponse = await _client.PostAsJsonAsync("/api/donations", new
        {
            DonorId = donor!.Id,
            AmountOriginal = amount,
            Currency = "PHP",
            FxRateToPhp = 1m,
            DateReceived = DateTime.UtcNow,
            Channel = "Bank Transfer",
            FundCode = fundCode,
            AdminAllowed = false,
            AdminRateInput = 0m
        });
        donationResponse.EnsureSuccessStatusCode();
    }

    private async Task<decimal> GetBucketRemainingAsync(string bucketCode)
    {
        var response = await _client.GetAsync("/api/funding-buckets");
        response.EnsureSuccessStatusCode();
        var buckets = await response.Content.ReadFromJsonAsync<List<FundingBucketDto>>(JsonOptions);
        return buckets!.Single(b => b.Code == bucketCode).Remaining;
    }

    private async Task ApproveZakatEligibilityAsync(int beneficiaryId, string asnaf)
    {
        var createResponse = await _client.PostAsJsonAsync("/api/zakat-eligibilities", new
        {
            BeneficiaryId = beneficiaryId,
            AsnafCategory = asnaf,
            AssessmentDate = DateTime.UtcNow
        });
        createResponse.EnsureSuccessStatusCode();
        var eligibility = await createResponse.Content.ReadFromJsonAsync<CreateZakatEligibilityResponse>(JsonOptions);

        var submitResponse = await _client.PostAsync($"/api/zakat-eligibilities/{eligibility!.Id}/submit", null);
        submitResponse.EnsureSuccessStatusCode();

        var decisionResponse = await _client.PostAsJsonAsync($"/api/zakat-eligibilities/{eligibility.Id}/decision", new
        {
            Approve = true,
            ValidUntil = DateTime.UtcNow.AddYears(1)
        });
        decisionResponse.EnsureSuccessStatusCode();
    }

    private async Task<CreateDistributionResponse> CreateDistributionAsync(
        int activityId, string bucketCode, string? zakatAsnaf = null)
    {
        var response = await _client.PostAsJsonAsync("/api/distributions", new
        {
            DistributionType = "FOOD_PACK",
            ActivityId = activityId,
            FundingBucketCode = bucketCode,
            Quantity = 2,
            UnitValuePhp = 250m,
            DistributionDate = DateTime.UtcNow,
            FieldVerified = true,
            ReceivedConfirmation = true,
            ZakatAsnaf = zakatAsnaf
        });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CreateDistributionResponse>(JsonOptions))!;
    }

    /// <summary>Reads back the server-maintained "people reached" count for a distribution.</summary>
    private async Task<int> GetReachAsync(int distributionId)
    {
        var response = await _client.GetAsync($"/api/distributions/{distributionId}");
        response.EnsureSuccessStatusCode();
        var detail = await response.Content.ReadFromJsonAsync<DistributionReachDto>(JsonOptions);
        return detail!.BeneficiaryCount;
    }

    private async Task<List<RosterRowDto>> GetRosterAsync(int distributionId, bool includeInactive = false)
    {
        var response = await _client.GetAsync(
            $"/api/distributions/{distributionId}/beneficiaries?includeInactive={includeInactive.ToString().ToLowerInvariant()}");
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<List<RosterRowDto>>(JsonOptions))!;
    }

    private Task<HttpResponseMessage> AddToRosterAsync(
        int distributionId, int beneficiaryId, bool confirmDuplicate = false) =>
        _client.PostAsJsonAsync($"/api/distributions/{distributionId}/beneficiaries", new
        {
            BeneficiaryId = beneficiaryId,
            ReceivedConfirmation = true,
            ConfirmDuplicate = confirmDuplicate
        });

    [Fact]
    public async Task CreateDistribution_StartsWithEmptyRosterAndZeroReach()
    {
        await AuthenticateAsAdminAsync();
        var activityId = await CreateActivityAsync();
        await FundBucketAsync("SADA-FUND", 10000m);

        var distribution = await CreateDistributionAsync(activityId, "SADA-PROG");

        Assert.Equal(0, distribution.BeneficiaryCount);
        Assert.Empty(await GetRosterAsync(distribution.Id));
    }

    [Fact]
    public async Task ReachCount_TracksRosterAddsAndRemoves()
    {
        await AuthenticateAsAdminAsync();
        var activityId = await CreateActivityAsync();
        var firstId = await CreateBeneficiaryAsync();
        var secondId = await CreateBeneficiaryAsync();
        await FundBucketAsync("SADA-FUND", 10000m);

        var distribution = await CreateDistributionAsync(activityId, "SADA-PROG");

        (await AddToRosterAsync(distribution.Id, firstId)).EnsureSuccessStatusCode();
        (await AddToRosterAsync(distribution.Id, secondId)).EnsureSuccessStatusCode();
        Assert.Equal(2, await GetReachAsync(distribution.Id));

        var removeResponse = await _client.DeleteAsync($"/api/distributions/{distribution.Id}/beneficiaries/{secondId}");
        removeResponse.EnsureSuccessStatusCode();
        Assert.Equal(1, await GetReachAsync(distribution.Id));

        // Removing the last recipient is allowed — it just returns the event to zero reach.
        var removeLast = await _client.DeleteAsync($"/api/distributions/{distribution.Id}/beneficiaries/{firstId}");
        removeLast.EnsureSuccessStatusCode();
        Assert.Equal(0, await GetReachAsync(distribution.Id));
    }

    [Fact]
    public async Task AddBeneficiary_AlreadyIssuedSameAidThatDay_WarnsThenAllowsOverride()
    {
        await AuthenticateAsAdminAsync();
        var activityId = await CreateActivityAsync();
        var beneficiaryId = await CreateBeneficiaryAsync();
        await FundBucketAsync("SADA-FUND", 10000m);

        // Two separate distribution records, same activity, same aid type, same day.
        var first = await CreateDistributionAsync(activityId, "SADA-PROG");
        var second = await CreateDistributionAsync(activityId, "SADA-PROG");

        (await AddToRosterAsync(first.Id, beneficiaryId)).EnsureSuccessStatusCode();

        var duplicate = await AddToRosterAsync(second.Id, beneficiaryId);
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        var problem = await duplicate.Content.ReadFromJsonAsync<ProblemDetailsDto>(JsonOptions);
        Assert.Equal("DistributionBeneficiaries.PossibleDuplicate", problem!.Title);

        var confirmed = await AddToRosterAsync(second.Id, beneficiaryId, confirmDuplicate: true);
        Assert.Equal(HttpStatusCode.Created, confirmed.StatusCode);
    }

    [Fact]
    public async Task RosterMutations_DoNotMoveMoney()
    {
        await AuthenticateAsAdminAsync();
        var activityId = await CreateActivityAsync();
        var primaryId = await CreateBeneficiaryAsync();
        var extraId = await CreateBeneficiaryAsync();
        await FundBucketAsync("SADA-FUND", 10000m);

        var distribution = await CreateDistributionAsync(activityId, "SADA-PROG");
        var remainingAfterCreate = await GetBucketRemainingAsync("SADA-PROG");

        (await AddToRosterAsync(distribution.Id, extraId)).EnsureSuccessStatusCode();

        var updateResponse = await _client.PutAsJsonAsync(
            $"/api/distributions/{distribution.Id}/beneficiaries/{extraId}",
            new { ReceivedConfirmation = false, Remarks = "corrected" });
        updateResponse.EnsureSuccessStatusCode();

        var deleteResponse = await _client.DeleteAsync($"/api/distributions/{distribution.Id}/beneficiaries/{extraId}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        (await AddToRosterAsync(distribution.Id, extraId)).EnsureSuccessStatusCode();

        // The whole design rests on this: reach is not money.
        Assert.Equal(remainingAfterCreate, await GetBucketRemainingAsync("SADA-PROG"));
    }

    [Fact]
    public async Task AddBeneficiary_AlreadyOnRoster_ReturnsConflict()
    {
        await AuthenticateAsAdminAsync();
        var activityId = await CreateActivityAsync();
        var primaryId = await CreateBeneficiaryAsync();
        var extraId = await CreateBeneficiaryAsync();
        await FundBucketAsync("SADA-FUND", 10000m);

        var distribution = await CreateDistributionAsync(activityId, "SADA-PROG");
        (await AddToRosterAsync(distribution.Id, extraId)).EnsureSuccessStatusCode();

        var response = await AddToRosterAsync(distribution.Id, extraId);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task AddBeneficiary_Inactive_ReturnsBadRequest()
    {
        await AuthenticateAsAdminAsync();
        var activityId = await CreateActivityAsync();
        var primaryId = await CreateBeneficiaryAsync();
        var inactiveId = await CreateBeneficiaryAsync();
        await FundBucketAsync("SADA-FUND", 10000m);

        var distribution = await CreateDistributionAsync(activityId, "SADA-PROG");
        await DeactivateBeneficiaryAsync(inactiveId);

        var response = await AddToRosterAsync(distribution.Id, inactiveId);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task RemoveThenReAddBeneficiary_ReactivatesRowWithoutDuplicating()
    {
        await AuthenticateAsAdminAsync();
        var activityId = await CreateActivityAsync();
        var primaryId = await CreateBeneficiaryAsync();
        var extraId = await CreateBeneficiaryAsync();
        await FundBucketAsync("SADA-FUND", 10000m);

        var distribution = await CreateDistributionAsync(activityId, "SADA-PROG");
        (await AddToRosterAsync(distribution.Id, extraId)).EnsureSuccessStatusCode();

        await _client.DeleteAsync($"/api/distributions/{distribution.Id}/beneficiaries/{extraId}");
        Assert.DoesNotContain(await GetRosterAsync(distribution.Id), r => r.BeneficiaryId == extraId);

        (await AddToRosterAsync(distribution.Id, extraId)).EnsureSuccessStatusCode();

        var allRows = await GetRosterAsync(distribution.Id, includeInactive: true);
        var rowsForExtra = allRows.Where(r => r.BeneficiaryId == extraId).ToList();

        Assert.Single(rowsForExtra);
        Assert.True(rowsForExtra[0].IsActive);
    }

    [Fact]
    public async Task AddBeneficiary_ToVoidedDistribution_ReturnsConflict()
    {
        await AuthenticateAsAdminAsync();
        var activityId = await CreateActivityAsync();
        var primaryId = await CreateBeneficiaryAsync();
        var extraId = await CreateBeneficiaryAsync();
        await FundBucketAsync("SADA-FUND", 10000m);

        var distribution = await CreateDistributionAsync(activityId, "SADA-PROG");
        (await _client.DeleteAsync($"/api/distributions/{distribution.Id}")).EnsureSuccessStatusCode();

        var response = await AddToRosterAsync(distribution.Id, extraId);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task AddBeneficiary_ToZakatDistribution_WithoutApprovedEligibility_ReturnsBadRequest()
    {
        await AuthenticateAsAdminAsync();
        var activityId = await CreateActivityAsync();
        var primaryId = await CreateBeneficiaryAsync();
        var unassessedId = await CreateBeneficiaryAsync();
        await FundBucketAsync("ZAKA-FUND", 10000m);
        await ApproveZakatEligibilityAsync(primaryId, "FUQARA");

        var distribution = await CreateDistributionAsync(activityId, "ZAK-PROG", "FUQARA");

        var response = await AddToRosterAsync(distribution.Id, unassessedId);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task AddBeneficiary_ToZakatDistribution_WithMismatchedAsnaf_ReturnsBadRequest()
    {
        await AuthenticateAsAdminAsync();
        var activityId = await CreateActivityAsync();
        var primaryId = await CreateBeneficiaryAsync();
        var otherAsnafId = await CreateBeneficiaryAsync();
        await FundBucketAsync("ZAKA-FUND", 10000m);
        await ApproveZakatEligibilityAsync(primaryId, "FUQARA");
        await ApproveZakatEligibilityAsync(otherAsnafId, "MASAKIN");

        var distribution = await CreateDistributionAsync(activityId, "ZAK-PROG", "FUQARA");

        var response = await AddToRosterAsync(distribution.Id, otherAsnafId);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task AddBeneficiary_ToZakatDistribution_WithMatchingAsnaf_Succeeds()
    {
        await AuthenticateAsAdminAsync();
        var activityId = await CreateActivityAsync();
        var primaryId = await CreateBeneficiaryAsync();
        var sameAsnafId = await CreateBeneficiaryAsync();
        await FundBucketAsync("ZAKA-FUND", 10000m);
        await ApproveZakatEligibilityAsync(primaryId, "FUQARA");
        await ApproveZakatEligibilityAsync(sameAsnafId, "FUQARA");

        var distribution = await CreateDistributionAsync(activityId, "ZAK-PROG", "FUQARA");
        (await AddToRosterAsync(distribution.Id, primaryId)).EnsureSuccessStatusCode();
        var remainingBefore = await GetBucketRemainingAsync("ZAK-PROG");

        var response = await AddToRosterAsync(distribution.Id, sameAsnafId);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(2, (await GetRosterAsync(distribution.Id)).Count);
        Assert.Equal(remainingBefore, await GetBucketRemainingAsync("ZAK-PROG"));
    }

    private sealed record LoginResponseDto(string AccessToken, string RefreshToken, DateTime RefreshTokenExpiresAt);

    private sealed record FundingBucketDto(int Id, string Code, string Name, string FundCode, decimal Remaining);

    /// <summary>Partial bind of the distribution detail — only the reach count matters here.</summary>
    private sealed record DistributionReachDto(int Id, int BeneficiaryCount);

    private sealed record ProblemDetailsDto(string Title, string Detail);

    private sealed record RosterRowDto(
        int BeneficiaryId, string BeneficiaryName, string BeneficiaryType,
        bool ReceivedConfirmation, string? EvidenceLink, string? Remarks, bool IsActive);
}
