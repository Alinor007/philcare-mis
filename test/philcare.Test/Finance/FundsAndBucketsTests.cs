using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using philcare.Test.Common;
using Xunit;

namespace philcare.Test.Finance;

public class FundsAndBucketsTests : IClassFixture<TestWebAppFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly HttpClient _client;

    public FundsAndBucketsTests(TestWebAppFactory factory)
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

    [Fact]
    public async Task GetFunds_WithoutAuthentication_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/funds");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetFunds_ReturnsAllSixSeededFunds()
    {
        await AuthenticateAsAdminAsync();

        var response = await _client.GetAsync("/api/funds");
        response.EnsureSuccessStatusCode();
        var funds = await response.Content.ReadFromJsonAsync<List<FundDto>>(JsonOptions);

        var codes = funds!.Select(f => f.Code).ToList();
        Assert.Equal(6, funds!.Count);
        Assert.Contains("ZAKA-FUND", codes);
        Assert.Contains("SADA-FUND", codes);
        Assert.Contains("GENE-FUND", codes);
        Assert.Contains("REST-FUND", codes);
        Assert.Contains("OPER-FUND", codes);
        Assert.Contains("CAPX-FUND", codes);

        var zakat = funds.Single(f => f.Code == "ZAKA-FUND");
        Assert.True(zakat.IsRestricted);
    }

    [Fact]
    public async Task GetFundingBuckets_ReturnsAllTenSeededBuckets()
    {
        await AuthenticateAsAdminAsync();

        var response = await _client.GetAsync("/api/funding-buckets");
        response.EnsureSuccessStatusCode();
        var buckets = await response.Content.ReadFromJsonAsync<List<FundingBucketDto>>(JsonOptions);

        Assert.Equal(10, buckets!.Count);

        var amil = buckets.Single(b => b.Code == "ZAK-AMIL");
        Assert.Equal("Admin", amil.BucketType);
        Assert.Equal(0.125m, amil.MaxAdminRate);

        var genAdmin = buckets.Single(b => b.Code == "GENE-ADMIN");
        Assert.Equal(0.15m, genAdmin.MaxAdminRate);
    }

    [Fact]
    public async Task GetFundingBuckets_FilteredByFundCode_ReturnsOnlyThatFundsBuckets()
    {
        await AuthenticateAsAdminAsync();

        var response = await _client.GetAsync("/api/funding-buckets?fundCode=ZAKA-FUND");
        response.EnsureSuccessStatusCode();
        var buckets = await response.Content.ReadFromJsonAsync<List<FundingBucketDto>>(JsonOptions);

        Assert.Equal(2, buckets!.Count); // ZAK-PROG, ZAK-AMIL
        Assert.All(buckets, b => Assert.Equal("ZAKA-FUND", b.FundCode));
    }

    [Fact]
    public async Task GetFundingBucketById_UnknownId_ReturnsNotFound()
    {
        await AuthenticateAsAdminAsync();

        var response = await _client.GetAsync("/api/funding-buckets/999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetFundingBucketById_KnownBucket_ReturnsDetail()
    {
        await AuthenticateAsAdminAsync();

        var listResponse = await _client.GetAsync("/api/funding-buckets?fundCode=GENE-FUND");
        listResponse.EnsureSuccessStatusCode();
        var buckets = await listResponse.Content.ReadFromJsonAsync<List<FundingBucketDto>>(JsonOptions);
        var programBucket = buckets!.Single(b => b.Code == "GENE-PROG");

        var detailResponse = await _client.GetAsync($"/api/funding-buckets/{programBucket.Id}");
        detailResponse.EnsureSuccessStatusCode();
        var detail = await detailResponse.Content.ReadFromJsonAsync<FundingBucketDetailDto>(JsonOptions);

        Assert.Equal("GENE-PROG", detail!.Code);
        Assert.NotNull(detail.Allocations);
        Assert.NotNull(detail.Expenses);
    }

    private sealed record LoginResponseDto(string AccessToken, string RefreshToken, DateTime RefreshTokenExpiresAt);

    private sealed record FundDto(int Id, string Code, string Name, bool IsRestricted, string? PolicyNotes, string? UseCase, bool SeparateTrackingRequired);

    private sealed record FundingBucketDto(
        int Id, string Code, string Name, string FundCode, string BucketType, decimal MaxAdminRate,
        decimal AllocatedAmount, decimal ExpensedAmount, decimal Remaining);

    private sealed record FundingBucketDetailDto(
        int Id, string Code, string Name, string FundCode, string BucketType, decimal MaxAdminRate,
        decimal AllocatedAmount, decimal ExpensedAmount, decimal Remaining, List<object> Allocations, List<object> Expenses);
}
