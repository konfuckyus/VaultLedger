namespace VaultLedger.Application.DTOs.Auth;

public sealed class SetTransactionPinDto
{
    public string Pin { get; set; } = string.Empty;
    public string? OldPin { get; set; }
}
