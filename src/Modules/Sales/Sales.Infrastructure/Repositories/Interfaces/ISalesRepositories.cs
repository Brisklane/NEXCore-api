using Nexcore.SharedKernel.Repository;
using Sales.Domain.Entities;
using Sales.Domain.Enums;

namespace Sales.Infrastructure.Repositories.Interfaces;

// ?? POS Settings ???????????????????????????????????????????????????????????????

public interface IPosSettingsRepository : IRepository<PosSettings>
{
    Task<PosSettings?> GetCurrentAsync();
}

// ?? Document Sequence ?????????????????????????????????????????????????????????????

public interface IDocumentSequenceRepository : IRepository<DocumentSequence>
{
    Task<DocumentSequence?> GetByDocumentTypeAsync(DocumentType documentType);
    Task<List<DocumentSequence>> GetAllActiveAsync();
}

// ?? Currency & Exchange Rates ???????????????????????????????????????????????????

public interface ICurrencyRepository : IRepository<Currency>
{
    Task<Currency?> GetByCodeAsync(string code);
    Task<Currency?> GetBaseCurrencyAsync();
    Task<List<Currency>> GetActiveAsync();
    Task<Currency?> GetWithRatesAsync(Guid id);
}

public interface ICurrencyRateRepository : IRepository<CurrencyRate>
{
    /// <summary>
    /// Returns the best matching rate for the given currency, rate type, and date.
    /// Looks for the most recent EffectiveDate that is &lt;= asOfDate.
    /// When <paramref name="rateName"/> is provided, only rates with that name are considered.
    /// When null, the unnamed (default) rates for the type are preferred; if none exist,
    /// the first named rate found is returned.
    /// </summary>
    Task<CurrencyRate?> GetRateAsync(
        string currencyCode,
        string baseCurrencyCode,
        ExchangeRateType rateType,
        DateOnly asOfDate,
        string? rateName = null);

    /// <summary>All rates for a currency (any type), ordered by date descending.</summary>
    Task<List<CurrencyRate>> GetByCurrencyAsync(Guid currencyId);

    /// <summary>Rate history for a currency pair between two dates (all rate types).</summary>
    Task<List<CurrencyRate>> GetHistoryAsync(
        string currencyCode,
        string baseCurrencyCode,
        DateOnly fromDate,
        DateOnly toDate);

    /// <summary>
    /// Latest rate for every active currency for each rate type — used for the rate dashboard.
    /// </summary>
    Task<List<CurrencyRate>> GetLatestAllAsync(DateOnly asOfDate);
}

// ?? Sales Order ???????????????????????????????????????????????????????????????

public interface ISalesOrderRepository : IRepository<SalesOrder>
{
    Task<SalesOrder?> GetByNumberAsync(string orderNumber);
    Task<SalesOrder?> GetByOfflineNumberAsync(string offlineOrderNumber);
    Task<SalesOrder?> GetWithLinesAsync(Guid id);
    Task<SalesOrder?> GetWithFullDetailsAsync(Guid id);
    /// <summary>All orders for a given contact (cross-module ContactId).</summary>
    Task<List<SalesOrder>> GetByContactAsync(Guid contactId);
    Task<List<SalesOrder>> GetByStatusAsync(SalesOrderStatus status);
    Task<List<SalesOrder>> GetByChannelAsync(SalesChannel channel);
    /// <summary>Store-facing queue — only Placed+ orders for this store.</summary>
    Task<List<SalesOrder>> GetStoreOrderQueueAsync(Guid storeId);
    /// <summary>
    /// Count of online orders (app / web store / marketplace) still awaiting store
    /// acknowledgement (Status = Placed) for this store — drives the POS "Orders" badge.
    /// </summary>
    Task<int> GetPendingOnlineOrderCountAsync(Guid storeId);
    Task<List<SalesOrder>> GetActiveDraftsByContactAsync(Guid contactId);
    /// <summary>Confirmed orders with uninvoiced quantities — the "To Invoice" queue.</summary>
    Task<List<SalesOrder>> GetOrdersToInvoiceAsync();

