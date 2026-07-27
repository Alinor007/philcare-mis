using philcare.Api.Features.Sponsorships.Domain;

namespace philcare.Api.Features.Sponsorships.ChangeSponsorshipStatus;

public sealed record ChangeSponsorshipStatusRequest(SponsorshipStatus Status, DateTime? EndDate);

public sealed record ChangeSponsorshipStatusResponse(int Id, string Status, DateTime? EndDate);
