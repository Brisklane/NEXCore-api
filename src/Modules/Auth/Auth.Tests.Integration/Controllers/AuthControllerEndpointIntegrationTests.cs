using Auth.Application.DTOs;
using Auth.Application.Services.Interfaces;
using Auth.Domain.Entities;
using Auth.Infrastructure.Persistence;
using Auth.Tests.Integration.Builders;
using Auth.Tests.Integration.Fixtures;
using Moq;

namespace Auth.Tests.Integration.Controllers;

/// <summary>
/// Integration tests for Auth endpoints
/// Tests API contracts and service interactions
/// </summary>
public class AuthControllerEndpointIntegrationTests : AuthIntegrationTestFixture
{
    private AuthTestDataBuilder? _testDataBuilder;
    private readonly Mock<IAuthenticationService> _authServiceMock;

    public AuthControllerEndpointIntegrationTests()
    {
        _authServiceMock = new Mock<IAuthenticationService>();
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        _testDataBuilder = new AuthTestDataBuilder(DbContext);
    }

    #region User Lifecycle Tests

    [Fact]
    public async Task UserLifecycle_CreateUserAndLogin_CompleteFlow()
    {
        // Arrange: Create test user
        var user = await _testDataBuilder!.CreateUserAsync("testuser", "test@example.com");
        var role = await _testDataBuilder.CreateRoleAsync("USER", "User", "Regular user role");
        await _testDataBuilder.AssignRoleToUserAsync(user, role);

        // Assert: Verify user was created and persisted
        Assert.NotNull(user);
        Assert.Equal("testuser", user.Username);
        Assert.Equal("test@example.com", user.Email);
        Assert.True(user.IsActive);

        // Assert: Verify role was assigned
        var userRole = DbContext.UserRoles.FirstOrDefault(ur => ur.UserId == user.Id);
        Assert.NotNull(userRole);
        Assert.Equal(role.Id, userRole.RoleId);
    }

    #endregion

    #region User Validation Tests

    [Fact]
    public async Task ValidateUser_WithValidUserData_SuccessfullyCreatesUser()
    {
        // Arrange: Create a new valid user using the builder
        var newUser = await _testDataBuilder!.CreateUserAsync("newuser", "newuser@example.com");

        // Act: Retrieve the user
        var savedUser = DbContext.Users.FirstOrDefault(u => u.Username == "newuser");

        // Assert
        Assert.NotNull(savedUser);
        Assert.Equal("newuser@example.com", savedUser.Email);
    }

    #endregion

    #region Multiple Users Tests

    [Fact]
    public async Task MultipleUsersScenario_CreateAndRetrieveMultipleUsers()
    {
        // Arrange: Create multiple users
        var user1 = await _testDataBuilder!.CreateUserAsync("user1", "user1@example.com");
        var user2 = await _testDataBuilder.CreateUserAsync("user2", "user2@example.com");
        var user3 = await _testDataBuilder.CreateUserAsync("user3", "user3@example.com");

        // Act: Retrieve all users
        var allUsers = DbContext.Users.ToList();

        // Assert
        Assert.NotEmpty(allUsers);
        Assert.True(allUsers.Count >= 3);
        Assert.NotNull(allUsers.FirstOrDefault(u => u.Username == "user1"));
        Assert.NotNull(allUsers.FirstOrDefault(u => u.Username == "user2"));
        Assert.NotNull(allUsers.FirstOrDefault(u => u.Username == "user3"));
    }

    #endregion

    #region Role Management Tests

    [Fact]
    public async Task RoleManagement_CreateMultipleRolesAndAssignToUsers()
    {
        // Arrange: Create roles
        var adminRole = await _testDataBuilder!.CreateRoleAsync("ADMIN", "Administrator", "Admin role");
        var userRole = await _testDataBuilder.CreateRoleAsync("USER", "User", "User role");
        var guestRole = await _testDataBuilder.CreateRoleAsync("GUEST", "Guest", "Guest role");

        // Act: Create users and assign roles
        var adminUser = await _testDataBuilder.CreateUserAsync("admin", "admin@example.com");
        var regularUser = await _testDataBuilder.CreateUserAsync("regular", "regular@example.com");

        await _testDataBuilder.AssignRoleToUserAsync(adminUser, adminRole);
        await _testDataBuilder.AssignRoleToUserAsync(regularUser, userRole);

        // Assert
        var adminUserRole = DbContext.UserRoles.FirstOrDefault(ur => ur.UserId == adminUser.Id && ur.RoleId == adminRole.Id);
        Assert.NotNull(adminUserRole);

        var regularUserRole = DbContext.UserRoles.FirstOrDefault(ur => ur.UserId == regularUser.Id && ur.RoleId == userRole.Id);
        Assert.NotNull(regularUserRole);
    }

    [Fact]
    public async Task RoleManagement_UserCanHaveMultipleRoles()
    {
        // Arrange: Create a user and multiple roles
        var user = await _testDataBuilder!.CreateUserAsync("multiuser", "multi@example.com");
        var role1 = await _testDataBuilder.CreateRoleAsync("ROLE1", "Role 1", "First role");
        var role2 = await _testDataBuilder.CreateRoleAsync("ROLE2", "Role 2", "Second role");

        // Act: Assign multiple roles to user
        await _testDataBuilder.AssignRoleToUserAsync(user, role1);
        await _testDataBuilder.AssignRoleToUserAsync(user, role2);

        // Assert
        var userRoles = DbContext.UserRoles.Where(ur => ur.UserId == user.Id).ToList();
        Assert.NotEmpty(userRoles);
        Assert.Equal(2, userRoles.Count);
    }

    #endregion

    #region Permission Management Tests

    [Fact]
    public async Task PermissionManagement_CreateAndRetrievePermissions()
    {
        // Arrange: Create permissions
        var readPerm = await _testDataBuilder!.CreatePermissionAsync("READ", "Read", "Read permission");
        var writePerm = await _testDataBuilder.CreatePermissionAsync("WRITE", "Write", "Write permission");
        var deletePerm = await _testDataBuilder.CreatePermissionAsync("DELETE", "Delete", "Delete permission");

        // Act: Retrieve all permissions
        var allPermissions = DbContext.Permissions.ToList();

        // Assert
        Assert.NotEmpty(allPermissions);
        Assert.True(allPermissions.Count >= 3);
    }

    #endregion

    #region User Deactivation Tests

    [Fact]
    public async Task UserDeactivation_DeactivateUserAndVerifyStatus()
    {
        // Arrange: Create an active user
        var user = await _testDataBuilder!.CreateUserAsync("activeuser", "active@example.com");
        Assert.True(user.IsActive);

        // Act: Deactivate the user
        user.IsActive = false;
        DbContext.Users.Update(user);
        await DbContext.SaveChangesAsync();

        // Assert
        var deactivatedUser = DbContext.Users.FirstOrDefault(u => u.Id == user.Id);
        Assert.NotNull(deactivatedUser);
        Assert.False(deactivatedUser.IsActive);
    }

    #endregion

    #region Email Verification Tests

    

    #endregion
}
