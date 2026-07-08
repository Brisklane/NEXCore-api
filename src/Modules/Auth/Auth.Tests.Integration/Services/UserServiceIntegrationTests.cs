using Auth.Infrastructure.Services;
using Auth.Tests.Integration.Builders;

namespace Auth.Tests.Integration.Services;

/// <summary>
/// Integration tests for UserService
/// </summary>
public class UserServiceIntegrationTests : AuthIntegrationTestFixture
{
    [Fact]
    public async Task CreateUser_WithValidData_SuccessfullyCreatesUser()
    {
        // Arrange
        var testDataBuilder = new AuthTestDataBuilder(DbContext);

        // Act
        var user = await testDataBuilder.CreateUserAsync("testuser", "test@example.com");

        // Assert
        Assert.NotNull(user);
        Assert.Equal("testuser", user.Username);
        Assert.Equal("test@example.com", user.Email);
    }

    [Fact]
    public async Task GetUserByUsername_WithExistingUser_ReturnsCorrectUser()
    {
        // Arrange
        var testDataBuilder = new AuthTestDataBuilder(DbContext);
        var expectedUser = await testDataBuilder.CreateUserAsync("johndoe", "john@example.com");

        // Act
        var foundUser = DbContext.Users.FirstOrDefault(u => u.Username == "johndoe");

        // Assert
        Assert.NotNull(foundUser);
        Assert.Equal(expectedUser.Id, foundUser.Id);
    }

    [Fact]
    public void GetUserByUsername_WithNonExistingUser_ReturnsNull()
    {
        // Arrange & Act
        var foundUser = DbContext.Users.FirstOrDefault(u => u.Username == "nonexistent");

        // Assert
        Assert.Null(foundUser);
    }

    [Fact]
    public async Task GetUserById_WithExistingUser_ReturnsCorrectUser()
    {
        // Arrange
        var testDataBuilder = new AuthTestDataBuilder(DbContext);
        var expectedUser = await testDataBuilder.CreateUserAsync();

        // Act
        var foundUser = DbContext.Users.FirstOrDefault(u => u.Id == expectedUser.Id);

        // Assert
        Assert.NotNull(foundUser);
        Assert.Equal(expectedUser.Id, foundUser.Id);
        Assert.Equal(expectedUser.Username, foundUser.Username);
    }
}
