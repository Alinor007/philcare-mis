using philcare.Api.Common.Domain;

namespace philcare.Api.Features.ReferenceData.Geography.Domain;

/// <summary>
/// A Philippine city or municipality, keyed by its real 9-digit PSGC code.
///
/// <see cref="ProvinceCode"/> is nullable — this is a real PSGC feature, not missing data. NCR's
/// 17 cities sit directly under their region with no province, and a handful of independent
/// cities elsewhere (e.g. City of Isabela in Basilan) do the same. Always trust RegionCode; only
/// use ProvinceCode when present.
/// </summary>
public class CityMunicipality : Entity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;

    /// <summary>True for a city, false for a municipality — mutually exclusive per PSGC.</summary>
    public bool IsCity { get; set; }

    /// <summary>True if this is its province's capital. Meaningless when ProvinceCode is null.</summary>
    public bool IsCapital { get; set; }

    public string? ProvinceCode { get; set; }
    public Province? Province { get; set; }

    public string RegionCode { get; set; } = string.Empty;
    public Region Region { get; set; } = null!;
}
