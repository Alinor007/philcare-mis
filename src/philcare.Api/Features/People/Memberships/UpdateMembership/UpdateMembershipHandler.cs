using Microsoft.EntityFrameworkCore;
using philcare.Api.Common.Domain;
using philcare.Api.Common.Persistence;
using philcare.Api.Features.ReferenceData.Domain;

namespace philcare.Api.Features.People.Memberships.UpdateMembership;

public sealed class UpdateMembershipHandler(AppDbContext db)
{
    public async Task<Result<UpdateMembershipResponse>> HandleAsync(int id, UpdateMembershipRequest request, CancellationToken cancellationToken)
    {
        var membership = await db.Memberships
            .Include(m => m.Person)
            .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);

        if (membership is null)
        {
            return Result.Failure<UpdateMembershipResponse>(Error.NotFound("Memberships.NotFound", "Membership not found."));
        }

        // Same lookup checks as create — an edit must not be a way around them.
        var membershipTypeExists = await db.LookupItems.AnyAsync(
            l => l.Category == LookupCategory.MembershipType && l.Code == request.MembershipType && l.IsActive,
            cancellationToken);

        if (!membershipTypeExists)
        {
            return Result.Failure<UpdateMembershipResponse>(
                Error.Validation("Memberships.InvalidMembershipType", "Membership type is not a recognised value."));
        }

        var statusExists = await db.LookupItems.AnyAsync(
            l => l.Category == LookupCategory.MembershipStatus && l.Code == request.Status && l.IsActive,
            cancellationToken);

        if (!statusExists)
        {
            return Result.Failure<UpdateMembershipResponse>(
                Error.Validation("Memberships.InvalidStatus", "Membership status is not a recognised value."));
        }

        if (request.MembershipNumber != membership.MembershipNumber)
        {
            var numberTaken = await db.Memberships.AnyAsync(
                m => m.MembershipNumber == request.MembershipNumber && m.Id != id, cancellationToken);

            if (numberTaken)
            {
                return Result.Failure<UpdateMembershipResponse>(
                    Error.Conflict("Memberships.DuplicateNumber", "That membership number is already in use."));
            }
        }

        membership.MembershipNumber = request.MembershipNumber;
        membership.MembershipType = request.MembershipType;
        membership.Status = request.Status;
        membership.JoinDate = request.JoinDate;
        membership.RenewalDate = request.RenewalDate;
        membership.ExitDate = request.ExitDate;
        membership.ReferredBy = request.ReferredBy;
        membership.Notes = request.Notes;
        membership.IsActive = request.IsActive;

        await db.SaveChangesAsync(cancellationToken);

        return Result.Success(new UpdateMembershipResponse(
            membership.Id, membership.PersonId, membership.Person.FullName, membership.MembershipNumber,
            membership.MembershipType, membership.Status, membership.JoinDate, membership.RenewalDate,
            membership.ExitDate, membership.IsActive));
    }
}
