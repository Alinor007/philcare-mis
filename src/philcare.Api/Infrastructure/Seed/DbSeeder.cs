using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using philcare.Api.Common.Persistence;
using philcare.Api.Features.Auth.Domain;
using philcare.Api.Features.Auth.Services;
using philcare.Api.Features.Finance.Domain;
using philcare.Api.Features.ReferenceData.Domain;
using philcare.Api.Features.ReferenceData.Geography.Domain;

namespace philcare.Api.Infrastructure.Seed;

public sealed record SeedOptions
{
    public const string SectionName = "Seed";

    public string AdminEmail { get; init; } = string.Empty;
    public string AdminPassword { get; init; } = string.Empty;

    /// <summary>
    /// Zakat &amp; Donations Collection Department account (<see cref="UserRole.ZakatDonations"/>).
    /// Left blank in an environment that doesn't need it — the seeder logs and skips.
    /// </summary>
    public string ZakatDonationsEmail { get; init; } = string.Empty;
    public string ZakatDonationsPassword { get; init; } = string.Empty;

    /// <summary>
    /// Escape hatch: when true, seed-owned label/sort-order relabelling is applied even if a
    /// row's UpdatedBy doesn't match the "system" sentinel — i.e. force every row back to the
    /// JSON regardless of who last touched it. Off by default; admin edits win by default.
    /// </summary>
    public bool ForceLookupLabels { get; init; }
}

public sealed record LookupSeedRow(string Category, string Code, string Label, int SortOrder);

public sealed record FundSeedRow(string Code, string Name, bool IsRestricted, string? PolicyNotes, string? UseCase, bool SeparateTrackingRequired);

public sealed record BucketSeedRow(
    string Code, string Name, string FundCode, string BucketType, decimal MaxAdminRate,
    string? PolicyRule, string? TypicalUse, bool SeparateTrackingRequired);

public sealed record OpeningBalanceSeedRow(int Year, string FundCode, string Currency, decimal OpeningBalancePhp, string? Source, string? Notes);

public sealed record FinanceSeedData(List<FundSeedRow> Funds, List<BucketSeedRow> Buckets, List<OpeningBalanceSeedRow> OpeningBalances);

public sealed record RegionSeedRow(string Code, string Name, string DesignationName, string IslandGroup);

public sealed record ProvinceSeedRow(string Code, string Name, string RegionCode);

public sealed record CityMunicipalitySeedRow(string Code, string Name, bool IsCity, bool IsCapital, string? ProvinceCode, string RegionCode);

