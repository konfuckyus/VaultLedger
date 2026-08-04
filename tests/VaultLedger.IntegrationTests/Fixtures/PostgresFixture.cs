using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Respawn;
using Respawn.Graph;
using Testcontainers.PostgreSql;
using VaultLedger.Application;
using VaultLedger.Domain.Common;
using VaultLedger.Domain.Entities;
using VaultLedger.Domain.Enums;
using VaultLedger.Infrastructure;
using VaultLedger.Infrastructure.Persistence;

namespace VaultLedger.IntegrationTests.Fixtures;

public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("vaultledger_it")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    private Respawner? _respawner;

    public string ConnectionString => _container.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();

        using var conn = db.Database.GetDbConnection();
        await conn.OpenAsync();
        _respawner = await Respawner.CreateAsync(conn, new RespawnerOptions
        {
            DbAdapter = DbAdapter.Postgres,
            SchemasToInclude = ["public"],
            // Keep migration history and reference seed data (budget categories).
            TablesToIgnore =
            [
                new Table("__EFMigrationsHistory"),
                new Table("budget_category_definitions")
            ]
        });
    }

    public async Task ResetAsync()
    {
        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        using var conn = db.Database.GetDbConnection();
        await conn.OpenAsync();
        await _respawner!.ResetAsync(conn);

        await EnsureSystemClearingAsync(db);
    }

    private static async Task EnsureSystemClearingAsync(AppDbContext db)
    {
        if (!await db.Users.AnyAsync(u => u.Id == SystemAccounts.SystemUserId))
        {
            var systemUser = User.Create(
                "System Clearing",
                "system@vaultledger.internal",
                "SEED-SYSTEM-USER-NOT-FOR-LOGIN",
                UserRole.Admin);
            typeof(Domain.Common.BaseEntity)
                .GetProperty(nameof(Domain.Common.BaseEntity.Id))!
                .SetValue(systemUser, SystemAccounts.SystemUserId);
            systemUser.Deactivate();
            db.Users.Add(systemUser);
        }

        if (!await db.Accounts.AnyAsync(a => a.Id == SystemAccounts.ClearingAccountId))
        {
            db.Accounts.Add(Account.CreateSystemClearing(SystemAccounts.SystemUserId));
        }

        await db.SaveChangesAsync();
    }

    public IServiceScope CreateScope(Action<IServiceCollection>? configure = null)
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = ConnectionString,
                ["Jwt:Issuer"] = "VaultLedger",
                ["Jwt:Audience"] = "VaultLedger",
                ["Jwt:Secret"] = "VaultLedger_Integration_Test_Secret_Key_32chars!",
                ["Jwt:AccessTokenMinutes"] = "15",
                ["Jwt:RefreshTokenDays"] = "7",
                ["CardHash:Secret"] = "VaultLedger_Integration_CardHash_Secret_Key_32!"
            })
            .Build();

        services.AddSingleton<IConfiguration>(configuration);
        services.AddLogging();
        services.AddApplication();
        services.AddInfrastructure(configuration);

        // Allow tests to replace singletons registered by AddApplication (e.g. failure gate).
        if (configure is not null)
        {
            var overrides = new ServiceCollection();
            configure(overrides);
            foreach (var descriptor in overrides)
            {
                var existing = services.Where(d => d.ServiceType == descriptor.ServiceType).ToList();
                foreach (var d in existing)
                    services.Remove(d);

                ((IList<ServiceDescriptor>)services).Add(descriptor);
            }
        }

        return services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateScopes = true
        }).CreateScope();
    }

    public async Task DisposeAsync() => await _container.DisposeAsync();
}

[CollectionDefinition(Name)]
public sealed class PostgresCollection : ICollectionFixture<PostgresFixture>
{
    public const string Name = "postgres";
}
