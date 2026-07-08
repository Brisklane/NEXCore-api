using System.Text.Json;
using Auth.Application.DTOs;
using Auth.Application.Services.Interfaces;
using Auth.Domain.Entities;
using Auth.Domain.Interfaces;
using Auth.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Nexcore.SharedKernel;

namespace Auth.Infrastructure.Services;

/// <summary>
/// CRUD for company-scoped roles, user↔role assignment, and resolution of the effective role and
/// permission codes for a user. Every mutation writes an audit entry via <see cref="IAuditService"/>.
/// </summary>
public class RoleService : IRoleService
{
    private readonly AuthDbContext _context;
    private readonly IAuditService _auditService;

    public RoleService(AuthDbContext context, IAuditService auditService)
    {
        _context = context;
        _auditService = auditService;
    }

    public async Task<Result<RoleResponseDto>> CreateRoleAsync(CreateRoleRequest request, Guid userId)
    {
        try
        {
            var existingRole = await _context.Roles
                .FirstOrDefaultAsync(r => r.CompanyId == request.CompanyId && r.Code == request.Code);
            if (existingRole != null)
                return Result<RoleResponseDto>.Fail("Role code already exists for this company");

            var role = new Role
            {
                CompanyId = request.CompanyId,
                Code = request.Code,
                Name = request.Name,
                Description = request.Description,
                IsActive = true,
                CreatedByUserId = userId,
                CreatedAt = DateTime.UtcNow,
                BranchId = Guid.Empty
            };

            if (request.PermissionIds.Count > 0)
                role.Permissions = await ActivePermissionsByIdAsync(request.PermissionIds);

            _context.Roles.Add(role);
            await _context.SaveChangesAsync();

            await _auditService.LogRoleCreatedAsync(role.Id, role.Name, userId, null, null);

            return Result<RoleResponseDto>.Ok(await MapToDtoAsync(role), "Role created successfully");
        }
        catch (Exception ex)
        {
            return Result<RoleResponseDto>.Fail($"Error creating role: {ex.Message}");
        }
    }

    public async Task<Result<RoleResponseDto>> GetRoleByIdAsync(Guid roleId)
    {
        try
        {
            var role = await _context.Roles
                .Include(r => r.Permissions)
                .FirstOrDefaultAsync(r => r.Id == roleId && r.IsActive);

            return role == null
                ? Result<RoleResponseDto>.Fail("Role not found")
                : Result<RoleResponseDto>.Ok(await MapToDtoAsync(role));
        }
        catch (Exception ex)
        {
            return Result<RoleResponseDto>.Fail($"Error retrieving role: {ex.Message}");
        }
    }

    public async Task<Result<IEnumerable<RoleResponseDto>>> GetRolesByCompanyAsync(Guid companyId)
    {
        try
        {
            var roles = await _context.Roles
                .Where(r => r.CompanyId == companyId && r.IsActive)
                .Include(r => r.Permissions)
                .ToListAsync();

            var dtos = new List<RoleResponseDto>();
            foreach (var role in roles)
                dtos.Add(await MapToDtoAsync(role));

            return Result<IEnumerable<RoleResponseDto>>.Ok(dtos);
        }
        catch (Exception ex)
        {
            return Result<IEnumerable<RoleResponseDto>>.Fail($"Error retrieving roles: {ex.Message}");
        }
    }

    public async Task<Result<RoleResponseDto>> UpdateRoleAsync(Guid roleId, UpdateRoleRequest request, Guid userId)
    {
        try
        {
            var role = await _context.Roles
                .Include(r => r.Permissions)
                .FirstOrDefaultAsync(r => r.Id == roleId);

            if (role == null)
                return Result<RoleResponseDto>.Fail("Role not found");

            var oldValues = SerializeRoleState(role);

            role.Name = request.Name;
            role.Description = request.Description;
            role.IsActive = request.IsActive;
            role.UpdatedAt = DateTime.UtcNow;
            role.UpdatedByUserId = userId;

            if (request.PermissionIds.Count > 0)
            {
                role.Permissions.Clear();
                role.Permissions = await ActivePermissionsByIdAsync(request.PermissionIds);
            }

            await _context.SaveChangesAsync();

            await _auditService.LogRoleUpdatedAsync(roleId, oldValues, SerializeRoleState(role), userId, null, null);

            return Result<RoleResponseDto>.Ok(await MapToDtoAsync(role), "Role updated successfully");
        }
        catch (Exception ex)
        {
            return Result<RoleResponseDto>.Fail($"Error updating role: {ex.Message}");
        }
    }

