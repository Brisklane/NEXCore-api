using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Nexcore.SharedKernel.Api;
using Nexcore.SharedKernel.Helpers;
using Hr.Application.DTOs;
using Hr.Application.Services.Interfaces;

namespace Hr.Api.Controllers;

/// <summary>
/// Offer Letter management controller - CRUD for offer letters
/// Tenant context automatically extracted and applied at service/repository level
/// </summary>
[ApiController]
[Route("api/v1/hr/[controller]")]
[Produces("application/json")]
[Authorize]
public class OfferLetterController : ControllerBase
{
    private readonly IOfferLetterService _service;
    private readonly ILogger<OfferLetterController> _logger;

    public OfferLetterController(IOfferLetterService service, ILogger<OfferLetterController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>Get all offer letters for the current tenant</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<OfferLetterDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            var offers = await _service.GetAllAsync();
            return Ok(new ApiResponse<IEnumerable<OfferLetterDto>> { Success = true, Data = offers });
        }
        catch (InvalidOperationException ex) { return Unauthorized(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving offer letters"); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    /// <summary>Get offer letter by ID</summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<OfferLetterDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        try
        {
            var offer = await _service.GetByIdAsync(id);
            if (offer == null) return NotFound(new ApiErrorResponse { Message = "Offer letter not found" });
            return Ok(new ApiResponse<OfferLetterDto> { Success = true, Data = offer });
        }
        catch (InvalidOperationException ex) { return Unauthorized(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving offer letter"); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    /// <summary>Get offer letters by application</summary>
    [HttpGet("by-application/{applicationId}")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<OfferLetterDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByApplication(Guid applicationId)
    {
        try
        {
            var offers = await _service.GetByApplicationIdAsync(applicationId);
            return Ok(new ApiResponse<IEnumerable<OfferLetterDto>> { Success = true, Data = offers });
        }
        catch (InvalidOperationException ex) { return Unauthorized(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving offer letters by application"); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    /// <summary>Create a new offer letter</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<OfferLetterDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateOfferLetterDto request)
    {
        try
        {
            var userId = TenantContextHelper.ExtractUserId(User);
            var result = await _service.CreateAsync(request, userId);
            return CreatedAtAction(nameof(GetById), new { id = result.Id },
                new ApiResponse<OfferLetterDto> { Success = true, Message = "Offer letter created successfully", Data = result });
        }
        catch (InvalidOperationException ex) { return BadRequest(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error creating offer letter"); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    /// <summary>Update an existing offer letter</summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse<OfferLetterDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateOfferLetterDto request)
    {
        try
        {
            var userId = TenantContextHelper.ExtractUserId(User);
            var result = await _service.UpdateAsync(id, request, userId);
            return Ok(new ApiResponse<OfferLetterDto> { Success = true, Message = "Offer letter updated successfully", Data = result });
        }
        catch (InvalidOperationException ex) { return NotFound(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error updating offer letter"); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    /// <summary>Delete an offer letter (soft delete)</summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            var userId = TenantContextHelper.ExtractUserId(User);
            await _service.DeleteAsync(id, userId);
            return NoContent();
        }
        catch (InvalidOperationException ex) { return BadRequest(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error deleting offer letter"); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }
}
