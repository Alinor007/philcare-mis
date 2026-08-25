using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using philcare.Api.Features.Auth.Domain;
using philcare.Api.Features.Finance.Domain;
using philcare.Api.Features.Governance.Domain;
using philcare.Api.Features.Partners.Domain;
using philcare.Api.Features.People.Domain;
using philcare.Api.Features.Programs.Domain;
using philcare.Api.Features.ReferenceData.Domain;
using philcare.Api.Features.ReferenceData.Geography.Domain;
using philcare.Api.Features.Sponsorships.Domain;
using philcare.Api.Features.HumanResources.Domain;
using philcare.Api.Features.Zakat.Domain;

namespace philcare.Api.Common.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<LookupItem> LookupItems => Set<LookupItem>();

    // Geography (real PSGC hierarchy) — Sprint 7
    public DbSet<Region> Regions => Set<Region>();
    public DbSet<Province> Provinces => Set<Province>();
    public DbSet<CityMunicipality> CitiesMunicipalities => Set<CityMunicipality>();

    // Finance
    public DbSet<Fund> Funds => Set<Fund>();
    public DbSet<FundingBucket> FundingBuckets => Set<FundingBucket>();
    public DbSet<OpeningBalance> OpeningBalances => Set<OpeningBalance>();
    public DbSet<Donor> Donors => Set<Donor>();
    public DbSet<Donation> Donations => Set<Donation>();
    public DbSet<OtherIncome> OtherIncomes => Set<OtherIncome>();
    public DbSet<DonorEngagement> DonorEngagements => Set<DonorEngagement>();
    public DbSet<Allocation> Allocations => Set<Allocation>();
    public DbSet<OutboxEmail> OutboxEmails => Set<OutboxEmail>();
    public DbSet<Expense> Expenses => Set<Expense>();

    // Programs
    public DbSet<AidProgram> Programs => Set<AidProgram>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<ProjectDonor> ProjectDonors => Set<ProjectDonor>();
    public DbSet<Activity> Activities => Set<Activity>();
    public DbSet<Beneficiary> Beneficiaries => Set<Beneficiary>();
    public DbSet<ActivityParticipant> ActivityParticipants => Set<ActivityParticipant>();
    public DbSet<Distribution> Distributions => Set<Distribution>();
    // Reach roster — who received aid at a distribution. Carries no money; see DistributionBeneficiary.
    public DbSet<DistributionBeneficiary> DistributionBeneficiaries => Set<DistributionBeneficiary>();

    // Partners, Volunteers, Sponsorship, Zakat Eligibility — Sprint 4
    // (Volunteer/ActivityVolunteer moved into the HumanResources module in Sprint 7; the DbSet
    // names and table names are unchanged, so no schema or API change came with that move.)
    public DbSet<Partner> Partners => Set<Partner>();
    public DbSet<Volunteer> Volunteers => Set<Volunteer>();
    public DbSet<ActivityVolunteer> ActivityVolunteers => Set<ActivityVolunteer>();
    public DbSet<Sponsorship> Sponsorships => Set<Sponsorship>();
    public DbSet<ZakatEligibility> ZakatEligibilities => Set<ZakatEligibility>();

    // Governance — Sprint 5
    public DbSet<Person> GovernancePeople => Set<Person>();
    public DbSet<Membership> Memberships => Set<Membership>();
    public DbSet<OrgBody> OrgBodies => Set<OrgBody>();
    public DbSet<GovernanceRole> GovernanceRoles => Set<GovernanceRole>();
    public DbSet<Assignment> Assignments => Set<Assignment>();
    public DbSet<Meeting> Meetings => Set<Meeting>();
    public DbSet<MeetingParticipant> MeetingParticipants => Set<MeetingParticipant>();
    public DbSet<MeetingMinutes> MeetingMinutes => Set<MeetingMinutes>();
    public DbSet<MeetingDecision> MeetingDecisions => Set<MeetingDecision>();

    // Human Resources — Sprint 7
    public DbSet<StaffMember> StaffMembers => Set<StaffMember>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }

    /// <summary>
    /// MySQL has no timezone-aware datetime type, so every DateTime EF Core reads back comes out
    /// tagged Kind=Unspecified even though this codebase only ever writes DateTime.UtcNow. Left
    /// alone, System.Text.Json omits the "Z" suffix for Unspecified values, and every API client
    /// (this app's own frontend included) then parses that bare string as local time instead of
    /// UTC — the actual cause of donation-email timestamps appearing hours off from when they were
    /// really sent. Re-stamping every DateTime as Utc on both read and write fixes this everywhere
    /// at once, with no schema/migration change since it's a CLR-side reinterpretation only.
    /// </summary>
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<DateTime>().HaveConversion<UtcDateTimeConverter>();
        configurationBuilder.Properties<DateTime?>().HaveConversion<UtcNullableDateTimeConverter>();
    }

    private sealed class UtcDateTimeConverter : ValueConverter<DateTime, DateTime>
    {
        public UtcDateTimeConverter() : base(
            v => v.Kind == DateTimeKind.Utc ? v : DateTime.SpecifyKind(v, DateTimeKind.Utc),
            v => DateTime.SpecifyKind(v, DateTimeKind.Utc))
        {
        }
    }

    private sealed class UtcNullableDateTimeConverter : ValueConverter<DateTime?, DateTime?>
    {
        public UtcNullableDateTimeConverter() : base(
            v => v.HasValue && v.Value.Kind != DateTimeKind.Utc ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v,
            v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v)
        {
        }
    }
}
