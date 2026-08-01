namespace VaultLedger.Application.DTOs.Requests;

public sealed class SubmitTopUpRequestDto
{
    public Guid AccountId { get; set; }
    public decimal Amount { get; set; }
    public string? Note { get; set; }
}