    public async Task<Result> AssignRoleToUserAsync(Guid userId, Guid roleId, Guid createdByUserId)
    {
        try
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
                return Result.Fail("User not found");

            var role = await _context.Roles.FirstOrDefaultAsync(r => r.Id == roleId);
            if (role == null)
                return Result.Fail("Role not found");

            var alreadyAssigned = await _context.UserRoles
                .AnyAsync(ur => ur.UserId == userId && ur.RoleId == roleId);
            if (alreadyAssigned)
                return Result.Fail("User already has this role");

            _context.UserRoles.Add(new UserRole
            {
                UserId = userId,
                RoleId = roleId,
                IsActive = true,
                CreatedByUserId = createdByUserId,
                CreatedAt = DateTime.UtcNow,
                CompanyId = user.CompanyId,
                BranchId = user.BranchId
            });
            await _context.SaveChangesAsync();

            await _auditService.LogRoleAssignedToUserAsync(userId, roleId, role.Name, createdByUserId, null, null);

            return Result.Ok("Role assigned to user successfully");
        }
        catch (Exception ex)
        {
            return Result.Fail($"Error assigning role: {ex.Message}");
        }
    }

    public async Task<Result> RemoveRoleFromUserAsync(Guid userId, Guid roleId)
    {
        try
        {
            var userRole = await _context.UserRoles
                .Include(ur => ur.Role)
                .FirstOrDefaultAsync(ur => ur.UserId == userId && ur.RoleId == roleId);

            if (userRole == null)
                return Result.Fail("User role not found");

            var roleName = userRole.Role?.Name ?? "Unknown";

            _context.UserRoles.Remove(userRole);
            await _context.SaveChangesAsync();

            // Attribute the removal to whoever last touched the assignment.
            var actorId = userRole.UpdatedByUserId ?? userRole.CreatedByUserId;
            await _auditService.LogRoleRemovedFromUserAsync(userId, roleId, roleName, actorId, null, null);

            return Result.Ok("Role removed from user successfully");
        }
        catch (Exception ex)
        {
            return Result.Fail($"Error removing role: {ex.Message}");
        }
    }

    public async Task<IEnumerable<string>> GetUserPermissionsAsync(Guid userId) =>
        await _context.UserRoles
            .Where(ur => ur.UserId == userId && ur.IsActive)
            .SelectMany(ur => ur.Role!.Permissions)
            .Where(p => p.IsActive)
            .Select(p => p.Code)
            .Distinct()
            .ToListAsync();

    public async Task<IEnumerable<string>> GetUserRolesAsync(Guid userId) =>
        await _context.UserRoles
            .Where(ur => ur.UserId == userId && ur.IsActive)
            .Select(ur => ur.Role!.Code)
            .Distinct()
            .ToListAsync();

    // ── Internals ────────────────────────────────────────────────────────────

    private Task<List<Permission>> ActivePermissionsByIdAsync(IEnumerable<Guid> permissionIds) =>
        _context.Permissions
            .Where(p => permissionIds.Contains(p.Id) && p.IsActive)
            .ToListAsync();

    /// <summary>Serializes the audit-relevant fields of a role for the before/after snapshot.</summary>
    private static string SerializeRoleState(Role role) =>
        JsonSerializer.Serialize(new { role.Name, role.Description, role.IsActive });

    private async Task<RoleResponseDto> MapToDtoAsync(Role role)
    {
        var permissions = await _context.Permissions
            .Where(p => role.Permissions.Select(rp => rp.Id).Contains(p.Id))
            .ToListAsync();

        return new RoleResponseDto
        {
            Id = role.Id,
            CompanyId = role.CompanyId,
            Code = role.Code,
            Name = role.Name,
            Description = role.Description,
            IsActive = role.IsActive,
            Permissions = permissions.Select(p => new PermissionDto
            {
                Id = p.Id,
                Code = p.Code,
                Name = p.Name,
                Description = p.Description,
                IsActive = p.IsActive
            }).ToList(),
            CreatedAt = role.CreatedAt,
            UpdatedAt = role.UpdatedAt
        };
    }
}
