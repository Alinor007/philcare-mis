namespace philcare.Api.Features.Programs.Activities.ChangeActivityStatus;

public sealed record ChangeActivityStatusRequest(
    string Status,
    int? ActualBeneficiaries,
    DateTime? ActualEndDate);

public sealed record ChangeActivityStatusResponse(
    int Id,
    string ImplementationStatus,
    int? ActualBeneficiaries,
    DateTime? ActualEndDate);
