using Sales.Application.DTOs;

namespace Sales.Application.Services.Interfaces;

/// <summary>
/// Orchestrates the POS settlement (checkout) flow:
/// invoices a scanned sales order, registers the tender(s), writes the POS transaction,
/// and publishes the accounting + stock-deduction events — all in one call.
/// </summary>
public interface IPosCheckoutService
{
    /// <summary>
    /// Settle a POS sale. Creates (or reuses) the invoice for the order, records the payment(s),
    /// writes the <c>PosTransaction</c> with ReceiptNumber = InvoiceNumber, updates the session totals,
    /// and fires the AR / COGS / payment / stock events.
    /// </summary>
    Task<PosCheckoutResultDto> CheckoutAsync(PosCheckoutDto dto);

    Task<PosTransactionDto?> GetByIdAsync(Guid id);
    Task<PosTransactionDto?> GetByNumberAsync(string transactionNumber);
    Task<List<PosTransactionDto>> GetBySessionAsync(Guid sessionId);
    /// <summary>All transactions for the current tenant's branch within a UTC date range.</summary>
    Task<List<PosTransactionDto>> GetByDateRangeAsync(DateTime from, DateTime to);
}
