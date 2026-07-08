using Crm.Application.DTOs;
using Crm.Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nexcore.SharedKernel.Api;
using Nexcore.SharedKernel.Helpers;

namespace Crm.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
[Authorize]
public class NotesController : ControllerBase
{
    private readonly INoteService _service;
    private readonly ILogger<NotesController> _logger;

    public NotesController(INoteService service, ILogger<NotesController> logger)
    { _service = service; _logger = logger; }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<NoteDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetById(Guid id)
    {
        try
        {
            var result = await _service.GetByIdAsync(id);
            if (result is null) return NotFound(new ApiErrorResponse { Message = "Note not found" });
            return Ok(new ApiResponse<NoteDto> { Success = true, Data = result });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving note {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    [HttpGet("by-parent/{parentId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<NoteDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByParent(Guid parentId, [FromQuery] string parentType = "Account")
    {
        try { var result = await _service.GetByParentAsync(parentId, parentType); return Ok(new ApiResponse<IEnumerable<NoteDto>> { Success = true, Data = result }); }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving notes"); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<NoteDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreateNoteDto dto)
    {
        try
        {
            var userId = TenantContextHelper.ExtractUserId(User);
            var result = await _service.CreateAsync(dto, userId);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, new ApiResponse<NoteDto> { Success = true, Data = result, Message = "Note created successfully" });
        }
        catch (InvalidOperationException ex) { return BadRequest(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error creating note"); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<NoteDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateNoteDto dto)
    {
        try
        {
            var userId = TenantContextHelper.ExtractUserId(User);
            var result = await _service.UpdateAsync(id, dto, userId);
            return Ok(new ApiResponse<NoteDto> { Success = true, Data = result });
        }
        catch (InvalidOperationException ex) { return NotFound(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error updating note {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            var userId = TenantContextHelper.ExtractUserId(User);
            await _service.DeleteAsync(id, userId);
            return NoContent();
        }
        catch (InvalidOperationException ex) { return NotFound(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error deleting note {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }
}

[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
[Authorize]
public class AttachmentsController : ControllerBase
{
    private readonly IAttachmentService _service;
    private readonly ILogger<AttachmentsController> _logger;

    public AttachmentsController(IAttachmentService service, ILogger<AttachmentsController> logger)
    { _service = service; _logger = logger; }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<AttachmentDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetById(Guid id)
    {
        try
        {
            var result = await _service.GetByIdAsync(id);
            if (result is null) return NotFound(new ApiErrorResponse { Message = "Attachment not found" });
            return Ok(new ApiResponse<AttachmentDto> { Success = true, Data = result });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving attachment {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    [HttpGet("by-parent/{parentId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<AttachmentDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByParent(Guid parentId, [FromQuery] string parentType = "Account")
    {
        try { var result = await _service.GetByParentAsync(parentId, parentType); return Ok(new ApiResponse<IEnumerable<AttachmentDto>> { Success = true, Data = result }); }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving attachments"); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<AttachmentDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreateAttachmentDto dto)
    {
        try
        {
            var userId = TenantContextHelper.ExtractUserId(User);
            var result = await _service.CreateAsync(dto, userId);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, new ApiResponse<AttachmentDto> { Success = true, Data = result });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error creating attachment"); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            var userId = TenantContextHelper.ExtractUserId(User);
            await _service.DeleteAsync(id, userId);
            return NoContent();
        }
        catch (InvalidOperationException ex) { return NotFound(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error deleting attachment {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }
}
