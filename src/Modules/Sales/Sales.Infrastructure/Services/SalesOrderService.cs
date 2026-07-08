using Microsoft.Extensions.Logging;
using Nexcore.SharedKernel.Events;
using Sales.Application.DTOs;
using Sales.Application.Services.Interfaces;
using Sales.Domain.Entities;
using Sales.Domain.Enums;
using Sales.Infrastructure.Repositories.Interfaces;

namespace Sales.Infrastructure.Services;

/// <summary>
/// SalesOrder service — handles order creation, placement, status transitions, and totals.
/// Publishes accounting events so the Accounting module can post the corresponding
/// GL journal entries in a fully decoupled manner.
/// </summary>
public class SalesOrderService : ISalesOrderService
{
    private readonly ISalesOrderRepository _orders;
    private readonly ISalesInvoiceRepository _invoices;
    private readonly IPricingService _pricing;
    private readonly IDocumentSequenceService _sequences;
    private readonly IEventPublisher _events;
    private readonly ILogger<SalesOrderService> _logger;

    public SalesOrderService(
        ISalesOrderRepository orders,
        ISalesInvoiceRepository invoices,
        IPricingService pricing,
        IDocumentSequenceService sequences,
        IEventPublisher events,
        ILogger<SalesOrderService> logger)
    {
        _orders    = orders;
        _invoices  = invoices;
        _pricing   = pricing;
        _sequences = sequences;
        _events    = events;
        _logger    = logger;
    }

    public async Task<List<SalesOrderDto>> GetAllAsync()
    {
        var list = await _orders.GetAllAsync();
        return list.OrderByDescending(o => o.CreatedAt).Select(MapToDto).ToList();
    }

    public async Task<SalesOrderDto?> GetByIdAsync(Guid id)
    {
        var order = await _orders.GetWithFullDetailsAsync(id);
        return order is null ? null : MapToDto(order);
    }

    public async Task<SalesOrderDto?> GetByNumberAsync(string orderNumber)
    {
        var order = await _orders.GetByNumberAsync(orderNumber);
        return order is null ? null : MapToDto(order);
    }

    public async Task<SalesOrderDto?> GetWithFullDetailsAsync(Guid id)
    {
        var order = await _orders.GetWithFullDetailsAsync(id);
        return order is null ? null : MapToDto(order);
    }

    public async Task<List<SalesOrderDto>> GetByStatusAsync(SalesOrderStatus status)
    {
        var list = await _orders.GetByStatusAsync(status);
        return list.Select(MapToDto).ToList();
    }

    public async Task<List<SalesOrderDto>> GetByChannelAsync(SalesChannel channel)
    {
        var list = await _orders.GetByChannelAsync(channel);
        return list.Select(MapToDto).ToList();
    }

    public async Task<List<SalesOrderDto>> GetActiveDraftsByContactAsync(Guid contactId)
    {
        var list = await _orders.GetActiveDraftsByContactAsync(contactId);
        return list.Select(MapToDto).ToList();
    }

    public async Task<List<SalesOrderDto>> GetByCustomerAsync(Guid customerId)
    {
        var list = await _orders.GetByContactAsync(customerId);
        return list.Select(MapToDto).ToList();
    }

    public async Task<List<SalesOrderDto>> GetStoreQueueAsync(Guid storeId)
    {
        var list = await _orders.GetStoreOrderQueueAsync(storeId);
        return list.Select(MapToDto).ToList();
    }

    public Task<int> GetPendingOnlineOrderCountAsync(Guid storeId)
        => _orders.GetPendingOnlineOrderCountAsync(storeId);

