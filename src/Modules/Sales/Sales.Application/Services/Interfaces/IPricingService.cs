using Sales.Application.DTOs;

namespace Sales.Application.Services.Interfaces;

/// <summary>
/// Central pricing &amp; promotion engine. Resolves a server-authoritative price for every line
/// (price list → catalog base price), applies active per-line promotions and an optional coupon,
/// and returns the priced basket with totals.
///
/// One engine for all channels: the POS screen and the Sales Order screen call <see cref="PriceOrderAsync"/>
/// for a live quote, and <c>SalesOrderService.CreateAsync</c> calls it so the persisted order is priced
/// the same way regardless of who created it.
/// </summary>
public interface IPricingService
{
    Task<PricedOrderDto> PriceOrderAsync(PriceOrderRequestDto request);
}
