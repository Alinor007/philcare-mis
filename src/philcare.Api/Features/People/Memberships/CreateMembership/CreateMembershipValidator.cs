using FluentValidation;

namespace philcare.Api.Features.People.Memberships.CreateMembership;

public sealed class CreateMembershipValidator : AbstractValidator<CreateMembershipRequest>
{
    public CreateMembershipValidator()
    {
        RuleFor(x => x.PersonId).GreaterThan(0);
        RuleFor(x => x.MembershipNumber).NotEmpty().MaximumLength(50);
        RuleFor(x => x.MembershipType).NotEmpty().MaximumLength(50);
        RuleFor(x => x.ReferredBy).MaximumLength(200);
        RuleFor(x => x.Notes).MaximumLength(1000);
    }
}
