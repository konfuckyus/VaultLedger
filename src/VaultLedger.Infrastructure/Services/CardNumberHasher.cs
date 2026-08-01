using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using VaultLedger.Application.Common;
using VaultLedger.Application.Interfaces.Services;

namespace VaultLedger.Infrastructure.Services;

public sealed class CardNumberHasher : ICardNumberHasher
{
    private readonly byte[] _key;

    public CardNumberHasher(IOptions<CardHashOptions> options)
    {
        var secret = options.Value.Secret
            ?? throw new InvalidOperationException("CardHash:Secret is not configured.");

        if (string.IsNullOrWhiteSpace(secret) || secret.Length < 32)
        {
            throw new InvalidOperationException(
                "CardHash:Secret must be configured (min 32 chars) via User Secrets, environment variables, " +
                "or a non-committed appsettings.*.json file. See README.");
        }

        _key = Encoding.UTF8.GetBytes(secret);
    }

    public string Hash(string cardNumber)
    {
        var digits = DigitsOnly(cardNumber);
        var bytes = HMACSHA256.HashData(_key, Encoding.UTF8.GetBytes(digits));
        return Convert.ToHexString(bytes);
    }

    public string LastFour(string cardNumber)
    {
        var digits = DigitsOnly(cardNumber);
        if (digits.Length < 4)
            throw new ArgumentException("Card number must contain at least 4 digits.", nameof(cardNumber));

        return digits[^4..];
    }

    private static string DigitsOnly(string cardNumber)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cardNumber);
        return new string(cardNumber.Where(char.IsDigit).ToArray());
    }
}
