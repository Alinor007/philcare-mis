using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using philcare.Api.Features.Programs.Activities.CreateActivity;
using philcare.Api.Features.Programs.AidPrograms.CreateProgram;
using philcare.Api.Features.Programs.Distributions.CreateDistribution;
using philcare.Api.Features.Programs.Participants.CreateParticipant;
using philcare.Api.Features.Programs.Projects.CreateProject;
using philcare.Test.Common;
using Xunit;

namespace philcare.Test.Programs;

public class ProgramReportsTests : IClassFixture<TestWebAppFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly HttpClient _client;

    public ProgramReportsTests(TestWebAppFactory factory)
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
    public async Task GetProgramSummary_AggregatesProjectsActivitiesAndDistributions()
    {
        await AuthenticateAsAdminAsync();

        var programResponse = await _client.PostAsJsonAsync("/api/programs", new { Name = $"Program-{Guid.NewGuid():N}", Category = "RELIEF" });
        programResponse.EnsureSuccessStatusCode();
        var program = await programResponse.Content.ReadFromJsonAsync<CreateProgramResponse>(JsonOptions);

        var projectResponse = await _client.PostAsJsonAsync("/api/projects", new
        {
            ProgramId = program!.Id,
            Name = $"Project-{Guid.NewGuid():N}",
            TotalBudget = 50000m
        });
        projectResponse.EnsureSuccessStatusCode();
        var project = await projectResponse.Content.ReadFromJsonAsync<CreateProjectResponse>(JsonOptions);

        var activityResponse = await _client.PostAsJsonAsync("/api/activities", new
        {
            ProjectId = project!.Id,
            Name = $"Activity-{Guid.NewGuid():N}",
            ActivityType = "RELIEF_DISTRIBUTION",
            Budget = 20000m
        });
        activityResponse.EnsureSuccessStatusCode();
        var activity = await activityResponse.Content.ReadFromJsonAsync<CreateActivityResponse>(JsonOptions);

        var participantResponse = await _client.PostAsJsonAsync("/api/participants", new
        {
            FullName = $"Participant-{Guid.NewGuid():N}",
            ParticipantType = "BENEFICIARY",
            BeneficiaryType = "INDIVIDUAL",
            Gender = "Unspecified",
            ConsentOnFile = true
        });
        participantResponse.EnsureSuccessStatusCode();
        var participant = await participantResponse.Content.ReadFromJsonAsync<CreateParticipantResponse>(JsonOptions);

        var distributionResponse = await _client.PostAsJsonAsync("/api/distributions", new
        {
            DistributionType = "FOOD_PACK",
            ParticipantId = participant!.Id,
            ActivityId = activity!.Id,
            Quantity = 1,
            TotalValuePhp = 750m,
            DistributionDate = DateTime.UtcNow,
            FieldVerified = true,
            ReceivedConfirmation = true
        });
        distributionResponse.EnsureSuccessStatusCode();

        var reportResponse = await _client.GetAsync("/api/reports/program-summary");
        reportResponse.EnsureSuccessStatusCode();
        var rows = await reportResponse.Content.ReadFromJsonAsync<List<ProgramSummaryRowDto>>(JsonOptions);

        var row = rows!.Single(r => r.ProgramId == program.Id);
        Assert.Equal(1, row.ProjectCount);
        Assert.Equal(1, row.ActivityCount);
        Assert.Equal(50000m, row.TotalProjectBudget);
        Assert.Equal(20000m, row.TotalActivityBudget);
        Assert.Equal(750m, row.TotalDistributedValuePhp);
    }

    [Fact]
    public async Task GetDistributionSummary_AggregatesByTypeAndCountsDistinctParticipants()
    {
        await AuthenticateAsAdminAsync();
        var uniqueType = $"TYPE-{Guid.NewGuid():N}"[..20];

        var participant1Response = await _client.PostAsJsonAsync("/api/participants", new
        {
            FullName = $"Participant-{Guid.NewGuid():N}",
            ParticipantType = "BENEFICIARY",
            BeneficiaryType = "INDIVIDUAL",
            Gender = "Unspecified",
            ConsentOnFile = true
        });
        participant1Response.EnsureSuccessStatusCode();
        var participant1 = await participant1Response.Content.ReadFromJsonAsync<CreateParticipantResponse>(JsonOptions);

        var participant2Response = await _client.PostAsJsonAsync("/api/participants", new
        {
            FullName = $"Participant-{Guid.NewGuid():N}",
            ParticipantType = "BENEFICIARY",
            BeneficiaryType = "INDIVIDUAL",
            Gender = "Unspecified",
            ConsentOnFile = true
        });
        participant2Response.EnsureSuccessStatusCode();
        var participant2 = await participant2Response.Content.ReadFromJsonAsync<CreateParticipantResponse>(JsonOptions);

        foreach (var (participantId, value, qty) in new[] { (participant1!.Id, 100m, 2), (participant2!.Id, 150m, 3) })
        {
            var response = await _client.PostAsJsonAsync("/api/distributions", new
            {
                DistributionType = uniqueType,
                ParticipantId = participantId,
                Quantity = qty,
                TotalValuePhp = value,
                DistributionDate = DateTime.UtcNow,
                FieldVerified = false,
                ReceivedConfirmation = false
            });
            response.EnsureSuccessStatusCode();
        }

        var reportResponse = await _client.GetAsync("/api/reports/distribution-summary");
        reportResponse.EnsureSuccessStatusCode();
        var rows = await reportResponse.Content.ReadFromJsonAsync<List<DistributionSummaryRowDto>>(JsonOptions);

        var row = rows!.Single(r => r.DistributionType == uniqueType);
        Assert.Equal(2, row.DistributionCount);
        Assert.Equal(2, row.DistinctParticipants);
        Assert.Equal(5, row.TotalQuantity);
        Assert.Equal(250m, row.TotalValuePhp);
    }

    private sealed record LoginResponseDto(string AccessToken, string RefreshToken, DateTime RefreshTokenExpiresAt);

    private sealed record ProgramSummaryRowDto(
        int ProgramId, string ProgramName, int ProjectCount, int ActivityCount,
        decimal TotalProjectBudget, decimal TotalActivityBudget, decimal TotalDistributedValuePhp);

    private sealed record DistributionSummaryRowDto(string DistributionType, int DistributionCount, int DistinctParticipants, int TotalQuantity, decimal TotalValuePhp);
}
