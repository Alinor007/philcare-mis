using philcare.Api.Common.Domain;

namespace philcare.Api.Features.ReferenceData.Geography.Domain;

/// <summary>
/// A Philippine administrative region, keyed by its real 9-digit PSGC code (e.g. "130000000" for
/// NCR) — not a synthetic ID and not the hand-picked 9-item "region" lookup category it replaces.
/// Seeded once from the official Philippine Standard Geographic Code publication (17 regions) and
/// never edited through the app; there is no create/update endpoint for this reference data.
/// </summary>
public class Region : Entity
{
    /// <summary>The official 9-digit PSGC region code. Unique; the FK target for Province/City.</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Short/common name, e.g. "NCR", "Ilocos Region", "CALABARZON".</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Formal designation, e.g. "Region I", "National Capital Region".</summary>
    public string DesignationName { get; set; } = string.Empty;

    /// <summary>"luzon" | "visayas" | "mindanao", as published in the PSGC source.</summary>
    public string IslandGroup { get; set; } = string.Empty;

    public List<Province> Provinces { get; set; } = [];
    public List<CityMunicipality> CitiesMunicipalities { get; set; } = [];
}
