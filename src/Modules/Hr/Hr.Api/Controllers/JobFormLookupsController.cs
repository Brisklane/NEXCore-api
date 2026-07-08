using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Nexcore.SharedKernel.Api;
using Hr.Application.DTOs;
using Hr.Application.Services.Interfaces;

namespace Hr.Api.Controllers;

/// <summary>
/// Provides pre-loaded lookup lists for the Job Creation form.
/// Call GET /api/v1/hr/job-form/lookups to fetch all dropdowns in a single request
/// instead of making individual calls per entity.
/// </summary>
[ApiController]
[Route("api/v1/hr/job-form")]
[Produces("application/json")]
[Authorize]
public class JobFormLookupsController : ControllerBase
{
    private readonly IDepartmentService _departmentService;
    private readonly IDesignationService _designationService;
    private readonly ILogger<JobFormLookupsController> _logger;

    public JobFormLookupsController(
        IDepartmentService departmentService,
        IDesignationService designationService,
        ILogger<JobFormLookupsController> logger)
    {
        _departmentService = departmentService;
        _designationService = designationService;
        _logger = logger;
    }

    /// <summary>
    /// Returns all dropdown lists required by the Job Creation form in a single call.
    /// Each list contains { value: "&lt;guid&gt;", label: "&lt;display text&gt;" } items.
    /// </summary>
    /// <remarks>
    /// Included lists:
    /// - **departments** — active departments, sorted by name
    /// - **designations** — active designations, sorted by name
    /// </remarks>
    [HttpGet("lookups")]
    [ProducesResponseType(typeof(ApiResponse<JobFormLookupsDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetJobFormLookups()
    {
        try
        {
            // Run both lookups in parallel — no dependency between them
            var departmentsTask   = _departmentService.GetLookupListAsync();
            var designationsTask  = _designationService.GetLookupListAsync();

            await Task.WhenAll(departmentsTask, designationsTask);

            var result = new JobFormLookupsDto
            {
                Departments  = (await departmentsTask).ToList(),
                Designations = (await designationsTask).ToList()
            };

            return Ok(new ApiResponse<JobFormLookupsDto>
            {
                Success = true,
                Data    = result
            });
        }
        catch (InvalidOperationException ex)
        {
            return Unauthorized(new ApiErrorResponse { Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving job form lookups");
            return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" });
        }
    }
}
