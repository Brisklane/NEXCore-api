using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nexcore.SharedKernel.Api;
using Sales.Application.DTOs;
using Sales.Domain.Entities;
using Sales.Domain.Enums;
using Sales.Infrastructure.Repositories.Interfaces;
using System.Text.Json;

namespace Sales.Api.Controllers;

/// <summary>
/// Manages the KYC / onboarding profile for marketplace sellers (home chefs, restaurants).
/// Each profile is linked 1:1 to a PosStore.
///
/// Seller flow:  PUT /{storeId}/vendor-profile  →  POST /{storeId}/vendor-profile/submit
/// Review flow:  GET /pending  →  POST /{storeId}/vendor-profile/review
/// </summary>
[ApiController]
[Route("api/sales/PosStore/{storeId:guid}/vendor-profile")]
[Authorize]
public class StoreVendorProfileController : ControllerBase
{
    private readonly IStoreVendorProfileRepository _profiles;
    private readonly IPosStoreRepository _stores;
    private readonly ILogger<StoreVendorProfileController> _logger;

    public StoreVendorProfileController(
        IStoreVendorProfileRepository profiles,
        IPosStoreRepository stores,
        ILogger<StoreVendorProfileController> logger)
    {
        _profiles = profiles;
        _stores = stores;
        _logger = logger;
    }

    // ── Seller endpoints ──────────────────────────────────────────────────────

