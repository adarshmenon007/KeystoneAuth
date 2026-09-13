using ErrorOr;
using FluentAssertions;
using Keystone.Application.Common.Interfaces;
using Keystone.Application.Identity;
using Keystone.Application.Identity.Commands.RegisterUser;
using Keystone.Application.Identity.Common;
using Keystone.Domain.Accounts;
using NSubstitute;
using Xunit;

namespace Keystone.Application.UnitTests.Identity;

public class RegisterUserCommandHandlerTests
{
    private readonly IIdentityService _identityService = Substitute.For<IIdentityService>();
    private readonly IAuthTokenService _tokenService = Substitute.For<IAuthTokenService>();

    private RegisterUserCommandHandler CreateHandler() => new(_identityService, _tokenService);

    [Fact]
    public async Task Handle_WithValidCommand_CreatesAccountAndIssuesTokens()
    {
        var account = new AuthenticatedAccount(Guid.NewGuid(), "ada@example.com", "Ada Lovelace");
        var expected = new AuthResult("access", "refresh", DateTimeOffset.UtcNow.AddMinutes(15), account.Id, account.DisplayName);

        _identityService
            .RegisterAccountAsync("ada@example.com", "password123", "Ada Lovelace", Arg.Any<CancellationToken>())
            .Returns(account);
        _tokenService.IssueTokensAsync(account, Arg.Any<CancellationToken>()).Returns(expected);

        var result = await CreateHandler().Handle(
            new RegisterUserCommand("ada@example.com", "password123", "Ada Lovelace"), CancellationToken.None);

        result.IsError.Should().BeFalse();
        result.Value.Should().Be(expected);
    }

    [Fact]
    public async Task Handle_TrimsEmailAndDisplayName_BeforeCallingTheStore()
    {
        var account = new AuthenticatedAccount(Guid.NewGuid(), "ada@example.com", "Ada Lovelace");
        _identityService.RegisterAccountAsync("ada@example.com", "password123", "Ada Lovelace", Arg.Any<CancellationToken>())
            .Returns(account);
        _tokenService.IssueTokensAsync(account, Arg.Any<CancellationToken>())
            .Returns(new AuthResult("a", "r", DateTimeOffset.UtcNow, account.Id, account.DisplayName));

        await CreateHandler().Handle(
            new RegisterUserCommand("  ada@example.com  ", "password123", "  Ada Lovelace  "), CancellationToken.None);

        await _identityService.Received(1).RegisterAccountAsync(
            "ada@example.com", "password123", "Ada Lovelace", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenEmailIsTaken_ReturnsConflictAndDoesNotIssueTokens()
    {
        _identityService
            .RegisterAccountAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(IdentityErrors.EmailAlreadyInUse);

        var result = await CreateHandler().Handle(
            new RegisterUserCommand("taken@example.com", "password123", "Someone"), CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.FirstError.Should().Be(IdentityErrors.EmailAlreadyInUse);
        await _tokenService.DidNotReceive().IssueTokensAsync(Arg.Any<AuthenticatedAccount>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("short")]
    [InlineData("")]
    [InlineData(null)]
    public async Task Handle_WithPasswordShorterThan8_ReturnsValidationAndNeverTouchesTheStore(string? password)
    {
        var result = await CreateHandler().Handle(
            new RegisterUserCommand("ada@example.com", password!, "Ada Lovelace"), CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.FirstError.Should().Be(IdentityErrors.WeakPassword);
        await _identityService.DidNotReceive().RegisterAccountAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithBlankDisplayName_ReturnsValidation()
    {
        var result = await CreateHandler().Handle(
            new RegisterUserCommand("ada@example.com", "password123", "   "), CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.FirstError.Should().Be(IdentityErrors.DisplayNameRequired);
    }
}
