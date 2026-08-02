namespace philcare.Api.Features.Finance.DonorEngagements.UpdateDonorEngagement;

// DonorId is intentionally excluded — the parent donor is immutable once an engagement is logged.
public sealed record UpdateDonorEngagementRequest(
    string EngagementType,
    DateTime EngagementDate,
    string Subject,
    string? Notes,
    bool FollowUpRequired,
    DateTime? FollowUpDate);

public sealed record UpdateDonorEngagementResponse(
    int Id,
    int DonorId,
    string EngagementType,
    DateTime EngagementDate,
    string Subject,
    string? Notes,
    bool FollowUpRequired,
    DateTime? FollowUpDate,
    string? CreatedBy,
    DateTime CreatedAt);
