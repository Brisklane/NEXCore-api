using Sales.Application.DTOs;
using Sales.Domain.Enums;

namespace Sales.Application.Services.Interfaces;

/// <summary>
/// Service layer for SalesOrder business logic.
/// Handles order creation, lifecycle transitions, totals calculation, and status history.
/// </summary>
public interface ISalesOrderService
{
    Task<List<SalesOrderDto>> GetAllAsync();
    Task<SalesOrderDto?> GetByIdAsync(Guid id);
    Task<SalesOrderDto?> GetByNumberAsync(string orderNumber);
    Task<SalesOrderDto?> GetWithFullDetailsAsync(Guid id);
    Task<List<SalesOrderDto>> GetByCustomerAsync(Guid customerId);
    Task<List<SalesOrderDto>> GetByStatusAsync(SalesOrderStatus status);
    Task<List<SalesOrderDto>> GetByChannelAsync(SalesChannel channel);
    Task<List<SalesOrderDto>> GetActiveDraftsByContactAsync(Guid contactId);
    Task<List<SalesOrderDto>> GetStoreQueueAsync(Guid storeId);
    /// <summary>
    /// Count of online orders (app / web store / marketplace) awaiting store
    /// acknowledgement (Status = Placed). Drives the POS "Orders" button badge on
    /// initial load and reconnect; live changes are pushed over the POS order hub.
    /// </summary>
    Task<int> GetPendingOnlineOrderCountAsync(Guid storeId);
    Task<SalesOrderDto> CreateAsync(CreateSalesOrderDto dto);
    Task<SalesOrderDto> PlaceOrderAsync(Guid id);
    /// <summary>
    /// Re-run the central pricing engine over the order's current lines and refresh prices,
    /// discounts and totals. Used on finalize (place / POS checkout) to catch promotions that
    /// expired or prices that changed since the basket was built. No-op once a payment exists.
    /// </summary>
    Task<SalesOrderDto> RepriceAsync(Guid orderId);
    Task<SalesOrderDto> UpdateStatusAsync(Guid id, UpdateSalesOrderStatusDto dto, string? changedBy);
    Task DeleteAsync(Guid id);

    // ── Invoicing ─────────────────────────────────────────────────────────────
    /// <summary>Orders with uninvoiced quantities — the "To Invoice" queue.</summary>
    Task<List<SalesOrderDto>> GetOrdersToInvoiceAsync();
    /// <summary>
    /// Creates a draft invoice from the order (Regular, DownPaymentPercentage, or DownPaymentFixed).
    /// Updates InvoicedQuantity on lines and InvoiceStatus on the order.
    /// </summary>
    Task<SalesInvoiceDto> CreateInvoiceFromOrderAsync(Guid orderId, CreateInvoiceFromOrderDto dto);

    // ── Lock / Unlock ─────────────────────────────────────────────────────────
    Task<SalesOrderDto> LockAsync(Guid id);
    Task<SalesOrderDto> UnlockAsync(Guid id);
}
