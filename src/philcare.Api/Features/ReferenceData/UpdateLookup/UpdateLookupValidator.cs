using FluentValidation;

namespace philcare.Api.Features.ReferenceData.UpdateLookup;

public sealed class UpdateLookupValidator : AbstractValidator<UpdateLookupRequest>
{
    public UpdateLookupValidator()
    {
        RuleFor(x => x.Label).NotEmpty().MaximumLength(200);
        RuleFor(x => x.SortOrder).GreaterThanOrEqualTo(0);
    }
}
