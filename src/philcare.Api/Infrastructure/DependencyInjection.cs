using System.Reflection;
using System.Text;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.IdentityModel.Tokens;
using philcare.Api.Common.Persistence;
using philcare.Api.Features.Auth.ChangePassword;
using philcare.Api.Features.Auth.Login;
using philcare.Api.Features.Auth.Logout;
using philcare.Api.Features.Auth.RefreshToken;
using philcare.Api.Features.Auth.Register;
using philcare.Api.Features.Auth.RevokeAllSessions;
using philcare.Api.Features.Auth.Services;
using philcare.Api.Features.Finance.Donations.CreateDonation;
using philcare.Api.Features.Finance.Donations.VoidDonation;
using philcare.Api.Features.Finance.DonorEngagements.CreateDonorEngagement;
using philcare.Api.Features.Finance.DonorEngagements.UpdateDonorEngagement;
using philcare.Api.Features.Finance.Donors.CreateDonor;
using philcare.Api.Features.Finance.Donors.UpdateDonor;
using philcare.Api.Features.Finance.Expenses.CreateExpense;
using philcare.Api.Features.Finance.Expenses.VoidExpense;
using philcare.Api.Features.Finance.OtherIncomes.CreateOtherIncome;
using philcare.Api.Features.Finance.OtherIncomes.VoidOtherIncome;
using philcare.Api.Features.Governance.Assignments.CreateAssignment;
using philcare.Api.Features.Governance.Assignments.UpdateAssignment;
using philcare.Api.Features.Governance.Decisions.CreateDecision;
using philcare.Api.Features.Governance.Decisions.UpdateDecision;
using philcare.Api.Features.Governance.Meetings.CreateMeeting;
using philcare.Api.Features.Governance.Meetings.UpdateMeeting;
using philcare.Api.Features.Governance.MeetingParticipants.AddMeetingParticipant;
using philcare.Api.Features.Governance.Minutes.CreateMinutes;
using philcare.Api.Features.Governance.Minutes.UpdateMinutes;
using philcare.Api.Features.Governance.OrgBodies.CreateOrgBody;
using philcare.Api.Features.Governance.OrgBodies.UpdateOrgBody;
using philcare.Api.Features.Governance.People.CreatePerson;
using philcare.Api.Features.Governance.People.UpdatePerson;
using philcare.Api.Features.Governance.Roles.CreateGovernanceRole;
using philcare.Api.Features.Governance.Roles.UpdateGovernanceRole;
using philcare.Api.Features.Programs.ActivityParticipants.AddActivityParticipant;
using philcare.Api.Features.Programs.Activities.CreateActivity;
using philcare.Api.Features.Programs.Activities.UpdateActivity;
using philcare.Api.Features.Programs.AidPrograms.CreateProgram;
using philcare.Api.Features.Programs.AidPrograms.UpdateProgram;
using philcare.Api.Features.Programs.Distributions.CreateDistribution;
using philcare.Api.Features.Programs.Participants.CreateParticipant;
using philcare.Api.Features.Programs.Participants.UpdateParticipant;
using philcare.Api.Features.Programs.Projects.CreateProject;
using philcare.Api.Features.Programs.Projects.UpdateProject;
using philcare.Api.Features.Partners.CreatePartner;
using philcare.Api.Features.Partners.UpdatePartner;
using philcare.Api.Features.ReferenceData.CreateLookup;
using philcare.Api.Features.ReferenceData.UpdateLookup;
using philcare.Api.Features.Sponsorships.ChangeSponsorshipStatus;
using philcare.Api.Features.Sponsorships.CreateSponsorship;
using philcare.Api.Features.Sponsorships.UpdateSponsorship;
using philcare.Api.Features.Users.UpdateUser;
using philcare.Api.Features.Volunteers.ActivityVolunteers.AddActivityVolunteer;
using philcare.Api.Features.Volunteers.CreateVolunteer;
using philcare.Api.Features.Volunteers.UpdateVolunteer;
using philcare.Api.Features.Zakat.CreateZakatEligibility;
using philcare.Api.Features.Zakat.DecideZakatEligibility;
using philcare.Api.Features.Zakat.SubmitZakatEligibility;
using philcare.Api.Features.Zakat.UpdateZakatEligibility;
using philcare.Api.Infrastructure.Seed;

