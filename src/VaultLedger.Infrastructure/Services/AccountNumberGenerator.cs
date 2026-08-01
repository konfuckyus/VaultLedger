using System.Security.Cryptography;
using VaultLedger.Application.Exceptions;
using VaultLedger.Application.Interfaces.Repositories;
using VaultLedger.Application.Interfaces.Services;

namespace VaultLedger.Infrastructure.Services;

public sealed class AccountNumberGenerator : IAccountNumberGenerator
{
    public const int MaxAttempts = 5;

    private readonly IAccountRepository _accounts;

    public AccountNumberGenerator(IAccountRepository accounts)
    {
        _accounts = accounts;
    }

    public async Task<string> GenerateUniqueAsync(CancellationToken cancellationToken = default)
    {
        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            var candidate = GenerateCandidate();
            var exists = await _accounts.ExistsByAccountNumberAsync(candidate, cancellationToken);
            if (!exists)
                return candidate;
        }

        throw new AccountNumberGenerationException(
            $"Failed to generate a unique account number after {MaxAttempts} attempts.");
    }

    private static string GenerateCandidate()
    {
        // 10-digit range: 1_000_000_000 .. 9_999_999_999 (no leading zero).
        Span<byte> bytes = stackalloc byte[8];
        RandomNumberGenerator.Fill(bytes);
        var value = BitConverter.ToUInt64(bytes);
        var number = 1_000_000_000UL + (value % 9_000_000_000UL);
        return number.ToString();
    }
}
