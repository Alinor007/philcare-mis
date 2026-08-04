using FluentValidation;

namespace philcare.Api.Features.Finance.OtherIncomes.CreateOtherIncome;

public sealed class CreateOtherIncomeValidator : AbstractValidator<CreateOtherIncomeRequest>
{
    public CreateOtherIncomeValidator()
    {
        RuleFor(x => x.IncomeType).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Source).NotEmpty().MaximumLength(200);
        RuleFor(x => x.AmountOriginal).GreaterThan(0);
        RuleFor(x => x.Currency).NotEmpty().MaximumLength(10);
        RuleFor(x => x.FxRateToPhp).GreaterThan(0);
        RuleFor(x => x.FundingBucketCode).NotEmpty().MaximumLength(20);
        RuleFor(x => x.ReceiptNo).MaximumLength(100);
        RuleFor(x => x.EvidenceLink).MaximumLength(500);
        RuleFor(x => x.Notes).MaximumLength(1000);
    }
}
