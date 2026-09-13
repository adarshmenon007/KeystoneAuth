using ErrorOr;
using FluentAssertions;
using Keystone.Application.Common.Interfaces;
using Keystone.Application.Identity;
using Keystone.Application.Identity.Commands.LoginUser;
using Keystone.Application.Identity.Common;
using Keystone.Domain.Accounts;
using NSubstitute;
using Xunit;

namespace Keystone.Application.UnitTests.Identity;

public class LoginUserCommandHandlerTests
{
    private readonly IIdentityService _identityService = Substitute.For<IIdentityService>();
    private readonly IAuthTokenService _tokenService = Substitute.For<IAuthTokenService>();

    private LoginUserCommandHandler CreateHandler() => new(_identityService, _tokenService);

    [Fact]
    public async Task Handle_WithValidCredentials_IssuesTokens()
    {
        var account = new AuthenticatedAccount(Guid.NewGuid(), "jordan@example.com", "Jordan Ellis");
        var expected = new AuthResult("access", "refresh", DateTimeOffset.UtcNow.AddMinutes(15), account.Id, account.DisplayName);

        _identityService.ValidateCredentialsAsync("jordan@example.com", "password123", Arg.Any<CancellationToken>())
            .Returns(account);
        _tokenService.IssueTokensAsync(account, Arg.Any<CancellationToken>()).Returns(expected);

        var result = await CreateHandler().Handle(
            new LoginUserCommand("jordan@example.com", "password123"), CancellationToken.None);

        result.IsError.Should().BeFalse();
        result.Value.Should().Be(expected);
    }

    [Fact]
    public async Task Handle_WithBadCredentials_ReturnsUnauthorizedAndDoesNotIssueTokens()
    {
        _identityService.ValidateCredentialsAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(IdentityErrors.InvalidCredentials);

        var result = await CreateHandler().Handle(
            new LoginUserCommand("jordan@example.com", "wrong"), CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.FirstError.Should().Be(IdentityErrors.InvalidCredentials);
        await _tokenService.DidNotReceive().IssueTokensAsync(Arg.Any<AuthenticatedAccount>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("", "password123")]
    [InlineData("   ", "password123")]
    [InlineData("jordan@example.com", "")]
    public async Task Handle_WithMissingInput_ShortCircuitsToUnauthorized(string email, string password)
    {
        var result = await CreateHandler().Handle(new LoginUserCommand(email, password), CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.FirstError.Should().Be(IdentityErrors.InvalidCredentials);
        await _identityService.DidNotReceive().ValidateCredentialsAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
