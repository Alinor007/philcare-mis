using Microsoft.EntityFrameworkCore;
using philcare.Api.Features.Auth.Domain;
using philcare.Api.Features.Finance.Domain;
using philcare.Api.Features.ReferenceData.Domain;

namespace philcare.Api.Common.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<LookupItem> LookupItems => Set<LookupItem>();

    // Finance — Sprint 2
    public DbSet<Donor> Donors => Set<Donor>();
    public DbSet<Donation> Donations => Set<Donation>();
    public DbSet<FundBucket> FundBuckets => Set<FundBucket>();
    public DbSet<Allocation> Allocations => Set<Allocation>();
    public DbSet<Expense> Expenses => Set<Expense>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
