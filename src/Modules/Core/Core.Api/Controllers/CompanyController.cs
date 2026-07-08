using Core.Application.DTOs;
using Core.Application.DTOs.ValidateDtos;
using Core.Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Nexcore.SharedKernel.Api;

namespace Core.Api.Controllers;

/// <summary>
/// Company management endpoints
/// Supports both public registration (no auth) and authenticated operations
/// </summary>
/// <summary>Company endpoints: registration, CRUD, logo storage, and the anonymous slug-based branding/logo lookups for the login page.</summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class CompanyController : ControllerBase
{
    private readonly ICompanyService _companyService;
    private readonly ILogger<CompanyController> _logger;
    private readonly ICompanyValidator _companySetupValidator;

    public CompanyController(ICompanyService companyService, ILogger<CompanyController> logger, ICompanyValidator companySetupValidator)
    {
        _companyService = companyService;
        _logger = logger;
        _companySetupValidator = companySetupValidator;
    }

    /// <summary>
    /// Create a new company, branch, and business unit in a single transaction.
    /// This endpoint does not require authentication.
    /// </summary>
    [HttpPost]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<CompanyDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateCompanyAndUser([FromBody] CreateCompanyAndUserDto request)
    {
        try
        {
            var result = await _companyService.CreateCompanyAndUserAsync(request);

            if (!result.Success)
            {
                return BadRequest(new ApiErrorResponse
                {
                    Errors = [],
                    Message = result.Message
                });
            }

            return Created(string.Empty, new ApiResponse<CompanyDto>
            {
                Data = result.Data,
                Success = true,
                Message = "Company and User created successfully."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating company setup");
            return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" });
        }
    }


    /// <summary>
    /// Get company by ID
    /// Authenticated users can view companies they have access to
    /// </summary>
    /// <param name="companyId">The ID of the company to retrieve</param>
    [HttpGet("{companyId:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<CompanyDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetCompany(Guid companyId)
    {
        try
        {
            _logger.LogInformation("Retrieving company: {CompanyId}", companyId);

            var result = await _companyService.GetCompanyByIdAsync(companyId);
            if (!result.Success)
            {
                return NotFound(new ApiErrorResponse { Message = result.Message });
            }

            return Ok(new ApiResponse<CompanyDto>
            {
                Data = result.Data,
                Success = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving company");
            return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" });
        }
    }

    /// <summary>
    /// Get all companies
    /// Authenticated users can view all companies they have access to.
    /// Super admin can view all registered companies.
    /// </summary>
    /// <remarks>
    /// For super admin dashboard view, use /api/superadmin/companies instead
    /// This endpoint respects user permissions and company associations
    /// </remarks>
    [HttpGet]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<CompanyResponseDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAllCompanies()
    {
        try
        {
            _logger.LogInformation("Retrieving all companies");

            var result = await _companyService.GetAllCompaniesAsync();
            if (!result.Success)
            {
                return BadRequest(new ApiErrorResponse { Message = result.Message, Errors = result.Errors });
            }

            return Ok(new ApiResponse<IEnumerable<CompanyResponseDto>>
            {
                Data = result.Data,
                Success = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving companies");
            return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" });
        }
    }

    /// <summary>
    /// Update company
    /// Only company admins and super admins can update company details
    /// </summary>
    /// <remarks>
    /// Sample request:
    /// 
    ///     PUT /api/company/12345678-1234-1234-1234-123456789012
    ///     Authorization: Bearer {access_token}
    ///     {
    ///         "name": "ACME Corporation Updated",
    ///         "registrationNumber": "987654321",
    ///         "baseCurrencyCode": "USD",
    ///         "isActive": true
    ///     }
    /// </remarks>
    [HttpPut("{companyId:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<CompanyDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateCompany(Guid companyId, [FromBody] UpdateCompanyRequest request)
    {
        try
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userId, out var parsedUserId))
            {
                return Unauthorized(new ApiErrorResponse
                {
                    Message = "Invalid user",
                    Errors = []
                });
            }

            _logger.LogInformation("User {UserId} updating company: {CompanyId}", parsedUserId, companyId);

            var result = await _companyService.UpdateCompanyAsync(companyId, request, parsedUserId);

            if (!result.Success)
            {
                return BadRequest(new ApiErrorResponse
                {
                    Message = result.Message,
                    Errors = []
                });
            }

            return Ok(new ApiResponse<CompanyDto>
            {
                Data = result.Data,
                Success = true,
                Message = result.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating company {CompanyId}", companyId);
            return StatusCode(500, new ApiErrorResponse
            {
                Message = "Internal server error",
                Errors = []
            });
        }
    }

    /// <summary>
    /// Deactivate company
    /// Only company admins and super admins can deactivate companies
    /// </summary>
    /// <remarks>
    /// Deactivates a company (soft delete). The company data is retained but marked as inactive.
    /// All associated records (branches, business units, etc.) will also be affected.
    /// 
    /// This is an administrative action that cannot be undone except by reactivating the company.
    /// </remarks>
    /// <param name="companyId">The ID of the company to deactivate</param>
    [HttpDelete("{companyId:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeactivateCompany(Guid companyId)
    {
        try
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userId, out var parsedUserId))
            {
                return Unauthorized(new ApiErrorResponse { Message = "Invalid user" });
            }

            _logger.LogInformation("User {UserId} deactivating company: {CompanyId}", parsedUserId, companyId);

            var result = await _companyService.DeactivateCompanyAsync(companyId, parsedUserId);
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
            _logger.LogError(ex, "Error deactivating company");
            return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" });
        }
    }

    /// <summary>
    /// Validate company setup before registration
    /// Checks company, branch, and business unit data without persisting anything
    /// </summary>
    /// <remarks>
    /// Use this endpoint to validate the full company setup form before submitting.
    /// Returns the validated payload on success, or an error response on failure.
    /// </remarks>
    /// <summary>
    /// Returns the company logo as an image file.
    /// Uses the CompanyId from the authenticated user's JWT claims.
    /// Returns 404 when no logo has been uploaded for the company.
    /// </summary>
    [HttpGet("logo")]
    [Authorize]
    [Produces("image/png", "image/jpeg", "image/webp", "image/gif")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetLogo()
    {
        try
        {
            var (companyId, _, _) = Nexcore.SharedKernel.Helpers.TenantContextHelper
                .ExtractTenantContext(User);

            var bytes = await _companyService.GetLogoAsync(companyId);
            if (bytes is null || bytes.Length == 0)
                return NotFound(new ApiErrorResponse { Message = "No logo set for this company" });

            var contentType = DetectImageContentType(bytes);
            return File(bytes, contentType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving company logo");
            return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving logo" });
        }
    }

    /// <summary>
    /// Upload or replace the company logo.
    /// Accepts PNG, JPEG, WebP, GIF — max 2 MB.
    /// </summary>
    [HttpPost("logo")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UploadLogo(IFormFile file)
    {
        try
        {
            if (file is null || file.Length == 0)
                return BadRequest(new ApiErrorResponse { Message = "No file provided" });
            if (file.Length > 2 * 1024 * 1024)
                return BadRequest(new ApiErrorResponse { Message = "Logo must be smaller than 2 MB" });

            var allowed = new[] { "image/png", "image/jpeg", "image/webp", "image/gif" };
            if (!allowed.Contains(file.ContentType.ToLowerInvariant()))
                return BadRequest(new ApiErrorResponse { Message = "Only PNG, JPEG, WebP, and GIF logos are accepted" });

            var (companyId, _, _) = Nexcore.SharedKernel.Helpers.TenantContextHelper
                .ExtractTenantContext(User);
            var userId = Guid.TryParse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out var uid) ? uid : Guid.Empty;

            using var ms = new MemoryStream();
            await file.CopyToAsync(ms);
            var bytes = ms.ToArray();

            await _companyService.UpdateLogoAsync(companyId, bytes);

            return Ok(new ApiResponse<bool> { Success = true, Data = true, Message = "Logo uploaded successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading company logo");
            return StatusCode(500, new ApiErrorResponse { Message = "Error uploading logo" });
        }
    }

    private static string DetectImageContentType(byte[] bytes)
    {
        if (bytes.Length >= 4 && bytes[0] == 0x89 && bytes[1] == 0x50) return "image/png";
        if (bytes.Length >= 3 && bytes[0] == 0xFF && bytes[1] == 0xD8) return "image/jpeg";
        if (bytes.Length >= 4 && bytes[0] == 0x52 && bytes[1] == 0x49) return "image/webp";
        return "image/png";  // default
    }

    [HttpPost("validate")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<CompanyDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ValidateCompany([FromBody] CompanyDto request)
    {
        try
        {
            var result = await _companySetupValidator.ValidateAsync(request);

            if (!result.Success)
            {
                return BadRequest(new ApiErrorResponse
                {
                    Message = result.Message,
                    Errors = []
                });
            }

            return Ok(new ApiResponse<CompanyDto>
            {
                Success = true,
                Message = "Validation passed. You may proceed.",
                Data = result.Data
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during company setup validation");
            return StatusCode(500, new ApiErrorResponse
            {
                Message = "Internal server error"
            });
        }
    }

    /// <summary>
    /// Public, pre-login branding for a company subdomain ({slug}.{BaseDomain}). Lets the SPA
    /// render a tenant-specific login page before the user authenticates. Cross-tenant by design.
    /// </summary>
    [HttpGet("by-slug/{slug}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<CompanyBrandingDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBrandingBySlug(string slug)
    {
        try
        {
            var branding = await _companyService.GetBrandingBySlugAsync(slug);
            if (branding == null)
                return NotFound(new ApiErrorResponse { Message = "Workspace not found." });

            return Ok(new ApiResponse<CompanyBrandingDto>
            {
                Data = branding,
                Success = true,
                Message = "OK"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching branding for slug {Slug}", slug);
            return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" });
        }
    }

    /// <summary>
    /// Public company logo for a subdomain login page. Returns the image bytes or 404.
    /// </summary>
    [HttpGet("by-slug/{slug}/logo")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetLogoBySlug(string slug)
    {
        try
        {
            var logo = await _companyService.GetLogoBySlugAsync(slug);
            if (logo == null || logo.Length == 0)
                return NotFound();

            return File(logo, DetectImageContentType(logo));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching logo for slug {Slug}", slug);
            return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" });
        }
    }

    /// <summary>
    /// Check whether a company slug (subdomain) is available — registration helper. Reports
    /// "invalid-format" / "reserved" / "taken" so the UI can give precise feedback.
    /// </summary>
    [HttpGet("slug-available")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<SlugAvailabilityDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> CheckSlugAvailability([FromQuery] string slug)
    {
        try
        {
            var result = await _companyService.CheckSlugAvailabilityAsync(slug ?? string.Empty);
            return Ok(new ApiResponse<SlugAvailabilityDto>
            {
                Data = result,
                Success = true,
                Message = result.Available ? "Available" : "Unavailable"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking slug availability for {Slug}", slug);
            return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" });
        }
    }
}
