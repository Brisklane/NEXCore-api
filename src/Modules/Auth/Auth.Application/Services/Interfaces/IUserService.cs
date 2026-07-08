using Auth.Application.DTOs;
using Core.Application.DTOs.ValidateDtos;
using Nexcore.SharedKernel;

namespace Auth.Application.Services.Interfaces;

/// <summary>
/// User administration: lookups (by id/username/company), profile edits, activation and lockout
/// toggles, and admin invitation of a user into an existing company.
/// </summary>
public interface IUserService
{
    /// <summary>Loads a user together with their resolved roles and permissions.</summary>
    Task<Result<UserDto>> GetUserByIdAsync(Guid userId);

    Task<Result<UserDto>> GetUserByUsernameAsync(string username);

    Task<Result<IEnumerable<UserDto>>> GetUsersByCompanyAsync(Guid companyId);

    Task<Result<UserDto>> UpdateUserAsync(Guid userId, UpdateUserRequest request, Guid modifiedByUserId);

    Task<Result> DeactivateUserAsync(Guid userId, Guid modifiedByUserId);

    Task<Result> ActivateUserAsync(Guid userId, Guid modifiedByUserId);

    /// <summary>Locks the account immediately, independent of the failed-attempt policy.</summary>
    Task<Result> LockUserAsync(Guid userId);

    Task<Result> UnlockUserAsync(Guid userId);

    Task<bool> UserExistsAsync(Guid userId);

    /// <summary>Admin-invites a user straight into an existing company — no self-registration or company creation.</summary>
    Task<Result<UserDto>> InviteUserAsync(InviteUserRequest request, Guid invitedByUserId);
}
