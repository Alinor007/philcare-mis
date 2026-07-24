namespace philcare.Api.Features.Zakat.DecideZakatEligibility;

public sealed record DecideZakatEligibilityRequest(
    bool Approve, string? DecidedBy, DateTime? ValidUntil, string? RejectionReason);

public sealed record DecideZakatEligibilityResponse(int Id, string Status, DateTime? ValidUntil, string? RejectionReason);
