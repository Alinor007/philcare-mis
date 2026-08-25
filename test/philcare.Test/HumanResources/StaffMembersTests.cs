using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using philcare.Api.Features.Governance.People.CreatePerson;
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

    /// <summary>
    /// Identity now lives on Person — a staff profile is attached to one rather than carrying its
    /// own name. Every staff fixture starts here.
    /// </summary>
    private async Task<int> CreatePersonAsync(string? fullName = null)
    {
        var response = await _client.PostAsJsonAsync("/api/governance/people", new
        {
            FullName = fullName ?? $"Person-{Guid.NewGuid():N}",
            PersonCategory = "MEMBER",
            DefaultVotingRights = false
        });

        response.EnsureSuccessStatusCode();
        var person = await response.Content.ReadFromJsonAsync<CreatePersonResponse>(JsonOptions);
        return person!.Id;
    }

    private async Task<int> CreateStaffMemberAsync(string? fullName = null)
    {
        var personId = await CreatePersonAsync(fullName);

        var response = await _client.PostAsJsonAsync("/api/staff", new
        {
            PersonId = personId,
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
        var personId = await CreatePersonAsync();

        var response = await _client.PostAsJsonAsync("/api/staff", new
        {
            PersonId = personId,
            Position = "Finance Officer",
            Department = "ZAKAT_AND_DONATION_COLLECTION_DEPARTMENT",
            EmploymentType = "CONTRACT",
            HiredDate = new DateTime(2026, 3, 1)
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var staff = await response.Content.ReadFromJsonAsync<CreateStaffMemberResponse>(JsonOptions);

        Assert.Equal("CONTRACT", staff!.EmploymentType);
        Assert.Equal("ZAKAT_AND_DONATION_COLLECTION_DEPARTMENT", staff.Department);
        Assert.Equal(personId, staff.PersonId);
        Assert.True(staff.IsActive);
    }

    /// <summary>Hire dates are optional — the existing staff sheet has rows without one.</summary>
    [Fact]
    public async Task CreateStaffMember_WithoutHiredDate_Succeeds()
    {
        await AuthenticateAsAdminAsync();
        var personId = await CreatePersonAsync();

        var response = await _client.PostAsJsonAsync("/api/staff", new
        {
            PersonId = personId,
            Position = "Volunteer Coordinator",
            EmploymentType = "PART_TIME"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var staff = await response.Content.ReadFromJsonAsync<CreateStaffMemberResponse>(JsonOptions);
        Assert.Null(staff!.HiredDate);
    }

    [Fact]
    public async Task CreateStaffMember_UnknownPerson_ReturnsNotFound()
    {
        await AuthenticateAsAdminAsync();

        var response = await _client.PostAsJsonAsync("/api/staff", new
        {
            PersonId = 999999,
            Position = "Program Officer",
            EmploymentType = "FULL_TIME"
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDto>(JsonOptions);
        Assert.Equal("Staff.PersonNotFound", problem!.Title);
    }

    /// <summary>One employment profile per person — the unique index on PersonId, surfaced early.</summary>
    [Fact]
    public async Task CreateStaffMember_ForPersonWhoAlreadyHasProfile_ReturnsConflict()
    {
        await AuthenticateAsAdminAsync();
        var personId = await CreatePersonAsync();

        var first = await _client.PostAsJsonAsync("/api/staff", new
        {
            PersonId = personId,
            Position = "Program Officer",
            EmploymentType = "FULL_TIME"
        });
        first.EnsureSuccessStatusCode();

        var second = await _client.PostAsJsonAsync("/api/staff", new
        {
            PersonId = personId,
            Position = "Finance Officer",
            EmploymentType = "CONTRACT"
        });

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        var problem = await second.Content.ReadFromJsonAsync<ProblemDto>(JsonOptions);
        Assert.Equal("Staff.AlreadyStaff", problem!.Title);
    }

    [Fact]
    public async Task CreateStaffMember_SupervisorIsSelf_ReturnsBadRequest()
    {
        await AuthenticateAsAdminAsync();
        var personId = await CreatePersonAsync();

        var response = await _client.PostAsJsonAsync("/api/staff", new
        {
            PersonId = personId,
            Position = "Executive Director",
            EmploymentType = "FULL_TIME",
            SupervisorPersonId = personId
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDto>(JsonOptions);
        Assert.Equal("Staff.CannotSuperviseSelf", problem!.Title);
    }

    [Fact]
    public async Task CreateStaffMember_WithSupervisor_ExposesSupervisorOnDetail()
    {
        await AuthenticateAsAdminAsync();
        var supervisorPersonId = await CreatePersonAsync("Supervising Manager");
        var personId = await CreatePersonAsync();

        var response = await _client.PostAsJsonAsync("/api/staff", new
        {
            PersonId = personId,
            Position = "Program Officer",
            EmploymentType = "FULL_TIME",
            SupervisorPersonId = supervisorPersonId
        });
        response.EnsureSuccessStatusCode();
        var staff = await response.Content.ReadFromJsonAsync<CreateStaffMemberResponse>(JsonOptions);

        var detail = await _client.GetFromJsonAsync<StaffDetailDto>($"/api/staff/{staff!.Id}", JsonOptions);

        Assert.Equal(supervisorPersonId, detail!.SupervisorPersonId);
        Assert.Equal("Supervising Manager", detail.SupervisorName);
    }

    [Fact]
    public async Task CreateStaffMember_UnknownEmploymentType_ReturnsBadRequest()
    {
        await AuthenticateAsAdminAsync();
        var personId = await CreatePersonAsync();

        var response = await _client.PostAsJsonAsync("/api/staff", new
        {
            PersonId = personId,
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
        var personId = await CreatePersonAsync();

        var response = await _client.PostAsJsonAsync("/api/staff", new
        {
            PersonId = personId,
            Position = "Program Officer",
            Department = "NOT_A_REAL_DEPARTMENT",
            EmploymentType = "FULL_TIME"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDto>(JsonOptions);
        Assert.Equal("Staff.InvalidDepartment", problem!.Title);
    }

    /// <summary>
    /// Two employees can legitimately share a name — the constraint is one staff profile per
    /// Person, not one per name. Two distinct Persons sharing a name each get their own profile.
    /// </summary>
    [Fact]
    public async Task CreateStaffMember_DuplicateFullName_Succeeds()
    {
        await AuthenticateAsAdminAsync();
        var sharedName = $"Juan Dela Cruz {Guid.NewGuid():N}";

        await CreateStaffMemberAsync(sharedName);
        var secondPersonId = await CreatePersonAsync(sharedName);

        var response = await _client.PostAsJsonAsync("/api/staff", new
        {
            PersonId = secondPersonId,
            Position = "Field Officer",
            EmploymentType = "FULL_TIME"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task CreateStaffMember_WithoutAdminRole_ReturnsForbidden()
    {
        await AuthenticateAsAdminAsync();
        var personId = await CreatePersonAsync();

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
            PersonId = personId,
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
            Position = "Senior Program Officer",
            Department = "COMMUNITY_EMPOWERMENT_DEPARTMENT",
            EmploymentType = "SECONDED",
            HiredDate = new DateTime(2026, 1, 15),
            IsActive = true
        });

        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);

        var bad = await _client.PutAsJsonAsync($"/api/staff/{id}", new
        {
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

    /// <summary>Search matches the Person name even though it no longer lives on StaffMember.</summary>
    [Fact]
    public async Task GetStaffMembers_SearchByName_MatchesThroughPerson()
    {
        await AuthenticateAsAdminAsync();
        var uniqueName = $"Searchable {Guid.NewGuid():N}";
        var id = await CreateStaffMemberAsync(uniqueName);

        var rows = await _client.GetFromJsonAsync<List<StaffListItemDto>>(
            $"/api/staff?search={Uri.EscapeDataString(uniqueName)}", JsonOptions);

        var row = Assert.Single(rows!);
        Assert.Equal(id, row.Id);
        Assert.Equal(uniqueName, row.FullName);
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
        int Id, int PersonId, string FullName, string Position, string? Department, string EmploymentType,
        DateTime? HiredDate, bool IsActive);

    private sealed record StaffDetailDto(
        int Id, int PersonId, string FullName, string? Email, string? ContactNumber, string? PhotoUrl,
        string Position, string? Department, string EmploymentType, DateTime? HiredDate,
        int? SupervisorPersonId, string? SupervisorName, string? Notes, bool IsActive);

    private sealed record ProblemDto(string Title, string Detail, int Status);
}
