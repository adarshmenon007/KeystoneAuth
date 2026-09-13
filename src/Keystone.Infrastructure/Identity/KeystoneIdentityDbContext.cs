using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Keystone.Infrastructure.Identity;

/// <summary>
/// The one and only DbContext in this repo. Uses <see cref="IdentityUserContext{TUser,TKey}"/>
/// (user tables only, no roles) since there's no authorization-roles feature here.
/// </summary>
internal sealed class KeystoneIdentityDbContext : IdentityUserContext<KeystoneUser, Guid>
{
    public KeystoneIdentityDbContext(DbContextOptions<KeystoneIdentityDbContext> options) : base(options)
    {
    }

    public DbSet<RefreshTokenRecord> RefreshTokens => Set<RefreshTokenRecord>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<KeystoneUser>(user =>
        {
            user.Property(u => u.DisplayName).HasMaxLength(200).IsRequired();
        });

        builder.Entity<RefreshTokenRecord>(token =>
        {
            token.HasKey(t => t.Id);
            token.HasIndex(t => t.TokenHash).IsUnique();
            token.HasIndex(t => t.FamilyId);
            token.HasIndex(t => t.UserId);
            token.Property(t => t.TokenHash).HasMaxLength(64).IsRequired();
            token.Property(t => t.ReplacedByTokenHash).HasMaxLength(64);
        });
    }
}
