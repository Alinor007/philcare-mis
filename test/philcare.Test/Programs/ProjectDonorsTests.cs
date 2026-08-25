using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using philcare.Api.Features.Finance.Donors.CreateDonor;
using philcare.Api.Features.Programs.AidPrograms.CreateProgram;
using philcare.Api.Features.Programs.Projects.CreateProject;
using philcare.Test.Common;
using Xunit;

namespace philcare.Test.Programs;

public class ProjectDonorsTests : IClassFixture<TestWebAppFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly HttpClient _client;

    public ProjectDonorsTests(TestWebAppFactory factory)
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

    private async Task<int> CreateProjectAsync()
    {
        var programResponse = await _client.PostAsJsonAsync("/api/programs", new
        {
            Name = $"Program-{Guid.NewGuid():N}",
            Category = "RELIEF"
        });
        programResponse.EnsureSuccessStatusCode();
        var program = await programResponse.Content.ReadFromJsonAsync<CreateProgramResponse>(JsonOptions);

        var projectResponse = await _client.PostAsJsonAsync("/api/projects", new
        {
            ProgramId = program!.Id,
            Name = $"Project-{Guid.NewGuid():N}",
            FundType = "PARTNER_CONTRIBUTION",
            TotalBudget = 100000m
        });
        projectResponse.EnsureSuccessStatusCode();
        var project = await projectResponse.Content.ReadFromJsonAsync<CreateProjectResponse>(JsonOptions);
        return project!.Id;
    }

    private async Task<int> CreateDonorAsync(bool isActive = true)
    {
        var response = await _client.PostAsJsonAsync("/api/donors", new
        {
            Name = $"Donor-{Guid.NewGuid():N}",
            Type = "Individual",
            RiskRating = "Low",
            PepFlag = false,
            PrivacyConsent = true
        });
        response.EnsureSuccessStatusCode();
        var donor = await response.Content.ReadFromJsonAsync<CreateDonorResponse>(JsonOptions);

        if (!isActive)
        {
            var deactivate = await _client.PutAsJsonAsync($"/api/donors/{donor!.Id}", new
            {
                Name = donor.Name,
                Type = "Individual",
                RiskRating = "Low",
                PepFlag = false,
                PrivacyConsent = true,
                IsActive = false,
                KydStatus = "Pending"
            });
            deactivate.EnsureSuccessStatusCode();
        }

        return donor!.Id;
    }

    [Fact]
    public async Task CreateProject_HasFundTypeAndNoDonorsInitially()
    {
        await AuthenticateAsAdminAsync();
        var projectId = await CreateProjectAsync();

        var response = await _client.GetAsync($"/api/projects/{projectId}");
        response.EnsureSuccessStatusCode();
        var project = await response.Content.ReadFromJsonAsync<ProjectDetailDto>(JsonOptions);

        Assert.Equal("PARTNER_CONTRIBUTION", project!.FundType);
        Assert.Equal(0, project.DonorCount);
    }

    [Fact]
    public async Task AddProjectDonor_MultipleDonors_AllLinked()
    {
        await AuthenticateAsAdminAsync();
        var projectId = await CreateProjectAsync();
        var donor1 = await CreateDonorAsync();
        var donor2 = await CreateDonorAsync();

        var add1 = await _client.PostAsJsonAsync($"/api/projects/{projectId}/donors", new { DonorId = donor1 });
        Assert.Equal(HttpStatusCode.Created, add1.StatusCode);

        var add2 = await _client.PostAsJsonAsync($"/api/projects/{projectId}/donors", new { DonorId = donor2 });
        Assert.Equal(HttpStatusCode.Created, add2.StatusCode);

        var listResponse = await _client.GetAsync($"/api/projects/{projectId}/donors");
        listResponse.EnsureSuccessStatusCode();
        var donors = await listResponse.Content.ReadFromJsonAsync<List<ProjectDonorRowDto>>(JsonOptions);

        Assert.Equal(2, donors!.Count);
        Assert.Contains(donors, d => d.DonorId == donor1);
        Assert.Contains(donors, d => d.DonorId == donor2);

        var detail = await (await _client.GetAsync($"/api/projects/{projectId}")).Content.ReadFromJsonAsync<ProjectDetailDto>(JsonOptions);
        Assert.Equal(2, detail!.DonorCount);
    }

    [Fact]
    public async Task AddProjectDonor_AlreadyLinked_ReturnsConflict()
    {
        await AuthenticateAsAdminAsync();
        var projectId = await CreateProjectAsync();
        var donorId = await CreateDonorAsync();

        (await _client.PostAsJsonAsync($"/api/projects/{projectId}/donors", new { DonorId = donorId })).EnsureSuccessStatusCode();

        var duplicate = await _client.PostAsJsonAsync($"/api/projects/{projectId}/donors", new { DonorId = donorId });

        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        var problem = await duplicate.Content.ReadFromJsonAsync<ProblemDetailsDto>(JsonOptions);
        Assert.Equal("ProjectDonors.AlreadyLinked", problem!.Title);
    }

    [Fact]
    public async Task AddProjectDonor_InactiveDonor_ReturnsBadRequest()
    {
        await AuthenticateAsAdminAsync();
        var projectId = await CreateProjectAsync();
        var donorId = await CreateDonorAsync(isActive: false);

        var response = await _client.PostAsJsonAsync($"/api/projects/{projectId}/donors", new { DonorId = donorId });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsDto>(JsonOptions);
        Assert.Equal("ProjectDonors.DonorInactive", problem!.Title);
    }

    [Fact]
    public async Task RemoveProjectDonor_ThenReAdd_Succeeds()
    {
        await AuthenticateAsAdminAsync();
        var projectId = await CreateProjectAsync();
        var donorId = await CreateDonorAsync();

        (await _client.PostAsJsonAsync($"/api/projects/{projectId}/donors", new { DonorId = donorId })).EnsureSuccessStatusCode();

        var removeResponse = await _client.DeleteAsync($"/api/projects/{projectId}/donors/{donorId}");
        Assert.Equal(HttpStatusCode.NoContent, removeResponse.StatusCode);

        var listAfterRemove = await (await _client.GetAsync($"/api/projects/{projectId}/donors"))
            .Content.ReadFromJsonAsync<List<ProjectDonorRowDto>>(JsonOptions);
        Assert.Empty(listAfterRemove!);

        // Hard delete, not soft — a plain link with no history worth preserving — so re-adding is
        // an ordinary insert, not a reactivation.
        var reAdd = await _client.PostAsJsonAsync($"/api/projects/{projectId}/donors", new { DonorId = donorId });
        Assert.Equal(HttpStatusCode.Created, reAdd.StatusCode);
    }

    [Fact]
    public async Task RemoveProjectDonor_NotLinked_ReturnsNotFound()
    {
        await AuthenticateAsAdminAsync();
        var projectId = await CreateProjectAsync();
        var donorId = await CreateDonorAsync();

        var response = await _client.DeleteAsync($"/api/projects/{projectId}/donors/{donorId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private sealed record LoginResponseDto(string AccessToken, string RefreshToken, DateTime RefreshTokenExpiresAt);

    private sealed record ProblemDetailsDto(string Title, string Detail);

    private sealed record ProjectDetailDto(int Id, string? FundType, int DonorCount);

    private sealed record ProjectDonorRowDto(int DonorId, string DonorName, string DonorType, bool IsActive);
}
