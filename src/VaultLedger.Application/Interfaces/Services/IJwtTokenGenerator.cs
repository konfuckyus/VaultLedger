using VaultLedger.Domain.Entities;

namespace VaultLedger.Application.Interfaces.Services;

public interface IJwtTokenGenerator
{
    string GenerateAccessToken(User user, out DateTime expiresAtUtc);
    (string RawToken, string TokenHash, DateTime ExpiresAt) GenerateRefreshToken();
    string HashRefreshToken(string rawToken);
}
