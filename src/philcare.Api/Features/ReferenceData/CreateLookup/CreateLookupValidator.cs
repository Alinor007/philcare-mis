using FluentValidation;

namespace philcare.Api.Features.ReferenceData.CreateLookup;

public sealed class CreateLookupValidator : AbstractValidator<CreateLookupRequest>
{
    public CreateLookupValidator()
    {
        RuleFor(x => x.Category).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Code).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Label).NotEmpty().MaximumLength(200);
        RuleFor(x => x.SortOrder).GreaterThanOrEqualTo(0);
    }
}
