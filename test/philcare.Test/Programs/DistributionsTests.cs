using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using philcare.Api.Features.Programs.Distributions.CreateDistribution;
using philcare.Api.Features.Programs.Participants.CreateParticipant;
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
            Gender = "Unspecified",
            ConsentOnFile = true
        });
        response.EnsureSuccessStatusCode();
        var participant = await response.Content.ReadFromJsonAsync<CreateParticipantResponse>(JsonOptions);
        return participant!.Id;
    }

    [Fact]
    public async Task CreateDistribution_WithoutActivityOrBucket_Succeeds()
    {
        await AuthenticateAsAdminAsync();
        var participantId = await CreateParticipantAsync();

        var response = await _client.PostAsJsonAsync("/api/distributions", new
        {
            DistributionType = "FOOD_PACK",
            ParticipantId = participantId,
            Quantity = 2,
            TotalValuePhp = 500m,
            DistributionDate = DateTime.UtcNow,
            FieldVerified = true,
            ReceivedConfirmation = true
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var distribution = await response.Content.ReadFromJsonAsync<CreateDistributionResponse>(JsonOptions);

        Assert.Equal("FOOD_PACK", distribution!.DistributionType);
        Assert.Null(distribution.FundingBucketCode);
        Assert.False(distribution.IsVoided);
    }

    [Fact]
    public async Task CreateDistribution_UnknownParticipant_ReturnsNotFound()
    {
        await AuthenticateAsAdminAsync();

        var response = await _client.PostAsJsonAsync("/api/distributions", new
        {
            DistributionType = "FOOD_PACK",
            ParticipantId = 999999,
            Quantity = 1,
            TotalValuePhp = 100m,
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
        var participantId = await CreateParticipantAsync();

        var response = await _client.PostAsJsonAsync("/api/distributions", new
        {
            DistributionType = "CASH_ASSISTANCE",
            ParticipantId = participantId,
            FundingBucketCode = "NOT-A-REAL-BUCKET",
            Quantity = 1,
            TotalValuePhp = 1000m,
            DistributionDate = DateTime.UtcNow,
            FieldVerified = false,
            ReceivedConfirmation = false
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateDistribution_AgainstZakatProgramBucketWithoutAsnaf_ReturnsBadRequest()
    {
        await AuthenticateAsAdminAsync();
        var participantId = await CreateParticipantAsync();

        var response = await _client.PostAsJsonAsync("/api/distributions", new
        {
            DistributionType = "CASH_ASSISTANCE",
            ParticipantId = participantId,
            FundingBucketCode = "ZAK-PROG", // seeded Finance zakat program bucket
            Quantity = 1,
            TotalValuePhp = 1000m,
            DistributionDate = DateTime.UtcNow,
            FieldVerified = false,
            ReceivedConfirmation = false
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateDistribution_AgainstZakatProgramBucketWithAsnaf_Succeeds()
    {
        await AuthenticateAsAdminAsync();
        var participantId = await CreateParticipantAsync();

        var response = await _client.PostAsJsonAsync("/api/distributions", new
        {
            DistributionType = "CASH_ASSISTANCE",
            ParticipantId = participantId,
            FundingBucketCode = "ZAK-PROG",
            ZakatAsnaf = "FUQARA",
            Quantity = 1,
            TotalValuePhp = 1000m,
            DistributionDate = DateTime.UtcNow,
            FieldVerified = true,
            ReceivedConfirmation = true
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task VoidDistribution_ThenVoidAgain_ReturnsConflict()
    {
        await AuthenticateAsAdminAsync();
        var participantId = await CreateParticipantAsync();

        var createResponse = await _client.PostAsJsonAsync("/api/distributions", new
        {
            DistributionType = "HYGIENE_KIT",
            ParticipantId = participantId,
            Quantity = 1,
            TotalValuePhp = 200m,
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
        var participantId = await CreateParticipantAsync();

        await _client.PostAsJsonAsync("/api/distributions", new
        {
            DistributionType = "SCHOOL_SUPPLIES",
            ParticipantId = participantId,
            Quantity = 1,
            TotalValuePhp = 300m,
            DistributionDate = DateTime.UtcNow,
            FieldVerified = false,
            ReceivedConfirmation = false
        });

        var response = await _client.GetAsync($"/api/distributions?participantId={participantId}");
        response.EnsureSuccessStatusCode();
        var distributions = await response.Content.ReadFromJsonAsync<List<DistributionListItemDto>>(JsonOptions);

        Assert.Single(distributions!);
        Assert.Equal(participantId, distributions![0].ParticipantId);
    }

    private sealed record LoginResponseDto(string AccessToken, string RefreshToken, DateTime RefreshTokenExpiresAt);

    private sealed record DistributionListItemDto(int Id, string DistributionType, int ParticipantId, string ParticipantName, decimal TotalValuePhp, DateTime DistributionDate, bool IsVoided);
}
