using Crm.Application.DTOs;
using Crm.Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nexcore.SharedKernel.Api;
using Nexcore.SharedKernel.Helpers;

namespace Crm.Api.Controllers;

[ApiController]
[Route("api/v1/KnowledgeArticles")]
[Produces("application/json")]
[Authorize]
public class KnowledgeArticlesController : ControllerBase
{
    private readonly IKnowledgeArticleService _service;
    private readonly ILogger<KnowledgeArticlesController> _logger;

    public KnowledgeArticlesController(IKnowledgeArticleService service, ILogger<KnowledgeArticlesController> logger)
    { _service = service; _logger = logger; }

    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResponse<KnowledgeArticleDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string? search = null)
    {
        try { var (items, total) = await _service.GetPagedAsync(page, pageSize, search); return Ok(items.ToPaginatedResponse(total, page, pageSize)); }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving knowledge articles"); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<KnowledgeArticleDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetById(Guid id)
    {
        try
        {
            var result = await _service.GetByIdAsync(id);
            if (result is null) return NotFound(new ApiErrorResponse { Message = "Knowledge article not found" });
            return Ok(new ApiResponse<KnowledgeArticleDto> { Success = true, Data = result });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving knowledge article {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<KnowledgeArticleDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreateKnowledgeArticleDto dto)
    {
        try
        {
            var userId = TenantContextHelper.ExtractUserId(User);
            var result = await _service.CreateAsync(dto, userId);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, new ApiResponse<KnowledgeArticleDto> { Success = true, Data = result });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error creating knowledge article"); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<KnowledgeArticleDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateKnowledgeArticleDto dto)
    {
        try
        {
            var userId = TenantContextHelper.ExtractUserId(User);
            var result = await _service.UpdateAsync(id, dto, userId);
            return Ok(new ApiResponse<KnowledgeArticleDto> { Success = true, Data = result });
        }
        catch (InvalidOperationException ex) { return NotFound(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error updating knowledge article {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
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
        catch (Exception ex) { _logger.LogError(ex, "Error deleting knowledge article {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    [HttpPost("{id:guid}/publish")]
    [ProducesResponseType(typeof(ApiResponse<KnowledgeArticleDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Publish(Guid id)
    {
        try
        {
            var userId = TenantContextHelper.ExtractUserId(User);
            var result = await _service.PublishAsync(id, userId);
            return Ok(new ApiResponse<KnowledgeArticleDto> { Success = true, Data = result, Message = "Article published" });
        }
        catch (InvalidOperationException ex) { return BadRequest(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error publishing article {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    [HttpPost("{id:guid}/archive")]
    [ProducesResponseType(typeof(ApiResponse<KnowledgeArticleDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Archive(Guid id)
    {
        try
        {
            var userId = TenantContextHelper.ExtractUserId(User);
            var result = await _service.ArchiveAsync(id, userId);
            return Ok(new ApiResponse<KnowledgeArticleDto> { Success = true, Data = result, Message = "Article archived" });
        }
        catch (InvalidOperationException ex) { return BadRequest(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error archiving article {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }
}

[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
[Authorize]
public class EntitlementsController : ControllerBase
{
    private readonly IEntitlementService _service;
    private readonly ILogger<EntitlementsController> _logger;

    public EntitlementsController(IEntitlementService service, ILogger<EntitlementsController> logger)
    { _service = service; _logger = logger; }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<EntitlementDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetById(Guid id)
    {
        try
        {
            var result = await _service.GetByIdAsync(id);
            if (result is null) return NotFound(new ApiErrorResponse { Message = "Entitlement not found" });
            return Ok(new ApiResponse<EntitlementDto> { Success = true, Data = result });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving entitlement {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    [HttpGet("by-account/{accountId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<EntitlementDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByAccount(Guid accountId)
    {
        try { var result = await _service.GetByAccountIdAsync(accountId); return Ok(new ApiResponse<IEnumerable<EntitlementDto>> { Success = true, Data = result }); }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving entitlements for account {Id}", accountId); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<EntitlementDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreateEntitlementDto dto)
    {
        try
        {
            var userId = TenantContextHelper.ExtractUserId(User);
            var result = await _service.CreateAsync(dto, userId);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, new ApiResponse<EntitlementDto> { Success = true, Data = result });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error creating entitlement"); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<EntitlementDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateEntitlementDto dto)
    {
        try
        {
            var userId = TenantContextHelper.ExtractUserId(User);
            var result = await _service.UpdateAsync(id, dto, userId);
            return Ok(new ApiResponse<EntitlementDto> { Success = true, Data = result });
        }
        catch (InvalidOperationException ex) { return NotFound(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error updating entitlement {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
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
        catch (Exception ex) { _logger.LogError(ex, "Error deleting entitlement {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }
}
