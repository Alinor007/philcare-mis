using FluentValidation;

namespace philcare.Api.Features.People.Memberships.UpdateMembership;

public sealed class UpdateMembershipValidator : AbstractValidator<UpdateMembershipRequest>
{
    public UpdateMembershipValidator()
    {
        RuleFor(x => x.MembershipNumber).NotEmpty().MaximumLength(50);
        RuleFor(x => x.MembershipType).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Status).NotEmpty().MaximumLength(50);
        RuleFor(x => x.ReferredBy).MaximumLength(200);
        RuleFor(x => x.Notes).MaximumLength(1000);
    }
}