    /// <summary>
    /// Set an order's status with a set-based SQL UPDATE (bypasses the change tracker).
    /// Avoids the DbUpdateConcurrencyException seen when saving the status through the
    /// tracked, fully-included order graph.
    /// </summary>
    Task SetStatusAsync(Guid id, SalesOrderStatus status);

    /// <summary>Insert a single status-history row independently of the order graph.</summary>
    Task AddStatusHistoryAsync(SalesOrderStatusHistory history);
}

public interface ISalesOrderLineRepository : IRepository<SalesOrderLine>
{
    Task<List<SalesOrderLine>> GetByOrderAsync(Guid salesOrderId);
}

// ?? Quotation ?????????????????????????????????????????????????????????????????

public interface IQuotationRepository : IRepository<Quotation>
{
    Task<Quotation?> GetByNumberAsync(string quotationNumber);
    Task<Quotation?> GetWithLinesAsync(Guid id);
    Task<List<Quotation>> GetByContactAsync(Guid contactId);
    Task<List<Quotation>> GetByStatusAsync(QuotationStatus status);
}

// ?? Delivery ??????????????????????????????????????????????????????????????????

public interface IDeliveryRepository : IRepository<Delivery>
{
    Task<Delivery?> GetByNumberAsync(string deliveryNumber);
    Task<Delivery?> GetWithLinesAsync(Guid id);
    Task<List<Delivery>> GetByOrderAsync(Guid salesOrderId);
    Task<List<Delivery>> GetByStatusAsync(DeliveryStatus status);
}

// ?? Sales Invoice ?????????????????????????????????????????????????????????????

public interface ISalesInvoiceRepository : IRepository<SalesInvoice>
{
    Task<SalesInvoice?> GetByNumberAsync(string invoiceNumber);
    Task<SalesInvoice?> GetWithLinesAsync(Guid id);
    Task<List<SalesInvoice>> GetByContactAsync(Guid contactId);
    Task<List<SalesInvoice>> GetByOrderAsync(Guid salesOrderId);
    /// <summary>Invoices for an order with their Lines eagerly loaded — use when summing line amounts.</summary>
    Task<List<SalesInvoice>> GetByOrderWithLinesAsync(Guid salesOrderId);
    /// <summary>Invoices for an order that still have an outstanding balance (for auto-allocation).</summary>
    Task<List<SalesInvoice>> GetUnpaidByOrderAsync(Guid salesOrderId);
    Task<List<SalesInvoice>> GetOverdueAsync();
}

// ?? Sales Team ????????????????????????????????????????????????????????????????

public interface ISalesTeamRepository : IRepository<SalesTeam>
{
    Task<List<SalesTeam>> GetActiveAsync();
    Task<SalesTeam?> GetWithMembersAsync(Guid id);
}

// ?? Payment Allocation ????????????????????????????????????????????????????????

public interface IPaymentAllocationRepository : IRepository<PaymentAllocation>
{
    Task<List<PaymentAllocation>> GetByPaymentAsync(Guid paymentId);
    Task<List<PaymentAllocation>> GetByInvoiceAsync(Guid invoiceId);
}

// ?? Sales Payment ?????????????????????????????????????????????????????????????

public interface ISalesPaymentRepository : IRepository<SalesPayment>
{
    Task<SalesPayment?> GetByNumberAsync(string paymentNumber);
    Task<List<SalesPayment>> GetByOrderAsync(Guid salesOrderId);
    Task<List<SalesPayment>> GetByContactAsync(Guid contactId);
}

// ?? POS Terminals ?????????????????????????????????????????????????????????????

public interface IPosTerminalRepository : IRepository<PosTerminal>
{
    /// <summary>All terminals for a branch (the branch IS the store).</summary>
    Task<List<PosTerminal>> GetByBranchAsync(Guid branchId);
    Task<PosTerminal?> GetByCodeAsync(string code, Guid branchId);
    Task<List<PosTerminal>> GetActiveByBranchAsync(Guid branchId);
    /// <summary>All terminals across every store for the company (ignores BranchId filter).</summary>
    Task<List<PosTerminal>> GetAllByCompanyAsync();
    /// <summary>All terminals for the current company AND active branch (from JWT context).</summary>
    Task<List<PosTerminal>> GetAllByBranchAsync();
}

