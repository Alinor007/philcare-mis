using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using philcare.Api.Features.Governance.People.CreatePerson;
using philcare.Test.Common;
using Xunit;

namespace philcare.Test.Governance;

public class PeopleTests : IClassFixture<TestWebAppFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly HttpClient _client;

    public PeopleTests(TestWebAppFactory factory)
    {
        _client = factory.CreateClient();
    }

    private async Task AuthenticateAsAdminAsync()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new { Email = "admin@philcare.local", Password = "Admin@12345" });
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<LoginResponseDto>(JsonOptions);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body!.AccessToken);
    }

    private async Task<int> CreatePersonAsync(string? category = null)
    {
        var response = await _client.PostAsJsonAsync("/api/governance/people", new
        {
            FullName = $"Person-{Guid.NewGuid():N}",
            PersonCategory = category ?? "BOARD",
            DefaultVotingRights = true
        });
        response.EnsureSuccessStatusCode();
        var person = await response.Content.ReadFromJsonAsync<CreatePersonResponse>(JsonOptions);
        return person!.Id;
    }

    [Fact]
    public async Task CreatePerson_ValidRequest_DefaultsStatusToActive()
    {
        await AuthenticateAsAdminAsync();

        var response = await _client.PostAsJsonAsync("/api/governance/people", new
        {
            FullName = "Test Trustee",
            PersonCategory = "BOARD",
            Email = "trustee@philcare.local",
            DefaultVotingRights = true
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var person = await response.Content.ReadFromJsonAsync<CreatePersonResponse>(JsonOptions);
        Assert.Equal("ACTIVE", person!.Status);
    }

    [Fact]
    public async Task CreatePerson_WithUnknownVolunteerId_ReturnsNotFound()
    {
        await AuthenticateAsAdminAsync();

        var response = await _client.PostAsJsonAsync("/api/governance/people", new
        {
            FullName = "Test Person",
            PersonCategory = "MEMBER",
            DefaultVotingRights = false,
            VolunteerId = 999999
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetPeople_FilteredByCategory_ReturnsOnlyMatching()
    {
        await AuthenticateAsAdminAsync();
        var boardId = await CreatePersonAsync("BOARD");
        await CreatePersonAsync("MEMBER");

        var response = await _client.GetAsync("/api/governance/people?personCategory=BOARD");
        response.EnsureSuccessStatusCode();
        var people = await response.Content.ReadFromJsonAsync<List<PersonListItemDto>>(JsonOptions);

        Assert.Contains(people!, p => p.Id == boardId);
        Assert.All(people!, p => Assert.Equal("BOARD", p.PersonCategory));
    }

    [Fact]
    public async Task DeactivatePerson_SetsInactive()
    {
        await AuthenticateAsAdminAsync();
        var personId = await CreatePersonAsync();

        var response = await _client.DeleteAsync($"/api/governance/people/{personId}");
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var getResponse = await _client.GetAsync($"/api/governance/people/{personId}");
        getResponse.EnsureSuccessStatusCode();
        var person = await getResponse.Content.ReadFromJsonAsync<PersonDetailDto>(JsonOptions);
        Assert.False(person!.IsActive);
    }

    [Fact]
    public async Task UpdatePerson_UnknownId_ReturnsNotFound()
    {
        await AuthenticateAsAdminAsync();

        var response = await _client.PutAsJsonAsync("/api/governance/people/999999", new
        {
            FullName = "Ghost",
            PersonCategory = "MEMBER",
            Status = "ACTIVE",
            DefaultVotingRights = false,
            IsActive = true
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private sealed record LoginResponseDto(string AccessToken, string RefreshToken, DateTime RefreshTokenExpiresAt);
    private sealed record PersonListItemDto(int Id, string FullName, string PersonCategory, string Status, bool IsActive);
    private sealed record PersonDetailDto(
        int Id, string FullName, string PersonCategory, string Status, string? Email, string? ContactNumber,
        bool DefaultVotingRights, int? VolunteerId, string? Notes, bool IsActive, int AssignmentCount);
}
