using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using philcare.Api.Common.Persistence;
using philcare.Api.Features.Auth.Domain;
using philcare.Api.Features.Auth.Services;
using philcare.Api.Features.ReferenceData.Domain;

namespace philcare.Api.Infrastructure.Seed;

public sealed record SeedOptions
{
    public const string SectionName = "Seed";

    public string AdminEmail { get; init; } = string.Empty;
    public string AdminPassword { get; init; } = string.Empty;
}

public sealed record LookupSeedRow(string Category, string Code, string Label, int SortOrder);

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
        if (await db.LookupItems.AnyAsync(cancellationToken))
        {
            return;
        }

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

        db.LookupItems.AddRange(rows.Select(r => new LookupItem
        {
            Category = r.Category,
            Code = r.Code,
            Label = r.Label,
            SortOrder = r.SortOrder,
            IsActive = true
        }));

        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Seeded {Count} lookup items", rows.Count);
    }
}
