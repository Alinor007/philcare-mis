namespace philcare.Api.Features.Zakat.UpdateZakatEligibility;

public sealed record UpdateZakatEligibilityRequest(
    string AsnafCategory,
    decimal? MonthlyIncomePhp,
    int? HouseholdSize,
    DateTime AssessmentDate,
    string? AssessedBy,
    string? AssessmentNotes,
    string? Notes);

public sealed record UpdateZakatEligibilityResponse(int Id, int ParticipantId, string AsnafCategory, string Status);
