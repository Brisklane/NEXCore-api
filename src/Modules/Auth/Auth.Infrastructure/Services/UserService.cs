using Auth.Application.DTOs;
using Auth.Application.Services.Interfaces;
using Auth.Domain.Entities;
using Auth.Infrastructure.Persistence;
using Core.Application.DTOs.ValidateDtos;
using Core.Application.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Nexcore.SharedKernel;

namespace Auth.Infrastructure.Services;

/// <summary>
/// User administration: lookups (with resolved roles/permissions), profile edits, the
/// activate/deactivate/lock/unlock toggles, and admin invitation of a user into an existing company.
/// </summary>
public class UserService : IUserService
{
    private const int LockoutHours = 24;

    private readonly AuthDbContext _context;
    private readonly IRoleService _roleService;
    private readonly ICompanyService _companyService;
    private readonly IUserValidator _adminValidator;
    private readonly IPasswordHasher<User> _passwordHasher;

    public UserService(
        AuthDbContext context,
        IRoleService roleService,
        ICompanyService companyService,
        IUserValidator adminValidator,
        IPasswordHasher<User> passwordHasher)
    {
        _context = context;
        _roleService = roleService;
        _companyService = companyService;
        _adminValidator = adminValidator;
        _passwordHasher = passwordHasher;
    }

    public async Task<Result<UserDto>> GetUserByIdAsync(Guid userId)
    {
        try
        {
            var user = await UsersWithRoles().FirstOrDefaultAsync(u => u.Id == userId);
            return user == null
                ? Result<UserDto>.Fail("User not found")
                : Result<UserDto>.Ok(await MapToDtoAsync(user));
        }
        catch (Exception ex)
        {
            return Result<UserDto>.Fail($"Error retrieving user: {ex.Message}");
        }
    }

    public async Task<Result<UserDto>> GetUserByUsernameAsync(string username)
    {
        try
        {
            var normalized = User.Normalize(username);
            var user = await UsersWithRoles().FirstOrDefaultAsync(u => u.UsernameNormalized == normalized && u.IsActive);
            return user == null
                ? Result<UserDto>.Fail("User not found")
                : Result<UserDto>.Ok(await MapToDtoAsync(user));
        }
        catch (Exception ex)
        {
            return Result<UserDto>.Fail($"Error retrieving user: {ex.Message}");
        }
    }

    public async Task<Result<IEnumerable<UserDto>>> GetUsersByCompanyAsync(Guid companyId)
    {
        try
        {
            var users = await UsersWithRoles().Where(u => u.CompanyId == companyId).ToListAsync();

            var dtos = new List<UserDto>();
            foreach (var user in users)
                dtos.Add(await MapToDtoAsync(user));

            return Result<IEnumerable<UserDto>>.Ok(dtos);
        }
        catch (Exception ex)
        {
            return Result<IEnumerable<UserDto>>.Fail($"Error retrieving users: {ex.Message}");
        }
    }

    public async Task<Result<UserDto>> UpdateUserAsync(Guid userId, UpdateUserRequest request, Guid modifiedByUserId)
    {
        try
        {
            var user = await UsersWithRoles().FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
                return Result<UserDto>.Fail("User not found");

            user.FullName = request.FullName;
            user.PhoneNumber = request.PhoneNumber!;
            user.IsActive = request.IsActive;
            user.UpdatedAt = DateTime.UtcNow;
            user.UpdatedByUserId = modifiedByUserId;

            await _context.SaveChangesAsync();

            return Result<UserDto>.Ok(await MapToDtoAsync(user), "User updated successfully");
        }
        catch (Exception ex)
        {
            return Result<UserDto>.Fail($"Error updating user: {ex.Message}");
        }
    }

    public Task<Result> DeactivateUserAsync(Guid userId, Guid modifiedByUserId) =>
        MutateUserAsync(userId, u => StampActive(u, false, modifiedByUserId), "User deactivated successfully", "deactivating user");

    public Task<Result> ActivateUserAsync(Guid userId, Guid modifiedByUserId) =>
        MutateUserAsync(userId, u => StampActive(u, true, modifiedByUserId), "User activated successfully", "activating user");

    public Task<Result> LockUserAsync(Guid userId) =>
        MutateUserAsync(userId, u =>
        {
            u.IsLockedOut = true;
            u.LockoutEndAt = DateTime.UtcNow.AddHours(LockoutHours);
        }, "User locked successfully", "locking user");

    public Task<Result> UnlockUserAsync(Guid userId) =>
        MutateUserAsync(userId, u =>
        {
            u.IsLockedOut = false;
            u.LockoutEndAt = null;
            u.FailedLoginAttempts = 0;
        }, "User unlocked successfully", "unlocking user");

