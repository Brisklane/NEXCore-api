using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nexcore.SharedKernel.Api;
using Sales.Application.DTOs;
using Sales.Domain.Entities;
using Sales.Domain.Enums;
using Sales.Infrastructure.Repositories.Interfaces;

namespace Sales.Api.Controllers;

[ApiController]
[Route("api/sales/[controller]")]
[Authorize]
public class PromotionController : ControllerBase
{
    private readonly IPromotionRepository _promotions;
    private readonly IPromotionItemRepository _items;
    private readonly ILogger<PromotionController> _logger;

    public PromotionController(
        IPromotionRepository promotions,
        IPromotionItemRepository items,
        ILogger<PromotionController> logger)
    {
        _promotions = promotions;
        _items = items;
        _logger = logger;
    }

    // ── Queries ───────────────────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            var list = await _promotions.GetAllAsync();
            return Ok(new ApiResponse<List<PromotionDto>>
            {
                Success = true,
                Data = list.Where(p => !p.IsDeleted).OrderByDescending(p => p.CreatedAt).Select(MapToDto).ToList(),
                Message = "Promotions retrieved"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving promotions");
            return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving promotions" });
        }
    }

    [HttpGet("active")]
    public async Task<IActionResult> GetActive([FromQuery] DateTime? at = null)
    {
        try
        {
            var now = at ?? DateTime.Now;
            var date = DateOnly.FromDateTime(now);
            var time = TimeOnly.FromDateTime(now);
            var list = await _promotions.GetActiveAsync(date, time);
            return Ok(new ApiResponse<List<PromotionDto>>
            {
                Success = true,
                Data = list.Select(MapToDto).ToList(),
                Message = $"{list.Count} active promotion(s)"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving active promotions");
            return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving active promotions" });
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        try
        {
            var promo = await _promotions.GetWithItemsAsync(id);
            if (promo == null) return NotFound(new ApiErrorResponse { Message = "Promotion not found" });
            return Ok(new ApiResponse<PromotionDto> { Success = true, Data = MapToDto(promo), Message = "Promotion retrieved" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving promotion {Id}", id);
            return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving promotion" });
        }
    }

    // ── Commands ──────────────────────────────────────────────────────────────

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePromotionDto dto)
    {
        try
        {
            if (dto.StartDate > dto.EndDate)
                return BadRequest(new ApiErrorResponse { Message = "StartDate must be before or equal to EndDate" });

            // Codes already taken in this tenant (incl. soft-deleted — the unique index spans them).
            var usedCodes = await _promotions.GetUsedCodesAsync();

            string code;
            if (string.IsNullOrWhiteSpace(dto.PromotionCode))
            {
                code = NextCode(usedCodes);
            }
            else
            {
                code = dto.PromotionCode.Trim().ToUpperInvariant();
                if (usedCodes.Contains(code))
                    return Conflict(new ApiErrorResponse { Message = $"Promotion code '{code}' is already in use" });
            }

            var promo = new Promotion
            {
                Name = dto.Name,
                Description = dto.Description,
                PromotionCode = code,
                IsAutoApplied = dto.IsAutoApplied,
                Status = PromotionStatus.Draft,
                Priority = dto.Priority,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                StartTime = dto.StartTime,
                EndTime = dto.EndTime,
                ScheduledDays = dto.ScheduledDays,
                MaxUsageCount = dto.MaxUsageCount,
                MaxUsagePerCustomer = dto.MaxUsagePerCustomer,
                MinOrderAmount = dto.MinOrderAmount,
                TargetType = dto.TargetType,
                RequiredLoyaltyTier = dto.RequiredLoyaltyTier,
                RequiredPriceListId = dto.RequiredPriceListId,
                TargetContactId = dto.TargetContactId,
                IsStackable = dto.IsStackable,
                Notes = dto.Notes,
            };

            foreach (var item in dto.Items)
                promo.Items.Add(MapItemFromDto(item, Guid.Empty));

            await _promotions.AddAsync(promo);
            await _promotions.SaveChangesAsync();
            return CreatedAtAction(nameof(GetById), new { id = promo.Id },
                new ApiResponse<PromotionDto> { Success = true, Data = MapToDto(promo), Message = "Promotion created" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating promotion");
            return StatusCode(500, new ApiErrorResponse { Message = "Error creating promotion" });
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdatePromotionDto dto)
    {
        try
        {
            var promo = await _promotions.GetByIdAsync(id);
            if (promo == null) return NotFound(new ApiErrorResponse { Message = "Promotion not found" });
            if (promo.Status == PromotionStatus.Cancelled)
                return BadRequest(new ApiErrorResponse { Message = "Cannot update a cancelled promotion" });

            if (dto.Name != null) promo.Name = dto.Name;
            if (dto.Description != null) promo.Description = dto.Description;
            if (dto.PromotionCode != null) promo.PromotionCode = dto.PromotionCode.Trim().ToUpperInvariant();
            if (dto.IsAutoApplied.HasValue) promo.IsAutoApplied = dto.IsAutoApplied.Value;
            if (dto.Priority.HasValue) promo.Priority = dto.Priority.Value;
            if (dto.StartDate.HasValue) promo.StartDate = dto.StartDate.Value;
            if (dto.EndDate.HasValue) promo.EndDate = dto.EndDate.Value;
            if (dto.StartTime.HasValue) promo.StartTime = dto.StartTime.Value;
            if (dto.EndTime.HasValue) promo.EndTime = dto.EndTime.Value;
            if (dto.ScheduledDays.HasValue) promo.ScheduledDays = dto.ScheduledDays.Value;
            if (dto.MaxUsageCount.HasValue) promo.MaxUsageCount = dto.MaxUsageCount.Value;
            if (dto.MaxUsagePerCustomer.HasValue) promo.MaxUsagePerCustomer = dto.MaxUsagePerCustomer.Value;
            if (dto.MinOrderAmount.HasValue) promo.MinOrderAmount = dto.MinOrderAmount.Value;
            if (dto.TargetType.HasValue) promo.TargetType = dto.TargetType.Value;
            if (dto.RequiredLoyaltyTier.HasValue) promo.RequiredLoyaltyTier = dto.RequiredLoyaltyTier.Value;
            if (dto.RequiredPriceListId.HasValue) promo.RequiredPriceListId = dto.RequiredPriceListId.Value;
            if (dto.TargetContactId.HasValue) promo.TargetContactId = dto.TargetContactId.Value;
            if (dto.IsStackable.HasValue) promo.IsStackable = dto.IsStackable.Value;
            if (dto.Notes != null) promo.Notes = dto.Notes;

            _promotions.Update(promo);
            await _promotions.SaveChangesAsync();
            return Ok(new ApiResponse<PromotionDto> { Success = true, Data = MapToDto(promo), Message = "Promotion updated" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating promotion {Id}", id);
            return StatusCode(500, new ApiErrorResponse { Message = "Error updating promotion" });
        }
    }

    [HttpPut("{id}/activate")]
    public async Task<IActionResult> Activate(Guid id)
        => await SetStatus(id, PromotionStatus.Active, "Promotion activated");

    [HttpPut("{id}/pause")]
    public async Task<IActionResult> Pause(Guid id)
        => await SetStatus(id, PromotionStatus.Paused, "Promotion paused");

    [HttpPut("{id}/cancel")]
    public async Task<IActionResult> Cancel(Guid id)
        => await SetStatus(id, PromotionStatus.Cancelled, "Promotion cancelled");

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            var promo = await _promotions.GetByIdAsync(id);
            if (promo == null) return NotFound(new ApiErrorResponse { Message = "Promotion not found" });
            if (promo.Status == PromotionStatus.Active)
                return Conflict(new ApiErrorResponse { Message = "Deactivate or pause the promotion before deleting" });

            _promotions.SoftDelete(promo);
            await _promotions.SaveChangesAsync();
            return Ok(new ApiResponse<object> { Success = true, Message = "Promotion deleted" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting promotion {Id}", id);
            return StatusCode(500, new ApiErrorResponse { Message = "Error deleting promotion" });
        }
    }

    // ── Items ──────────────────────────────────────────────────────────────────

    [HttpPost("{id}/items")]
    public async Task<IActionResult> AddItem(Guid id, [FromBody] CreatePromotionItemDto dto)
    {
        try
        {
            var promo = await _promotions.GetByIdAsync(id);
            if (promo == null) return NotFound(new ApiErrorResponse { Message = "Promotion not found" });

            if (dto.ItemId == null && dto.ItemCategoryId == null)
                return BadRequest(new ApiErrorResponse { Message = "Provide either ItemId or ItemCategoryId" });

            var item = MapItemFromDto(dto, id);
            await _items.AddAsync(item);
            return Ok(new ApiResponse<PromotionItemDto> { Success = true, Data = MapItemToDto(item), Message = "Item added to promotion" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding item to promotion {Id}", id);
            return StatusCode(500, new ApiErrorResponse { Message = "Error adding item" });
        }
    }

    [HttpDelete("{id}/items/{itemId}")]
    public async Task<IActionResult> RemoveItem(Guid id, Guid itemId)
    {
        try
        {
            var item = await _items.GetByIdAsync(itemId);
            if (item == null || item.PromotionId != id)
                return NotFound(new ApiErrorResponse { Message = "Promotion item not found" });

            _items.SoftDelete(item);
            await _items.SaveChangesAsync();
            return Ok(new ApiResponse<object> { Success = true, Message = "Item removed from promotion" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing item {ItemId} from promotion {Id}", itemId, id);
            return StatusCode(500, new ApiErrorResponse { Message = "Error removing item" });
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>Next free PROMO-#### code, skipping every code already in use (incl. soft-deleted).</summary>
    private static string NextCode(HashSet<string> usedCodes)
    {
        var max = 0;
        foreach (var c in usedCodes)
        {
            var m = System.Text.RegularExpressions.Regex.Match(c, @"^PROMO-(\d+)$");
            if (m.Success && int.TryParse(m.Groups[1].Value, out var n) && n > max) max = n;
        }
        var next = max + 1;
        while (usedCodes.Contains($"PROMO-{next:D4}")) next++;
        return $"PROMO-{next:D4}";
    }

    private async Task<IActionResult> SetStatus(Guid id, PromotionStatus status, string message)
    {
        try
        {
            var promo = await _promotions.GetByIdAsync(id);
            if (promo == null) return NotFound(new ApiErrorResponse { Message = "Promotion not found" });

            promo.Status = status;
            _promotions.Update(promo);
            await _promotions.SaveChangesAsync();
            return Ok(new ApiResponse<PromotionDto> { Success = true, Data = MapToDto(promo), Message = message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing status of promotion {Id}", id);
            return StatusCode(500, new ApiErrorResponse { Message = "Error updating promotion status" });
        }
    }

    private static PromotionItem MapItemFromDto(CreatePromotionItemDto dto, Guid promotionId) => new()
    {
        PromotionId = promotionId,
        ItemId = dto.ItemId,
        ItemCategoryId = dto.ItemCategoryId,
        DiscountType = dto.DiscountType,
        PriceTarget = dto.PriceTarget,
        Value = dto.Value,
        IsConditional = dto.IsConditional,
        ConditionType = dto.ConditionType,
        ConditionQuantity = dto.ConditionQuantity,
        ConditionAmount = dto.ConditionAmount,
        MaxDiscountedQuantity = dto.MaxDiscountedQuantity,
        BuyQuantity = dto.BuyQuantity,
        GetQuantity = dto.GetQuantity,
        FreeItemId = dto.FreeItemId,
    };

    private static PromotionDto MapToDto(Promotion p) => new()
    {
        Id = p.Id,
        Name = p.Name,
        Description = p.Description,
        PromotionCode = p.PromotionCode,
        IsAutoApplied = p.IsAutoApplied,
        Status = p.Status,
        Priority = p.Priority,
        StartDate = p.StartDate,
        EndDate = p.EndDate,
        StartTime = p.StartTime,
        EndTime = p.EndTime,
        ScheduledDays = p.ScheduledDays,
        MaxUsageCount = p.MaxUsageCount,
        MaxUsagePerCustomer = p.MaxUsagePerCustomer,
        CurrentUsageCount = p.CurrentUsageCount,
        MinOrderAmount = p.MinOrderAmount,
        TargetType = p.TargetType,
        RequiredLoyaltyTier = p.RequiredLoyaltyTier,
        RequiredPriceListId = p.RequiredPriceListId,
        TargetContactId = p.TargetContactId,
        IsStackable = p.IsStackable,
        Notes = p.Notes,
        Items = p.Items.Where(i => !i.IsDeleted).Select(MapItemToDto).ToList(),
    };

    private static PromotionItemDto MapItemToDto(PromotionItem i) => new()
    {
        Id = i.Id,
        PromotionId = i.PromotionId,
        ItemId = i.ItemId,
        ItemCategoryId = i.ItemCategoryId,
        DiscountType = i.DiscountType,
        PriceTarget = i.PriceTarget,
        Value = i.Value,
        IsConditional = i.IsConditional,
        ConditionType = i.ConditionType,
        ConditionQuantity = i.ConditionQuantity,
        ConditionAmount = i.ConditionAmount,
        MaxDiscountedQuantity = i.MaxDiscountedQuantity,
        BuyQuantity = i.BuyQuantity,
        GetQuantity = i.GetQuantity,
        FreeItemId = i.FreeItemId,
    };
}
