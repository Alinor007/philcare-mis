namespace philcare.Api.Features.Zakat.CreateZakatEligibility;

public sealed record CreateZakatEligibilityRequest(
    int BeneficiaryId,
    string AsnafCategory,
    decimal? MonthlyIncomePhp,
    int? HouseholdSize,
    DateTime AssessmentDate,
    string? AssessedBy,
    string? AssessmentNotes,
    string? Notes);

public sealed record CreateZakatEligibilityResponse(int Id, int BeneficiaryId, string AsnafCategory, string Status);
