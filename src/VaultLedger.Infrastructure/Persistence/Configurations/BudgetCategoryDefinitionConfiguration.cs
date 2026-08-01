using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VaultLedger.Domain.Entities;

namespace VaultLedger.Infrastructure.Persistence.Configurations;

public sealed class BudgetCategoryDefinitionConfiguration
    : IEntityTypeConfiguration<BudgetCategoryDefinition>
{
    public void Configure(EntityTypeBuilder<BudgetCategoryDefinition> builder)
    {
        builder.ToTable("budget_category_definitions");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .HasMaxLength(BudgetCategoryDefinition.MaxNameLength)
            .IsRequired();

        builder.HasIndex(x => x.Name)
            .IsUnique();

        builder.Property(x => x.DefaultAllocatedAmount)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(x => x.IsTransferable)
            .IsRequired();

        builder.Property(x => x.IsSelfRequestable)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(x => x.IsActive)
            .IsRequired();

        builder.Property(x => x.IsSystemDefault)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .IsRequired();
    }
}
