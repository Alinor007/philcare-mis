using FluentValidation;

namespace philcare.Api.Features.Zakat.CreateZakatEligibility;

public sealed class CreateZakatEligibilityValidator : AbstractValidator<CreateZakatEligibilityRequest>
{
    public CreateZakatEligibilityValidator()
    {
        RuleFor(x => x.ParticipantId).GreaterThan(0);
        RuleFor(x => x.AsnafCategory).NotEmpty().MaximumLength(50);
        RuleFor(x => x.MonthlyIncomePhp).GreaterThanOrEqualTo(0).When(x => x.MonthlyIncomePhp is not null);
        RuleFor(x => x.HouseholdSize).GreaterThan(0).When(x => x.HouseholdSize is not null);
        RuleFor(x => x.AssessedBy).MaximumLength(100);
        RuleFor(x => x.AssessmentNotes).MaximumLength(2000);
        RuleFor(x => x.Notes).MaximumLength(1000);
    }
}
