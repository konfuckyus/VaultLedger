using VaultLedger.Domain.Entities;

namespace VaultLedger.Application.Interfaces.Repositories;

public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<User>> SearchAsync(
        string search,
        int take = 20,
        CancellationToken cancellationToken = default);
}
