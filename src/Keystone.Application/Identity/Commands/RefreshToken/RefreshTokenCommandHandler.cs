using ErrorOr;
using Keystone.Application.Common.Interfaces;
using Keystone.Application.Identity.Common;
using MediatR;

namespace Keystone.Application.Identity.Commands.RefreshToken;

/// <summary>
/// Rotates a refresh token. All validation, rotation, and reuse-detection lives in
/// <see cref="IAuthTokenService"/>; this handler just guards the input shape.
/// </summary>
public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, ErrorOr<AuthResult>>
{
    private readonly IAuthTokenService _tokenService;

    public RefreshTokenCommandHandler(IAuthTokenService tokenService)
    {
        _tokenService = tokenService;
    }

    public async Task<ErrorOr<AuthResult>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
            return IdentityErrors.InvalidRefreshToken;

        return await _tokenService.RefreshAsync(request.RefreshToken, cancellationToken);
    }
}
