namespace philcare.Api.Features.Programs.AidPrograms.UpdateProgram;

public sealed record UpdateProgramRequest(string Name, string Category, string? OwnerDepartment, string Status, string? Notes, bool IsActive);

public sealed record UpdateProgramResponse(
    int Id, string Name, string Category, string? OwnerDepartment, string Status, string? Notes, bool IsActive);
