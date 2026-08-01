namespace VaultLedger.Application.Interfaces.Services;

public sealed record GeneratedCardNumber(
    string CardNumberHash,
    string LastFourDigits,
    string RawCardNumber);

public interface ICardNumberGenerator
{
    /// <summary>
    /// Produces a unique card number hash (HMAC-SHA256) and last-four digits via a cryptographically secure RNG.
    /// </summary>
    Task<GeneratedCardNumber> GenerateUniqueAsync(CancellationToken cancellationToken = default);
}
