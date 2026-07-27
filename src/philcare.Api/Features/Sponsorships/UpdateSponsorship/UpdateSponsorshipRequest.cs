namespace philcare.Api.Features.Sponsorships.UpdateSponsorship;

public sealed record UpdateSponsorshipRequest(
    string SponsorshipType,
    decimal MonthlyAmountPhp,
    string? CaseWorker,
    DateTime? NextReviewDate,
    string? Notes);

public sealed record UpdateSponsorshipResponse(
    int Id, int DonorId, int ParticipantId, string SponsorshipType, decimal MonthlyAmountPhp, string Status);
