using System.Security.Cryptography;
using VaultLedger.Application.Exceptions;
using VaultLedger.Application.Interfaces.Repositories;
using VaultLedger.Application.Interfaces.Services;

namespace VaultLedger.Infrastructure.Services;

public sealed class CardNumberGenerator : ICardNumberGenerator
{
    public const int MaxAttempts = 5;
    public const int PanLength = 16;

    private readonly ICardRepository _cards;
    private readonly ICardNumberHasher _hasher;

    public CardNumberGenerator(ICardRepository cards, ICardNumberHasher hasher)
    {
        _cards = cards;
        _hasher = hasher;
    }

    public async Task<GeneratedCardNumber> GenerateUniqueAsync(CancellationToken cancellationToken = default)
    {
        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            var pan = GeneratePan();
            var hash = _hasher.Hash(pan);
            var exists = await _cards.ExistsByCardNumberHashAsync(hash, cancellationToken);
            if (!exists)
                return new GeneratedCardNumber(hash, _hasher.LastFour(pan), pan);
        }

        throw new CardNumberGenerationException(
            $"Failed to generate a unique card number after {MaxAttempts} attempts.");
    }

    private static string GeneratePan()
    {
        Span<char> digits = stackalloc char[PanLength];
        Span<byte> bytes = stackalloc byte[PanLength];
        RandomNumberGenerator.Fill(bytes);

        // First digit 1-9 (no leading zero); remaining 0-9.
        digits[0] = (char)('1' + (bytes[0] % 9));
        for (var i = 1; i < PanLength; i++)
            digits[i] = (char)('0' + (bytes[i] % 10));

        return new string(digits);
    }
}
