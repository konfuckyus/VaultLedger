using VaultLedger.Application.DTOs.Auth;
using VaultLedger.Application.Exceptions;
using VaultLedger.Application.Interfaces;
using VaultLedger.Application.Interfaces.Services;
using VaultLedger.Domain.Entities;
using VaultLedger.Domain.Enums;
using VaultLedger.Domain.Exceptions;

namespace VaultLedger.Application.Services;

public sealed class AuthService : IAuthService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;

    public AuthService(
        IUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator)
    {
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
    }

    public async Task<AuthResponseDto> RegisterAsync(
        RegisterRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var existing = await _unitOfWork.Users.GetByEmailAsync(email, cancellationToken);
        if (existing is not null)
            throw new InvalidAccountOperationException("Email is already registered.");

        var user = User.Create(
            request.FullName,
            email,
            _passwordHasher.Hash(request.Password),
            UserRole.User);

        await _unitOfWork.Users.AddAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return await IssueTokensAsync(user, cancellationToken);
    }

    public async Task<AuthResponseDto> LoginAsync(
        LoginRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await _unitOfWork.Users.GetByEmailAsync(email, cancellationToken);

        if (user is null || !_passwordHasher.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedException("Invalid email or password.");

        // Mandatory: inactive users (incl. system@vaultledger.internal) cannot authenticate.
        if (!user.IsActive)
            throw new UnauthorizedException("User account is inactive.");

        return await IssueTokensAsync(user, cancellationToken);
    }

    public async Task<AuthResponseDto> RefreshAsync(
        RefreshTokenRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var hash = _jwtTokenGenerator.HashRefreshToken(request.RefreshToken);
        var stored = await _unitOfWork.RefreshTokens.GetByTokenHashAsync(hash, cancellationToken);

        if (stored is null)
            throw new UnauthorizedException("Invalid or expired refresh token.");

        // Reuse of an already-rotated (revoked) token → treat as theft: revoke all sessions.
        if (stored.IsRevoked)
        {
            await _unitOfWork.RefreshTokens.RevokeAllForUserAsync(stored.UserId, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            throw new UnauthorizedException("Refresh token reuse detected. All sessions have been revoked.");
        }

        if (!stored.IsActive)
            throw new UnauthorizedException("Invalid or expired refresh token.");

        var user = await _unitOfWork.Users.GetByIdAsync(stored.UserId, cancellationToken)
            ?? throw new UnauthorizedException("Invalid or expired refresh token.");

        if (!user.IsActive)
            throw new UnauthorizedException("User account is inactive.");

        // Rotation: revoke presented token and issue a new pair in one SaveChanges.
        stored.Revoke();
        _unitOfWork.RefreshTokens.Update(stored);
        return await IssueTokensAsync(user, cancellationToken);
    }

    public async Task LogoutAsync(
        RefreshTokenRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var hash = _jwtTokenGenerator.HashRefreshToken(request.RefreshToken);
        var stored = await _unitOfWork.RefreshTokens.GetByTokenHashAsync(hash, cancellationToken);
        if (stored is null)
            return;

        stored.Revoke();
        _unitOfWork.RefreshTokens.Update(stored);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<MeDto> GetMeAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken)
            ?? throw new NotFoundException(nameof(User), userId);

        return new MeDto
        {
            UserId = user.Id,
            Email = user.Email,
            FullName = user.FullName,
            Role = user.Role.ToString(),
            HasTransactionPin = user.HasTransactionPin
        };
    }

    public async Task SetTransactionPinAsync(
        Guid userId,
        SetTransactionPinDto request,
        CancellationToken cancellationToken = default)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken)
            ?? throw new NotFoundException(nameof(User), userId);

        if (user.HasTransactionPin)
        {
            if (string.IsNullOrWhiteSpace(request.OldPin)
                || !_passwordHasher.Verify(request.OldPin, user.TransactionPinHash!))
            {
                throw new InvalidPinException("Mevcut işlem PIN'i hatalı.");
            }
        }

        user.SetTransactionPinHash(_passwordHasher.Hash(request.Pin));
        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task<AuthResponseDto> IssueTokensAsync(User user, CancellationToken cancellationToken)
    {
        var accessToken = _jwtTokenGenerator.GenerateAccessToken(user, out var accessExpiresAt);
        var (rawRefresh, refreshHash, refreshExpires) = _jwtTokenGenerator.GenerateRefreshToken();

        var refreshEntity = RefreshToken.Create(user.Id, refreshHash, refreshExpires);
        await _unitOfWork.RefreshTokens.AddAsync(refreshEntity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new AuthResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = rawRefresh,
            AccessTokenExpiresAtUtc = accessExpiresAt,
            UserId = user.Id,
            Email = user.Email,
            FullName = user.FullName,
            Role = user.Role.ToString()
        };
    }
}
