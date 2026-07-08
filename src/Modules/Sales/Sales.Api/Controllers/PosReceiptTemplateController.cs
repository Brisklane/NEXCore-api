using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nexcore.SharedKernel.Api;
using Nexcore.SharedKernel.Helpers;
using Sales.Application.DTOs;
using Sales.Application.Services.Interfaces;
using Sales.Domain.Entities;
using Sales.Infrastructure.Repositories.Interfaces;

namespace Sales.Api.Controllers;

/// <summary>
/// Receipt / invoice template management.
///
/// Templates are branch-scoped and drive the rendered thermal receipt, full HTML receipt, and the
/// HTML invoice document (header/logo/footer, tax registration, display toggles, paper size).
/// A store/terminal selects a template via its <c>ReceiptTemplateId</c>; the <c>IsDefault</c> template
/// is used when nothing more specific is configured.
/// </summary>
[ApiController]
[Route("api/sales/[controller]")]
[Authorize]
public class PosReceiptTemplateController : ControllerBase
{
    private static readonly HashSet<string> ValidPaperSizes =
        new(StringComparer.OrdinalIgnoreCase) { "Thermal58mm", "Thermal80mm", "A4" };

    private static readonly HashSet<string> AllowedLogoTypes =
        new(StringComparer.OrdinalIgnoreCase) { "image/png", "image/jpeg", "image/webp", "image/gif" };

    private readonly IPosReceiptTemplateRepository _templates;
    private readonly IReceiptLogoService _logos;
    private readonly ILogger<PosReceiptTemplateController> _logger;

    public PosReceiptTemplateController(
        IPosReceiptTemplateRepository templates,
        IReceiptLogoService logos,
        ILogger<PosReceiptTemplateController> logger)
    {
        _templates = templates;
        _logos     = logos;
        _logger    = logger;
    }

    // ── Reads ───────────────────────────────────────────────────────────────────

    /// <summary>List all receipt/invoice templates for the current branch.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<PosReceiptTemplateDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            var list = await _templates.GetByBranchAsync(Guid.Empty);   // repo scopes by JWT tenant
            return Ok(new ApiResponse<List<PosReceiptTemplateDto>>
            {
                Success = true,
                Data = list.Select(MapToDto).ToList(),
                Message = "Templates retrieved",
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving receipt templates");
            return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving templates" });
        }
    }

