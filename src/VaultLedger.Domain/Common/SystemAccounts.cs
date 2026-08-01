namespace VaultLedger.Domain.Common;

/// <summary>
/// Well-known identifiers for the house/clearing side of the closed-loop ledger.
/// TopUp and Refund always use <see cref="ClearingAccountId"/> as the counterparty.
/// </summary>
public static class SystemAccounts
{
    /// <summary>Technical owner of the system clearing account (not for interactive login).</summary>
    public static readonly Guid SystemUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    /// <summary>Single System Clearing Account used as double-entry counterparty for TopUp/Refund.</summary>
    public static readonly Guid ClearingAccountId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    /// <summary>Well-known 10-digit account number for the system clearing account.</summary>
    public const string ClearingAccountNumber = "0000000001";

    public const string ClearingAccountCurrency = "TRY";
}
