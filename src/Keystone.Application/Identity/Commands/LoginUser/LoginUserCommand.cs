using ErrorOr;
using Keystone.Application.Identity.Common;
using MediatR;

namespace Keystone.Application.Identity.Commands.LoginUser;

public record LoginUserCommand(string Email, string Password)
    : IRequest<ErrorOr<AuthResult>>;
