namespace VaultLedger.Application.Interfaces.Services;

public interface IAccountNumberGenerator
{
    /// <summary>
    /// Produces a unique 10-digit account number using a cryptographically secure RNG.
    /// </summary>
    Task<string> GenerateUniqueAsync(CancellationToken cancellationToken = default);
}