public interface IPosCashierRepository : IRepository<PosCashier>
{
    Task<List<PosCashier>> GetByBranchAsync(Guid branchId);
    /// <summary>Finds an active cashier whose hashed PIN matches the supplied plain text PIN.</summary>
    Task<PosCashier?> GetByPinAsync(string pin, Guid branchId);
    Task<PosCashier?> GetByEmployeeAsync(Guid employeeId, Guid branchId);
}

// ?? POS Session & Transaction ?????????????????????????????????????????????????

public interface IPosSessionRepository : IRepository<PosSession>
{
    Task<PosSession?> GetOpenSessionAsync(Guid cashierId, Guid terminalId);
    /// <summary>The single open session on a terminal, if any (one-open-session-per-terminal rule).</summary>
    Task<PosSession?> GetOpenSessionByTerminalAsync(Guid terminalId);
    /// <summary>Any open session for the cashier across all terminals.</summary>
    Task<PosSession?> GetOpenSessionByCashierAsync(Guid cashierId);
    Task<List<PosSession>> GetByTerminalAsync(Guid terminalId);
    Task<List<PosSession>> GetByDateAsync(DateTime date, Guid branchId);
    /// <summary>All open sessions for the given branch (company-scoped, bypasses branch tenant filter).</summary>
    Task<List<PosSession>> GetOpenByBranchAsync(Guid branchId);
    /// <summary>All sessions for a branch on a specific date (company-scoped).</summary>
    Task<List<PosSession>> GetByBranchAndDateAsync(Guid branchId, DateTime date);
    /// <summary>Sessions for a branch within a date range — used for the dashboard chart.</summary>
    Task<List<PosSession>> GetByBranchAndDateRangeAsync(Guid branchId, DateTime from, DateTime to);
    /// <summary>Tracked session with CashMovements eagerly loaded — use for reading movements.</summary>
    Task<PosSession?> GetByIdWithMovementsAsync(Guid id);
    /// <summary>Inserts a cash movement directly without going through a tracked session navigation.</summary>
    Task AddCashMovementAsync(PosCashMovement movement);
    /// <summary>All sessions for a specific store on a specific date — ignores BranchId JWT filter, for cross-branch dashboard queries.</summary>
    Task<List<PosSession>> GetByStoreAndDateForCompanyAsync(Guid storeId, DateTime date);
}

public interface IPosTransactionRepository : IRepository<PosTransaction>
{
    Task<PosTransaction?> GetByNumberAsync(string transactionNumber);
    Task<PosTransaction?> GetWithLinesAsync(Guid id);
    Task<List<PosTransaction>> GetBySessionAsync(Guid sessionId);
    Task<List<PosTransaction>> GetByDateRangeAsync(DateTime from, DateTime to, Guid branchId);
}

// ?? POS Receipt Template ?????????????????????????????????????????????????????

public interface IPosReceiptTemplateRepository : IRepository<PosReceiptTemplate>
{
    Task<List<PosReceiptTemplate>> GetByBranchAsync(Guid branchId);
    Task<PosReceiptTemplate?> GetDefaultAsync(Guid branchId);
}

public interface IPosBarcodeLabelTemplateRepository : IRepository<PosBarcodeLabelTemplate>
{
    Task<List<PosBarcodeLabelTemplate>> GetByBranchAsync(Guid branchId);
    Task<PosBarcodeLabelTemplate?> GetDefaultAsync(Guid branchId);
}

// ?? POS Cash Drawer ???????????????????????????????????????????????????????????

public interface IPosCashDrawerRepository : IRepository<PosCashDrawer>
{
    Task<List<PosCashDrawer>> GetByBranchAsync(Guid branchId);
    Task<PosCashDrawer?> GetByCodeAsync(string code, Guid branchId);
    Task<List<PosCashDrawer>> GetActiveByBranchAsync(Guid branchId);
}

// ?? Rider ?????????????????????????????????????????????????????????????????????

