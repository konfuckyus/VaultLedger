using Microsoft.Extensions.DependencyInjection;
using VaultLedger.Application.Interfaces;
using VaultLedger.Application.Interfaces.Services;
using VaultLedger.Domain.Common;
using VaultLedger.Domain.Entities;
using VaultLedger.Domain.Enums;
using VaultLedger.Infrastructure.Persistence;
using VaultLedger.IntegrationTests.Fixtures;

namespace VaultLedger.IntegrationTests.Helpers;

public static class TestDataSeeder
{
    public const string DefaultTransactionPin = "1234";

    public static async Task<(User User, Account Account)> SeedUserWithAccountAsync(
        IServiceScope scope,
        decimal openingBalance,
        string? email = null,
        Guid? categoryId = null,
        bool isTransferable = true)
    {
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

        var user = User.Create(
            "IT User",
            email ?? $"user-{Guid.NewGuid():N}@test.local",
            hasher.Hash("Password123!"),
            UserRole.User);
        user.SetTransactionPinHash(hasher.Hash(DefaultTransactionPin));

        await uow.Users.AddAsync(user);
        await uow.SaveChangesAsync();

        var account = Account.Create(
            user.Id,
            Random.Shared.NextInt64(1_000_000_000L, 10_000_000_000L).ToString(),
            categoryId ?? SystemBudgetCategories.GenelId,
            isTransferable);
        if (openingBalance > 0m)
            account.Credit(openingBalance);

        await uow.Accounts.AddAsync(account);
        await uow.SaveChangesAsync();

        return (user, account);
    }

    public static async Task<(User User, Account Account, Card Card)> SeedUserWithAccountAndCardAsync(
        IServiceScope scope,
        decimal openingBalance,
        string? email = null)
    {
        var (user, account) = await SeedUserWithAccountAsync(scope, openingBalance, email);
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var card = Card.Issue(
            account.Id,
            $"hash-{Guid.NewGuid():N}",
            "4242",
            DateTime.UtcNow.AddYears(3));
        await uow.Cards.AddAsync(card);
        await uow.SaveChangesAsync();

        return (user, account, card);
    }

    public static async Task TopUpViaDbAsync(IServiceScope scope, Guid accountId, decimal amount)
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var account = await db.Accounts.FindAsync(accountId)
            ?? throw new InvalidOperationException("Account not found.");
        account.Credit(amount);
        await db.SaveChangesAsync();
    }
}

public sealed class ThrowingTransactionFailureGate : ITransactionFailureGate
{
    public Task BeforeCommitAsync(CancellationToken cancellationToken = default)
        => throw new InvalidOperationException("Simulated failure after debit/credit staging (before commit).");
}
