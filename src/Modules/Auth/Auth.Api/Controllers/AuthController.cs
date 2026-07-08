using Auth.Application.DTOs;
using Auth.Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Nexcore.SharedKernel.Api;

namespace Auth.Api.Controllers;

/// <summary>
/// Authentication endpoints for login, registration, and token refresh
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly IAuthenticationService _authenticationService;
    private readonly IUserService _userService;
    private readonly IUserValidator _userValidator;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthenticationService authenticationService, ILogger<AuthController> logger, IUserService userService, IUserValidator userValidator)
    {
        _authenticationService = authenticationService;
        _logger = logger;
        _userService = userService;
        _userValidator = userValidator;
    }

    /// <summary>
    /// Register a new user
    /// </summary>
    /// <remarks>
    /// Sample request:
    /// 
    ///     POST /api/auth/register
    ///     {
    ///         "email": "user@example.com",
    ///         "username": "john.doe",
    ///         "firstName": "John",
    ///         "lastName": "Doe",
    ///         "password": "P@ssw0rd123!",
    ///         "companyId": "12345678-1234-1234-1234-123456789012"
    ///     }
    /// </remarks>
    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] CreateUserDto request)
    {
        try
        {
            var result = await _authenticationService.RegisterAsync(request);
            if (!result.Success)
            {
                return BadRequest(new ApiErrorResponse
                {
                    Message = result.Message,
                    Errors = []
                });
            }

            return Ok(new ApiResponse
            {
                Message = result.Message,
                Success = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering user");
            return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" });
        }
    }

    /// <summary>
    /// Validates a registration payload without persisting anything — lets the sign-up form
    /// check the input before it is submitted, returning a success message or the first error.
    /// </summary>
    [HttpPost("validate")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ValidateUser([FromBody] ValidateUserDto request)
    {
        try
        {
            var result = await _userValidator.ValidateAsync(request);

            if (!result.Success)
            {
                return BadRequest(new ApiErrorResponse
                {
                    Message = result.Message,
                    Errors = []
                });
            }

            return Ok(new ApiResponse
            {
                Success = true,
                Message = "Validation passed. You may proceed.",
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during User validation");
            return StatusCode(500, new ApiErrorResponse
            {
                Message = "Internal server error"
            });
        }
    }

    /// <summary>
    /// Login with username and password
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<LoginResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        try
        {
            var result = await _authenticationService.LoginAsync(request);
            if (!result.Success)
                return Unauthorized(new ApiErrorResponse { Message = result.Message });

            return Ok(new ApiResponse<LoginResponse>
            {
                Data = result.Data,
                Message = result.Message,
                Success = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging in");
            return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" });
        }
    }

    /// <summary>
    /// Step 2 of login: exchange a pre-auth token and a selected company for a scoped JWT.
    /// </summary>
    [HttpPost("select-company")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<SelectCompanyResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> SelectCompany([FromBody] SelectCompanyRequest request)
    {
        try
        {
            var result = await _authenticationService.SelectCompanyAsync(request);
            if (!result.Success)
                return Unauthorized(new ApiErrorResponse { Message = result.Message });

            return Ok(new ApiResponse<SelectCompanyResponse>
            {
                Data = result.Data,
                Message = result.Message,
                Success = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error selecting company");
            return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" });
        }
    }

    /// <summary>
    /// Refresh access token using refresh token
    /// </summary>
    /// <remarks>
    /// Sample request:
    /// 
    ///     POST /api/auth/refresh-token
    ///     {
    ///         "refreshToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
    ///     }
    /// 
    /// Use this endpoint to obtain a new access token when the current one expires.
    /// </remarks>
    [HttpPost("refresh-token")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<LoginResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        try
        {
            var result = await _authenticationService.RefreshTokenAsync(request);
            if (!result.Success)
            {
                return Unauthorized(new ApiErrorResponse { Message = result.Message });
            }

            return Ok(new ApiResponse<LoginResponse>
            {
                Data = result.Data,
                Message = result.Message,
                Success = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing token");
            return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" });
        }
    }

    /// <summary>
    /// Logout and revoke refresh token
    /// </summary>
    /// <remarks>
    /// Sample request:
    /// 
    ///     POST /api/auth/logout
    ///     {
    ///         "refreshToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
    ///     }
    /// 
    /// Invalidates the refresh token and logs the user out.
    /// </remarks>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Logout([FromBody] LogoutRequest request)
    {
        try
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userId, out var parsedUserId))
            {
                return Unauthorized(new ApiErrorResponse { Message = "Invalid user" });
            }

            var result = await _authenticationService.LogoutAsync(parsedUserId, request.RefreshToken);
            
            return Ok(new ApiResponse
            {
                Message = result.Message,
                Success = result.Success
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging out");
            return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" });
        }
    }

    /// <summary>
    /// Change password
    /// </summary>
    /// <remarks>
    /// Sample request:
    /// 
    ///     POST /api/auth/change-password
    ///     {
    ///         "currentPassword": "OldP@ssw0rd123!",
    ///         "newPassword": "NewP@ssw0rd123!",
    ///         "confirmPassword": "NewP@ssw0rd123!"
    ///     }
    /// 
    /// Requires authentication. User must provide their current password to change it.
    /// </remarks>
    [HttpPost("change-password")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        try
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userId, out var parsedUserId))
            {
                return Unauthorized(new ApiErrorResponse { Message = "Invalid user" });
            }

            var result = await _authenticationService.ChangePasswordAsync(parsedUserId, request);
            if (!result.Success)
            {
                return BadRequest(new ApiErrorResponse { Message = result.Message, Errors = result.Errors });
            }

            return Ok(new ApiResponse
            {
                Message = result.Message,
                Success = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing password");
            return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" });
        }
    }

    /// <summary>
    /// Switch the active company/branch/business unit context and return a new scoped JWT.
    /// Called when the user selects a different entry from the header dropdowns. The tenant is
    /// taken from the current token; the requested company/branch/business unit chain is validated
    /// against that tenant (via the Core module) before a new token is issued.
    /// </summary>
    [HttpPost("switch-context")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<SwitchContextResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> SwitchContext([FromBody] SwitchContextRequest request)
    {
        try
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userId, out var parsedUserId))
                return Unauthorized(new ApiErrorResponse { Message = "Invalid user" });

            var tenantId = Nexcore.SharedKernel.Helpers.TenantContextHelper.ExtractTenantId(User);
            if (tenantId is null || tenantId == Guid.Empty)
                return Unauthorized(new ApiErrorResponse { Message = "Invalid tenant" });

            var result = await _authenticationService.SwitchContextAsync(parsedUserId, tenantId.Value, request);
            if (!result.Success)
                return BadRequest(new ApiErrorResponse { Message = result.Message });

            return Ok(new ApiResponse<SwitchContextResponse>
            {
                Data = result.Data,
                Message = result.Message,
                Success = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error switching context");
            return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" });
        }
    }

    /// <summary>
    /// Issue a one-time, short-lived handoff code that transfers the current session to another
    /// company's subdomain ({slug}.{BaseDomain}). The caller must be authenticated and have access
    /// to the target company. The caller then redirects to
    /// https://{CompanySlug}.{BaseDomain}/auth/handoff?code={Code}.
    /// </summary>
    [HttpPost("handoff")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<HandoffResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Handoff([FromBody] HandoffRequest request)
    {
        try
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userId, out var parsedUserId))
                return Unauthorized(new ApiErrorResponse { Message = "Invalid user" });

            var result = await _authenticationService.IssueHandoffAsync(parsedUserId, request.CompanyId);
            if (!result.Success)
                return BadRequest(new ApiErrorResponse { Message = result.Message });

            return Ok(new ApiResponse<HandoffResponse>
            {
                Data = result.Data,
                Message = result.Message,
                Success = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error issuing handoff");
            return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" });
        }
    }

    /// <summary>
    /// Redeem a one-time handoff code (anonymous) for a full company-scoped session. Called by the
    /// destination subdomain's SPA when it loads with a ?code= parameter.
    /// </summary>
    [HttpPost("handoff/exchange")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<LoginResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> HandoffExchange([FromBody] HandoffExchangeRequest request)
    {
        try
        {
            var result = await _authenticationService.ExchangeHandoffAsync(request.Code);
            if (!result.Success)
                return Unauthorized(new ApiErrorResponse { Message = result.Message });

            return Ok(new ApiResponse<LoginResponse>
            {
                Data = result.Data,
                Message = result.Message,
                Success = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exchanging handoff code");
            return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" });
        }
    }

    /// <summary>
    /// Verify email address
    /// </summary>
    /// <remarks>
    /// Marks an email address as verified for the specified user.
    /// Requires authentication.
    /// </remarks>
    /// <param name="userId">The ID of the user whose email should be verified</param>
    [HttpPost("verify-email/{userId:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> VerifyEmail(Guid userId)
    {
        try
        {
            var result = await _authenticationService.VerifyEmailAsync(userId);
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
            _logger.LogError(ex, "Error verifying email");
            return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" });
        }
    }
}

/// <summary>Body for logout: the refresh token to revoke.</summary>
public class LogoutRequest
{
    public required string RefreshToken { get; set; }
}
