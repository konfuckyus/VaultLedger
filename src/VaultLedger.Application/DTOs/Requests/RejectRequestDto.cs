namespace VaultLedger.Application.DTOs.Requests;

public sealed class RejectRequestDto
{
    public string Reason { get; set; } = string.Empty;
}