    public async Task<SalesOrderDto> CreateAsync(CreateSalesOrderDto dto)
    {
        if (dto.ContactId == null || dto.ContactId == Guid.Empty)
            throw new InvalidOperationException("Customer (ContactId) is required to create a Sales Order");
        if (string.IsNullOrWhiteSpace(dto.ContactName))
            throw new InvalidOperationException("Customer name (ContactName) is required to create a Sales Order");

        // Server-authoritative pricing: resolve prices + promotions + coupon centrally,
        // so an order created from POS, the app, or B2B is priced identically.
        var pricing = await _pricing.PriceOrderAsync(new PriceOrderRequestDto
        {
            ContactId           = dto.ContactId,
            PriceListId         = dto.PriceListId,
            PosStoreId          = dto.OriginBranchId,
            CouponCode          = dto.CouponCode,
            SalesChannel        = dto.SalesChannel,
            CustomerCountryCode = dto.BillToCountry,
            Lines = dto.Lines.Select(l => new PriceOrderLineRequestDto
            {
                ProductId     = l.ProductId,
                ProductCode   = l.ProductCode,
                ProductName   = l.ProductName,
                VariantId     = l.VariantId,
                Quantity      = l.Quantity,
                UnitOfMeasure = l.UnitOfMeasure,
                TaxCategory   = l.TaxCategory,
            }).ToList(),
        });

        var orderSeq = await _sequences.GetNextNumberAsync(DocumentType.SalesOrder);
        var order = new SalesOrder
        {
            OrderNumber          = orderSeq.Code,
            Code                 = orderSeq.Code,
            CodeInt              = orderSeq.CodeInt,
            OrderName            = dto.OrderName,
            OfflineOrderNumber   = dto.OfflineOrderNumber,
            ContactId            = dto.ContactId,
            ContactName          = dto.ContactName,
            CustomerPONumber     = dto.CustomerPONumber,
            CustomerPODate       = dto.CustomerPODate,
            SalesChannel         = dto.SalesChannel,
            FulfillmentType      = dto.FulfillmentType,
            OriginBranchId     = dto.OriginBranchId,
            OriginPosTerminalId  = dto.OriginPosTerminalId,
            OriginPosCashierId   = dto.OriginPosCashierId,
            OriginPosSessionId   = dto.OriginPosSessionId,
            QuotationId          = dto.QuotationId,
            SalesAgreementId     = dto.SalesAgreementId,
            PriceListId          = dto.PriceListId,
            CurrencyCode         = dto.CurrencyCode,
            ExchangeRate         = dto.ExchangeRate,
            CouponCode           = dto.CouponCode,
            LoyaltyPointsRedeemed = dto.LoyaltyPointsRedeemed,
            PaymentTerms         = dto.PaymentTerms,
            Incoterm             = dto.Incoterm,
            IncotermLocation     = dto.IncotermLocation,
            BillToName           = dto.BillToName,
            BillToStreet         = dto.BillToStreet,
            BillToCity           = dto.BillToCity,
            BillToState          = dto.BillToState,
            BillToPostalCode     = dto.BillToPostalCode,
            BillToCountry        = dto.BillToCountry,
            ShipToAddressId      = dto.ShipToAddressId,
            ShippingAmount       = dto.ShippingAmount,
            SalesRepId           = dto.SalesRepId,
            SalesTerritoryId     = dto.SalesTerritoryId,
            Notes                = dto.Notes,
            InternalNotes        = dto.InternalNotes,
            TermsAndConditions   = dto.TermsAndConditions,
            ExpiresAt            = dto.ExpiresAt,
            Status               = SalesOrderStatus.Draft,
            Lines = dto.Lines.Select((l, i) => new SalesOrderLine
            {
                LineNumber          = i + 1,
                ProductId           = l.ProductId,
                ProductCode         = l.ProductCode,
                ProductName         = l.ProductName,
                ProductDescription  = l.ProductDescription,
                ProductImageUrl     = l.ProductImageUrl,
                VariantId           = l.VariantId,
                VariantName         = l.VariantName,
                OrderedQuantity     = l.Quantity,
                UnitOfMeasure       = l.UnitOfMeasure,
                WarehouseId         = l.WarehouseId,
                // ── Pricing comes from the central engine, not the client ──
                UnitPrice           = pricing.Lines[i].ListUnitPrice,
                DiscountPercentage  = l.DiscountPercentage,
                DiscountAmount      = pricing.Lines[i].DiscountAmount,
                NetUnitPrice        = pricing.Lines[i].NetUnitPrice,
                TaxCategory         = l.TaxCategory,
                TaxRate             = pricing.Lines[i].TaxRate,
                TaxAmount           = pricing.Lines[i].TaxAmount,
                Notes               = l.SpecialInstructions,
                RequestedDeliveryDate = l.RequestedDeliveryDate,
                LineAmount          = pricing.Lines[i].LineAmount,
                TotalAmount         = pricing.Lines[i].LineAmount,
                Addons = l.Addons.Select(a => new SalesOrderLineAddon
                {
                    AddonProductId = a.AddonProductId,
                    AddonName      = a.AddonName,
                    UnitPrice      = a.AddonPrice,
                    Quantity       = a.Quantity,
                }).ToList(),
            }).ToList(),
        };

        order.SubtotalAmount       = order.Lines.Sum(l => l.TotalAmount);
        order.CouponDiscountAmount = pricing.CouponDiscountAmount;
        order.DiscountAmount       = pricing.DiscountAmount;
        order.TaxAmount            = pricing.TaxAmount;
        order.TotalAmount          = order.SubtotalAmount - order.CouponDiscountAmount + order.TaxAmount + order.ShippingAmount;
        order.BalanceDue           = order.TotalAmount - order.PaidAmount;

        await _orders.AddAsync(order);
        await _orders.SaveChangesAsync();

        _logger.LogInformation("Sales order created: {OrderNumber} (ID: {Id})", order.OrderNumber, order.Id);
        return MapToDto(order);
    }

    public async Task<SalesOrderDto> RepriceAsync(Guid orderId)
    {
        var order = await _orders.GetWithFullDetailsAsync(orderId)
            ?? throw new InvalidOperationException("Order not found");

        await RepriceEntityAsync(order);
        await _orders.SaveChangesAsync();
        return MapToDto(order);
    }

