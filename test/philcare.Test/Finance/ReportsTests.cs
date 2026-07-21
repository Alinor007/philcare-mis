using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using philcare.Api.Features.Finance.Donations.CreateDonation;
using philcare.Api.Features.Finance.Donors.CreateDonor;
using philcare.Test.Common;
using Xunit;

namespace philcare.Test.Finance;

public class ReportsTests : IClassFixture<TestWebAppFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly HttpClient _client;

    public ReportsTests(TestWebAppFactory factory)
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

    [Fact]
    public async Task GetFundSummary_ReturnsRowForEverySeededBucket()
    {
        await AuthenticateAsAdminAsync();

        var response = await _client.GetAsync("/api/reports/fund-summary");
        response.EnsureSuccessStatusCode();
        var summary = await response.Content.ReadFromJsonAsync<FundSummaryDto>(JsonOptions);

        Assert.Equal(10, summary!.Buckets.Count);
        Assert.Equal(summary.Buckets.Sum(b => b.AllocatedAmount), summary.GrandTotalAllocated);
        Assert.Equal(summary.Buckets.Sum(b => b.ExpensedAmount), summary.GrandTotalExpensed);
    }

    [Fact]
    public async Task GetZakatAsnafReport_AggregatesByAsnaf()
    {
        await AuthenticateAsAdminAsync();
        var donorId = await CreateDonorAsync($"Donor-{Guid.NewGuid():N}");

        // Fund the shared ZAK-PROG bucket generously so this test's expenses never
        // trip the balance check, regardless of other tests' cumulative state.
        var donationResponse = await _client.PostAsJsonAsync("/api/donations", new
        {
            DonorId = donorId,
            AmountOriginal = 200_000m,
            Currency = "PHP",
            FxRateToPhp = 1m,
            DateReceived = DateTime.UtcNow,
            Channel = "Bank Transfer",
            FundCode = "ZAKA-FUND",
            AdminAllowed = false,
            AdminRateInput = 0m
        });
        donationResponse.EnsureSuccessStatusCode();

        var uniqueAsnaf = $"ASNAF-{Guid.NewGuid():N}"[..20];
        var expenseResponse = await _client.PostAsJsonAsync("/api/expenses", new
        {
            ExpenseDate = DateTime.UtcNow,
            PayeeVendor = "Test Vendor",
            ExpenseCategory = "RELIEF",
            Description = "Zakat asnaf report test",
            PaymentMethod = "CASH",
            AmountOriginal = 3000m,
            Currency = "PHP",
            FxRateToPhp = 1m,
            FundingBucketCode = "ZAK-PROG",
            ZakatAsnaf = uniqueAsnaf,
            BeneficiaryCount = 12
        });
        expenseResponse.EnsureSuccessStatusCode();

        var reportResponse = await _client.GetAsync("/api/reports/zakat-asnaf");
        reportResponse.EnsureSuccessStatusCode();
        var rows = await reportResponse.Content.ReadFromJsonAsync<List<ZakatAsnafRowDto>>(JsonOptions);

        var row = rows!.Single(r => r.ZakatAsnaf == uniqueAsnaf);
        Assert.Equal(3000m, row.TotalAmountPhp);
        Assert.Equal(12, row.TotalBeneficiaries);
        Assert.Equal(1, row.ExpenseCount);
    }

    [Fact]
    public async Task GetDonorUtilization_UnknownDonor_ReturnsNotFound()
    {
        await AuthenticateAsAdminAsync();

        var response = await _client.GetAsync("/api/reports/donor-utilization/999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetDonorUtilization_AggregatesDonationsByFund()
    {
        await AuthenticateAsAdminAsync();
        var donorId = await CreateDonorAsync($"Donor-{Guid.NewGuid():N}");

        await CreateDonationForUtilizationAsync(donorId, "GENE-FUND", 400m);
        await CreateDonationForUtilizationAsync(donorId, "GENE-FUND", 600m);
        await CreateDonationForUtilizationAsync(donorId, "SADA-FUND", 250m);

        var response = await _client.GetAsync($"/api/reports/donor-utilization/{donorId}");
        response.EnsureSuccessStatusCode();
        var utilization = await response.Content.ReadFromJsonAsync<DonorUtilizationDto>(JsonOptions);

        var geneRow = utilization!.Funds.Single(f => f.FundCode == "GENE-FUND");
        Assert.Equal(2, geneRow.DonationCount);
        Assert.Equal(1000m, geneRow.TotalAmountPhp);

        var sadaRow = utilization.Funds.Single(f => f.FundCode == "SADA-FUND");
        Assert.Equal(1, sadaRow.DonationCount);
        Assert.Equal(250m, sadaRow.TotalAmountPhp);
    }

    private async Task CreateDonationForUtilizationAsync(int donorId, string fundCode, decimal amount)
    {
        var response = await _client.PostAsJsonAsync("/api/donations", new
        {
            DonorId = donorId,
            AmountOriginal = amount,
            Currency = "PHP",
            FxRateToPhp = 1m,
            DateReceived = DateTime.UtcNow,
            Channel = "Bank Transfer",
            FundCode = fundCode,
            AdminAllowed = false,
            AdminRateInput = 0m
        });

        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task GetZakatAmilReport_ReflectsMaxAmilCapOf12Point5Percent()
    {
        await AuthenticateAsAdminAsync();

        var response = await _client.GetAsync("/api/reports/zakat-amil");
        response.EnsureSuccessStatusCode();
        var report = await response.Content.ReadFromJsonAsync<ZakatAmilReportDto>(JsonOptions);

        var expectedMaxAmil = Math.Round(report!.TotalZakatCollectedPhp * 0.125m, 2);
        Assert.Equal(expectedMaxAmil, report.MaxAmilSharePhp);
        Assert.True(report.AmilAllocatedPhp <= report.MaxAmilSharePhp);
    }

    [Fact]
    public async Task GetRestrictedFundLedger_RestFund_IncludesSeededOpeningBalance()
    {
        await AuthenticateAsAdminAsync();

        var response = await _client.GetAsync("/api/reports/restricted-ledger?fundCode=REST-FUND&year=2026");
        response.EnsureSuccessStatusCode();
        var ledgers = await response.Content.ReadFromJsonAsync<List<RestrictedFundLedgerDto>>(JsonOptions);

        var restFund = ledgers!.Single(l => l.FundCode == "REST-FUND");
        Assert.Equal(139930.15m, restFund.OpeningBalancePhp);
    }

    [Fact]
    public async Task GetRestrictedFundLedger_OnlyReturnsRestrictedFunds()
    {
        await AuthenticateAsAdminAsync();

        var response = await _client.GetAsync("/api/reports/restricted-ledger?year=2026");
        response.EnsureSuccessStatusCode();
        var ledgers = await response.Content.ReadFromJsonAsync<List<RestrictedFundLedgerDto>>(JsonOptions);

        var codes = ledgers!.Select(l => l.FundCode).ToList();
        // Restricted per seed data: ZAKA-FUND, REST-FUND, CAPX-FUND. Unrestricted: SADA/GENE/OPER-FUND.
        Assert.Contains("ZAKA-FUND", codes);
        Assert.Contains("REST-FUND", codes);
        Assert.Contains("CAPX-FUND", codes);
        Assert.DoesNotContain("GENE-FUND", codes);
        Assert.DoesNotContain("OPER-FUND", codes);
    }

    private sealed record LoginResponseDto(string AccessToken, string RefreshToken, DateTime RefreshTokenExpiresAt);

    private sealed record FundSummaryBucketRowDto(string FundCode, string BucketCode, string BucketName, decimal AllocatedAmount, decimal ExpensedAmount, decimal Remaining);

    private sealed record FundSummaryDto(List<FundSummaryBucketRowDto> Buckets, decimal GrandTotalAllocated, decimal GrandTotalExpensed, decimal GrandTotalRemaining, decimal OverallAdminRatio);

    private sealed record ZakatAsnafRowDto(string ZakatAsnaf, decimal TotalAmountPhp, int TotalBeneficiaries, int ExpenseCount);

    private sealed record DonorUtilizationFundRowDto(string FundCode, int DonationCount, decimal TotalAmountPhp, decimal ProgramAmountPhp, decimal AdminAmountPhp);

    private sealed record DonorUtilizationDto(int DonorId, string DonorName, List<DonorUtilizationFundRowDto> Funds);

    private sealed record ZakatAmilReportDto(decimal TotalZakatCollectedPhp, decimal MaxAmilSharePhp, decimal AmilAllocatedPhp, decimal AmilExpensedPhp, decimal AmilRemainingPhp);

    private sealed record RestrictedLedgerEntryDto(DateTime Date, string TransactionType, string Reference, decimal AmountIn, decimal AmountOut, decimal RunningBalance, string? Notes);

    private sealed record RestrictedFundLedgerDto(string FundCode, string FundName, decimal OpeningBalancePhp, List<RestrictedLedgerEntryDto> Entries, decimal ClosingBalancePhp);
}
