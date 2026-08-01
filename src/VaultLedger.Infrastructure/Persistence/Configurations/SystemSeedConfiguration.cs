using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VaultLedger.Domain.Common;
using VaultLedger.Domain.Enums;

namespace VaultLedger.Infrastructure.Persistence.Configurations;

/// <summary>
/// Seeds the technical system user (owner of the System Clearing Account).
/// Login is blocked because <c>IsActive = false</c>; AuthService rejects inactive users with 401.
/// </summary>
public sealed class SystemSeedConfiguration : IEntityTypeConfiguration<Domain.Entities.User>
{
    private static readonly DateTime SeedCreatedAt =
        new(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public void Configure(EntityTypeBuilder<Domain.Entities.User> builder)
    {
        builder.HasData(new
        {
            Id = SystemAccounts.SystemUserId,
            FullName = "System Clearing",
            Email = "system@vaultledger.internal",
            PasswordHash = "SEED-SYSTEM-USER-NOT-FOR-LOGIN",
            Role = UserRole.Admin,
            // Must never pass login; enforce IsActive check in Auth service (see remarks).
            IsActive = false,
            CreatedAt = SeedCreatedAt
        });
    }
}
