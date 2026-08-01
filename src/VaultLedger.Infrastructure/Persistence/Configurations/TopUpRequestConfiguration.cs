using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VaultLedger.Domain.Entities;
using VaultLedger.Domain.Enums;

namespace VaultLedger.Infrastructure.Persistence.Configurations;

public sealed class TopUpRequestConfiguration : IEntityTypeConfiguration<TopUpRequest>
{
    public void Configure(EntityTypeBuilder<TopUpRequest> builder)
    {
        builder.ToTable("topup_requests");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.UserId).IsRequired();
        builder.Property(x => x.AccountId).IsRequired();

        builder.Property(x => x.Amount)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(x => x.Note)
            .HasMaxLength(TopUpRequest.MaxNoteLength);

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(x => x.RequestedAt).IsRequired();
        builder.Property(x => x.ReviewedAt);
        builder.Property(x => x.ReviewedByUserId);

        builder.Property(x => x.RejectionReason)
            .HasMaxLength(500);

        builder.Property(x => x.ResultingTransactionRecordId);
        builder.Property(x => x.CreatedAt).IsRequired();

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

        builder.HasOne(x => x.ResultingTransactionRecord)
            .WithMany()
            .HasForeignKey(x => x.ResultingTransactionRecordId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => x.AccountId);

        builder.HasIndex(x => new { x.UserId, x.AccountId })
            .IsUnique()
            .HasFilter($"\"Status\" = '{nameof(RequestStatus.Pending)}'")
            .HasDatabaseName("IX_topup_requests_UserId_AccountId_Pending");
    }
}
