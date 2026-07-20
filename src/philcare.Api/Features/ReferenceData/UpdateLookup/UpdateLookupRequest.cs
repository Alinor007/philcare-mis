namespace philcare.Api.Features.ReferenceData.UpdateLookup;

public sealed record UpdateLookupRequest(string Label, int SortOrder, bool IsActive);

public sealed record UpdateLookupResponse(int Id, string Category, string Code, string Label, int SortOrder, bool IsActive);
