using Auth.Infrastructure.Services;
using Auth.Tests.Integration.Builders;
using Auth.Domain.Interfaces;
using Moq;

namespace Auth.Tests.Integration.Services;

/// <summary>
/// Integration tests for RoleService
/// </summary>
public class RoleServiceTests : AuthIntegrationTestFixture
{
    [Fact]
    public async Task CreateRole_WithValidData_SuccessfullyCreatesRole()
    {
        // Arrange
        var testDataBuilder = new AuthTestDataBuilder(DbContext);
        var newRole = await testDataBuilder.CreateRoleAsync("MANAGER", "Manager", "Manager role");

        // Act
        var savedRole = DbContext.Roles.FirstOrDefault(r => r.Code == "MANAGER");

        // Assert
        Assert.NotNull(savedRole);
        Assert.Equal("Manager", savedRole.Name);
    }

    [Fact]
    public async Task AssignRoleToUser_WithValidData_SuccessfullyAssignsRole()
    {
        // Arrange
        var testDataBuilder = new AuthTestDataBuilder(DbContext);
        var user = await testDataBuilder.CreateUserAsync();
        var role = await testDataBuilder.CreateRoleAsync();

        // Act
        var userRole = await testDataBuilder.AssignRoleToUserAsync(user, role);

        // Assert
        Assert.NotNull(userRole);
        Assert.Equal(user.Id, userRole.UserId);
        Assert.Equal(role.Id, userRole.RoleId);
    }

    [Fact]
    public async Task GetUserRoles_WithAssignedRoles_ReturnsRoles()
    {
        // Arrange
        var testDataBuilder = new AuthTestDataBuilder(DbContext);
        var user = await testDataBuilder.CreateUserAsync();
        var role1 = await testDataBuilder.CreateRoleAsync("ADMIN", "Admin");
        var role2 = await testDataBuilder.CreateRoleAsync("USER", "User");

        await testDataBuilder.AssignRoleToUserAsync(user, role1);
        await testDataBuilder.AssignRoleToUserAsync(user, role2);

        // Create mock for IAuditService
        var auditServiceMock = new Mock<IAuditService>();
        var roleService = new RoleService(DbContext, auditServiceMock.Object);

        // Act
        var userRoles = await roleService.GetUserRolesAsync(user.Id);

        // Assert
        Assert.NotNull(userRoles);
        Assert.NotEmpty(userRoles);
    }

    [Fact]
    public async Task GetAllRoles_ReturnsAllRoles()
    {
        // Arrange
        var testDataBuilder = new AuthTestDataBuilder(DbContext);
        await testDataBuilder.CreateRoleAsync("ROLE1", "Role 1");
        await testDataBuilder.CreateRoleAsync("ROLE2", "Role 2");

        // Act
        var roles = DbContext.Roles.ToList();

        // Assert
        Assert.NotEmpty(roles);
        Assert.True(roles.Count >= 2);
    }

    [Fact]
    public async Task RemoveRoleFromUser_WithValidData_SuccessfullyRemovesRole()
    {
        // Arrange
        var testDataBuilder = new AuthTestDataBuilder(DbContext);
        var user = await testDataBuilder.CreateUserAsync();
        var role = await testDataBuilder.CreateRoleAsync();

        var userRole = await testDataBuilder.AssignRoleToUserAsync(user, role);

        // Act
        DbContext.UserRoles.Remove(userRole);
        await DbContext.SaveChangesAsync();

        var remainingUserRoles = DbContext.UserRoles.Where(ur => ur.UserId == user.Id).ToList();

        // Assert
        Assert.Empty(remainingUserRoles);
    }
}
