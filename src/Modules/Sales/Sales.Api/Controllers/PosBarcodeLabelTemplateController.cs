using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nexcore.SharedKernel.Api;
using Sales.Application.DTOs;
using Sales.Application.Services.Interfaces;
using Sales.Domain.Entities;
using Sales.Infrastructure.Repositories.Interfaces;

namespace Sales.Api.Controllers;

/// <summary>
/// Product barcode-label (price tag / shelf label) template management, plus label rendering.
/// Templates are branch-scoped; one can be the <c>IsDefault</c>.
/// </summary>
[ApiController]
[Route("api/sales/[controller]")]
[Authorize]
public class PosBarcodeLabelTemplateController : ControllerBase
{
    private static readonly HashSet<string> ValidSymbologies =
        new(StringComparer.OrdinalIgnoreCase) { "Code128", "Code39", "EAN-13", "EAN13", "EAN-8", "EAN8", "UPC-A", "UPCA", "QR", "QRCode" };

    private readonly IPosBarcodeLabelTemplateRepository _templates;
    private readonly IBarcodeLabelService _labels;
    private readonly ILogger<PosBarcodeLabelTemplateController> _logger;

    public PosBarcodeLabelTemplateController(
        IPosBarcodeLabelTemplateRepository templates,
        IBarcodeLabelService labels,
        ILogger<PosBarcodeLabelTemplateController> logger)
    {
        _templates = templates;
        _labels = labels;
        _logger = logger;
    }

    // ── Reads ───────────────────────────────────────────────────────────────────

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<PosBarcodeLabelTemplateDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            var list = await _templates.GetByBranchAsync(Guid.Empty);   // repo scopes by JWT tenant
            return Ok(new ApiResponse<List<PosBarcodeLabelTemplateDto>>
            {
                Success = true,
                Data = list.Select(MapToDto).ToList(),
                Message = "Label templates retrieved",
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving label templates");
            return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving label templates" });
        }
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<PosBarcodeLabelTemplateDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        try
        {
            var t = await _templates.GetByIdAsync(id);
            if (t == null) return NotFound(new ApiErrorResponse { Message = "Label template not found" });
            return Ok(new ApiResponse<PosBarcodeLabelTemplateDto> { Success = true, Data = MapToDto(t), Message = "Label template retrieved" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving label template {Id}", id);
            return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving label template" });
        }
    }

    // ── Create / Update / Delete ────────────────────────────────────────────────

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<PosBarcodeLabelTemplateDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreatePosBarcodeLabelTemplateDto dto)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(dto.TemplateName))
                return BadRequest(new ApiErrorResponse { Message = "TemplateName is required" });
            if (!ValidSymbologies.Contains(dto.BarcodeSymbology))
                return BadRequest(new ApiErrorResponse { Message = "Unsupported BarcodeSymbology" });
            if (dto.LabelWidthMm <= 0 || dto.LabelHeightMm <= 0)
                return BadRequest(new ApiErrorResponse { Message = "Label width and height must be positive" });

            var t = new PosBarcodeLabelTemplate
            {
                TemplateName     = dto.TemplateName,
                LabelWidthMm     = dto.LabelWidthMm,
                LabelHeightMm    = dto.LabelHeightMm,
                BarcodeSymbology = dto.BarcodeSymbology,
                HeaderText       = dto.HeaderText,
                ShowProductName  = dto.ShowProductName,
                ShowPrice        = dto.ShowPrice,
                ShowSku          = dto.ShowSku,
                ShowBarcodeValue = dto.ShowBarcodeValue,
                CurrencySymbol   = dto.CurrencySymbol,
                ProductNameFontPt = dto.ProductNameFontPt <= 0 ? 8  : dto.ProductNameFontPt,
                PriceFontPt       = dto.PriceFontPt       <= 0 ? 11 : dto.PriceFontPt,
                BarcodeHeightPt   = dto.BarcodeHeightPt   <= 0 ? 12 : dto.BarcodeHeightPt,
                ShowBorders      = dto.ShowBorders,
                RollPaper        = dto.RollPaper,
                IsDefault        = dto.IsDefault,
            };