    public Task<bool> UserExistsAsync(Guid userId) => _context.Users.AnyAsync(u => u.Id == userId);

    public async Task<Result<UserDto>> InviteUserAsync(InviteUserRequest request, Guid invitedByUserId)
    {
        try
        {
            var inviter = await _context.Users.FirstOrDefaultAsync(u => u.Id == invitedByUserId);
            if (inviter == null)
                return Result<UserDto>.Fail("Inviting user not found");

            var companyResult = await _companyService.GetCompanyByIdAsync(inviter.CompanyId);
            if (!companyResult.Success || companyResult.Data == null)
                return Result<UserDto>.Fail("Company not found");

            var validation = await _adminValidator.ValidateAsync(new ValidateUserDto
            {
                Email = request.Email,
                Password = request.Password,
                FullName = request.FullName,
                UserName = request.UserName,
                PhoneNumber = request.PhoneNumber ?? string.Empty,
            });
            if (!validation.Success)
                return Result<UserDto>.Fail(validation.Message ?? "Validation failed");

            var normalizedUsername = User.Normalize(request.UserName);
            var normalizedEmail = User.Normalize(request.Email);
            if (await _context.Users.AnyAsync(u => u.UsernameNormalized == normalizedUsername
                                                || u.EmailNormalized == normalizedEmail))
                return Result<UserDto>.Fail("A user with that username or email already exists");

            var user = new User
            {
                TenantId = inviter.TenantId,
                CompanyId = inviter.CompanyId,
                BranchId = request.BranchId ?? inviter.BranchId,
                BusinessUnitId = request.BusinessUnitId ?? inviter.BusinessUnitId,
                Username = request.UserName,
                Email = request.Email,
                FullName = request.FullName,
                PhoneNumber = request.PhoneNumber ?? string.Empty,
                Address = string.Empty,
                PasswordHash = _passwordHasher.HashPassword(null!, request.Password),
                IsActive = true,
                IsEmailVerified = false,
                CreatedByUserId = invitedByUserId,
                CreatedAt = DateTime.UtcNow,
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            _context.UserCompanies.Add(new UserCompany
            {
                UserId = user.Id,
                CompanyId = inviter.CompanyId,
                CompanyName = companyResult.Data.CompanyName,
                CompanySlug = companyResult.Data.CompanySlug ?? string.Empty,
                TenantId = inviter.TenantId,
                DefaultBranchId = user.BranchId ?? Guid.Empty,
                DefaultBusinessUnitId = user.BusinessUnitId,
                IsDefault = true,
                IsActive = true,
                CreatedByUserId = invitedByUserId,
                CreatedAt = DateTime.UtcNow,
            });
            await _context.SaveChangesAsync();

            if (request.RoleId.HasValue)
                await _roleService.AssignRoleToUserAsync(user.Id, request.RoleId.Value, invitedByUserId);

            return Result<UserDto>.Ok(await MapToDtoAsync(user), "User invited successfully");
        }
        catch (Exception ex)
        {
            return Result<UserDto>.Fail($"Error inviting user: {ex.Message}");
        }
    }

    // ── Internals ────────────────────────────────────────────────────────────

    private IQueryable<User> UsersWithRoles() =>
        _context.Users.Include(u => u.UserRoles).ThenInclude(ur => ur.Role);

    private static void StampActive(User user, bool isActive, Guid actorId)
    {
        user.IsActive = isActive;
        user.UpdatedAt = DateTime.UtcNow;
        user.UpdatedByUserId = actorId;
    }

    /// <summary>Loads a user, applies <paramref name="mutate"/>, and saves — the shared shape of the toggle actions.</summary>
    private async Task<Result> MutateUserAsync(Guid userId, Action<User> mutate, string successMessage, string errorVerb)
    {
        try
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
                return Result.Fail("User not found");

            mutate(user);
            await _context.SaveChangesAsync();
            return Result.Ok(successMessage);
        }
        catch (Exception ex)
        {
            return Result.Fail($"Error {errorVerb}: {ex.Message}");
        }
    }

    private async Task<UserDto> MapToDtoAsync(User user)
    {
        var roles = await _roleService.GetUserRolesAsync(user.Id);
        var permissions = await _roleService.GetUserPermissionsAsync(user.Id);

        return new UserDto
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            FullName = user.FullName,
            PhoneNumber = user.PhoneNumber,
            CompanyId = user.CompanyId,
            BranchId = user.BranchId,
            BusinessUnitId = user.BusinessUnitId,
            IsActive = user.IsActive,
            IsEmailVerified = user.IsEmailVerified,
            LastLoginAt = user.LastLoginAt,
            Roles = roles.ToList(),
            Permissions = permissions.ToList()
        };
    }
}
