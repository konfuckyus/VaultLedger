namespace VaultLedger.Application.DTOs.Accounts;

public sealed class CreateAccountRequestDto
{
    public Guid UserId { get; set; }
    public Guid? CategoryId { get; set; }
    public string? Currency { get; set; }
}
