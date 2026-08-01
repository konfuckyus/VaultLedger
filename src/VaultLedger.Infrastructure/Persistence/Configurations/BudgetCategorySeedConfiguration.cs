using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VaultLedger.Domain.Common;

namespace VaultLedger.Infrastructure.Persistence.Configurations;

public sealed class BudgetCategorySeedConfiguration
    : IEntityTypeConfiguration<Domain.Entities.BudgetCategoryDefinition>
{
    private static readonly DateTime SeedCreatedAt =
        new(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public void Configure(EntityTypeBuilder<Domain.Entities.BudgetCategoryDefinition> builder)
    {
        builder.HasData(
            new
            {
                Id = SystemBudgetCategories.GenelId,
                Name = "Genel",
                DefaultAllocatedAmount = 0m,
                IsTransferable = true,
                IsSelfRequestable = true,
                IsActive = true,
                IsSystemDefault = true,
                CreatedAt = SeedCreatedAt
            },
            new
            {
                Id = SystemBudgetCategories.YemekId,
                Name = "Yemek",
                DefaultAllocatedAmount = 250m,
                IsTransferable = false,
                IsSelfRequestable = true,
                IsActive = true,
                IsSystemDefault = false,
                CreatedAt = SeedCreatedAt
            },
            new
            {
                Id = SystemBudgetCategories.KahveCayId,
                Name = "Kahve/Çay",
                DefaultAllocatedAmount = 100m,
                IsTransferable = false,
                IsSelfRequestable = true,
                IsActive = true,
                IsSystemDefault = false,
                CreatedAt = SeedCreatedAt
            });
    }
}
