using Auth.Application.DTOs;
using Auth.Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Nexcore.SharedKernel.Api;

namespace Auth.Api.Controllers;

/// <summary>
/// Tenant-admin endpoints for managing the users belonging to a company
/// (list, update, activate/deactivate, lock/unlock, invite).
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Authorize]
public class UserAdminController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ILogger<UserAdminController> _logger;

    public UserAdminController(IUserService userService, ILogger<UserAdminController> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    /// <summary>
    /// List all users belonging to a company (active and inactive).
    /// </summary>
    [HttpGet("company/{companyId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<UserDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetUsersByCompany(Guid companyId)
    {
        try
        {
            var result = await _userService.GetUsersByCompanyAsync(companyId);
            if (!result.Success)
            {
                return BadRequest(new ApiErrorResponse { Message = result.Message, Errors = result.Errors });
            }

            return Ok(new ApiResponse<IEnumerable<UserDto>> { Data = result.Data, Success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving company users");
            return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" });
        }
    }

    /// <summary>
    /// Get a single user by ID.
    /// </summary>
    [HttpGet("{userId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetUser(Guid userId)
    {
        try
        {
            var result = await _userService.GetUserByIdAsync(userId);
            if (!result.Success)
            {
                return NotFound(new ApiErrorResponse { Message = result.Message });
            }

            return Ok(new ApiResponse<UserDto> { Data = result.Data, Success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user");
            return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" });
        }
    }

    /// <summary>
    /// Update a user's name / phone / active flag.
    /// </summary>
    [HttpPut("{userId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdateUser(Guid userId, [FromBody] UpdateUserRequest request)
    {
        try
        {
            var modifiedByUserId = CurrentUserId();
            if (modifiedByUserId == null)
            {
                return Unauthorized(new ApiErrorResponse { Message = "Invalid user" });
            }

            var result = await _userService.UpdateUserAsync(userId, request, modifiedByUserId.Value);
            if (!result.Success)
            {
                return BadRequest(new ApiErrorResponse { Message = result.Message, Errors = result.Errors });
            }

            return Ok(new ApiResponse<UserDto> { Data = result.Data, Message = result.Message, Success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user");
            return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" });
        }
    }

    /// <summary>
    /// Reactivate a previously deactivated user.
    /// </summary>
    [HttpPost("{userId:guid}/activate")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ActivateUser(Guid userId)
    {
        try
        {
            var modifiedByUserId = CurrentUserId();
            if (modifiedByUserId == null)
            {
                return Unauthorized(new ApiErrorResponse { Message = "Invalid user" });
            }

            var result = await _userService.ActivateUserAsync(userId, modifiedByUserId.Value);
            if (!result.Success)
            {
                return BadRequest(new ApiErrorResponse { Message = result.Message, Errors = result.Errors });
            }

            return Ok(new ApiResponse { Message = result.Message, Success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error activating user");
            return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" });
        }
    }

    /// <summary>
    /// Deactivate (soft-disable) a user.
    /// </summary>
    [HttpPost("{userId:guid}/deactivate")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> DeactivateUser(Guid userId)
    {
        try
        {
            var modifiedByUserId = CurrentUserId();
            if (modifiedByUserId == null)
            {
                return Unauthorized(new ApiErrorResponse { Message = "Invalid user" });
            }

            var result = await _userService.DeactivateUserAsync(userId, modifiedByUserId.Value);
            if (!result.Success)
            {
                return BadRequest(new ApiErrorResponse { Message = result.Message, Errors = result.Errors });
            }

            return Ok(new ApiResponse { Message = result.Message, Success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deactivating user");
            return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" });
        }
    }

    /// <summary>
    /// Lock a user account for 24 hours.
    /// </summary>
    [HttpPost("{userId:guid}/lock")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> LockUser(Guid userId)
    {
        try
        {
            var result = await _userService.LockUserAsync(userId);
            if (!result.Success)
            {
                return BadRequest(new ApiErrorResponse { Message = result.Message, Errors = result.Errors });
            }

            return Ok(new ApiResponse { Message = result.Message, Success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error locking user");
            return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" });
        }
    }

    /// <summary>
    /// Unlock a user account.
    /// </summary>
    [HttpPost("{userId:guid}/unlock")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UnlockUser(Guid userId)
    {
        try
        {
            var result = await _userService.UnlockUserAsync(userId);
            if (!result.Success)
            {
                return BadRequest(new ApiErrorResponse { Message = result.Message, Errors = result.Errors });
            }

            return Ok(new ApiResponse { Message = result.Message, Success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unlocking user");
            return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" });
        }
    }

    /// <summary>
    /// Invite (directly create) a new user into the calling admin's own company.
    /// </summary>
    /// <remarks>
    /// Sample request:
    ///
    ///     POST /api/UserAdmin/invite
    ///     {
    ///         "email": "jane@acme.com",
    ///         "password": "Str0ng!Pass",
    ///         "fullName": "Jane Doe",
    ///         "userName": "jane.doe",
    ///         "phoneNumber": "",
    ///         "roleId": null
    ///     }
    /// </remarks>
    [HttpPost("invite")]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> InviteUser([FromBody] InviteUserRequest request)
    {
        try
        {
            var invitedByUserId = CurrentUserId();
            if (invitedByUserId == null)
            {
                return Unauthorized(new ApiErrorResponse { Message = "Invalid user" });
            }

            var result = await _userService.InviteUserAsync(request, invitedByUserId.Value);
            if (!result.Success)
            {
                return BadRequest(new ApiErrorResponse { Message = result.Message, Errors = result.Errors });
            }

            return Created(string.Empty, new ApiResponse<UserDto> { Data = result.Data, Message = result.Message, Success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inviting user");
            return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" });
        }
    }

    private Guid? CurrentUserId()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userId, out var parsedUserId) ? parsedUserId : null;
    }
}