    /// <summary>Get a template by ID.</summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<PosReceiptTemplateDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        try
        {
            var template = await _templates.GetByIdAsync(id);
            if (template == null) return NotFound(new ApiErrorResponse { Message = "Template not found" });
            return Ok(new ApiResponse<PosReceiptTemplateDto> { Success = true, Data = MapToDto(template), Message = "Template retrieved" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving template {Id}", id);
            return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving template" });
        }
    }

    /// <summary>Get the branch's default template.</summary>
    [HttpGet("default")]
    [ProducesResponseType(typeof(ApiResponse<PosReceiptTemplateDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDefault()
    {
        try
        {
            var template = await _templates.GetDefaultAsync(Guid.Empty);
            if (template == null) return NotFound(new ApiErrorResponse { Message = "No default template set" });
            return Ok(new ApiResponse<PosReceiptTemplateDto> { Success = true, Data = MapToDto(template), Message = "Default template retrieved" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving default template");
            return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving template" });
        }
    }

    // ── Create ──────────────────────────────────────────────────────────────────

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<PosReceiptTemplateDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreatePosReceiptTemplateDto dto)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(dto.TemplateName))
                return BadRequest(new ApiErrorResponse { Message = "TemplateName is required" });
            if (!ValidPaperSizes.Contains(dto.PaperSize))
                return BadRequest(new ApiErrorResponse { Message = "PaperSize must be Thermal58mm, Thermal80mm, or A4" });

            var template = new PosReceiptTemplate
            {
                TemplateName          = dto.TemplateName,
                HeaderBusinessName    = dto.HeaderBusinessName,
                HeaderAddressLine1    = dto.HeaderAddressLine1,
                HeaderAddressLine2    = dto.HeaderAddressLine2,
                HeaderPhone           = dto.HeaderPhone,
                HeaderEmail           = dto.HeaderEmail,
                HeaderWebsite         = dto.HeaderWebsite,
                TaxRegistrationNumber = dto.TaxRegistrationNumber,
                LogoUrl               = dto.LogoUrl,
                HeaderMessage         = dto.HeaderMessage,
                FooterMessage         = dto.FooterMessage,
                ReturnPolicy          = dto.ReturnPolicy,
                ShowBarcode           = dto.ShowBarcode,
                ShowQrCode            = dto.ShowQrCode,
                ShowCashierName       = dto.ShowCashierName,
                ShowCustomerName      = dto.ShowCustomerName,
                ShowDiscountLine      = dto.ShowDiscountLine,
                ShowTaxBreakdown      = dto.ShowTaxBreakdown,
                ShowLoyaltyPoints     = dto.ShowLoyaltyPoints,
                ShowSavingsAmount     = dto.ShowSavingsAmount,
                PaperSize             = dto.PaperSize,
                IsDefault             = dto.IsDefault,
                PrintCopies           = dto.PrintCopies <= 0 ? 1 : dto.PrintCopies,
                AutoCutPaper          = dto.AutoCutPaper,
                OpenCashDrawer        = dto.OpenCashDrawer,
                BarcodeSymbology      = string.IsNullOrWhiteSpace(dto.BarcodeSymbology) ? "Code128" : dto.BarcodeSymbology,
            };

            await _templates.AddAsync(template);
            await _templates.SaveChangesAsync();

            if (template.IsDefault)
                await ClearOtherDefaultsAsync(template.Id);

            _logger.LogInformation("Receipt template created: {Name} ({Id})", template.TemplateName, template.Id);
            return StatusCode(201, new ApiResponse<PosReceiptTemplateDto> { Success = true, Data = MapToDto(template), Message = "Template created" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating receipt template");
            return StatusCode(500, new ApiErrorResponse { Message = "Error creating template" });
        }
    }

    // ── Update ──────────────────────────────────────────────────────────────────

    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse<PosReceiptTemplateDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdatePosReceiptTemplateDto dto)
    {
        try
        {
            var t = await _templates.GetByIdAsync(id);
            if (t == null) return NotFound(new ApiErrorResponse { Message = "Template not found" });

            if (dto.PaperSize != null && !ValidPaperSizes.Contains(dto.PaperSize))
                return BadRequest(new ApiErrorResponse { Message = "PaperSize must be Thermal58mm, Thermal80mm, or A4" });

            if (dto.TemplateName          != null) t.TemplateName          = dto.TemplateName;
            if (dto.HeaderBusinessName    != null) t.HeaderBusinessName    = dto.HeaderBusinessName;
            if (dto.HeaderAddressLine1    != null) t.HeaderAddressLine1    = dto.HeaderAddressLine1;
            if (dto.HeaderAddressLine2    != null) t.HeaderAddressLine2    = dto.HeaderAddressLine2;
            if (dto.HeaderPhone           != null) t.HeaderPhone           = dto.HeaderPhone;
            if (dto.HeaderEmail           != null) t.HeaderEmail           = dto.HeaderEmail;
            if (dto.HeaderWebsite         != null) t.HeaderWebsite         = dto.HeaderWebsite;
            if (dto.TaxRegistrationNumber != null) t.TaxRegistrationNumber = dto.TaxRegistrationNumber;
            if (dto.LogoUrl               != null) t.LogoUrl               = dto.LogoUrl;
            if (dto.HeaderMessage         != null) t.HeaderMessage         = dto.HeaderMessage;
            if (dto.FooterMessage         != null) t.FooterMessage         = dto.FooterMessage;
            if (dto.ReturnPolicy          != null) t.ReturnPolicy          = dto.ReturnPolicy;
            if (dto.ShowBarcode.HasValue)          t.ShowBarcode           = dto.ShowBarcode.Value;
            if (dto.ShowQrCode.HasValue)           t.ShowQrCode            = dto.ShowQrCode.Value;
            if (dto.ShowCashierName.HasValue)      t.ShowCashierName       = dto.ShowCashierName.Value;
            if (dto.ShowCustomerName.HasValue)     t.ShowCustomerName      = dto.ShowCustomerName.Value;
            if (dto.ShowDiscountLine.HasValue)     t.ShowDiscountLine      = dto.ShowDiscountLine.Value;
            if (dto.ShowTaxBreakdown.HasValue)     t.ShowTaxBreakdown      = dto.ShowTaxBreakdown.Value;
            if (dto.ShowLoyaltyPoints.HasValue)    t.ShowLoyaltyPoints     = dto.ShowLoyaltyPoints.Value;
            if (dto.ShowSavingsAmount.HasValue)    t.ShowSavingsAmount     = dto.ShowSavingsAmount.Value;
            if (dto.PaperSize             != null) t.PaperSize             = dto.PaperSize;
            if (dto.PrintCopies.HasValue)          t.PrintCopies           = dto.PrintCopies.Value <= 0 ? 1 : dto.PrintCopies.Value;
            if (dto.AutoCutPaper.HasValue)         t.AutoCutPaper          = dto.AutoCutPaper.Value;
            if (dto.OpenCashDrawer.HasValue)       t.OpenCashDrawer        = dto.OpenCashDrawer.Value;
            if (dto.BarcodeSymbology      != null) t.BarcodeSymbology      = dto.BarcodeSymbology;

            var becameDefault = dto.IsDefault == true && !t.IsDefault;
            if (dto.IsDefault.HasValue) t.IsDefault = dto.IsDefault.Value;

            await _templates.SaveChangesAsync();
            if (becameDefault) await ClearOtherDefaultsAsync(t.Id);

            return Ok(new ApiResponse<PosReceiptTemplateDto> { Success = true, Data = MapToDto(t), Message = "Template updated" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating template {Id}", id);
            return StatusCode(500, new ApiErrorResponse { Message = "Error updating template" });
        }
    }

    /// <summary>Make this template the branch default (clears the flag on the others).</summary>
    [HttpPost("{id}/set-default")]
    [ProducesResponseType(typeof(ApiResponse<PosReceiptTemplateDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetDefault(Guid id)
    {
        try
        {
            var t = await _templates.GetByIdAsync(id);
            if (t == null) return NotFound(new ApiErrorResponse { Message = "Template not found" });

            t.IsDefault = true;
            await _templates.SaveChangesAsync();
            await ClearOtherDefaultsAsync(t.Id);

            return Ok(new ApiResponse<PosReceiptTemplateDto> { Success = true, Data = MapToDto(t), Message = "Template set as default" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting default template {Id}", id);
            return StatusCode(500, new ApiErrorResponse { Message = "Error setting default template" });
        }
    }

    // ── Delete ──────────────────────────────────────────────────────────────────

    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            var t = await _templates.GetByIdAsync(id);
            if (t == null) return NotFound(new ApiErrorResponse { Message = "Template not found" });

            _templates.Delete(t);
            await _templates.SaveChangesAsync();

            _logger.LogInformation("Receipt template {Id} ({Name}) deleted", id, t.TemplateName);
            return Ok(new ApiResponse<bool> { Success = true, Data = true, Message = "Template deleted" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting template {Id}", id);
            return StatusCode(500, new ApiErrorResponse { Message = "Error deleting template" });
        }
    }

    // ── Logo upload / clear ─────────────────────────────────────────────────────

    /// <summary>
    /// Upload a custom logo for this receipt template.
    /// Accepts PNG, JPEG, WebP, GIF — max 2 MB.
    /// When a logo is uploaded, it overrides the company default on all receipts using this template.
    /// </summary>
    [HttpPost("{id}/upload-logo")]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UploadLogo(Guid id, IFormFile file)
    {
        try
        {
            if (file is null || file.Length == 0)
                return BadRequest(new ApiErrorResponse { Message = "No file provided" });
            if (file.Length > 2 * 1024 * 1024)
                return BadRequest(new ApiErrorResponse { Message = "Logo must be smaller than 2 MB" });
            if (!AllowedLogoTypes.Contains(file.ContentType.ToLowerInvariant()))
                return BadRequest(new ApiErrorResponse { Message = "Only PNG, JPEG, WebP, and GIF logos are accepted" });

            var t = await _templates.GetByIdAsync(id);
            if (t == null) return NotFound(new ApiErrorResponse { Message = "Template not found" });

            var (companyId, branchId, businessUnitId) = TenantContextHelper.ExtractTenantContext(User);

            // Delete previous custom logo if one exists
            if (!string.IsNullOrWhiteSpace(t.LogoUrl))
                await _logos.DeleteAsync(t.LogoUrl);

            using var stream = file.OpenReadStream();
            var url = await _logos.UploadAsync(
                stream, file.FileName, file.ContentType,
                companyId, branchId, businessUnitId, t.Id);

            t.LogoUrl = url;
            _templates.Update(t);
            await _templates.SaveChangesAsync();

            _logger.LogInformation("Logo uploaded for receipt template {Id}: {Url}", id, url);
            return Ok(new ApiResponse<string> { Success = true, Data = url, Message = "Logo uploaded successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading logo for template {Id}", id);
            return StatusCode(500, new ApiErrorResponse { Message = "Error uploading logo" });
        }
    }

    /// <summary>
    /// Remove the custom logo from a receipt template.
    /// Receipts will fall back to the company logo (GET /api/core/company/logo).
    /// </summary>
    [HttpDelete("{id}/logo")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ClearLogo(Guid id)
    {
        try
        {
            var t = await _templates.GetByIdAsync(id);
            if (t == null) return NotFound(new ApiErrorResponse { Message = "Template not found" });

            if (!string.IsNullOrWhiteSpace(t.LogoUrl))
            {
                await _logos.DeleteAsync(t.LogoUrl);
                t.LogoUrl = null;
                _templates.Update(t);
                await _templates.SaveChangesAsync();
            }

            return Ok(new ApiResponse<bool> { Success = true, Data = true, Message = "Custom logo removed — company logo will be used" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing logo for template {Id}", id);
            return StatusCode(500, new ApiErrorResponse { Message = "Error clearing logo" });
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────────

    /// <summary>Ensures only one template per branch carries IsDefault.</summary>
    private async Task ClearOtherDefaultsAsync(Guid keepId)
    {
        var all = await _templates.GetByBranchAsync(Guid.Empty);
        var changed = false;
        foreach (var other in all.Where(x => x.Id != keepId && x.IsDefault))
        {
            other.IsDefault = false;
            _templates.Update(other);
            changed = true;
        }
        if (changed) await _templates.SaveChangesAsync();
    }

    private static PosReceiptTemplateDto MapToDto(PosReceiptTemplate t) => new()
    {
        Id                    = t.Id,
        TemplateName          = t.TemplateName,
        HeaderBusinessName    = t.HeaderBusinessName,
        HeaderAddressLine1    = t.HeaderAddressLine1,
        HeaderAddressLine2    = t.HeaderAddressLine2,
        HeaderPhone           = t.HeaderPhone,
        HeaderEmail           = t.HeaderEmail,
        HeaderWebsite         = t.HeaderWebsite,
        TaxRegistrationNumber = t.TaxRegistrationNumber,
        LogoUrl               = t.LogoUrl,
        HeaderMessage         = t.HeaderMessage,
        FooterMessage         = t.FooterMessage,
        ReturnPolicy          = t.ReturnPolicy,
        ShowBarcode           = t.ShowBarcode,
        ShowQrCode            = t.ShowQrCode,
        ShowCashierName       = t.ShowCashierName,
        ShowCustomerName      = t.ShowCustomerName,
        ShowDiscountLine      = t.ShowDiscountLine,
        ShowTaxBreakdown      = t.ShowTaxBreakdown,
        ShowLoyaltyPoints     = t.ShowLoyaltyPoints,
        ShowSavingsAmount     = t.ShowSavingsAmount,
        PaperSize             = t.PaperSize,
        IsDefault             = t.IsDefault,
        PrintCopies           = t.PrintCopies,
        AutoCutPaper          = t.AutoCutPaper,
        OpenCashDrawer        = t.OpenCashDrawer,
        BarcodeSymbology      = t.BarcodeSymbology,
    };
}
