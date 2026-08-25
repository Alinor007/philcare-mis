namespace philcare.Api.Features.People.Memberships.UpdateMembership;

/// <summary>No PersonId — a membership stays attached to the Person it was registered for.</summary>
public sealed record UpdateMembershipRequest(
    string MembershipNumber,
    string MembershipType,
    string Status,
    DateTime? JoinDate,
    DateTime? RenewalDate,
    DateTime? ExitDate,
    string? ReferredBy,
    string? Notes,
    bool IsActive);

public sealed record UpdateMembershipResponse(
    int Id,
    int PersonId,
    string FullName,
    string MembershipNumber,
    string MembershipType,
    string Status,
    DateTime? JoinDate,
    DateTime? RenewalDate,
    DateTime? ExitDate,
    bool IsActive);
