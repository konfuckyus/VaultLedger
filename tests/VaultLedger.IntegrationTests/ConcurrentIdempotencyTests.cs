using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using VaultLedger.Application.Interfaces.Services;
using VaultLedger.Domain.Enums;
using VaultLedger.Infrastructure.Persistence;
using VaultLedger.IntegrationTests.Fixtures;
using VaultLedger.IntegrationTests.Helpers;

namespace VaultLedger.IntegrationTests;

[Collection(PostgresCollection.Name)]
public sealed class ConcurrentIdempotencyTests
{
    private readonly PostgresFixture _fx;

    public ConcurrentIdempotencyTests(PostgresFixture fx) => _fx = fx;

    [Fact]
    public async Task TwoConcurrentSpends_SameIdempotencyKey_ChargeOnceOnly()
    {
        await _fx.ResetAsync();

        using var seedScope = _fx.CreateScope();
        var (_, account, card) = await TestDataSeeder.SeedUserWithAccountAndCardAsync(seedScope, 100m);
        var accountId = account.Id;
        var cardId = card.Id;
        const string key = "idempotent-concurrent-spend";

        using var scope1 = _fx.CreateScope();
        using var scope2 = _fx.CreateScope();
        var svc1 = scope1.ServiceProvider.GetRequiredService<ITransactionService>();
        var svc2 = scope2.ServiceProvider.GetRequiredService<ITransactionService>();

        var task1 = SafeSpendAsync(svc1, accountId, cardId, 40m, key);
        var task2 = SafeSpendAsync(svc2, accountId, cardId, 40m, key);
        var outcomes = await Task.WhenAll(task1, task2);

        var successes = outcomes.Where(o => o.Record is not null).Select(o => o.Record!).ToList();
        successes.Should().NotBeEmpty("at least one call must complete");
        successes.Select(r => r.Id).Distinct().Should().HaveCount(1, "same completed TransactionRecord");

        using var assertScope = _fx.CreateScope();
        var db = assertScope.ServiceProvider.GetRequiredService<AppDbContext>();

        var reloaded = await db.Accounts.AsNoTracking().SingleAsync(a => a.Id == accountId);
        reloaded.Balance.Should().Be(60m, "only one 40 spend may apply");

        var completed = await db.TransactionRecords.AsNoTracking()
            .CountAsync(t => t.IdempotencyKey == key && t.Status == TransactionStatus.Completed);
        completed.Should().Be(1);

        var debitRows = await db.LedgerEntries.AsNoTracking()
            .CountAsync(e => e.IdempotencyKey == $"{key}:debit");
        debitRows.Should().Be(1);
    }

    private static async Task<(Domain.Entities.TransactionRecord? Record, Exception? Error)> SafeSpendAsync(
        ITransactionService svc,
        Guid accountId,
        Guid cardId,
        decimal amount,
        string key)
    {
        try
        {
            var record = await svc.SpendAsync(accountId, cardId, amount, key);
            return (record, null);
        }
        catch (Exception ex)
        {
            return (null, ex);
        }
    }
}