public sealed class DbSeeder(
    AppDbContext db,
    IPasswordHasher passwordHasher,
    IConfiguration configuration,
    ILogger<DbSeeder> logger)
{
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (db.Database.IsRelational())
        {
            await db.Database.MigrateAsync(cancellationToken);
        }

        await SeedUsersAsync(cancellationToken);
        await SeedLookupsAsync(cancellationToken);
        await SeedFinanceAsync(cancellationToken);
        await SeedGeographyAsync(cancellationToken);
    }

    private async Task SeedUsersAsync(CancellationToken cancellationToken)
    {
        var seedOptions = configuration.GetSection(SeedOptions.SectionName).Get<SeedOptions>();

        if (seedOptions is null)
        {
            logger.LogWarning("Seed section is not configured; skipping user seed.");
            return;
        }

        await SeedUserAsync(seedOptions.AdminEmail, seedOptions.AdminPassword, UserRole.Admin, "Seed:AdminEmail", cancellationToken);

        // Zakat & Donations Collection Department — donor management, fundraising and zakat casework.
        await SeedUserAsync(
            seedOptions.ZakatDonationsEmail,
            seedOptions.ZakatDonationsPassword,
            UserRole.ZakatDonations,
            "Seed:ZakatDonationsEmail",
            cancellationToken);
    }

    /// <summary>
    /// Idempotent per email: an already-present account is left completely untouched, so a
    /// password an admin has since rotated is never reset by a redeploy.
    /// </summary>
    private async Task SeedUserAsync(
        string email,
        string password,
        UserRole role,
        string configKeyForLogging,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            logger.LogWarning("{ConfigKey} is not configured; skipping {Role} user seed.", configKeyForLogging, role);
            return;
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            logger.LogWarning("No password configured for {Email}; skipping {Role} user seed.", email, role);
            return;
        }

        if (await db.Users.AnyAsync(u => u.Email == email, cancellationToken))
        {
            return;
        }

        db.Users.Add(new User
        {
            Email = email,
            PasswordHash = passwordHasher.Hash(password),
            Role = role,
            IsActive = true
        });

        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Seeded {Role} user {Email}", role, email);
    }

    private async Task SeedLookupsAsync(CancellationToken cancellationToken)
    {
        var seedFilePath = Path.Combine(AppContext.BaseDirectory, "Infrastructure", "Seed", "lookup-seed.json");
        if (!File.Exists(seedFilePath))
        {
            logger.LogWarning("Lookup seed file not found at {Path}", seedFilePath);
            return;
        }

        var json = await File.ReadAllTextAsync(seedFilePath, cancellationToken);
        var rows = JsonSerializer.Deserialize<List<LookupSeedRow>>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (rows is null || rows.Count == 0)
        {
            return;
        }

        var forceLabels = configuration.GetSection(SeedOptions.SectionName).Get<SeedOptions>()?.ForceLookupLabels ?? false;

        // Insert new (category, code) pairs additively — new lookup categories introduced in
        // later sprints seed into databases that already have data — and reconcile the label/
        // sort order of rows that already exist, but ONLY while they're still seed-owned (i.e.
        // nobody has edited them via /admin/lookups since). UpdatedBy is the ownership signal:
        // a row last touched by the seeder reads AuditInterceptor.SystemUser; a row last touched
        // by an admin reads their email. We never touch IsActive or delete a row for either kind
        // — an admin's deactivation must survive every restart regardless of who "owns" the label.
        var existing = await db.LookupItems.ToListAsync(cancellationToken);
        var byKey = existing.ToDictionary(l => (l.Category, l.Code));

        var inserted = 0;
        var updated = 0;
        var preserved = new List<string>();

        foreach (var row in rows)
        {
            if (!byKey.TryGetValue((row.Category, row.Code), out var item))
            {
                db.LookupItems.Add(new LookupItem
                {
                    Category = row.Category,
                    Code = row.Code,
                    Label = row.Label,
                    SortOrder = row.SortOrder,
                    IsActive = true
                });
                inserted++;
                continue;
            }

            if (item.Label == row.Label && item.SortOrder == row.SortOrder)
            {
                continue;
            }

            var isSeedOwned = item.UpdatedBy is null || item.UpdatedBy == AuditInterceptor.SystemUser;
            if (isSeedOwned || forceLabels)
            {
                item.Label = row.Label;
                item.SortOrder = row.SortOrder;
                updated++;
            }
            else
            {
                preserved.Add($"{item.Category}.{item.Code}");
            }
        }

        if (inserted == 0 && updated == 0)
        {
            return;
        }

        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation(
            "Lookups: {Inserted} inserted, {Updated} relabelled, {Skipped} preserved (admin-edited)",
            inserted, updated, preserved.Count);

        if (preserved.Count > 0)
        {
            logger.LogInformation("Preserved admin-edited lookups: {Codes}", string.Join(", ", preserved));
        }
    }

    private async Task SeedFinanceAsync(CancellationToken cancellationToken)
    {
        if (await db.Funds.AnyAsync(cancellationToken))
        {
            return;
        }

        var seedFilePath = Path.Combine(AppContext.BaseDirectory, "Infrastructure", "Seed", "finance-seed.json");
        if (!File.Exists(seedFilePath))
        {
            logger.LogWarning("Finance seed file not found at {Path}", seedFilePath);
            return;
        }

        var json = await File.ReadAllTextAsync(seedFilePath, cancellationToken);
        var data = JsonSerializer.Deserialize<FinanceSeedData>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (data is null)
        {
            return;
        }

        db.Funds.AddRange(data.Funds.Select(f => new Fund
        {
            Code = f.Code,
            Name = f.Name,
            IsRestricted = f.IsRestricted,
            PolicyNotes = f.PolicyNotes,
            UseCase = f.UseCase,
            SeparateTrackingRequired = f.SeparateTrackingRequired
        }));

        db.FundingBuckets.AddRange(data.Buckets.Select(b => new FundingBucket
        {
            Code = b.Code,
            Name = b.Name,
            FundCode = b.FundCode,
            BucketType = Enum.Parse<BucketType>(b.BucketType, ignoreCase: true),
            MaxAdminRate = b.MaxAdminRate,
            PolicyRule = b.PolicyRule,
            TypicalUse = b.TypicalUse,
            SeparateTrackingRequired = b.SeparateTrackingRequired,
            AllocatedAmount = 0,
            ExpensedAmount = 0
        }));

        db.OpeningBalances.AddRange(data.OpeningBalances.Select(o => new OpeningBalance
        {
            Year = o.Year,
            FundCode = o.FundCode,
            Currency = o.Currency,
            OpeningBalancePhp = o.OpeningBalancePhp,
            Source = o.Source,
            Notes = o.Notes,
            Status = "Opening"
        }));

        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation(
            "Seeded {Funds} funds, {Buckets} funding buckets, {OpeningBalances} opening balances",
            data.Funds.Count, data.Buckets.Count, data.OpeningBalances.Count);
    }

    /// <summary>
    /// The real PSGC (Philippine Standard Geographic Code) Region → Province → City/Municipality
    /// hierarchy — 17 regions, 81 provinces, 1,634 cities/municipalities, sourced from the
    /// official PSGC publication. Seeds once, like Finance: this is a closed, versioned dataset
    /// with no admin-edit path, not an open vocabulary that grows sprint over sprint the way
    /// LookupItem's categories do, so there is no per-row reconciliation pass — only ever
    /// inserted if the table is empty.
    /// </summary>
    private async Task SeedGeographyAsync(CancellationToken cancellationToken)
    {
        if (await db.Regions.AnyAsync(cancellationToken))
        {
            return;
        }

        var seedDir = Path.Combine(AppContext.BaseDirectory, "Infrastructure", "Seed");
        var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        var regions = await ReadGeographySeedAsync<RegionSeedRow>(Path.Combine(seedDir, "regions-seed.json"), jsonOptions, cancellationToken);
        var provinces = await ReadGeographySeedAsync<ProvinceSeedRow>(Path.Combine(seedDir, "provinces-seed.json"), jsonOptions, cancellationToken);
        var cities = await ReadGeographySeedAsync<CityMunicipalitySeedRow>(Path.Combine(seedDir, "cities-seed.json"), jsonOptions, cancellationToken);

        if (regions is null || provinces is null || cities is null)
        {
            return;
        }

        // Parents before children — Province/CityMunicipality FK to Region.Code (and
        // CityMunicipality optionally to Province.Code) must already exist when SaveChanges runs.
        db.Regions.AddRange(regions.Select(r => new Region
        {
            Code = r.Code,
            Name = r.Name,
            DesignationName = r.DesignationName,
            IslandGroup = r.IslandGroup
        }));
        await db.SaveChangesAsync(cancellationToken);

        db.Provinces.AddRange(provinces.Select(p => new Province
        {
            Code = p.Code,
            Name = p.Name,
            RegionCode = p.RegionCode
        }));
        await db.SaveChangesAsync(cancellationToken);

        db.CitiesMunicipalities.AddRange(cities.Select(c => new CityMunicipality
        {
            Code = c.Code,
            Name = c.Name,
            IsCity = c.IsCity,
            IsCapital = c.IsCapital,
            ProvinceCode = c.ProvinceCode,
            RegionCode = c.RegionCode
        }));
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Seeded {Regions} regions, {Provinces} provinces, {Cities} cities/municipalities",
            regions.Count, provinces.Count, cities.Count);
    }

    private async Task<List<T>?> ReadGeographySeedAsync<T>(string path, JsonSerializerOptions jsonOptions, CancellationToken cancellationToken)
    {
        if (!File.Exists(path))
        {
            logger.LogWarning("Geography seed file not found at {Path}", path);
            return null;
        }

        var json = await File.ReadAllTextAsync(path, cancellationToken);
        return JsonSerializer.Deserialize<List<T>>(json, jsonOptions);
    }
}
