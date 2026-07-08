using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nexcore.SharedKernel.Api;
using Sales.Application.DTOs;
using Sales.Application.Services.Interfaces;

namespace Sales.Api.Controllers;

/// <summary>
/// POS transactions — the point-of-sale settlement flow.
///
/// Lifecycle:
///   1. Cashier scans items  → a Draft <c>SalesOrder</c> is created via <c>POST api/sales/salesorder</c>.
///   2. Cashier taps "Pay"   → <c>POST api/sales/postransaction/checkout</c> invoices the order,
///                             records the tender(s), writes the POS transaction and fires the
///                             accounting + stock events. The receipt number equals the invoice number.
/// </summary>
[ApiController]
[Route("api/sales/[controller]")]
[Authorize]
public class PosTransactionController : ControllerBase
{
    private readonly IPosCheckoutService _checkout;
    private readonly IPosOfflineSyncService _offlineSync;
    private readonly IReceiptRenderingService _receipts;
    private readonly ILogger<PosTransactionController> _logger;

    public PosTransactionController(
        IPosCheckoutService checkout,
        IPosOfflineSyncService offlineSync,
        IReceiptRenderingService receipts,
        ILogger<PosTransactionController> logger)
    {
        _checkout    = checkout;
        _offlineSync = offlineSync;
        _receipts    = receipts;
        _logger      = logger;
    }

