using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nexcore.SharedKernel.Api;
using Nexcore.SharedKernel.Events;
using Sales.Application.DTOs;
using Sales.Application.Services.Interfaces;
using Sales.Domain.Entities;
using Sales.Domain.Enums;
using Sales.Infrastructure.Repositories.Interfaces;
using Sales.Infrastructure.Services;

namespace Sales.Api.Controllers;

/// <summary>
/// Sales Invoice (AR Invoice) management.
///
/// The create / post / register-payment operations are delegated to <see cref="ISalesInvoiceService"/>
/// — the single, central invoice mechanism shared with the POS checkout flow — so a manual invoice and
/// a POS-generated invoice are created identically and appear in the same lists.
/// </summary>
[ApiController]
[Route("api/sales/[controller]")]
[Authorize]
public class SalesInvoiceController : ControllerBase
{
    private readonly ISalesInvoiceService _invoiceService;
    private readonly IReceiptRenderingService _receipts;
    private readonly ISalesInvoiceRepository _invoices;
    private readonly ISalesOrderRepository _orders;
    private readonly ISalesOrderLineRepository _orderLines;
    private readonly IDocumentSequenceService _sequences;
    private readonly IEventPublisher _events;
    private readonly ILogger<SalesInvoiceController> _logger;

    public SalesInvoiceController(
        ISalesInvoiceService invoiceService,
        IReceiptRenderingService receipts,
        ISalesInvoiceRepository invoices,
        ISalesOrderRepository orders,
        ISalesOrderLineRepository orderLines,
        IDocumentSequenceService sequences,
        IEventPublisher events,
        ILogger<SalesInvoiceController> logger)
    {
        _invoiceService = invoiceService;
        _receipts    = receipts;
        _invoices    = invoices;
        _orders      = orders;
        _orderLines  = orderLines;
        _sequences   = sequences;
        _events      = events;
        _logger      = logger;
    }

