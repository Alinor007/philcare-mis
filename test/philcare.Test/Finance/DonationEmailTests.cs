using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using philcare.Api.Features.Finance.Donations.CreateDonation;
using philcare.Api.Features.Finance.Donors.CreateDonor;
using philcare.Test.Common;
using Xunit;

namespace philcare.Test.Finance;

/// <summary>
/// Covers the donation confirmation email outbox: a Pending/Skipped row is written in the SAME
/// request as the donation, never blocking or failing it, and the manual resend action behaves
/// correctly around voided donations and donors with no email on file.
///
/// This test class explicitly overrides Email:Enabled=false/Email:ApiKey="" on top of whatever
/// TestWebAppFactory resolves — the Development environment plus this project's UserSecretsId
/// means a developer's real Resend key could otherwise be picked up here, which would make test
/// runs attempt live network sends. OutboxDispatcher never leaves Pending rows for these tests to
/// race against because it stays idle the whole time.
/// </summary>
public class DonationEmailTests : IClassFixture<TestWebAppFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly HttpClient _client;

    public DonationEmailTests(TestWebAppFactory factory)
    {
        var safeFactory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, configBuilder) =>
            {
                configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Email:Enabled"] = "false",
                    ["Email:ApiKey"] = ""
                });
            });
        });

        _client = safeFactory.CreateClient();
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

    private async Task<int> CreateDonorAsync(string name, string? email)
    {
        var response = await _client.PostAsJsonAsync("/api/donors", new
        {
            Name = name,
            Type = "Individual",
            Email = email,
            RiskRating = "Low",
            PepFlag = false,
            PrivacyConsent = true
        });

        response.EnsureSuccessStatusCode();
        var donor = await response.Content.ReadFromJsonAsync<CreateDonorResponse>(JsonOptions);
        return donor!.Id;
    }

    private async Task<CreateDonationResponse> CreateDonationAsync(int donorId, string fundCode, decimal amount, string? transactionRef = null)
    {
        var response = await _client.PostAsJsonAsync("/api/donations", new
        {
            DonorId = donorId,
            AmountOriginal = amount,
            Currency = "PHP",
            FxRateToPhp = 1m,
            DateReceived = DateTime.UtcNow,
            Channel = "GCash",
            FundCode = fundCode,
            AdminAllowed = false,
            AdminRateInput = 0m,
            TransactionRef = transactionRef
        });

        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CreateDonationResponse>(JsonOptions))!;
    }

    private async Task<DonationDetailDto> GetDonationDetailAsync(int id)
    {
        var response = await _client.GetAsync($"/api/donations/{id}");
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<DonationDetailDto>(JsonOptions))!;
    }

    [Fact]
    public async Task CreateDonation_DonorWithEmail_EnqueuesPendingConfirmationEmail()
    {
        await AuthenticateAsAdminAsync();
        var donorId = await CreateDonorAsync($"Donor-{Guid.NewGuid():N}", "donor@example.com");

        var donation = await CreateDonationAsync(donorId, "SADA-FUND", 500m, transactionRef: "GCASH-REF-123");
        var detail = await GetDonationDetailAsync(donation.Id);

        Assert.Equal("GCASH-REF-123", detail.TransactionRef);
        var email = Assert.Single(detail.Emails);
        Assert.Equal("DonationConfirmation", email.EmailType);
        Assert.Equal("Pending", email.Status);
        Assert.Equal("donor@example.com", email.ToEmail);
    }

    [Fact]
    public async Task CreateDonation_DonorWithoutEmail_EnqueuesSkippedConfirmationEmail()
    {
        await AuthenticateAsAdminAsync();
        var donorId = await CreateDonorAsync($"Donor-{Guid.NewGuid():N}", email: null);

        var donation = await CreateDonationAsync(donorId, "SADA-FUND", 500m);
        var detail = await GetDonationDetailAsync(donation.Id);

        var email = Assert.Single(detail.Emails);
        Assert.Equal("Skipped", email.Status);
        Assert.NotNull(email.LastError);
    }

    [Fact]
    public async Task VoidDonation_EnqueuesDonationVoidedEmail()
    {
        await AuthenticateAsAdminAsync();
        var donorId = await CreateDonorAsync($"Donor-{Guid.NewGuid():N}", "donor@example.com");
        var donation = await CreateDonationAsync(donorId, "SADA-FUND", 300m);

        var voidResponse = await _client.DeleteAsync($"/api/donations/{donation.Id}");
        voidResponse.EnsureSuccessStatusCode();

        var detail = await GetDonationDetailAsync(donation.Id);

        Assert.Equal(2, detail.Emails.Count);
        Assert.Contains(detail.Emails, e => e.EmailType == "DonationVoided" && e.Status == "Pending");
    }

    [Fact]
    public async Task ResendDonationReceipt_VoidedDonation_ReturnsBadRequest()
    {
        await AuthenticateAsAdminAsync();
        var donorId = await CreateDonorAsync($"Donor-{Guid.NewGuid():N}", "donor@example.com");
        var donation = await CreateDonationAsync(donorId, "SADA-FUND", 200m);

        (await _client.DeleteAsync($"/api/donations/{donation.Id}")).EnsureSuccessStatusCode();

        var response = await _client.PostAsync($"/api/donations/{donation.Id}/resend-receipt", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ResendDonationReceipt_DonorHasNoEmail_ReturnsBadRequest()
    {
        await AuthenticateAsAdminAsync();
        var donorId = await CreateDonorAsync($"Donor-{Guid.NewGuid():N}", email: null);
        var donation = await CreateDonationAsync(donorId, "SADA-FUND", 200m);

        var response = await _client.PostAsync($"/api/donations/{donation.Id}/resend-receipt", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ResendDonationReceipt_Success_EnqueuesAdditionalPendingRow()
    {
        await AuthenticateAsAdminAsync();
        var donorId = await CreateDonorAsync($"Donor-{Guid.NewGuid():N}", "donor@example.com");
        var donation = await CreateDonationAsync(donorId, "SADA-FUND", 200m);

        var response = await _client.PostAsync($"/api/donations/{donation.Id}/resend-receipt", null);
        response.EnsureSuccessStatusCode();

        var detail = await GetDonationDetailAsync(donation.Id);

        // The original confirmation plus the manual resend — two separate rows, not a dedup.
        Assert.Equal(2, detail.Emails.Count(e => e.EmailType == "DonationConfirmation"));
    }

    private sealed record LoginResponseDto(string AccessToken, string RefreshToken, DateTime RefreshTokenExpiresAt);

    private sealed record DonationEmailStatusDto(int Id, string EmailType, string Status, string ToEmail, int AttemptCount, string? LastError);

    private sealed record DonationDetailDto(int Id, string? TransactionRef, bool IsVoided, List<DonationEmailStatusDto> Emails);
}
