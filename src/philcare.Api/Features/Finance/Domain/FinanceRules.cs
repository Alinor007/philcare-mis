namespace philcare.Api.Features.Finance.Domain;

public static class FinanceRules
{
    public const decimal MaxAdminRate = 0.15m;
    public const decimal MaxAmilRate = 0.125m;

    // Matches the seeded lookup code for the zakat fund type (category "fund_type").
    public const string ZakatFundType = "ZAKAT";
}
