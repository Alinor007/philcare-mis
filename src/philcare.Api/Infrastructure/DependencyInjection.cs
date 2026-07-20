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
using philcare.Api.Features.Finance.Donors.CreateDonor;
using philcare.Api.Features.Finance.Donors.UpdateDonor;
using philcare.Api.Features.Finance.Expenses.CreateExpense;
using philcare.Api.Features.Finance.Expenses.VoidExpense;
using philcare.Api.Features.ReferenceData.CreateLookup;
using philcare.Api.Features.ReferenceData.UpdateLookup;
using philcare.Api.Features.Users.UpdateUser;
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
                options.UseInMemoryDatabase("PhilCareMisTests");
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
            .AddPolicy("Finance", policy => policy.RequireRole("Finance", "Admin"));

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

        return services;
    }
}
