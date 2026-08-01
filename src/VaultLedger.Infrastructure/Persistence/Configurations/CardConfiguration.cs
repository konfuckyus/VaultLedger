using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VaultLedger.Domain.Entities;

namespace VaultLedger.Infrastructure.Persistence.Configurations;

public sealed class CardConfiguration : IEntityTypeConfiguration<Card>
{
    public void Configure(EntityTypeBuilder<Card> builder)
    {
        builder.ToTable("cards");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.AccountId)
            .IsRequired();

        builder.Property(x => x.CardNumberHash)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(x => x.LastFourDigits)
            .HasMaxLength(4)
            .IsFixedLength()
            .IsRequired();

        builder.Property(x => x.Label)
            .HasMaxLength(Card.MaxLabelLength);

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(x => x.IssuedAt)
            .IsRequired();

        builder.Property(x => x.ExpiresAt)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Ignore(x => x.MaskedNumber);

        builder.HasIndex(x => x.CardNumberHash)
            .IsUnique();

        builder.HasIndex(x => x.AccountId);
    }
}
