using philcare.Api.Common.Domain;

namespace philcare.Api.Features.ReferenceData.Geography.Domain;

/// <summary>A Philippine province, keyed by its real 9-digit PSGC code. NCR has none — see CityMunicipality.</summary>
public class Province : Entity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;

    public string RegionCode { get; set; } = string.Empty;
    public Region Region { get; set; } = null!;

    public List<CityMunicipality> CitiesMunicipalities { get; set; } = [];
}
