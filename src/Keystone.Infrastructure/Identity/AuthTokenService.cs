using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using ErrorOr;
using Keystone.Application.Common.Interfaces;
using Keystone.Application.Identity;
using Keystone.Application.Identity.Common;
using Keystone.Domain.Accounts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Keystone.Infrastructure.Identity;

/// <summary>
/// Mints access JWTs and manages the rotating server-side refresh-token family.
/// </summary>
internal sealed class AuthTokenService : IAuthTokenService
{
    private readonly KeystoneIdentityDbContext _identityDb;
    private readonly JwtOptions _options;

    public AuthTokenService(KeystoneIdentityDbContext identityDb, IOptions<JwtOptions> options)
    {
        _identityDb = identityDb;
        _options = options.Value;
    }

    public async Task<AuthResult> IssueTokensAsync(AuthenticatedAccount account, CancellationToken ct = default) =>
        await IssueForFamilyAsync(account, Guid.NewGuid(), ct);

    public async Task<ErrorOr<AuthResult>> RefreshAsync(string refreshToken, CancellationToken ct = default)
    {
        var hash = Hash(refreshToken);
        var record = await _identityDb.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == hash, ct);
        if (record is null)
            return IdentityErrors.InvalidRefreshToken;

        var now = DateTimeOffset.UtcNow;

        // Presenting a token that was already rotated or revoked = reuse → burn the family.
        if (record.RevokedAt is not null || record.ReplacedByTokenHash is not null)
        {
            await RevokeFamilyAsync(record.FamilyId, now, ct);
            return IdentityErrors.InvalidRefreshToken;
        }

        if (record.ExpiresAt <= now)
        {
            record.RevokedAt = now;
            await _identityDb.SaveChangesAsync(ct);
            return IdentityErrors.InvalidRefreshToken;
        }

        var user = await _identityDb.Users.FirstOrDefaultAsync(u => u.Id == record.UserId, ct);
        if (user is null)
            return IdentityErrors.InvalidRefreshToken;

        var rawRefresh = CreateRawRefreshToken();

        // Atomic rotate: exactly one concurrent refresh flips this row from unused. A caller
        // that loses the race falls through to reuse-handling and burns the family.
        var rotated = await _identityDb.RefreshTokens
            .Where(t => t.Id == record.Id && t.RevokedAt == null && t.ReplacedByTokenHash == null)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(t => t.RevokedAt, now)
                    .SetProperty(t => t.ReplacedByTokenHash, Hash(rawRefresh)),
                ct);

        if (rotated == 0)
        {
            await RevokeFamilyAsync(record.FamilyId, now, ct);
            return IdentityErrors.InvalidRefreshToken;
        }

        var account = new AuthenticatedAccount(user.Id, user.Email!, user.DisplayName);
        return await PersistAndBuildAsync(account, record.FamilyId, rawRefresh, now, ct);
    }

    public async Task RevokeAllForOwnerAsync(string refreshToken, CancellationToken ct = default)
    {
        var hash = Hash(refreshToken);
        var record = await _identityDb.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == hash, ct);
        if (record is null)
            return;

        var now = DateTimeOffset.UtcNow;
        var owned = await _identityDb.RefreshTokens
            .Where(t => t.UserId == record.UserId && t.RevokedAt == null)
            .ToListAsync(ct);

        foreach (var token in owned)
            token.RevokedAt = now;

        await _identityDb.SaveChangesAsync(ct);
    }

    private async Task<AuthResult> IssueForFamilyAsync(AuthenticatedAccount account, Guid familyId, CancellationToken ct)
    {
        var rawRefresh = CreateRawRefreshToken();
        return await PersistAndBuildAsync(account, familyId, rawRefresh, DateTimeOffset.UtcNow, ct);
    }

    private async Task<AuthResult> PersistAndBuildAsync(
        AuthenticatedAccount account, Guid familyId, string rawRefresh, DateTimeOffset now, CancellationToken ct)
    {
        var accessExpiresAt = now.AddMinutes(_options.AccessTokenMinutes);
        var accessToken = CreateAccessToken(account, now, accessExpiresAt);

        _identityDb.RefreshTokens.Add(new RefreshTokenRecord
        {
            Id = Guid.NewGuid(),
            UserId = account.Id,
            TokenHash = Hash(rawRefresh),
            FamilyId = familyId,
            CreatedAt = now,
            ExpiresAt = now.AddDays(_options.RefreshTokenDays),
        });
        await _identityDb.SaveChangesAsync(ct);

        return new AuthResult(accessToken, rawRefresh, accessExpiresAt, account.Id, account.DisplayName);
    }

    private async Task RevokeFamilyAsync(Guid familyId, DateTimeOffset now, CancellationToken ct)
    {
        var family = await _identityDb.RefreshTokens
            .Where(t => t.FamilyId == familyId && t.RevokedAt == null)
            .ToListAsync(ct);

        foreach (var token in family)
            token.RevokedAt = now;

        await _identityDb.SaveChangesAsync(ct);
    }

    private string CreateAccessToken(AuthenticatedAccount account, DateTimeOffset issuedAt, DateTimeOffset expiresAt)
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, account.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Name, account.DisplayName),
            new Claim(JwtRegisteredClaimNames.Email, account.Email),
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: issuedAt.UtcDateTime,
            expires: expiresAt.UtcDateTime,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static string CreateRawRefreshToken() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

    private static string Hash(string token) =>
        Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
