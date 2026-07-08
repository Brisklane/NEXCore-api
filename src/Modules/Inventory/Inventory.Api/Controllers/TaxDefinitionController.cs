using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Inventory.Application.DTOs;
using Inventory.Domain.Enums;
using Inventory.Infrastructure.Repositories.Interfaces;
using Nexcore.SharedKernel.Api;
using Nexcore.SharedKernel.Helpers;

namespace Inventory.Api.Controllers;

/// <summary>Tax definitions management</summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TaxDefinitionController : ControllerBase
{
    private readonly ITaxDefinitionRepository _repository;
    private readonly ILogger<TaxDefinitionController> _logger;

    public TaxDefinitionController(ITaxDefinitionRepository repository, ILogger<TaxDefinitionController> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    /// <summary>Get all tax definitions</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResponse<TaxDefinitionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] PaginationParams pagination, [FromQuery] bool? isActive)
    {
        try
        {
            var search = pagination.SearchTerm?.Trim().ToLower();
            var (taxes, total) = await _repository.GetPagedAsync(
                pagination.PageNumber, pagination.PageSize,
                predicate: t =>
                    (isActive == null || t.IsActive == isActive) &&
                    (string.IsNullOrEmpty(search) ||
                        (t.Code != null && t.Code.ToLower().Contains(search)) ||
                        (t.Name != null && t.Name.ToLower().Contains(search)) ||
                        (t.Description != null && t.Description.ToLower().Contains(search))),
                orderBy: q => q.ApplyOrderNewestFirst(pagination.SortBy, pagination.SortDirection, "Code"));
            return Ok(PaginatedResponse<TaxDefinitionDto>.Ok(taxes.Select(MapToDto), total, pagination.PageNumber, pagination.PageSize));
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving tax definitions"); return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving tax definitions" }); }
    }

    /// <summary>Get tax definition by ID</summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<TaxDefinitionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        try
        {
            var tax = await _repository.GetByIdAsync(id);
            if (tax == null) return NotFound(new ApiErrorResponse { Message = "Tax definition not found" });
            return Ok(new ApiResponse<TaxDefinitionDto> { Success = true, Data = MapToDto(tax), Message = "Tax definition retrieved successfully" });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving tax definition {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving tax definition" }); }
    }

    /// <summary>Get active tax definitions</summary>
    [HttpGet("active")]
    [ProducesResponseType(typeof(PaginatedResponse<TaxDefinitionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetActive([FromQuery] PaginationParams pagination)
    {
        try
        {
            var (taxes, total) = await _repository.GetPagedAsync(pagination.PageNumber, pagination.PageSize, predicate: t => t.IsActive);
            return Ok(PaginatedResponse<TaxDefinitionDto>.Ok(taxes.Select(MapToDto), total, pagination.PageNumber, pagination.PageSize));
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving active tax definitions"); return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving active tax definitions" }); }
    }

    /// <summary>Get sales taxes</summary>
    [HttpGet("sales")]
    [ProducesResponseType(typeof(PaginatedResponse<TaxDefinitionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSalesTaxes([FromQuery] PaginationParams pagination)
    {
        try
        {
            var (taxes, total) = await _repository.GetPagedAsync(pagination.PageNumber, pagination.PageSize, predicate: t => t.ApplyOnSales);
            return Ok(PaginatedResponse<TaxDefinitionDto>.Ok(taxes.Select(MapToDto), total, pagination.PageNumber, pagination.PageSize));
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving sales taxes"); return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving sales taxes" }); }
    }

    /// <summary>Get purchase taxes</summary>
    [HttpGet("purchases")]
    [ProducesResponseType(typeof(PaginatedResponse<TaxDefinitionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPurchaseTaxes([FromQuery] PaginationParams pagination)
    {
        try
        {
            var (taxes, total) = await _repository.GetPagedAsync(pagination.PageNumber, pagination.PageSize, predicate: t => t.ApplyOnPurchases);
            return Ok(PaginatedResponse<TaxDefinitionDto>.Ok(taxes.Select(MapToDto), total, pagination.PageNumber, pagination.PageSize));
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving purchase taxes"); return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving purchase taxes" }); }
    }

    /// <summary>Create tax definition</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<TaxDefinitionDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateTaxDefinitionDto dto)
    {
        try
        {
            if (!ModelState.IsValid) return BadRequest(new ApiErrorResponse { Message = "Invalid input" });
            var existing = await _repository.GetByCodeAsync(dto.Code);
            if (existing != null) return BadRequest(new ApiErrorResponse { Message = "Tax code already exists" });

            if (!Enum.TryParse<TaxType>(dto.TaxType, ignoreCase: true, out var taxType))
                return BadRequest(new ApiErrorResponse { Message = $"Invalid TaxType '{dto.TaxType}'. Valid values: {string.Join(", ", Enum.GetNames<TaxType>())}" });

            if (!Enum.TryParse<TaxInclusionType>(dto.InclusionType, ignoreCase: true, out var inclusionType))
                return BadRequest(new ApiErrorResponse { Message = $"Invalid InclusionType '{dto.InclusionType}'. Valid values: {string.Join(", ", Enum.GetNames<TaxInclusionType>())}" });

            var tax = new Inventory.Domain.Entities.TaxDefinition
            {
                Code                   = dto.Code,
                Name                   = dto.Name,
                TaxType                = taxType,
                IsPercentage           = dto.IsPercentage,
                Rate                   = dto.Rate,
                InclusionType          = inclusionType,
                TaxPayableAccountId    = dto.TaxPayableAccountId,
                TaxRecoverableAccountId= dto.TaxRecoverableAccountId,
                ApplyOnSales           = dto.ApplyOnSales,
                ApplyOnPurchases       = dto.ApplyOnPurchases,
                CountryCode            = dto.CountryCode,
                ValidFrom              = dto.ValidFrom ?? DateTime.UtcNow,
                ValidTo                = dto.ValidTo,
                IsActive               = true
            };
            await _repository.AddAsync(tax);
            await _repository.SaveChangesAsync();
            return CreatedAtAction(nameof(GetById), new { id = tax.Id },
                new ApiResponse<TaxDefinitionDto> { Success = true, Data = MapToDto(tax), Message = "Tax definition created successfully" });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error creating tax definition"); return StatusCode(500, new ApiErrorResponse { Message = "Error creating tax definition" }); }
    }

    /// <summary>Update tax definition</summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse<TaxDefinitionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateTaxDefinitionDto dto)
    {
        try
        {
            if (!ModelState.IsValid) return BadRequest(new ApiErrorResponse { Message = "Invalid input" });
            var tax = await _repository.GetByIdAsync(id);
            if (tax == null) return NotFound(new ApiErrorResponse { Message = "Tax definition not found" });

            if (dto.Code != null && dto.Code != tax.Code)
            {
                var dup = await _repository.GetByCodeAsync(dto.Code);
                if (dup != null) return BadRequest(new ApiErrorResponse { Message = "Tax code already exists" });
                tax.Code = dto.Code;
            }
            if (dto.Name != null) tax.Name = dto.Name;
            if (dto.TaxType != null)
            {
                if (!Enum.TryParse<TaxType>(dto.TaxType, ignoreCase: true, out var taxType))
                    return BadRequest(new ApiErrorResponse { Message = $"Invalid TaxType '{dto.TaxType}'" });
                tax.TaxType = taxType;
            }
            if (dto.IsPercentage       != null) tax.IsPercentage            = dto.IsPercentage.Value;
            if (dto.Rate               != null) tax.Rate                    = dto.Rate.Value;
            if (dto.InclusionType      != null)
            {
                if (!Enum.TryParse<TaxInclusionType>(dto.InclusionType, ignoreCase: true, out var it))
                    return BadRequest(new ApiErrorResponse { Message = $"Invalid InclusionType '{dto.InclusionType}'" });
                tax.InclusionType = it;
            }
            if (dto.TaxPayableAccountId     != null) tax.TaxPayableAccountId     = dto.TaxPayableAccountId;
            if (dto.TaxRecoverableAccountId != null) tax.TaxRecoverableAccountId = dto.TaxRecoverableAccountId;
            if (dto.ApplyOnSales     != null) tax.ApplyOnSales     = dto.ApplyOnSales.Value;
            if (dto.ApplyOnPurchases != null) tax.ApplyOnPurchases = dto.ApplyOnPurchases.Value;
            if (dto.CountryCode      != null) tax.CountryCode      = dto.CountryCode;
            if (dto.ValidFrom        != null) tax.ValidFrom        = dto.ValidFrom.Value;
            if (dto.ValidTo          != null) tax.ValidTo          = dto.ValidTo;
            if (dto.IsActive         != null) tax.IsActive         = dto.IsActive.Value;

            _repository.Update(tax);
            await _repository.SaveChangesAsync();
            return Ok(new ApiResponse<TaxDefinitionDto> { Success = true, Data = MapToDto(tax), Message = "Tax definition updated successfully" });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error updating tax definition {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Error updating tax definition" }); }
    }

    /// <summary>Delete tax definition (soft-delete)</summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            var tax = await _repository.GetByIdAsync(id);
            if (tax == null) return NotFound(new ApiErrorResponse { Message = "Tax definition not found" });
            _repository.SoftDelete(tax);
            await _repository.SaveChangesAsync();
            return Ok(new ApiResponse<bool> { Success = true, Data = true, Message = "Tax definition deleted successfully" });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error deleting tax definition {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Error deleting tax definition" }); }
    }

    private static TaxDefinitionDto MapToDto(Inventory.Domain.Entities.TaxDefinition t) => new()
    {
        Id                     = t.Id,
        Code                   = t.Code,
        Name                   = t.Name,
        TaxType                = t.TaxType.ToString(),
        IsPercentage           = t.IsPercentage,
        Rate                   = t.Rate,
        InclusionType          = t.InclusionType.ToString(),
        TaxPayableAccountId    = t.TaxPayableAccountId,
        TaxRecoverableAccountId= t.TaxRecoverableAccountId,
        ApplyOnSales           = t.ApplyOnSales,
        ApplyOnPurchases       = t.ApplyOnPurchases,
        CountryCode            = t.CountryCode,
        ValidFrom              = t.ValidFrom,
        ValidTo                = t.ValidTo,
        IsActive               = t.IsActive
    };
}
