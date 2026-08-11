namespace philcare.Api.Features.Programs.Projects.ChangeProjectStatus;

public sealed record ChangeProjectStatusRequest(string Status);

public sealed record ChangeProjectStatusResponse(int Id, string ImplementationStatus, DateTime? ClosedAt);
