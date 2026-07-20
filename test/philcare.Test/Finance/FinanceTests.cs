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

public class FinanceTests : IClassFixture<TestWebAppFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly HttpClient _client;

    public FinanceTests(TestWebAppFactory factory)
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
            Type = "Individual"
        });

        response.EnsureSuccessStatusCode();
        var donor = await response.Content.ReadFromJsonAsync<CreateDonorResponse>(JsonOptions);
        return donor!.Id;
    }

    [Fact]
    public async Task CreateDonation_WithoutAuthentication_ReturnsUnauthorized()
    {
        var response = await _client.PostAsJsonAsync("/api/donations", new
        {
            DonorId = 1,
            Amount = 100,
            Currency = "PHP",
            FundType = "SADAQAH",
            ReceivedDate = DateTime.UtcNow,
            PaymentMethod = "CASH",
            AdminAllowed = false,
            AdminRate = 0,
            AmilRate = 0
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateDonation_Zakat_SplitsAllocationCorrectly()
    {
        await AuthenticateAsAdminAsync();
        var donorId = await CreateDonorAsync($"Donor-{Guid.NewGuid():N}");

        var response = await _client.PostAsJsonAsync("/api/donations", new
        {
            DonorId = donorId,
            Amount = 10000m,
            Currency = "PHP",
            FundType = "ZAKAT",
            ReceivedDate = DateTime.UtcNow,
            PaymentMethod = "CASH",
            AdminAllowed = true,
            AdminRate = 0.15m,
            AmilRate = 0.125m
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var donation = await response.Content.ReadFromJsonAsync<CreateDonationResponse>(JsonOptions);

        Assert.Equal(1500m, donation!.Allocation.AdminAmount);
        Assert.Equal(1250m, donation.Allocation.AmilAmount);
        Assert.Equal(7250m, donation.Allocation.ProgramAmount);
    }

    [Fact]
    public async Task CreateDonation_AdminRateAboveCap_ReturnsBadRequest()
    {
        await AuthenticateAsAdminAsync();
        var donorId = await CreateDonorAsync($"Donor-{Guid.NewGuid():N}");

        var response = await _client.PostAsJsonAsync("/api/donations", new
        {
            DonorId = donorId,
            Amount = 1000m,
            Currency = "PHP",
            FundType = "SADAQAH",
            ReceivedDate = DateTime.UtcNow,
            PaymentMethod = "CASH",
            AdminAllowed = true,
            AdminRate = 0.20m,
            AmilRate = 0m
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateExpense_AmountExceedsBucketBalance_ReturnsBadRequest()
    {
        await AuthenticateAsAdminAsync();
        var donorId = await CreateDonorAsync($"Donor-{Guid.NewGuid():N}");
        var fundType = $"TESTFUND-{Guid.NewGuid():N}"[..20];

        var donationResponse = await _client.PostAsJsonAsync("/api/donations", new
        {
            DonorId = donorId,
            Amount = 100m,
            Currency = "PHP",
            FundType = fundType,
            ReceivedDate = DateTime.UtcNow,
            PaymentMethod = "CASH",
            AdminAllowed = false,
            AdminRate = 0m,
            AmilRate = 0m
        });
        donationResponse.EnsureSuccessStatusCode();

        var bucketId = await GetBucketIdForFundTypeAsync(fundType);

        var expenseResponse = await _client.PostAsJsonAsync("/api/expenses", new
        {
            FundBucketId = bucketId,
            Amount = 999_999_999m,
            ExpenseCategory = "PROGRAM",
            PaymentMethod = "CASH",
            ExpenseDate = DateTime.UtcNow,
            Description = "Way more than the bucket holds"
        });

        Assert.Equal(HttpStatusCode.BadRequest, expenseResponse.StatusCode);
    }

    [Fact]
    public async Task CreateExpense_ZakatBucketWithoutAsnaf_ReturnsBadRequest()
    {
        await AuthenticateAsAdminAsync();
        var donorId = await CreateDonorAsync($"Donor-{Guid.NewGuid():N}");

        // Fund the shared ZAKAT bucket generously so this test's tiny expense
        // never trips the balance check, regardless of other tests' state.
        var donationResponse = await _client.PostAsJsonAsync("/api/donations", new
        {
            DonorId = donorId,
            Amount = 500_000m,
            Currency = "PHP",
            FundType = "ZAKAT",
            ReceivedDate = DateTime.UtcNow,
            PaymentMethod = "CASH",
            AdminAllowed = false,
            AdminRate = 0m,
            AmilRate = 0m
        });
        donationResponse.EnsureSuccessStatusCode();

        var bucketId = await GetBucketIdForFundTypeAsync("ZAKAT");

        var expenseResponse = await _client.PostAsJsonAsync("/api/expenses", new
        {
            FundBucketId = bucketId,
            Amount = 1m,
            ExpenseCategory = "RELIEF",
            PaymentMethod = "CASH",
            ExpenseDate = DateTime.UtcNow,
            Description = "Missing zakat asnaf"
        });

        Assert.Equal(HttpStatusCode.BadRequest, expenseResponse.StatusCode);
    }

    [Fact]
    public async Task VoidDonation_WhoseFundsAreAlreadySpent_ReturnsBadRequest()
    {
        await AuthenticateAsAdminAsync();
        var donorId = await CreateDonorAsync($"Donor-{Guid.NewGuid():N}");
        var fundType = $"TESTFUND-{Guid.NewGuid():N}"[..20];

        var donationResponse = await _client.PostAsJsonAsync("/api/donations", new
        {
            DonorId = donorId,
            Amount = 5000m,
            Currency = "PHP",
            FundType = fundType,
            ReceivedDate = DateTime.UtcNow,
            PaymentMethod = "CASH",
            AdminAllowed = false,
            AdminRate = 0m,
            AmilRate = 0m
        });
        donationResponse.EnsureSuccessStatusCode();
        var donation = await donationResponse.Content.ReadFromJsonAsync<CreateDonationResponse>(JsonOptions);

        var bucketId = await GetBucketIdForFundTypeAsync(fundType);

        var expenseResponse = await _client.PostAsJsonAsync("/api/expenses", new
        {
            FundBucketId = bucketId,
            Amount = 5000m,
            ExpenseCategory = "PROGRAM",
            PaymentMethod = "CASH",
            ExpenseDate = DateTime.UtcNow,
            Description = "Spends the entire donation"
        });
        expenseResponse.EnsureSuccessStatusCode();

        var voidResponse = await _client.DeleteAsync($"/api/donations/{donation!.Id}");

        Assert.Equal(HttpStatusCode.BadRequest, voidResponse.StatusCode);
    }

    private async Task<int> GetBucketIdForFundTypeAsync(string fundType)
    {
        var response = await _client.GetAsync("/api/fund-buckets");
        response.EnsureSuccessStatusCode();
        var buckets = await response.Content.ReadFromJsonAsync<List<FundBucketDto>>(JsonOptions);
        return buckets!.First(b => string.Equals(b.FundType, fundType, StringComparison.OrdinalIgnoreCase)).Id;
    }

    private sealed record LoginResponseDto(string AccessToken, string RefreshToken, DateTime RefreshTokenExpiresAt);

    private sealed record FundBucketDto(int Id, string Name, string FundType, decimal TotalReceived, decimal AdminAllocated, decimal ProgramAllocated, decimal TotalExpensed, decimal Balance);
}
