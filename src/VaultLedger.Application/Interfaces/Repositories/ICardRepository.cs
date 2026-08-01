using VaultLedger.Domain.Entities;

namespace VaultLedger.Application.Interfaces.Repositories;

public interface ICardRepository : IRepository<Card>
{
    Task<IReadOnlyList<Card>> GetByAccountIdAsync(Guid accountId, CancellationToken cancellationToken = default);

    Task<Card?> GetByCardNumberHashAsync(string hash, CancellationToken cancellationToken = default);

    Task<bool> ExistsByCardNumberHashAsync(string hash, CancellationToken cancellationToken = default);
}
