namespace VaultLedger.Application.Interfaces.Services;

/// <summary>Hashes card PANs with HMAC-SHA256; never store plaintext.</summary>
public interface ICardNumberHasher
{
    string Hash(string cardNumber);
    string LastFour(string cardNumber);
}
