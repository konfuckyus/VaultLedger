namespace VaultLedger.Application.Common;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "VaultLedger";
    public string Audience { get; set; } = "VaultLedger";
    public string Secret { get; set; } = "CHANGE_ME_TO_A_LONG_RANDOM_SECRET_KEY_32+";
    public int AccessTokenMinutes { get; set; } = 15;

    /// <summary>
    /// When &gt; 0, overrides <see cref="AccessTokenMinutes"/> (used by E2E short-lived token tests).
    /// </summary>
    public int AccessTokenSeconds { get; set; }

    public int RefreshTokenDays { get; set; } = 7;
}
