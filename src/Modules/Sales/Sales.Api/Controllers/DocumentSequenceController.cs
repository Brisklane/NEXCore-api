using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nexcore.SharedKernel.Api;
using Sales.Application.DTOs;
using Sales.Application.Services.Interfaces;
using Sales.Domain.Enums;

namespace Sales.Api.Controllers;

/// <summary>
/// Configure document numbering sequences for all Sales document types.
///
/// Each sequence controls:
///   • Prefix     — leading code   e.g. SO, INV, QT
///   • Separator  — joiner char    e.g. -, /
///   • Year       — include + format (full 2026 or short 26)
///   • Month      — embed 2-digit month (useful for high-volume monthly billing)
///   • Padding    — zero-pad width of the running counter
///   • ResetOn    — when the counter rolls back to 1 (Never / Yearly / Monthly)
///
/// Generated number examples:
///   SO-2026-00001       Prefix=SO,  Sep=-, Year=Full,  Month=false, Pad=5
///   INV/2026/05/00001   Prefix=INV, Sep=/, Year=Full,  Month=true,  Pad=5
///   PAY-26-00001        Prefix=PAY, Sep=-, Year=Short, Month=false, Pad=5
///   QT-00001            Prefix=QT,  Sep=-, Year=false, Month=false, Pad=5
///
/// Workflow:
///   GET    /api/sales/document-sequence           — list all
///   GET    /api/sales/document-sequence/{type}    — get by document type
///   POST   /api/sales/document-sequence           — create (if not seeded)
///   PUT    /api/sales/document-sequence/{id}      — update format settings
///   POST   /api/sales/document-sequence/{id}/reset — reset counter
///   POST   /api/sales/document-sequence/preview   — preview format without allocating
/// </summary>
[ApiController]
[Route("api/sales/[controller]")]
[Authorize]
public class DocumentSequenceController : ControllerBase
{
    private readonly IDocumentSequenceService _sequences;
    private readonly ILogger<DocumentSequenceController> _logger;

    public DocumentSequenceController(
        IDocumentSequenceService sequences,
        ILogger<DocumentSequenceController> logger)
    {
        _sequences = sequences;
        _logger    = logger;
    }

    // ── Read ──────────────────────────────────────────────────────────────────

    /// <summary>All document sequences configured for this tenant.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<DocumentSequenceDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            var list = await _sequences.GetAllAsync();
            return Ok(new ApiResponse<List<DocumentSequenceDto>>
            {
                Success = true, Data = list,
                Message = $"{list.Count} sequence(s)",
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving document sequences");
            return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving document sequences" });
        }
    }

    /// <summary>
    /// Get a single sequence by document type name.
    /// Valid values: Quotation, SalesOrder, Invoice, CreditNote, Payment,
    /// Delivery, SalesReturn, PosSession, PosTransaction, RiderAssignment.
    /// </summary>
    [HttpGet("{documentType}")]
    [ProducesResponseType(typeof(ApiResponse<DocumentSequenceDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByType(string documentType)
    {
        try
        {
            if (!Enum.TryParse<DocumentType>(documentType, ignoreCase: true, out var type))
                return BadRequest(new ApiErrorResponse { Message = $"Unknown document type '{documentType}'. Valid values: {string.Join(", ", Enum.GetNames<DocumentType>())}" });

            var seq = await _sequences.GetByDocumentTypeAsync(type);
            if (seq == null)
                return NotFound(new ApiErrorResponse { Message = $"No sequence configured for '{documentType}'" });
            return Ok(new ApiResponse<DocumentSequenceDto> { Success = true, Data = seq });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving sequence for {Type}", documentType);
            return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving sequence" });
        }
    }

    // ── Create ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Create a new document sequence.
    /// Sequences for all standard types (SalesOrder, Invoice, etc.) are seeded
    /// automatically on company creation — use this only for custom document types.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<DocumentSequenceDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateDocumentSequenceDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(new ApiErrorResponse { Message = "Invalid input" });

            var created = await _sequences.CreateAsync(dto);
            return CreatedAtAction(nameof(GetByType), new { documentType = created.DocumentType },
                new ApiResponse<DocumentSequenceDto>
                {
                    Success = true, Data = created,
                    Message = $"Sequence created — next number: {created.NextNumberPreview}",
                });
        }
        catch (InvalidOperationException ex) { return BadRequest(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating document sequence");
            return StatusCode(500, new ApiErrorResponse { Message = "Error creating sequence" });
        }
    }

    // ── Update ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Update format settings for an existing sequence.
    /// All fields are optional — only supplied fields are changed.
    /// Changing the format does NOT affect previously issued numbers.
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<DocumentSequenceDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateDocumentSequenceDto dto)
    {
        try
        {
            var updated = await _sequences.UpdateAsync(id, dto);
            return Ok(new ApiResponse<DocumentSequenceDto>
            {
                Success = true, Data = updated,
                Message = $"Sequence updated — next number: {updated.NextNumberPreview}",
            });
        }
        catch (InvalidOperationException ex) { return NotFound(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating sequence {Id}", id);
            return StatusCode(500, new ApiErrorResponse { Message = "Error updating sequence" });
        }
    }

    // ── Reset counter ─────────────────────────────────────────────────────────

    /// <summary>
    /// Manually reset the running counter on a sequence.
    /// Use after migrating from a legacy system, or to skip a range.
    /// The next document will receive the number specified in ResetTo.
    /// </summary>
    [HttpPost("{id:guid}/reset")]
    [ProducesResponseType(typeof(ApiResponse<DocumentSequenceDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Reset(Guid id, [FromBody] ResetSequenceDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(new ApiErrorResponse { Message = "Invalid input" });

            var updated = await _sequences.ResetCounterAsync(id, dto);
            return Ok(new ApiResponse<DocumentSequenceDto>
            {
                Success = true, Data = updated,
                Message = $"Counter reset — next number: {updated.NextNumberPreview}",
            });
        }
        catch (InvalidOperationException ex) { return NotFound(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting sequence {Id}", id);
            return StatusCode(500, new ApiErrorResponse { Message = "Error resetting counter" });
        }
    }

    // ── Format preview ────────────────────────────────────────────────────────

    /// <summary>
    /// Preview what a number would look like with the given format settings,
    /// without allocating a real number or touching any counter.
    ///
    /// Use this to validate settings before saving, or to show a live preview
    /// in the configuration UI.
    ///
    /// Example request body:
    ///   { "prefix": "SO", "separator": "-", "includeYear": true, "yearFormat": 0,
    ///     "includeMonth": false, "sequencePadding": 5, "sampleNumber": 42 }
    /// Example response:
    ///   { "preview": "SO-2026-00042", "pattern": "{Prefix}-{YYYY}-{00000}" }
    /// </summary>
    [HttpPost("preview")]
    [ProducesResponseType(typeof(ApiResponse<PreviewSequenceFormatResultDto>), StatusCodes.Status200OK)]
    public IActionResult Preview([FromBody] PreviewSequenceFormatDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(new ApiErrorResponse { Message = "Invalid input" });

            var result = _sequences.PreviewFormat(dto);
            return Ok(new ApiResponse<PreviewSequenceFormatResultDto> { Success = true, Data = result });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error previewing sequence format");
            return StatusCode(500, new ApiErrorResponse { Message = "Error previewing format" });
        }
    }
}
