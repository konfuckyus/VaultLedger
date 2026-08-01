using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VaultLedger.Domain.Entities;

namespace VaultLedger.Infrastructure.Persistence.Configurations;

public sealed class CategoryEligibilityConfiguration : IEntityTypeConfiguration<CategoryEligibility>
{
    public void Configure(EntityTypeBuilder<CategoryEligibility> builder)
    {
        builder.ToTable("category_eligibilities");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.UserId).IsRequired();
        builder.Property(x => x.CategoryId).IsRequired();
        builder.Property(x => x.GrantedByAdminUserId).IsRequired();
        builder.Property(x => x.GrantedAt).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();

        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Category)
            .WithMany()
            .HasForeignKey(x => x.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.GrantedByAdminUser)
            .WithMany()
            .HasForeignKey(x => x.GrantedByAdminUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.UserId, x.CategoryId })
            .IsUnique()
            .HasDatabaseName("IX_category_eligibilities_UserId_CategoryId");

        builder.HasIndex(x => x.CategoryId);
    }
}
