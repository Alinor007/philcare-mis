using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using philcare.Api.Features.Finance.Donors.CreateDonor;
using philcare.Api.Features.Governance.People.CreatePerson;
using philcare.Api.Features.HumanResources.Staff.CreateStaffMember;
using philcare.Api.Features.Programs.Activities.CreateActivity;
using philcare.Api.Features.Programs.AidPrograms.CreateProgram;
using philcare.Api.Features.Programs.Distributions.CreateDistribution;
using philcare.Api.Features.Programs.Beneficiaries.CreateBeneficiary;
using philcare.Api.Features.Programs.Projects.CreateProject;
using philcare.Test.Common;
using Xunit;

namespace philcare.Test.Programs;

public class ProgramReportsTests : IClassFixture<TestWebAppFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly HttpClient _client;

    public ProgramReportsTests(TestWebAppFactory factory)
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
        await AuthenticateAsAdminAsync(); // registering a role-specific user requires an Admin bearer token

        var email = $"viewer-{Guid.NewGuid():N}@philcare.local";
        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", new { Email = email, Password = "Viewer@12345", Role = "Viewer" });
        registerResponse.EnsureSuccessStatusCode();

        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new { Email = email, Password = "Viewer@12345" });
        loginResponse.EnsureSuccessStatusCode();
        var body = await loginResponse.Content.ReadFromJsonAsync<LoginResponseDto>(JsonOptions);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body!.AccessToken);
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

    [Fact]
    public async Task GetProgramSummary_AggregatesProjectsActivitiesAndDistributions()
    {
        await AuthenticateAsAdminAsync();
        await FundBucketAsync("SADA-FUND", 10000m);

        var programResponse = await _client.PostAsJsonAsync("/api/programs", new { Name = $"Program-{Guid.NewGuid():N}", Category = "RELIEF" });
        programResponse.EnsureSuccessStatusCode();
        var program = await programResponse.Content.ReadFromJsonAsync<CreateProgramResponse>(JsonOptions);

        var projectResponse = await _client.PostAsJsonAsync("/api/projects", new
        {
            ProgramId = program!.Id,
            Name = $"Project-{Guid.NewGuid():N}",
            TotalBudget = 50000m
        });
        projectResponse.EnsureSuccessStatusCode();
        var project = await projectResponse.Content.ReadFromJsonAsync<CreateProjectResponse>(JsonOptions);

        var activityResponse = await _client.PostAsJsonAsync("/api/activities", new
        {
            ProjectId = project!.Id,
            Name = $"Activity-{Guid.NewGuid():N}",
            ActivityType = "RELIEF_DISTRIBUTION",
            Budget = 20000m
        });
        activityResponse.EnsureSuccessStatusCode();
        var activity = await activityResponse.Content.ReadFromJsonAsync<CreateActivityResponse>(JsonOptions);

        var beneficiaryResponse = await _client.PostAsJsonAsync("/api/beneficiaries", new
        {
            FullName = $"Beneficiary-{Guid.NewGuid():N}",
            BeneficiaryType = "INDIVIDUAL",
            Gender = "Unspecified",
            ConsentOnFile = true
        });
        beneficiaryResponse.EnsureSuccessStatusCode();
        var beneficiary = await beneficiaryResponse.Content.ReadFromJsonAsync<CreateBeneficiaryResponse>(JsonOptions);

        var distributionResponse = await _client.PostAsJsonAsync("/api/distributions", new
        {
            DistributionType = "FOOD_PACK",
            ActivityId = activity!.Id,
            FundingBucketCode = "SADA-PROG",
            Quantity = 1,
            UnitValuePhp = 750m,
            DistributionDate = DateTime.UtcNow,
            FieldVerified = true,
            ReceivedConfirmation = true
        });
        distributionResponse.EnsureSuccessStatusCode();

        var reportResponse = await _client.GetAsync("/api/reports/program-summary");
        reportResponse.EnsureSuccessStatusCode();
        var rows = await reportResponse.Content.ReadFromJsonAsync<List<ProgramSummaryRowDto>>(JsonOptions);

        var row = rows!.Single(r => r.ProgramId == program.Id);
        Assert.Equal(1, row.ProjectCount);
        Assert.Equal(1, row.ActivityCount);
        Assert.Equal(50000m, row.TotalProjectBudget);
        Assert.Equal(20000m, row.TotalActivityBudget);
        Assert.Equal(750m, row.TotalDistributedValuePhp);
    }

    [Fact]
    public async Task GetDistributionSummary_AggregatesByTypeAndCountsDistinctBeneficiaries()
    {
        await AuthenticateAsAdminAsync();
        await FundBucketAsync("SADA-FUND", 10000m);
        var uniqueType = $"TYPE-{Guid.NewGuid():N}"[..20];

        var programResponse = await _client.PostAsJsonAsync("/api/programs", new { Name = $"Program-{Guid.NewGuid():N}", Category = "RELIEF" });
        programResponse.EnsureSuccessStatusCode();
        var program = await programResponse.Content.ReadFromJsonAsync<CreateProgramResponse>(JsonOptions);

        var projectResponse = await _client.PostAsJsonAsync("/api/projects", new
        {
            ProgramId = program!.Id,
            Name = $"Project-{Guid.NewGuid():N}",
            TotalBudget = 5000m
        });
        projectResponse.EnsureSuccessStatusCode();
        var project = await projectResponse.Content.ReadFromJsonAsync<CreateProjectResponse>(JsonOptions);

        var activityResponse = await _client.PostAsJsonAsync("/api/activities", new
        {
            ProjectId = project!.Id,
            Name = $"Activity-{Guid.NewGuid():N}",
            ActivityType = "RELIEF_DISTRIBUTION",
            Budget = 2000m
        });
        activityResponse.EnsureSuccessStatusCode();
        var activity = await activityResponse.Content.ReadFromJsonAsync<CreateActivityResponse>(JsonOptions);

        var beneficiary1Response = await _client.PostAsJsonAsync("/api/beneficiaries", new
        {
            FullName = $"Beneficiary-{Guid.NewGuid():N}",
            BeneficiaryType = "INDIVIDUAL",
            Gender = "Unspecified",
            ConsentOnFile = true
        });
        beneficiary1Response.EnsureSuccessStatusCode();
        var beneficiary1 = await beneficiary1Response.Content.ReadFromJsonAsync<CreateBeneficiaryResponse>(JsonOptions);

        var beneficiary2Response = await _client.PostAsJsonAsync("/api/beneficiaries", new
        {
            FullName = $"Beneficiary-{Guid.NewGuid():N}",
            BeneficiaryType = "INDIVIDUAL",
            Gender = "Unspecified",
            ConsentOnFile = true
        });
        beneficiary2Response.EnsureSuccessStatusCode();
        var beneficiary2 = await beneficiary2Response.Content.ReadFromJsonAsync<CreateBeneficiaryResponse>(JsonOptions);

        // Unit values, not totals — TotalValuePhp is now server-computed as Quantity x UnitValuePhp,
        // so (unitValue, qty) = (50, 2) and (50, 3) reproduce the original totals (100, 150) => 250
        // and the original combined quantity (5) unchanged.
        foreach (var (beneficiaryId, unitValue, qty) in new[] { (beneficiary1!.Id, 50m, 2), (beneficiary2!.Id, 50m, 3) })
        {
            var response = await _client.PostAsJsonAsync("/api/distributions", new
            {
                DistributionType = uniqueType,
                ActivityId = activity!.Id,
                FundingBucketCode = "SADA-PROG",
                Quantity = qty,
                UnitValuePhp = unitValue,
                DistributionDate = DateTime.UtcNow,
                FieldVerified = false,
                ReceivedConfirmation = false
            });
            response.EnsureSuccessStatusCode();
            var created = await response.Content.ReadFromJsonAsync<CreateDistributionResponse>(JsonOptions);

            // Reach is the roster now — a distribution reaches nobody until someone is added to it,
            // which is what DistinctBeneficiaries below counts.
            var addResponse = await _client.PostAsJsonAsync($"/api/distributions/{created!.Id}/beneficiaries", new
            {
                BeneficiaryId = beneficiaryId,
                ReceivedConfirmation = true
            });
            addResponse.EnsureSuccessStatusCode();
        }

        var reportResponse = await _client.GetAsync("/api/reports/distribution-summary");
        reportResponse.EnsureSuccessStatusCode();
        var rows = await reportResponse.Content.ReadFromJsonAsync<List<DistributionSummaryRowDto>>(JsonOptions);

        var row = rows!.Single(r => r.DistributionType == uniqueType);
        Assert.Equal(2, row.DistributionCount);
        Assert.Equal(2, row.DistinctBeneficiaries);
        Assert.Equal(5, row.TotalQuantity);
        Assert.Equal(250m, row.TotalValuePhp);
    }

    [Fact]
    public async Task GetActivityReport_ReturnsRosterAndDistributionTotals()
    {
        await AuthenticateAsAdminAsync();
        await FundBucketAsync("SADA-FUND", 10000m);

        var programResponse = await _client.PostAsJsonAsync("/api/programs", new { Name = $"Program-{Guid.NewGuid():N}", Category = "RELIEF" });
        programResponse.EnsureSuccessStatusCode();
        var program = await programResponse.Content.ReadFromJsonAsync<CreateProgramResponse>(JsonOptions);

        var projectResponse = await _client.PostAsJsonAsync("/api/projects", new
        {
            ProgramId = program!.Id,
            Name = $"Project-{Guid.NewGuid():N}",
            TotalBudget = 5000m
        });
        projectResponse.EnsureSuccessStatusCode();
        var project = await projectResponse.Content.ReadFromJsonAsync<CreateProjectResponse>(JsonOptions);

        var activityResponse = await _client.PostAsJsonAsync("/api/activities", new
        {
            ProjectId = project!.Id,
            Name = $"Activity-{Guid.NewGuid():N}",
            ActivityType = "RELIEF_DISTRIBUTION",
            Budget = 2000m
        });
        activityResponse.EnsureSuccessStatusCode();
        var activity = await activityResponse.Content.ReadFromJsonAsync<CreateActivityResponse>(JsonOptions);

        var beneficiaryResponse = await _client.PostAsJsonAsync("/api/beneficiaries", new
        {
            FullName = $"Beneficiary-{Guid.NewGuid():N}",
            BeneficiaryType = "INDIVIDUAL",
            Gender = "Unspecified",
            ConsentOnFile = true
        });
        beneficiaryResponse.EnsureSuccessStatusCode();
        var beneficiary = await beneficiaryResponse.Content.ReadFromJsonAsync<CreateBeneficiaryResponse>(JsonOptions);

        // Staff identity lives on Person now, so a staff fixture needs a Person first.
        var staffPersonResponse = await _client.PostAsJsonAsync("/api/governance/people", new
        {
            FullName = $"Staff-{Guid.NewGuid():N}",
            PersonCategory = "MEMBER",
            DefaultVotingRights = false
        });
        staffPersonResponse.EnsureSuccessStatusCode();
        var staffPerson = await staffPersonResponse.Content.ReadFromJsonAsync<CreatePersonResponse>(JsonOptions);

        var staffResponse = await _client.PostAsJsonAsync("/api/staff", new
        {
            PersonId = staffPerson!.Id,
            Position = "Field Officer",
            EmploymentType = "FULL_TIME"
        });
        staffResponse.EnsureSuccessStatusCode();
        var staff = await staffResponse.Content.ReadFromJsonAsync<CreateStaffMemberResponse>(JsonOptions);

        // BeneficiaryCount/PresentCount on this report count the activity's STAFF roster — the
        // roster pivoted away from beneficiaries; beneficiary reach is the distribution side below.
        var enrollResponse = await _client.PostAsJsonAsync($"/api/activities/{activity.Id}/participants", new
        {
            StaffMemberId = staff!.Id,
            AttendanceStatus = "PRESENT",
            ConsentRequired = false
        });
        enrollResponse.EnsureSuccessStatusCode();

        var distributionResponse = await _client.PostAsJsonAsync("/api/distributions", new
        {
            DistributionType = "FOOD_PACK",
            ActivityId = activity.Id,
            FundingBucketCode = "SADA-PROG",
            Quantity = 1,
            UnitValuePhp = 300m,
            DistributionDate = DateTime.UtcNow,
            FieldVerified = true,
            ReceivedConfirmation = true
        });
        distributionResponse.EnsureSuccessStatusCode();

        var reportResponse = await _client.GetAsync($"/api/reports/activity-report?projectId={project.Id}");
        reportResponse.EnsureSuccessStatusCode();
        var rows = await reportResponse.Content.ReadFromJsonAsync<List<ActivityReportRowDto>>(JsonOptions);

        var row = rows!.Single(r => r.ActivityId == activity.Id);
        Assert.Equal(1, row.BeneficiaryCount);
        Assert.Equal(1, row.PresentCount);
        Assert.Equal(1, row.DistributionCount);
        Assert.Equal(300m, row.TotalDistributedValuePhp);
    }

    [Fact]
    public async Task GetBeneficiaryMasterList_FilteredByVulnerabilityCategory_ReturnsOnlyMatching()
    {
        await AuthenticateAsAdminAsync();
        var uniqueCategory = "WIDOW";

        var beneficiaryResponse = await _client.PostAsJsonAsync("/api/beneficiaries", new
        {
            FullName = $"Beneficiary-{Guid.NewGuid():N}",
            BeneficiaryType = "INDIVIDUAL",
            Gender = "Female",
            VulnerabilityCategory = uniqueCategory,
            ConsentOnFile = true
        });
        beneficiaryResponse.EnsureSuccessStatusCode();
        var beneficiary = await beneficiaryResponse.Content.ReadFromJsonAsync<CreateBeneficiaryResponse>(JsonOptions);

        var reportResponse = await _client.GetAsync($"/api/reports/beneficiary-master-list?vulnerabilityCategory={uniqueCategory}");
        reportResponse.EnsureSuccessStatusCode();
        var rows = await reportResponse.Content.ReadFromJsonAsync<List<BeneficiaryMasterListRowDto>>(JsonOptions);

        Assert.Contains(rows!, r => r.BeneficiaryId == beneficiary!.Id);
        Assert.All(rows!, r => Assert.Equal(uniqueCategory, r.VulnerabilityCategory));
    }

    [Fact]
    public async Task GetBeneficiaryMasterList_AsViewer_ReturnsForbidden()
    {
        await AuthenticateAsViewerAsync();

        var response = await _client.GetAsync("/api/reports/beneficiary-master-list");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private sealed record LoginResponseDto(string AccessToken, string RefreshToken, DateTime RefreshTokenExpiresAt);

    private sealed record ActivityReportRowDto(
        int ActivityId, string ActivityName, int ProjectId, string ProjectName, string ActivityType,
        string ImplementationStatus, int? ActualBeneficiaries, DateTime? ActualEndDate,
        int BeneficiaryCount, int PresentCount, int DistributionCount, decimal TotalDistributedValuePhp);

    private sealed record BeneficiaryMasterListRowDto(
        int BeneficiaryId, string FullName, string BeneficiaryType,
        string? VulnerabilityCategory, string? SafeguardingCategory, string Status, bool ConsentOnFile,
        int ActivityCount, int DistributionCount, decimal TotalReceivedValuePhp);

    private sealed record ProgramSummaryRowDto(
        int ProgramId, string ProgramName, int ProjectCount, int ActivityCount,
        decimal TotalProjectBudget, decimal TotalActivityBudget, decimal TotalDistributedValuePhp);

    private sealed record DistributionSummaryRowDto(string DistributionType, int DistributionCount, int DistinctBeneficiaries, int TotalQuantity, decimal TotalValuePhp);
}