            await _templates.AddAsync(t);
            await _templates.SaveChangesAsync();
            if (t.IsDefault) await ClearOtherDefaultsAsync(t.Id);

            return StatusCode(201, new ApiResponse<PosBarcodeLabelTemplateDto> { Success = true, Data = MapToDto(t), Message = "Label template created" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating label template");
            return StatusCode(500, new ApiErrorResponse { Message = "Error creating label template" });
        }
    }

    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse<PosBarcodeLabelTemplateDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdatePosBarcodeLabelTemplateDto dto)
    {
        try
        {
            var t = await _templates.GetByIdAsync(id);
            if (t == null) return NotFound(new ApiErrorResponse { Message = "Label template not found" });

            if (dto.BarcodeSymbology != null && !ValidSymbologies.Contains(dto.BarcodeSymbology))
                return BadRequest(new ApiErrorResponse { Message = "Unsupported BarcodeSymbology" });

            if (dto.TemplateName     != null) t.TemplateName     = dto.TemplateName;
            if (dto.LabelWidthMm.HasValue && dto.LabelWidthMm > 0)   t.LabelWidthMm  = dto.LabelWidthMm.Value;
            if (dto.LabelHeightMm.HasValue && dto.LabelHeightMm > 0) t.LabelHeightMm = dto.LabelHeightMm.Value;
            if (dto.BarcodeSymbology != null) t.BarcodeSymbology = dto.BarcodeSymbology;
            if (dto.HeaderText       != null) t.HeaderText       = dto.HeaderText;
            if (dto.ShowProductName.HasValue)  t.ShowProductName  = dto.ShowProductName.Value;
            if (dto.ShowPrice.HasValue)        t.ShowPrice        = dto.ShowPrice.Value;
            if (dto.ShowSku.HasValue)          t.ShowSku          = dto.ShowSku.Value;
            if (dto.ShowBarcodeValue.HasValue) t.ShowBarcodeValue = dto.ShowBarcodeValue.Value;
            if (dto.CurrencySymbol   != null) t.CurrencySymbol   = dto.CurrencySymbol;
            if (dto.ProductNameFontPt.HasValue && dto.ProductNameFontPt > 0) t.ProductNameFontPt = dto.ProductNameFontPt.Value;
            if (dto.PriceFontPt.HasValue && dto.PriceFontPt > 0)             t.PriceFontPt       = dto.PriceFontPt.Value;
            if (dto.BarcodeHeightPt.HasValue && dto.BarcodeHeightPt > 0)     t.BarcodeHeightPt   = dto.BarcodeHeightPt.Value;
            if (dto.ShowBorders.HasValue)     t.ShowBorders      = dto.ShowBorders.Value;
            if (dto.RollPaper.HasValue)       t.RollPaper        = dto.RollPaper.Value;

            var becameDefault = dto.IsDefault == true && !t.IsDefault;
            if (dto.IsDefault.HasValue) t.IsDefault = dto.IsDefault.Value;

            await _templates.SaveChangesAsync();
            if (becameDefault) await ClearOtherDefaultsAsync(t.Id);

            return Ok(new ApiResponse<PosBarcodeLabelTemplateDto> { Success = true, Data = MapToDto(t), Message = "Label template updated" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating label template {Id}", id);
            return StatusCode(500, new ApiErrorResponse { Message = "Error updating label template" });
        }
    }

    [HttpPost("{id}/set-default")]
    [ProducesResponseType(typeof(ApiResponse<PosBarcodeLabelTemplateDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetDefault(Guid id)
    {
        try
        {
            var t = await _templates.GetByIdAsync(id);
            if (t == null) return NotFound(new ApiErrorResponse { Message = "Label template not found" });

            t.IsDefault = true;
            await _templates.SaveChangesAsync();
            await ClearOtherDefaultsAsync(t.Id);

            return Ok(new ApiResponse<PosBarcodeLabelTemplateDto> { Success = true, Data = MapToDto(t), Message = "Label template set as default" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting default label template {Id}", id);
            return StatusCode(500, new ApiErrorResponse { Message = "Error setting default label template" });
        }
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            var t = await _templates.GetByIdAsync(id);
            if (t == null) return NotFound(new ApiErrorResponse { Message = "Label template not found" });

            _templates.Delete(t);
            await _templates.SaveChangesAsync();
            return Ok(new ApiResponse<bool> { Success = true, Data = true, Message = "Label template deleted" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting label template {Id}", id);
            return StatusCode(500, new ApiErrorResponse { Message = "Error deleting label template" });
        }
    }

    // ── Render ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// Render product barcode label(s) to a PDF (one label per page, sized to the template).
    /// Pass a TemplateId (or omit for the branch default), the barcode value, and optional
    /// product name / SKU / price / copies.
    /// </summary>
    [HttpPost("render")]
    [Produces("application/pdf")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Render([FromBody] RenderBarcodeLabelDto dto)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(dto.BarcodeValue))
                return BadRequest(new ApiErrorResponse { Message = "BarcodeValue is required" });

            var pdf = await _labels.RenderLabelPdfAsync(dto);
            if (pdf == null) return NotFound(new ApiErrorResponse { Message = "Label template not found" });
            return File(pdf, "application/pdf", $"label-{dto.BarcodeValue}.pdf");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rendering barcode label for {Value}", dto.BarcodeValue);
            return StatusCode(500, new ApiErrorResponse { Message = "Error rendering label" });
        }
    }

    /// <summary>
    /// Render labels for several products in one PDF (each product × its Copies) — the price-tag
    /// designer's "Generate Preview" / "Print" over the selected products.
    /// </summary>
    [HttpPost("render-batch")]
    [Produces("application/pdf")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RenderBatch([FromBody] RenderBarcodeLabelBatchDto dto)
    {
        try
        {
            if (dto.Items == null || dto.Items.Count == 0)
                return BadRequest(new ApiErrorResponse { Message = "At least one item is required" });
            if (dto.Items.All(i => string.IsNullOrWhiteSpace(i.BarcodeValue)))
                return BadRequest(new ApiErrorResponse { Message = "Each item needs a BarcodeValue" });

            var pdf = await _labels.RenderBatchPdfAsync(dto);
            if (pdf == null) return NotFound(new ApiErrorResponse { Message = "Label template not found" });
            return File(pdf, "application/pdf", "labels.pdf");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rendering batch barcode labels");
            return StatusCode(500, new ApiErrorResponse { Message = "Error rendering labels" });
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────────

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

    private static PosBarcodeLabelTemplateDto MapToDto(PosBarcodeLabelTemplate t) => new()
    {
        Id               = t.Id,
        TemplateName     = t.TemplateName,
        LabelWidthMm     = t.LabelWidthMm,
        LabelHeightMm    = t.LabelHeightMm,
        BarcodeSymbology = t.BarcodeSymbology,
        HeaderText       = t.HeaderText,
        ShowProductName  = t.ShowProductName,
        ShowPrice        = t.ShowPrice,
        ShowSku          = t.ShowSku,
        ShowBarcodeValue = t.ShowBarcodeValue,
        CurrencySymbol   = t.CurrencySymbol,
        ProductNameFontPt = t.ProductNameFontPt,
        PriceFontPt       = t.PriceFontPt,
        BarcodeHeightPt   = t.BarcodeHeightPt,
        ShowBorders      = t.ShowBorders,
        RollPaper        = t.RollPaper,
        IsDefault        = t.IsDefault,
    };
}