public interface IRiderRepository : IRepository<Rider>
{
    Task<Rider?> GetByCodeAsync(string code);
    Task<List<Rider>> GetAvailableAsync(Guid? branchId = null);
    Task<List<Rider>> GetByBranchAsync(Guid branchId);
}

public interface IRiderAssignmentRepository : IRepository<RiderAssignment>
{
    Task<List<RiderAssignment>> GetByOrderAsync(Guid salesOrderId);
    Task<List<RiderAssignment>> GetByRiderAsync(Guid riderId);
    Task<RiderAssignment?> GetActiveAssignmentAsync(Guid salesOrderId);
}

// ?? Loyalty ???????????????????????????????????????????????????????????????????

public interface ILoyaltyAccountRepository : IRepository<LoyaltyAccount>
{
    Task<LoyaltyAccount?> GetByContactAsync(Guid contactId);
}

// ?? Price List ????????????????????????????????????????????????????????????????

public interface IPriceListRepository : IRepository<PriceList>
{
    Task<PriceList?> GetByCodeAsync(string code);
    Task<List<PriceList>> GetActiveAsync();
    Task<PriceList?> GetWithItemsAsync(Guid id);
}

// ?? Promotion ?????????????????????????????????????????????????????????????????

public interface IPromotionRepository : IRepository<Promotion>
{
    Task<Promotion?> GetByCodeAsync(string promotionCode);
    Task<Promotion?> GetWithItemsAsync(Guid id);
    /// <summary>
    /// All promotion codes in use within the tenant (company + branch), INCLUDING soft-deleted
    /// rows — the unique index IX_Promotion_Tenant_Code spans them, so they still block reuse.
    /// Upper-cased.
    /// </summary>
    Task<HashSet<string>> GetUsedCodesAsync();
    /// <summary>All active promotions valid at the given date/time.</summary>
    Task<List<Promotion>> GetActiveAsync(DateOnly date, TimeOnly time);
    /// <summary>Active auto-apply promotions that include the given item or its category.</summary>
    Task<List<Promotion>> GetActiveForItemAsync(Guid itemId, DateOnly date, TimeOnly time);
}

public interface IPromotionItemRepository : IRepository<PromotionItem>
{
    Task<List<PromotionItem>> GetByPromotionAsync(Guid promotionId);
}

// ?? Coupon ????????????????????????????????????????????????????????????????????

public interface ICouponRepository : IRepository<Coupon>
{
    Task<Coupon?> GetByCodeAsync(string code);
    /// <summary>Tenant-filter-free lookup for anonymous validation endpoints.</summary>
    Task<Coupon?> GetByCodePublicAsync(string code);
    Task<List<Coupon>> GetActiveAsync();
    Task<bool> IsCodeUsedByContactAsync(string code, Guid contactId);
}

// ?? POS Store ?????????????????????????????????????????????????????????????????

public interface IPosStoreRepository : IRepository<PosStore>
{
    Task<PosStore?> GetByCodeAsync(string storeCode);
    Task<List<PosStore>> GetActiveByBranchAsync(Guid branchId);
    Task<List<PosStore>> GetOnlineEnabledAsync();
    // No tenant filtering — used by AllowAnonymous endpoints such as GetNearby.
    Task<List<PosStore>> GetAllActiveWithLocationAsync();
    // Direct MAX query for atomic code generation — bypasses in-memory FindAsync.
    Task<int> GetMaxCodeIntAsync();

    /// <summary>True if another POS store in the same company already uses this trading name (case-insensitive).</summary>
    Task<bool> TradingNameExistsAsync(string tradingName, Guid? excludeStoreId = null);

    /// <summary>All stores across the whole company (ignores BranchId filter).</summary>
    Task<List<PosStore>> GetAllByCompanyAsync();
    /// <summary>All stores for the current company AND active branch (from JWT context).</summary>
    Task<List<PosStore>> GetAllByBranchAsync();
}

// ?? Vendor Profile ????????????????????????????????????????????????????????????

public interface IStoreVendorProfileRepository : IRepository<StoreVendorProfile>
{
    Task<StoreVendorProfile?> GetByStoreAsync(Guid storeId);
    Task<List<StoreVendorProfile>> GetByStatusAsync(VendorOnboardingStatus status);
}

