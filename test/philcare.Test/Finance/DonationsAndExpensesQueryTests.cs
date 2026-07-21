using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using philcare.Api.Features.Finance.Donations.CreateDonation;
using philcare.Api.Features.Finance.Donors.CreateDonor;
using philcare.Api.Features.Finance.Expenses.CreateExpense;
using philcare.Test.Common;
using Xunit;

namespace philcare.Test.Finance;

public class DonationsAndExpensesQueryTests : IClassFixture<TestWebAppFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly HttpClient _client;

    public DonationsAndExpensesQueryTests(TestWebAppFactory factory)
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

    private async Task<CreateDonationResponse> CreateDonationAsync(int donorId, string fundCode, decimal amount)
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
        return (await response.Content.ReadFromJsonAsync<CreateDonationResponse>(JsonOptions))!;
    }

    [Fact]
    public async Task GetDonations_WithoutAuthentication_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/donations");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetDonations_FilteredByDonorId_ReturnsOnlyThatDonorsDonations()
    {
        await AuthenticateAsAdminAsync();
        var donorId = await CreateDonorAsync($"Donor-{Guid.NewGuid():N}");

        await CreateDonationAsync(donorId, "SADA-FUND", 500m);
        await CreateDonationAsync(donorId, "SADA-FUND", 750m);

        var response = await _client.GetAsync($"/api/donations?donorId={donorId}");
        response.EnsureSuccessStatusCode();
        var donations = await response.Content.ReadFromJsonAsync<List<DonationListItemDto>>(JsonOptions);

        Assert.Equal(2, donations!.Count);
        Assert.All(donations, d => Assert.Equal(donorId, d.DonorId));
    }

    [Fact]
    public async Task GetDonationById_ReturnsAllocationLines()
    {
        await AuthenticateAsAdminAsync();
        var donorId = await CreateDonorAsync($"Donor-{Guid.NewGuid():N}");
        var donation = await CreateDonationAsync(donorId, "SADA-FUND", 1000m);

        var response = await _client.GetAsync($"/api/donations/{donation.Id}");
        response.EnsureSuccessStatusCode();
        var detail = await response.Content.ReadFromJsonAsync<DonationDetailDto>(JsonOptions);

        Assert.Equal(1000m, detail!.AmountPhp);
        Assert.Single(detail.Allocations);
        Assert.False(detail.IsVoided);
    }

    [Fact]
    public async Task GetDonationById_UnknownId_ReturnsNotFound()
    {
        await AuthenticateAsAdminAsync();

        var response = await _client.GetAsync("/api/donations/999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task VoidDonation_UnspentDonation_SucceedsAndMarksVoided()
    {
        await AuthenticateAsAdminAsync();
        var donorId = await CreateDonorAsync($"Donor-{Guid.NewGuid():N}");
        var donation = await CreateDonationAsync(donorId, "SADA-FUND", 250m);

        var voidResponse = await _client.DeleteAsync($"/api/donations/{donation.Id}");
        Assert.Equal(HttpStatusCode.NoContent, voidResponse.StatusCode);

        var getResponse = await _client.GetAsync($"/api/donations/{donation.Id}");
        getResponse.EnsureSuccessStatusCode();
        var detail = await getResponse.Content.ReadFromJsonAsync<DonationDetailDto>(JsonOptions);
        Assert.True(detail!.IsVoided);
    }

    [Fact]
    public async Task VoidDonation_AlreadyVoided_ReturnsConflict()
    {
        await AuthenticateAsAdminAsync();
        var donorId = await CreateDonorAsync($"Donor-{Guid.NewGuid():N}");
        var donation = await CreateDonationAsync(donorId, "SADA-FUND", 100m);

        var firstVoid = await _client.DeleteAsync($"/api/donations/{donation.Id}");
        firstVoid.EnsureSuccessStatusCode();

        var secondVoid = await _client.DeleteAsync($"/api/donations/{donation.Id}");
        Assert.Equal(HttpStatusCode.Conflict, secondVoid.StatusCode);
    }

    [Fact]
    public async Task VoidDonation_RequiresAdminRole()
    {
        await AuthenticateAsAdminAsync();
        var donorId = await CreateDonorAsync($"Donor-{Guid.NewGuid():N}");
        var donation = await CreateDonationAsync(donorId, "SADA-FUND", 100m);

        // Register a Finance-role (non-Admin) user and act as them.
        var financeEmail = $"finance-{Guid.NewGuid():N}@philcare.local";
        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            Email = financeEmail,
            Password = "Finance@12345",
            Role = "Finance"
        });
        registerResponse.EnsureSuccessStatusCode();

        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new { Email = financeEmail, Password = "Finance@12345" });
        loginResponse.EnsureSuccessStatusCode();
        var financeToken = await loginResponse.Content.ReadFromJsonAsync<LoginResponseDto>(JsonOptions);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", financeToken!.AccessToken);

        var voidResponse = await _client.DeleteAsync($"/api/donations/{donation.Id}");

        Assert.Equal(HttpStatusCode.Forbidden, voidResponse.StatusCode);
    }

    [Fact]
    public async Task CreateExpense_UnknownFundingBucket_ReturnsNotFound()
    {
        await AuthenticateAsAdminAsync();

        var response = await _client.PostAsJsonAsync("/api/expenses", new
        {
            ExpenseDate = DateTime.UtcNow,
            PayeeVendor = "Test Vendor",
            ExpenseCategory = "PROGRAM",
            Description = "Bucket does not exist",
            PaymentMethod = "CASH",
            AmountOriginal = 10m,
            Currency = "PHP",
            FxRateToPhp = 1m,
            FundingBucketCode = "NOT-A-REAL-BUCKET"
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetExpenses_FilteredByCategory_ReturnsOnlyMatchingExpenses()
    {
        await AuthenticateAsAdminAsync();
        var donorId = await CreateDonorAsync($"Donor-{Guid.NewGuid():N}");
        await CreateDonationAsync(donorId, "SADA-FUND", 10_000m);

        var uniqueCategory = $"CAT-{Guid.NewGuid():N}"[..20];
        var expenseResponse = await _client.PostAsJsonAsync("/api/expenses", new
        {
            ExpenseDate = DateTime.UtcNow,
            PayeeVendor = "Test Vendor",
            ExpenseCategory = uniqueCategory,
            Description = "Filter test",
            PaymentMethod = "CASH",
            AmountOriginal = 50m,
            Currency = "PHP",
            FxRateToPhp = 1m,
            FundingBucketCode = "SADA-PROG"
        });
        expenseResponse.EnsureSuccessStatusCode();
        var expense = await expenseResponse.Content.ReadFromJsonAsync<CreateExpenseResponse>(JsonOptions);

        var listResponse = await _client.GetAsync($"/api/expenses?expenseCategory={uniqueCategory}");
        listResponse.EnsureSuccessStatusCode();
        var expenses = await listResponse.Content.ReadFromJsonAsync<List<ExpenseListItemDto>>(JsonOptions);

        Assert.Single(expenses!);
        Assert.Equal(expense!.Id, expenses![0].Id);
    }

    [Fact]
    public async Task GetExpenseById_UnknownId_ReturnsNotFound()
    {
        await AuthenticateAsAdminAsync();

        var response = await _client.GetAsync("/api/expenses/999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task VoidExpense_RestoresBucketBalance()
    {
        await AuthenticateAsAdminAsync();
        var donorId = await CreateDonorAsync($"Donor-{Guid.NewGuid():N}");
        await CreateDonationAsync(donorId, "SADA-FUND", 10_000m);

        var beforeBucket = (await (await _client.GetAsync("/api/funding-buckets?fundCode=SADA-FUND")).Content
            .ReadFromJsonAsync<List<FundingBucketDto>>(JsonOptions))!.Single(b => b.Code == "SADA-PROG");

        var expenseResponse = await _client.PostAsJsonAsync("/api/expenses", new
        {
            ExpenseDate = DateTime.UtcNow,
            PayeeVendor = "Test Vendor",
            ExpenseCategory = "PROGRAM",
            Description = "To be voided",
            PaymentMethod = "CASH",
            AmountOriginal = 300m,
            Currency = "PHP",
            FxRateToPhp = 1m,
            FundingBucketCode = "SADA-PROG"
        });
        expenseResponse.EnsureSuccessStatusCode();
        var expense = await expenseResponse.Content.ReadFromJsonAsync<CreateExpenseResponse>(JsonOptions);

        var afterExpenseBucket = (await (await _client.GetAsync("/api/funding-buckets?fundCode=SADA-FUND")).Content
            .ReadFromJsonAsync<List<FundingBucketDto>>(JsonOptions))!.Single(b => b.Code == "SADA-PROG");
        Assert.Equal(beforeBucket.ExpensedAmount + 300m, afterExpenseBucket.ExpensedAmount);

        var voidResponse = await _client.DeleteAsync($"/api/expenses/{expense!.Id}");
        Assert.Equal(HttpStatusCode.NoContent, voidResponse.StatusCode);

        var afterVoidBucket = (await (await _client.GetAsync("/api/funding-buckets?fundCode=SADA-FUND")).Content
            .ReadFromJsonAsync<List<FundingBucketDto>>(JsonOptions))!.Single(b => b.Code == "SADA-PROG");
        Assert.Equal(beforeBucket.ExpensedAmount, afterVoidBucket.ExpensedAmount);
    }

    [Fact]
    public async Task VoidExpense_AlreadyVoided_ReturnsConflict()
    {
        await AuthenticateAsAdminAsync();
        var donorId = await CreateDonorAsync($"Donor-{Guid.NewGuid():N}");
        await CreateDonationAsync(donorId, "SADA-FUND", 5_000m);

        var expenseResponse = await _client.PostAsJsonAsync("/api/expenses", new
        {
            ExpenseDate = DateTime.UtcNow,
            PayeeVendor = "Test Vendor",
            ExpenseCategory = "PROGRAM",
            Description = "Double void",
            PaymentMethod = "CASH",
            AmountOriginal = 100m,
            Currency = "PHP",
            FxRateToPhp = 1m,
            FundingBucketCode = "SADA-PROG"
        });
        expenseResponse.EnsureSuccessStatusCode();
        var expense = await expenseResponse.Content.ReadFromJsonAsync<CreateExpenseResponse>(JsonOptions);

        var firstVoid = await _client.DeleteAsync($"/api/expenses/{expense!.Id}");
        firstVoid.EnsureSuccessStatusCode();

        var secondVoid = await _client.DeleteAsync($"/api/expenses/{expense.Id}");
        Assert.Equal(HttpStatusCode.Conflict, secondVoid.StatusCode);
    }

    private sealed record LoginResponseDto(string AccessToken, string RefreshToken, DateTime RefreshTokenExpiresAt);

    private sealed record DonationListItemDto(int Id, int DonorId, string DonorName, decimal AmountPhp, string FundCode, DateTime DateReceived, bool IsVoided);

    private sealed record DonationDetailDto(int Id, decimal AmountPhp, bool IsVoided, List<object> Allocations);

    private sealed record ExpenseListItemDto(int Id, string FundingBucketCode, decimal AmountPhp, string ExpenseCategory, DateTime ExpenseDate, bool IsVoided);

    private sealed record FundingBucketDto(
        int Id, string Code, string Name, string FundCode, string BucketType, decimal MaxAdminRate,
        decimal AllocatedAmount, decimal ExpensedAmount, decimal Remaining);
}
