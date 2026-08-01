using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VaultLedger.Domain.Entities;
using VaultLedger.Domain.Enums;

namespace VaultLedger.Infrastructure.Persistence.Configurations;

public sealed class CardRequestConfiguration : IEntityTypeConfiguration<CardRequest>
{
    public void Configure(EntityTypeBuilder<CardRequest> builder)
    {
        builder.ToTable("card_requests");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.UserId)
            .IsRequired();

        builder.Property(x => x.AccountId)
            .IsRequired();

        builder.Property(x => x.Label)
            .HasMaxLength(Card.MaxLabelLength);

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(x => x.RequestedAt)
            .IsRequired();

        builder.Property(x => x.ReviewedAt);

        builder.Property(x => x.ReviewedByUserId);

        builder.Property(x => x.RejectionReason)
            .HasMaxLength(500);

        builder.Property(x => x.ResultingCardId);

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Account)
            .WithMany()
            .HasForeignKey(x => x.AccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.ReviewedByUser)
            .WithMany()
            .HasForeignKey(x => x.ReviewedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.ResultingCard)
            .WithMany()
            .HasForeignKey(x => x.ResultingCardId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => x.AccountId);

        // Domain rule: at most one pending card request per user+account
        builder.HasIndex(x => new { x.UserId, x.AccountId })
            .IsUnique()
            .HasFilter($"\"Status\" = '{nameof(RequestStatus.Pending)}'")
            .HasDatabaseName("IX_card_requests_UserId_AccountId_Pending");
    }
}
