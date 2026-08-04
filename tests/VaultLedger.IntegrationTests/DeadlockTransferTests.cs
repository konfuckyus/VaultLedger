using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using VaultLedger.Application.Interfaces.Services;
using VaultLedger.Infrastructure.Persistence;
using VaultLedger.IntegrationTests.Fixtures;
using VaultLedger.IntegrationTests.Helpers;

namespace VaultLedger.IntegrationTests;

[Collection(PostgresCollection.Name)]
public sealed class DeadlockTransferTests
{
    private const int Iterations = 20;
    private readonly PostgresFixture _fx;

    public DeadlockTransferTests(PostgresFixture fx) => _fx = fx;

    [Fact]
    public async Task ConcurrentOppositeTransfers_TwentyIterations_NoDeadlock_BalancesCorrect()
    {
        var deadlocks = 0;
        var failures = new List<string>();

        for (var i = 0; i < Iterations; i++)
        {
            await _fx.ResetAsync();

            using var seedScope = _fx.CreateScope();
            var (_, accountA) = await TestDataSeeder.SeedUserWithAccountAsync(
                seedScope, 100m, $"a-{i}@test.local");
            var (_, accountB) = await TestDataSeeder.SeedUserWithAccountAsync(
                seedScope, 100m, $"b-{i}@test.local");

            using var scope1 = _fx.CreateScope();
            using var scope2 = _fx.CreateScope();
            var svc1 = scope1.ServiceProvider.GetRequiredService<ITransactionService>();
            var svc2 = scope2.ServiceProvider.GetRequiredService<ITransactionService>();

            try
            {
                var t1 = svc1.TransferAsync(
                    accountA.Id, accountB.Id, 10m, $"xfer-a2b-{i}-{Guid.NewGuid():N}",
                    pin: TestDataSeeder.DefaultTransactionPin);
                var t2 = svc2.TransferAsync(
                    accountB.Id, accountA.Id, 15m, $"xfer-b2a-{i}-{Guid.NewGuid():N}",
                    pin: TestDataSeeder.DefaultTransactionPin);

                await Task.WhenAll(t1, t2);
            }
            catch (Exception ex) when (IsDeadlock(ex))
            {
                deadlocks++;
                failures.Add($"iter {i}: deadlock — {ex.Message}");
                continue;
            }
            catch (Exception ex)
            {
                failures.Add($"iter {i}: {ex.GetType().Name}: {ex.Message}");
                continue;
            }

            using var assertScope = _fx.CreateScope();
            var db = assertScope.ServiceProvider.GetRequiredService<AppDbContext>();
            var a = await db.Accounts.AsNoTracking().SingleAsync(x => x.Id == accountA.Id);
            var b = await db.Accounts.AsNoTracking().SingleAsync(x => x.Id == accountB.Id);

            // A: 100 - 10 + 15 = 105; B: 100 + 10 - 15 = 95
            a.Balance.Should().Be(105m, $"iteration {i}");
            b.Balance.Should().Be(95m, $"iteration {i}");
        }

        deadlocks.Should().Be(0, $"deadlocks observed: {string.Join("; ", failures)}");
        failures.Should().BeEmpty($"unexpected failures: {string.Join("; ", failures)}");
    }

    private static bool IsDeadlock(Exception ex)
    {
        for (Exception? e = ex; e is not null; e = e.InnerException)
        {
            if (e is PostgresException pg && pg.SqlState == PostgresErrorCodes.DeadlockDetected)
                return true;
            if (e.Message.Contains("40P01", StringComparison.OrdinalIgnoreCase)
                || e.Message.Contains("deadlock detected", StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }
}
