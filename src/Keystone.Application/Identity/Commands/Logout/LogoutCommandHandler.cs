using ErrorOr;
using Keystone.Application.Common.Interfaces;
using MediatR;

namespace Keystone.Application.Identity.Commands.Logout;

/// <summary>
/// Best-effort logout: revokes every refresh token owned by the presenter of this one. An
/// unknown or already-revoked token is not an error — the client is logging out anyway.
/// </summary>
public class LogoutCommandHandler : IRequestHandler<LogoutCommand, ErrorOr<Success>>
{
    private readonly IAuthTokenService _tokenService;

    public LogoutCommandHandler(IAuthTokenService tokenService)
    {
        _tokenService = tokenService;
    }

    public async Task<ErrorOr<Success>> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(request.RefreshToken))
            await _tokenService.RevokeAllForOwnerAsync(request.RefreshToken, cancellationToken);

        return Result.Success;
    }
}
