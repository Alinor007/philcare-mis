using Microsoft.EntityFrameworkCore;
using philcare.Api.Common.Domain;
using philcare.Api.Common.Persistence;
using philcare.Api.Features.People.Domain;
using philcare.Api.Features.ReferenceData.Domain;

namespace philcare.Api.Features.People.Memberships.CreateMembership;

public sealed class CreateMembershipHandler(AppDbContext db)
{
    public async Task<Result<CreateMembershipResponse>> HandleAsync(CreateMembershipRequest request, CancellationToken cancellationToken)
    {
        var person = await db.GovernancePeople.FirstOrDefaultAsync(p => p.Id == request.PersonId, cancellationToken);

        if (person is null)
        {
            return Result.Failure<CreateMembershipResponse>(Error.NotFound("Memberships.PersonNotFound", "Person not found."));
        }

        if (!person.IsActive)
        {
            return Result.Failure<CreateMembershipResponse>(
                Error.Validation("Memberships.PersonInactive", "Cannot register a membership for an inactive person."));
        }

        var membershipTypeExists = await db.LookupItems.AnyAsync(
            l => l.Category == LookupCategory.MembershipType && l.Code == request.MembershipType && l.IsActive,
            cancellationToken);

        if (!membershipTypeExists)
        {
            return Result.Failure<CreateMembershipResponse>(
                Error.Validation("Memberships.InvalidMembershipType", "Membership type is not a recognised value."));
        }

        // The number is the org's own identifier and must be unique across the whole roll — the
        // unique index enforces it; this check exists to return a clean conflict instead of a 500.
        var numberTaken = await db.Memberships.AnyAsync(
            m => m.MembershipNumber == request.MembershipNumber, cancellationToken);

        if (numberTaken)
        {
            return Result.Failure<CreateMembershipResponse>(
                Error.Conflict("Memberships.DuplicateNumber", "That membership number is already in use."));
        }

        // Deliberately NOT one-per-person: a lapsed membership renewed under a new number is a
        // second row, so the roll keeps its history. Only one may be live at a time, though.
        var hasLiveMembership = await db.Memberships.AnyAsync(
            m => m.PersonId == request.PersonId && m.IsActive, cancellationToken);

        if (hasLiveMembership)
        {
            return Result.Failure<CreateMembershipResponse>(
                Error.Conflict("Memberships.AlreadyMember", "This person already holds a live membership."));
        }

        var membership = new Membership
        {
            PersonId = request.PersonId,
            MembershipNumber = request.MembershipNumber,
            MembershipType = request.MembershipType,
            Status = "ACTIVE",
            JoinDate = request.JoinDate,
            RenewalDate = request.RenewalDate,
            ReferredBy = request.ReferredBy,
            Notes = request.Notes,
            IsActive = true
        };

        db.Memberships.Add(membership);
        await db.SaveChangesAsync(cancellationToken);

        return Result.Success(new CreateMembershipResponse(
            membership.Id, membership.PersonId, person.FullName, membership.MembershipNumber,
            membership.MembershipType, membership.Status, membership.JoinDate, membership.RenewalDate, membership.IsActive));
    }
}
