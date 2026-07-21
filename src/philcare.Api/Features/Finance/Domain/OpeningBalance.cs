using philcare.Api.Common.Domain;

namespace philcare.Api.Features.Finance.Domain;

/// <summary>
/// The opening balance carried into a reporting year for a given fund (e.g. a restricted
/// grant balance carried over from the prior year). Seeded once per fund per year.
/// </summary>
public class OpeningBalance : Entity
{
    public int Year { get; set; }

    public string FundCode { get; set; } = string.Empty;
    public Fund Fund { get; set; } = null!;

    public string Currency { get; set; } = "PHP";
    public decimal OpeningBalancePhp { get; set; }
    public string? Source { get; set; }
    public string? Notes { get; set; }
    public string Status { get; set; } = "Opening";
}
