using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nexcore.SharedKernel.Api;
using Nexcore.SharedKernel.Events;
using Sales.Application.DTOs;
using Sales.Application.Services.Interfaces;
using Sales.Domain.Entities;
using Sales.Domain.Enums;
using Sales.Infrastructure.Repositories.Interfaces;
using System.Security.Claims;

namespace Sales.Api.Controllers;

[ApiController]
[Route("api/sales/[controller]")]
[Authorize]
public class SalesPaymentController : ControllerBase
{
    private readonly ISalesPaymentRepository _payments;
    private readonly ISalesOrderRepository _orders;
    private readonly ISalesInvoiceRepository _invoices;
    private readonly IPaymentAllocationRepository _allocations;
    private readonly IDocumentSequenceService _sequences;
    private readonly IEventPublisher _events;
    private readonly ILogger<SalesPaymentController> _logger;

    public SalesPaymentController(
        ISalesPaymentRepository payments,
        ISalesOrderRepository orders,
        ISalesInvoiceRepository invoices,
        IPaymentAllocationRepository allocations,
        IDocumentSequenceService sequences,
        IEventPublisher events,
        ILogger<SalesPaymentController> logger)
    {
        _payments    = payments;
        _orders      = orders;
        _invoices    = invoices;
        _allocations = allocations;
        _sequences   = sequences;
        _events      = events;
        _logger      = logger;
    }

