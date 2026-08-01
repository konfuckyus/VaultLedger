using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VaultLedger.Domain.Entities;
using VaultLedger.Domain.Enums;

namespace VaultLedger.Infrastructure.Persistence.Configurations;

public sealed class AccountConfiguration : IEntityTypeConfiguration<Account>
{
    public void Configure(EntityTypeBuilder<Account> builder)
    {
        builder.ToTable("accounts");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.UserId)
            .IsRequired();

        builder.Property(x => x.AccountNumber)
            .HasMaxLength(Account.AccountNumberLength)
            .IsFixedLength()
            .IsRequired();

        builder.Property(x => x.Balance)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(x => x.Currency)
            .HasMaxLength(3)
            .IsRequired();

        builder.Property(x => x.AccountType)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(x => x.CategoryId);

        builder.Property(x => x.IsTransferable)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        // PostgreSQL xmin system column — optimistic concurrency
        builder.Property(x => x.RowVersion)
            .IsRowVersion();

        builder.HasOne(x => x.Category)
            .WithMany()
            .HasForeignKey(x => x.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Cards)
            .WithOne(x => x.Account)
            .HasForeignKey(x => x.AccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.LedgerEntries)
            .WithOne(x => x.Account)
            .HasForeignKey(x => x.AccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Navigation(x => x.Cards)
            .HasField("_cards")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Navigation(x => x.LedgerEntries)
            .HasField("_ledgerEntries")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(x => x.UserId);

        builder.HasIndex(x => x.AccountNumber)
            .IsUnique();

        // At most one user account per budget category
        builder.HasIndex(x => new { x.UserId, x.CategoryId })
            .IsUnique()
            .HasFilter("\"CategoryId\" IS NOT NULL")
            .HasDatabaseName("IX_accounts_UserId_CategoryId");

        // At most one system clearing account in the closed loop
        builder.HasIndex(x => x.AccountType)
            .IsUnique()
            .HasFilter($"\"AccountType\" = '{nameof(AccountType.System)}'");
    }
}
