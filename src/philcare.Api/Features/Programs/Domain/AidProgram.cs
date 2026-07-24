using philcare.Api.Common.Domain;

namespace philcare.Api.Features.Programs.Domain;

/// <summary>
/// Top-level program (e.g. Relief, Education, Health). Named AidProgram to avoid colliding
/// with the ASP.NET entry-point `Program` class used across the codebase (WebApplicationFactory&lt;Program&gt;, etc).
/// </summary>
public class AidProgram : Entity
{
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty; // lookup: program_category
    public string? OwnerDepartment { get; set; }
    public string Status { get; set; } = "Active";
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;

    public List<Project> Projects { get; set; } = [];
}