// ?? Store Offer ???????????????????????????????????????????????????????????????

public interface IStoreOfferRepository : IRepository<StoreOffer>
{
    Task<List<StoreOffer>> GetByStoreAsync(Guid storeId);
    /// <summary>
    /// Active offers valid right now — used by staff dashboard.
    /// Tenant-filtered via JWT context.
    /// </summary>
    Task<List<StoreOffer>> GetActiveByStoreAsync(Guid storeId, DateOnly date, TimeOnly time);
    /// <summary>
    /// Active offers for the public customer app — no JWT required, filters by storeId only.
    /// </summary>
    Task<List<StoreOffer>> GetPublicActiveByStoreAsync(Guid storeId, DateOnly date, TimeOnly time);
}

// ?? Store Menu ????????????????????????????????????????????????????????????????

public interface IStoreMenuRepository : IRepository<StoreMenu>
{
    Task<List<StoreMenu>> GetByBranchAsync(Guid branchId);
    Task<StoreMenu?> GetActiveMenuAsync(Guid branchId);
    Task<StoreMenu?> GetWithSectionsAsync(Guid id);
}

// ?? Tax Engine ????????????????????????????????????????????????????????????????
/// <summary>
/// TaxDefinition (individual rate components) lives in Inventory.
/// Sales owns TaxGroup, TaxGroupRate, and TaxRule only.
/// </summary>
public interface ITaxGroupRepository : IRepository<TaxGroup>
{
    Task<TaxGroup?> GetByCodeAsync(string code);
    Task<TaxGroup?> GetWithRatesAsync(Guid id);
}

public interface ITaxRuleRepository : IRepository<TaxRule>
{
    /// <summary>
    /// Returns all active rules for the tenant, ordered by Priority ascending.
    /// The caller applies the first match.
    /// </summary>
    Task<List<TaxRule>> GetActiveRulesAsync();
    Task<TaxGroup?> ResolveAsync(TaxCategory productCategory, string? customerCountryCode, string? customerType, SalesChannel? channel);
}

// ?? Approval Engine ???????????????????????????????????????????????????????????

public interface IApprovalPolicyRepository : IRepository<ApprovalPolicy>
{
    /// <summary>Returns all active policies ordered by Priority ascending for evaluation.</summary>
    Task<List<ApprovalPolicy>> GetActivePoliciesAsync();
    Task<ApprovalPolicy?> GetWithStepsAsync(Guid id);
}

public interface IApprovalRequestRepository : IRepository<ApprovalRequest>
{
    Task<List<ApprovalRequest>> GetByOrderAsync(Guid salesOrderId);
    Task<List<ApprovalRequest>> GetPendingByUserAsync(Guid userId);
    Task<List<ApprovalRequest>> GetPendingByRoleAsync(string role);
    Task<ApprovalRequest?> GetPendingStepAsync(Guid salesOrderId, int stepOrder);
}

// ?? Commission ????????????????????????????????????????????????????????????????

public interface ICommissionRuleRepository : IRepository<CommissionRule>
{
    /// <summary>Returns active rules ordered by Priority ascending for evaluation.</summary>
    Task<List<CommissionRule>> GetActiveRulesAsync();
}

public interface ICommissionEntryRepository : IRepository<CommissionEntry>
{
    Task<List<CommissionEntry>> GetByOrderAsync(Guid salesOrderId);
    Task<List<CommissionEntry>> GetByRepAsync(Guid salesRepId, DateTime? from = null, DateTime? to = null);
    Task<List<CommissionEntry>> GetPendingByRepAsync(Guid salesRepId);
    Task<decimal> GetTotalEarnedAsync(Guid salesRepId, DateTime from, DateTime to);
}

public interface ISalesTargetRepository : IRepository<SalesTarget>
{
    Task<SalesTarget?> GetCurrentTargetAsync(Guid salesRepId, DateTime date);
    Task<List<SalesTarget>> GetByRepAsync(Guid salesRepId);
    Task<List<SalesTarget>> GetByTerritoryAsync(Guid territoryId);
}
