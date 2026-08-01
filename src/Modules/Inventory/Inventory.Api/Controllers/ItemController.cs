using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Inventory.Application.DTOs;
using Inventory.Application.Services.Interfaces;
using Inventory.Infrastructure.Repositories.Interfaces;
using Nexcore.SharedKernel.Api;
using System.Security.Claims;
using Inv = Inventory.Domain.Constants;

namespace Inventory.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ItemController : ControllerBase
{
    private readonly IItemRepository _itemRepository;
    private readonly IUnitRepository _unitRepository;
    private readonly IBlobStorageService _blobStorage;
    private readonly ILogger<ItemController> _logger;

    public ItemController(
        IItemRepository itemRepository,
        IUnitRepository unitRepository,
        IBlobStorageService blobStorage,
        ILogger<ItemController> logger)
    {
        _itemRepository = itemRepository;
        _unitRepository = unitRepository;
        _blobStorage = blobStorage;
        _logger = logger;
    }

    /// <summary>Get all items (paginated)</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResponse<ItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10,
        [FromQuery] string? searchTerm = null, [FromQuery] string? sortBy = null, [FromQuery] string? sortDirection = null, [FromQuery] bool? isActive = null,
        [FromQuery] Guid? warehouseId = null, [FromQuery] string? itemType = null)
    {
        try
        {
            var (items, total) = await _itemRepository.GetAllPagedAsync(pageNumber, pageSize, searchTerm, sortBy, sortDirection, isActive, warehouseId, itemType);

            return Ok(PaginatedResponse<ItemDto>.Ok(
                items.Select(MapToDtoWithSas),
                total,
                pageNumber,
                pageSize,
                "Items retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving items");
            return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving items", Errors = GetErrorDetail(ex) });
        }
    }

    /// <summary>Get item by ID</summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<ItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        try
        {
            var item = await _itemRepository.GetWithFullDetailsReadOnlyAsync(id);
            if (item == null)
                return NotFound(new ApiErrorResponse { Message = "Item not found" });

            var dto = MapToDto(item);
            ApplySasUrls(dto);

            return Ok(new ApiResponse<ItemDto>
            {
                Success = true,
                Data = dto,
                Message = "Item retrieved successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving item {ItemId}", id);
            return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving item", Errors = GetErrorDetail(ex) });
        }
    }

    /// <summary>Get item by code (SKU)</summary>
    [HttpGet("by-code/{code}")]
    [ProducesResponseType(typeof(ApiResponse<ItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByCode(string code)
    {
        try
        {
            var item = await _itemRepository.GetWithFullDetailsByCodeAsync(code);
            if (item == null)
                return NotFound(new ApiErrorResponse { Message = "Item not found" });

            return Ok(new ApiResponse<ItemDto>
            {
                Success = true,
                Data = MapToDtoWithSas(item),
                Message = "Item retrieved successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving item by code {Code}", code);
            return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving item", Errors = GetErrorDetail(ex) });
        }
    }

    /// <summary>Get all items with basic information (lightweight for dropdowns)</summary>
    [HttpGet("basic")]
    [ProducesResponseType(typeof(ApiResponse<List<ItemBasicDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetItemsBasic(
        [FromQuery] int? pageNumber = null,
        [FromQuery] int? pageSize = null,
        [FromQuery] string? search = null,
        [FromQuery] string? itemType = null)
    {
        try
        {
            // If pagination parameters are provided, use paginated endpoint
            if (pageNumber.HasValue && pageSize.HasValue)
            {
                var page = Math.Max(1, pageNumber.Value);
                var size = Math.Max(1, Math.Min(100, pageSize.Value)); // Max 100 items per page

                var (items, totalCount) = await _itemRepository.GetItemsBasicPaginatedAsync(
                    page, 
                    size, 
                    search, 
                    itemType);

                var itemDtos = items.Select(MapToBasicDto).ToList();

                return Ok(PaginatedResponse<ItemBasicDto>.Ok(
                    data: itemDtos,
                    totalCount: totalCount,
                    pageNumber: page,
                    pageSize: size,
                    message: "Items retrieved successfully"
                ));
            }
            else
            {
                // Backward compatibility: return all items if no pagination params
                var items = await _itemRepository.GetItemsBasicAsync();

                return Ok(new ApiResponse<List<ItemBasicDto>>
                {
                    Success = true,
                    Data = items.Select(MapToBasicDto).ToList(),
                    Message = "Items retrieved successfully"
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving items (basic)");
            return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving items", Errors = GetErrorDetail(ex) });
        }
    }

    /// <summary>Create new item</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<ItemDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateItemDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(new ApiErrorResponse { Message = "Invalid input" });

            var existingItem = await _itemRepository.GetByCodeAsync(dto.Code);
            if (existingItem != null)
                return BadRequest(new ApiErrorResponse { Message = "Item code already exists" });

            var baseUnit = await _unitRepository.GetByIdAsync(dto.BaseUnitId);
            if (baseUnit == null)
                return BadRequest(new ApiErrorResponse { Message = "Base unit not found" });

            // \u2500\u2500 Convert simple Barcode/Price fields to collections if provided \u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500
            if (!string.IsNullOrWhiteSpace(dto.Barcode) && (dto.Barcodes == null || !dto.Barcodes.Any()))
            {
                dto.Barcodes = new List<CreateItemBarcodeDto>
                {
                    new CreateItemBarcodeDto
                    {
                        Barcode = dto.Barcode,
                        UnitId = dto.BaseUnitId,
                        BarcodeType = Inv.BarcodeType.EAN13,
                        IsPrimary = true
                    }
                };
            }

            // \u2500\u2500 Duplicate barcode check \u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500
            foreach (var b in dto.Barcodes ?? [])
            {
                var owner = await _itemRepository.GetByBarcodeAsync(b.Barcode);
                if (owner != null)
                    return BadRequest(new ApiErrorResponse
                    {
                        Message = $"Barcode '{b.Barcode}' is already assigned to item '{owner.Code} \u2013 {owner.Name}'"
                    });
            }

            if ((dto.SalePrice.HasValue || dto.PurchasePrice.HasValue) && (dto.Prices == null || !dto.Prices.Any()))
            {
                dto.Prices = new List<CreateItemPriceDto>
                {
                    new CreateItemPriceDto
                    {
                        UnitId = dto.BaseUnitId,
                        PriceList = Inv.PriceList.Default,
                        SalePrice = dto.SalePrice ?? 0,
                        PurchasePrice = dto.PurchasePrice ?? 0,
                        IsTaxInclusive = false,
                        CurrencyCode = "USD"
                    }
                };
            }

            // Normalize the unit-level tracking mode: TrackingType is the source of truth; if the client
            // only sent the legacy booleans, derive it from them. Booleans are then kept in sync.
            var (trackingType, isBatchTracked, isSerialTracked) =
                NormalizeTracking(dto.TrackingType, dto.IsSerialTracked, dto.IsBatchTracked);

            var item = new Inventory.Domain.Entities.Item
            {
                // ?? Identity ??????????????????????????????????????????????????
                Code = dto.Code,
                Name = dto.Name,
                ItemType = dto.ItemType,
                ShortDescription = dto.ShortDescription,
                Description = dto.Description,

                // ?? Classification ????????????????????????????????????????????
                CategoryId = dto.CategoryId,
                BrandId = dto.BrandId,
                DisplayColorId = dto.DisplayColorId,
                BaseUnitId = dto.BaseUnitId,
                Condition = dto.Condition,
                AgeRestriction = dto.AgeRestriction,

                // ?? GL Accounts ???????????????????????????????????????????????
                InventoryAccountId = dto.InventoryAccountId,
                CogsAccountId = dto.CogsAccountId,
                PurchaseAccountId = dto.PurchaseAccountId,
                SalesAccountId = dto.SalesAccountId,

                // ?? Costing & Tracking ????????????????????????????????????????
                CostingMethod = dto.CostingMethod,
                TrackingType = trackingType,
                IsBatchTracked = isBatchTracked,
                IsSerialTracked = isSerialTracked,
                HasVariants = dto.HasVariants,
                IsComponent = dto.IsComponent,

                // ?? Stock Control ?????????????????????????????????????????????
                ReorderLevel = dto.ReorderLevel,
                MaxStockLevel = dto.MaxStockLevel,
                EconomicOrderQuantity = dto.EconomicOrderQuantity,
                AlertOnLowStock = dto.AlertOnLowStock,
                AlertOnExcessStock = dto.AlertOnLowStock,

                // ?? Status ????????????????????????????????????????????????????
                IsActive = dto.IsActive,
                IsPublished = dto.IsPublished,
                IsFeatured = dto.IsFeatured,

                // ?? Nested collections ????????????????????????????????????????
                Images = (dto.Images ?? []).Select(i => new Inventory.Domain.Entities.ItemImage
                {
                    Url = i.Url,
                    Resolution = i.Resolution,
                    Width = i.Width,
                    Height = i.Height,
                    FileSizeBytes = i.FileSizeBytes,
                    ContentType = i.ContentType,
                    AltText = i.AltText,
                    DisplayOrder = i.DisplayOrder,
                    IsPrimary = i.IsPrimary,
                }).ToList(),

                Barcodes = (dto.Barcodes ?? []).Select(b => new Inventory.Domain.Entities.ItemBarcode
                {
                    UnitId = b.UnitId,
                    Barcode = b.Barcode,
                    BarcodeType = b.BarcodeType,
                    IsPrimary = b.IsPrimary,
                }).ToList(),

                Prices = (dto.Prices ?? []).Select(p => new Inventory.Domain.Entities.ItemPrice
                {
                    UnitId = p.UnitId,
                    PriceList = p.PriceList,
                    SalePrice = p.SalePrice,
                    MinSalePrice = p.MinSalePrice,
                    PurchasePrice = p.PurchasePrice,
                    IsTaxInclusive = p.IsTaxInclusive,
                    CurrencyCode = p.CurrencyCode,
                    ValidFrom = p.ValidFrom,
                    ValidTo = p.ValidTo,
                }).ToList(),

                Taxes = dto.Taxes?.Select(t => new Inventory.Domain.Entities.ItemTax
                {
                    TaxDefinitionId = t.TaxDefinitionId,
                    OverrideRate = t.OverrideRate,
                }).ToList() ?? new List<Inventory.Domain.Entities.ItemTax>(),

                Attributes = dto.Attributes?.Select(a => new Inventory.Domain.Entities.ItemAttribute
                {
                    AttributeDefinitionId = a.AttributeDefinitionId,
                    Value = a.Value,
                }).ToList() ?? new List<Inventory.Domain.Entities.ItemAttribute>(),

                Colors = dto.ColorIds?.Select(colorId => new Inventory.Domain.Entities.ItemColor
                {
                    ColorId = colorId,
                }).ToList() ?? new List<Inventory.Domain.Entities.ItemColor>(),

                Sizes = dto.Sizes?.Select(s => new Inventory.Domain.Entities.ItemSize
                {
                    SizeId = s.SizeId,
                    ItemBarcodeId = s.ItemBarcodeId,
                    IsDefault = s.IsDefault,
                }).ToList() ?? new List<Inventory.Domain.Entities.ItemSize>(),

                Variants = dto.Variants?.Select(v => new Inventory.Domain.Entities.ItemVariant
                {
                    VariantCode = v.VariantCode,
                    VariantName = v.VariantName,
                    ColorId = v.ColorId,
                    SizeId = v.SizeId,
                    ExtraDimension = v.ExtraDimension,
                    Barcode = v.Barcode,
                    ImageUrl = v.ImageUrl,
                    SalePriceOverride = v.SalePriceOverride,
                    PurchasePriceOverride = v.PurchasePriceOverride,
                    WeightKg = v.WeightKg,
                    DisplayOrder = v.DisplayOrder,
                }).ToList() ?? new List<Inventory.Domain.Entities.ItemVariant>(),

                Shipping = dto.Shipping == null ? null : new Inventory.Domain.Entities.ItemShipping
                {
                    WeightKg = dto.Shipping.WeightKg,
                    LengthCm = dto.Shipping.LengthCm,
                    WidthCm = dto.Shipping.WidthCm,
                    HeightCm = dto.Shipping.HeightCm,
                    CountryOfOrigin = dto.Shipping.CountryOfOrigin,
                    HsCode = dto.Shipping.HsCode,
                    UnitsPerCarton = dto.Shipping.UnitsPerCarton,
                    CartonsPerPallet = dto.Shipping.CartonsPerPallet,
                    CartonWeightKg = dto.Shipping.CartonWeightKg,
                    CartonLengthCm = dto.Shipping.CartonLengthCm,
                    CartonWidthCm = dto.Shipping.CartonWidthCm,
                    CartonHeightCm = dto.Shipping.CartonHeightCm,
                    RequiresSpecialHandling = dto.Shipping.RequiresSpecialHandling,
                    HandlingNotes = dto.Shipping.HandlingNotes,
                    IsHazmat = dto.Shipping.IsHazmat,
                    IsShippableInternational = dto.Shipping.IsShippableInternational,
                },

                Seo = dto.Seo == null ? null : new Inventory.Domain.Entities.ItemSeo
                {
                    MetaTitle = dto.Seo.MetaTitle,
                    MetaDescription = dto.Seo.MetaDescription,
                    MetaKeywords = dto.Seo.MetaKeywords,
                    Slug = dto.Seo.Slug,
                    CanonicalUrl = dto.Seo.CanonicalUrl,
                },

                Warranty = dto.Warranty == null ? null : new Inventory.Domain.Entities.ItemWarranty
                {
                    WarrantyType = dto.Warranty.WarrantyType,
                    DurationMonths = dto.Warranty.DurationMonths,
                    PolicyDescription = dto.Warranty.PolicyDescription,
                    ProviderName = dto.Warranty.ProviderName,
                    ProviderContact = dto.Warranty.ProviderContact,
                },

                ChannelListings = dto.ChannelListings?.Select(cl => new Inventory.Domain.Entities.ItemChannelListing
                {
                    Channel = cl.Channel,
                    ListingStatus = cl.ListingStatus,
                    ChannelTitle = cl.ChannelTitle,
                    ChannelDescription = cl.ChannelDescription,
                    ChannelPrice = cl.ChannelPrice,
                    ChannelDiscount = cl.ChannelDiscount,
                    DiscountType = cl.DiscountType,
                    ExternalProductId = cl.ExternalProductId,
                    ExternalSku = cl.ExternalSku,
                    AutoSyncStock = cl.AutoSyncStock,
                    AutoSyncPrice = cl.AutoSyncPrice,
                }).ToList() ?? new List<Inventory.Domain.Entities.ItemChannelListing>(),

                Suppliers = dto.Suppliers?.Select(s => new Inventory.Domain.Entities.ItemSupplier
                {
                    SupplierId = s.SupplierId,
                    SupplierItemCode = s.SupplierItemCode,
                    SupplierItemName = s.SupplierItemName,
                    LastPurchasePrice = s.LastPurchasePrice,
                    CurrencyCode = s.CurrencyCode,
                    MinOrderQuantity = s.MinOrderQuantity,
                    LeadTimeDays = s.LeadTimeDays,
                    IsPrimary = s.IsPrimary,
                }).ToList() ?? new List<Inventory.Domain.Entities.ItemSupplier>(),

                BundleComponents = dto.BundleComponents?.Select(b => new Inventory.Domain.Entities.ItemBundle
                {
                    ComponentItemId = b.ComponentItemId,
                    Quantity = b.Quantity,
                    UnitId = b.UnitId,
                    IsIncludedInCost = b.IsIncludedInCost,
                    DisplayOrder = b.DisplayOrder,
                }).ToList() ?? new List<Inventory.Domain.Entities.ItemBundle>(),

                Substitutions = dto.Substitutions?.Select(s => new Inventory.Domain.Entities.ItemSubstitution
                {
                    SubstituteItemId = s.SubstituteItemId,
                    Priority = s.Priority,
                    Note = s.Note,
                    IsBidirectional = s.IsBidirectional,
                }).ToList() ?? new List<Inventory.Domain.Entities.ItemSubstitution>(),

                Discounts = dto.Discounts?.Select(d => new Inventory.Domain.Entities.ItemDiscount
                {
                    Name = d.Name,
                    DiscountType = d.DiscountType,
                    DiscountValue = d.DiscountValue,
                    BuyQuantity = d.BuyQuantity,
                    GetQuantity = d.GetQuantity,
                    MinQuantity = d.MinQuantity,
                    MaxQuantity = d.MaxQuantity,
                    ApplicableChannels = d.ApplicableChannels,
                    ValidFrom = d.ValidFrom,
                    ValidTo = d.ValidTo,
                    Priority = d.Priority,
                }).ToList() ?? new List<Inventory.Domain.Entities.ItemDiscount>(),

                Comments = dto.Comments?.Select(c => new Inventory.Domain.Entities.ItemComment
                {
                    CommentType = c.CommentType,
                    Comment = c.Comment,
                    IsPinned = c.IsPinned,
                }).ToList() ?? new List<Inventory.Domain.Entities.ItemComment>(),
            };

            await _itemRepository.AddAsync(item);
            await _itemRepository.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = item.Id },
                new ApiResponse<ItemDto>
                {
                    Success = true,
                    Data = MapToDtoWithSas(item),
                    Message = "Item created successfully"
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating item");
            return StatusCode(500, new ApiErrorResponse { Message = "Error creating item", Errors = GetErrorDetail(ex) });
        }
    }

    /// <summary>Get active items only</summary>
    [HttpGet("active")]
    [ProducesResponseType(typeof(PaginatedResponse<ItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetActive([FromQuery] PaginationParams pagination)
    {
        try
        {
            var (items, total) = await _itemRepository.GetActivePagedAsync(pagination.PageNumber, pagination.PageSize);
            return Ok(PaginatedResponse<ItemDto>.Ok(items.Select(MapToDtoWithSas), total, pagination.PageNumber, pagination.PageSize));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving active items");
            return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving active items", Errors = GetErrorDetail(ex) });
        }
    }

    /// <summary>Search items by code or name (partial match). Minimum 2 characters.</summary>
    [HttpGet("search")]
    [ProducesResponseType(typeof(ApiResponse<List<ItemDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Search([FromQuery] string term)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(term) || term.Length < 2)
                return BadRequest(new ApiErrorResponse { Message = "Search term must be at least 2 characters" });

            var items = await _itemRepository.SearchAsync(term);
            return Ok(new ApiResponse<List<ItemDto>>
            {
                Success = true,
                Data = items.Select(MapToDtoWithSas).ToList(),
                Message = $"{items.Count} item(s) found"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching items with term {Term}", term);
            return StatusCode(500, new ApiErrorResponse { Message = "Error searching items", Errors = GetErrorDetail(ex) });
        }
    }

    /// <summary>Get items published on e-commerce portals</summary>
    [HttpGet("published")]
    [ProducesResponseType(typeof(ApiResponse<List<ItemDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPublished()
    {
        try
        {
            var items = await _itemRepository.GetPublishedItemsAsync();
            return Ok(new ApiResponse<List<ItemDto>>
            {
                Success = true,
                Data = items.Select(MapToDtoWithSas).ToList(),
                Message = "Published items retrieved successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving published items");
            return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving items", Errors = GetErrorDetail(ex) });
        }
    }

    /// <summary>Get featured items — used for banners and POS quick-access grids</summary>
    [HttpGet("featured")]
    [ProducesResponseType(typeof(ApiResponse<List<ItemDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetFeatured()
    {
        try
        {
            var items = await _itemRepository.GetFeaturedItemsAsync();
            return Ok(new ApiResponse<List<ItemDto>>
            {
                Success = true,
                Data = items.Select(MapToDtoWithSas).ToList(),
                Message = "Featured items retrieved successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving featured items");
            return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving items", Errors = GetErrorDetail(ex) });
        }
    }

    /// <summary>Get items listed on a specific sales channel (e.g. POS, DARAZ, WEBSITE)</summary>
    [HttpGet("by-channel/{channel}")]
    [ProducesResponseType(typeof(ApiResponse<List<ItemDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByChannel(string channel)
    {
        try
        {
            var items = await _itemRepository.GetItemsByChannelAsync(channel);
            return Ok(new ApiResponse<List<ItemDto>>
            {
                Success = true,
                Data = items.Select(MapToDtoWithSas).ToList(),
                Message = $"Items for channel '{channel}' retrieved successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving items for channel {Channel}", channel);
            return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving items", Errors = GetErrorDetail(ex) });
        }
    }

    /// <summary>Get items by category</summary>
    [HttpGet("by-category/{categoryId}")]
    [ProducesResponseType(typeof(ApiResponse<List<ItemDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByCategory(Guid categoryId)
    {
        try
        {
            var items = await _itemRepository.GetItemsByCategory(categoryId);
            return Ok(new ApiResponse<List<ItemDto>>
            {
                Success = true,
                Data = items.Select(MapToDtoWithSas).ToList(),
                Message = "Items retrieved successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving items for category {CategoryId}", categoryId);
            return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving items", Errors = GetErrorDetail(ex) });
        }
    }

    /// <summary>Get item by barcode value — used for POS scanner input</summary>
    [HttpGet("by-barcode/{barcode}")]
    [ProducesResponseType(typeof(ApiResponse<ItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByBarcode(string barcode)
    {
        try
        {
            var item = await _itemRepository.GetByBarcodeAsync(barcode);
            if (item == null)
                return NotFound(new ApiErrorResponse { Message = "Item not found for barcode" });

            return Ok(new ApiResponse<ItemDto>
            {
                Success = true,
                Data = MapToDtoWithSas(item),
                Message = "Item retrieved successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving item by barcode {Barcode}", barcode);
            return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving item", Errors = GetErrorDetail(ex) });
        }
    }

    /// <summary>
    /// Get item with all navigation properties fully loaded.
    /// More expensive than GET /{id} — use for detail/edit pages only.
    /// </summary>
    [HttpGet("{id}/details")]
    [ProducesResponseType(typeof(ApiResponse<ItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetFullDetails(Guid id)
    {
        try
        {
            var item = await _itemRepository.GetWithFullDetailsAsync(id);
            if (item == null)
                return NotFound(new ApiErrorResponse { Message = "Item not found" });

            return Ok(new ApiResponse<ItemDto>
            {
                Success = true,
                Data = MapToDtoWithSas(item),
                Message = "Item details retrieved successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving full details for item {ItemId}", id);
            return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving item details", Errors = GetErrorDetail(ex) });
        }
    }

    /// <summary>Delete item (soft-delete)</summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            var item = await _itemRepository.GetByIdAsync(id);
            if (item == null)
                return NotFound(new ApiErrorResponse { Message = "Item not found" });

            _itemRepository.SoftDelete(item);
            await _itemRepository.SaveChangesAsync();

            return Ok(new ApiResponse<bool>
            {
                Success = true,
                Data = true,
                Message = "Item deleted successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting item {ItemId}", id);
            return StatusCode(500, new ApiErrorResponse { Message = "Error deleting item", Errors = GetErrorDetail(ex) });
        }
    }

    /// <summary>Update item</summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse<ItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateItemDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(new ApiErrorResponse { Message = "Invalid input" });

            var item = await _itemRepository.GetWithFullDetailsAsync(id);
            if (item == null)
                return NotFound(new ApiErrorResponse { Message = "Item not found" });

            if (dto.Code != null && dto.Code != item.Code)
            {
                var dup = await _itemRepository.GetByCodeAsync(dto.Code);
                if (dup != null)
                    return BadRequest(new ApiErrorResponse { Message = "Item code already exists" });
                item.Code = dto.Code;
            }
            if (dto.Name != null) item.Name = dto.Name;
            if (dto.ItemType != null) item.ItemType = dto.ItemType;
            if (dto.ShortDescription != null) item.ShortDescription = dto.ShortDescription;
            if (dto.Description != null) item.Description = dto.Description;
            if (dto.Condition != null) item.Condition = dto.Condition;
            if (dto.CostingMethod != null) item.CostingMethod = dto.CostingMethod;
            if (dto.AgeRestriction.HasValue) item.AgeRestriction = dto.AgeRestriction.Value;
            if (dto.CategoryId.HasValue) item.CategoryId = dto.CategoryId.Value;
            if (dto.BrandId.HasValue) item.BrandId = dto.BrandId.Value;
            if (dto.DisplayColorId.HasValue) item.DisplayColorId = dto.DisplayColorId.Value;
            if (dto.BaseUnitId.HasValue) item.BaseUnitId = dto.BaseUnitId.Value;
            if (dto.InventoryAccountId.HasValue) item.InventoryAccountId = dto.InventoryAccountId.Value;
            if (dto.CogsAccountId.HasValue) item.CogsAccountId = dto.CogsAccountId.Value;
            if (dto.PurchaseAccountId.HasValue) item.PurchaseAccountId = dto.PurchaseAccountId.Value;
            if (dto.SalesAccountId.HasValue) item.SalesAccountId = dto.SalesAccountId.Value;
            // Tracking mode: TrackingType wins when supplied; otherwise a supplied legacy boolean drives it.
            if (!string.IsNullOrWhiteSpace(dto.TrackingType) || dto.IsBatchTracked.HasValue || dto.IsSerialTracked.HasValue)
            {
                var (tt, isBatch, isSerial) = NormalizeTracking(
                    dto.TrackingType,
                    dto.IsSerialTracked ?? item.IsSerialTracked,
                    dto.IsBatchTracked ?? item.IsBatchTracked);
                item.TrackingType = tt;
                item.IsBatchTracked = isBatch;
                item.IsSerialTracked = isSerial;
            }
            if (dto.HasVariants.HasValue) item.HasVariants = dto.HasVariants.Value;
            if (dto.IsComponent.HasValue) item.IsComponent = dto.IsComponent.Value;
            if (dto.AlertOnLowStock.HasValue) item.AlertOnLowStock = dto.AlertOnLowStock.Value;
            if (dto.AlertOnExcessStock.HasValue) item.AlertOnExcessStock = dto.AlertOnExcessStock.Value;
            if (dto.IsActive.HasValue) item.IsActive = dto.IsActive.Value;
            if (dto.IsPublished.HasValue) item.IsPublished = dto.IsPublished.Value;
            if (dto.IsFeatured.HasValue) item.IsFeatured = dto.IsFeatured.Value;
            if (dto.ReorderLevel.HasValue) item.ReorderLevel = dto.ReorderLevel;
            if (dto.MaxStockLevel.HasValue) item.MaxStockLevel = dto.MaxStockLevel;
            if (dto.EconomicOrderQuantity.HasValue) item.EconomicOrderQuantity = dto.EconomicOrderQuantity;

            if (dto.ColorIds != null)
            {
                var toRemove = item.Colors.Where(c => !dto.ColorIds.Contains(c.ColorId)).ToList();
                foreach (var c in toRemove) item.Colors.Remove(c);
                var existing = item.Colors.Select(c => c.ColorId).ToHashSet();
                foreach (var colorId in dto.ColorIds.Where(cid => !existing.Contains(cid)))
                    item.Colors.Add(new Inventory.Domain.Entities.ItemColor { ColorId = colorId });
            }

            // ── Quick prices — upsert the Default price list entry ─────────────────
            if (dto.SalePrice.HasValue || dto.PurchasePrice.HasValue)
            {
                var unitId = dto.BaseUnitId ?? item.BaseUnitId;
                var existing = item.Prices.FirstOrDefault(p => p.PriceList == Inv.PriceList.Default);
                if (existing != null)
                {
                    if (dto.SalePrice.HasValue) existing.SalePrice = dto.SalePrice.Value;
                    if (dto.PurchasePrice.HasValue) existing.PurchasePrice = dto.PurchasePrice.Value;
                }
                else
                {
                    existing = new Inventory.Domain.Entities.ItemPrice
                    {
                        UnitId = unitId,
                        PriceList = Inv.PriceList.Default,
                        SalePrice = dto.SalePrice ?? 0,
                        PurchasePrice = dto.PurchasePrice ?? 0,
                        IsTaxInclusive = false,
                    };
                    item.Prices.Add(existing);
                }

                // Recompute the tax-derived fields (SalePriceExcludingTax, etc.). The Sales pricing
                // engine reads SalePriceExcludingTax for the POS unit price, so without this a price
                // edit would persist the new SalePrice but leave the old computed value — the POS kept
                // charging the stale price. With no item-level taxes the net equals the entered price.
                var salesTaxes = item.Taxes
                    .Where(t => t.TaxDefinition != null && t.TaxDefinition.IsActive && t.TaxDefinition.ApplyOnSales)
                    .Select(t => (t.EffectiveRate, t.TaxDefinition.IsPercentage,
                                  t.TaxDefinition.InclusionType == Inventory.Domain.Enums.TaxInclusionType.Inclusive));
                var purchaseTaxes = item.Taxes
                    .Where(t => t.TaxDefinition != null && t.TaxDefinition.IsActive && t.TaxDefinition.ApplyOnPurchases)
                    .Select(t => (t.EffectiveRate, t.TaxDefinition.IsPercentage,
                                  t.TaxDefinition.InclusionType == Inventory.Domain.Enums.TaxInclusionType.Inclusive));
                Inventory.Domain.Services.ItemPriceCalculator.RecalculateEntity(existing, salesTaxes, purchaseTaxes);
            }

            // ── Barcodes — full replacement via set-based SQL ───────────────────────
            // Persisted through ReplaceItemBarcodesAsync (ExecuteDelete + insert) rather than
            // by mutating the tracked item.Barcodes graph: the latter raised a phantom
            // DbUpdateConcurrencyException ("expected 1 row, affected 0") when the rows had
            // been loaded as part of the heavy AsSplitQuery graph.
            List<Inventory.Domain.Entities.ItemBarcode>? barcodesToPersist = null;
            if (dto.Barcodes != null)
            {
                var incoming = dto.Barcodes
                    .Where(b => !string.IsNullOrWhiteSpace(b.Barcode))
                    .ToList();

                // Reject barcodes already owned by a different item.
                foreach (var b in incoming)
                {
                    var owner = await _itemRepository.GetByBarcodeAsync(b.Barcode);
                    if (owner != null && owner.Id != item.Id)
                        return BadRequest(new ApiErrorResponse
                        {
                            Message = $"Barcode '{b.Barcode}' is already assigned to item '{owner.Code} – {owner.Name}'"
                        });
                }

                // Exactly one primary: the explicitly-flagged barcode, else the first row.
                var primaryValue = incoming.FirstOrDefault(b => b.IsPrimary)?.Barcode
                    ?? incoming.FirstOrDefault()?.Barcode;

                barcodesToPersist = incoming.Select(b => new Inventory.Domain.Entities.ItemBarcode
                {
                    CompanyId = item.CompanyId,
                    BranchId = item.BranchId,
                    BusinessUnitId = item.BusinessUnitId,
                    UnitId = b.UnitId ?? item.BaseUnitId,
                    Barcode = b.Barcode,
                    BarcodeType = b.BarcodeType,
                    IsPrimary = b.Barcode == primaryValue,
                }).ToList();
            }

            await _itemRepository.SaveChangesAsync();

            // Reload read-only so the response reflects the freshly-replaced barcodes
            // (the tracked item.Barcodes navigation is stale after the set-based replace).
            ItemDto responseDto;
            if (barcodesToPersist != null)
            {
                await _itemRepository.ReplaceItemBarcodesAsync(item.Id, barcodesToPersist);
                var refreshed = await _itemRepository.GetWithFullDetailsReadOnlyAsync(item.Id);
                responseDto = MapToDtoWithSas(refreshed ?? item);
            }
            else
            {
                responseDto = MapToDtoWithSas(item);
            }

            return Ok(new ApiResponse<ItemDto>
            {
                Success = true,
                Data = responseDto,
                Message = "Item updated successfully"
            });
        }
        catch (DbUpdateConcurrencyException ex)
        {
            // Surface exactly which entity tripped the affected-rows check.
            var details = ex.Entries.Select(e =>
                $"{e.Metadata.Name}[{e.State}] Id={e.Property("Id").CurrentValue}");
            _logger.LogError(ex, "Concurrency updating item {ItemId}. Offending entries: {Entries}",
                id, string.Join("; ", details));
            return StatusCode(500, new ApiErrorResponse { Message = "Error updating item", Errors = GetErrorDetail(ex) });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating item {ItemId}", id);
            return StatusCode(500, new ApiErrorResponse { Message = "Error updating item", Errors = GetErrorDetail(ex) });
        }
    }

    /// <summary>
    /// Upload one or more images for an item. Bytes are stored in the database and each
    /// image is persisted with a relative URL (/api/inventory/images/{id}) that serves it.
    ///
    /// Accepts multipart/form-data with one or more files under the key "files".
    /// Optional form fields: altText, displayOrder, isPrimary.
    /// Returns the created ItemImageDto records with their image URLs.
    /// </summary>
    [HttpPost("{id}/images/upload")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ApiResponse<List<ItemImageDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UploadImages(
        Guid id,
        [FromForm] IFormFileCollection files,
        [FromForm] string? altText = null,
        [FromForm] int displayOrder = 0,
        [FromForm] bool isPrimary = false,
        CancellationToken ct = default)
    {
        try
        {
            if (files == null || files.Count == 0)
                return BadRequest(new ApiErrorResponse { Message = "No files provided." });

            if (!await _itemRepository.ExistsAsync(id))
                return NotFound(new ApiErrorResponse { Message = "Item not found." });

            // ── Resolve tenant context from JWT claims ────────────────────────
            var companyId = Guid.Parse(User.FindFirstValue("CompanyId") ?? Guid.Empty.ToString());
            var branchId = Guid.Parse(User.FindFirstValue("BranchId") ?? Guid.Empty.ToString());
            var businessUnitId = Guid.TryParse(User.FindFirstValue("BusinessUnitId"), out var buId)
                                 ? buId : (Guid?)null;

            var uploaded = new List<ItemImageDto>();
            var order = displayOrder;

            foreach (var file in files)
            {
                // Validate content type — images only
                if (!file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning(
                        "Skipping non-image file {FileName} (ContentType:{ContentType})",
                        file.FileName, file.ContentType);
                    continue;
                }

                // Store image bytes in the database; returns /api/inventory/images/{id}
                await using var stream = file.OpenReadStream();
                var url = await _blobStorage.UploadInventoryImageAsync(
                    stream,
                    file.FileName,
                    file.ContentType,
                    companyId,
                    branchId,
                    businessUnitId,
                    ct);

                // If this file should be primary, demote any existing primary first
                // to avoid violating IX_ItemImage_Item_Primary unique index.
                var thisIsPrimary = isPrimary && uploaded.Count == 0;
                if (thisIsPrimary)
                    await _itemRepository.ClearPrimaryFlagAsync(id);

                // Persist ItemImage record directly — no parent Item tracking involved
                var image = new Inventory.Domain.Entities.ItemImage
                {
                    ItemId = id,
                    Url = url,
                    Resolution = Inventory.Domain.Constants.ImageResolution.Original,
                    FileSizeBytes = file.Length,
                    ContentType = file.ContentType,
                    AltText = altText ?? file.FileName,
                    DisplayOrder = order++,
                    IsPrimary = thisIsPrimary,
                    CompanyId = companyId,
                    BranchId = branchId,
                    BusinessUnitId = businessUnitId ?? Guid.Empty,
                };

                await _itemRepository.AddImageAsync(image);
                uploaded.Add(new ItemImageDto
                {
                    Id = image.Id,
                    ItemId = id,
                    Url = image.Url,
                    Resolution = image.Resolution,
                    FileSizeBytes = image.FileSizeBytes,
                    ContentType = image.ContentType,
                    AltText = image.AltText,
                    DisplayOrder = image.DisplayOrder,
                    IsPrimary = image.IsPrimary,
                });
            }

            if (uploaded.Count == 0)
                return BadRequest(new ApiErrorResponse
                { Message = "No valid image files were uploaded." });

            await _itemRepository.SaveChangesAsync();

            foreach (var img in uploaded)
                img.Url = _blobStorage.GenerateSasUrl(img.Url ?? string.Empty);

            return Ok(new ApiResponse<List<ItemImageDto>>
            {
                Success = true,
                Data = uploaded,
                Message = $"{uploaded.Count} image(s) uploaded successfully.",
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading images for item {ItemId}", id);
            return StatusCode(500, new ApiErrorResponse { Message = "Error uploading images.", Errors = GetErrorDetail(ex) });
        }
    }

    /// <summary>Delete an image record and remove its stored bytes from the database.</summary>
    [HttpDelete("{id}/images/{imageId}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteImage(Guid id, Guid imageId, CancellationToken ct = default)
    {
        try
        {
            if (!await _itemRepository.ExistsAsync(id))
                return NotFound(new ApiErrorResponse { Message = "Item not found." });

            var image = await _itemRepository.GetImageByIdAsync(imageId);
            if (image == null || image.ItemId != id)
                return NotFound(new ApiErrorResponse { Message = "Image not found." });

            // Delete the stored bytes first (non-blocking on failure)
            await _blobStorage.DeleteAsync(image.Url ?? string.Empty, ct);

            _itemRepository.RemoveImage(image);
            await _itemRepository.SaveChangesAsync();

            return Ok(new ApiResponse<bool>
            {
                Success = true,
                Data = true,
                Message = "Image deleted successfully.",
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting image {ImageId} for item {ItemId}", imageId, id);
            return StatusCode(500, new ApiErrorResponse { Message = "Error deleting image.", Errors = GetErrorDetail(ex) });
        }
    }

    /// <summary>
    /// Walks the full exception chain and returns every message so the client
    /// can see the root cause (e.g. SqlException inside DbUpdateException).
    /// </summary>
    private static List<string> GetErrorDetail(Exception ex)
    {
        var details = new List<string>();
        var current = ex;
        while (current != null)
        {
            details.Add($"{current.GetType().Name}: {current.Message}");
            current = current.InnerException;
        }
        return details;
    }

    private void ApplySasUrls(ItemDto dto)
    {
        if (dto.Images == null) return;
        foreach (var img in dto.Images)
            img.Url = _blobStorage.GenerateSasUrl(img.Url ?? string.Empty);
    }

    private ItemDto MapToDtoWithSas(Inventory.Domain.Entities.Item item)
    {
        var dto = MapToDto(item);
        ApplySasUrls(dto);
        return dto;
    }

    private static ItemDto MapToDto(Inventory.Domain.Entities.Item item) => new()
    {
        // ?? Identity ??????????????????????????????????????????????????????????
        Id = item.Id,
        Code = item.Code,
        Name = item.Name,
        ItemType = item.ItemType,
        ShortDescription = item.ShortDescription,
        Description = item.Description,

        // ?? Classification ????????????????????????????????????????????????????
        CategoryId = item.CategoryId,
        BrandId = item.BrandId,
        DisplayColorId = item.DisplayColorId,
        DisplayColorName = item.DisplayColor?.Name,
        DisplayColorHex = item.DisplayColor?.HexCode,
        BaseUnitId = item.BaseUnitId,
        Condition = item.Condition,
        AgeRestriction = item.AgeRestriction,

        // ?? GL Accounts ???????????????????????????????????????????????????????
        InventoryAccountId = item.InventoryAccountId,
        CogsAccountId = item.CogsAccountId,
        PurchaseAccountId = item.PurchaseAccountId,
        SalesAccountId = item.SalesAccountId,

        // ?? Costing & Tracking ????????????????????????????????????????????????
        CostingMethod = item.CostingMethod,
        TrackingType = item.TrackingType,
        IsBatchTracked = item.IsBatchTracked,
        IsSerialTracked = item.IsSerialTracked,
        HasVariants = item.HasVariants,
        IsComponent = item.IsComponent,

        // ?? Stock Control ?????????????????????????????????????????????????????
        ReorderLevel = item.ReorderLevel,
        MaxStockLevel = item.MaxStockLevel,
        EconomicOrderQuantity = item.EconomicOrderQuantity,
        AlertOnLowStock = item.AlertOnLowStock,
        AlertOnExcessStock = item.AlertOnLowStock,

        // ?? Status ????????????????????????????????????????????????????????????
        IsActive = item.IsActive,
        IsPublished = item.IsPublished,
        IsFeatured = item.IsFeatured,

        // ?? Nested collections ????????????????????????????????????????????????
        Images = item.Images.Select(img => new ItemImageDto
        {
            Id = img.Id,
            ItemId = img.ItemId,
            Url = img.Url,
            Resolution = img.Resolution,
            Width = img.Width,
            Height = img.Height,
            FileSizeBytes = img.FileSizeBytes,
            ContentType = img.ContentType,
            AltText = img.AltText,
            DisplayOrder = img.DisplayOrder,
            IsPrimary = img.IsPrimary,
        }).ToList(),

        Barcodes = item.Barcodes.Select(b => new ItemBarcodeDto
        {
            Id = b.Id,
            ItemId = b.ItemId,
            UnitId = b.UnitId,
            Barcode = b.Barcode,
            BarcodeType = b.BarcodeType,
            IsPrimary = b.IsPrimary,
            IsActive = b.IsActive,
        }).ToList(),

        Prices = item.Prices.Select(p => new ItemPriceDto
        {
            Id = p.Id,
            ItemId = p.ItemId,
            UnitId = p.UnitId,
            PriceList = p.PriceList,
            SalePrice = p.SalePrice,
            MinSalePrice = p.MinSalePrice,
            PurchasePrice = p.PurchasePrice,
            IsTaxInclusive = p.IsTaxInclusive,
            EffectiveTaxRate = p.EffectiveTaxRate,
            SalePriceExcludingTax = p.SalePriceExcludingTax,
            SaleTaxAmount = p.SaleTaxAmount,
            SalePriceIncludingTax = p.SalePriceIncludingTax,
            PurchasePriceExcludingTax = p.PurchasePriceExcludingTax,
            PurchaseTaxAmount = p.PurchaseTaxAmount,
            PurchasePriceIncludingTax = p.PurchasePriceIncludingTax,
            CurrencyCode = p.CurrencyCode,
            ValidFrom = p.ValidFrom,
            ValidTo = p.ValidTo,
            IsActive = p.IsActive,
        }).ToList(),

        Taxes = item.Taxes.Select(t => new ItemTaxDto
        {
            Id = t.Id,
            ItemId = t.ItemId,
            TaxDefinitionId = t.TaxDefinitionId,
            TaxCode = t.TaxDefinition?.Code ?? string.Empty,
            TaxName = t.TaxDefinition?.Name ?? string.Empty,
            TaxType = t.TaxDefinition?.TaxType.ToString() ?? string.Empty,
            IsPercentage = t.TaxDefinition?.IsPercentage ?? true,
            InclusionType = t.TaxDefinition?.InclusionType.ToString() ?? string.Empty,
            OverrideRate = t.OverrideRate,
            EffectiveRate = t.EffectiveRate,
            IsActive = t.IsActive,
        }).ToList(),

        Attributes = item.Attributes.Select(a => new ItemAttributeDto
        {
            Id = a.Id,
            ItemId = a.ItemId,
            AttributeDefinitionId = a.AttributeDefinitionId,
            AttributeName = a.AttributeDefinition?.Name ?? string.Empty,
            DataType = a.AttributeDefinition?.DataType ?? string.Empty,
            Value = a.Value,
        }).ToList(),

        Comments = item.Comments.Select(c => new ItemCommentDto
        {
            Id = c.Id,
            ItemId = c.ItemId,
            CommentType = c.CommentType,
            Comment = c.Comment,
            AuthorUserId = c.CreatedByUserId,
            IsPinned = c.IsPinned,
            CreatedAt = c.CreatedAt,
        }).ToList(),

        Colors = item.Colors.Select(ic => new ColorDto
        {
            Id = ic.Color?.Id ?? Guid.Empty,
            Code = ic.Color?.Code ?? string.Empty,
            Name = ic.Color?.Name ?? string.Empty,
            HexCode = ic.Color?.HexCode,
            R = ic.Color?.R,
            G = ic.Color?.G,
            B = ic.Color?.B,
            ColorFamily = ic.Color?.ColorFamily,
            SwatchImageUrl = ic.Color?.SwatchImageUrl,
            DisplayOrder = ic.Color?.DisplayOrder ?? 0,
            IsActive = ic.Color?.IsActive ?? false,
        }).ToList(),

        Sizes = item.Sizes.Select(s => new ItemSizeDto
        {
            Id = s.Id,
            ItemId = s.ItemId,
            SizeId = s.SizeId,
            SizeCode = s.Size?.Code ?? string.Empty,
            SizeName = s.Size?.Name ?? string.Empty,
            ItemBarcodeId = s.ItemBarcodeId,
            IsDefault = s.IsDefault,
            IsAvailable = s.IsAvailable,
        }).ToList(),

        Variants = item.Variants.Select(v => new ItemVariantDto
        {
            Id = v.Id,
            ItemId = v.ItemId,
            VariantCode = v.VariantCode,
            VariantName = v.VariantName,
            ColorId = v.ColorId,
            ColorName = v.Color?.Name,
            ColorHex = v.Color?.HexCode,
            SizeId = v.SizeId,
            SizeName = v.Size?.Name,
            ExtraDimension = v.ExtraDimension,
            Barcode = v.Barcode,
            ImageUrl = v.ImageUrl,
            SalePriceOverride = v.SalePriceOverride,
            PurchasePriceOverride = v.PurchasePriceOverride,
            WeightKg = v.WeightKg,
            IsActive = v.IsActive,
            DisplayOrder = v.DisplayOrder,
        }).ToList(),

        Shipping = item.Shipping == null ? null : new ItemShippingDto
        {
            Id = item.Shipping.Id,
            ItemId = item.Shipping.ItemId,
            WeightKg = item.Shipping.WeightKg,
            LengthCm = item.Shipping.LengthCm,
            WidthCm = item.Shipping.WidthCm,
            HeightCm = item.Shipping.HeightCm,
            VolumetricWeightKg = item.Shipping.VolumetricWeightKg,
            CountryOfOrigin = item.Shipping.CountryOfOrigin,
            HsCode = item.Shipping.HsCode,
            UnitsPerCarton = item.Shipping.UnitsPerCarton,
            CartonsPerPallet = item.Shipping.CartonsPerPallet,
            CartonWeightKg = item.Shipping.CartonWeightKg,
            CartonLengthCm = item.Shipping.CartonLengthCm,
            CartonWidthCm = item.Shipping.CartonWidthCm,
            CartonHeightCm = item.Shipping.CartonHeightCm,
            RequiresSpecialHandling = item.Shipping.RequiresSpecialHandling,
            HandlingNotes = item.Shipping.HandlingNotes,
            IsHazmat = item.Shipping.IsHazmat,
            IsShippableInternational = item.Shipping.IsShippableInternational,
        },

        Seo = item.Seo == null ? null : new ItemSeoDto
        {
            Id = item.Seo.Id,
            ItemId = item.Seo.ItemId,
            MetaTitle = item.Seo.MetaTitle,
            MetaDescription = item.Seo.MetaDescription,
            MetaKeywords = item.Seo.MetaKeywords,
            Slug = item.Seo.Slug,
            CanonicalUrl = item.Seo.CanonicalUrl,
        },

        Warranty = item.Warranty == null ? null : new ItemWarrantyDto
        {
            Id = item.Warranty.Id,
            ItemId = item.Warranty.ItemId,
            WarrantyType = item.Warranty.WarrantyType,
            DurationMonths = item.Warranty.DurationMonths,
            PolicyDescription = item.Warranty.PolicyDescription,
            ProviderName = item.Warranty.ProviderName,
            ProviderContact = item.Warranty.ProviderContact,
            IsActive = item.Warranty.IsActive,
        },

        ChannelListings = item.ChannelListings.Select(cl => new ItemChannelListingDto
        {
            Id = cl.Id,
            ItemId = cl.ItemId,
            Channel = cl.Channel,
            ListingStatus = cl.ListingStatus,
            ChannelTitle = cl.ChannelTitle,
            ChannelDescription = cl.ChannelDescription,
            ChannelPrice = cl.ChannelPrice,
            ChannelDiscount = cl.ChannelDiscount,
            DiscountType = cl.DiscountType,
            ExternalProductId = cl.ExternalProductId,
            ExternalSku = cl.ExternalSku,
            ListingUrl = cl.ListingUrl,
            ListedAt = cl.ListedAt,
            LastSyncedAt = cl.LastSyncedAt,
            AutoSyncStock = cl.AutoSyncStock,
            AutoSyncPrice = cl.AutoSyncPrice,
        }).ToList(),

        Suppliers = item.Suppliers.Select(s => new ItemSupplierDto
        {
            Id = s.Id,
            ItemId = s.ItemId,
            SupplierId = s.SupplierId,
            SupplierItemCode = s.SupplierItemCode,
            SupplierItemName = s.SupplierItemName,
            LastPurchasePrice = s.LastPurchasePrice,
            CurrencyCode = s.CurrencyCode,
            MinOrderQuantity = s.MinOrderQuantity,
            LeadTimeDays = s.LeadTimeDays,
            IsPrimary = s.IsPrimary,
            IsActive = s.IsActive,
            LastPurchaseDate = s.LastPurchaseDate,
        }).ToList(),

        BundleComponents = item.BundleComponents.Select(b => new ItemBundleDto
        {
            Id = b.Id,
            BundleItemId = b.BundleItemId,
            ComponentItemId = b.ComponentItemId,
            ComponentItemCode = b.ComponentItem?.Code ?? string.Empty,
            ComponentItemName = b.ComponentItem?.Name ?? string.Empty,
            Quantity = b.Quantity,
            UnitId = b.UnitId,
            UnitCode = b.Unit?.Code ?? string.Empty,
            IsIncludedInCost = b.IsIncludedInCost,
            DisplayOrder = b.DisplayOrder,
        }).ToList(),

        Substitutions = item.Substitutions.Select(s => new ItemSubstitutionDto
        {
            Id = s.Id,
            ItemId = s.ItemId,
            SubstituteItemId = s.SubstituteItemId,
            SubstituteItemCode = s.SubstituteItem?.Code ?? string.Empty,
            SubstituteItemName = s.SubstituteItem?.Name ?? string.Empty,
            Priority = s.Priority,
            Note = s.Note,
            IsBidirectional = s.IsBidirectional,
            IsActive = s.IsActive,
        }).ToList(),

        Discounts = item.Discounts.Select(d => new ItemDiscountDto
        {
            Id = d.Id,
            ItemId = d.ItemId,
            Name = d.Name,
            DiscountType = d.DiscountType,
            DiscountValue = d.DiscountValue,
            BuyQuantity = d.BuyQuantity,
            GetQuantity = d.GetQuantity,
            MinQuantity = d.MinQuantity,
            MaxQuantity = d.MaxQuantity,
            ApplicableChannels = d.ApplicableChannels,
            ValidFrom = d.ValidFrom,
            ValidTo = d.ValidTo,
            IsActive = d.IsActive,
            Priority = d.Priority,
        }).ToList(),
    };

    private ItemBasicDto MapToBasicDto(Inventory.Domain.Entities.Item item)
    {
        var primaryBarcode = item.Barcodes.FirstOrDefault(b => b.IsPrimary)?.Barcode;
        var defaultPrice = item.Prices.FirstOrDefault(p => p.PriceList == "Default");

        return new ItemBasicDto
        {
            Id = item.Id,
            Code = item.Code,
            Name = item.Name,
            ItemType = item.ItemType,
            ShortDescription = item.ShortDescription,
            Barcode = primaryBarcode,
            SalePrice = defaultPrice?.SalePrice,
            PurchasePrice = defaultPrice?.PurchasePrice,
            CategoryId = item.CategoryId,
            CategoryName = item.Category?.Name,
            BrandId = item.BrandId,
            BrandName = item.Brand?.Name,
            BaseUnitId = item.BaseUnitId,
            BaseUnitCode = item.BaseUnit?.Code,
            BaseUnitName = item.BaseUnit?.Name,
            IsActive = item.IsActive,
            TrackingType = item.TrackingType,
            IsBatchTracked = item.IsBatchTracked,
            IsSerialTracked = item.IsSerialTracked,
        };
    }

    /// <summary>
    /// Normalizes the unit-level tracking mode. TrackingType (None|Lot|Serial) is the source of truth;
    /// when it is not supplied we derive it from the legacy booleans (Serial wins over Lot). The returned
    /// booleans are always kept in sync with the resolved TrackingType.
    /// </summary>
    private static (string TrackingType, bool IsBatchTracked, bool IsSerialTracked) NormalizeTracking(
        string? trackingType, bool isSerialTracked, bool isBatchTracked)
    {
        var tt = (trackingType ?? "").Trim();
        if (!Inventory.Domain.Constants.ItemTrackingType.All.Contains(tt))
            tt = isSerialTracked ? Inventory.Domain.Constants.ItemTrackingType.Serial
               : isBatchTracked  ? Inventory.Domain.Constants.ItemTrackingType.Lot
               : Inventory.Domain.Constants.ItemTrackingType.None;

        return (tt,
                IsBatchTracked: tt == Inventory.Domain.Constants.ItemTrackingType.Lot,
                IsSerialTracked: tt == Inventory.Domain.Constants.ItemTrackingType.Serial);
    }
}



