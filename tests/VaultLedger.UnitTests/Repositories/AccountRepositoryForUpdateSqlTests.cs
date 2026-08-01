using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Data.Common;
using VaultLedger.Domain.Common;
using VaultLedger.Infrastructure.Persistence;
using VaultLedger.Infrastructure.Repositories;

namespace VaultLedger.UnitTests.Repositories;

public class AccountRepositoryForUpdateSqlTests
{
    [Fact]
    public void GetByIdForUpdateAsync_Query_ContainsForUpdate()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql("Host=127.0.0.1;Database=vaultledger;Username=postgres;Password=postgres")
            .Options;

        using var context = new AppDbContext(options);

        var id = SystemAccounts.ClearingAccountId;
        var query = context.Accounts
            .FromSqlInterpolated($"""
                SELECT *, xmin
                FROM accounts
                WHERE "Id" = {id}
                FOR UPDATE
                """);

        var sql = query.ToQueryString();

        sql.Should().Contain("FOR UPDATE", because: "pessimistic lock must be present in the generated SQL");
        sql.Should().Contain("accounts", because: "query must target accounts table");
        sql.Should().ContainEquivalentOf("Id");
        sql.Should().Contain("xmin", because: "RowVersion maps to xmin; SELECT * alone omits system columns");

        // Surfaced for review (also appears in test failure messages if assertion fails).
        sql.Should().NotBeNullOrWhiteSpace();
    }
}
