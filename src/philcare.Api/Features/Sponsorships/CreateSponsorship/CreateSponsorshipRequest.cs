namespace philcare.Api.Features.Sponsorships.CreateSponsorship;

public sealed record CreateSponsorshipRequest(
    int DonorId,
    int ParticipantId,
    string SponsorshipType,
    decimal MonthlyAmountPhp,
    DateTime StartDate,
    string? CaseWorker,
    DateTime? NextReviewDate,
    string? Notes);

public sealed record CreateSponsorshipResponse(
    int Id, int DonorId, int ParticipantId, string SponsorshipType, decimal MonthlyAmountPhp, string Status);
