namespace philcare.Api.Infrastructure.Email;

public sealed record EmailOptions
{
    public const string SectionName = "Email";

    /// <summary>
    /// Master switch. False (the default when unconfigured) means the dispatcher idles instead of
    /// calling Resend — safe for environments with no API key. Rows queued while disabled stay
    /// Pending and drain automatically once this flips true, so nothing is lost by leaving it off.
    /// </summary>
    public bool Enabled { get; init; }

    /// <summary>Resend API key. Blank in appsettings.json; supplied via Email__ApiKey or user-secrets.</summary>
    public string ApiKey { get; init; } = string.Empty;

    public string FromAddress { get; init; } = "PhilCare MIS <onboarding@resend.dev>";
    public string? ReplyToAddress { get; init; }

    public string OrganizationName { get; init; } = "PhilCare";
    public string SupportEmail { get; init; } = "support@philcare.org";
    public string SupportPhone { get; init; } = "";

    public int PollIntervalSeconds { get; init; } = 15;
    public int MaxAttempts { get; init; } = 5;
}
