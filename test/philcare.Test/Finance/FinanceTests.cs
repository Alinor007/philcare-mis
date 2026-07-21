using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using philcare.Api.Features.Finance.Domain;
using philcare.Api.Features.Finance.Donations.CreateDonation;
using philcare.Api.Features.Finance.Donors.CreateDonor;
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
    public async Task CreateDonation_WithoutAuthentication_ReturnsUnauthorized()
    {
        var response = await _client.PostAsJsonAsync("/api/donations", new
        {
            DonorId = 1,
            AmountOriginal = 100m,
            Currency = "PHP",
            FxRateToPhp = 1m,
            DateReceived = DateTime.UtcNow,
            Channel = "Bank Transfer",
            FundCode = "SADA-FUND",
            AdminAllowed = false,
            AdminRateInput = 0m
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateDonation_Zakat_SplitsIntoProgramAndAmil()
    {
        await AuthenticateAsAdminAsync();
        var donorId = await CreateDonorAsync($"Donor-{Guid.NewGuid():N}");

        var response = await _client.PostAsJsonAsync("/api/donations", new
        {
            DonorId = donorId,
            AmountOriginal = 10000m,
            Currency = "PHP",
            FxRateToPhp = 1m,
            DateReceived = DateTime.UtcNow,
            Channel = "Bank Transfer",
            FundCode = "ZAKA-FUND",
            AdminAllowed = true,
            AdminRateInput = 0.125m // the zakat amil bucket caps at 12.5%
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var donation = await response.Content.ReadFromJsonAsync<CreateDonationResponse>(JsonOptions);

        Assert.Equal(10000m, donation!.AmountPhp);
        Assert.Equal(1250m, donation.AdminAllocationPhp);
        Assert.Equal(8750m, donation.ProgramAllocationPhp);
        Assert.Equal(2, donation.Allocations.Count);
        Assert.Contains(donation.Allocations, a => a.AllocationType == AllocationType.Amil && a.AllocatedAmountPhp == 1250m);
        Assert.Contains(donation.Allocations, a => a.AllocationType == AllocationType.Program && a.AllocatedAmountPhp == 8750m);
    }

    [Fact]
    public async Task CreateDonation_ForeignCurrency_ConvertsToPhp()
    {
        await AuthenticateAsAdminAsync();
        var donorId = await CreateDonorAsync($"Donor-{Guid.NewGuid():N}");

        var response = await _client.PostAsJsonAsync("/api/donations", new
        {
            DonorId = donorId,
            AmountOriginal = 100m,
            Currency = "USD",
            FxRateToPhp = 58m,
            DateReceived = DateTime.UtcNow,
            Channel = "Bank Transfer",
            FundCode = "GENE-FUND",
            AdminAllowed = false,
            AdminRateInput = 0m
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var donation = await response.Content.ReadFromJsonAsync<CreateDonationResponse>(JsonOptions);

        Assert.Equal(5800m, donation!.AmountPhp);
        Assert.Equal(5800m, donation.ProgramAllocationPhp);
    }

    [Fact]
    public async Task CreateDonation_AdminRateAboveBucketCap_ReturnsBadRequest()
    {
        await AuthenticateAsAdminAsync();
        var donorId = await CreateDonorAsync($"Donor-{Guid.NewGuid():N}");

        var response = await _client.PostAsJsonAsync("/api/donations", new
        {
            DonorId = donorId,
            AmountOriginal = 1000m,
            Currency = "PHP",
            FxRateToPhp = 1m,
            DateReceived = DateTime.UtcNow,
            Channel = "Bank Transfer",
            FundCode = "GENE-FUND", // admin bucket caps at 15%
            AdminAllowed = true,
            AdminRateInput = 0.20m
        });

        // FluentValidation also rejects >15% globally, but the assertion holds either way.
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateExpense_AmountExceedsBucketBalance_ReturnsBadRequest()
    {
        await AuthenticateAsAdminAsync();
        var donorId = await CreateDonorAsync($"Donor-{Guid.NewGuid():N}");

        var donationResponse = await _client.PostAsJsonAsync("/api/donations", new
        {
            DonorId = donorId,
            AmountOriginal = 100m,
            Currency = "PHP",
            FxRateToPhp = 1m,
            DateReceived = DateTime.UtcNow,
            Channel = "Bank Transfer",
            FundCode = "SADA-FUND",
            AdminAllowed = false,
            AdminRateInput = 0m
        });
        donationResponse.EnsureSuccessStatusCode();

        var expenseResponse = await _client.PostAsJsonAsync("/api/expenses", new
        {
            ExpenseDate = DateTime.UtcNow,
            PayeeVendor = "Test Vendor",
            ExpenseCategory = "PROGRAM",
            Description = "Way more than the bucket holds",
            PaymentMethod = "CASH",
            AmountOriginal = 999_999_999m,
            Currency = "PHP",
            FxRateToPhp = 1m,
            FundingBucketCode = "SADA-PROG"
        });

        Assert.Equal(HttpStatusCode.BadRequest, expenseResponse.StatusCode);
    }

    [Fact]
    public async Task CreateExpense_ZakatProgramBucketWithoutAsnaf_ReturnsBadRequest()
    {
        await AuthenticateAsAdminAsync();
        var donorId = await CreateDonorAsync($"Donor-{Guid.NewGuid():N}");

        // Fund the shared ZAK-PROG bucket generously so this test's tiny expense
        // never trips the balance check, regardless of other tests' state.
        var donationResponse = await _client.PostAsJsonAsync("/api/donations", new
        {
            DonorId = donorId,
            AmountOriginal = 500_000m,
            Currency = "PHP",
            FxRateToPhp = 1m,
            DateReceived = DateTime.UtcNow,
            Channel = "Bank Transfer",
            FundCode = "ZAKA-FUND",
            AdminAllowed = false,
            AdminRateInput = 0m
        });
        donationResponse.EnsureSuccessStatusCode();

        var expenseResponse = await _client.PostAsJsonAsync("/api/expenses", new
        {
            ExpenseDate = DateTime.UtcNow,
            PayeeVendor = "Test Vendor",
            ExpenseCategory = "RELIEF",
            Description = "Missing zakat asnaf",
            PaymentMethod = "CASH",
            AmountOriginal = 1m,
            Currency = "PHP",
            FxRateToPhp = 1m,
            FundingBucketCode = "ZAK-PROG"
        });

        Assert.Equal(HttpStatusCode.BadRequest, expenseResponse.StatusCode);
    }

    [Fact]
    public async Task VoidDonation_WhoseFundsAreAlreadySpent_ReturnsBadRequest()
    {
        await AuthenticateAsAdminAsync();
        var donorId = await CreateDonorAsync($"Donor-{Guid.NewGuid():N}");

        // CAPX-FUND is not touched by any other test, so its single bucket is
        // guaranteed pristine here regardless of test execution order.
        var donationResponse = await _client.PostAsJsonAsync("/api/donations", new
        {
            DonorId = donorId,
            AmountOriginal = 5000m,
            Currency = "PHP",
            FxRateToPhp = 1m,
            DateReceived = DateTime.UtcNow,
            Channel = "Bank Transfer",
            FundCode = "CAPX-FUND",
            AdminAllowed = false,
            AdminRateInput = 0m
        });
        donationResponse.EnsureSuccessStatusCode();
        var donation = await donationResponse.Content.ReadFromJsonAsync<CreateDonationResponse>(JsonOptions);

        var expenseResponse = await _client.PostAsJsonAsync("/api/expenses", new
        {
            ExpenseDate = DateTime.UtcNow,
            PayeeVendor = "Test Vendor",
            ExpenseCategory = "CAPEX",
            Description = "Spends the entire donation",
            PaymentMethod = "CASH",
            AmountOriginal = 5000m,
            Currency = "PHP",
            FxRateToPhp = 1m,
            FundingBucketCode = "CAPX-FUND"
        });
        expenseResponse.EnsureSuccessStatusCode();

        var voidResponse = await _client.DeleteAsync($"/api/donations/{donation!.Id}");

        Assert.Equal(HttpStatusCode.BadRequest, voidResponse.StatusCode);
    }

    [Fact]
    public async Task GetAdminRecoveryReport_ReturnsRowPerAdminBucket()
    {
        await AuthenticateAsAdminAsync();

        var response = await _client.GetAsync("/api/reports/admin-recovery");
        response.EnsureSuccessStatusCode();

        var report = await response.Content.ReadFromJsonAsync<AdminRecoveryReportDto>(JsonOptions);

        // Seeded admin/amil buckets: ZAK-AMIL, REST-ADMIN, GENE-ADMIN, SADA-ADMIN.
        Assert.Contains(report!.Buckets, b => b.BucketCode == "ZAK-AMIL" && b.PolicyCapRate == 0.125m);
        Assert.Contains(report.Buckets, b => b.BucketCode == "GENE-ADMIN" && b.PolicyCapRate == 0.15m);
    }

    private sealed record LoginResponseDto(string AccessToken, string RefreshToken, DateTime RefreshTokenExpiresAt);

    private sealed record AdminRecoveryRowDto(string BucketCode, decimal PolicyCapRate);

    private sealed record AdminRecoveryReportDto(List<AdminRecoveryRowDto> Buckets);
}
