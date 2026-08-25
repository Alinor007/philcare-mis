using philcare.Api.Common.Domain;

namespace philcare.Api.Features.Attachments.Domain;

/// <summary>
/// Photo upload policy — allowed categories, accepted image types, and the filename rules that
/// keep a request from escaping the storage folder. A static class of policy, mirroring the
/// FinanceRules / ExpensePosting idiom; there is no Services/ folder in this codebase.
///
/// The category is part of the stored path (<c>beneficiary/{guid}.jpg</c>) rather than a new
/// table: it is the only thing the read endpoint needs in order to re-apply the right policy,
/// and encoding it in the path keeps this feature free of a schema change. The existing
/// <c>PhotoUrl</c> string columns hold the resulting relative URL unchanged.
/// </summary>
public static class PhotoRules
{
    public const string BeneficiaryCategory = "beneficiary";
    public const string StaffCategory = "staff";
    public const string VolunteerCategory = "volunteer";

    public static readonly string[] Categories = [BeneficiaryCategory, StaffCategory, VolunteerCategory];

    /// <summary>
    /// Extension per accepted content type. The upload is keyed off the sniffed magic bytes, not
    /// the client-supplied name, so a .exe renamed to .jpg never lands on disk.
    /// </summary>
    private static readonly Dictionary<string, string> ExtensionByContentType = new(StringComparer.OrdinalIgnoreCase)
    {
        ["image/jpeg"] = ".jpg",
        ["image/png"] = ".png",
        ["image/webp"] = ".webp"
    };

    public static bool IsKnownCategory(string? category) =>
        !string.IsNullOrWhiteSpace(category)
        && Categories.Contains(category, StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Identifies the image from its leading bytes and returns the extension to store it under.
    /// Returns null when the content is not one of the three formats we accept.
    /// </summary>
    public static string? ResolveExtension(ReadOnlySpan<byte> header)
    {
        // JPEG: FF D8 FF
        if (header.Length >= 3 && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF)
        {
            return ExtensionByContentType["image/jpeg"];
        }

        // PNG: 89 50 4E 47 0D 0A 1A 0A
        if (header.Length >= 8 && header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E
            && header[3] == 0x47 && header[4] == 0x0D && header[5] == 0x0A && header[6] == 0x1A && header[7] == 0x0A)
        {
            return ExtensionByContentType["image/png"];
        }

        // WEBP: "RIFF" .... "WEBP"
        if (header.Length >= 12
            && header[0] == 0x52 && header[1] == 0x49 && header[2] == 0x46 && header[3] == 0x46
            && header[8] == 0x57 && header[9] == 0x45 && header[10] == 0x42 && header[11] == 0x50)
        {
            return ExtensionByContentType["image/webp"];
        }

        return null;
    }

    public static string ContentTypeFor(string extension) => extension.ToLowerInvariant() switch
    {
        ".png" => "image/png",
        ".webp" => "image/webp",
        _ => "image/jpeg"
    };

    /// <summary>Server-generated name. The client never influences what lands on disk.</summary>
    public static string NewFileName(string extension) => $"{Guid.NewGuid():N}{extension}";

    /// <summary>
    /// Rejects anything that is not exactly a name this class generated. This is the guard that
    /// makes the read endpoint safe: no separators, no traversal, no absolute paths, no
    /// alternate data streams — a 32-char hex id and a known extension, nothing else.
    /// </summary>
    public static bool IsSafeFileName(string? fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return false;
        }

        var extension = Path.GetExtension(fileName);

        if (!ExtensionByContentType.ContainsValue(extension.ToLowerInvariant()))
        {
            return false;
        }

        var stem = Path.GetFileNameWithoutExtension(fileName);

        return stem.Length == 32 && stem.All(Uri.IsHexDigit);
    }

    public static Error TooLarge(long maxBytes) =>
        Error.Validation("Photos.TooLarge", $"The image must be {maxBytes / (1024 * 1024)} MB or smaller.");

    public static Error UnsupportedFormat() =>
        Error.Validation("Photos.UnsupportedFormat", "Only JPEG, PNG and WEBP images are accepted.");

    public static Error UnknownCategory() =>
        Error.Validation("Photos.UnknownCategory", "Unknown photo category.");
}
