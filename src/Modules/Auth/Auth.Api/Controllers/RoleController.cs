using Auth.Application.DTOs;
using Auth.Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Nexcore.SharedKernel.Api;
using Nexcore.SharedKernel.Audit;
using System.Security.Claims;

namespace Auth.Api.Controllers;

/// <summary>
/// Role and permission management endpoints with audit trail
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Authorize]
public class RoleController : ControllerBase
{
    private readonly IRoleService _roleService;
    private readonly Nexcore.SharedKernel.Audit.IAuditService _auditService;
    private readonly ILogger<RoleController> _logger;

    public RoleController(IRoleService roleService, Nexcore.SharedKernel.Audit.IAuditService auditService, ILogger<RoleController> logger)
    {
        _roleService = roleService;
        _auditService = auditService;
        _logger = logger;
    }

    /// <summary>
    /// Create a new role
    /// </summary>
    /// <remarks>
    /// Sample request:
    /// 
    ///     POST /api/role
    ///     {
    ///         "companyId": "12345678-1234-1234-1234-123456789012",
    ///         "code": "ADMIN",
    ///         "name": "Administrator",
    ///         "description": "Administrator role with full access",
    ///         "permissionIds": ["12345678-1234-1234-1234-123456789012"]
    ///     }
    /// </remarks>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<RoleResponseDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [Authorize(Policy = "ROLE_CREATE")]
    public async Task<IActionResult> CreateRole([FromBody] CreateRoleRequest request)
    {
        try
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userId, out var parsedUserId))
            {
                return Unauthorized(new ApiErrorResponse { Message = "Invalid user" });
            }

            var result = await _roleService.CreateRoleAsync(request, parsedUserId);
            if (!result.Success)
            {
                return BadRequest(new ApiErrorResponse { Message = result.Message, Errors = result.Errors });
            }

            return Created(string.Empty, new ApiResponse<RoleResponseDto>
            {
                Data = result.Data,
                Message = result.Message,
                Success = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating role");
            return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" });
        }
    }

    /// <summary>
    /// Get role by ID
    /// </summary>
    /// <param name="roleId">The ID of the role to retrieve</param>
    [HttpGet("{roleId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<RoleResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [Authorize(Policy = "ROLE_VIEW")]
    public async Task<IActionResult> GetRole(Guid roleId)
    {
        try
        {
            var result = await _roleService.GetRoleByIdAsync(roleId);
            if (!result.Success)
            {
                return NotFound(new ApiErrorResponse { Message = result.Message });
            }

            return Ok(new ApiResponse<RoleResponseDto>
            {
                Data = result.Data,
                Success = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving role");
            return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" });
        }
    }

    /// <summary>
    /// Get all roles for a company
    /// </summary>
    /// <param name="companyId">The ID of the company</param>
    [HttpGet("company/{companyId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<RoleResponseDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [Authorize(Policy = "ROLE_VIEW")]
    public async Task<IActionResult> GetRolesByCompany(Guid companyId)
    {
        try
        {
            var result = await _roleService.GetRolesByCompanyAsync(companyId);
            if (!result.Success)
            {
                return BadRequest(new ApiErrorResponse { Message = result.Message, Errors = result.Errors });
            }

            return Ok(new ApiResponse<IEnumerable<RoleResponseDto>>
            {
                Data = result.Data,
                Success = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving roles");
            return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" });
        }
    }

    /// <summary>
    /// Update role
    /// </summary>
    /// <remarks>
    /// Sample request:
    /// 
    ///     PUT /api/role/12345678-1234-1234-1234-123456789012
    ///     {
    ///         "name": "Super Administrator",
    ///         "description": "Super admin role",
    ///         "isActive": true,
    ///         "permissionIds": ["12345678-1234-1234-1234-123456789012"]
    ///     }
    /// </remarks>
    [HttpPut("{roleId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<RoleResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Authorize(Policy = "ROLE_EDIT")]
    public async Task<IActionResult> UpdateRole(Guid roleId, [FromBody] UpdateRoleRequest request)
    {
        try
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userId, out var parsedUserId))
            {
                return Unauthorized(new ApiErrorResponse { Message = "Invalid user" });
            }

            var result = await _roleService.UpdateRoleAsync(roleId, request, parsedUserId);
            if (!result.Success)
            {
                return BadRequest(new ApiErrorResponse { Message = result.Message, Errors = result.Errors });
            }

            return Ok(new ApiResponse<RoleResponseDto>
            {
                Data = result.Data,
                Message = result.Message,
                Success = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating role");
            return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" });
        }
    }

    /// <summary>
    /// Assign role to user
    /// </summary>
    /// <remarks>
    /// Sample request:
    /// 
    ///     POST /api/role/assign
    ///     {
    ///         "userId": "12345678-1234-1234-1234-123456789012",
    ///         "roleId": "12345678-1234-1234-1234-123456789012"
    ///     }
    /// </remarks>
    [HttpPost("assign")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [Authorize(Policy = "ROLE_EDIT")]
    public async Task<IActionResult> AssignRoleToUser([FromBody] AssignRoleRequest request)
    {
        try
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userId, out var parsedUserId))
            {
                return Unauthorized(new ApiErrorResponse { Message = "Invalid user" });
            }

            var result = await _roleService.AssignRoleToUserAsync(request.UserId, request.RoleId, parsedUserId);
            if (!result.Success)
            {
                return BadRequest(new ApiErrorResponse { Message = result.Message });
            }

            return Ok(new ApiResponse
            {
                Message = result.Message,
                Success = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning role");
            return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" });
        }
    }

    /// <summary>
    /// Remove role from user
    /// </summary>
    /// <remarks>
    /// Sample request:
    /// 
    ///     POST /api/role/remove
    ///     {
    ///         "userId": "12345678-1234-1234-1234-123456789012",
    ///         "roleId": "12345678-1234-1234-1234-123456789012"
    ///     }
    /// </remarks>
    [HttpPost("remove")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [Authorize(Policy = "ROLE_EDIT")]
    public async Task<IActionResult> RemoveRoleFromUser([FromBody] AssignRoleRequest request)
    {
        try
        {
            var result = await _roleService.RemoveRoleFromUserAsync(request.UserId, request.RoleId);
            if (!result.Success)
            {
                return BadRequest(new ApiErrorResponse { Message = result.Message });
            }

            return Ok(new ApiResponse
            {
                Message = result.Message,
                Success = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing role");
            return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" });
        }
    }

    /// <summary>
    /// Get audit logs for a role entity
    /// </summary>
    /// <remarks>
    /// Retrieves all audit trail entries for a specific role, showing all changes made to that role over time.
    /// </remarks>
    /// <param name="roleId">The ID of the role</param>
    /// <param name="pageNumber">Page number for pagination (default: 1)</param>
    /// <param name="pageSize">Number of records per page (default: 20, max: 100)</param>
    [HttpGet("{roleId:guid}/audit-logs")]
    [ProducesResponseType(typeof(ApiResponse<AuditLogPagedResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [Authorize(Policy = "ROLE_VIEW")]
    public async Task<IActionResult> GetRoleAuditLogs(Guid roleId, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20)
    {
        try
        {
            var logs = await _auditService.GetEntityAuditLogsAsync(roleId, "Role", pageNumber, pageSize);
            if (logs.Items.Count == 0)
            {
                return NotFound(new ApiErrorResponse { Message = "No audit logs found for this role" });
            }

            return Ok(new ApiResponse<AuditLogPagedResponse>
            {
                Data = logs,
                Success = true,
                Message = $"Retrieved {logs.Items.Count} audit log entries"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving role audit logs");
            return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" });
        }
    }

    /// <summary>
    /// Get audit logs for user role assignments
    /// </summary>
    /// <remarks>
    /// Retrieves all audit trail entries for a user's role changes, showing when roles were assigned or removed.
    /// </remarks>
    /// <param name="userId">The ID of the user</param>
    /// <param name="pageNumber">Page number for pagination (default: 1)</param>
    /// <param name="pageSize">Number of records per page (default: 20, max: 100)</param>
    [HttpGet("user/{userId:guid}/audit-logs")]
    [ProducesResponseType(typeof(ApiResponse<AuditLogPagedResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetUserRoleAuditLogs(Guid userId, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20)
    {
        try
        {
            var filter = new AuditLogFilterRequest
            {
                EntityType = "UserRole",
                UserId = userId,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            var logs = await _auditService.QueryAuditLogsAsync(filter);
            if (logs.Items.Count == 0)
            {
                return NotFound(new ApiErrorResponse { Message = "No audit logs found for this user" });
            }

            return Ok(new ApiResponse<AuditLogPagedResponse>
            {
                Data = logs,
                Success = true,
                Message = $"Retrieved {logs.Items.Count} role change audit entries"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user role audit logs");
            return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" });
        }
    }

    /// <summary>
    /// Get all audit activity for a specific user
    /// </summary>
    /// <remarks>
    /// Retrieves all audit trail entries created by a specific user, showing all actions and changes they made.
    /// </remarks>
    /// <param name="userId">The ID of the user</param>
    /// <param name="pageNumber">Page number for pagination (default: 1)</param>
    /// <param name="pageSize">Number of records per page (default: 20, max: 100)</param>
    [HttpGet("activity/{userId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<AuditLogPagedResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [Authorize(Policy = "ROLE_VIEW")]
    public async Task<IActionResult> GetUserActivity(Guid userId, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20)
    {
        try
        {
            var logs = await _auditService.GetUserActivityLogsAsync(userId, pageNumber, pageSize);
            if (logs.Items.Count == 0)
            {
                return NotFound(new ApiErrorResponse { Message = "No activity found for this user" });
            }

            return Ok(new ApiResponse<AuditLogPagedResponse>
            {
                Data = logs,
                Success = true,
                Message = $"Retrieved {logs.Items.Count} activity log entries"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user activity logs");
            return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" });
        }
    }
}
