using Core.Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Nexcore.SharedKernel.Api;
using Nexcore.SharedKernel.Helpers;

namespace Core.Api.Controllers;

public class InstalledAppsDto
{
    public List<string> Installed { get; set; } = [];
}

public class AppKeysDto
{
    public List<string> Keys { get; set; } = [];
}

/// <summary>
/// Which apps a company has installed.
/// </summary>
/// <remarks>
/// The server is the source of truth so the choice follows a user between devices and
/// browsers. It stores keys only — the catalogue (names, icons, dependencies, which URLs
/// belong to which app) is client-side, so releasing a new app needs no schema change.
///
/// Dependency rules are enforced on the client, which knows the graph. This endpoint is
/// deliberately permissive about *what* is stored: it validates shape and tenancy, not
/// product logic, so an older client cannot be locked out by a newer rule it has not
/// heard of.
/// </remarks>
[ApiController]
[Route("api/core/apps")]
[Authorize]
[Produces("application/json")]
public class AppsController : ControllerBase
{
    private readonly ICompanyAppService _apps;
    private readonly ILogger<AppsController> _logger;

    /// <summary>Guards against a malformed client filling the table with junk keys.</summary>
    private const int MaxKeyLength = 64;
    private const int MaxKeysPerRequest = 50;

    public AppsController(ICompanyAppService apps, ILogger<AppsController> logger)
    {
        _apps = apps;
        _logger = logger;
    }

    private Guid CompanyId
    {
        get
        {
            var (companyId, _, _) = TenantContextHelper.ExtractTenantContext(User);
            return companyId;
        }
    }

    /// <summary>The keys this company currently has installed.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<InstalledAppsDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetInstalled()
    {
        try
        {
            var keys = await _apps.GetInstalledAsync(CompanyId);

            return Ok(new ApiResponse<InstalledAppsDto>
            {
                Success = true,
                Data = new InstalledAppsDto { Installed = keys },
                Message = "Installed apps retrieved",
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving installed apps");
            return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving installed apps" });
        }
    }

    [HttpPost("install")]
    [ProducesResponseType(typeof(ApiResponse<InstalledAppsDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public Task<IActionResult> Install([FromBody] AppKeysDto dto) => SetInstalled(dto, install: true);

    [HttpPost("uninstall")]
    [ProducesResponseType(typeof(ApiResponse<InstalledAppsDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public Task<IActionResult> Uninstall([FromBody] AppKeysDto dto) => SetInstalled(dto, install: false);

    private async Task<IActionResult> SetInstalled(AppKeysDto dto, bool install)
    {
        var keys = (dto?.Keys ?? [])
            .Where(k => !string.IsNullOrWhiteSpace(k))
            .Select(k => k.Trim().ToLowerInvariant())
            .Distinct()
            .ToList();

        if (keys.Count == 0)
            return BadRequest(new ApiErrorResponse { Message = "At least one app key is required" });
        if (keys.Count > MaxKeysPerRequest)
            return BadRequest(new ApiErrorResponse { Message = $"At most {MaxKeysPerRequest} keys per request" });
        if (keys.Any(k => k.Length > MaxKeyLength))
            return BadRequest(new ApiErrorResponse { Message = $"App keys cannot exceed {MaxKeyLength} characters" });

        try
        {
            var companyId = CompanyId;
            var userId = TenantContextHelper.ExtractUserId(User);
            var installed = await _apps.SetInstalledAsync(companyId, keys, install, userId);

            _logger.LogInformation("Company {CompanyId} {Action} apps: {Keys}",
                companyId, install ? "installed" : "uninstalled", string.Join(", ", keys));

            return Ok(new ApiResponse<InstalledAppsDto>
            {
                Success = true,
                Data = new InstalledAppsDto { Installed = installed },
                Message = install ? "Apps installed" : "Apps uninstalled",
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating installed apps");
            return StatusCode(500, new ApiErrorResponse { Message = "Error updating installed apps" });
        }
    }
}