    /// <summary>
    /// Settle a scanned sales order: create &amp; post the invoice, register the tender(s),
    /// write the POS transaction (receipt no = invoice no) and deduct stock.
    /// Supports split tenders and — with <c>AllowCredit</c> — partial / credit sales.
    /// </summary>
    [HttpPost("checkout")]
    [ProducesResponseType(typeof(ApiResponse<PosCheckoutResultDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Checkout([FromBody] PosCheckoutDto dto)
    {
        try
        {
            if (!ModelState.IsValid) return BadRequest(new ApiErrorResponse { Message = "Invalid input" });

            var result = await _checkout.CheckoutAsync(dto);

            return StatusCode(201, new ApiResponse<PosCheckoutResultDto>
            {
                Success = true,
                Data    = result,
                Message = result.IsFullyPaid
                    ? $"Sale completed — receipt {result.ReceiptNumber}"
                    : $"Partial payment recorded — receipt {result.ReceiptNumber}, balance due {result.BalanceDue:N2}",
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ApiErrorResponse { Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during POS checkout for order {OrderId}", dto.SalesOrderId);
            return StatusCode(500, new ApiErrorResponse { Message = "Error processing POS checkout" });
        }
    }

    /// <summary>
    /// Replay sales a POS device completed while offline. Each order is created (server re-prices)
    /// and settled through the same checkout pipeline; idempotent on OfflineOrderNumber so retrying
    /// the same batch never double-posts. Returns the real order/receipt numbers per item.
    /// </summary>
    [HttpPost("offline-sync")]
    [ProducesResponseType(typeof(ApiResponse<OfflineSyncResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> OfflineSync([FromBody] OfflineSyncRequestDto dto)
    {
        try
        {
            if (dto?.Orders is null || dto.Orders.Count == 0)
                return BadRequest(new ApiErrorResponse { Message = "No offline orders to sync" });

            var result = await _offlineSync.SyncAsync(dto);
            return Ok(new ApiResponse<OfflineSyncResultDto>
            {
                Success = true,
                Data    = result,
                Message = $"{result.SyncedCount} synced, {result.DuplicateCount} duplicate, {result.FailedCount} failed",
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during offline POS sync");
            return StatusCode(500, new ApiErrorResponse { Message = "Error processing offline sync" });
        }
    }

    /// <summary>Get a POS transaction (with lines and tenders) by ID.</summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<PosTransactionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        try
        {
            var txn = await _checkout.GetByIdAsync(id);
            if (txn == null) return NotFound(new ApiErrorResponse { Message = "Transaction not found" });
            return Ok(new ApiResponse<PosTransactionDto> { Success = true, Data = txn, Message = "Transaction retrieved" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving POS transaction {Id}", id);
            return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving transaction" });
        }
    }

    /// <summary>
    /// Render the thermal printer receipt (plain monospace text) for a POS transaction.
    /// Optional <paramref name="paperSize"/> ("Thermal58mm" / "Thermal80mm") overrides the template width.
    /// The UI sends <c>Content</c> straight to the printer and can build a barcode from <c>ReceiptNumber</c>.
    /// </summary>
    [HttpGet("{id}/receipt")]
    [ProducesResponseType(typeof(ApiResponse<ThermalReceiptDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetReceipt(Guid id, [FromQuery] string? paperSize = null)
    {
        try
        {
            var receipt = await _receipts.RenderThermalAsync(id, paperSize);
            if (receipt == null) return NotFound(new ApiErrorResponse { Message = "Transaction not found" });
            return Ok(new ApiResponse<ThermalReceiptDto> { Success = true, Data = receipt, Message = "Receipt rendered" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rendering receipt for POS transaction {Id}", id);
            return StatusCode(500, new ApiErrorResponse { Message = "Error rendering receipt" });
        }
    }

    /// <summary>
    /// Render the full A4 HTML receipt (header/logo + items + tender/change) for a POS transaction.
    /// Returns text/html the UI can open in an iframe/new window and print.
    /// </summary>
    [HttpGet("{id}/receipt/html")]
    [Produces("text/html")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetReceiptHtml(Guid id)
    {
        try
        {
            var html = await _receipts.RenderHtmlReceiptAsync(id);
            if (html == null) return NotFound(new ApiErrorResponse { Message = "Transaction not found" });
            return Content(html, "text/html");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rendering HTML receipt for POS transaction {Id}", id);
            return StatusCode(500, new ApiErrorResponse { Message = "Error rendering receipt" });
        }
    }

    /// <summary>Render the A4 PDF receipt for a POS transaction (application/pdf).</summary>
    [HttpGet("{id}/receipt/pdf")]
    [Produces("application/pdf")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetReceiptPdf(Guid id)
    {
        try
        {
            var pdf = await _receipts.RenderPdfReceiptAsync(id);
            if (pdf == null) return NotFound(new ApiErrorResponse { Message = "Transaction not found" });
            return File(pdf, "application/pdf", $"receipt-{id}.pdf");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rendering PDF receipt for POS transaction {Id}", id);
            return StatusCode(500, new ApiErrorResponse { Message = "Error rendering receipt" });
        }
    }

    /// <summary>Get a POS transaction by its transaction number.</summary>
    [HttpGet("by-number/{transactionNumber}")]
    [ProducesResponseType(typeof(ApiResponse<PosTransactionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByNumber(string transactionNumber)
    {
        try
        {
            var txn = await _checkout.GetByNumberAsync(transactionNumber);
            if (txn == null) return NotFound(new ApiErrorResponse { Message = "Transaction not found" });
            return Ok(new ApiResponse<PosTransactionDto> { Success = true, Data = txn, Message = "Transaction retrieved" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving POS transaction {Number}", transactionNumber);
            return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving transaction" });
        }
    }

    /// <summary>Get all POS transactions for a session — used for the session sales list / X-report.</summary>
    [HttpGet("by-session/{sessionId}")]
    [ProducesResponseType(typeof(ApiResponse<List<PosTransactionDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBySession(Guid sessionId)
    {
        try
        {
            var list = await _checkout.GetBySessionAsync(sessionId);
            return Ok(new ApiResponse<List<PosTransactionDto>> { Success = true, Data = list, Message = "Session transactions retrieved" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving transactions for session {SessionId}", sessionId);
            return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving transactions" });
        }
    }

    /// <summary>
    /// Get POS transactions for the current branch within a UTC date range.
    /// Used for the sales history panel (cross-session / cross-day).
    /// <c>from</c> and <c>to</c> are inclusive UTC timestamps (ISO-8601).
    /// If <c>to</c> is omitted it defaults to end-of-day today (UTC).
    /// </summary>
    [HttpGet("by-date")]
    [ProducesResponseType(typeof(ApiResponse<List<PosTransactionDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByDateRange(
        [FromQuery] DateTime from,
        [FromQuery] DateTime? to = null)
    {
        try
        {
            var toUtc = (to ?? DateTime.UtcNow.Date.AddDays(1).AddTicks(-1));
            var list = await _checkout.GetByDateRangeAsync(from, toUtc);
            return Ok(new ApiResponse<List<PosTransactionDto>>
            {
                Success = true,
                Data    = list,
                Message = $"{list.Count} transaction(s) found",
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving transactions by date range");
            return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving transactions" });
        }
    }
}
