using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Inventory.Application.DTOs;
using Inventory.Infrastructure.Repositories.Interfaces;
using Nexcore.SharedKernel.Api;
using Nexcore.SharedKernel.Helpers;

namespace Inventory.Api.Controllers;

/// <summary>Attribute definitions management</summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AttributeDefinitionController : ControllerBase
{
    private readonly IAttributeDefinitionRepository _repository;
    private readonly ILogger<AttributeDefinitionController> _logger;

    public AttributeDefinitionController(IAttributeDefinitionRepository repository, ILogger<AttributeDefinitionController> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    /// <summary>Get all attribute definitions</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResponse<AttributeDefinitionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] PaginationParams pagination, [FromQuery] bool? isActive)
    {
        try
        {
            var search = pagination.SearchTerm?.Trim().ToLower();
            var (defs, total) = await _repository.GetPagedAsync(
                pagination.PageNumber, pagination.PageSize,
                predicate: d =>
                    (isActive == null || d.IsActive == isActive) &&
                    (string.IsNullOrEmpty(search) ||
                        (d.Code != null && d.Code.ToLower().Contains(search)) ||
                        (d.Name != null && d.Name.ToLower().Contains(search)) ||
                        (d.Description != null && d.Description.ToLower().Contains(search)) ||
                        (d.DataType != null && d.DataType.ToLower().Contains(search))),
                orderBy: q => q.ApplyOrderNewestFirst(pagination.SortBy, pagination.SortDirection, "Code"));
            return Ok(PaginatedResponse<AttributeDefinitionDto>.Ok(defs.Select(MapToDto), total, pagination.PageNumber, pagination.PageSize));
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving attribute definitions"); return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving attribute definitions" }); }
    }

    /// <summary>Get attribute definition by ID</summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<AttributeDefinitionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        try
        {
            var def = await _repository.GetByIdAsync(id);
            if (def == null) return NotFound(new ApiErrorResponse { Message = "Attribute definition not found" });
            return Ok(new ApiResponse<AttributeDefinitionDto> { Success = true, Data = MapToDto(def), Message = "Attribute definition retrieved successfully" });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving attribute definition {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving attribute definition" }); }
    }

    /// <summary>Get active attribute definitions</summary>
    [HttpGet("active")]
    [ProducesResponseType(typeof(PaginatedResponse<AttributeDefinitionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetActive([FromQuery] PaginationParams pagination)
    {
        try
        {
            var (defs, total) = await _repository.GetPagedAsync(pagination.PageNumber, pagination.PageSize, predicate: a => a.IsActive);
            return Ok(PaginatedResponse<AttributeDefinitionDto>.Ok(defs.Select(MapToDto), total, pagination.PageNumber, pagination.PageSize));
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving active attribute definitions"); return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving attribute definitions" }); }
    }

    /// <summary>Get variant attributes only (usable in product variant filters)</summary>
    [HttpGet("variants")]
    [ProducesResponseType(typeof(PaginatedResponse<AttributeDefinitionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetVariantAttributes([FromQuery] PaginationParams pagination)
    {
        try
        {
            var (defs, total) = await _repository.GetPagedAsync(pagination.PageNumber, pagination.PageSize, predicate: a => a.IsVariant);
            return Ok(PaginatedResponse<AttributeDefinitionDto>.Ok(defs.Select(MapToDto), total, pagination.PageNumber, pagination.PageSize));
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving variant attributes"); return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving variant attributes" }); }
    }

    /// <summary>Create attribute definition</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<AttributeDefinitionDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateAttributeDefinitionDto dto)
    {
        try
        {
            if (!ModelState.IsValid) return BadRequest(new ApiErrorResponse { Message = "Invalid input" });
            var existing = await _repository.GetByCodeAsync(dto.Code);
            if (existing != null) return BadRequest(new ApiErrorResponse { Message = "Attribute code already exists" });

            var def = new Inventory.Domain.Entities.AttributeDefinition
            {
                Code = dto.Code, Name = dto.Name, DataType = dto.DataType,
                Unit = dto.Unit, AllowedValues = dto.AllowedValues,
                IsRequired = dto.IsRequired, IsVariant = dto.IsVariant,
                DisplayOrder = dto.DisplayOrder, IsActive = true
            };
            await _repository.AddAsync(def);
            await _repository.SaveChangesAsync();
            return CreatedAtAction(nameof(GetById), new { id = def.Id },
                new ApiResponse<AttributeDefinitionDto> { Success = true, Data = MapToDto(def), Message = "Attribute definition created successfully" });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error creating attribute definition"); return StatusCode(500, new ApiErrorResponse { Message = "Error creating attribute definition" }); }
    }

    /// <summary>Update attribute definition</summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse<AttributeDefinitionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateAttributeDefinitionDto dto)
    {
        try
        {
            if (!ModelState.IsValid) return BadRequest(new ApiErrorResponse { Message = "Invalid input" });
            var def = await _repository.GetByIdAsync(id);
            if (def == null) return NotFound(new ApiErrorResponse { Message = "Attribute definition not found" });

            if (dto.Code != null && dto.Code != def.Code)
            {
                var dup = await _repository.GetByCodeAsync(dto.Code);
                if (dup != null) return BadRequest(new ApiErrorResponse { Message = "Attribute code already exists" });
                def.Code = dto.Code;
            }
            if (dto.Name          != null) def.Name          = dto.Name;
            if (dto.DataType      != null) def.DataType      = dto.DataType;
            if (dto.Unit          != null) def.Unit          = dto.Unit;
            if (dto.AllowedValues != null) def.AllowedValues = dto.AllowedValues;
            if (dto.IsRequired    != null) def.IsRequired    = dto.IsRequired.Value;
            if (dto.IsVariant     != null) def.IsVariant     = dto.IsVariant.Value;
            if (dto.DisplayOrder  != null) def.DisplayOrder  = dto.DisplayOrder.Value;
            if (dto.IsActive      != null) def.IsActive      = dto.IsActive.Value;

            _repository.Update(def);
            await _repository.SaveChangesAsync();
            return Ok(new ApiResponse<AttributeDefinitionDto> { Success = true, Data = MapToDto(def), Message = "Attribute definition updated successfully" });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error updating attribute definition {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Error updating attribute definition" }); }
    }

    /// <summary>Delete attribute definition (soft-delete)</summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            var def = await _repository.GetByIdAsync(id);
            if (def == null) return NotFound(new ApiErrorResponse { Message = "Attribute definition not found" });
            _repository.SoftDelete(def);
            await _repository.SaveChangesAsync();
            return Ok(new ApiResponse<bool> { Success = true, Data = true, Message = "Attribute definition deleted successfully" });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error deleting attribute definition {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Error deleting attribute definition" }); }
    }

    private static AttributeDefinitionDto MapToDto(Inventory.Domain.Entities.AttributeDefinition d) => new()
    {
        Id = d.Id, Code = d.Code, Name = d.Name, DataType = d.DataType,
        Unit = d.Unit, AllowedValues = d.AllowedValues, IsRequired = d.IsRequired,
        IsVariant = d.IsVariant, IsActive = d.IsActive, DisplayOrder = d.DisplayOrder
    };
}
