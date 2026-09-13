using Microsoft.AspNetCore.Identity;

namespace Keystone.Infrastructure.Identity;

/// <summary>
/// The ASP.NET Core Identity user. Carries <see cref="DisplayName"/> directly since this
/// repo has no separate business entity to hold it. Lives only in Infrastructure, persisted
/// by <see cref="KeystoneIdentityDbContext"/>.
/// </summary>
internal sealed class KeystoneUser : IdentityUser<Guid>
{
    public string DisplayName { get; set; } = string.Empty;
}
