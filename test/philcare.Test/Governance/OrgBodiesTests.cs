using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using philcare.Api.Features.Governance.OrgBodies.CreateOrgBody;
using philcare.Test.Common;
using Xunit;

namespace philcare.Test.Governance;

public class OrgBodiesTests : IClassFixture<TestWebAppFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly HttpClient _client;

    public OrgBodiesTests(TestWebAppFactory factory)
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

    private async Task<int> CreateBodyAsync(string? name = null, int? parentBodyId = null)
    {
        var response = await _client.PostAsJsonAsync("/api/governance/bodies", new
        {
            Name = name ?? $"Body-{Guid.NewGuid():N}",
            BodyType = "GOVERNANCE",
            ParentBodyId = parentBodyId
        });
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<CreateOrgBodyResponse>(JsonOptions);
        return body!.Id;
    }

    [Fact]
    public async Task CreateOrgBody_ValidRequest_ReturnsCreated()
    {
        await AuthenticateAsAdminAsync();

        var response = await _client.PostAsJsonAsync("/api/governance/bodies", new
        {
            Name = $"Board of Trustees-{Guid.NewGuid():N}",
            BodyType = "GOVERNANCE",
            QuorumRule = "50% + 1",
            DecisionThreshold = "Simple majority; 75% for strategic decisions"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task CreateOrgBody_DuplicateName_ReturnsConflict()
    {
        await AuthenticateAsAdminAsync();
        var name = $"Duplicate-Body-{Guid.NewGuid():N}";

        var first = await _client.PostAsJsonAsync("/api/governance/bodies", new { Name = name, BodyType = "GOVERNANCE" });
        first.EnsureSuccessStatusCode();

        var second = await _client.PostAsJsonAsync("/api/governance/bodies", new { Name = name, BodyType = "COMMITTEE" });

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task UpdateOrgBody_ParentToOwnDescendant_ReturnsCircularHierarchyError()
    {
        await AuthenticateAsAdminAsync();
        var parentId = await CreateBodyAsync();
        var childId = await CreateBodyAsync(parentBodyId: parentId);

        // Try to make the parent report to its own child — a direct cycle.
        var response = await _client.PutAsJsonAsync($"/api/governance/bodies/{parentId}", new
        {
            Name = $"Renamed-{Guid.NewGuid():N}",
            BodyType = "GOVERNANCE",
            ParentBodyId = childId,
            IsActive = true
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateOrgBody_SelfAsParent_ReturnsCircularHierarchyError()
    {
        await AuthenticateAsAdminAsync();
        var bodyId = await CreateBodyAsync();

        var response = await _client.PutAsJsonAsync($"/api/governance/bodies/{bodyId}", new
        {
            Name = $"Renamed-{Guid.NewGuid():N}",
            BodyType = "GOVERNANCE",
            ParentBodyId = bodyId,
            IsActive = true
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task DeactivateOrgBody_WithActiveChild_ReturnsConflict()
    {
        await AuthenticateAsAdminAsync();
        var parentId = await CreateBodyAsync();
        await CreateBodyAsync(parentBodyId: parentId);

        var response = await _client.DeleteAsync($"/api/governance/bodies/{parentId}");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task DeactivateOrgBody_WithNoChildrenOrAssignments_Succeeds()
    {
        await AuthenticateAsAdminAsync();
        var bodyId = await CreateBodyAsync();

        var response = await _client.DeleteAsync($"/api/governance/bodies/{bodyId}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task GetOrgBodyById_ReturnsChildBodiesAndMemberCount()
    {
        await AuthenticateAsAdminAsync();
        var parentId = await CreateBodyAsync();
        var childId = await CreateBodyAsync(parentBodyId: parentId);

        var response = await _client.GetAsync($"/api/governance/bodies/{parentId}");
        response.EnsureSuccessStatusCode();
        var detail = await response.Content.ReadFromJsonAsync<OrgBodyDetailDto>(JsonOptions);

        Assert.Contains(detail!.ChildBodies, c => c.Id == childId);
        Assert.Equal(0, detail.CurrentMemberCount);
    }

    private sealed record LoginResponseDto(string AccessToken, string RefreshToken, DateTime RefreshTokenExpiresAt);
    private sealed record ChildBodyDto(int Id, string Name, string BodyType);
    private sealed record OrgBodyDetailDto(
        int Id, string Name, string BodyType, int? ParentBodyId, string? ParentBodyName, string? QuorumRule,
        string? DecisionThreshold, string? MeetingFrequency, string? PolicyBasis, string? Notes, bool IsActive,
        int CurrentMemberCount, List<ChildBodyDto> ChildBodies);
}
