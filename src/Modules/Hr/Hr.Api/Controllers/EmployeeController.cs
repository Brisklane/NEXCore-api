using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Nexcore.SharedKernel.Api;
using Nexcore.SharedKernel.Helpers;
using Hr.Application.DTOs;
using Hr.Application.Services.Interfaces;

namespace Hr.Api.Controllers;

[ApiController]
[Route("api/v1/hr/[controller]")]
[Produces("application/json")]
[Authorize]
public class EmployeeController : ControllerBase
{
    private readonly IEmployeeService _service;
    private readonly ILogger<EmployeeController> _logger;

    public EmployeeController(IEmployeeService service, ILogger<EmployeeController> logger)
    { _service = service; _logger = logger; }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<EmployeeDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        try { return Ok(new ApiResponse<IEnumerable<EmployeeDto>> { Success = true, Data = await _service.GetAllAsync() }); }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving employees"); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<EmployeeDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        try
        {
            var data = await _service.GetByIdAsync(id);
            if (data is null) return NotFound(new ApiErrorResponse { Message = "Employee not found" });
            return Ok(new ApiResponse<EmployeeDto> { Success = true, Data = data });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving employee"); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    /// <summary>Get employees by department</summary>
    [HttpGet("by-department/{departmentId}")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<EmployeeDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByDepartment(Guid departmentId)
    {
        try { return Ok(new ApiResponse<IEnumerable<EmployeeDto>> { Success = true, Data = await _service.GetByDepartmentAsync(departmentId) }); }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving employees by department"); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    /// <summary>Get employees by designation</summary>
    [HttpGet("by-designation/{designationId}")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<EmployeeDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByDesignation(Guid designationId)
    {
        try { return Ok(new ApiResponse<IEnumerable<EmployeeDto>> { Success = true, Data = await _service.GetByDesignationAsync(designationId) }); }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving employees by designation"); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    /// <summary>Get direct reports for a manager</summary>
    [HttpGet("{managerId}/direct-reports")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<EmployeeDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDirectReports(Guid managerId)
    {
        try { return Ok(new ApiResponse<IEnumerable<EmployeeDto>> { Success = true, Data = await _service.GetDirectReportsAsync(managerId) }); }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving direct reports"); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<EmployeeDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreateEmployeeDto request)
    {
        try
        {
            var result = await _service.CreateAsync(request, TenantContextHelper.ExtractUserId(User));
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, new ApiResponse<EmployeeDto> { Success = true, Message = "Employee created successfully", Data = result });
        }
        catch (InvalidOperationException ex) { return BadRequest(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error creating employee"); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse<EmployeeDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateEmployeeDto request)
    {
        try
        {
            var result = await _service.UpdateAsync(id, request, TenantContextHelper.ExtractUserId(User));
            return Ok(new ApiResponse<EmployeeDto> { Success = true, Message = "Employee updated successfully", Data = result });
        }
        catch (InvalidOperationException ex) { return NotFound(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error updating employee"); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid id)
    {
        try { await _service.DeleteAsync(id, TenantContextHelper.ExtractUserId(User)); return NoContent(); }
        catch (InvalidOperationException ex) { return NotFound(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error deleting employee"); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }
}
