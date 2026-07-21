namespace philcare.Api.Features.Finance.Domain;

public static class FinanceRules
{
    public const decimal MaxAdminRate = 0.15m;
    public const decimal MaxAmilRate = 0.125m;

    public const string ZakatFundCode = "ZAKA-FUND";
    public const string ZakatProgramBucket = "ZAK-PROG";
    public const string ZakatAmilBucket = "ZAK-AMIL";
}
