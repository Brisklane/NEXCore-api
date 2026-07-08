using Core.Application.DTOs;
using Core.Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Nexcore.SharedKernel.Api;

namespace Core.Api.Controllers;

/// <summary>Business-unit CRUD within a branch.</summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class BusinessUnitController : ControllerBase
{
    private readonly IBusinessUnitService _businessUnitService;
    private readonly ILogger<BusinessUnitController> _logger;

    public BusinessUnitController(IBusinessUnitService businessUnitService, ILogger<BusinessUnitController> logger)
    {
        _businessUnitService = businessUnitService;
        _logger = logger;
    }

    // Public: Create business unit (no authorization)
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<BusinessUnitResponseDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateBusinessUnit([FromBody] CreateBusinessUnitRequestDto request)
    {
        try
        {
            var result = await _businessUnitService.CreateBusinessUnitAsync(request);
            if (!result.Success)
                return BadRequest(new ApiErrorResponse { Message = result.Message, Errors = result.Errors });

            return Created(string.Empty, new ApiResponse<BusinessUnitResponseDto>
            {
                Data = result.Data,
                Message = result.Message,
                Success = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating business unit");
            return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" });
        }
    }

    // Require authorization for all other endpoints
    [HttpGet("{businessUnitId:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<BusinessUnitResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBusinessUnit(Guid businessUnitId)
    {
        var result = await _businessUnitService.GetBusinessUnitByIdAsync(businessUnitId);
        if (!result.Success)
            return NotFound(new ApiErrorResponse { Message = result.Message });

        return Ok(new ApiResponse<BusinessUnitResponseDto>
        {
            Data = result.Data,
            Success = true
        });
    }

    [HttpGet]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<BusinessUnitResponseDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllBusinessUnits([FromQuery] Guid? branchId = null)
    {
        var result = await _businessUnitService.GetAllBusinessUnitsAsync(branchId);
        return Ok(new ApiResponse<IEnumerable<BusinessUnitResponseDto>>
        {
            Data = result.Data,
            Success = true
        });
    }

    [HttpPut("{businessUnitId:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<BusinessUnitResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateBusinessUnit(Guid businessUnitId, [FromBody] UpdateBusinessUnitRequestDto request)
    {
        var result = await _businessUnitService.UpdateBusinessUnitAsync(businessUnitId, request);
        if (!result.Success)
            return BadRequest(new ApiErrorResponse { Message = result.Message, Errors = result.Errors });

        return Ok(new ApiResponse<BusinessUnitResponseDto>
        {
            Data = result.Data,
            Message = result.Message,
            Success = true
        });
    }

    [HttpDelete("{businessUnitId:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeactivateBusinessUnit(Guid businessUnitId)
    {
        var result = await _businessUnitService.DeactivateBusinessUnitAsync(businessUnitId);
        if (!result.Success)
            return BadRequest(new ApiErrorResponse { Message = result.Message, Errors = result.Errors });

        return Ok(new ApiResponse
        {
            Message = result.Message,
            Success = true
        });
    }
}
