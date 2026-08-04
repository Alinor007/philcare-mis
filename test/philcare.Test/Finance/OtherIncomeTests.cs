using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using philcare.Api.Features.Finance.OtherIncomes.CreateOtherIncome;
using philcare.Test.Common;
using Xunit;

namespace philcare.Test.Finance;

public class OtherIncomeTests : IClassFixture<TestWebAppFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly HttpClient _client;

    public OtherIncomeTests(TestWebAppFactory factory)
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

    private async Task<CreateOtherIncomeResponse> CreateOtherIncomeAsync(
        string incomeType = "CAPITAL_INCOME", string fundingBucketCode = "CAPX-FUND", decimal amountOriginal = 1000m,
        string currency = "PHP", decimal fxRateToPhp = 1m)
    {
        var response = await _client.PostAsJsonAsync("/api/other-income", new
        {
            IncomeType = incomeType,
            Source = $"Source-{Guid.NewGuid():N}",
            DateReceived = DateTime.UtcNow,
            AmountOriginal = amountOriginal,
            Currency = currency,
            FxRateToPhp = fxRateToPhp,
            FundingBucketCode = fundingBucketCode
        });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CreateOtherIncomeResponse>(JsonOptions))!;
    }

    [Fact]
    public async Task CreateOtherIncome_WithoutAuthentication_ReturnsUnauthorized()
    {
        var response = await _client.PostAsJsonAsync("/api/other-income", new
        {
            IncomeType = "CAPITAL_INCOME",
            Source = "Should not be created",
            DateReceived = DateTime.UtcNow,
            AmountOriginal = 100m,
            Currency = "PHP",
            FxRateToPhp = 1m,
            FundingBucketCode = "CAPX-FUND"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateOtherIncome_CapitalIncome_IncrementsBucketAllocatedAmount()
    {
        await AuthenticateAsAdminAsync();

        var summaryBefore = await _client.GetFromJsonAsync<FundSummaryDto>("/api/reports/fund-summary", JsonOptions);
        var capxBefore = summaryBefore!.Buckets.First(b => b.BucketCode == "CAPX-FUND").AllocatedAmount;

        var income = await CreateOtherIncomeAsync(amountOriginal: 50000m);

        Assert.Equal(50000m, income.AmountPhp);
        Assert.Equal("CAPX-FUND", income.FundingBucketCode);

        var summaryAfter = await _client.GetFromJsonAsync<FundSummaryDto>("/api/reports/fund-summary", JsonOptions);
        var capxAfter = summaryAfter!.Buckets.First(b => b.BucketCode == "CAPX-FUND").AllocatedAmount;

        Assert.Equal(capxBefore + 50000m, capxAfter);
    }

    [Fact]
    public async Task CreateOtherIncome_ForeignCurrency_ConvertsToPhp()
    {
        await AuthenticateAsAdminAsync();

        var income = await CreateOtherIncomeAsync(amountOriginal: 100m, currency: "USD", fxRateToPhp: 56.5m);

        Assert.Equal(5650m, income.AmountPhp);
    }

    [Fact]
    public async Task CreateOtherIncome_IntoZakatProgramBucket_ReturnsBadRequest()
    {
        await AuthenticateAsAdminAsync();

        var response = await _client.PostAsJsonAsync("/api/other-income", new
        {
            IncomeType = "GRANT",
            Source = "Should be rejected",
            DateReceived = DateTime.UtcNow,
            AmountOriginal = 10000m,
            Currency = "PHP",
            FxRateToPhp = 1m,
            FundingBucketCode = "ZAK-PROG"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateOtherIncome_IntoZakatAmilBucket_ReturnsBadRequest()
    {
        await AuthenticateAsAdminAsync();

        var response = await _client.PostAsJsonAsync("/api/other-income", new
        {
            IncomeType = "GRANT",
            Source = "Should be rejected",
            DateReceived = DateTime.UtcNow,
            AmountOriginal = 10000m,
            Currency = "PHP",
            FxRateToPhp = 1m,
            FundingBucketCode = "ZAK-AMIL"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateOtherIncome_UnknownBucket_ReturnsNotFound()
    {
        await AuthenticateAsAdminAsync();

        var response = await _client.PostAsJsonAsync("/api/other-income", new
        {
            IncomeType = "CAPITAL_INCOME",
            Source = "Unknown bucket",
            DateReceived = DateTime.UtcNow,
            AmountOriginal = 100m,
            Currency = "PHP",
            FxRateToPhp = 1m,
            FundingBucketCode = "NOT-A-REAL-BUCKET"
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateOtherIncome_UnknownIncomeType_ReturnsBadRequest()
    {
        await AuthenticateAsAdminAsync();

        var response = await _client.PostAsJsonAsync("/api/other-income", new
        {
            IncomeType = "NOT_A_REAL_TYPE",
            Source = "Unknown type",
            DateReceived = DateTime.UtcNow,
            AmountOriginal = 100m,
            Currency = "PHP",
            FxRateToPhp = 1m,
            FundingBucketCode = "OPER-POOL"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task VoidOtherIncome_RestoresBucketBalance()
    {
        await AuthenticateAsAdminAsync();

        var income = await CreateOtherIncomeAsync(fundingBucketCode: "OPER-POOL", amountOriginal: 2000m);

        var summaryBefore = await _client.GetFromJsonAsync<FundSummaryDto>("/api/reports/fund-summary", JsonOptions);
        var operBefore = summaryBefore!.Buckets.First(b => b.BucketCode == "OPER-POOL").AllocatedAmount;

        var voidResponse = await _client.DeleteAsync($"/api/other-income/{income.Id}");
        Assert.Equal(HttpStatusCode.NoContent, voidResponse.StatusCode);

        var summaryAfter = await _client.GetFromJsonAsync<FundSummaryDto>("/api/reports/fund-summary", JsonOptions);
        var operAfter = summaryAfter!.Buckets.First(b => b.BucketCode == "OPER-POOL").AllocatedAmount;

        Assert.Equal(operBefore - 2000m, operAfter);

        var detailResponse = await _client.GetAsync($"/api/other-income/{income.Id}");
        detailResponse.EnsureSuccessStatusCode();
        var detail = await detailResponse.Content.ReadFromJsonAsync<OtherIncomeDetailDto>(JsonOptions);
        Assert.True(detail!.IsVoided);
    }

    [Fact]
    public async Task VoidOtherIncome_AlreadyVoided_ReturnsConflict()
    {
        await AuthenticateAsAdminAsync();
        var income = await CreateOtherIncomeAsync(fundingBucketCode: "OPER-POOL", amountOriginal: 500m);

        var firstVoid = await _client.DeleteAsync($"/api/other-income/{income.Id}");
        Assert.Equal(HttpStatusCode.NoContent, firstVoid.StatusCode);

        var secondVoid = await _client.DeleteAsync($"/api/other-income/{income.Id}");
        Assert.Equal(HttpStatusCode.Conflict, secondVoid.StatusCode);
    }

    [Fact]
    public async Task VoidOtherIncome_WhoseFundsAreAlreadySpent_ReturnsBadRequest()
    {
        await AuthenticateAsAdminAsync();

        var income = await CreateOtherIncomeAsync(fundingBucketCode: "REST-PROG", amountOriginal: 3000m);

        var expenseResponse = await _client.PostAsJsonAsync("/api/expenses", new
        {
            ExpenseDate = DateTime.UtcNow,
            PayeeVendor = "Test Vendor",
            ExpenseCategory = "PROGRAM",
            Description = "Spends the income",
            PaymentMethod = "CASH",
            AmountOriginal = 3000m,
            Currency = "PHP",
            FxRateToPhp = 1m,
            FundingBucketCode = "REST-PROG"
        });
        expenseResponse.EnsureSuccessStatusCode();

        var voidResponse = await _client.DeleteAsync($"/api/other-income/{income.Id}");

        Assert.Equal(HttpStatusCode.BadRequest, voidResponse.StatusCode);
    }

    [Fact]
    public async Task GetOtherIncomes_FilteredByIncomeType_ReturnsOnlyMatching()
    {
        await AuthenticateAsAdminAsync();
        await CreateOtherIncomeAsync(incomeType: "EARNED_INCOME", fundingBucketCode: "OPER-POOL", amountOriginal: 100m);
        await CreateOtherIncomeAsync(incomeType: "CAPITAL_INCOME", fundingBucketCode: "CAPX-FUND", amountOriginal: 100m);

        var response = await _client.GetAsync("/api/other-income?incomeType=EARNED_INCOME");
        response.EnsureSuccessStatusCode();
        var incomes = await response.Content.ReadFromJsonAsync<List<OtherIncomeListItemDto>>(JsonOptions);

        Assert.NotEmpty(incomes!);
        Assert.All(incomes!, i => Assert.Equal("EARNED_INCOME", i.IncomeType));
    }

    [Fact]
    public async Task GetIncomeSummary_GroupsByIncomeType_ExcludesVoided()
    {
        await AuthenticateAsAdminAsync();
        var year = DateTime.UtcNow.Year;

        await CreateOtherIncomeAsync(incomeType: "WAQF_ENDOWMENT", fundingBucketCode: "OPER-POOL", amountOriginal: 1000m);
        var toVoid = await CreateOtherIncomeAsync(incomeType: "WAQF_ENDOWMENT", fundingBucketCode: "OPER-POOL", amountOriginal: 2000m);
        await _client.DeleteAsync($"/api/other-income/{toVoid.Id}");

        var response = await _client.GetAsync($"/api/reports/income-summary?year={year}");
        response.EnsureSuccessStatusCode();
        var summary = await response.Content.ReadFromJsonAsync<IncomeSummaryDto>(JsonOptions);

        var waqfRow = summary!.ByType.First(r => r.IncomeType == "WAQF_ENDOWMENT");
        Assert.Equal(1, waqfRow.Count);
        Assert.Equal(1000m, waqfRow.TotalPhp);
    }

    [Fact]
    public async Task GetRestrictedFundLedger_LabelsIncomeRowWithIncPrefix()
    {
        await AuthenticateAsAdminAsync();
        var income = await CreateOtherIncomeAsync(incomeType: "GRANT", fundingBucketCode: "REST-PROG", amountOriginal: 4000m);

        var response = await _client.GetAsync($"/api/reports/restricted-ledger?fundCode=REST-FUND&year={DateTime.UtcNow.Year}");
        response.EnsureSuccessStatusCode();
        var ledgers = await response.Content.ReadFromJsonAsync<List<RestrictedLedgerDto>>(JsonOptions);

        var restLedger = ledgers!.First(l => l.FundCode == "REST-FUND");
        Assert.Contains(restLedger.Entries, e => e.Reference == $"INC-{income.Id}" && e.AmountIn == 4000m);
    }

    [Fact]
    public async Task GetRestrictedFundLedger_ExcludesVoidedIncome()
    {
        await AuthenticateAsAdminAsync();
        var income = await CreateOtherIncomeAsync(incomeType: "GRANT", fundingBucketCode: "REST-PROG", amountOriginal: 1234m);

        var voidResponse = await _client.DeleteAsync($"/api/other-income/{income.Id}");
        voidResponse.EnsureSuccessStatusCode();

        var response = await _client.GetAsync($"/api/reports/restricted-ledger?fundCode=REST-FUND&year={DateTime.UtcNow.Year}");
        response.EnsureSuccessStatusCode();
        var ledgers = await response.Content.ReadFromJsonAsync<List<RestrictedLedgerDto>>(JsonOptions);

        var restLedger = ledgers!.First(l => l.FundCode == "REST-FUND");
        Assert.DoesNotContain(restLedger.Entries, e => e.Reference == $"INC-{income.Id}");
    }

    private sealed record LoginResponseDto(string AccessToken, string RefreshToken, DateTime RefreshTokenExpiresAt);

    private sealed record FundSummaryBucketDto(string FundCode, string BucketCode, string BucketName, decimal AllocatedAmount, decimal ExpensedAmount, decimal Remaining);

    private sealed record FundSummaryDto(List<FundSummaryBucketDto> Buckets);

    private sealed record OtherIncomeDetailDto(int Id, string IncomeType, bool IsVoided);

    private sealed record OtherIncomeListItemDto(int Id, string IncomeType, string Source, decimal AmountPhp, string FundingBucketCode, DateTime DateReceived, bool IsVoided);

    private sealed record IncomeTypeSummaryRowDto(string IncomeType, int Count, decimal TotalPhp);

    private sealed record IncomeSummaryDto(List<IncomeTypeSummaryRowDto> ByType, decimal GrandTotalPhp);

    private sealed record RestrictedLedgerEntryDto(DateTime Date, string TransactionType, string Reference, decimal AmountIn, decimal AmountOut, decimal RunningBalance, string? Notes);

    private sealed record RestrictedLedgerDto(string FundCode, string FundName, decimal OpeningBalancePhp, List<RestrictedLedgerEntryDto> Entries, decimal ClosingBalancePhp);
}
