using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VaultLedger.Domain.Common;
using VaultLedger.Domain.Enums;

namespace VaultLedger.Infrastructure.Persistence.Configurations;

public sealed class SystemClearingAccountSeedConfiguration : IEntityTypeConfiguration<Domain.Entities.Account>
{
    private static readonly DateTime SeedCreatedAt =
        new(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public void Configure(EntityTypeBuilder<Domain.Entities.Account> builder)
    {
        builder.HasData(new
        {
            Id = SystemAccounts.ClearingAccountId,
            UserId = SystemAccounts.SystemUserId,
            AccountNumber = SystemAccounts.ClearingAccountNumber,
            Balance = 0m,
            Currency = SystemAccounts.ClearingAccountCurrency,
            AccountType = AccountType.System,
            Status = AccountStatus.Active,
            CategoryId = (Guid?)null,
            IsTransferable = false,
            CreatedAt = SeedCreatedAt
            // RowVersion (xmin) is system-generated — do not seed
        });
    }
}
