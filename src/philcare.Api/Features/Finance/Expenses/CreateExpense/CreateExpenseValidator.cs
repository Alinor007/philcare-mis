using FluentValidation;

namespace philcare.Api.Features.Finance.Expenses.CreateExpense;

public sealed class CreateExpenseValidator : AbstractValidator<CreateExpenseRequest>
{
    public CreateExpenseValidator()
    {
        RuleFor(x => x.FundBucketId).GreaterThan(0);
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.ExpenseCategory).NotEmpty().MaximumLength(50);
        RuleFor(x => x.PaymentMethod).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(1000);
        RuleFor(x => x.Reference).MaximumLength(100);
        RuleFor(x => x.ZakatAsnaf).MaximumLength(50);
        RuleFor(x => x.BeneficiaryCount).GreaterThan(0).When(x => x.BeneficiaryCount is not null);
    }
}
