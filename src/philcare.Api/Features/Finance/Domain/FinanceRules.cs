namespace philcare.Api.Features.Finance.Domain;

public static class FinanceRules
{
    public const decimal MaxAdminRate = 0.15m;
    public const decimal MaxAmilRate = 0.125m;

    public const string ZakatFundCode = "ZAKA-FUND";
    public const string ZakatProgramBucket = "ZAK-PROG";
    public const string ZakatAmilBucket = "ZAK-AMIL";

    // Expense category used for the Expense that CreateDistributionHandler posts alongside a
    // Distribution — a real expense_category lookup code (see lookup-seed.json).
    public const string DistributionExpenseCategory = "DISTRIBUTION";
}
