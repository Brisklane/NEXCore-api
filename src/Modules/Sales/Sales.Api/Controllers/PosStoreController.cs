using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nexcore.SharedKernel.Api;
using Nexcore.SharedKernel.Events;
using Nexcore.SharedKernel.ValueObjects;
using Sales.Application.DTOs;
using Sales.Domain.Entities;
using Sales.Domain.Enums;
using Sales.Infrastructure.Repositories.Interfaces;
using System.Net;
using System.Text.Json;

namespace Sales.Api.Controllers;

[ApiController]
[Route("api/sales/[controller]")]
[Authorize]
public class PosStoreController : ControllerBase
{
    private readonly IPosStoreRepository _stores;
    private readonly IStoreMenuRepository _menus;
    private readonly IEventPublisher _events;
    private readonly IHttpClientFactory _http;
    private readonly ILogger<PosStoreController> _logger;

    public PosStoreController(
        IPosStoreRepository stores,
        IStoreMenuRepository menus,
        IEventPublisher events,
        IHttpClientFactory http,
        ILogger<PosStoreController> logger)
    {
        _stores = stores;
        _menus = menus;
        _events = events;
        _http = http;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            var stores = await _stores.GetAllAsync();
            return Ok(new ApiResponse<List<PosStoreDto>>
            {
                Success = true,
                Data = stores.Where(s => !s.IsDeleted).OrderBy(s => s.CodeInt).Select(MapToDto).ToList(),
                Message = "POS stores retrieved"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving POS stores");
            return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving POS stores" });
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        try
        {
            var store = await _stores.GetByIdAsync(id);
            if (store == null || store.IsDeleted)
                return NotFound(new ApiErrorResponse { Message = "POS store not found" });
            return Ok(new ApiResponse<PosStoreDto> { Success = true, Data = MapToDto(store), Message = "POS store retrieved" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving POS store {Id}", id);
            return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving POS store" });
        }
    }

    [HttpGet("active")]
    public async Task<IActionResult> GetActive()
    {
        try
        {
            var stores = (await _stores.FindAsync(s => s.IsActive && !s.IsDeleted)).OrderBy(s => s.CodeInt);
            return Ok(new ApiResponse<List<PosStoreDto>>
            {
                Success = true,
                Data = stores.Select(MapToDto).ToList(),
                Message = "Active POS stores retrieved"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving active POS stores");
            return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving active POS stores" });
        }
    }

    [HttpGet("online-enabled")]
    public async Task<IActionResult> GetOnlineEnabled()
    {
        try
        {
            var stores = await _stores.GetOnlineEnabledAsync();
            return Ok(new ApiResponse<List<PosStoreDto>>
            {
                Success = true,
                Data = stores.Select(MapToDto).ToList(),
                Message = "Online-enabled POS stores retrieved"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving online-enabled POS stores");
            return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving online-enabled POS stores" });
        }
    }

    /// <summary>Get the active store menu for a store — used by customer app.</summary>
    [HttpGet("{id}/menu")]
    [AllowAnonymous]
    public async Task<IActionResult> GetMenu(Guid id)
    {
        try
        {
            var menu = await _menus.GetActiveMenuAsync(id);
            if (menu == null)
                return NotFound(new ApiErrorResponse { Message = "No active menu found for this store" });
            return Ok(new ApiResponse<StoreMenuDto> { Success = true, Data = MapMenuToDto(menu), Message = "Store menu retrieved" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving menu for store {Id}", id);
            return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving menu" });
        }
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePosStoreDto dto)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(dto.TradingName))
                return BadRequest(new ApiErrorResponse { Message = "Store name (TradingName) is required" });

            if (await _stores.TradingNameExistsAsync(dto.TradingName))
                return Conflict(new ApiErrorResponse { Message = $"A POS store named '{dto.TradingName.Trim()}' already exists in this company" });

            // Resolve city code for the auto-generated store code
            string citySegment = "GEN";
            if (dto.CityId.HasValue)
            {
                var cityLookup = new CityCodeLookupEvent { CityId = dto.CityId.Value };
                await _events.PublishAsync(cityLookup);
                citySegment = await cityLookup.Result.Task ?? "GEN";
            }

            // Next sequence number — direct MAX query for atomic code generation
            var nextCodeInt = await _stores.GetMaxCodeIntAsync() + 1;

            var generatedCode = $"POS-{citySegment}-{nextCodeInt:D6}";

            var store = new PosStore
            {
                Code = generatedCode,
                CodeInt = nextCodeInt,
                CityId = dto.CityId,
                CountryCode = dto.CountryCode ?? await DetectCountryFromIpAsync(),
                TradingName = dto.TradingName,
                NativeLanguageId = dto.NativeLanguageId,
                StoreType = dto.StoreType,
                StoreFormat = dto.StoreFormat,
                DefaultWarehouseId = dto.DefaultWarehouseId,
                DefaultPriceListId = dto.DefaultPriceListId,
                AcceptsOnlinePickup = dto.AcceptsOnlinePickup,
                HasDelivery = dto.HasDelivery,
                IsOnlineOrderingEnabled = dto.IsOnlineOrderingEnabled,
                EstimatedPrepTimeMinutes = dto.EstimatedPrepTimeMinutes,
                MinOnlineOrderAmount = dto.MinOnlineOrderAmount,
                MaxDeliveryRadiusKm = dto.MaxDeliveryRadiusKm,
                OnlineLogoUrl = dto.OnlineLogoUrl,
                OnlineBannerUrl = dto.OnlineBannerUrl,
                Location = (dto.Latitude.HasValue || dto.Longitude.HasValue)
                    ? new GeoCoordinate(dto.Latitude, dto.Longitude) : null,
                IsActive = true,
                OnlineStatus = StoreOnlineStatus.Closed,
            };

            await _stores.AddAsync(store);
            await _stores.SaveChangesAsync();
            return CreatedAtAction(nameof(GetById), new { id = store.Id },
                new ApiResponse<PosStoreDto> { Success = true, Data = MapToDto(store), Message = $"POS store created with code {generatedCode}" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating POS store");
            return StatusCode(500, new ApiErrorResponse { Message = "Error creating POS store" });
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdatePosStoreDto dto)
    {
        try
        {
            var store = await _stores.GetByIdAsync(id);
            if (store == null || store.IsDeleted)
                return NotFound(new ApiErrorResponse { Message = "POS store not found" });

            if (dto.TradingName != null
                && !string.Equals(dto.TradingName.Trim(), store.TradingName?.Trim(), StringComparison.OrdinalIgnoreCase)
                && await _stores.TradingNameExistsAsync(dto.TradingName, excludeStoreId: id))
                return Conflict(new ApiErrorResponse { Message = $"A POS store named '{dto.TradingName.Trim()}' already exists in this company" });

            if (dto.TradingName != null) store.TradingName = dto.TradingName;
            if (dto.NativeLanguageId.HasValue) store.NativeLanguageId = dto.NativeLanguageId;
            if (dto.StoreType.HasValue) store.StoreType = dto.StoreType.Value;
            if (dto.StoreFormat.HasValue) store.StoreFormat = dto.StoreFormat.Value;
            // Defaults panel is a full editor: persist the picked warehouse / price list,
            // including clearing back to "— None —" (null). Without this the edit silently
            // reverted — the POS kept deducting from the original warehouse.
            store.DefaultWarehouseId = dto.DefaultWarehouseId;
            store.DefaultPriceListId = dto.DefaultPriceListId;
            if (dto.OnlineStatus.HasValue) store.OnlineStatus = dto.OnlineStatus.Value;
            if (dto.OnlineStatusNote != null) store.OnlineStatusNote = dto.OnlineStatusNote;
            if (dto.IsOnlineOrderingEnabled.HasValue) store.IsOnlineOrderingEnabled = dto.IsOnlineOrderingEnabled.Value;
            if (dto.EstimatedPrepTimeMinutes.HasValue) store.EstimatedPrepTimeMinutes = dto.EstimatedPrepTimeMinutes.Value;
            if (dto.MinOnlineOrderAmount.HasValue) store.MinOnlineOrderAmount = dto.MinOnlineOrderAmount.Value;
            if (dto.MaxDeliveryRadiusKm.HasValue) store.MaxDeliveryRadiusKm = dto.MaxDeliveryRadiusKm.Value;
            if (dto.Latitude.HasValue || dto.Longitude.HasValue)
                store.Location = new GeoCoordinate(dto.Latitude, dto.Longitude);
            if (dto.IsActive.HasValue) store.IsActive = dto.IsActive.Value;

            _stores.Update(store);
            await _stores.SaveChangesAsync();
            return Ok(new ApiResponse<PosStoreDto> { Success = true, Data = MapToDto(store), Message = "POS store updated" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating POS store {Id}", id);
            return StatusCode(500, new ApiErrorResponse { Message = "Error updating POS store" });
        }
    }

    /// <summary>Set real-time online status (Open / Busy / Closed) with an optional note.</summary>
    [HttpPut("{id}/online-status")]
    public async Task<IActionResult> SetOnlineStatus(Guid id, [FromBody] SetStoreOnlineStatusDto dto)
    {
        try
        {
            var store = await _stores.GetByIdAsync(id);
            if (store == null || store.IsDeleted)
                return NotFound(new ApiErrorResponse { Message = "POS store not found" });

            store.OnlineStatus = dto.OnlineStatus;
            store.OnlineStatusNote = dto.Note;
            _stores.Update(store);
            await _stores.SaveChangesAsync();
            return Ok(new ApiResponse<PosStoreDto> { Success = true, Data = MapToDto(store), Message = "Online status updated" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating online status for store {Id}", id);
            return StatusCode(500, new ApiErrorResponse { Message = "Error updating online status" });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            var store = await _stores.GetByIdAsync(id);
            if (store == null || store.IsDeleted)
                return NotFound(new ApiErrorResponse { Message = "POS store not found" });

            if (store.Terminals.Any(t => !t.IsDeleted))
                return Conflict(new ApiErrorResponse { Message = "Cannot delete a store that has active terminals" });

            _stores.SoftDelete(store);
            await _stores.SaveChangesAsync();
            return Ok(new ApiResponse<object> { Success = true, Message = "POS store deleted" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting POS store {Id}", id);
            return StatusCode(500, new ApiErrorResponse { Message = "Error deleting POS store" });
        }
    }

    /// <summary>
    /// Find active stores within a given radius of the user's location.
    /// Returns results sorted by distance ascending.
    /// Only stores that have GPS coordinates set are included.
    /// </summary>
    [HttpGet("nearby")]
    [AllowAnonymous]
    public async Task<IActionResult> GetNearby(
        [FromQuery] double lat,
        [FromQuery] double lng,
        [FromQuery] double radiusKm = 10)
    {
        try
        {
            if (radiusKm <= 0 || radiusKm > 500)
                return BadRequest(new ApiErrorResponse { Message = "radiusKm must be between 1 and 500" });

            var stores = await _stores.GetAllActiveWithLocationAsync();

            var results = stores
                .Select(s => (store: s, km: Haversine(lat, lng, (double)s.Location!.Latitude!, (double)s.Location!.Longitude!)))
                .Where(x => x.km <= radiusKm)
                .OrderBy(x => x.km)
                .Select(x => new PosStoreNearbyDto
                {
                    Id = x.store.Id,
                    BranchId = x.store.BranchId,
                    Code = x.store.Code,
                    CodeInt = x.store.CodeInt,
                    CityId = x.store.CityId,
                    CountryCode = x.store.CountryCode,
                    TradingName = x.store.TradingName,
                    NativeLanguageId = x.store.NativeLanguageId,
                    StoreType = x.store.StoreType,
                    DefaultWarehouseId = x.store.DefaultWarehouseId,
                    DefaultPriceListId = x.store.DefaultPriceListId,
                    AcceptsOnlinePickup = x.store.AcceptsOnlinePickup,
                    HasDelivery = x.store.HasDelivery,
                    IsOnlineOrderingEnabled = x.store.IsOnlineOrderingEnabled,
                    OnlineStatus = x.store.OnlineStatus,
                    OnlineStatusNote = x.store.OnlineStatusNote,
                    EstimatedPrepTimeMinutes = x.store.EstimatedPrepTimeMinutes,
                    MinOnlineOrderAmount = x.store.MinOnlineOrderAmount,
                    MaxDeliveryRadiusKm = x.store.MaxDeliveryRadiusKm,
                    OnlineLogoUrl = x.store.OnlineLogoUrl,
                    OnlineBannerUrl = x.store.OnlineBannerUrl,
                    Latitude = x.store.Location?.Latitude,
                    Longitude = x.store.Location?.Longitude,
                    IsActive = x.store.IsActive,
                    DistanceKm = Math.Round(x.km, 2),
                })
                .ToList();

            return Ok(new ApiResponse<List<PosStoreNearbyDto>>
            {
                Success = true,
                Data = results,
                Message = $"{results.Count} store(s) found within {radiusKm} km"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error finding nearby stores");
            return StatusCode(500, new ApiErrorResponse { Message = "Error finding nearby stores" });
        }
    }

    // Haversine great-circle distance in kilometres
    private static double Haversine(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371.0;
        var dLat = ToRad(lat2 - lat1);
        var dLon = ToRad(lon2 - lon1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
              + Math.Cos(ToRad(lat1)) * Math.Cos(ToRad(lat2))
              * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        return R * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
    }

    private static double ToRad(double deg) => deg * Math.PI / 180.0;

    private static PosStoreDto MapToDto(PosStore s) => new()
    {
        Id = s.Id,
        BranchId = s.BranchId,
        Code = s.Code,
        CodeInt = s.CodeInt,
        CityId = s.CityId,
        CountryCode = s.CountryCode,
        TradingName = s.TradingName,
        NativeLanguageId = s.NativeLanguageId,
        StoreType = s.StoreType,
        StoreFormat = s.StoreFormat,
        DefaultWarehouseId = s.DefaultWarehouseId,
        DefaultPriceListId = s.DefaultPriceListId,
        AcceptsOnlinePickup = s.AcceptsOnlinePickup,
        HasDelivery = s.HasDelivery,
        IsOnlineOrderingEnabled = s.IsOnlineOrderingEnabled,
        OnlineStatus = s.OnlineStatus,
        OnlineStatusNote = s.OnlineStatusNote,
        EstimatedPrepTimeMinutes = s.EstimatedPrepTimeMinutes,
        MinOnlineOrderAmount = s.MinOnlineOrderAmount,
        MaxDeliveryRadiusKm = s.MaxDeliveryRadiusKm,
        OnlineLogoUrl = s.OnlineLogoUrl,
        OnlineBannerUrl = s.OnlineBannerUrl,
        Latitude = s.Location?.Latitude,
        Longitude = s.Location?.Longitude,
        IsActive = s.IsActive,
    };

    /// <summary>Detect the ISO alpha-2 country code for the requesting IP — used by the frontend to pre-populate the country field.</summary>
    [HttpGet("detect-country")]
    [AllowAnonymous]
    public async Task<IActionResult> DetectCountry()
    {
        var country = await DetectCountryFromIpAsync();
        return Ok(new ApiResponse<string?>
        {
            Success = true,
            Data = country,
            Message = country != null ? $"Detected country: {country}" : "Country not detected (local/private IP)"
        });
    }

    private async Task<string?> DetectCountryFromIpAsync()
    {
        try
        {
            var ip = HttpContext.Connection.RemoteIpAddress;
            if (ip == null) return null;
            if (ip.IsIPv4MappedToIPv6) ip = ip.MapToIPv4();
            if (IPAddress.IsLoopback(ip) || IsPrivateIp(ip)) return null;

            var client = _http.CreateClient();
            var json = await client.GetStringAsync($"http://ip-api.com/json/{ip}?fields=status,countryCode");
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            return root.GetProperty("status").GetString() == "success"
                ? root.GetProperty("countryCode").GetString()
                : null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "IP country detection failed");
            return null;
        }
    }

    private static bool IsPrivateIp(IPAddress ip)
    {
        var b = ip.GetAddressBytes();
        return b[0] == 10
            || (b[0] == 172 && b[1] >= 16 && b[1] <= 31)
            || (b[0] == 192 && b[1] == 168);
    }

    private static StoreMenuDto MapMenuToDto(Sales.Domain.Entities.StoreMenu m) => new()
    {
        Id = m.Id,
        BranchId = m.BranchId,
        Name = m.Name,
        Description = m.Description,
        ImageUrl = m.ImageUrl,
        DisplayOrder = m.DisplayOrder,
        IsActive = m.IsActive,
        AvailableFrom = m.AvailableFrom,
        AvailableTo = m.AvailableTo,
        Sections = m.Sections.Select(s => new StoreMenuSectionDto
        {
            Id = s.Id,
            ItemCategoryId = s.ItemCategoryId,
            DisplayOrder = s.DisplayOrder,
            IsActive = s.IsActive,
        }).ToList(),
    };
}
