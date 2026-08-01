namespace VaultLedger.Application.Common;

public sealed class CardHashOptions
{
    public const string SectionName = "CardHash";

    /// <summary>HMAC-SHA256 key used when hashing card PANs. Never store plaintext PANs.</summary>
    public string Secret { get; set; } = string.Empty;
}
