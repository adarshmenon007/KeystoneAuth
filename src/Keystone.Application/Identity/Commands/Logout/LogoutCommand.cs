using ErrorOr;
using MediatR;

namespace Keystone.Application.Identity.Commands.Logout;

/// <summary>Revokes every refresh-token family the caller holds.</summary>
public record LogoutCommand(string RefreshToken) : IRequest<ErrorOr<Success>>;
