using FluentAssertions;
using Keystone.Application.Identity;
using Keystone.Domain.Accounts;
using Keystone.Infrastructure.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Xunit;

namespace Keystone.Infrastructure.IntegrationTests.Identity;

/// <summary>
/// Exercises the rotating refresh-token family against a real database: rotation,
/// reuse-detection, and the atomic rotate guard in <see cref="AuthTokenService"/>.
/// </summary>
public sealed class AuthTokenServiceTests : IAsyncLifetime
{
    private SqliteConnection _connection = null!;
    private KeystoneIdentityDbContext _identityDb = null!;
    private AuthTokenService _service = null!;
    private AuthenticatedAccount _account = null!;

    public async Task InitializeAsync()
    {
        _connection = new SqliteConnection("Filename=:memory:");
        await _connection.OpenAsync();

        _identityDb = new KeystoneIdentityDbContext(
            new DbContextOptionsBuilder<KeystoneIdentityDbContext>().UseSqlite(_connection).Options);
        await _identityDb.Database.MigrateAsync();

        _account = new AuthenticatedAccount(Guid.NewGuid(), "ada@example.com", "Ada Lovelace");

        // AuthTokenService.RefreshAsync looks up the owning KeystoneUser row by id, so the
        // account must actually exist in the identity store, not just as an in-memory value.
        _identityDb.Users.Add(new KeystoneUser
        {
            Id = _account.Id,
            UserName = _account.Email,
            Email = _account.Email,
            DisplayName = _account.DisplayName,
        });
        await _identityDb.SaveChangesAsync();

        _service = new AuthTokenService(_identityDb, Options.Create(new JwtOptions
        {
            SigningKey = "test-signing-key-at-least-32-characters-long!!",
        }));
    }

    public async Task DisposeAsync()
    {
        await _identityDb.DisposeAsync();
        await _connection.DisposeAsync();
    }

    [Fact]
    public async Task RefreshAsync_WithACurrentToken_RotatesToAFreshPair()
    {
        var issued = await _service.IssueTokensAsync(_account);

        var refreshed = await _service.RefreshAsync(issued.RefreshToken);

        refreshed.IsError.Should().BeFalse();
        refreshed.Value.RefreshToken.Should().NotBe(issued.RefreshToken);
        refreshed.Value.UserId.Should().Be(_account.Id);

        _identityDb.RefreshTokens.Should().HaveCount(2);
    }

    [Fact]
    public async Task RefreshAsync_ReusingARotatedToken_FailsAndRevokesTheWholeFamily()
    {
        var issued = await _service.IssueTokensAsync(_account);
        var refreshed = await _service.RefreshAsync(issued.RefreshToken);

        // Present the already-rotated original again.
        var reuse = await _service.RefreshAsync(issued.RefreshToken);

        reuse.IsError.Should().BeTrue();
        reuse.FirstError.Should().Be(IdentityErrors.InvalidRefreshToken);

        // The successor is now dead too, so a legitimate client can't continue.
        var afterBurn = await _service.RefreshAsync(refreshed.Value.RefreshToken);
        afterBurn.IsError.Should().BeTrue();

        _identityDb.ChangeTracker.Clear();
        (await _identityDb.RefreshTokens.ToListAsync())
            .Should().OnlyContain(t => t.RevokedAt != null);
    }

    [Fact]
    public async Task RefreshAsync_WithAnUnknownToken_ReturnsUnauthorized()
    {
        var result = await _service.RefreshAsync("not-a-real-token");

        result.IsError.Should().BeTrue();
        result.FirstError.Should().Be(IdentityErrors.InvalidRefreshToken);
    }

    [Fact]
    public async Task RevokeAllForOwnerAsync_RevokesEveryLiveTokenForThatOwner()
    {
        var first = await _service.IssueTokensAsync(_account);
        await _service.IssueTokensAsync(_account);

        await _service.RevokeAllForOwnerAsync(first.RefreshToken);

        _identityDb.ChangeTracker.Clear();
        (await _identityDb.RefreshTokens.ToListAsync())
            .Should().OnlyContain(t => t.RevokedAt != null);
    }
}
