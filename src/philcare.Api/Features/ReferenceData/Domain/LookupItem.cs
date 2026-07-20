using philcare.Api.Common.Domain;

namespace philcare.Api.Features.ReferenceData.Domain;

public class LookupItem : Entity
{
    public string Category { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}