    /// <summary>
    /// Re-prices a tracked order in place (no save). Skips orders that already carry a payment
    /// so settled totals are never disturbed.
    /// </summary>
    private async Task RepriceEntityAsync(SalesOrder order)
    {
        if (order.PaidAmount > 0 || order.Lines.Count == 0) return;

        var ordered = order.Lines.OrderBy(l => l.LineNumber).ToList();

        var pricing = await _pricing.PriceOrderAsync(new PriceOrderRequestDto
        {
            ContactId           = order.ContactId,
            PriceListId         = order.PriceListId,
            PosStoreId          = order.OriginBranchId,
            CouponCode          = order.CouponCode,
            SalesChannel        = order.SalesChannel,
            CustomerCountryCode = order.BillToCountry,
            Lines = ordered.Select(l => new PriceOrderLineRequestDto
            {
                ProductId     = l.ProductId,
                ProductCode   = l.ProductCode,
                ProductName   = l.ProductName,
                VariantId     = l.VariantId,
                Quantity      = l.OrderedQuantity,
                UnitOfMeasure = l.UnitOfMeasure,
                TaxCategory   = l.TaxCategory,
            }).ToList(),
        });

        for (int i = 0; i < ordered.Count && i < pricing.Lines.Count; i++)
        {
            var pl = pricing.Lines[i];
            ordered[i].UnitPrice      = pl.ListUnitPrice;
            ordered[i].DiscountAmount = pl.DiscountAmount;
            ordered[i].NetUnitPrice   = pl.NetUnitPrice;
            ordered[i].LineAmount     = pl.LineAmount;
            ordered[i].TotalAmount    = pl.LineAmount;
            ordered[i].TaxRate        = pl.TaxRate;
            ordered[i].TaxAmount      = pl.TaxAmount;
        }

        order.SubtotalAmount       = order.Lines.Sum(l => l.TotalAmount);
        order.CouponDiscountAmount = pricing.CouponDiscountAmount;
        order.DiscountAmount       = pricing.DiscountAmount;
        order.TaxAmount            = pricing.TaxAmount;
        order.TotalAmount          = order.SubtotalAmount - order.CouponDiscountAmount + order.TaxAmount + order.ShippingAmount;
        order.BalanceDue           = order.TotalAmount - order.PaidAmount;
    }

    public async Task<SalesOrderDto> PlaceOrderAsync(Guid id)
    {
        var order = await _orders.GetWithLinesAsync(id)
            ?? throw new InvalidOperationException("Order not found");

        if (order.Status != SalesOrderStatus.Draft)
            throw new InvalidOperationException($"Order cannot be placed from status {order.Status}");

        // Finalize-time reprice: catch promotions/prices that changed since the basket was built.
        await RepriceEntityAsync(order);

        order.Status = SalesOrderStatus.Placed;
        order.PlacedAt = DateTime.UtcNow;
        order.SubmittedDate = DateTime.UtcNow;

        // Save the status change first on the tracked entity.
        await _orders.SaveChangesAsync();

        // Record the status history as a separate tracked insert to avoid EF
        // collection-state issues when StatusHistory was not included in the load.
        try
        {
            order.StatusHistory.Add(new SalesOrderStatusHistory
            {
                SalesOrderId = order.Id,
                Status = SalesOrderStatus.Placed,
                ChangedAt = DateTime.UtcNow,
                ChangedBy = "Customer",
                Note = "Order placed by customer",
            });
            await _orders.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to write status history for order {Id}", id);
        }

        _logger.LogInformation("Sales order placed: {OrderNumber} (ID: {Id})", order.OrderNumber, id);

        // ── Real-time POS notification: pop the "new order" animation + badge ──
        await PublishOnlineOrderPlacedAsync(order);

        return MapToDto(order);
    }

