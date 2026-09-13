namespace Keystone.Domain.Accounts;

/// <summary>
/// A registered account, projected out of the identity store for token issuance. Kept
/// deliberately thin — this repo has no separate business aggregate to link an identity
/// user to (unlike a full app, where a domain entity like a "Player" or "Profile" would
/// carry the linkage). The identity user's own id, email, and display name are enough.
/// </summary>
public sealed record AuthenticatedAccount(Guid Id, string Email, string DisplayName);
