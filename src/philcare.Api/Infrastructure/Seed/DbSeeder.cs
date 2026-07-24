using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using philcare.Api.Common.Persistence;
using philcare.Api.Features.Auth.Domain;
using philcare.Api.Features.Auth.Services;
using philcare.Api.Features.Finance.Domain;
using philcare.Api.Features.ReferenceData.Domain;

namespace philcare.Api.Infrastructure.Seed;

public sealed record SeedOptions
{
    public const string SectionName = "Seed";

    public string AdminEmail { get; init; } = string.Empty;
    public string AdminPassword { get; init; } = string.Empty;
}

public sealed record LookupSeedRow(string Category, string Code, string Label, int SortOrder);

public sealed record FundSeedRow(string Code, string Name, bool IsRestricted, string? PolicyNotes, string? UseCase, bool SeparateTrackingRequired);

public sealed record BucketSeedRow(
    string Code, string Name, string FundCode, string BucketType, decimal MaxAdminRate,
    string? PolicyRule, string? TypicalUse, bool SeparateTrackingRequired);

public sealed record OpeningBalanceSeedRow(int Year, string FundCode, string Currency, decimal OpeningBalancePhp, string? Source, string? Notes);

public sealed record FinanceSeedData(List<FundSeedRow> Funds, List<BucketSeedRow> Buckets, List<OpeningBalanceSeedRow> OpeningBalances);

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

        await SeedAdminUserAsync(cancellationToken);
        await SeedLookupsAsync(cancellationToken);
        await SeedFinanceAsync(cancellationToken);
    }

    private async Task SeedAdminUserAsync(CancellationToken cancellationToken)
    {
        var seedOptions = configuration.GetSection(SeedOptions.SectionName).Get<SeedOptions>();

        if (seedOptions is null || string.IsNullOrWhiteSpace(seedOptions.AdminEmail))
        {
            logger.LogWarning("Seed:AdminEmail is not configured; skipping admin user seed.");
            return;
        }

        var adminExists = await db.Users.AnyAsync(u => u.Email == seedOptions.AdminEmail, cancellationToken);
        if (adminExists)
        {
            return;
        }

        db.Users.Add(new User
        {
            Email = seedOptions.AdminEmail,
            PasswordHash = passwordHasher.Hash(seedOptions.AdminPassword),
            Role = UserRole.Admin,
            IsActive = true
        });

        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Seeded admin user {Email}", seedOptions.AdminEmail);
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

        // Additive: only insert (category, code) pairs not already present, so new lookup
        // categories introduced in later sprints seed into databases that already have data.
        var existingKeys = (await db.LookupItems
                .Select(l => new { l.Category, l.Code })
                .ToListAsync(cancellationToken))
            .Select(l => (l.Category, l.Code))
            .ToHashSet();

        var newRows = rows.Where(r => !existingKeys.Contains((r.Category, r.Code))).ToList();

        if (newRows.Count == 0)
        {
            return;
        }

        db.LookupItems.AddRange(newRows.Select(r => new LookupItem
        {
            Category = r.Category,
            Code = r.Code,
            Label = r.Label,
            SortOrder = r.SortOrder,
            IsActive = true
        }));

        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Seeded {Count} new lookup items", newRows.Count);
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
}
