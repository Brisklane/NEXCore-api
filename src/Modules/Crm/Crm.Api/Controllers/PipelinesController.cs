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
public class PipelinesController : ControllerBase
{
    private readonly IPipelineService _service;
    private readonly ILogger<PipelinesController> _logger;

    public PipelinesController(IPipelineService service, ILogger<PipelinesController> logger)
    { _service = service; _logger = logger; }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<PipelineDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        try { var result = await _service.GetAllAsync(); return Ok(new ApiResponse<IEnumerable<PipelineDto>> { Success = true, Data = result }); }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving pipelines"); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<PipelineDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetById(Guid id)
    {
        try
        {
            var result = await _service.GetByIdAsync(id);
            if (result is null) return NotFound(new ApiErrorResponse { Message = "Pipeline not found" });
            return Ok(new ApiResponse<PipelineDto> { Success = true, Data = result });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving pipeline {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<PipelineDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreatePipelineDto dto)
    {
        try
        {
            var userId = TenantContextHelper.ExtractUserId(User);
            var result = await _service.CreateAsync(dto, userId);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, new ApiResponse<PipelineDto> { Success = true, Data = result, Message = "Pipeline created successfully" });
        }
        catch (InvalidOperationException ex) { return BadRequest(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error creating pipeline"); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<PipelineDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdatePipelineDto dto)
    {
        try
        {
            var userId = TenantContextHelper.ExtractUserId(User);
            var result = await _service.UpdateAsync(id, dto, userId);
            return Ok(new ApiResponse<PipelineDto> { Success = true, Data = result });
        }
        catch (InvalidOperationException ex) { return NotFound(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error updating pipeline {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
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
        catch (Exception ex) { _logger.LogError(ex, "Error deleting pipeline {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    // ??? Pipeline Stages ???????????????????????????????????????????????????????

    [HttpGet("{id:guid}/stages")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<PipelineStageDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStages(Guid id)
    {
        try { var result = await _service.GetStagesAsync(id); return Ok(new ApiResponse<IEnumerable<PipelineStageDto>> { Success = true, Data = result }); }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving pipeline stages {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    [HttpPost("{id:guid}/stages")]
    [ProducesResponseType(typeof(ApiResponse<PipelineStageDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> AddStage(Guid id, [FromBody] CreatePipelineStageDto dto)
    {
        try
        {
            var userId = TenantContextHelper.ExtractUserId(User);
            var result = await _service.AddStageAsync(id, dto, userId);
            return StatusCode(StatusCodes.Status201Created, new ApiResponse<PipelineStageDto> { Success = true, Data = result });
        }
        catch (InvalidOperationException ex) { return BadRequest(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error adding pipeline stage"); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    [HttpPut("{id:guid}/stages/{stageId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<PipelineStageDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateStage(Guid id, Guid stageId, [FromBody] UpdatePipelineStageDto dto)
    {
        try
        {
            var userId = TenantContextHelper.ExtractUserId(User);
            var result = await _service.UpdateStageAsync(id, stageId, dto, userId);
            return Ok(new ApiResponse<PipelineStageDto> { Success = true, Data = result });
        }
        catch (InvalidOperationException ex) { return NotFound(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error updating pipeline stage"); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    [HttpDelete("{id:guid}/stages/{stageId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RemoveStage(Guid id, Guid stageId)
    {
        try
        {
            var userId = TenantContextHelper.ExtractUserId(User);
            await _service.RemoveStageAsync(id, stageId, userId);
            return NoContent();
        }
        catch (InvalidOperationException ex) { return NotFound(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error removing pipeline stage"); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }
}
