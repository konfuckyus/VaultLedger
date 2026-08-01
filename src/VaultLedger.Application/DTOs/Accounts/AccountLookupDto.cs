namespace VaultLedger.Application.DTOs.Accounts;

/// <summary>
/// Limited account info for peer lookup (e.g. transfer destination). No balance.
/// </summary>
public sealed class AccountLookupDto
{
    public Guid Id { get; set; }
    public string AccountNumber { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string OwnerDisplayName { get; set; } = string.Empty;
}
