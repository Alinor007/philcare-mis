using FluentValidation;

namespace philcare.Api.Features.Finance.Expenses.CreateExpense;

public sealed class CreateExpenseValidator : AbstractValidator<CreateExpenseRequest>
{
    public CreateExpenseValidator()
    {
        RuleFor(x => x.PayeeVendor).NotEmpty().MaximumLength(200);
        RuleFor(x => x.ExpenseCategory).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(1000);
        RuleFor(x => x.ProgramOrProject).MaximumLength(200);
        RuleFor(x => x.PaymentMethod).NotEmpty().MaximumLength(50);
        RuleFor(x => x.AmountOriginal).GreaterThan(0);
        RuleFor(x => x.Currency).NotEmpty().MaximumLength(10);
        RuleFor(x => x.FxRateToPhp).GreaterThan(0);
        RuleFor(x => x.ReceiptNo).MaximumLength(100);
        RuleFor(x => x.ApprovedByPersonId).GreaterThan(0).When(x => x.ApprovedByPersonId is not null);
        RuleFor(x => x.SupportingDocStatus).MaximumLength(50);
        RuleFor(x => x.ExpenseFunction).MaximumLength(100);
        RuleFor(x => x.FundingBucketCode).NotEmpty().MaximumLength(20);
        RuleFor(x => x.ZakatAsnaf).MaximumLength(50);
        RuleFor(x => x.BeneficiaryType).MaximumLength(100);
        RuleFor(x => x.Notes).MaximumLength(1000);
        RuleFor(x => x.BeneficiaryCount).GreaterThan(0).When(x => x.BeneficiaryCount is not null);
    }
}