    /// <summary>
    /// Render the invoice as a full A4 HTML document (header/logo, bill-to, lines, tax, balance due).
    /// Returns text/html the UI can open and print.
    /// </summary>
    [HttpGet("{id}/document")]
    [Produces("text/html")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDocument(Guid id)
    {
        try
        {
            var html = await _receipts.RenderHtmlInvoiceAsync(id);
            if (html == null) return NotFound(new ApiErrorResponse { Message = "Invoice not found" });
            return Content(html, "text/html");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rendering invoice document {Id}", id);
            return StatusCode(500, new ApiErrorResponse { Message = "Error rendering invoice document" });
        }
    }

    /// <summary>Render the invoice as an A4 PDF document (application/pdf).</summary>
    [HttpGet("{id}/document/pdf")]
    [Produces("application/pdf")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDocumentPdf(Guid id)
    {
        try
        {
            var pdf = await _receipts.RenderPdfInvoiceAsync(id);
            if (pdf == null) return NotFound(new ApiErrorResponse { Message = "Invoice not found" });
            return File(pdf, "application/pdf", $"invoice-{id}.pdf");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rendering invoice PDF {Id}", id);
            return StatusCode(500, new ApiErrorResponse { Message = "Error rendering invoice document" });
        }
    }

    // ── GET ───────────────────────────────────────────────────────────────────

    /// <summary>Get all invoices</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<SalesInvoiceDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            var list = await _invoices.GetAllAsync();
            return Ok(new ApiResponse<List<SalesInvoiceDto>> { Success = true, Data = list.OrderByDescending(i => i.CreatedAt).Select(MapToDto).ToList(), Message = "Invoices retrieved" });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving invoices"); return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving invoices" }); }
    }

    /// <summary>Get invoice by ID (with lines)</summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<SalesInvoiceDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        try
        {
            var invoice = await _invoices.GetWithLinesAsync(id);
            if (invoice == null) return NotFound(new ApiErrorResponse { Message = "Invoice not found" });
            return Ok(new ApiResponse<SalesInvoiceDto> { Success = true, Data = MapToDto(invoice), Message = "Invoice retrieved" });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving invoice {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving invoice" }); }
    }

    /// <summary>Get invoice by number</summary>
    [HttpGet("by-number/{invoiceNumber}")]
    [ProducesResponseType(typeof(ApiResponse<SalesInvoiceDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByNumber(string invoiceNumber)
    {
        try
        {
            var invoice = await _invoices.GetByNumberAsync(invoiceNumber);
            if (invoice == null) return NotFound(new ApiErrorResponse { Message = "Invoice not found" });
            return Ok(new ApiResponse<SalesInvoiceDto> { Success = true, Data = MapToDto(invoice), Message = "Invoice retrieved" });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving invoice {Number}", invoiceNumber); return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving invoice" }); }
    }

    /// <summary>Get invoices for a specific customer</summary>
    [HttpGet("by-customer/{contactId}")]
    [ProducesResponseType(typeof(ApiResponse<List<SalesInvoiceDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByCustomer(Guid contactId)
    {
        try
        {
            var list = await _invoices.GetByContactAsync(contactId);
            return Ok(new ApiResponse<List<SalesInvoiceDto>> { Success = true, Data = list.Select(MapToDto).ToList(), Message = "Customer invoices" });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving invoices for customer"); return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving invoices" }); }
    }

    /// <summary>Get invoices for a specific sales order</summary>
    [HttpGet("by-order/{salesOrderId}")]
    [ProducesResponseType(typeof(ApiResponse<List<SalesInvoiceDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByOrder(Guid salesOrderId)
    {
        try
        {
            var list = await _invoices.GetByOrderAsync(salesOrderId);
            return Ok(new ApiResponse<List<SalesInvoiceDto>> { Success = true, Data = list.Select(MapToDto).ToList(), Message = "Order invoices" });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving invoices for order"); return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving invoices" }); }
    }

    /// <summary>Get all overdue invoices</summary>
    [HttpGet("overdue")]
    [ProducesResponseType(typeof(ApiResponse<List<SalesInvoiceDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOverdue()
    {
        try
        {
            var list = await _invoices.GetOverdueAsync();
            return Ok(new ApiResponse<List<SalesInvoiceDto>> { Success = true, Data = list.Select(MapToDto).ToList(), Message = "Overdue invoices" });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving overdue invoices"); return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving invoices" }); }
    }

    // ── POST (create) ─────────────────────────────────────────────────────────

    /// <summary>
    /// Create a draft invoice manually from provided lines.
    /// Use POST /api/sales/salesorder/{id}/create-invoice for the Odoo-style "Create Invoice" flow.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<SalesInvoiceDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateSalesInvoiceDto dto)
    {
        try
        {
            if (!ModelState.IsValid) return BadRequest(new ApiErrorResponse { Message = "Invalid input" });

            var invoice = await _invoiceService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = invoice.Id },
                new ApiResponse<SalesInvoiceDto> { Success = true, Data = invoice, Message = "Invoice created" });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error creating invoice"); return StatusCode(500, new ApiErrorResponse { Message = "Error creating invoice" }); }
    }

    // ── Lifecycle actions ─────────────────────────────────────────────────────

    /// <summary>
    /// Confirm invoice — transitions Draft → Posted (Issued) and posts the AR journal entry.
    /// Equivalent to Odoo's "Confirm" button.
    /// </summary>
    [HttpPost("{id}/confirm")]
    [ProducesResponseType(typeof(ApiResponse<SalesInvoiceDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Confirm(Guid id)
    {
        try
        {
            var invoice = await _invoiceService.PostAsync(id);
            return Ok(new ApiResponse<SalesInvoiceDto> { Success = true, Data = invoice, Message = "Invoice confirmed (Posted)" });
        }
        catch (InvalidOperationException ex) { return BadRequest(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error confirming invoice {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Error confirming invoice" }); }
    }

    /// <summary>
    /// Reset a Posted invoice back to Draft.
    /// Equivalent to Odoo's "Reset to Draft" button. Reverses InvoicedQuantity on order lines.
    /// </summary>
    [HttpPost("{id}/reset-to-draft")]
    [ProducesResponseType(typeof(ApiResponse<SalesInvoiceDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ResetToDraft(Guid id)
    {
        try
        {
            var invoice = await _invoices.GetWithLinesAsync(id);
            if (invoice == null) return NotFound(new ApiErrorResponse { Message = "Invoice not found" });

            if (invoice.Status != InvoiceStatus.Issued)
                return BadRequest(new ApiErrorResponse { Message = "Only Posted invoices can be reset to Draft" });
            if (invoice.PaidAmount > 0)
                return BadRequest(new ApiErrorResponse { Message = "Invoice has registered payments — reverse them before resetting" });

            invoice.Status        = InvoiceStatus.Draft;
            invoice.PaymentStatus = InvoicePaymentStatus.NotPaid;
            _invoices.Update(invoice);

            await ReverseInvoicedQuantitiesAsync(invoice);

            _logger.LogInformation("Invoice {InvoiceNumber} reset to Draft", invoice.InvoiceNumber);
            return Ok(new ApiResponse<SalesInvoiceDto> { Success = true, Data = MapToDto(invoice), Message = "Invoice reset to Draft" });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error resetting invoice {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Error resetting invoice" }); }
    }

    /// <summary>
    /// Register a payment against a Posted invoice — equivalent to Odoo's "Pay" button dialog.
    /// Delegates to the central <see cref="ISalesInvoiceService"/> (same path used by POS).
    /// </summary>
    [HttpPost("{id}/register-payment")]
    [ProducesResponseType(typeof(ApiResponse<SalesInvoiceDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RegisterPayment(Guid id, [FromBody] RegisterInvoicePaymentDto dto)
    {
        try
        {
            if (!ModelState.IsValid) return BadRequest(new ApiErrorResponse { Message = "Invalid input" });

            var invoice = await _invoiceService.RegisterPaymentAsync(id, dto);
            return Ok(new ApiResponse<SalesInvoiceDto>
            {
                Success = true,
                Data    = invoice,
                Message = invoice.BalanceDue <= 0 ? "Invoice fully paid" : $"Partial payment recorded — balance due: {invoice.BalanceDue:N2}",
            });
        }
        catch (InvalidOperationException ex) { return BadRequest(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error registering payment for invoice {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Error registering payment" }); }
    }

    /// <summary>
    /// Send invoice by email — equivalent to Odoo's "Send" button.
    /// Publishes a SalesInvoiceSentEvent for the notification layer to email the PDF.
    /// </summary>
    [HttpPost("{id}/send")]
    [ProducesResponseType(typeof(ApiResponse<SalesInvoiceDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Send(Guid id, [FromBody] SendInvoiceDto dto)
    {
        try
        {
            var invoice = await _invoices.GetWithLinesAsync(id);
            if (invoice == null) return NotFound(new ApiErrorResponse { Message = "Invoice not found" });
            if (invoice.Status == InvoiceStatus.Draft)
                return BadRequest(new ApiErrorResponse { Message = "Confirm the invoice before sending" });
            if (invoice.Status == InvoiceStatus.Cancelled)
                return BadRequest(new ApiErrorResponse { Message = "Cannot send a cancelled invoice" });

            await PublishInvoiceSentEventAsync(invoice, dto);

            _logger.LogInformation("Invoice {InvoiceNumber} sent", invoice.InvoiceNumber);
            return Ok(new ApiResponse<SalesInvoiceDto> { Success = true, Data = MapToDto(invoice), Message = "Invoice sent" });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error sending invoice {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Error sending invoice" }); }
    }

    /// <summary>Cancel invoice</summary>
    [HttpPost("{id}/cancel")]
    [ProducesResponseType(typeof(ApiResponse<SalesInvoiceDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Cancel(Guid id, [FromBody] CancelInvoiceDto dto)
    {
        try
        {
            var invoice = await _invoices.GetByIdAsync(id);
            if (invoice == null) return NotFound(new ApiErrorResponse { Message = "Invoice not found" });
            if (invoice.Status == InvoiceStatus.Paid || invoice.Status == InvoiceStatus.Cancelled)
                return BadRequest(new ApiErrorResponse { Message = $"Invoice cannot be cancelled from status {invoice.Status}" });

            invoice.Status = InvoiceStatus.Cancelled;
            invoice.Notes  = dto.Reason ?? invoice.Notes;
            _invoices.Update(invoice);
            await _invoices.SaveChangesAsync();

            return Ok(new ApiResponse<SalesInvoiceDto> { Success = true, Data = MapToDto(invoice), Message = "Invoice cancelled" });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error cancelling invoice {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Error cancelling invoice" }); }
    }

    /// <summary>
    /// Create a credit note from a Posted or Paid invoice — equivalent to Odoo's "Credit Note" button.
    /// Full reversal: creates a credit note for the full invoice amount and sets the invoice to Reversed.
    /// </summary>
    [HttpPost("{id}/credit-note")]
    [ProducesResponseType(typeof(ApiResponse<CreditNoteDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateCreditNote(Guid id, [FromBody] CreateCreditNoteFromInvoiceDto dto)
    {
        try
        {
            var invoice = await _invoices.GetWithLinesAsync(id);
            if (invoice == null) return NotFound(new ApiErrorResponse { Message = "Invoice not found" });
            if (invoice.Status == InvoiceStatus.Draft || invoice.Status == InvoiceStatus.Cancelled)
                return BadRequest(new ApiErrorResponse { Message = "Credit notes can only be issued for Posted or Paid invoices" });

            var cnSeq = await _sequences.GetNextNumberAsync(DocumentType.CreditNote);
            var creditNote = new CreditNote
            {
                CreditNoteNumber = cnSeq.Code,
                Code             = cnSeq.Code,
                CodeInt          = cnSeq.CodeInt,
                SalesInvoiceId   = invoice.Id,
                ContactId        = invoice.ContactId,
                ContactName      = invoice.ContactName,
                CreditNoteDate   = dto.CreditNoteDate ?? DateTime.UtcNow,
                CurrencyCode     = invoice.CurrencyCode,
                Reason           = dto.Reason,
                Notes            = dto.Notes,
                SubtotalAmount   = invoice.SubtotalAmount,
                TaxAmount        = invoice.TaxAmount,
                TotalAmount      = invoice.TotalAmount,
                Lines = invoice.Lines.Select((l, i) => new CreditNoteLine
                {
                    LineNumber    = i + 1,
                    ProductId     = l.ProductId ?? Guid.Empty,
                    ProductCode   = l.ProductCode,
                    ProductName   = l.ProductName,
                    Quantity      = l.Quantity,
                    UnitOfMeasure = l.UnitOfMeasure,
                    UnitPrice     = l.UnitPrice,
                    LineAmount    = l.LineAmount,
                    TaxCategory   = l.TaxCategory,
                    TaxRate       = l.TaxRate,
                    TaxAmount     = l.TaxAmount,
                    TotalAmount   = l.TotalAmount,
                    Reason        = dto.Reason,
                }).ToList(),
            };

            invoice.Status        = InvoiceStatus.CreditNote;
            invoice.PaymentStatus = InvoicePaymentStatus.Reversed;
            _invoices.Update(invoice);

            invoice.CreditNotes.Add(creditNote);
            await _invoices.SaveChangesAsync();

            _logger.LogInformation(
                "Credit note {CreditNoteNumber} created for invoice {InvoiceNumber}",
                creditNote.CreditNoteNumber, invoice.InvoiceNumber);

            return StatusCode(201, new ApiResponse<CreditNoteDto>
            {
                Success = true,
                Data    = MapCreditNoteToDto(creditNote),
                Message = "Credit note created",
            });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error creating credit note for invoice {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Error creating credit note" }); }
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    /// <summary>Reverses InvoicedQuantity when resetting an invoice to Draft.</summary>
    private async Task ReverseInvoicedQuantitiesAsync(SalesInvoice invoice)
    {
        try
        {
            foreach (var line in invoice.Lines.Where(l => l.SalesOrderLineId.HasValue))
            {
                var orderLine = await _orderLines.GetByIdAsync(line.SalesOrderLineId!.Value);
                if (orderLine == null) continue;
                orderLine.InvoicedQuantity = Math.Max(0, orderLine.InvoicedQuantity - line.Quantity);
            }

            var order = await _orders.GetWithLinesAsync(invoice.SalesOrderId);
            if (order != null)
            {
                RecalculateOrderInvoiceStatus(order);
                _orders.Update(order);
            }

            await _orders.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to reverse invoiced quantities for invoice {Id}", invoice.Id);
        }
    }

    private static void RecalculateOrderInvoiceStatus(SalesOrder order)
    {
        if (!order.Lines.Any() ||
            order.Status == SalesOrderStatus.Draft ||
            order.Status == SalesOrderStatus.Cancelled)
        {
            order.InvoiceStatus = OrderInvoiceStatus.NothingToInvoice;
            return;
        }

        var invoicedLines = order.Lines.Count(l => l.InvoicedQuantity >= l.OrderedQuantity);
        var partialLines  = order.Lines.Count(l => l.InvoicedQuantity > 0 && l.InvoicedQuantity < l.OrderedQuantity);
        var totalLines    = order.Lines.Count;

        order.InvoiceStatus = invoicedLines == totalLines
            ? OrderInvoiceStatus.FullyInvoiced
            : (invoicedLines > 0 || partialLines > 0)
                ? OrderInvoiceStatus.PartiallyInvoiced
                : OrderInvoiceStatus.ToInvoice;
    }

    private async Task PublishInvoiceSentEventAsync(SalesInvoice invoice, SendInvoiceDto dto)
    {
        try
        {
            await _events.PublishAsync(new SalesInvoiceSentEvent
            {
                InvoiceId     = invoice.Id,
                InvoiceNumber = invoice.InvoiceNumber,
                ContactId     = invoice.ContactId,
                ContactName   = invoice.ContactName,
                TotalAmount   = invoice.TotalAmount,
                CurrencyCode  = invoice.CurrencyCode,
                DueDate       = invoice.DueDate,
                ToEmails      = dto.ToEmails,
                Subject       = dto.Subject ?? $"Invoice {invoice.InvoiceNumber}",
                Body          = dto.Body,
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Send event failed for Invoice {InvoiceId}", invoice.Id);
        }
    }

    // ── Mapping ───────────────────────────────────────────────────────────────

    private static SalesInvoiceDto MapToDto(SalesInvoice i) => SalesInvoiceService.MapToDto(i);

    private static CreditNoteDto MapCreditNoteToDto(CreditNote cn) => new()
    {
        Id               = cn.Id,
        CreditNoteNumber = cn.CreditNoteNumber,
        SalesInvoiceId   = cn.SalesInvoiceId,
        ContactId        = cn.ContactId,
        ContactName      = cn.ContactName,
        CreditNoteDate   = cn.CreditNoteDate,
        CurrencyCode     = cn.CurrencyCode,
        SubtotalAmount   = cn.SubtotalAmount,
        TaxAmount        = cn.TaxAmount,
        TotalAmount      = cn.TotalAmount,
        Reason           = cn.Reason,
        AccountingJournalEntryId = cn.AccountingJournalEntryId,
        Notes            = cn.Notes,
        Lines = cn.Lines.Select(l => new CreditNoteLineDto
        {
            Id          = l.Id,
            LineNumber  = l.LineNumber,
            ProductId   = l.ProductId,
            ProductCode = l.ProductCode,
            ProductName = l.ProductName,
            Quantity    = l.Quantity,
            UnitOfMeasure = l.UnitOfMeasure,
            UnitPrice   = l.UnitPrice,
            LineAmount  = l.LineAmount,
            TaxCategory = l.TaxCategory,
            TaxRate     = l.TaxRate,
            TaxAmount   = l.TaxAmount,
            TotalAmount = l.TotalAmount,
            Reason      = l.Reason,
        }).ToList(),
    };
}
