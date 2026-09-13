using ErrorOr;
using FluentAssertions;
using Keystone.Application.Common.Interfaces;
using Keystone.Application.Identity.Commands.Logout;
using NSubstitute;
using Xunit;

namespace Keystone.Application.UnitTests.Identity;

public class LogoutCommandHandlerTests
{
    private readonly IAuthTokenService _tokenService = Substitute.For<IAuthTokenService>();

    private LogoutCommandHandler CreateHandler() => new(_tokenService);

    [Fact]
    public async Task Handle_WithAToken_RevokesTheOwnersTokensAndSucceeds()
    {
        var result = await CreateHandler().Handle(new LogoutCommand("refresh1"), CancellationToken.None);

        result.IsError.Should().BeFalse();
        await _tokenService.Received(1).RevokeAllForOwnerAsync("refresh1", Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Handle_WithBlankToken_StillSucceedsButNeverCallsTheService(string token)
    {
        var result = await CreateHandler().Handle(new LogoutCommand(token), CancellationToken.None);

        result.IsError.Should().BeFalse();
        await _tokenService.DidNotReceive().RevokeAllForOwnerAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
