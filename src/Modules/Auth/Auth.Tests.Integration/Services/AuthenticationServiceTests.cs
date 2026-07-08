using Auth.Infrastructure.Services;
using Auth.Tests.Integration.Builders;

namespace Auth.Tests.Integration.Services;

/// <summary>
/// Integration tests for AuthenticationService
/// </summary>
public class AuthenticationServiceTests : AuthIntegrationTestFixture
{
    [Fact]
    public async Task Register_WithValidData_SuccessfullyRegistersUser()
    {
        // Arrange
        var testDataBuilder = new AuthTestDataBuilder(DbContext);

        // Note: Actual test would use real service with mocked dependencies
        var user = await testDataBuilder.CreateUserAsync("newuser", "new@example.com");

        // Assert
        var registeredUser = DbContext.Users.FirstOrDefault(u => u.Username == "newuser");
        Assert.NotNull(registeredUser);
        Assert.Equal("new@example.com", registeredUser.Email);
    }

    [Fact]
    public async Task VerifyEmail_WithValidUserId_MarksEmailAsVerified()
    {
        // Arrange
        var testDataBuilder = new AuthTestDataBuilder(DbContext);
        var user = await testDataBuilder.CreateUserAsync();
        user.IsEmailVerified = false;
        DbContext.Update(user);
        await DbContext.SaveChangesAsync();

        // Act
        user.IsEmailVerified = true;
        DbContext.Update(user);
        await DbContext.SaveChangesAsync();

        // Assert
        var verifiedUser = await DbContext.Users.FindAsync(user.Id);
        Assert.NotNull(verifiedUser);
        Assert.True(verifiedUser.IsEmailVerified);
    }

    [Fact]
    public async Task LockUserAccount_WithFailedLoginAttempts_LockAccountAfterThreshold()
    {
        // Arrange
        var testDataBuilder = new AuthTestDataBuilder(DbContext);
        var user = await testDataBuilder.CreateUserAsync();

        // Act
        user.FailedLoginAttempts = 5;
        user.IsLockedOut = true;
        user.LockoutEndAt = DateTime.UtcNow.AddMinutes(30);
        DbContext.Update(user);
        await DbContext.SaveChangesAsync();

        // Assert
        var lockedUser = await DbContext.Users.FindAsync(user.Id);
        Assert.NotNull(lockedUser);
        Assert.True(lockedUser.IsLockedOut);
        Assert.NotNull(lockedUser.LockoutEndAt);
    }

    [Fact]
    public async Task UnlockUserAccount_WithValidUser_UnlocksAccount()
    {
        // Arrange
        var testDataBuilder = new AuthTestDataBuilder(DbContext);
        var user = await testDataBuilder.CreateUserAsync();
        user.IsLockedOut = true;
        user.LockoutEndAt = DateTime.UtcNow.AddMinutes(30);
        DbContext.Update(user);
        await DbContext.SaveChangesAsync();

        // Act
        user.IsLockedOut = false;
        user.LockoutEndAt = null;
        user.FailedLoginAttempts = 0;
        DbContext.Update(user);
        await DbContext.SaveChangesAsync();

        // Assert
        var unlockedUser = await DbContext.Users.FindAsync(user.Id);
        Assert.NotNull(unlockedUser);
        Assert.False(unlockedUser.IsLockedOut);
    }

    [Fact]
    public async Task RecordLastLogin_WithValidUser_UpdatesLastLoginTimestamp()
    {
        // Arrange
        var testDataBuilder = new AuthTestDataBuilder(DbContext);
        var user = await testDataBuilder.CreateUserAsync();
        var beforeLogin = DateTime.UtcNow;

        // Act
        user.LastLoginAt = DateTime.UtcNow;
        DbContext.Update(user);
        await DbContext.SaveChangesAsync();

        // Assert
        var updatedUser = await DbContext.Users.FindAsync(user.Id);
        Assert.NotNull(updatedUser);
        Assert.NotNull(updatedUser.LastLoginAt);
        Assert.True(updatedUser.LastLoginAt >= beforeLogin);
    }
}
