using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using philcare.Api.Features.Programs.Domain;
using philcare.Api.Features.Programs.Participants.CreateParticipant;
using philcare.Test.Common;
using Xunit;

namespace philcare.Test.Programs;

public class ParticipantsTests : IClassFixture<TestWebAppFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly HttpClient _client;

    public ParticipantsTests(TestWebAppFactory factory)
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
    public async Task CreateParticipant_ValidRequest_DefaultsStatusToPending()
    {
        await AuthenticateAsAdminAsync();

        var response = await _client.PostAsJsonAsync("/api/participants", new
        {
            FullName = $"Participant-{Guid.NewGuid():N}",
            ParticipantType = "BENEFICIARY",
            BeneficiaryType = "INDIVIDUAL",
            Gender = "Female",
            VulnerabilityCategory = "WIDOW",
            ConsentOnFile = true
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var participant = await response.Content.ReadFromJsonAsync<CreateParticipantResponse>(JsonOptions);

        Assert.Equal("PENDING", participant!.Status);
        Assert.Equal(Gender.Female, participant.Gender);
        Assert.True(participant.IsActive);
    }

    [Fact]
    public async Task CreateParticipant_EmptyName_ReturnsBadRequest()
    {
        await AuthenticateAsAdminAsync();

        var response = await _client.PostAsJsonAsync("/api/participants", new
        {
            FullName = "",
            ParticipantType = "BENEFICIARY",
            BeneficiaryType = "INDIVIDUAL",
            Gender = "Male",
            ConsentOnFile = false
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetParticipants_FilteredByType_ReturnsOnlyMatching()
    {
        await AuthenticateAsAdminAsync();
        var uniqueType = $"TYPE-{Guid.NewGuid():N}"[..20];

        var createResponse = await _client.PostAsJsonAsync("/api/participants", new
        {
            FullName = $"Participant-{Guid.NewGuid():N}",
            ParticipantType = uniqueType,
            BeneficiaryType = "INDIVIDUAL",
            Gender = "Unspecified",
            ConsentOnFile = false
        });
        createResponse.EnsureSuccessStatusCode();

        var listResponse = await _client.GetAsync($"/api/participants?participantType={uniqueType}");
        listResponse.EnsureSuccessStatusCode();
        var participants = await listResponse.Content.ReadFromJsonAsync<List<ParticipantListItemDto>>(JsonOptions);

        Assert.Single(participants!);
        Assert.Equal(uniqueType, participants![0].ParticipantType);
    }

    [Fact]
    public async Task GetParticipantById_UnknownId_ReturnsNotFound()
    {
        await AuthenticateAsAdminAsync();

        var response = await _client.GetAsync("/api/participants/999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdateParticipant_ChangesStatusAndVulnerability()
    {
        await AuthenticateAsAdminAsync();

        var createResponse = await _client.PostAsJsonAsync("/api/participants", new
        {
            FullName = $"Participant-{Guid.NewGuid():N}",
            ParticipantType = "BENEFICIARY",
            BeneficiaryType = "INDIVIDUAL",
            Gender = "Male",
            ConsentOnFile = false
        });
        createResponse.EnsureSuccessStatusCode();
        var participant = await createResponse.Content.ReadFromJsonAsync<CreateParticipantResponse>(JsonOptions);

        var updateResponse = await _client.PutAsJsonAsync($"/api/participants/{participant!.Id}", new
        {
            FullName = participant.FullName,
            ParticipantType = "BENEFICIARY",
            BeneficiaryType = "INDIVIDUAL",
            Gender = "Male",
            VulnerabilityCategory = "PWD",
            ConsentOnFile = true,
            Status = "VERIFIED",
            IsActive = true
        });

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        var getResponse = await _client.GetAsync($"/api/participants/{participant.Id}");
        getResponse.EnsureSuccessStatusCode();
        var detail = await getResponse.Content.ReadFromJsonAsync<ParticipantDetailDto>(JsonOptions);

        Assert.Equal("VERIFIED", detail!.Status);
        Assert.Equal("PWD", detail.VulnerabilityCategory);
        Assert.True(detail.ConsentOnFile);
    }

    private sealed record LoginResponseDto(string AccessToken, string RefreshToken, DateTime RefreshTokenExpiresAt);

    private sealed record ParticipantListItemDto(int Id, string FullName, string ParticipantType, string Gender, string Status, bool IsActive);

    private sealed record ParticipantDetailDto(int Id, string FullName, string Status, string? VulnerabilityCategory, bool ConsentOnFile);
}
