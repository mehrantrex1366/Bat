using Microsoft.Extensions.Options;

namespace Bat.AspNetCore;

/// <summary>
/// Validates <see cref="JwtSettings"/> at startup (opt-in, see <see cref="JwtSettingsValidationExtensions.ValidateBatJwtSettings"/>):
/// catches the configurations that otherwise fail at the first request or silently use Bat's hard-coded fallback keys.
/// </summary>
public sealed class JwtSettingsValidator : IValidateOptions<JwtSettings>
{
    public ValidateOptionsResult Validate(string name, JwtSettings options)
    {
        var errors = GetErrors(options);
        return errors.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(errors);
    }

    /// <summary>Returns the problems of <paramref name="settings"/> (empty when valid). Usable without DI.</summary>
    public static IReadOnlyList<string> GetErrors(JwtSettings settings)
    {
        var errors = new List<string>();
        if (settings is null)
        {
            errors.Add("JwtSettings is not configured.");
            return errors;
        }

        // HMAC-SHA256 signing keys shorter than 256 bits are rejected by Microsoft.IdentityModel (IDX10720) when a token is created.
        if (string.IsNullOrWhiteSpace(settings.SecretKey))
            errors.Add("JwtSettings.SecretKey is empty: Bat would use its public hard-coded fallback key.");
        else if (Encoding.UTF8.GetByteCount(settings.SecretKey) < 32)
            errors.Add("JwtSettings.SecretKey must be at least 32 bytes (256 bits) for HMAC-SHA256.");

        // Tokens are encrypted with A128KW, which needs exactly a 128-bit key.
        if (string.IsNullOrWhiteSpace(settings.Encryptionkey))
            errors.Add("JwtSettings.Encryptionkey is empty: Bat would use its public hard-coded fallback key.");
        else if (Encoding.UTF8.GetByteCount(settings.Encryptionkey) != 16)
            errors.Add("JwtSettings.Encryptionkey must be exactly 16 bytes (A128KW).");

        if (settings.ExpirationMinutes < 0) errors.Add("JwtSettings.ExpirationMinutes cannot be negative.");
        if (settings.NotBeforeMinutes < 0) errors.Add("JwtSettings.NotBeforeMinutes cannot be negative.");
        if (settings.RefreshTokenExpirationMinutes < 0) errors.Add("JwtSettings.RefreshTokenExpirationMinutes cannot be negative.");

        return errors;
    }
}

public static class JwtSettingsValidationExtensions
{
    /// <summary>
    /// Validates the bound <see cref="JwtSettings"/> with <see cref="JwtSettingsValidator"/> when the host starts
    /// (the app fails fast instead of at the first login). Opt-in: existing services keep their current behavior.
    /// <code>services.AddOptions&lt;JwtSettings&gt;().BindConfiguration("JwtSettings").ValidateBatJwtSettings();</code>
    /// </summary>
    public static OptionsBuilder<JwtSettings> ValidateBatJwtSettings(this OptionsBuilder<JwtSettings> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.Validate<JwtSettingsValidator>().ValidateOnStart();
    }
}