namespace philcare.Api.Features.Attachments.Domain;

public sealed record PhotoStorageOptions
{
    public const string SectionName = "PhotoStorage";

    /// <summary>
    /// Absolute or content-root-relative folder the uploads live in. Deliberately NOT under a
    /// web root — this app serves no static files, and photos of beneficiaries must only ever be
    /// reachable through the authorized GET that re-applies the owning record's policy.
    /// </summary>
    public string RootPath { get; init; } = "App_Data/photos";

    public long MaxBytes { get; init; } = 5 * 1024 * 1024;
}
