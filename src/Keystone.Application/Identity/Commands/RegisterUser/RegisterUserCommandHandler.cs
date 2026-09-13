using ErrorOr;
using Keystone.Application.Common.Interfaces;
using Keystone.Application.Identity.Common;
using MediatR;

namespace Keystone.Application.Identity.Commands.RegisterUser;

/// <summary>
/// Registers a new account, then issues the first token pair. Input shape is validated
/// here; uniqueness and the store-level password policy are enforced by the identity
/// service and surfaced as <see cref="IdentityErrors"/>.
/// </summary>
public class RegisterUserCommandHandler : IRequestHandler<RegisterUserCommand, ErrorOr<AuthResult>>
{
    private readonly IIdentityService _identityService;
    private readonly IAuthTokenService _tokenService;

    public RegisterUserCommandHandler(IIdentityService identityService, IAuthTokenService tokenService)
    {
        _identityService = identityService;
        _tokenService = tokenService;
    }

    public async Task<ErrorOr<AuthResult>> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
            return IdentityErrors.EmailRequired;

        if (string.IsNullOrWhiteSpace(request.DisplayName))
            return IdentityErrors.DisplayNameRequired;

        if (request.Password is null || request.Password.Length < 8)
            return IdentityErrors.WeakPassword;

        var accountResult = await _identityService.RegisterAccountAsync(
            request.Email.Trim(), request.Password, request.DisplayName.Trim(), cancellationToken);

        if (accountResult.IsError)
            return accountResult.Errors;

        return await _tokenService.IssueTokensAsync(accountResult.Value, cancellationToken);
    }
}
