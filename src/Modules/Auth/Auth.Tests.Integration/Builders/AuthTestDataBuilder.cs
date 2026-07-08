using Auth.Domain.Entities;
using Auth.Infrastructure.Persistence;

namespace Auth.Tests.Integration.Builders;

/// <summary>
/// Builder pattern for integration test data creation
/// </summary>
public class AuthTestDataBuilder
{
    private readonly AuthDbContext _dbContext;

    public AuthTestDataBuilder(AuthDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<User> CreateUserAsync(string username = "testuser", string email = "test@example.com")
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = username,
            Email = email,
            FullName = "Test User",
            FirstName = "Test",
            LastName = "User",
            PhoneNumber = "+1234567890",
            Gender = "M",
            DateOfBirth = DateTime.UtcNow.AddYears(-25),
            Address = "123 Test Street",
            PasswordHash = "hashedpassword",
            IsActive = true,
            IsEmailVerified = true,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();
        return user;
    }

    public async Task<Role> CreateRoleAsync(string code = "ADMIN", string name = "Admin", string description = "Administrator role")
    {
        var role = new Role
        {
            Id = Guid.NewGuid(),
            Code = code,
            Name = name,
            Description = description,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Roles.Add(role);
        await _dbContext.SaveChangesAsync();
        return role;
    }

    public async Task<UserRole> AssignRoleToUserAsync(User user, Role role)
    {
        var userRole = new UserRole
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            RoleId = role.Id,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.UserRoles.Add(userRole);
        await _dbContext.SaveChangesAsync();
        return userRole;
    }

    public async Task<Permission> CreatePermissionAsync(string code = "READ", string name = "Read", string description = "Read permission")
    {
        var permission = new Permission
        {
            Id = Guid.NewGuid(),
            Code = code,
            Name = name,
            Description = description,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Permissions.Add(permission);
        await _dbContext.SaveChangesAsync();
        return permission;
    }

    public async Task ClearAllAsync()
    {
        var userRoles = _dbContext.UserRoles.ToList();
        _dbContext.UserRoles.RemoveRange(userRoles);

        var refreshTokens = _dbContext.RefreshTokens.ToList();
        _dbContext.RefreshTokens.RemoveRange(refreshTokens);

        var users = _dbContext.Users.ToList();
        _dbContext.Users.RemoveRange(users);

        var roles = _dbContext.Roles.ToList();
        _dbContext.Roles.RemoveRange(roles);

        var permissions = _dbContext.Permissions.ToList();
        _dbContext.Permissions.RemoveRange(permissions);

        await _dbContext.SaveChangesAsync();
    }
}
