namespace philcare.Api.Features.People.Memberships.CreateMembership;

public sealed record CreateMembershipRequest(
    int PersonId,
    string MembershipNumber,
    string MembershipType,
    DateTime? JoinDate,
    DateTime? RenewalDate,
    string? ReferredBy,
    string? Notes);

public sealed record CreateMembershipResponse(
    int Id,
    int PersonId,
    string FullName,
    string MembershipNumber,
    string MembershipType,
    string Status,
    DateTime? JoinDate,
    DateTime? RenewalDate,
    bool IsActive);
