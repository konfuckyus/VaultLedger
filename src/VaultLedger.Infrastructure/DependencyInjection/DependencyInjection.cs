using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using VaultLedger.Application.Common;
using VaultLedger.Application.Interfaces;
using VaultLedger.Application.Interfaces.Repositories;
using VaultLedger.Application.Interfaces.Services;
using VaultLedger.Infrastructure.Identity;
using VaultLedger.Infrastructure.Persistence;
using VaultLedger.Infrastructure.Repositories;
using VaultLedger.Infrastructure.Services;

namespace VaultLedger.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "Connection string 'DefaultConnection' is not configured.");

        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.Configure<CardHashOptions>(configuration.GetSection(CardHashOptions.SectionName));

        var jwt = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
            ?? new JwtOptions();

        if (string.IsNullOrWhiteSpace(jwt.Secret) || jwt.Secret.Length < 32)
        {
            throw new InvalidOperationException(
                "Jwt:Secret must be configured (min 32 chars) via User Secrets, environment variables, " +
                "or a non-committed appsettings.*.json file. See README.");
        }

        var cardHash = configuration.GetSection(CardHashOptions.SectionName).Get<CardHashOptions>()
            ?? new CardHashOptions();

        if (string.IsNullOrWhiteSpace(cardHash.Secret) || cardHash.Secret.Length < 32)
        {
            throw new InvalidOperationException(
                "CardHash:Secret must be configured (min 32 chars) via User Secrets, environment variables, " +
                "or a non-committed appsettings.*.json file. See README.");
        }

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "ConnectionStrings:DefaultConnection must be configured via User Secrets, " +
                "environment variables, or a non-committed appsettings file. See README.");
        }

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName)));

        services.AddScoped<IAccountRepository, AccountRepository>();
        services.AddScoped<ICardRepository, CardRepository>();
        services.AddScoped<ILedgerEntryRepository, LedgerEntryRepository>();
        services.AddScoped<ITransactionRecordRepository, TransactionRecordRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IAccountRequestRepository, AccountRequestRepository>();
        services.AddScoped<ICardRequestRepository, CardRequestRepository>();
        services.AddScoped<ITopUpRequestRepository, TopUpRequestRepository>();
        services.AddScoped<IBudgetCategoryRepository, BudgetCategoryRepository>();
        services.AddScoped<ICategoryEligibilityRepository, CategoryEligibilityRepository>();
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        services.AddScoped<IAccountNumberGenerator, AccountNumberGenerator>();
        services.AddScoped<ICardNumberGenerator, CardNumberGenerator>();
        services.AddSingleton<ICardNumberHasher, CardNumberHasher>();

        services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();
        services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateIssuerSigningKey = true,
                    ValidateLifetime = true,
                    ValidIssuer = jwt.Issuer,
                    ValidAudience = jwt.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Secret)),
                    // Short-lived E2E tokens must not be kept alive by the default 5m/1m skew.
                    ClockSkew = jwt.AccessTokenSeconds > 0 ? TimeSpan.Zero : TimeSpan.FromMinutes(1),
                    RoleClaimType = System.Security.Claims.ClaimTypes.Role
                };
            });

        services.AddAuthorization();

        return services;
    }
}
