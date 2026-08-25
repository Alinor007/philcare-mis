using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using philcare.Api.Features.Governance.People.CreatePerson;
using philcare.Api.Features.People.Memberships.CreateMembership;
using philcare.Test.Common;
using Xunit;

namespace philcare.Test.People;

public class MembershipsTests : IClassFixture<TestWebAppFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly HttpClient _client;

    public MembershipsTests(TestWebAppFactory factory)
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

    private async Task<int> CreatePersonAsync(string? fullName = null)
    {
        var response = await _client.PostAsJsonAsync("/api/governance/people", new
        {
            FullName = fullName ?? $"Member-{Guid.NewGuid():N}",
            PersonCategory = "MEMBER",
            DefaultVotingRights = false
        });

        response.EnsureSuccessStatusCode();
        var person = await response.Content.ReadFromJsonAsync<CreatePersonResponse>(JsonOptions);
        return person!.Id;
    }

    private static string NewNumber() => $"M-{Guid.NewGuid():N}"[..20];

    private async Task<CreateMembershipResponse> CreateMembershipAsync(int personId, string? number = null)
    {
        var response = await _client.PostAsJsonAsync("/api/memberships", new
        {
            PersonId = personId,
            MembershipNumber = number ?? NewNumber(),
            MembershipType = "REGULAR",
            JoinDate = new DateTime(2026, 1, 10)
        });

        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CreateMembershipResponse>(JsonOptions))!;
    }

    [Fact]
    public async Task CreateMembership_ValidRequest_DefaultsStatusToActive()
    {
        await AuthenticateAsAdminAsync();
        var personId = await CreatePersonAsync("Registered Member");

        var membership = await CreateMembershipAsync(personId);

        Assert.Equal("ACTIVE", membership.Status);
        Assert.Equal(personId, membership.PersonId);
        Assert.Equal("Registered Member", membership.FullName);
        Assert.True(membership.IsActive);
    }

    [Fact]
    public async Task CreateMembership_UnknownPerson_ReturnsNotFound()
    {
        await AuthenticateAsAdminAsync();

        var response = await _client.PostAsJsonAsync("/api/memberships", new
        {
            PersonId = 999999,
            MembershipNumber = NewNumber(),
            MembershipType = "REGULAR"
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateMembership_UnknownType_ReturnsBadRequest()
    {
        await AuthenticateAsAdminAsync();
        var personId = await CreatePersonAsync();

        var response = await _client.PostAsJsonAsync("/api/memberships", new
        {
            PersonId = personId,
            MembershipNumber = NewNumber(),
            MembershipType = "NOT_A_REAL_TYPE"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDto>(JsonOptions);
        Assert.Equal("Memberships.InvalidMembershipType", problem!.Title);
    }

    /// <summary>The membership number is the org identifier — unique across the whole roll.</summary>
    [Fact]
    public async Task CreateMembership_DuplicateNumber_ReturnsConflict()
    {
        await AuthenticateAsAdminAsync();
        var sharedNumber = NewNumber();
        await CreateMembershipAsync(await CreatePersonAsync(), sharedNumber);

        var response = await _client.PostAsJsonAsync("/api/memberships", new
        {
            PersonId = await CreatePersonAsync(),
            MembershipNumber = sharedNumber,
            MembershipType = "REGULAR"
        });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDto>(JsonOptions);
        Assert.Equal("Memberships.DuplicateNumber", problem!.Title);
    }

    [Fact]
    public async Task CreateMembership_WhilePersonAlreadyHasLiveOne_ReturnsConflict()
    {
        await AuthenticateAsAdminAsync();
        var personId = await CreatePersonAsync();
        await CreateMembershipAsync(personId);

        var response = await _client.PostAsJsonAsync("/api/memberships", new
        {
            PersonId = personId,
            MembershipNumber = NewNumber(),
            MembershipType = "LIFETIME"
        });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDto>(JsonOptions);
        Assert.Equal("Memberships.AlreadyMember", problem!.Title);
    }

    /// <summary>
    /// Unlike Staff/Volunteer, membership is not one-per-person: once the first is closed, the
    /// person can be re-registered under a new number and the roll keeps both rows.
    /// </summary>
    [Fact]
    public async Task Membership_CanBeRenewedUnderNewNumberAfterClosing()
    {
        await AuthenticateAsAdminAsync();
        var personId = await CreatePersonAsync();
        var first = await CreateMembershipAsync(personId);

        var close = await _client.DeleteAsync($"/api/memberships/{first.Id}");
        Assert.Equal(HttpStatusCode.NoContent, close.StatusCode);

        var renewed = await CreateMembershipAsync(personId);
        Assert.NotEqual(first.Id, renewed.Id);

        // Both rows survive — the closed one is only hidden from the default list.
        var all = await _client.GetFromJsonAsync<List<MembershipListItemDto>>(
            $"/api/memberships?personId={personId}&includeInactive=true", JsonOptions);
        Assert.Equal(2, all!.Count);
    }

    [Fact]
    public async Task DeactivateMembership_ClosesRowAndStampsExitDate()
    {
        await AuthenticateAsAdminAsync();
        var membership = await CreateMembershipAsync(await CreatePersonAsync());

        var response = await _client.DeleteAsync($"/api/memberships/{membership.Id}");
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var detail = await _client.GetFromJsonAsync<MembershipDetailDto>($"/api/memberships/{membership.Id}", JsonOptions);

        Assert.False(detail!.IsActive);
        Assert.Equal("RESIGNED", detail.Status);
        Assert.NotNull(detail.ExitDate);
    }

    [Fact]
    public async Task UpdateMembership_UnknownStatus_ReturnsBadRequest()
    {
        await AuthenticateAsAdminAsync();
        var membership = await CreateMembershipAsync(await CreatePersonAsync());

        var response = await _client.PutAsJsonAsync($"/api/memberships/{membership.Id}", new
        {
            MembershipNumber = membership.MembershipNumber,
            MembershipType = "REGULAR",
            Status = "NOT_A_REAL_STATUS",
            IsActive = true
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDto>(JsonOptions);
        Assert.Equal("Memberships.InvalidStatus", problem!.Title);
    }

    [Fact]
    public async Task UpdateMembership_ChangesTypeAndStatus()
    {
        await AuthenticateAsAdminAsync();
        var membership = await CreateMembershipAsync(await CreatePersonAsync());

        var response = await _client.PutAsJsonAsync($"/api/memberships/{membership.Id}", new
        {
            MembershipNumber = membership.MembershipNumber,
            MembershipType = "LIFETIME",
            Status = "LAPSED",
            JoinDate = new DateTime(2026, 1, 10),
            RenewalDate = new DateTime(2027, 1, 10),
            IsActive = true
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var detail = await _client.GetFromJsonAsync<MembershipDetailDto>($"/api/memberships/{membership.Id}", JsonOptions);
        Assert.Equal("LIFETIME", detail!.MembershipType);
        Assert.Equal("LAPSED", detail.Status);
    }

    [Fact]
    public async Task GetMemberships_FilteredByPerson_ReturnsOnlyTheirs()
    {
        await AuthenticateAsAdminAsync();
        var personId = await CreatePersonAsync();
        var membership = await CreateMembershipAsync(personId);
        await CreateMembershipAsync(await CreatePersonAsync());

        var rows = await _client.GetFromJsonAsync<List<MembershipListItemDto>>(
            $"/api/memberships?personId={personId}", JsonOptions);

        var row = Assert.Single(rows!);
        Assert.Equal(membership.Id, row.Id);
    }

    [Fact]
    public async Task GetMembershipById_UnknownId_ReturnsNotFound()
    {
        await AuthenticateAsAdminAsync();

        var response = await _client.GetAsync("/api/memberships/999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private sealed record LoginResponseDto(string AccessToken, string RefreshToken, DateTime RefreshTokenExpiresAt);

    private sealed record MembershipListItemDto(
        int Id, int PersonId, string FullName, string MembershipNumber, string MembershipType,
        string Status, DateTime? JoinDate, DateTime? RenewalDate, bool IsActive);

    private sealed record MembershipDetailDto(
        int Id, int PersonId, string FullName, string? Email, string? ContactNumber,
        string MembershipNumber, string MembershipType, string Status,
        DateTime? JoinDate, DateTime? RenewalDate, DateTime? ExitDate,
        string? ReferredBy, string? Notes, bool IsActive);

    private sealed record ProblemDto(string Title, string Detail, int Status);
}
