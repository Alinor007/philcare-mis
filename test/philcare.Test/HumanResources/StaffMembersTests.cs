using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using philcare.Api.Features.HumanResources.Staff.CreateStaffMember;
using philcare.Test.Common;
using Xunit;

namespace philcare.Test.HumanResources;

public class StaffMembersTests : IClassFixture<TestWebAppFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly HttpClient _client;

    public StaffMembersTests(TestWebAppFactory factory)
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

    private async Task<int> CreateStaffMemberAsync(string? fullName = null)
    {
        var response = await _client.PostAsJsonAsync("/api/staff", new
        {
            FullName = fullName ?? $"Staff-{Guid.NewGuid():N}",
            Position = "Program Officer",
            Department = "HUMANITARIAN_PROGRAMS_DEPARTMENT",
            EmploymentType = "FULL_TIME",
            HiredDate = new DateTime(2026, 1, 15)
        });

        response.EnsureSuccessStatusCode();
        var staff = await response.Content.ReadFromJsonAsync<CreateStaffMemberResponse>(JsonOptions);
        return staff!.Id;
    }

    [Fact]
    public async Task CreateStaffMember_WithValidLookups_Succeeds()
    {
        await AuthenticateAsAdminAsync();

        var response = await _client.PostAsJsonAsync("/api/staff", new
        {
            FullName = $"Staff-{Guid.NewGuid():N}",
            Position = "Finance Officer",
            Department = "ZAKAT_AND_DONATION_COLLECTION_DEPARTMENT",
            EmploymentType = "CONTRACT",
            HiredDate = new DateTime(2026, 3, 1),
            Email = "officer@philcare.local",
            Phone = "09171234567"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var staff = await response.Content.ReadFromJsonAsync<CreateStaffMemberResponse>(JsonOptions);

        Assert.Equal("CONTRACT", staff!.EmploymentType);
        Assert.Equal("ZAKAT_AND_DONATION_COLLECTION_DEPARTMENT", staff.Department);
        Assert.True(staff.IsActive);
    }

    /// <summary>Hire dates are optional — the org's existing sheet has rows without one.</summary>
    [Fact]
    public async Task CreateStaffMember_WithoutHiredDate_Succeeds()
    {
        await AuthenticateAsAdminAsync();

        var response = await _client.PostAsJsonAsync("/api/staff", new
        {
            FullName = $"Staff-{Guid.NewGuid():N}",
            Position = "Volunteer Coordinator",
            EmploymentType = "PART_TIME"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var staff = await response.Content.ReadFromJsonAsync<CreateStaffMemberResponse>(JsonOptions);
        Assert.Null(staff!.HiredDate);
    }

    [Fact]
    public async Task CreateStaffMember_UnknownEmploymentType_ReturnsBadRequest()
    {
        await AuthenticateAsAdminAsync();

        var response = await _client.PostAsJsonAsync("/api/staff", new
        {
            FullName = $"Staff-{Guid.NewGuid():N}",
            Position = "Program Officer",
            EmploymentType = "NOT_A_REAL_TYPE"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDto>(JsonOptions);
        Assert.Equal("Staff.InvalidEmploymentType", problem!.Title);
    }

    [Fact]
    public async Task CreateStaffMember_UnknownDepartment_ReturnsBadRequest()
    {
        await AuthenticateAsAdminAsync();

        var response = await _client.PostAsJsonAsync("/api/staff", new
        {
            FullName = $"Staff-{Guid.NewGuid():N}",
            Position = "Program Officer",
            Department = "NOT_A_REAL_DEPARTMENT",
            EmploymentType = "FULL_TIME"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDto>(JsonOptions);
        Assert.Equal("Staff.InvalidDepartment", problem!.Title);
    }

    /// <summary>
    /// Two employees can legitimately share a name — unlike Partner, StaffMember.FullName is
    /// indexed but deliberately not unique.
    /// </summary>
    [Fact]
    public async Task CreateStaffMember_DuplicateFullName_Succeeds()
    {
        await AuthenticateAsAdminAsync();
        var sharedName = $"Juan Dela Cruz {Guid.NewGuid():N}";

        await CreateStaffMemberAsync(sharedName);

        var response = await _client.PostAsJsonAsync("/api/staff", new
        {
            FullName = sharedName,
            Position = "Field Officer",
            EmploymentType = "FULL_TIME"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task CreateStaffMember_WithoutAdminRole_ReturnsForbidden()
    {
        await AuthenticateAsAdminAsync();

        var email = $"viewer-{Guid.NewGuid():N}@philcare.local";
        var register = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            Email = email,
            Password = "Viewer@12345",
            Role = "Viewer",
            FullName = "Read Only"
        });
        register.EnsureSuccessStatusCode();

        var login = await _client.PostAsJsonAsync("/api/auth/login", new { Email = email, Password = "Viewer@12345" });
        login.EnsureSuccessStatusCode();
        var body = await login.Content.ReadFromJsonAsync<LoginResponseDto>(JsonOptions);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body!.AccessToken);

        var response = await _client.PostAsJsonAsync("/api/staff", new
        {
            FullName = $"Staff-{Guid.NewGuid():N}",
            Position = "Program Officer",
            EmploymentType = "FULL_TIME"
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UpdateStaffMember_ChangesFieldsAndValidatesLookups()
    {
        await AuthenticateAsAdminAsync();
        var id = await CreateStaffMemberAsync();

        var ok = await _client.PutAsJsonAsync($"/api/staff/{id}", new
        {
            FullName = "Updated Name",
            Position = "Senior Program Officer",
            Department = "COMMUNITY_EMPOWERMENT_DEPARTMENT",
            EmploymentType = "SECONDED",
            HiredDate = new DateTime(2026, 1, 15),
            IsActive = true
        });

        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);

        var bad = await _client.PutAsJsonAsync($"/api/staff/{id}", new
        {
            FullName = "Updated Name",
            Position = "Senior Program Officer",
            EmploymentType = "STILL_NOT_REAL",
            IsActive = true
        });

        Assert.Equal(HttpStatusCode.BadRequest, bad.StatusCode);
    }

    [Fact]
    public async Task GetStaffMembers_HidesDeactivatedUnlessRequested()
    {
        await AuthenticateAsAdminAsync();
        var id = await CreateStaffMemberAsync();

        var delete = await _client.DeleteAsync($"/api/staff/{id}");
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);

        var defaultList = await _client.GetFromJsonAsync<List<StaffListItemDto>>("/api/staff", JsonOptions);
        Assert.DoesNotContain(defaultList!, s => s.Id == id);

        var withInactive = await _client.GetFromJsonAsync<List<StaffListItemDto>>("/api/staff?includeInactive=true", JsonOptions);
        Assert.Contains(withInactive!, s => s.Id == id);
    }

    [Fact]
    public async Task GetStaffMemberById_UnknownId_ReturnsNotFound()
    {
        await AuthenticateAsAdminAsync();

        var response = await _client.GetAsync("/api/staff/999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetStaffMembers_FilteredByDepartment_ReturnsOnlyThatDepartment()
    {
        await AuthenticateAsAdminAsync();
        await CreateStaffMemberAsync(); // HUMANITARIAN_PROGRAMS_DEPARTMENT

        var rows = await _client.GetFromJsonAsync<List<StaffListItemDto>>(
            "/api/staff?department=HUMANITARIAN_PROGRAMS_DEPARTMENT", JsonOptions);

        Assert.NotEmpty(rows!);
        Assert.All(rows!, r => Assert.Equal("HUMANITARIAN_PROGRAMS_DEPARTMENT", r.Department));
    }

    private sealed record LoginResponseDto(string AccessToken, string RefreshToken, DateTime RefreshTokenExpiresAt);

    private sealed record StaffListItemDto(
        int Id, string FullName, string Position, string? Department, string EmploymentType, DateTime? HiredDate, bool IsActive);

    private sealed record ProblemDto(string Title, string Detail, int Status);
}
