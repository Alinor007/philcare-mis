namespace philcare.Api.Features.Programs.ProjectDonors.AddProjectDonor;

public sealed record AddProjectDonorRequest(int DonorId);

public sealed record AddProjectDonorResponse(int Id, int ProjectId, int DonorId, string DonorName);
