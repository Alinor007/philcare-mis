using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using philcare.Api.Features.HumanResources.Volunteers.CreateVolunteer;
using philcare.Test.Common;
using Xunit;

namespace philcare.Test.HumanResources.Volunteers;

public class VolunteersTests : IClassFixture<TestWebAppFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly HttpClient _client;

    public VolunteersTests(TestWebAppFactory factory)
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

    private async Task<int> CreateVolunteerAsync(bool orientationCompleted = false)
    {
        var response = await _client.PostAsJsonAsync("/api/volunteers", new
        {
            FullName = $"Volunteer-{Guid.NewGuid():N}",
            Gender = "Unspecified",
            OrientationCompleted = orientationCompleted,
            CodeOfConductSigned = false,
            PoliceClearanceOnFile = false
        });
        response.EnsureSuccessStatusCode();
        var volunteer = await response.Content.ReadFromJsonAsync<CreateVolunteerResponse>(JsonOptions);
        return volunteer!.Id;
    }

    [Fact]
    public async Task CreateVolunteer_ValidRequest_DefaultsStatusToActive()
    {
        await AuthenticateAsAdminAsync();

        var response = await _client.PostAsJsonAsync("/api/volunteers", new
        {
            FullName = "Test Volunteer",
            Gender = "Female",
            OrientationCompleted = true,
            OrientationDate = DateTime.UtcNow,
            CodeOfConductSigned = true,
            CodeOfConductDate = DateTime.UtcNow,
            PoliceClearanceOnFile = true
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var volunteer = await response.Content.ReadFromJsonAsync<CreateVolunteerResponse>(JsonOptions);
        Assert.Equal("ACTIVE", volunteer!.Status);
        Assert.True(volunteer.OrientationCompleted);
    }

    [Fact]
    public async Task GetVolunteers_FilteredByOrientationCompleted_ReturnsOnlyMatching()
    {
        await AuthenticateAsAdminAsync();
        var orientedId = await CreateVolunteerAsync(orientationCompleted: true);
        await CreateVolunteerAsync(orientationCompleted: false);

        var response = await _client.GetAsync("/api/volunteers?orientationCompleted=true");
        response.EnsureSuccessStatusCode();
        var volunteers = await response.Content.ReadFromJsonAsync<List<VolunteerListItemDto>>(JsonOptions);

        Assert.Contains(volunteers!, v => v.Id == orientedId);
        Assert.All(volunteers!, v => Assert.True(v.OrientationCompleted));
    }

    [Fact]
    public async Task UpdateVolunteer_UnknownId_ReturnsNotFound()
    {
        await AuthenticateAsAdminAsync();

        var response = await _client.PutAsJsonAsync("/api/volunteers/999999", new
        {
            FullName = "Ghost",
            Gender = "Unspecified",
            Status = "ACTIVE",
            OrientationCompleted = false,
            CodeOfConductSigned = false,
            PoliceClearanceOnFile = false,
            IsActive = true
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeactivateVolunteer_SetsInactiveAndStatus()
    {
        await AuthenticateAsAdminAsync();
        var volunteerId = await CreateVolunteerAsync();

        var response = await _client.DeleteAsync($"/api/volunteers/{volunteerId}");
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var getResponse = await _client.GetAsync($"/api/volunteers/{volunteerId}");
        getResponse.EnsureSuccessStatusCode();
        var volunteer = await getResponse.Content.ReadFromJsonAsync<VolunteerDetailDto>(JsonOptions);
        Assert.False(volunteer!.IsActive);
        Assert.Equal("INACTIVE", volunteer.Status);
    }

    private sealed record LoginResponseDto(string AccessToken, string RefreshToken, DateTime RefreshTokenExpiresAt);

    private sealed record VolunteerListItemDto(int Id, string FullName, string Gender, string Status, bool OrientationCompleted, bool IsActive);

    private sealed record VolunteerDetailDto(
        int Id, string FullName, string Gender, string? Phone, string? Email, string? Barangay, string? City, string? Province,
        string? Region, string? Skills, string Status, bool OrientationCompleted, DateTime? OrientationDate, bool CodeOfConductSigned,
        DateTime? CodeOfConductDate, bool PoliceClearanceOnFile, string? Notes, bool IsActive, int ActivityCount);
}
