using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nexcore.SharedKernel.Api;
using Procurement.Application.DTOs;
using Procurement.Application.Services.Interfaces;
using Procurement.Domain.Enums;

namespace Procurement.Api.Controllers;

/// <summary>Document Sequences — configure auto-numbering formats (prefix, year, padding) for all procurement documents.</summary>
[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
[Authorize]
public class DocumentSequenceController : ControllerBase
{
    private readonly IDocumentSequenceManagementService _service;
    private readonly ILogger<DocumentSequenceController> _logger;

    public DocumentSequenceController(IDocumentSequenceManagementService service, ILogger<DocumentSequenceController> logger)
    {
        _service = service;
        _logger  = logger;
    }

    /// <summary>Get all document sequences (one per document type per tenant).</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<DocumentSequenceDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            var list = await _service.GetAllAsync();
            return Ok(new ApiResponse<List<DocumentSequenceDto>> { Success = true, Data = list });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving document sequences"); return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving sequences" }); }
    }

    /// <summary>Get sequence configuration for a specific document type.</summary>
    [HttpGet("by-document-type/{documentType}")]
    [ProducesResponseType(typeof(ApiResponse<DocumentSequenceDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByDocumentType(ProcurementDocumentType documentType)
    {
        try
        {
            var seq = await _service.GetByDocumentTypeAsync(documentType);
            if (seq is null) return NotFound(new ApiErrorResponse { Message = "Sequence not found for this document type" });
            return Ok(new ApiResponse<DocumentSequenceDto> { Success = true, Data = seq });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving sequence"); return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving sequence" }); }
    }

    /// <summary>Update a document sequence format (prefix, padding, reset period etc.).</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<DocumentSequenceDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateDocumentSequenceDto dto)
    {
        try
        {
            var seq = await _service.UpdateAsync(id, dto);
            return Ok(new ApiResponse<DocumentSequenceDto> { Success = true, Data = seq, Message = "Document sequence updated" });
        }
        catch (KeyNotFoundException) { return NotFound(new ApiErrorResponse { Message = "Document sequence not found" }); }
        catch (InvalidOperationException ex) { return BadRequest(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error updating sequence {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Error updating sequence" }); }
    }
}
