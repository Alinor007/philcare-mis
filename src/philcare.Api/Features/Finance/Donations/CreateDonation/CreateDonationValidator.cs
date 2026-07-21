using FluentValidation;
using philcare.Api.Features.Finance.Domain;

namespace philcare.Api.Features.Finance.Donations.CreateDonation;

public sealed class CreateDonationValidator : AbstractValidator<CreateDonationRequest>
{
    public CreateDonationValidator()
    {
        RuleFor(x => x.DonorId).GreaterThan(0);
        RuleFor(x => x.AmountOriginal).GreaterThan(0);
        RuleFor(x => x.Currency).NotEmpty().MaximumLength(10);
        RuleFor(x => x.FxRateToPhp).GreaterThan(0);
        RuleFor(x => x.Channel).NotEmpty().MaximumLength(50);
        RuleFor(x => x.FundCode).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Purpose).MaximumLength(200);
        RuleFor(x => x.ProgramOrProject).MaximumLength(200);
        RuleFor(x => x.ReceiptNo).MaximumLength(100);
        RuleFor(x => x.CashDocumentationStatus).MaximumLength(50);
        RuleFor(x => x.Notes).MaximumLength(1000);

        // Sanity ceiling; the authoritative cap is the target admin bucket's own MaxAdminRate,
        // enforced by CreateDonationHandler once the fund/bucket is resolved.
        RuleFor(x => x.AdminRateInput).InclusiveBetween(0, FinanceRules.MaxAdminRate)
            .WithMessage($"Admin rate cannot exceed {FinanceRules.MaxAdminRate:P1}.");
    }
}
