using Auth.Application.DTOs;
using Nexcore.SharedKernel;

namespace Auth.Application.Services.Interfaces;

/// <summary>
/// Manages roles and their assignment to users, and resolves the effective role/permission sets
/// used to build a user's access token.
/// </summary>
public interface IRoleService
{
    Task<Result<RoleResponseDto>> CreateRoleAsync(CreateRoleRequest request, Guid userId);

    Task<Result<RoleResponseDto>> GetRoleByIdAsync(Guid roleId);

    Task<Result<IEnumerable<RoleResponseDto>>> GetRolesByCompanyAsync(Guid companyId);

    Task<Result<RoleResponseDto>> UpdateRoleAsync(Guid roleId, UpdateRoleRequest request, Guid userId);

    Task<Result> AssignRoleToUserAsync(Guid userId, Guid roleId, Guid createdByUserId);

    Task<Result> RemoveRoleFromUserAsync(Guid userId, Guid roleId);

    /// <summary>Flattened permission codes across all of the user's roles — the source for token claims.</summary>
    Task<IEnumerable<string>> GetUserPermissionsAsync(Guid userId);

    /// <summary>The user's role codes.</summary>
    Task<IEnumerable<string>> GetUserRolesAsync(Guid userId);
}
