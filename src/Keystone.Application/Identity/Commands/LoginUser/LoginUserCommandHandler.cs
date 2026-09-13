using ErrorOr;
using Keystone.Application.Common.Interfaces;
using Keystone.Application.Identity.Common;
using MediatR;

namespace Keystone.Application.Identity.Commands.LoginUser;

/// <summary>
/// Validates credentials and issues a token pair. Bad credentials (or a locked-out
/// account) surface as <see cref="IdentityErrors.InvalidCredentials"/> — no distinction, by design.
/// </summary>
public class LoginUserCommandHandler : IRequestHandler<LoginUserCommand, ErrorOr<AuthResult>>
{
    private readonly IIdentityService _identityService;
    private readonly IAuthTokenService _tokenService;

    public LoginUserCommandHandler(IIdentityService identityService, IAuthTokenService tokenService)
    {
        _identityService = identityService;
        _tokenService = tokenService;
    }

    public async Task<ErrorOr<AuthResult>> Handle(LoginUserCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrEmpty(request.Password))
            return IdentityErrors.InvalidCredentials;

        var accountResult = await _identityService.ValidateCredentialsAsync(
            request.Email.Trim(), request.Password, cancellationToken);

        if (accountResult.IsError)
            return accountResult.Errors;

        return await _tokenService.IssueTokensAsync(accountResult.Value, cancellationToken);
    }
}