namespace philcare.Api.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpContextAccessor();
        services.AddSingleton<AuditInterceptor>();

        services.AddDbContext<AppDbContext>((sp, options) =>
        {
            var interceptor = sp.GetRequiredService<AuditInterceptor>();
            var useInMemory = configuration.GetValue<bool>("Persistence:UseInMemory");

            if (useInMemory)
            {
                var databaseName = configuration["Persistence:InMemoryDatabaseName"] ?? "PhilCareMisTests";
                options.UseInMemoryDatabase(databaseName);
            }
            else
            {
                var connectionString = configuration.GetConnectionString("Default")
                    ?? throw new InvalidOperationException("Connection string 'Default' is not configured.");
                options.UseMySql(connectionString, new MySqlServerVersion(new Version(10, 4, 32)));
            }

            options.AddInterceptors(interceptor);
        });

        services.AddScoped<DbSeeder>();

        return services;
    }

    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var jwtSection = configuration.GetSection(JwtOptions.SectionName);
        services.Configure<JwtOptions>(jwtSection);
        var jwtOptions = jwtSection.Get<JwtOptions>()
            ?? throw new InvalidOperationException("Jwt configuration section is missing.");

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtOptions.Issuer,
                    ValidAudience = jwtOptions.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Secret)),
                    ClockSkew = TimeSpan.FromSeconds(30)
                };
            });

        services.AddAuthorizationBuilder()
            .AddPolicy("Admin", policy => policy.RequireRole("Admin"))
            .AddPolicy("Finance", policy => policy.RequireRole("Finance", "Admin"))
            .AddPolicy("Program", policy => policy.RequireRole("Program", "Admin"));

        services.Configure<LockoutOptions>(configuration.GetSection(LockoutOptions.SectionName));

        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IPasswordHasher, PasswordHasher>();

        return services;
    }

    public static IServiceCollection AddValidation(this IServiceCollection services, Assembly assembly)
    {
        services.AddValidatorsFromAssembly(assembly);
        return services;
    }

    public static IServiceCollection AddFeatureHandlers(this IServiceCollection services)
    {
        services.AddScoped<LoginHandler>();
        services.AddScoped<RegisterHandler>();
        services.AddScoped<RefreshTokenHandler>();
        services.AddScoped<ChangePasswordHandler>();
        services.AddScoped<LogoutHandler>();
        services.AddScoped<RevokeAllSessionsHandler>();
        services.AddScoped<CreateLookupHandler>();
        services.AddScoped<UpdateLookupHandler>();
        services.AddScoped<UpdateUserHandler>();

        // Finance handlers — Sprint 2
        services.AddScoped<CreateDonorHandler>();
        services.AddScoped<UpdateDonorHandler>();
        services.AddScoped<CreateDonationHandler>();
        services.AddScoped<VoidDonationHandler>();
        services.AddScoped<CreateExpenseHandler>();
        services.AddScoped<VoidExpenseHandler>();
        services.AddScoped<CreateOtherIncomeHandler>();
        services.AddScoped<VoidOtherIncomeHandler>();

        // Programs handlers — Sprint 3
        services.AddScoped<CreateProgramHandler>();
        services.AddScoped<UpdateProgramHandler>();
        services.AddScoped<CreateProjectHandler>();
        services.AddScoped<UpdateProjectHandler>();
        services.AddScoped<CreateActivityHandler>();
        services.AddScoped<UpdateActivityHandler>();
        services.AddScoped<CreateParticipantHandler>();
        services.AddScoped<UpdateParticipantHandler>();
        services.AddScoped<AddActivityParticipantHandler>();
        services.AddScoped<CreateDistributionHandler>();

        // Partners, Volunteers, Sponsorship, Zakat Eligibility — Sprint 4
        services.AddScoped<CreatePartnerHandler>();
        services.AddScoped<UpdatePartnerHandler>();
        services.AddScoped<CreateVolunteerHandler>();
        services.AddScoped<UpdateVolunteerHandler>();
        services.AddScoped<AddActivityVolunteerHandler>();
        services.AddScoped<CreateSponsorshipHandler>();
        services.AddScoped<UpdateSponsorshipHandler>();
        services.AddScoped<ChangeSponsorshipStatusHandler>();
        services.AddScoped<CreateZakatEligibilityHandler>();
        services.AddScoped<UpdateZakatEligibilityHandler>();
        services.AddScoped<SubmitZakatEligibilityHandler>();
        services.AddScoped<DecideZakatEligibilityHandler>();

        // Donor engagement workflow — Sprint 5
        services.AddScoped<CreateDonorEngagementHandler>();
        services.AddScoped<UpdateDonorEngagementHandler>();

        // Governance handlers — Sprint 5
        services.AddScoped<CreatePersonHandler>();
        services.AddScoped<UpdatePersonHandler>();
        services.AddScoped<CreateOrgBodyHandler>();
        services.AddScoped<UpdateOrgBodyHandler>();
        services.AddScoped<CreateGovernanceRoleHandler>();
        services.AddScoped<UpdateGovernanceRoleHandler>();
        services.AddScoped<CreateAssignmentHandler>();
        services.AddScoped<UpdateAssignmentHandler>();
        services.AddScoped<CreateMeetingHandler>();
        services.AddScoped<UpdateMeetingHandler>();
        services.AddScoped<AddMeetingParticipantHandler>();
        services.AddScoped<CreateMinutesHandler>();
        services.AddScoped<UpdateMinutesHandler>();
        services.AddScoped<CreateDecisionHandler>();
        services.AddScoped<UpdateDecisionHandler>();

        return services;
    }
}
