namespace philcare.Api.Features.ReferenceData.CreateLookup;

public sealed record CreateLookupRequest(string Category, string Code, string Label, int SortOrder);

public sealed record CreateLookupResponse(int Id, string Category, string Code, string Label, int SortOrder, bool IsActive);
