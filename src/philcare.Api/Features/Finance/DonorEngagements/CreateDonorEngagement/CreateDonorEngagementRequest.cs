namespace philcare.Api.Features.Finance.DonorEngagements.CreateDonorEngagement;

public sealed record CreateDonorEngagementRequest(
    int DonorId,
    string EngagementType,
    DateTime EngagementDate,
    string Subject,
    string? Notes,
    bool FollowUpRequired,
    DateTime? FollowUpDate);

public sealed record CreateDonorEngagementResponse(
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
