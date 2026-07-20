using FluentValidation;
using philcare.Api.Features.Finance.Domain;

namespace philcare.Api.Features.Finance.Donations.CreateDonation;

public sealed class CreateDonationValidator : AbstractValidator<CreateDonationRequest>
{
    public CreateDonationValidator()
    {
        RuleFor(x => x.DonorId).GreaterThan(0);
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.Currency).NotEmpty().MaximumLength(10);
        RuleFor(x => x.FundType).NotEmpty().MaximumLength(50);
        RuleFor(x => x.PaymentMethod).NotEmpty().MaximumLength(50);
        RuleFor(x => x.AdminRate).InclusiveBetween(0, FinanceRules.MaxAdminRate)
            .WithMessage($"Admin rate cannot exceed {FinanceRules.MaxAdminRate:P1}.");
        RuleFor(x => x.AmilRate).InclusiveBetween(0, FinanceRules.MaxAmilRate)
            .WithMessage($"Amil rate cannot exceed {FinanceRules.MaxAmilRate:P1}.");
        RuleFor(x => x.AmilRate)
            .Equal(0)
            .When(x => !string.Equals(x.FundType, FinanceRules.ZakatFundType, StringComparison.OrdinalIgnoreCase))
            .WithMessage("Amil rate is only allowed for zakat donations.");
        RuleFor(x => x.Reference).MaximumLength(100);
        RuleFor(x => x.Notes).MaximumLength(1000);
    }
}
