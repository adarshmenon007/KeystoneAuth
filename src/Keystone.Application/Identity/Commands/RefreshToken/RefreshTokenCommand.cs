using ErrorOr;
using Keystone.Application.Identity.Common;
using MediatR;

namespace Keystone.Application.Identity.Commands.RefreshToken;

public record RefreshTokenCommand(string RefreshToken)
    : IRequest<ErrorOr<AuthResult>>;