    public async Task<SalesOrderDto> UpdateStatusAsync(Guid id, UpdateSalesOrderStatusDto dto, string? changedBy)
    {
        // Read-only load (AsNoTracking) — used only for validation, events and the response.
        var order = await _orders.GetByIdAsync(id)
            ?? throw new InvalidOperationException("Order not found");

        var previousStatus = order.Status;

        // Persist the status with a set-based UPDATE. Saving it through the tracked,
        // fully-included order graph raised a DbUpdateConcurrencyException ("expected 1
        // row, affected 0"), so we bypass the change tracker here.
        await _orders.SetStatusAsync(id, dto.Status);

        // Reflect the change on the in-memory object for event publishing / the response.
        order.Status = dto.Status;
        if (dto.Status == SalesOrderStatus.Confirmed) order.ApprovedDate = DateTime.UtcNow;
        if (dto.Status == SalesOrderStatus.Cancelled) { order.CancelledDate = DateTime.UtcNow; order.CancellationReason = dto.Note; }
        if (dto.Status == SalesOrderStatus.Rejected) order.RejectedDate = DateTime.UtcNow;
        if (dto.Status == SalesOrderStatus.Delivered || dto.Status == SalesOrderStatus.PaidAndClosed) order.ClosedDate = DateTime.UtcNow;

        // Status history is audit data — never let its failure abort the status change.
        try
        {
            await _orders.AddStatusHistoryAsync(new SalesOrderStatusHistory
            {
                SalesOrderId = order.Id,
                Status = dto.Status,
                ChangedAt = DateTime.UtcNow,
                ChangedBy = changedBy ?? "System",
                Note = dto.Note,
                Latitude = dto.Latitude,
                Longitude = dto.Longitude,
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to write status history for order {Id}", id);
        }

        // Publish accounting events on key status transitions
        await PublishAccountingEventsAsync(order, dto.Status);

        // ── Soft stock allocation ──
        // Reserve on commit (… → Confirmed): holds stock so it can't be sold twice before shipment.
        // Release on cancel/reject from a committed-but-unshipped status. The reservation is released at
        // shipment by the deduction handler, so completion statuses must NOT release here.
        if (dto.Status == SalesOrderStatus.Confirmed && IsPreCommitment(previousStatus))
            await PublishStockReservationAsync(order.Id, +1);
        else if ((dto.Status == SalesOrderStatus.Cancelled || dto.Status == SalesOrderStatus.Rejected)
                 && IsCommittedUnshipped(previousStatus))
            await PublishStockReservationAsync(order.Id, -1);

        // ── Real-time POS notification ──
        // Online order paid/confirmed at the gateway (e.g. PaymentPending → Placed) → arrival.
        // Online order leaving the unacknowledged queue (Placed → Confirmed/Rejected/Cancelled) → badge refresh.
        if (order.SalesChannel.IsOnlineOrderChannel())
        {
            if (dto.Status == SalesOrderStatus.Placed && previousStatus != SalesOrderStatus.Placed)
                await PublishOnlineOrderPlacedAsync(order);
            else if (previousStatus == SalesOrderStatus.Placed && dto.Status != SalesOrderStatus.Placed)
                await PublishOnlineQueueChangedAsync(order);
        }

        _logger.LogInformation("Sales order status updated to {Status}: {Id}", dto.Status, id);
        return MapToDto(order);
    }

    // Statuses BEFORE an order is committed (stock not yet reserved).
    private static bool IsPreCommitment(SalesOrderStatus s) =>
        s is SalesOrderStatus.Draft or SalesOrderStatus.PosParked or SalesOrderStatus.PaymentPending
          or SalesOrderStatus.Placed or SalesOrderStatus.PendingApproval or SalesOrderStatus.OnHold;

    // Statuses where stock IS reserved but not yet shipped (so a cancel must release the reservation).
    private static bool IsCommittedUnshipped(SalesOrderStatus s) =>
        s is SalesOrderStatus.Confirmed or SalesOrderStatus.Preparing or SalesOrderStatus.ReadyForPickup
          or SalesOrderStatus.OutForDelivery or SalesOrderStatus.PartiallyDelivered;

    /// <summary>
    /// Publishes a soft stock reservation change for the order's lines. <paramref name="sign"/> is +1 to
    /// reserve (on commit) or -1 to release (on cancel). Adjusts only InventoryBalance.QuantityReserved /
    /// QuantityAvailable — no physical movement. Non-blocking: a failure never rolls back the status change.
    /// </summary>
    private async Task PublishStockReservationAsync(Guid orderId, int sign)
    {
        try
        {
            var order = await _orders.GetWithLinesAsync(orderId);
            if (order?.Lines is null || order.Lines.Count == 0) return;

            var lines = order.Lines
                .Where(l => l.ProductId != Guid.Empty && l.OrderedQuantity > 0)
                .Select(l => new StockReservationLine
                {
                    ProductId   = l.ProductId,
                    VariantId   = l.VariantId,
                    WarehouseId = l.WarehouseId,
                    Quantity    = sign * l.OrderedQuantity,
                })
                .ToList();

            if (lines.Count == 0) return;

            await _events.PublishAsync(new StockReservationChangedEvent
            {
                ReferenceId     = order.Id,
                ReferenceType   = "SalesOrder",
                WarehouseId     = Guid.Empty,   // resolved per-line
                CompanyId       = order.CompanyId,
                BranchId        = order.BranchId,
                BusinessUnitId  = order.BusinessUnitId,
                CreatedByUserId = order.CreatedByUserId,
                Lines           = lines,
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Stock reservation (sign {Sign}) failed for order {OrderId}", sign, orderId);
        }
    }

    /// <summary>
    /// Publishes <see cref="OnlineOrderPlacedEvent"/> for an online order that just became
    /// visible to its store, so the POS screen plays the arrival animation and bumps the badge.
    /// Non-blocking: a notification failure must never roll back order placement.
    /// </summary>
    private async Task PublishOnlineOrderPlacedAsync(SalesOrder order)
    {
        if (!order.SalesChannel.IsOnlineOrderChannel()) return;

        if (order.OriginBranchId is not { } storeId || storeId == Guid.Empty)
        {
            _logger.LogWarning(
                "Online order {OrderNumber} placed without OriginBranchId — cannot route to a store POS screen",
                order.OrderNumber);
            return;
        }

        try
        {
            await _events.PublishAsync(new OnlineOrderPlacedEvent
            {
                OrderId         = order.Id,
                OrderNumber     = order.OrderNumber,
                StoreId         = storeId,
                ContactId       = order.ContactId,
                ContactName     = order.ContactName,
                ItemCount       = order.Lines?.Count ?? 0,
                TotalAmount     = order.TotalAmount,
                CurrencyCode    = order.CurrencyCode,
                SalesChannel    = (int)order.SalesChannel,
                FulfillmentType = (int)order.FulfillmentType,
                PlacedAt        = order.PlacedAt ?? DateTime.UtcNow,
                CompanyId       = order.CompanyId,
                BranchId        = order.BranchId,
                BusinessUnitId  = order.BusinessUnitId,
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish OnlineOrderPlacedEvent for order {OrderId}", order.Id);
        }
    }

    /// <summary>
    /// Publishes <see cref="OnlineOrderQueueChangedEvent"/> so the store's POS screen re-renders
    /// the pending-order badge (no arrival animation). Non-blocking.
    /// </summary>
    private async Task PublishOnlineQueueChangedAsync(SalesOrder order)
    {
        if (order.OriginBranchId is not { } storeId || storeId == Guid.Empty) return;

        try
        {
            await _events.PublishAsync(new OnlineOrderQueueChangedEvent
            {
                StoreId        = storeId,
                CompanyId      = order.CompanyId,
                BranchId       = order.BranchId,
                BusinessUnitId = order.BusinessUnitId,
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish OnlineOrderQueueChangedEvent for order {OrderId}", order.Id);
        }
    }

    /// <summary>
    /// Fires the appropriate accounting events based on the new order status.
    /// This keeps the accounting integration decoupled from order logic.
    /// </summary>
    private async Task PublishAccountingEventsAsync(SalesOrder order, SalesOrderStatus newStatus)
    {
        try
        {
            // POS immediate sale - fire when a POS walk-in order is paid and closed
            if (newStatus == SalesOrderStatus.PaidAndClosed
                && order.SalesChannel == SalesChannel.PosWalkIn)
            {
                // Build stock deduction lines re-used for accounting total
                var totalAmount = order.TotalAmount;

                // The PosTransactionCompletedEvent is already fired by the POS terminal
                // controller. We fire SalesPaymentReceivedEvent to clear the AR balance
                // created when the order was placed, or skip if it was anonymous.
                if (order.PaidAmount > 0m)
                {
                    var paySeq = await _sequences.GetNextNumberAsync(DocumentType.Payment);
                    await _events.PublishAsync(new SalesPaymentReceivedEvent
                    {
                        PaymentId          = Guid.NewGuid(),
                        PaymentNumber      = paySeq.Code,
                        SalesOrderId       = order.Id,
                        ContactId          = order.ContactId,
                        AmountPaid         = order.PaidAmount,
                        PaymentMethod      = "Cash",
                        CurrencyCode       = order.CurrencyCode,
                        PaymentDate        = order.ClosedDate ?? DateTime.UtcNow,
                        CompanyId          = order.CompanyId,
                        BranchId           = order.BranchId,
                        BusinessUnitId     = order.BusinessUnitId,
                        CreatedByUserId    = order.CreatedByUserId,
                    });
                }
            }
        }
        catch (Exception ex)
        {
            // Non-blocking - accounting posting failure must never roll back a sales operation
            _logger.LogError(ex,
                "Failed to publish accounting event for Order {OrderId} status {Status}",
                order.Id, newStatus);
        }
    }

    public async Task DeleteAsync(Guid id)
    {
        var order = await _orders.GetByIdAsync(id)
            ?? throw new InvalidOperationException("Order not found");

        if (order.Status != SalesOrderStatus.Draft && order.Status != SalesOrderStatus.PosParked)
            throw new InvalidOperationException("Only Draft orders can be deleted");

        _orders.Delete(order);
        await _orders.SaveChangesAsync();

        _logger.LogInformation("Sales order deleted: {Id}", id);
    }

    // Mapping

    internal static SalesOrderDto MapToDto(SalesOrder o) => new()
    {
        // Identity
        Id              = o.Id,
        OrderNumber     = o.OrderNumber,
        OrderName       = o.OrderName,
        // Customer
        ContactId       = o.ContactId,
        ContactName     = o.ContactName,
        CustomerPONumber = o.CustomerPONumber,
        CustomerPODate  = o.CustomerPODate,
        // Status & Dates
        Status                  = o.Status,
        SalesChannel            = o.SalesChannel,
        FulfillmentType         = o.FulfillmentType,
        OrderDate               = o.OrderDate,
        PlacedAt                = o.PlacedAt,
        RequestedDeliveryDate   = o.RequestedDeliveryDate,
        ConfirmedDeliveryDate   = o.ConfirmedDeliveryDate,
        SubmittedDate           = o.SubmittedDate,
        ApprovedDate            = o.ApprovedDate,
        ClosedDate              = o.ClosedDate,
        CancelledDate           = o.CancelledDate,
        RejectedDate            = o.RejectedDate,
        ExpiresAt               = o.ExpiresAt,
        // Source
        QuotationId          = o.QuotationId,
        SalesAgreementId     = o.SalesAgreementId,
        OriginBranchId     = o.OriginBranchId,
        OriginPosTerminalId  = o.OriginPosTerminalId,
        OriginPosCashierId   = o.OriginPosCashierId,
        OriginPosSessionId   = o.OriginPosSessionId,
        // Pricing
        PriceListId            = o.PriceListId,
        CurrencyCode           = o.CurrencyCode,
        ExchangeRate           = o.ExchangeRate,
        CouponCode             = o.CouponCode,
        CouponDiscountAmount   = o.CouponDiscountAmount,
        LoyaltyPointsRedeemed  = o.LoyaltyPointsRedeemed,
        LoyaltyPointsEarned    = o.LoyaltyPointsEarned,
        PaymentTerms           = o.PaymentTerms,
        Incoterm               = o.Incoterm,
        IncotermLocation       = o.IncotermLocation,
        // Financials
        SubtotalAmount  = o.SubtotalAmount,
        DiscountAmount  = o.DiscountAmount,
        TaxAmount       = o.TaxAmount,
        ShippingAmount  = o.ShippingAmount,
        TipAmount       = o.TipAmount,
        TotalAmount     = o.TotalAmount,
        PaidAmount      = o.PaidAmount,
        BalanceDue      = o.BalanceDue,
        // Credit
        CreditCheckPassed = o.CreditCheckPassed,
        CreditCheckDate   = o.CreditCheckDate,
        // Billing Address
        BillToName       = o.BillToName,
        BillToStreet     = o.BillToStreet,
        BillToCity       = o.BillToCity,
        BillToState      = o.BillToState,
        BillToPostalCode = o.BillToPostalCode,
        BillToCountry    = o.BillToCountry,
        ShipToAddressId = o.ShipToAddressId,
        // Ownership
        SalesRepId       = o.SalesRepId,
        SalesTerritoryId = o.SalesTerritoryId,
        // Notes
        Notes              = o.Notes,
        InternalNotes      = o.InternalNotes,
        TermsAndConditions = o.TermsAndConditions,
        CancellationReason = o.CancellationReason,
        // Invoicing
        InvoiceStatus  = o.InvoiceStatus,
        InvoicePolicy  = o.InvoicePolicy,
        IsLocked       = o.IsLocked,
        // Lines
        Lines = o.Lines.Select(l => new SalesOrderLineDto
        {
            Id                  = l.Id,
            LineNumber          = l.LineNumber,
            ProductId           = l.ProductId,
            ProductCode         = l.ProductCode,
            ProductName         = l.ProductName,
            ProductDescription  = l.ProductDescription,
            ProductImageUrl     = l.ProductImageUrl,
            VariantId           = l.VariantId,
            VariantName         = l.VariantName,
            Quantity            = l.OrderedQuantity,
            DeliveredQuantity   = l.DeliveredQuantity,
            InvoicedQuantity    = l.InvoicedQuantity,
            CancelledQuantity   = l.CancelledQuantity,
            UnitOfMeasure       = l.UnitOfMeasure,
            WarehouseId         = l.WarehouseId,
            UnitPrice           = l.UnitPrice,
            DiscountPercentage  = l.DiscountPercentage,
            DiscountAmount      = l.DiscountAmount,
            NetUnitPrice        = l.NetUnitPrice,
            LineAmount          = l.LineAmount,
            TaxCategory         = l.TaxCategory,
            TaxRate             = l.TaxRate,
            TaxAmount           = l.TaxAmount,
            TotalAmount         = l.TotalAmount,
            RequestedDeliveryDate  = l.RequestedDeliveryDate,
            ConfirmedDeliveryDate  = l.ConfirmedDeliveryDate,
            ActualDeliveryDate     = l.ActualDeliveryDate,
            LineStatus          = l.LineStatus,
            SpecialInstructions = l.Notes,
            Addons = l.Addons.Select(a => new SalesOrderLineAddonDto
            {
                Id             = a.Id,
                AddonProductId = a.AddonProductId,
                AddonName      = a.AddonName,
                AddonPrice     = a.UnitPrice,
                Quantity       = a.Quantity,
            }).ToList(),
        }).ToList(),
    };

    // ── To Invoice queue ──────────────────────────────────────────────────────

    public async Task<List<SalesOrderDto>> GetOrdersToInvoiceAsync()
    {
        var list = await _orders.GetOrdersToInvoiceAsync();
        return list.Select(MapToDto).ToList();
    }

    // ── Create Invoice from Order ─────────────────────────────────────────────

    public async Task<SalesInvoiceDto> CreateInvoiceFromOrderAsync(Guid orderId, CreateInvoiceFromOrderDto dto)
    {
        var order = await _orders.GetWithFullDetailsAsync(orderId)
            ?? throw new InvalidOperationException("Order not found");

        if (order.IsLocked)
            throw new InvalidOperationException("Order is locked and cannot be invoiced");

        var invoiceableStatuses = new HashSet<SalesOrderStatus>
        {
            SalesOrderStatus.Confirmed, SalesOrderStatus.Preparing,
            SalesOrderStatus.ReadyForPickup, SalesOrderStatus.OutForDelivery,
            SalesOrderStatus.PartiallyDelivered, SalesOrderStatus.FullyDelivered,
            SalesOrderStatus.PartiallyPaid,
        };
        if (!invoiceableStatuses.Contains(order.Status))
            throw new InvalidOperationException($"Cannot invoice an order with status '{order.Status}'");

        var invSeq = await _sequences.GetNextNumberAsync(DocumentType.Invoice);
        var invoice = new SalesInvoice
        {
            InvoiceNumber  = invSeq.Code,
            Code           = invSeq.Code,
            CodeInt        = invSeq.CodeInt,
            SalesOrderId   = order.Id,
            ContactId      = order.ContactId,
            ContactName    = order.ContactName,
            DueDate        = dto.DueDate ?? DateTime.UtcNow.AddDays(30),
            CurrencyCode   = order.CurrencyCode,
            ExchangeRate   = order.ExchangeRate,
            PaymentTerms   = order.PaymentTerms,
            BillToName     = order.BillToName,
            BillToStreet   = order.BillToStreet,
            BillToCity     = order.BillToCity,
            BillToState    = order.BillToState,
            BillToPostalCode = order.BillToPostalCode,
            BillToCountry  = order.BillToCountry,
            Notes          = dto.Notes,
            CustomerReference = dto.CustomerReference,
            Status         = InvoiceStatus.Draft,
        };

        if (dto.InvoiceType == CreateInvoiceType.Regular)
        {
            var lines = BuildRegularLines(order);
            if (lines.Count == 0)
                throw new InvalidOperationException("No uninvoiced quantities remaining on this order");

            // Product subtotal/tax captured before negative adjustment lines are appended.
            var productSubtotal = lines.Sum(l => l.LineAmount);
            var productTax      = lines.Sum(l => l.TaxAmount);

            // Deduct existing down payments as a negative line
            var downTotal = await GetIssuedDownPaymentTotalAsync(order.Id);
            if (downTotal > 0)
                lines.Add(new SalesInvoiceLine
                {
                    LineNumber  = lines.Count + 1,
                    ProductCode = "DOWN-PMT",
                    ProductName = "Down Payment Deduction",
                    Quantity    = 1,
                    UnitPrice   = -downTotal,
                    LineAmount  = -downTotal,
                    TaxAmount   = 0,
                    TotalAmount = -downTotal,
                    IsDownPayment = true,
                });

            // Apply the order-level coupon as a negative discount line so the invoice total,
            // the POS charge, and the AR journal all reflect it. The contra line posts as
            // negative revenue, keeping the accounting entry balanced (DR AR = CR revenue + tax).
            if (order.CouponDiscountAmount > 0)
                lines.Add(new SalesInvoiceLine
                {
                    LineNumber  = lines.Count + 1,
                    ProductCode = "COUPON",
                    ProductName = string.IsNullOrWhiteSpace(order.CouponCode)
                                    ? "Coupon Discount"
                                    : $"Coupon Discount ({order.CouponCode})",
                    Quantity    = 1,
                    UnitPrice   = -order.CouponDiscountAmount,
                    LineAmount  = -order.CouponDiscountAmount,
                    TaxAmount   = 0,
                    TotalAmount = -order.CouponDiscountAmount,
                });

            invoice.Lines           = lines;
            invoice.SubtotalAmount  = productSubtotal;
            invoice.DiscountAmount  = order.CouponDiscountAmount;
            invoice.TaxAmount       = productTax;
            invoice.ShippingAmount  = order.ShippingAmount;
            invoice.TotalAmount     = lines.Sum(l => l.TotalAmount) + order.ShippingAmount;
            invoice.BalanceDue      = invoice.TotalAmount;

            // Mark quantities as invoiced on order lines
            foreach (var ol in order.Lines)
            {
                var qty = GetQtyToInvoice(ol, order.InvoicePolicy);
                if (qty > 0) ol.InvoicedQuantity += qty;
            }
        }
        else
        {
            decimal amount = dto.InvoiceType == CreateInvoiceType.DownPaymentPercentage
                ? order.TotalAmount * (dto.DownPaymentPercentage
                    ?? throw new InvalidOperationException("DownPaymentPercentage is required")) / 100m
                : dto.DownPaymentAmount
                    ?? throw new InvalidOperationException("DownPaymentAmount is required");

            if (amount <= 0) throw new InvalidOperationException("Down payment amount must be positive");

            invoice.Lines = [new SalesInvoiceLine
            {
                LineNumber  = 1,
                ProductCode = "DOWN-PMT",
                ProductName = dto.InvoiceType == CreateInvoiceType.DownPaymentPercentage
                    ? $"Down Payment ({dto.DownPaymentPercentage:0.##}%)"
                    : "Down Payment (Fixed Amount)",
                Quantity    = 1,
                UnitPrice   = amount,
                LineAmount  = amount,
                TaxAmount   = 0,
                TotalAmount = amount,
                IsDownPayment = true,
            }];
            invoice.SubtotalAmount = amount;
            invoice.TaxAmount      = 0;
            invoice.TotalAmount    = amount;
            invoice.BalanceDue     = amount;
        }

        await _invoices.AddAsync(invoice);
        await _invoices.SaveChangesAsync();

        // Update order InvoiceStatus
        RecalculateInvoiceStatus(order);
        _orders.Update(order);
        await _orders.SaveChangesAsync();

        _logger.LogInformation(
            "Invoice {InvoiceNumber} created for order {OrderNumber} (type: {Type})",
            invoice.InvoiceNumber, order.OrderNumber, dto.InvoiceType);

        return MapInvoiceToDto(invoice);
    }

    // ── Lock / Unlock ─────────────────────────────────────────────────────────

    public async Task<SalesOrderDto> LockAsync(Guid id)
    {
        var order = await _orders.GetByIdAsync(id)
            ?? throw new InvalidOperationException("Order not found");

        order.IsLocked = true;
        _orders.Update(order);
        await _orders.SaveChangesAsync();
        return MapToDto(order);
    }

    public async Task<SalesOrderDto> UnlockAsync(Guid id)
    {
        var order = await _orders.GetByIdAsync(id)
            ?? throw new InvalidOperationException("Order not found");

        order.IsLocked = false;
        _orders.Update(order);
        await _orders.SaveChangesAsync();
        return MapToDto(order);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static decimal GetQtyToInvoice(SalesOrderLine line, InvoicePolicy policy) =>
        policy == InvoicePolicy.OnDelivery
            ? Math.Max(0, line.DeliveredQuantity - line.InvoicedQuantity)
            : Math.Max(0, line.OrderedQuantity   - line.InvoicedQuantity);

    private static List<SalesInvoiceLine> BuildRegularLines(SalesOrder order)
    {
        var lines = new List<SalesInvoiceLine>();
        var i = 1;
        foreach (var ol in order.Lines)
        {
            var qty = GetQtyToInvoice(ol, order.InvoicePolicy);
            if (qty <= 0) continue;

            // Derive the invoice amounts from the order line's STORED totals (prorated by the
            // quantity being invoiced) rather than recomputing from UnitPrice. Recomputing drifts
            // by sub-cent rounding (e.g. 10 × 113.04 = 1130.40 vs the order's stored 1130.44),
            // which left a tiny residual balance and a false "PartiallyPaid" status. For a full-
            // quantity invoice the ratio is 1, so the invoice total matches the order exactly.
            var fullQty    = ol.OrderedQuantity <= 0 ? 1m : ol.OrderedQuantity;
            var ratio      = qty / fullQty;
            var lineAmount = Math.Round(ol.LineAmount * ratio, 2);
            var taxAmt     = Math.Round(ol.TaxAmount  * ratio, 2);
            var discAmt    = Math.Round(ol.DiscountAmount * ratio, 2);

            lines.Add(new SalesInvoiceLine
            {
                LineNumber       = i++,
                SalesOrderLineId = ol.Id,
                ProductId        = ol.ProductId,
                ProductCode      = ol.ProductCode,
                ProductName      = ol.ProductName,
                Quantity         = qty,
                UnitOfMeasure    = ol.UnitOfMeasure,
                UnitPrice        = ol.UnitPrice,
                DiscountAmount   = discAmt,
                LineAmount       = lineAmount,
                TaxCategory      = ol.TaxCategory,
                TaxRate          = ol.TaxRate,
                TaxAmount        = taxAmt,
                TotalAmount      = lineAmount + taxAmt,
            });
        }
        return lines;
    }

    private async Task<decimal> GetIssuedDownPaymentTotalAsync(Guid salesOrderId)
    {
        var invoices = await _invoices.GetByOrderWithLinesAsync(salesOrderId);
        return invoices
            .Where(i => i.Status != InvoiceStatus.Cancelled)
            .SelectMany(i => i.Lines)
            .Where(l => l.IsDownPayment && l.TotalAmount > 0)
            .Sum(l => l.TotalAmount);
    }

    internal static void RecalculateInvoiceStatus(SalesOrder order)
    {
        if (!order.Lines.Any() ||
            order.Status == SalesOrderStatus.Draft ||
            order.Status == SalesOrderStatus.Cancelled)
        {
            order.InvoiceStatus = OrderInvoiceStatus.NothingToInvoice;
            return;
        }

        var invoicedLines  = order.Lines.Count(l => l.InvoicedQuantity >= l.OrderedQuantity);
        var partialLines   = order.Lines.Count(l => l.InvoicedQuantity > 0 && l.InvoicedQuantity < l.OrderedQuantity);
        var totalLines     = order.Lines.Count;

        if (invoicedLines == totalLines)
            order.InvoiceStatus = OrderInvoiceStatus.FullyInvoiced;
        else if (invoicedLines > 0 || partialLines > 0)
            order.InvoiceStatus = OrderInvoiceStatus.PartiallyInvoiced;
        else
            order.InvoiceStatus = OrderInvoiceStatus.ToInvoice;
    }

    private static SalesInvoiceDto MapInvoiceToDto(SalesInvoice i) => new()
    {
        Id             = i.Id,
        InvoiceNumber  = i.InvoiceNumber,
        SalesOrderId   = i.SalesOrderId,
        ContactId      = i.ContactId,
        ContactName    = i.ContactName,
        DeliveryId     = i.DeliveryId,
        Status         = i.Status,
        InvoiceDate    = i.InvoiceDate,
        DueDate        = i.DueDate,
        CurrencyCode   = i.CurrencyCode,
        ExchangeRate   = i.ExchangeRate,
        PaymentTerms   = i.PaymentTerms,
        SubtotalAmount = i.SubtotalAmount,
        DiscountAmount = i.DiscountAmount,
        TaxAmount      = i.TaxAmount,
        ShippingAmount = i.ShippingAmount,
        TotalAmount    = i.TotalAmount,
        PaidAmount     = i.PaidAmount,
        BalanceDue     = i.BalanceDue,
        BillToName     = i.BillToName,
        BillToStreet   = i.BillToStreet,
        BillToCity     = i.BillToCity,
        BillToState    = i.BillToState,
        BillToPostalCode = i.BillToPostalCode,
        BillToCountry  = i.BillToCountry,
        CustomerReference = i.CustomerReference,
        AccountingJournalEntryId = i.AccountingJournalEntryId,
        Notes          = i.Notes,
        Lines = i.Lines.Select(l => new SalesInvoiceLineDto
        {
            Id               = l.Id,
            LineNumber       = l.LineNumber,
            SalesOrderLineId = l.SalesOrderLineId ?? Guid.Empty,
            ProductId        = l.ProductId ?? Guid.Empty,
            ProductCode      = l.ProductCode,
            ProductName      = l.ProductName,
            Quantity         = l.Quantity,
            UnitOfMeasure    = l.UnitOfMeasure,
            UnitPrice        = l.UnitPrice,
            DiscountAmount   = l.DiscountAmount,
            LineAmount       = l.LineAmount,
            TaxCategory      = l.TaxCategory,
            TaxRate          = l.TaxRate,
            TaxAmount        = l.TaxAmount,
            TotalAmount      = l.TotalAmount,
            IsDownPayment    = l.IsDownPayment,
        }).ToList(),
    };

    // end of SalesOrderService
}
