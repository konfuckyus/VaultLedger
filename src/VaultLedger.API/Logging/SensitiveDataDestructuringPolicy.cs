using Serilog.Core;
using Serilog.Events;

namespace VaultLedger.API.Logging;

/// <summary>
/// Prevents sensitive properties from appearing in structured Serilog output
/// when entities/DTOs are destructured (e.g. <c>{@user}</c>).
/// </summary>
public sealed class SensitiveDataDestructuringPolicy : IDestructuringPolicy
{
    private static readonly HashSet<string> SensitiveNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "Password",
        "PasswordHash",
        "CardNumber",
        "CardNumberHash",
        "RawCardNumber",
        "TokenHash",
        "RefreshToken",
        "AccessToken",
        "Authorization"
    };

    public bool TryDestructure(
        object value,
        ILogEventPropertyValueFactory propertyValueFactory,
        out LogEventPropertyValue result)
    {
        result = null!;
        var type = value.GetType();

        // Only rewrite our domain / application types — leave primitives and BCL alone.
        var ns = type.Namespace ?? string.Empty;
        if (!ns.StartsWith("VaultLedger", StringComparison.Ordinal))
            return false;

        var properties = new List<LogEventProperty>();
        foreach (var prop in type.GetProperties(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public))
        {
            if (!prop.CanRead || prop.GetIndexParameters().Length > 0)
                continue;

            object? raw;
            try
            {
                raw = prop.GetValue(value);
            }
            catch
            {
                continue;
            }

            if (SensitiveNames.Contains(prop.Name))
            {
                properties.Add(new LogEventProperty(prop.Name, new ScalarValue("***REDACTED***")));
                continue;
            }

            properties.Add(new LogEventProperty(
                prop.Name,
                propertyValueFactory.CreatePropertyValue(raw, destructureObjects: true)));
        }

        result = new StructureValue(properties, type.Name);
        return true;
    }
}