    // ── GET ───────────────────────────────────────────────────────────────────

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<SalesPaymentDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            var list = await _payments.GetAllAsync();
            return Ok(new ApiResponse<List<SalesPaymentDto>>
            {
                Success = true,
                Data = list.OrderByDescending(p => p.CreatedAt).Select(MapToDto).ToList(),
                Message = "Payments retrieved",
            });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving payments"); return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving payments" }); }
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<SalesPaymentDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetById(Guid id)
    {
        try
        {
            var payment = await _payments.GetByIdAsync(id);
            if (payment == null) return NotFound(new ApiErrorResponse { Message = "Payment not found" });
            return Ok(new ApiResponse<SalesPaymentDto> { Success = true, Data = MapToDto(payment) });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving payment {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving payment" }); }
    }

    [HttpGet("by-number/{paymentNumber}")]
    [ProducesResponseType(typeof(ApiResponse<SalesPaymentDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByNumber(string paymentNumber)
    {
        try
        {
            var payment = await _payments.GetByNumberAsync(paymentNumber);
            if (payment == null) return NotFound(new ApiErrorResponse { Message = "Payment not found" });
            return Ok(new ApiResponse<SalesPaymentDto> { Success = true, Data = MapToDto(payment) });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving payment {Number}", paymentNumber); return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving payment" }); }
    }

    [HttpGet("by-customer/{customerId}")]
    [ProducesResponseType(typeof(ApiResponse<List<SalesPaymentDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByCustomer(Guid customerId)
    {
        try
        {
            var list = await _payments.GetByContactAsync(customerId);
            return Ok(new ApiResponse<List<SalesPaymentDto>> { Success = true, Data = list.Select(MapToDto).ToList(), Message = "Customer payments" });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving customer payments"); return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving payments" }); }
    }

    [HttpGet("by-order/{orderId}")]
    [ProducesResponseType(typeof(ApiResponse<List<SalesPaymentDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByOrder(Guid orderId)
    {
        try
        {
            var list = await _payments.GetByOrderAsync(orderId);
            return Ok(new ApiResponse<List<SalesPaymentDto>> { Success = true, Data = list.Select(MapToDto).ToList(), Message = "Payments retrieved" });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving payments"); return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving payments" }); }
    }

    /// <summary>All payments allocated to a specific invoice.</summary>
    [HttpGet("by-invoice/{invoiceId}")]
    [ProducesResponseType(typeof(ApiResponse<List<SalesPaymentDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByInvoice(Guid invoiceId)
    {
        try
        {
            var allocations = await _allocations.GetByInvoiceAsync(invoiceId);
            var paymentIds = allocations.Select(a => a.SalesPaymentId).Distinct().ToList();
            var all = await _payments.GetAllAsync();
            var result = all.Where(p => paymentIds.Contains(p.Id)).Select(MapToDto).ToList();
            return Ok(new ApiResponse<List<SalesPaymentDto>> { Success = true, Data = result, Message = "Invoice payments" });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving payments for invoice {InvoiceId}", invoiceId); return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving payments" }); }
    }

    // ── POST /create ──────────────────────────────────────────────────────────

    /// <summary>
    /// Record a payment and allocate it to invoices.
    ///
    /// Allocation rules:
    ///   • If dto.Allocations is provided, use those amounts exactly.
    ///     The sum must equal dto.Amount.
    ///   • If dto.Allocations is empty, auto-allocate FIFO across all unpaid
    ///     invoices for the order (oldest invoice first).
    ///   • For POS walk-in sales with no invoices, pass an empty Allocations
    ///     list — the payment is recorded against the order only.
    ///
    /// After allocation the invoice Status is auto-updated:
    ///   BalanceDue == 0  →  Paid
    ///   BalanceDue &gt;  0  →  PartiallyPaid
    /// The order Status is set to PaidAndClosed when BalanceDue reaches 0.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<SalesPaymentDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateSalesPaymentDto dto)
    {
        try
        {
            if (!ModelState.IsValid) return BadRequest(new ApiErrorResponse { Message = "Invalid input" });
            if (dto.Amount <= 0) return BadRequest(new ApiErrorResponse { Message = "Amount must be greater than zero" });

            var order = await _orders.GetByIdAsync(dto.SalesOrderId);
            if (order == null) return NotFound(new ApiErrorResponse { Message = "Order not found" });

            // ── Validate manual allocations if provided ───────────────────────
            if (dto.Allocations.Count > 0)
            {
                var total = dto.Allocations.Sum(a => a.AllocatedAmount);
                if (Math.Abs(total - dto.Amount) > 0.01m)
                    return BadRequest(new ApiErrorResponse
                    {
                        Message = $"Allocation total ({total:N2}) must equal payment amount ({dto.Amount:N2})",
                    });
            }

            // ── Create payment record ─────────────────────────────────────────
            var paySeq = await _sequences.GetNextNumberAsync(DocumentType.Payment);
            var payment = new SalesPayment
            {
                PaymentNumber        = paySeq.Code,
                Code                 = paySeq.Code,
                CodeInt              = paySeq.CodeInt,
                SalesOrderId         = dto.SalesOrderId,
                ContactId            = dto.ContactId,
                Amount               = dto.Amount,
                CurrencyCode         = dto.CurrencyCode,
                ExchangeRate         = dto.ExchangeRate,
                PaymentMethod        = dto.PaymentMethod,
                ReferenceNumber      = dto.ReferenceNumber,
                BankName             = dto.BankName,
                GatewayTransactionId = dto.GatewayTransactionId,
                Notes                = dto.Notes,
                ConfirmedAt          = DateTime.UtcNow,
            };

            await _payments.AddAsync(payment);
            await _payments.SaveChangesAsync();

            // ── Resolve allocations (manual or FIFO auto) ─────────────────────
            var allocationPlan = await BuildAllocationPlanAsync(payment.Id, dto);

            // ── Apply allocations — update invoice balances and statuses ──────
            foreach (var plan in allocationPlan)
            {
                var invoice = await _invoices.GetByIdAsync(plan.SalesInvoiceId);
                if (invoice == null) continue;

                await _allocations.AddAsync(new PaymentAllocation
                {
                    SalesPaymentId  = payment.Id,
                    SalesInvoiceId  = plan.SalesInvoiceId,
                    AllocatedAmount = plan.AllocatedAmount,
                    AllocatedAt     = DateTime.UtcNow,
                });

                invoice.PaidAmount  += plan.AllocatedAmount;
                invoice.BalanceDue   = invoice.TotalAmount - invoice.PaidAmount;
                invoice.Status       = invoice.BalanceDue <= 0
                    ? InvoiceStatus.Paid
                    : InvoiceStatus.PartiallyPaid;

                _invoices.Update(invoice);
            }

            await _allocations.SaveChangesAsync();

            // ── Update order financials ───────────────────────────────────────
            order.PaidAmount += dto.Amount;
            order.BalanceDue  = order.TotalAmount - order.PaidAmount;

            if (order.BalanceDue <= 0)
            {
                order.Status     = SalesOrderStatus.PaidAndClosed;
                order.ClosedDate = DateTime.UtcNow;
            }
            else if (order.PaidAmount > 0 && order.Status == SalesOrderStatus.Confirmed)
            {
                order.Status = SalesOrderStatus.PartiallyPaid;
            }

            _orders.Update(order);
            await _orders.SaveChangesAsync();

            // ── Publish accounting event (non-blocking) ───────────────────────
            await PublishPaymentEventAsync(payment, allocationPlan);

            _logger.LogInformation(
                "Payment {PaymentNumber} ({Amount:C}) recorded for order {OrderId} — {Count} allocation(s)",
                payment.PaymentNumber, payment.Amount, dto.SalesOrderId, allocationPlan.Count);

            return StatusCode(201, new ApiResponse<SalesPaymentDto>
            {
                Success = true,
                Data    = MapToDto(payment),
                Message = $"Payment recorded — {allocationPlan.Count} invoice(s) updated",
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording payment");
            return StatusCode(500, new ApiErrorResponse { Message = "Error recording payment" });
        }
    }

    // ── private helpers ───────────────────────────────────────────────────────

    /// <summary>
    /// Returns the final list of (InvoiceId, AllocatedAmount) pairs.
    /// Uses dto.Allocations if provided; otherwise distributes FIFO.
    /// </summary>
    private async Task<List<CreatePaymentAllocationDto>> BuildAllocationPlanAsync(
        Guid paymentId, CreateSalesPaymentDto dto)
    {
        if (dto.Allocations.Count > 0)
            return dto.Allocations;

        // Auto-allocate FIFO across unpaid invoices
        var unpaid = await _invoices.GetUnpaidByOrderAsync(dto.SalesOrderId);
        if (unpaid.Count == 0) return [];

        var plan = new List<CreatePaymentAllocationDto>();
        var remaining = dto.Amount;

        foreach (var invoice in unpaid)
        {
            if (remaining <= 0) break;
            var apply = Math.Min(remaining, invoice.BalanceDue);
            plan.Add(new CreatePaymentAllocationDto
            {
                SalesInvoiceId  = invoice.Id,
                AllocatedAmount = apply,
            });
            remaining -= apply;
        }

        return plan;
    }

    private async Task PublishPaymentEventAsync(
        SalesPayment payment,
        List<CreatePaymentAllocationDto> allocations)
    {
        try
        {
            var companyId      = Guid.TryParse(User.FindFirstValue("CompanyId"),      out var c)  ? c  : Guid.Empty;
            var branchId       = Guid.TryParse(User.FindFirstValue("BranchId"),       out var b)  ? b  : Guid.Empty;
            var businessUnitId = Guid.TryParse(User.FindFirstValue("BusinessUnitId"), out var bu) ? bu : Guid.Empty;
            var userId         = Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var u) ? u : Guid.Empty;

            await _events.PublishAsync(new SalesPaymentReceivedEvent
            {
                PaymentId         = payment.Id,
                PaymentNumber     = payment.PaymentNumber,
                SalesOrderId      = payment.SalesOrderId,
                ContactId         = payment.ContactId,
                AmountPaid        = payment.Amount,
                PaymentMethod     = payment.PaymentMethod ?? "Unknown",
                CurrencyCode      = payment.CurrencyCode,
                ExchangeRate      = payment.ExchangeRate,
                PaymentDate       = payment.PaymentDate,
                InvoiceAllocations = allocations
                    .Select(a => new PaymentInvoiceAllocation(a.SalesInvoiceId, a.AllocatedAmount))
                    .ToList(),
                CompanyId         = companyId,
                BranchId          = branchId,
                BusinessUnitId    = businessUnitId,
                CreatedByUserId   = userId,
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Accounting event failed for Payment {PaymentId}", payment.Id);
        }
    }

    private static SalesPaymentDto MapToDto(SalesPayment p) => new()
    {
        Id                   = p.Id,
        PaymentNumber        = p.PaymentNumber,
        SalesOrderId         = p.SalesOrderId,
        OrderNumber          = p.SalesOrder?.OrderNumber,
        PosTransactionId     = p.PosTransactionId,
        ContactId            = p.ContactId,
        PaymentDate          = p.PaymentDate,
        Amount               = p.Amount,
        CurrencyCode         = p.CurrencyCode,
        ExchangeRate         = p.ExchangeRate,
        PaymentMethod        = p.PaymentMethod,
        ReferenceNumber      = p.ReferenceNumber,
        BankName             = p.BankName,
        GatewayTransactionId = p.GatewayTransactionId,
        GatewayResponse      = p.GatewayResponse,
        IsRefunded           = p.IsRefunded,
        RefundedAmount       = p.RefundedAmount,
        RefundedAt           = p.RefundedAt,
        AccountingReceiptId  = p.AccountingReceiptId,
        Notes                = p.Notes,
        Allocations          = p.Allocations.Select(a => new PaymentAllocationDto
        {
            Id              = a.Id,
            SalesInvoiceId  = a.SalesInvoiceId,
            InvoiceNumber   = a.Invoice?.InvoiceNumber,
            AllocatedAmount = a.AllocatedAmount,
            AllocatedAt     = a.AllocatedAt,
        }).ToList(),
    };
}
