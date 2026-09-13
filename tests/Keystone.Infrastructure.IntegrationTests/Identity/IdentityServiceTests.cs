using FluentAssertions;
using Keystone.Application.Identity;
using Keystone.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Keystone.Infrastructure.IntegrationTests.Identity;

/// <summary>Exercises registration and credential validation against a real ASP.NET Identity store.</summary>
public sealed class IdentityServiceTests : IAsyncLifetime
{
    private SqliteConnection _connection = null!;
    private ServiceProvider _services = null!;
    private IdentityService _service = null!;

    public async Task InitializeAsync()
    {
        _connection = new SqliteConnection("Filename=:memory:");
        await _connection.OpenAsync();

        var collection = new ServiceCollection();
        collection.AddDbContext<KeystoneIdentityDbContext>(options => options.UseSqlite(_connection));
        collection.AddIdentityCore<KeystoneUser>(options =>
            {
                options.Password.RequiredLength = 8;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireDigit = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireLowercase = false;
                options.User.RequireUniqueEmail = true;
            })
            .AddEntityFrameworkStores<KeystoneIdentityDbContext>();

        _services = collection.BuildServiceProvider();

        await _services.GetRequiredService<KeystoneIdentityDbContext>().Database.MigrateAsync();

        _service = new IdentityService(_services.GetRequiredService<UserManager<KeystoneUser>>());
    }

    public async Task DisposeAsync()
    {
        await _services.DisposeAsync();
        await _connection.DisposeAsync();
    }

    [Fact]
    public async Task RegisterAccountAsync_WithANewEmail_CreatesTheAccount()
    {
        var result = await _service.RegisterAccountAsync("ada@example.com", "password123", "Ada Lovelace");

        result.IsError.Should().BeFalse();
        result.Value.Email.Should().Be("ada@example.com");
        result.Value.DisplayName.Should().Be("Ada Lovelace");
    }

    [Fact]
    public async Task RegisterAccountAsync_WithADuplicateEmail_ReturnsConflict()
    {
        await _service.RegisterAccountAsync("ada@example.com", "password123", "Ada Lovelace");

        var result = await _service.RegisterAccountAsync("ada@example.com", "password456", "Someone Else");

        result.IsError.Should().BeTrue();
        result.FirstError.Should().Be(IdentityErrors.EmailAlreadyInUse);
    }

    [Fact]
    public async Task ValidateCredentialsAsync_WithCorrectPassword_ReturnsTheAccount()
    {
        await _service.RegisterAccountAsync("ada@example.com", "password123", "Ada Lovelace");

        var result = await _service.ValidateCredentialsAsync("ada@example.com", "password123");

        result.IsError.Should().BeFalse();
        result.Value.Email.Should().Be("ada@example.com");
    }

    [Fact]
    public async Task ValidateCredentialsAsync_WithWrongPassword_ReturnsInvalidCredentials()
    {
        await _service.RegisterAccountAsync("ada@example.com", "password123", "Ada Lovelace");

        var result = await _service.ValidateCredentialsAsync("ada@example.com", "wrong-password");

        result.IsError.Should().BeTrue();
        result.FirstError.Should().Be(IdentityErrors.InvalidCredentials);
    }

    [Fact]
    public async Task ValidateCredentialsAsync_WithUnknownEmail_ReturnsInvalidCredentials()
    {
        var result = await _service.ValidateCredentialsAsync("nobody@example.com", "password123");

        result.IsError.Should().BeTrue();
        result.FirstError.Should().Be(IdentityErrors.InvalidCredentials);
    }
}
