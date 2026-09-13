using ErrorOr;
using FluentAssertions;
using Keystone.Application.Common.Interfaces;
using Keystone.Application.Identity;
using Keystone.Application.Identity.Commands.RefreshToken;
using Keystone.Application.Identity.Common;
using NSubstitute;
using Xunit;

namespace Keystone.Application.UnitTests.Identity;

public class RefreshTokenCommandHandlerTests
{
    private readonly IAuthTokenService _tokenService = Substitute.For<IAuthTokenService>();

    private RefreshTokenCommandHandler CreateHandler() => new(_tokenService);

    [Fact]
    public async Task Handle_WithValidToken_ReturnsTheRotatedPair()
    {
        var rotated = new AuthResult("access2", "refresh2", DateTimeOffset.UtcNow.AddMinutes(15), Guid.NewGuid(), "Ada");
        _tokenService.RefreshAsync("refresh1", Arg.Any<CancellationToken>()).Returns(rotated);

        var result = await CreateHandler().Handle(new RefreshTokenCommand("refresh1"), CancellationToken.None);

        result.IsError.Should().BeFalse();
        result.Value.Should().Be(rotated);
    }

    [Fact]
    public async Task Handle_WhenServiceRejectsTheToken_PropagatesUnauthorized()
    {
        _tokenService.RefreshAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(IdentityErrors.InvalidRefreshToken);

        var result = await CreateHandler().Handle(new RefreshTokenCommand("stale"), CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.FirstError.Should().Be(IdentityErrors.InvalidRefreshToken);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Handle_WithBlankToken_ShortCircuits(string token)
    {
        var result = await CreateHandler().Handle(new RefreshTokenCommand(token), CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.FirstError.Should().Be(IdentityErrors.InvalidRefreshToken);
        await _tokenService.DidNotReceive().RefreshAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
