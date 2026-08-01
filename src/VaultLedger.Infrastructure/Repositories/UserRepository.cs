using Microsoft.EntityFrameworkCore;
using VaultLedger.Application.Interfaces.Repositories;
using VaultLedger.Domain.Entities;
using VaultLedger.Infrastructure.Persistence;

namespace VaultLedger.Infrastructure.Repositories;

public sealed class UserRepository : Repository<User>, IUserRepository
{
    public UserRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalized = email.Trim().ToLowerInvariant();
        return await DbSet.FirstOrDefaultAsync(x => x.Email == normalized, cancellationToken);
    }

    public async Task<IReadOnlyList<User>> SearchAsync(
        string search,
        int take = 20,
        CancellationToken cancellationToken = default)
    {
        if (take < 1) take = 20;
        if (take > 50) take = 50;

        var term = search.Trim();
        if (string.IsNullOrWhiteSpace(term))
            return [];

        return await DbSet
            .AsNoTracking()
            .Where(x => x.IsActive
                        && (x.Email.Contains(term) || x.FullName.Contains(term)))
            .OrderBy(x => x.FullName)
            .Take(take)
            .ToListAsync(cancellationToken);
    }
}
