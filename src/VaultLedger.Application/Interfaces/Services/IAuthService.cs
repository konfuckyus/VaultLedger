using VaultLedger.Application.DTOs.Auth;

namespace VaultLedger.Application.Interfaces.Services;

public interface IAuthService
{
    Task<AuthResponseDto> RegisterAsync(RegisterRequestDto request, CancellationToken cancellationToken = default);
    Task<AuthResponseDto> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default);
    Task<AuthResponseDto> RefreshAsync(RefreshTokenRequestDto request, CancellationToken cancellationToken = default);
    Task LogoutAsync(RefreshTokenRequestDto request, CancellationToken cancellationToken = default);
    Task<MeDto> GetMeAsync(Guid userId, CancellationToken cancellationToken = default);
    Task SetTransactionPinAsync(
        Guid userId,
        SetTransactionPinDto request,
        CancellationToken cancellationToken = default);
}
