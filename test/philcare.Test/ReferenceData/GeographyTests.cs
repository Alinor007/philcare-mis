using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using philcare.Api.Features.ReferenceData.Geography.GetCitiesMunicipalities;
using philcare.Api.Features.ReferenceData.Geography.GetProvinces;
using philcare.Api.Features.ReferenceData.Geography.GetRegions;
using philcare.Api.Infrastructure.Seed;
using philcare.Test.Common;
using Xunit;

namespace philcare.Test.ReferenceData;

/// <summary>
/// Covers the real PSGC geography reference API. The counts asserted here (17/81/1634) are the
/// published PSGC totals — if a future seed refresh changes them, that is a deliberate data
/// update and these assertions are the intended place to notice it.
/// </summary>
public class GeographyTests : IClassFixture<TestWebAppFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private const string NcrRegionCode = "130000000";
    private const string IlocosNorteProvinceCode = "012800000";

    private readonly TestWebAppFactory _factory;
    private readonly HttpClient _client;

    public GeographyTests(TestWebAppFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task AuthenticateAsAdminAsync()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new { Email = "admin@philcare.local", Password = "Admin@12345" });
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<LoginResponseDto>(JsonOptions);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body!.AccessToken);
    }

    [Fact]
    public async Task GetRegions_RequiresAuthentication()
    {
        var response = await _client.GetAsync("/api/regions");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetRegions_ReturnsAllSeventeenRealRegions()
    {
        await AuthenticateAsAdminAsync();

        var response = await _client.GetAsync("/api/regions");
        response.EnsureSuccessStatusCode();
        var regions = await response.Content.ReadFromJsonAsync<List<RegionResponse>>(JsonOptions);

        Assert.Equal(17, regions!.Count);

        // Regions absent from the old hand-picked 9-item "region" lookup this replaces.
        Assert.Contains(regions, r => r.Name == "CAR");
        Assert.Contains(regions, r => r.Name == "MIMAROPA Region");
        Assert.Contains(regions, r => r.Name == "Caraga");
        Assert.Contains(regions, r => r.Name == "Bicol Region");

        var ncr = Assert.Single(regions, r => r.Code == NcrRegionCode);
        Assert.Equal("National Capital Region", ncr.DesignationName);
        Assert.Equal("luzon", ncr.IslandGroup);
    }

    [Fact]
    public async Task GetProvinces_ReturnsAllEightyOneRealProvinces()
    {
        await AuthenticateAsAdminAsync();

        var response = await _client.GetAsync("/api/provinces");
        response.EnsureSuccessStatusCode();
        var provinces = await response.Content.ReadFromJsonAsync<List<ProvinceResponse>>(JsonOptions);

        Assert.Equal(81, provinces!.Count);
        Assert.Contains(provinces, p => p.Name == "Ilocos Norte");
    }

    [Fact]
    public async Task GetProvinces_FilteredByRegion_ReturnsOnlyThatRegions()
    {
        await AuthenticateAsAdminAsync();

        const string ilocosRegionCode = "010000000";
        var response = await _client.GetAsync($"/api/provinces?regionCode={ilocosRegionCode}");
        response.EnsureSuccessStatusCode();
        var provinces = await response.Content.ReadFromJsonAsync<List<ProvinceResponse>>(JsonOptions);

        Assert.NotEmpty(provinces!);
        Assert.All(provinces!, p => Assert.Equal(ilocosRegionCode, p.RegionCode));
        Assert.Contains(provinces!, p => p.Name == "Ilocos Norte");
    }

    /// <summary>
    /// NCR genuinely has no provinces in the PSGC — an empty list is the correct answer here,
    /// not a bug, and its cities are only reachable via regionCode.
    /// </summary>
    [Fact]
    public async Task GetProvinces_ForNcr_ReturnsEmptyBecauseNcrHasNoProvinces()
    {
        await AuthenticateAsAdminAsync();

        var response = await _client.GetAsync($"/api/provinces?regionCode={NcrRegionCode}");
        response.EnsureSuccessStatusCode();
        var provinces = await response.Content.ReadFromJsonAsync<List<ProvinceResponse>>(JsonOptions);

        Assert.Empty(provinces!);
    }

    [Fact]
    public async Task GetCities_ReturnsAllRealCitiesAndMunicipalities()
    {
        await AuthenticateAsAdminAsync();

        var response = await _client.GetAsync("/api/cities");
        response.EnsureSuccessStatusCode();
        var cities = await response.Content.ReadFromJsonAsync<List<CityMunicipalityResponse>>(JsonOptions);

        Assert.Equal(1634, cities!.Count);
    }

    [Fact]
    public async Task GetCities_FilteredByProvince_ReturnsOnlyThatProvinces()
    {
        await AuthenticateAsAdminAsync();

        var response = await _client.GetAsync($"/api/cities?provinceCode={IlocosNorteProvinceCode}");
        response.EnsureSuccessStatusCode();
        var cities = await response.Content.ReadFromJsonAsync<List<CityMunicipalityResponse>>(JsonOptions);

        Assert.NotEmpty(cities!);
        Assert.All(cities!, c => Assert.Equal(IlocosNorteProvinceCode, c.ProvinceCode));
        Assert.Contains(cities!, c => c.Name == "Adams");

        // Laoag is Ilocos Norte's capital — proves IsCity/IsCapital survive the seed.
        var capital = Assert.Single(cities!, c => c.IsCapital);
        Assert.True(capital.IsCity);
    }

    [Fact]
    public async Task GetCities_FilteredByNcrRegion_ReturnsProvincelessCities()
    {
        await AuthenticateAsAdminAsync();

        var response = await _client.GetAsync($"/api/cities?regionCode={NcrRegionCode}");
        response.EnsureSuccessStatusCode();
        var cities = await response.Content.ReadFromJsonAsync<List<CityMunicipalityResponse>>(JsonOptions);

        Assert.Equal(17, cities!.Count);
        Assert.All(cities!, c => Assert.Null(c.ProvinceCode));
        Assert.Contains(cities!, c => c.Name == "City of Manila");
    }

    /// <summary>Every city must resolve to a seeded region, and to its province when it has one.</summary>
    [Fact]
    public async Task Geography_HierarchyIsInternallyConsistent()
    {
        await AuthenticateAsAdminAsync();

        var regions = await _client.GetFromJsonAsync<List<RegionResponse>>("/api/regions", JsonOptions);
        var provinces = await _client.GetFromJsonAsync<List<ProvinceResponse>>("/api/provinces", JsonOptions);
        var cities = await _client.GetFromJsonAsync<List<CityMunicipalityResponse>>("/api/cities", JsonOptions);

        var regionCodes = regions!.Select(r => r.Code).ToHashSet();
        var provinceCodes = provinces!.Select(p => p.Code).ToHashSet();

        Assert.All(provinces!, p => Assert.Contains(p.RegionCode, regionCodes));
        Assert.All(cities!, c =>
        {
            Assert.Contains(c.RegionCode, regionCodes);
            if (c.ProvinceCode is not null)
            {
                Assert.Contains(c.ProvinceCode, provinceCodes);
            }
        });
    }

    /// <summary>
    /// Geography seeds once and is not duplicated when the seeder re-runs, which it does on every
    /// boot. The guard is the "table already has rows" check, so this asserts a real restart.
    /// </summary>
    [Fact]
    public async Task Reseeding_DoesNotDuplicateGeography()
    {
        await AuthenticateAsAdminAsync();

        using (var scope = _factory.Services.CreateScope())
        {
            await scope.ServiceProvider.GetRequiredService<DbSeeder>().SeedAsync();
        }

        var regions = await _client.GetFromJsonAsync<List<RegionResponse>>("/api/regions", JsonOptions);
        var provinces = await _client.GetFromJsonAsync<List<ProvinceResponse>>("/api/provinces", JsonOptions);
        var cities = await _client.GetFromJsonAsync<List<CityMunicipalityResponse>>("/api/cities", JsonOptions);

        Assert.Equal(17, regions!.Count);
        Assert.Equal(81, provinces!.Count);
        Assert.Equal(1634, cities!.Count);
    }

    private sealed record LoginResponseDto(string AccessToken, string RefreshToken, DateTime RefreshTokenExpiresAt);
}
