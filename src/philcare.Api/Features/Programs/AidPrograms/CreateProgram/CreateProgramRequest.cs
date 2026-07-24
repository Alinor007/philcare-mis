namespace philcare.Api.Features.Programs.AidPrograms.CreateProgram;

public sealed record CreateProgramRequest(string Name, string Category, string? OwnerDepartment, string? Notes);

public sealed record CreateProgramResponse(
    int Id, string Name, string Category, string? OwnerDepartment, string Status, string? Notes, bool IsActive);
