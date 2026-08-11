namespace philcare.Api.Features.Auth.Domain;

public enum UserRole
{
    Admin,
    Finance,
    Program,
    Viewer,

    /// <summary>
    /// Zakat &amp; Donations Collection Department — donor management, fundraising (donations and
    /// other income) and zakat casework.
    ///
    /// Exists because no combination of the original four roles fits: <see cref="Finance"/> covers
    /// the money-in side but is refused on every zakat endpoint, while <see cref="Program"/> covers
    /// zakat casework but not donors or donations. Rather than granting this department Admin, it
    /// joins the narrower "Income" and "ZakatCasework" policies (see DependencyInjection).
    ///
    /// Deliberately excluded: expenses (money out stays Finance), the zakat approval decision
    /// (stays Admin, so the department that assesses a case is not the one that approves it), and
    /// every void/deactivation.
    ///
    /// Persisted via HasConversion&lt;string&gt; into an existing varchar(20), so adding this value
    /// needed no migration.
    /// </summary>
    ZakatDonations
}
