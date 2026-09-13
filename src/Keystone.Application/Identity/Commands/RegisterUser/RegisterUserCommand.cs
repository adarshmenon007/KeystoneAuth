using ErrorOr;
using Keystone.Application.Identity.Common;
using MediatR;

namespace Keystone.Application.Identity.Commands.RegisterUser;

/// <param name="Email">Account email — must be unique.</param>
/// <param name="Password">Plain-text password — v1 policy is min length 8.</param>
/// <param name="DisplayName">The account's display name.</param>
public record RegisterUserCommand(string Email, string Password, string DisplayName)
    : IRequest<ErrorOr<AuthResult>>;
