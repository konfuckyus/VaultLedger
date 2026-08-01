namespace VaultLedger.Application.DTOs.Requests;

public sealed class SubmitCardRequestDto
{
    public Guid AccountId { get; set; }
    public string? Label { get; set; }
}
