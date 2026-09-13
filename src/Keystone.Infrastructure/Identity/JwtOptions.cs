namespace Keystone.Infrastructure.Identity;

/// <summary>
/// Bound from the <c>Jwt</c> configuration section. <see cref="SigningKey"/> is a secret and
/// comes from user-secrets / environment, never a committed file; the rest live in
/// <c>appsettings.json</c>.
/// </summary>
internal sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "Keystone.Api";

    public string Audience { get; set; } = "Keystone.Web";

    public string SigningKey { get; set; } = string.Empty;

    /// <summary>Access-token lifetime in minutes.</summary>
    public int AccessTokenMinutes { get; set; } = 15;

    /// <summary>Refresh-token sliding lifetime in days.</summary>
    public int RefreshTokenDays { get; set; } = 14;
}
