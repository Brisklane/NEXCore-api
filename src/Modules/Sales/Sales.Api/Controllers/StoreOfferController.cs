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
public class StoreOfferController : ControllerBase
{
    private readonly IStoreOfferRepository _offers;
    private readonly IPosStoreRepository _stores;
    private readonly ILogger<StoreOfferController> _logger;

    public StoreOfferController(
        IStoreOfferRepository offers,
        IPosStoreRepository stores,
        ILogger<StoreOfferController> logger)
    {
        _offers = offers;
        _stores = stores;
        _logger = logger;
    }

    // ── Public customer-app endpoint ──────────────────────────────────────────

    /// <summary>
    /// Returns all currently active offers for a store — no authentication required.
    /// Used by the customer app to populate the store's home page.
    /// Includes linked promotion summary when present.
    /// </summary>
    [HttpGet("store/{storeId:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetPublicOffers(Guid storeId, [FromQuery] DateTime? at = null)
    {
        try
        {
            var now = at ?? DateTime.Now;
            var date = DateOnly.FromDateTime(now);
            var time = TimeOnly.FromDateTime(now);

            var offers = await _offers.GetPublicActiveByStoreAsync(storeId, date, time);
            return Ok(new ApiResponse<List<StoreOfferDto>>
            {
                Success = true,
                Data = offers.Select(o => MapToDto(o, includePromotion: true)).ToList(),
                Message = $"{offers.Count} offer(s) available"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving public offers for store {StoreId}", storeId);
            return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving offers" });
        }
    }

    // ── Staff / admin endpoints ───────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            var all = await _offers.GetAllAsync();
            return Ok(new ApiResponse<List<StoreOfferDto>>
            {
                Success = true,
                Data = all.Where(o => !o.IsDeleted).OrderBy(o => o.DisplayOrder).Select(o => MapToDto(o)).ToList(),
                Message = "Store offers retrieved"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving store offers");
            return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving store offers" });
        }
    }

    [HttpGet("store/{storeId:guid}/all")]
    public async Task<IActionResult> GetByStore(Guid storeId)
    {
        try
        {
            var offers = await _offers.GetByStoreAsync(storeId);
            return Ok(new ApiResponse<List<StoreOfferDto>>
            {
                Success = true,
                Data = offers.Select(o => MapToDto(o)).ToList(),
                Message = "Store offers retrieved"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving offers for store {StoreId}", storeId);
            return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving offers" });
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        try
        {
            var offer = await _offers.GetByIdAsync(id);
            if (offer == null || offer.IsDeleted)
                return NotFound(new ApiErrorResponse { Message = "Store offer not found" });
            return Ok(new ApiResponse<StoreOfferDto> { Success = true, Data = MapToDto(offer), Message = "Store offer retrieved" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving store offer {Id}", id);
            return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving store offer" });
        }
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateStoreOfferDto dto)
    {
        try
        {
            var store = await _stores.GetByIdAsync(dto.StoreId);
            if (store == null || store.IsDeleted)
                return BadRequest(new ApiErrorResponse { Message = "Store not found" });

            var offer = new StoreOffer
            {
                StoreId = dto.StoreId,
                OfferType = dto.OfferType,
                Title = dto.Title,
                Subtitle = dto.Subtitle,
                Description = dto.Description,
                ImageUrl = dto.ImageUrl,
                BannerUrl = dto.BannerUrl,
                BadgeText = dto.BadgeText,
                BadgeColor = dto.BadgeColor,
                CallToAction = dto.CallToAction,
                DeepLinkUrl = dto.DeepLinkUrl,
                DisplayOrder = dto.DisplayOrder,
                IsActive = true,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                StartTime = dto.StartTime,
                EndTime = dto.EndTime,
                PromotionId = dto.PromotionId,
                ItemId = dto.ItemId,
                ItemCategoryId = dto.ItemCategoryId,
            };

            await _offers.AddAsync(offer);
            await _offers.SaveChangesAsync();
            return CreatedAtAction(nameof(GetById), new { id = offer.Id },
                new ApiResponse<StoreOfferDto> { Success = true, Data = MapToDto(offer), Message = "Store offer created" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating store offer");
            return StatusCode(500, new ApiErrorResponse { Message = "Error creating store offer" });
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateStoreOfferDto dto)
    {
        try
        {
            var offer = await _offers.GetByIdAsync(id);
            if (offer == null || offer.IsDeleted)
                return NotFound(new ApiErrorResponse { Message = "Store offer not found" });

            if (dto.OfferType.HasValue) offer.OfferType = dto.OfferType.Value;
            if (dto.Title != null) offer.Title = dto.Title;
            if (dto.Subtitle != null) offer.Subtitle = dto.Subtitle;
            if (dto.Description != null) offer.Description = dto.Description;
            if (dto.ImageUrl != null) offer.ImageUrl = dto.ImageUrl;
            if (dto.BannerUrl != null) offer.BannerUrl = dto.BannerUrl;
            if (dto.BadgeText != null) offer.BadgeText = dto.BadgeText;
            if (dto.BadgeColor != null) offer.BadgeColor = dto.BadgeColor;
            if (dto.CallToAction != null) offer.CallToAction = dto.CallToAction;
            if (dto.DeepLinkUrl != null) offer.DeepLinkUrl = dto.DeepLinkUrl;
            if (dto.DisplayOrder.HasValue) offer.DisplayOrder = dto.DisplayOrder.Value;
            if (dto.IsActive.HasValue) offer.IsActive = dto.IsActive.Value;
            if (dto.StartDate.HasValue) offer.StartDate = dto.StartDate.Value;
            if (dto.EndDate.HasValue) offer.EndDate = dto.EndDate.Value;
            if (dto.StartTime.HasValue) offer.StartTime = dto.StartTime.Value;
            if (dto.EndTime.HasValue) offer.EndTime = dto.EndTime.Value;
            if (dto.PromotionId.HasValue) offer.PromotionId = dto.PromotionId.Value;
            if (dto.ItemId.HasValue) offer.ItemId = dto.ItemId.Value;
            if (dto.ItemCategoryId.HasValue) offer.ItemCategoryId = dto.ItemCategoryId.Value;

            _offers.Update(offer);
            await _offers.SaveChangesAsync();
            return Ok(new ApiResponse<StoreOfferDto> { Success = true, Data = MapToDto(offer), Message = "Store offer updated" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating store offer {Id}", id);
            return StatusCode(500, new ApiErrorResponse { Message = "Error updating store offer" });
        }
    }

    /// <summary>Toggle the offer's active/inactive state without a full update.</summary>
    [HttpPut("{id:guid}/toggle")]
    public async Task<IActionResult> Toggle(Guid id)
    {
        try
        {
            var offer = await _offers.GetByIdAsync(id);
            if (offer == null || offer.IsDeleted)
                return NotFound(new ApiErrorResponse { Message = "Store offer not found" });

            offer.IsActive = !offer.IsActive;
            _offers.Update(offer);
            await _offers.SaveChangesAsync();
            var state = offer.IsActive ? "activated" : "deactivated";
            return Ok(new ApiResponse<StoreOfferDto> { Success = true, Data = MapToDto(offer), Message = $"Store offer {state}" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling store offer {Id}", id);
            return StatusCode(500, new ApiErrorResponse { Message = "Error toggling store offer" });
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            var offer = await _offers.GetByIdAsync(id);
            if (offer == null || offer.IsDeleted)
                return NotFound(new ApiErrorResponse { Message = "Store offer not found" });

            _offers.SoftDelete(offer);
            await _offers.SaveChangesAsync();
            return Ok(new ApiResponse<object> { Success = true, Message = "Store offer deleted" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting store offer {Id}", id);
            return StatusCode(500, new ApiErrorResponse { Message = "Error deleting store offer" });
        }
    }

    // ── Mapping ───────────────────────────────────────────────────────────────

    private static StoreOfferDto MapToDto(StoreOffer o, bool includePromotion = false) => new()
    {
        Id = o.Id,
        StoreId = o.StoreId,
        OfferType = o.OfferType,
        Title = o.Title,
        Subtitle = o.Subtitle,
        Description = o.Description,
        ImageUrl = o.ImageUrl,
        BannerUrl = o.BannerUrl,
        BadgeText = o.BadgeText,
        BadgeColor = o.BadgeColor,
        CallToAction = o.CallToAction,
        DeepLinkUrl = o.DeepLinkUrl,
        DisplayOrder = o.DisplayOrder,
        IsActive = o.IsActive,
        StartDate = o.StartDate,
        EndDate = o.EndDate,
        StartTime = o.StartTime,
        EndTime = o.EndTime,
        PromotionId = o.PromotionId,
        ItemId = o.ItemId,
        ItemCategoryId = o.ItemCategoryId,
        Promotion = includePromotion && o.Promotion != null ? new LinkedPromotionDto
        {
            Id = o.Promotion.Id,
            Name = o.Promotion.Name,
            PromotionCode = o.Promotion.PromotionCode,
            DiscountType = o.Promotion.Items.FirstOrDefault()?.DiscountType,
            DiscountValue = o.Promotion.Items.FirstOrDefault()?.Value,
            EndDate = o.Promotion.EndDate,
        } : null,
    };
}
