using Crm.Application.DTOs;
using Crm.Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nexcore.SharedKernel.Api;
using Nexcore.SharedKernel.Helpers;

namespace Crm.Api.Controllers;

[ApiController]
[Route("api/v1/contact-lists")]
[Produces("application/json")]
[Authorize]
public class ContactListsController : ControllerBase
{
    private readonly IContactListService _service;
    private readonly ILogger<ContactListsController> _logger;

    public ContactListsController(IContactListService service, ILogger<ContactListsController> logger)
    { _service = service; _logger = logger; }

    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResponse<ContactListDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        try { var (items, total) = await _service.GetPagedAsync(page, pageSize); return Ok(items.ToPaginatedResponse(total, page, pageSize)); }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving contact lists"); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<ContactListDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetById(Guid id)
    {
        try
        {
            var result = await _service.GetByIdAsync(id);
            if (result is null) return NotFound(new ApiErrorResponse { Message = "Contact list not found" });
            return Ok(new ApiResponse<ContactListDto> { Success = true, Data = result });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving contact list {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<ContactListDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreateContactListDto dto)
    {
        try
        {
            var userId = TenantContextHelper.ExtractUserId(User);
            var result = await _service.CreateAsync(dto, userId);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, new ApiResponse<ContactListDto> { Success = true, Data = result });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error creating contact list"); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<ContactListDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateContactListDto dto)
    {
        try
        {
            var userId = TenantContextHelper.ExtractUserId(User);
            var result = await _service.UpdateAsync(id, dto, userId);
            return Ok(new ApiResponse<ContactListDto> { Success = true, Data = result });
        }
        catch (InvalidOperationException ex) { return NotFound(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error updating contact list {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
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
        catch (Exception ex) { _logger.LogError(ex, "Error deleting contact list {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    [HttpGet("{id:guid}/members")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<ContactListMemberDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMembers(Guid id)
    {
        try { var result = await _service.GetMembersAsync(id); return Ok(new ApiResponse<IEnumerable<ContactListMemberDto>> { Success = true, Data = result }); }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving contact list members {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    [HttpPost("{id:guid}/members")]
    [ProducesResponseType(typeof(ApiResponse<ContactListMemberDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> AddMember(Guid id, [FromBody] CreateContactListMemberDto dto)
    {
        try
        {
            var userId = TenantContextHelper.ExtractUserId(User);
            var result = await _service.AddMemberAsync(id, dto, userId);
            return StatusCode(StatusCodes.Status201Created, new ApiResponse<ContactListMemberDto> { Success = true, Data = result });
        }
        catch (InvalidOperationException ex) { return BadRequest(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error adding member to contact list"); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    [HttpDelete("{id:guid}/members/{memberId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RemoveMember(Guid id, Guid memberId)
    {
        try
        {
            var userId = TenantContextHelper.ExtractUserId(User);
            await _service.RemoveMemberAsync(id, memberId, userId);
            return NoContent();
        }
        catch (InvalidOperationException ex) { return NotFound(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error removing contact list member"); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }
}
