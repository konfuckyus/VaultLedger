using VaultLedger.Domain.Common;
using VaultLedger.Domain.Entities;

namespace VaultLedger.UnitTests.Helpers;

internal static class TestAccounts
{
    public static Account CreateUser(
        Guid userId,
        string accountNumber,
        Guid? categoryId = null,
        bool isTransferable = true)
        => Account.Create(
            userId,
            accountNumber,
            categoryId ?? SystemBudgetCategories.GenelId,
            isTransferable);
}
