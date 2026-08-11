using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using philcare.Api.Features.Finance.Donors.CreateDonor;
using philcare.Api.Features.Programs.Activities.CreateActivity;
using philcare.Api.Features.Programs.AidPrograms.CreateProgram;
using philcare.Api.Features.Programs.Distributions.CreateDistribution;
using philcare.Api.Features.Programs.Participants.CreateParticipant;
using philcare.Api.Features.Programs.Projects.CreateProject;
using philcare.Api.Features.Zakat.CreateZakatEligibility;
using philcare.Test.Common;
using Xunit;

namespace philcare.Test.Programs;

public class DistributionsTests : IClassFixture<TestWebAppFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly HttpClient _client;

    public DistributionsTests(TestWebAppFactory factory)
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

    private async Task<int> CreateParticipantAsync()
    {
        var response = await _client.PostAsJsonAsync("/api/participants", new
        {
            FullName = $"Participant-{Guid.NewGuid():N}",
            ParticipantType = "BENEFICIARY",
            BeneficiaryType = "INDIVIDUAL",
            Gender = "Unspecified",
            ConsentOnFile = true
        });
        response.EnsureSuccessStatusCode();
        var participant = await response.Content.ReadFromJsonAsync<CreateParticipantResponse>(JsonOptions);
        return participant!.Id;
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

    /// <summary>Funds a fund's Program bucket via a donation — CreateDistributionHandler now requires
    /// a real, non-empty bucket balance, since recording a distribution posts a linked Expense.</summary>
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

    private async Task ApproveZakatEligibilityAsync(int participantId, string asnaf = "FUQARA", DateTime? validUntil = null)
    {
        var createResponse = await _client.PostAsJsonAsync("/api/zakat-eligibilities", new
        {
            ParticipantId = participantId,
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
            ValidUntil = validUntil
        });
        decisionResponse.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task CreateDistribution_WithFundedBucket_Succeeds()
    {
        await AuthenticateAsAdminAsync();
        var activityId = await CreateActivityAsync();
        var participantId = await CreateParticipantAsync();
        await FundBucketAsync("SADA-FUND", 10000m);

        var response = await _client.PostAsJsonAsync("/api/distributions", new
        {
            DistributionType = "FOOD_PACK",
            ParticipantId = participantId,
            ActivityId = activityId,
            FundingBucketCode = "SADA-PROG",
            Quantity = 2,
            UnitValuePhp = 250m,
            BeneficiaryCount = 1,
            DistributionDate = DateTime.UtcNow,
            FieldVerified = true,
            ReceivedConfirmation = true
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var distribution = await response.Content.ReadFromJsonAsync<CreateDistributionResponse>(JsonOptions);

        Assert.Equal("FOOD_PACK", distribution!.DistributionType);
        Assert.Equal("SADA-PROG", distribution.FundingBucketCode);
        Assert.Equal(500m, distribution.TotalValuePhp);
        Assert.NotNull(distribution.ExpenseId);
        Assert.False(distribution.IsVoided);
    }

    [Fact]
    public async Task CreateDistribution_ZeroUnitValue_SucceedsWithoutPostingExpense()
    {
        await AuthenticateAsAdminAsync();
        var activityId = await CreateActivityAsync();
        var participantId = await CreateParticipantAsync();
        await FundBucketAsync("SADA-FUND", 10000m);

        var response = await _client.PostAsJsonAsync("/api/distributions", new
        {
            DistributionType = "FOOD_PACK",
            ParticipantId = participantId,
            ActivityId = activityId,
            FundingBucketCode = "SADA-PROG",
            Quantity = 5,
            UnitValuePhp = 0m,
            BeneficiaryCount = 1,
            DistributionDate = DateTime.UtcNow,
            FieldVerified = true,
            ReceivedConfirmation = true
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var distribution = await response.Content.ReadFromJsonAsync<CreateDistributionResponse>(JsonOptions);

        Assert.Equal(0m, distribution!.TotalValuePhp);
        Assert.Null(distribution.ExpenseId);
    }

    [Fact]
    public async Task CreateDistribution_DeductsFromBucket_VoidRestoresBalance()
    {
        await AuthenticateAsAdminAsync();
        var activityId = await CreateActivityAsync();
        var participantId = await CreateParticipantAsync();
        await FundBucketAsync("SADA-FUND", 10000m);

        var beforeRemaining = await GetBucketRemainingAsync("SADA-PROG");

        var createResponse = await _client.PostAsJsonAsync("/api/distributions", new
        {
            DistributionType = "FOOD_PACK",
            ParticipantId = participantId,
            ActivityId = activityId,
            FundingBucketCode = "SADA-PROG",
            Quantity = 1,
            UnitValuePhp = 400m,
            BeneficiaryCount = 1,
            DistributionDate = DateTime.UtcNow,
            FieldVerified = true,
            ReceivedConfirmation = true
        });
        createResponse.EnsureSuccessStatusCode();
        var distribution = await createResponse.Content.ReadFromJsonAsync<CreateDistributionResponse>(JsonOptions);

        var afterCreateRemaining = await GetBucketRemainingAsync("SADA-PROG");
        Assert.Equal(beforeRemaining - 400m, afterCreateRemaining);

        var voidResponse = await _client.DeleteAsync($"/api/distributions/{distribution!.Id}");
        Assert.Equal(HttpStatusCode.NoContent, voidResponse.StatusCode);

        var afterVoidRemaining = await GetBucketRemainingAsync("SADA-PROG");
        Assert.Equal(beforeRemaining, afterVoidRemaining);

        // The linked expense must be blocked from a second, direct void.
        var directVoidResponse = await _client.DeleteAsync($"/api/expenses/{distribution.ExpenseId}");
        Assert.Equal(HttpStatusCode.Conflict, directVoidResponse.StatusCode);
    }

    [Fact]
    public async Task CreateDistribution_UnknownParticipant_ReturnsNotFound()
    {
        await AuthenticateAsAdminAsync();
        var activityId = await CreateActivityAsync();

        var response = await _client.PostAsJsonAsync("/api/distributions", new
        {
            DistributionType = "FOOD_PACK",
            ParticipantId = 999999,
            ActivityId = activityId,
            FundingBucketCode = "SADA-PROG",
            Quantity = 1,
            UnitValuePhp = 100m,
            BeneficiaryCount = 1,
            DistributionDate = DateTime.UtcNow,
            FieldVerified = false,
            ReceivedConfirmation = false
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateDistribution_UnknownFundingBucket_ReturnsNotFound()
    {
        await AuthenticateAsAdminAsync();
        var activityId = await CreateActivityAsync();
        var participantId = await CreateParticipantAsync();

        var response = await _client.PostAsJsonAsync("/api/distributions", new
        {
            DistributionType = "CASH_ASSISTANCE",
            ParticipantId = participantId,
            ActivityId = activityId,
            FundingBucketCode = "NOT-A-REAL-BUCKET",
            Quantity = 1,
            UnitValuePhp = 1000m,
            BeneficiaryCount = 1,
            DistributionDate = DateTime.UtcNow,
            FieldVerified = false,
            ReceivedConfirmation = false
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateDistribution_AgainstZakatProgramBucket_WithoutApprovedEligibility_ReturnsBadRequest()
    {
        await AuthenticateAsAdminAsync();
        var activityId = await CreateActivityAsync();
        var participantId = await CreateParticipantAsync();
        await FundBucketAsync("ZAKA-FUND", 100000m);

        var response = await _client.PostAsJsonAsync("/api/distributions", new
        {
            DistributionType = "CASH_ASSISTANCE",
            ParticipantId = participantId,
            ActivityId = activityId,
            FundingBucketCode = "ZAK-PROG", // seeded Finance zakat program bucket
            ZakatAsnaf = "FUQARA",
            Quantity = 1,
            UnitValuePhp = 1000m,
            BeneficiaryCount = 1,
            DistributionDate = DateTime.UtcNow,
            FieldVerified = false,
            ReceivedConfirmation = false
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsDto>(JsonOptions);
        Assert.Equal("Distributions.ZakatEligibilityRequired", problem!.Title);
    }

    [Fact]
    public async Task CreateDistribution_AgainstZakatProgramBucket_WithApprovedEligibilityAndOmittedAsnaf_AutoFillsAsnaf()
    {
        await AuthenticateAsAdminAsync();
        var activityId = await CreateActivityAsync();
        var participantId = await CreateParticipantAsync();
        await ApproveZakatEligibilityAsync(participantId, asnaf: "FUQARA");
        await FundBucketAsync("ZAKA-FUND", 100000m);

        var response = await _client.PostAsJsonAsync("/api/distributions", new
        {
            DistributionType = "CASH_ASSISTANCE",
            ParticipantId = participantId,
            ActivityId = activityId,
            FundingBucketCode = "ZAK-PROG",
            Quantity = 1,
            UnitValuePhp = 1000m,
            BeneficiaryCount = 1,
            DistributionDate = DateTime.UtcNow,
            FieldVerified = true,
            ReceivedConfirmation = true
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var distribution = await response.Content.ReadFromJsonAsync<CreateDistributionResponse>(JsonOptions);
        Assert.Equal("FUQARA", distribution!.ZakatAsnaf);
    }

    [Fact]
    public async Task CreateDistribution_AgainstZakatProgramBucket_WithMismatchedAsnaf_ReturnsBadRequest()
    {
        await AuthenticateAsAdminAsync();
        var activityId = await CreateActivityAsync();
        var participantId = await CreateParticipantAsync();
        await ApproveZakatEligibilityAsync(participantId, asnaf: "FUQARA");
        await FundBucketAsync("ZAKA-FUND", 100000m);

        var response = await _client.PostAsJsonAsync("/api/distributions", new
        {
            DistributionType = "CASH_ASSISTANCE",
            ParticipantId = participantId,
            ActivityId = activityId,
            FundingBucketCode = "ZAK-PROG",
            ZakatAsnaf = "GHARIMIN",
            Quantity = 1,
            UnitValuePhp = 1000m,
            BeneficiaryCount = 1,
            DistributionDate = DateTime.UtcNow,
            FieldVerified = true,
            ReceivedConfirmation = true
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsDto>(JsonOptions);
        Assert.Equal("Distributions.ZakatAsnafMismatch", problem!.Title);
    }

    [Fact]
    public async Task CreateDistribution_AgainstZakatProgramBucket_WithExpiredEligibility_ReturnsBadRequest()
    {
        await AuthenticateAsAdminAsync();
        var activityId = await CreateActivityAsync();
        var participantId = await CreateParticipantAsync();
        await ApproveZakatEligibilityAsync(participantId, asnaf: "FUQARA", validUntil: DateTime.UtcNow.Date.AddDays(-1));
        await FundBucketAsync("ZAKA-FUND", 100000m);

        var response = await _client.PostAsJsonAsync("/api/distributions", new
        {
            DistributionType = "CASH_ASSISTANCE",
            ParticipantId = participantId,
            ActivityId = activityId,
            FundingBucketCode = "ZAK-PROG",
            Quantity = 1,
            UnitValuePhp = 1000m,
            BeneficiaryCount = 1,
            DistributionDate = DateTime.UtcNow,
            FieldVerified = true,
            ReceivedConfirmation = true
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsDto>(JsonOptions);
        Assert.Equal("Distributions.ZakatEligibilityRequired", problem!.Title);
    }

    [Fact]
    public async Task VoidDistribution_ThenVoidAgain_ReturnsConflict()
    {
        await AuthenticateAsAdminAsync();
        var activityId = await CreateActivityAsync();
        var participantId = await CreateParticipantAsync();
        await FundBucketAsync("SADA-FUND", 10000m);

        var createResponse = await _client.PostAsJsonAsync("/api/distributions", new
        {
            DistributionType = "HYGIENE_KIT",
            ParticipantId = participantId,
            ActivityId = activityId,
            FundingBucketCode = "SADA-PROG",
            Quantity = 1,
            UnitValuePhp = 200m,
            BeneficiaryCount = 1,
            DistributionDate = DateTime.UtcNow,
            FieldVerified = false,
            ReceivedConfirmation = false
        });
        createResponse.EnsureSuccessStatusCode();
        var distribution = await createResponse.Content.ReadFromJsonAsync<CreateDistributionResponse>(JsonOptions);

        var firstVoid = await _client.DeleteAsync($"/api/distributions/{distribution!.Id}");
        Assert.Equal(HttpStatusCode.NoContent, firstVoid.StatusCode);

        var secondVoid = await _client.DeleteAsync($"/api/distributions/{distribution.Id}");
        Assert.Equal(HttpStatusCode.Conflict, secondVoid.StatusCode);
    }

    [Fact]
    public async Task GetDistributions_FilteredByParticipant_ReturnsOnlyThatParticipantsDistributions()
    {
        await AuthenticateAsAdminAsync();
        var activityId = await CreateActivityAsync();
        var participantId = await CreateParticipantAsync();
        await FundBucketAsync("SADA-FUND", 10000m);

        var createResponse = await _client.PostAsJsonAsync("/api/distributions", new
        {
            DistributionType = "SCHOOL_SUPPLIES",
            ParticipantId = participantId,
            ActivityId = activityId,
            FundingBucketCode = "SADA-PROG",
            Quantity = 1,
            UnitValuePhp = 300m,
            BeneficiaryCount = 1,
            DistributionDate = DateTime.UtcNow,
            FieldVerified = false,
            ReceivedConfirmation = false
        });
        createResponse.EnsureSuccessStatusCode();

        var response = await _client.GetAsync($"/api/distributions?participantId={participantId}");
        response.EnsureSuccessStatusCode();
        var distributions = await response.Content.ReadFromJsonAsync<List<DistributionListItemDto>>(JsonOptions);

        Assert.Single(distributions!);
        Assert.Equal(participantId, distributions![0].ParticipantId);
    }

    private sealed record LoginResponseDto(string AccessToken, string RefreshToken, DateTime RefreshTokenExpiresAt);

    private sealed record DistributionListItemDto(int Id, string DistributionType, int ParticipantId, string ParticipantName, decimal TotalValuePhp, DateTime DistributionDate, bool IsVoided);

    private sealed record FundingBucketDto(int Id, string Code, string Name, string FundCode, string BucketType, decimal MaxAdminRate, decimal AllocatedAmount, decimal ExpensedAmount, decimal Remaining);

    private sealed record ProblemDetailsDto(string Title, string Detail);
}
