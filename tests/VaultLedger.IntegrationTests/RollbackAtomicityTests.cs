using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using VaultLedger.Application.Interfaces.Services;
using VaultLedger.Infrastructure.Persistence;
using VaultLedger.IntegrationTests.Fixtures;
using VaultLedger.IntegrationTests.Helpers;

namespace VaultLedger.IntegrationTests;

[Collection(PostgresCollection.Name)]
public sealed class RollbackAtomicityTests
{
    private readonly PostgresFixture _fx;

    public RollbackAtomicityTests(PostgresFixture fx) => _fx = fx;

    [Fact]
    public async Task Spend_WhenFailureAfterLedgerStaging_LeavesNoCommittedRows_AndBalanceUnchanged()
    {
        await _fx.ResetAsync();

        using var seedScope = _fx.CreateScope();
        var (_, account, card) = await TestDataSeeder.SeedUserWithAccountAndCardAsync(seedScope, 80m);
        var accountId = account.Id;
        var cardId = card.Id;
        const string key = "rollback-spend-key";

        using var failScope = _fx.CreateScope(services =>
        {
            services.AddSingleton<ITransactionFailureGate, ThrowingTransactionFailureGate>();
        });

        var svc = failScope.ServiceProvider.GetRequiredService<ITransactionService>();

        var act = () => svc.SpendAsync(
            accountId, cardId, 25m, key, pin: TestDataSeeder.DefaultTransactionPin);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Simulated failure*");

        using var assertScope = _fx.CreateScope();
        var db = assertScope.ServiceProvider.GetRequiredService<AppDbContext>();

        var reloaded = await db.Accounts.AsNoTracking().SingleAsync(a => a.Id == accountId);
        reloaded.Balance.Should().Be(80m);

        var ledgerCount = await db.LedgerEntries.AsNoTracking()
            .CountAsync(e => e.IdempotencyKey.StartsWith(key));
        ledgerCount.Should().Be(0, "debit/credit ledger rows must not be committed");

        var completedTxn = await db.TransactionRecords.AsNoTracking()
            .CountAsync(t => t.IdempotencyKey == key && t.Status == Domain.Enums.TransactionStatus.Completed);
        completedTxn.Should().Be(0);
    }
}
