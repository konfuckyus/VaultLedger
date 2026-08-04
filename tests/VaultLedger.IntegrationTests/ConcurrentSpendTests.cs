using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using VaultLedger.Application.Interfaces.Services;
using VaultLedger.Domain.Common;
using VaultLedger.Infrastructure.Persistence;
using VaultLedger.IntegrationTests.Fixtures;
using VaultLedger.IntegrationTests.Helpers;

namespace VaultLedger.IntegrationTests;

[Collection(PostgresCollection.Name)]
public sealed class ConcurrentSpendTests
{
    private readonly PostgresFixture _fx;

    public ConcurrentSpendTests(PostgresFixture fx) => _fx = fx;

    [Fact]
    public async Task TwoConcurrentSpends_OnSameAccount_BothSucceed_BalanceCorrect_NoLostUpdate()
    {
        await _fx.ResetAsync();

        using var seedScope = _fx.CreateScope();
        var (_, account, card) = await TestDataSeeder.SeedUserWithAccountAndCardAsync(seedScope, openingBalance: 100m);
        var accountId = account.Id;
        var cardId = card.Id;

        using var scope1 = _fx.CreateScope();
        using var scope2 = _fx.CreateScope();
        var svc1 = scope1.ServiceProvider.GetRequiredService<ITransactionService>();
        var svc2 = scope2.ServiceProvider.GetRequiredService<ITransactionService>();

        var t1 = svc1.SpendAsync(accountId, cardId, 30m, $"spend-concurrent-{Guid.NewGuid():N}-a",
            pin: TestDataSeeder.DefaultTransactionPin);
        var t2 = svc2.SpendAsync(accountId, cardId, 20m, $"spend-concurrent-{Guid.NewGuid():N}-b",
            pin: TestDataSeeder.DefaultTransactionPin);

        var results = await Task.WhenAll(t1, t2);
        results.Should().HaveCount(2);
        results.Should().OnlyContain(r => r.Status == Domain.Enums.TransactionStatus.Completed);
        results.Should().OnlyContain(r => r.CardId == cardId);

        using var assertScope = _fx.CreateScope();
        var db = assertScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var reloaded = await db.Accounts.AsNoTracking().SingleAsync(a => a.Id == accountId);
        reloaded.Balance.Should().Be(50m, "100 - 30 - 20 with FOR UPDATE serialization");

        var userDebits = await db.LedgerEntries.AsNoTracking()
            .CountAsync(e => e.AccountId == accountId && e.EntryType == Domain.Enums.EntryType.Debit);
        userDebits.Should().Be(2);
    }
}
