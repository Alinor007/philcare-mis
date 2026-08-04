using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using philcare.Api.Features.Governance.Assignments.CreateAssignment;
using philcare.Api.Features.Governance.OrgBodies.CreateOrgBody;
using philcare.Api.Features.Governance.People.CreatePerson;
using philcare.Api.Features.Governance.Roles.CreateGovernanceRole;
using philcare.Test.Common;
using Xunit;

namespace philcare.Test.Governance;

public class AssignmentsTests : IClassFixture<TestWebAppFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly HttpClient _client;

    public AssignmentsTests(TestWebAppFactory factory)
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

    private async Task<int> CreatePersonAsync()
    {
        var response = await _client.PostAsJsonAsync("/api/governance/people", new
        {
            FullName = $"Person-{Guid.NewGuid():N}",
            PersonCategory = "BOARD",
            DefaultVotingRights = true
        });
        response.EnsureSuccessStatusCode();
        var person = await response.Content.ReadFromJsonAsync<CreatePersonResponse>(JsonOptions);
        return person!.Id;
    }

    private async Task<int> CreateBodyAsync()
    {
        var response = await _client.PostAsJsonAsync("/api/governance/bodies", new { Name = $"Body-{Guid.NewGuid():N}", BodyType = "GOVERNANCE" });
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<CreateOrgBodyResponse>(JsonOptions);
        return body!.Id;
    }

    private async Task<int> CreateRoleAsync()
    {
        var response = await _client.PostAsJsonAsync("/api/governance/roles", new { Name = $"Role-{Guid.NewGuid():N}", RoleCategory = "BOARD" });
        response.EnsureSuccessStatusCode();
        var role = await response.Content.ReadFromJsonAsync<CreateGovernanceRoleResponse>(JsonOptions);
        return role!.Id;
    }

    [Fact]
    public async Task CreateAssignment_ValidRequest_StartsAsCurrent()
    {
        await AuthenticateAsAdminAsync();
        var personId = await CreatePersonAsync();
        var bodyId = await CreateBodyAsync();
        var roleId = await CreateRoleAsync();

        var response = await _client.PostAsJsonAsync("/api/governance/assignments", new
        {
            PersonId = personId,
            OrgBodyId = bodyId,
            GovernanceRoleId = roleId,
            StartDate = DateTime.UtcNow,
            IsPrimary = true,
            VotingRights = true,
            IsTemporary = false
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var assignment = await response.Content.ReadFromJsonAsync<CreateAssignmentResponse>(JsonOptions);
        Assert.Equal("Current", assignment!.Status);
    }

    [Fact]
    public async Task CreateAssignment_UnknownPerson_ReturnsNotFound()
    {
        await AuthenticateAsAdminAsync();
        var bodyId = await CreateBodyAsync();
        var roleId = await CreateRoleAsync();

        var response = await _client.PostAsJsonAsync("/api/governance/assignments", new
        {
            PersonId = 999999,
            OrgBodyId = bodyId,
            GovernanceRoleId = roleId,
            StartDate = DateTime.UtcNow,
            IsPrimary = false,
            VotingRights = false,
            IsTemporary = false
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateAssignment_SecondPrimaryForSamePerson_ReturnsConflict()
    {
        await AuthenticateAsAdminAsync();
        var personId = await CreatePersonAsync();
        var bodyId = await CreateBodyAsync();
        var roleId = await CreateRoleAsync();

        var first = await _client.PostAsJsonAsync("/api/governance/assignments", new
        {
            PersonId = personId,
            OrgBodyId = bodyId,
            GovernanceRoleId = roleId,
            StartDate = DateTime.UtcNow,
            IsPrimary = true,
            VotingRights = true,
            IsTemporary = false
        });
        first.EnsureSuccessStatusCode();

        var secondBodyId = await CreateBodyAsync();
        var second = await _client.PostAsJsonAsync("/api/governance/assignments", new
        {
            PersonId = personId,
            OrgBodyId = secondBodyId,
            GovernanceRoleId = roleId,
            StartDate = DateTime.UtcNow,
            IsPrimary = true,
            VotingRights = true,
            IsTemporary = false
        });

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task CreateAssignment_NonPrimaryDuplicatesAllowed()
    {
        await AuthenticateAsAdminAsync();
        var personId = await CreatePersonAsync();
        var bodyId = await CreateBodyAsync();
        var roleId = await CreateRoleAsync();

        var first = await _client.PostAsJsonAsync("/api/governance/assignments", new
        {
            PersonId = personId,
            OrgBodyId = bodyId,
            GovernanceRoleId = roleId,
            StartDate = DateTime.UtcNow,
            IsPrimary = false,
            VotingRights = true,
            IsTemporary = false
        });
        first.EnsureSuccessStatusCode();

        var secondBodyId = await CreateBodyAsync();
        var second = await _client.PostAsJsonAsync("/api/governance/assignments", new
        {
            PersonId = personId,
            OrgBodyId = secondBodyId,
            GovernanceRoleId = roleId,
            StartDate = DateTime.UtcNow,
            IsPrimary = false,
            VotingRights = true,
            IsTemporary = false
        });

        Assert.Equal(HttpStatusCode.Created, second.StatusCode);
    }

    [Fact]
    public async Task EndAssignment_ThenCreateNewPrimary_Succeeds()
    {
        await AuthenticateAsAdminAsync();
        var personId = await CreatePersonAsync();
        var bodyId = await CreateBodyAsync();
        var roleId = await CreateRoleAsync();

        var createResponse = await _client.PostAsJsonAsync("/api/governance/assignments", new
        {
            PersonId = personId,
            OrgBodyId = bodyId,
            GovernanceRoleId = roleId,
            StartDate = DateTime.UtcNow,
            IsPrimary = true,
            VotingRights = true,
            IsTemporary = false
        });
        createResponse.EnsureSuccessStatusCode();
        var assignment = await createResponse.Content.ReadFromJsonAsync<CreateAssignmentResponse>(JsonOptions);

        var endResponse = await _client.PostAsJsonAsync($"/api/governance/assignments/{assignment!.Id}/end", new { });
        Assert.Equal(HttpStatusCode.OK, endResponse.StatusCode);

        var secondBodyId = await CreateBodyAsync();
        var newPrimaryResponse = await _client.PostAsJsonAsync("/api/governance/assignments", new
        {
            PersonId = personId,
            OrgBodyId = secondBodyId,
            GovernanceRoleId = roleId,
            StartDate = DateTime.UtcNow,
            IsPrimary = true,
            VotingRights = true,
            IsTemporary = false
        });

        Assert.Equal(HttpStatusCode.Created, newPrimaryResponse.StatusCode);
    }

    [Fact]
    public async Task EndAssignment_AlreadyEnded_ReturnsConflict()
    {
        await AuthenticateAsAdminAsync();
        var personId = await CreatePersonAsync();
        var bodyId = await CreateBodyAsync();
        var roleId = await CreateRoleAsync();

        var createResponse = await _client.PostAsJsonAsync("/api/governance/assignments", new
        {
            PersonId = personId,
            OrgBodyId = bodyId,
            GovernanceRoleId = roleId,
            StartDate = DateTime.UtcNow,
            IsPrimary = false,
            VotingRights = false,
            IsTemporary = false
        });
        createResponse.EnsureSuccessStatusCode();
        var assignment = await createResponse.Content.ReadFromJsonAsync<CreateAssignmentResponse>(JsonOptions);

        var firstEnd = await _client.PostAsJsonAsync($"/api/governance/assignments/{assignment!.Id}/end", new { });
        Assert.Equal(HttpStatusCode.OK, firstEnd.StatusCode);

        var secondEnd = await _client.PostAsJsonAsync($"/api/governance/assignments/{assignment.Id}/end", new { });
        Assert.Equal(HttpStatusCode.Conflict, secondEnd.StatusCode);
    }

    [Fact]
    public async Task GetOrgBodyMembers_ReflectsOnlyCurrentAssignments()
    {
        await AuthenticateAsAdminAsync();
        var personId = await CreatePersonAsync();
        var bodyId = await CreateBodyAsync();
        var roleId = await CreateRoleAsync();

        var createResponse = await _client.PostAsJsonAsync("/api/governance/assignments", new
        {
            PersonId = personId,
            OrgBodyId = bodyId,
            GovernanceRoleId = roleId,
            StartDate = DateTime.UtcNow,
            IsPrimary = true,
            VotingRights = true,
            IsTemporary = false
        });
        createResponse.EnsureSuccessStatusCode();
        var assignment = await createResponse.Content.ReadFromJsonAsync<CreateAssignmentResponse>(JsonOptions);

        var membersBeforeEnd = await _client.GetAsync($"/api/governance/bodies/{bodyId}/members");
        membersBeforeEnd.EnsureSuccessStatusCode();
        var beforeEnd = await membersBeforeEnd.Content.ReadFromJsonAsync<List<MemberRowDto>>(JsonOptions);
        Assert.Contains(beforeEnd!, m => m.PersonId == personId);

        var endResponse = await _client.PostAsJsonAsync($"/api/governance/assignments/{assignment!.Id}/end", new { });
        endResponse.EnsureSuccessStatusCode();

        var membersAfterEnd = await _client.GetAsync($"/api/governance/bodies/{bodyId}/members");
        membersAfterEnd.EnsureSuccessStatusCode();
        var afterEnd = await membersAfterEnd.Content.ReadFromJsonAsync<List<MemberRowDto>>(JsonOptions);
        Assert.DoesNotContain(afterEnd!, m => m.PersonId == personId);

        var membersIncludingFormer = await _client.GetAsync($"/api/governance/bodies/{bodyId}/members?includeFormer=true");
        membersIncludingFormer.EnsureSuccessStatusCode();
        var includingFormer = await membersIncludingFormer.Content.ReadFromJsonAsync<List<MemberRowDto>>(JsonOptions);
        Assert.Contains(includingFormer!, m => m.PersonId == personId);
    }

    [Fact]
    public async Task CreateAssignment_AsNonAdminProgramRole_ReturnsForbidden()
    {
        await AuthenticateAsAdminAsync();

        var programEmail = $"program-{Guid.NewGuid():N}@philcare.local";
        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", new { Email = programEmail, Password = "Program@12345", Role = "Program" });
        registerResponse.EnsureSuccessStatusCode();

        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new { Email = programEmail, Password = "Program@12345" });
        loginResponse.EnsureSuccessStatusCode();
        var loginBody = await loginResponse.Content.ReadFromJsonAsync<LoginResponseDto>(JsonOptions);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginBody!.AccessToken);

        var response = await _client.PostAsJsonAsync("/api/governance/assignments", new
        {
            PersonId = 1,
            OrgBodyId = 1,
            GovernanceRoleId = 1,
            StartDate = DateTime.UtcNow,
            IsPrimary = false,
            VotingRights = false,
            IsTemporary = false
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private sealed record LoginResponseDto(string AccessToken, string RefreshToken, DateTime RefreshTokenExpiresAt);
    private sealed record MemberRowDto(
        int PersonId, string PersonFullName, int AssignmentId, string RoleName, string? PositionTitle,
        bool IsPrimary, bool VotingRights, DateTime StartDate, DateTime? EndDate, string Status);
}
