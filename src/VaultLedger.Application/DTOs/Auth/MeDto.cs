namespace VaultLedger.Application.DTOs.Auth;

public sealed class MeDto
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool HasTransactionPin { get; set; }
}