    /// <summary>Get the vendor profile for a store.</summary>
    [HttpGet]
    public async Task<IActionResult> Get(Guid storeId)
    {
        try
        {
            var profile = await _profiles.GetByStoreAsync(storeId);
            if (profile == null)
                return NotFound(new ApiErrorResponse { Message = "No vendor profile found for this store" });
            return Ok(new ApiResponse<StoreVendorProfileDto> { Success = true, Data = MapToDto(profile), Message = "Vendor profile retrieved" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving vendor profile for store {StoreId}", storeId);
            return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving vendor profile" });
        }
    }

    /// <summary>
    /// Create or update the vendor profile (seller saves their details).
    /// Can be called multiple times while status is Draft or Rejected.
    /// </summary>
    [HttpPut]
    public async Task<IActionResult> Upsert(Guid storeId, [FromBody] UpsertVendorProfileDto dto)
    {
        try
        {
            var store = await _stores.GetByIdAsync(storeId);
            if (store == null || store.IsDeleted)
                return NotFound(new ApiErrorResponse { Message = "Store not found" });

            var existing = await _profiles.GetByStoreAsync(storeId);
            var photosJson = dto.KitchenPhotos.Count > 0
                ? JsonSerializer.Serialize(dto.KitchenPhotos) : null;

            if (existing != null)
            {
                if (existing.OnboardingStatus is VendorOnboardingStatus.Submitted or
                    VendorOnboardingStatus.UnderReview or VendorOnboardingStatus.Approved)
                    return Conflict(new ApiErrorResponse { Message = $"Profile cannot be edited in status '{existing.OnboardingStatus}'" });

                ApplyUpsert(existing, dto, photosJson);
                _profiles.Update(existing);
                await _profiles.SaveChangesAsync();
                return Ok(new ApiResponse<StoreVendorProfileDto> { Success = true, Data = MapToDto(existing), Message = "Vendor profile updated" });
            }

            var profile = new StoreVendorProfile { StoreId = storeId };
            ApplyUpsert(profile, dto, photosJson);
            await _profiles.AddAsync(profile);
            await _profiles.SaveChangesAsync();
            return CreatedAtAction(nameof(Get), new { storeId },
                new ApiResponse<StoreVendorProfileDto> { Success = true, Data = MapToDto(profile), Message = "Vendor profile created" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error upserting vendor profile for store {StoreId}", storeId);
            return StatusCode(500, new ApiErrorResponse { Message = "Error saving vendor profile" });
        }
    }

    /// <summary>
    /// Seller submits their completed profile for platform review.
    /// Transitions status: Draft / Rejected → Submitted.
    /// </summary>
    [HttpPost("submit")]
    public async Task<IActionResult> Submit(Guid storeId)
    {
        try
        {
            var profile = await _profiles.GetByStoreAsync(storeId);
            if (profile == null)
                return NotFound(new ApiErrorResponse { Message = "No vendor profile found. Save your details first." });

            if (profile.OnboardingStatus is not (VendorOnboardingStatus.Draft or VendorOnboardingStatus.Rejected))
                return Conflict(new ApiErrorResponse { Message = $"Cannot submit from status '{profile.OnboardingStatus}'" });

            if (string.IsNullOrWhiteSpace(profile.OwnerCnic))
                return BadRequest(new ApiErrorResponse { Message = "CNIC is required before submitting" });
            if (string.IsNullOrWhiteSpace(profile.AccountNumber) && string.IsNullOrWhiteSpace(profile.IbanNumber))
                return BadRequest(new ApiErrorResponse { Message = "Bank account number or IBAN is required before submitting" });

            profile.OnboardingStatus = VendorOnboardingStatus.Submitted;
            profile.SubmittedAt = DateTime.UtcNow;
            _profiles.Update(profile);

            // Mirror status on the store for quick filtering
            var store = await _stores.GetByIdAsync(storeId);
            if (store != null)
            {
                store.OnboardingStatus = VendorOnboardingStatus.Submitted;
                _stores.Update(store);
            }

            await _profiles.SaveChangesAsync();
            return Ok(new ApiResponse<StoreVendorProfileDto> { Success = true, Data = MapToDto(profile), Message = "Application submitted for review" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting vendor profile for store {StoreId}", storeId);
            return StatusCode(500, new ApiErrorResponse { Message = "Error submitting application" });
        }
    }

    // ── Platform reviewer endpoints ───────────────────────────────────────────

    /// <summary>
    /// List all profiles pending review (Submitted or UnderReview).
    /// Platform admin use.
    /// </summary>
    [HttpGet("/api/sales/vendor-applications/pending")]
    public async Task<IActionResult> GetPending()
    {
        try
        {
            var submitted = await _profiles.GetByStatusAsync(VendorOnboardingStatus.Submitted);
            var underReview = await _profiles.GetByStatusAsync(VendorOnboardingStatus.UnderReview);
            var all = submitted.Concat(underReview).OrderBy(p => p.SubmittedAt).ToList();
            return Ok(new ApiResponse<List<StoreVendorProfileDto>>
            {
                Success = true,
                Data = all.Select(MapToDto).ToList(),
                Message = $"{all.Count} application(s) pending"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving pending vendor applications");
            return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving applications" });
        }
    }

    /// <summary>
    /// Platform reviewer approves or rejects an application.
    /// On Approved: store OnlineStatus becomes available for seller to set to Open.
    /// On Rejected: seller can correct and resubmit.
    /// </summary>
    [HttpPost("review")]
    public async Task<IActionResult> Review(Guid storeId, [FromBody] ReviewVendorApplicationDto dto)
    {
        try
        {
            if (dto.Decision == VendorOnboardingStatus.Rejected && string.IsNullOrWhiteSpace(dto.RejectionReason))
                return BadRequest(new ApiErrorResponse { Message = "RejectionReason is required when rejecting" });

            var profile = await _profiles.GetByStoreAsync(storeId);
            if (profile == null)
                return NotFound(new ApiErrorResponse { Message = "Vendor profile not found" });

            if (profile.OnboardingStatus is not (VendorOnboardingStatus.Submitted or VendorOnboardingStatus.UnderReview))
                return Conflict(new ApiErrorResponse { Message = $"Cannot review an application in status '{profile.OnboardingStatus}'" });

            var reviewerId = User.FindFirst("UserId")?.Value;
            profile.OnboardingStatus = dto.Decision;
            profile.ReviewedAt = DateTime.UtcNow;
            profile.ReviewedByUserId = Guid.TryParse(reviewerId, out var uid) ? uid : null;
            profile.ReviewNotes = dto.ReviewNotes;
            profile.RejectionReason = dto.Decision == VendorOnboardingStatus.Rejected ? dto.RejectionReason : null;
            _profiles.Update(profile);

            // Mirror status on the store
            var store = await _stores.GetByIdAsync(storeId);
            if (store != null)
            {
                store.OnboardingStatus = dto.Decision;
                // Approved stores become active but stay Closed online until seller opens them
                if (dto.Decision == VendorOnboardingStatus.Approved)
                    store.IsActive = true;
                _stores.Update(store);
            }

            await _profiles.SaveChangesAsync();
            var message = dto.Decision == VendorOnboardingStatus.Approved
                ? "Application approved — store is now live"
                : $"Application {dto.Decision.ToString().ToLower()}";
            return Ok(new ApiResponse<StoreVendorProfileDto> { Success = true, Data = MapToDto(profile), Message = message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reviewing vendor profile for store {StoreId}", storeId);
            return StatusCode(500, new ApiErrorResponse { Message = "Error processing review" });
        }
    }

    // ── Mapping ───────────────────────────────────────────────────────────────

    private static void ApplyUpsert(StoreVendorProfile p, UpsertVendorProfileDto dto, string? photosJson)
    {
        p.OwnerName = dto.OwnerName;
        p.OwnerCnic = dto.OwnerCnic;
        p.OwnerPhone = dto.OwnerPhone;
        p.OwnerEmail = dto.OwnerEmail;
        p.CnicFrontDocUrl = dto.CnicFrontDocUrl;
        p.CnicBackDocUrl = dto.CnicBackDocUrl;
        p.BusinessName = dto.BusinessName;
        p.BusinessRegistrationNumber = dto.BusinessRegistrationNumber;
        p.FoodLicenseNumber = dto.FoodLicenseNumber;
        p.FoodLicenseExpiry = dto.FoodLicenseExpiry;
        p.FoodLicenseDocUrl = dto.FoodLicenseDocUrl;
        p.BusinessDescription = dto.BusinessDescription;
        p.BankName = dto.BankName;
        p.BankBranch = dto.BankBranch;
        p.AccountTitle = dto.AccountTitle;
        p.AccountNumber = dto.AccountNumber;
        p.IbanNumber = dto.IbanNumber;
        p.FacebookUrl = dto.FacebookUrl;
        p.InstagramUrl = dto.InstagramUrl;
        p.TiktokUrl = dto.TiktokUrl;
        p.WhatsappNumber = dto.WhatsappNumber;
        p.StoreFrontPhotoUrl = dto.StoreFrontPhotoUrl;
        p.KitchenPhotosJson = photosJson;
    }

    private static StoreVendorProfileDto MapToDto(StoreVendorProfile p) => new()
    {
        Id = p.Id,
        StoreId = p.StoreId,
        OwnerName = p.OwnerName,
        OwnerCnic = p.OwnerCnic,
        OwnerPhone = p.OwnerPhone,
        OwnerEmail = p.OwnerEmail,
        CnicFrontDocUrl = p.CnicFrontDocUrl,
        CnicBackDocUrl = p.CnicBackDocUrl,
        BusinessName = p.BusinessName,
        BusinessRegistrationNumber = p.BusinessRegistrationNumber,
        FoodLicenseNumber = p.FoodLicenseNumber,
        FoodLicenseExpiry = p.FoodLicenseExpiry,
        FoodLicenseDocUrl = p.FoodLicenseDocUrl,
        BusinessDescription = p.BusinessDescription,
        BankName = p.BankName,
        BankBranch = p.BankBranch,
        AccountTitle = p.AccountTitle,
        AccountNumber = p.AccountNumber,
        IbanNumber = p.IbanNumber,
        FacebookUrl = p.FacebookUrl,
        InstagramUrl = p.InstagramUrl,
        TiktokUrl = p.TiktokUrl,
        WhatsappNumber = p.WhatsappNumber,
        StoreFrontPhotoUrl = p.StoreFrontPhotoUrl,
        KitchenPhotos = string.IsNullOrEmpty(p.KitchenPhotosJson)
            ? [] : JsonSerializer.Deserialize<List<string>>(p.KitchenPhotosJson) ?? [],
        OnboardingStatus = p.OnboardingStatus,
        SubmittedAt = p.SubmittedAt,
        ReviewedAt = p.ReviewedAt,
        RejectionReason = p.RejectionReason,
    };
}
