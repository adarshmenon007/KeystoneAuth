using ErrorOr;
using MediatR;

namespace Keystone.Application.Identity.Commands.Logout;

/// <summary>Revokes the caller's whole refresh-token family.</summary>
public record LogoutCommand(string RefreshToken) : IRequest<ErrorOr<Success>>;
