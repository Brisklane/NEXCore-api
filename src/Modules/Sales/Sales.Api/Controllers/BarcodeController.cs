using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nexcore.SharedKernel.Api;
using Sales.Application.Services.Interfaces;

namespace Sales.Api.Controllers;

/// <summary>
/// On-demand barcode / QR image generation (PNG). Used by the UI to display or print barcodes.
/// </summary>
[ApiController]
[Route("api/sales/[controller]")]
[Authorize]
public class BarcodeController : ControllerBase
{
    private readonly IBarcodeService _barcodes;
    private readonly ILogger<BarcodeController> _logger;

    public BarcodeController(IBarcodeService barcodes, ILogger<BarcodeController> logger)
    {
        _barcodes = barcodes;
        _logger = logger;
    }

    /// <summary>
    /// Generate a barcode/QR PNG. Example: /api/sales/barcode?value=12345&amp;symbology=Code128&amp;width=400&amp;height=150
    /// </summary>
    [HttpGet]
    [Produces("image/png")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public IActionResult Get(
        [FromQuery] string value,
        [FromQuery] string symbology = "Code128",
        [FromQuery] int width = 400,
        [FromQuery] int height = 150)
    {
        if (string.IsNullOrWhiteSpace(value))
            return BadRequest(new ApiErrorResponse { Message = "value is required" });

        try
        {
            var png = _barcodes.GeneratePng(value, symbology, width, height);
            return File(png, "image/png");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating barcode for value {Value} ({Symbology})", value, symbology);
            return BadRequest(new ApiErrorResponse { Message = $"Could not generate '{symbology}' barcode for the supplied value" });
        }
    }
}
