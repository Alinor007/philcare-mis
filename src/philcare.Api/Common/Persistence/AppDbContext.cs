using Microsoft.EntityFrameworkCore;
using philcare.Api.Features.Auth.Domain;
using philcare.Api.Features.Finance.Domain;
using philcare.Api.Features.Partners.Domain;
using philcare.Api.Features.Programs.Domain;
using philcare.Api.Features.ReferenceData.Domain;
using philcare.Api.Features.Sponsorships.Domain;
using philcare.Api.Features.Volunteers.Domain;
using philcare.Api.Features.Zakat.Domain;

namespace philcare.Api.Common.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<LookupItem> LookupItems => Set<LookupItem>();

    // Finance
    public DbSet<Fund> Funds => Set<Fund>();
    public DbSet<FundingBucket> FundingBuckets => Set<FundingBucket>();
    public DbSet<OpeningBalance> OpeningBalances => Set<OpeningBalance>();
    public DbSet<Donor> Donors => Set<Donor>();
    public DbSet<Donation> Donations => Set<Donation>();
    public DbSet<OtherIncome> OtherIncomes => Set<OtherIncome>();
    public DbSet<Allocation> Allocations => Set<Allocation>();
    public DbSet<Expense> Expenses => Set<Expense>();

    // Programs
    public DbSet<AidProgram> Programs => Set<AidProgram>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<Activity> Activities => Set<Activity>();
    public DbSet<Participant> Participants => Set<Participant>();
    public DbSet<ActivityParticipant> ActivityParticipants => Set<ActivityParticipant>();
    public DbSet<Distribution> Distributions => Set<Distribution>();

    // Partners, Volunteers, Sponsorship, Zakat Eligibility — Sprint 4
    public DbSet<Partner> Partners => Set<Partner>();
    public DbSet<Volunteer> Volunteers => Set<Volunteer>();
    public DbSet<ActivityVolunteer> ActivityVolunteers => Set<ActivityVolunteer>();
    public DbSet<Sponsorship> Sponsorships => Set<Sponsorship>();
    public DbSet<ZakatEligibility> ZakatEligibilities => Set<ZakatEligibility>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
