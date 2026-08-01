namespace VaultLedger.Application.DTOs.Accounts;

public sealed class AccountDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string AccountNumber { get; set; } = string.Empty;
    public decimal Balance { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string AccountType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public Guid? CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public bool IsTransferable { get; set; }

    /// <summary>Owner identity — populated for admin lookups; optional on /me.</summary>
    public string? OwnerFullName { get; set; }

    public string? OwnerEmail { get; set; }
}
