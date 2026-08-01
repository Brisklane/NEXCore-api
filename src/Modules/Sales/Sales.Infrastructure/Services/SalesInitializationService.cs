using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Nexcore.SharedKernel;
using Sales.Application.Services.Interfaces;
using Sales.Domain.Entities;
using Sales.Infrastructure.Persistence;

namespace Sales.Infrastructure.Services;

/// <summary>
/// Seeds comprehensive Sales data for a newly created company.
///
/// Seeding order (respects FK constraints):
///   1.  Receipt Template
///   2.  Price Lists
///   3.  Customer Groups + Sales Territories
///   4.  Tax Groups + Rules
///   5.  Approval Policies
///   6.  Commission Rules + Sales Targets
///   7.  Discount Schemes + Coupons
///   8.  Loyalty Program
///   9.  POS Store + Schedule + Holidays + Delivery Zones
///   10. POS Terminals + Cash Drawers
///   11. POS Cashiers
///   12. POS Session + Cash Movements
///   13. Quotation
///   14. Sales Agreement
///   15. Sales Orders (POS + B2B + App) with Lines, Status History, Approvals
///   16. POS Transaction + Lines + Payments
///   17. Deliveries + Lines
///   18. Sales Invoices + Lines + Payments + Credit Notes
///   19. Sales Returns + Lines
///   20. Riders + Shifts + Assignments + Ratings + Location Logs
///   21. Gift Cards + Transactions
///   22. Loyalty Accounts + Transactions
///   23. Product Reviews
///   24. Store Menu + Sections
///   25. Wishlist + App Notifications
/// </summary>
public partial class SalesInitializationService : ISalesInitializationService
{
    private readonly SalesDbContext _ctx;
    private readonly ILogger<SalesInitializationService> _logger;

    public SalesInitializationService(
        SalesDbContext ctx,
        ILogger<SalesInitializationService> logger)
    {
        _ctx    = ctx;
        _logger = logger;
    }

    // ?? public API ????????????????????????????????????????????????????????????

    public async Task<Result> InitializeSalesForNewCompanyAsync(
        Guid companyId, Guid branchId, Guid businessUnitId, Guid userId, string? companyName = null,
        bool includeSampleData = false)
    {
        try
        {
            _logger.LogInformation(
                "Initializing Sales data for Company:{CompanyId} Branch:{BranchId}",
                companyId, branchId);

            if (await SalesDataExistsAsync(companyId))
            {
                _logger.LogWarning("Sales data already exists for Company:{CompanyId}", companyId);
                return Result.Ok("Sales data already initialized for this company");
            }

            var T = (companyId, branchId, businessUnitId, userId);

            // Shared between the master and demo phases.
            PriceList[] priceLists = null!;
            Coupon[] coupons = null!;
            LoyaltyProgram loyaltyProgram = null!;
            PosStore posStore = null!;
            PosTerminal[] terminals = null!;
            PosCashier[] cashiers = null!;
            PosSession posSession = null!;

            // ═══ PHASE 1 — MASTER / CONFIG (must succeed; committed as a unit) ═══
            var masterStrategy = _ctx.Database.CreateExecutionStrategy();
            try
            {
            await masterStrategy.ExecuteAsync(async () =>
            {
                await using var masterTx = await _ctx.Database.BeginTransactionAsync();
                try
                {
                // ?? 0. Document Sequences ????????????????????????????????????
                var docSequences = SeedDocumentSequences(T);
                _ctx.DocumentSequences.AddRange(docSequences);
                await _ctx.SaveChangesAsync();
                _logger.LogInformation("Seeded {N} document sequences", docSequences.Length);

                // ?? 1. Receipt Templates (thermal default + A4) ???????????????
                var receiptTemplates = SeedReceiptTemplates(T, companyName);
                _ctx.PosReceiptTemplates.AddRange(receiptTemplates);
                await _ctx.SaveChangesAsync();
                var receiptTemplate = receiptTemplates[0];   // thermal default — linked to the seeded store
                _logger.LogInformation("Seeded {N} receipt templates", receiptTemplates.Length);

                // ?? 1b. Barcode Label (Price Tag) Template ????????????????????
                var labelTemplate = SeedBarcodeLabelTemplate(T, companyName);
                _ctx.PosBarcodeLabelTemplates.Add(labelTemplate);
                await _ctx.SaveChangesAsync();
                _logger.LogInformation("Seeded barcode label template");

                // ?? 2. Price Lists ????????????????????????????????????????????
                // The price list headers are master data, but their lines are priced against
                // the demo item catalog that Inventory only seeds as sample data. Adding them
                // when sample data is off would leave rows pointing at items that don't exist.
                (priceLists, var priceListItems) = SeedPriceLists(T);
                _ctx.PriceLists.AddRange(priceLists);
                await _ctx.SaveChangesAsync();
                if (includeSampleData)
                {
                    _ctx.PriceListItems.AddRange(priceListItems);
                    await _ctx.SaveChangesAsync();
                }
                _logger.LogInformation("Seeded {N} price lists, {I} price list items",
                    priceLists.Length, includeSampleData ? priceListItems.Length : 0);

                // ?? 3. Customer Groups + Sales Territories ????????????????????
                var customerGroups = SeedCustomerGroups(T);
                _ctx.CustomerGroups.AddRange(customerGroups);
                await _ctx.SaveChangesAsync();

                var territories = SeedSalesTerritories(T);
                _ctx.SalesTerritories.AddRange(territories);
                await _ctx.SaveChangesAsync();
                _logger.LogInformation("Seeded {G} customer groups, {Tr} territories",
                    customerGroups.Length, territories.Length);

                // ?? 4. Tax Groups + Rules ?????????????????????????????????????
                var (taxGroups, taxGroupRates, taxRules) = SeedTaxGroups(T);
                _ctx.TaxGroups.AddRange(taxGroups);
                await _ctx.SaveChangesAsync();
                _ctx.TaxGroupRates.AddRange(taxGroupRates);
                _ctx.TaxRules.AddRange(taxRules);
                await _ctx.SaveChangesAsync();
                _logger.LogInformation("Seeded {N} tax groups", taxGroups.Length);

                // ?? 5. Approval Policies ??????????????????????????????????????
                var (approvalPolicies, approvalConditions, approvalSteps) = SeedApprovalPolicies(T);
                _ctx.ApprovalPolicies.AddRange(approvalPolicies);
                await _ctx.SaveChangesAsync();
                _ctx.ApprovalPolicyConditions.AddRange(approvalConditions);
                _ctx.ApprovalPolicySteps.AddRange(approvalSteps);
                await _ctx.SaveChangesAsync();
                _logger.LogInformation("Seeded {N} approval policies", approvalPolicies.Length);

                // ?? 6. Commission Rules + Sales Targets ???????????????????????
                var commissionRules = SeedCommissionRules(T);
                _ctx.CommissionRules.AddRange(commissionRules);
                await _ctx.SaveChangesAsync();

                var salesTargets = SeedSalesTargets(T);
                _ctx.SalesTargets.AddRange(salesTargets);
                await _ctx.SaveChangesAsync();
                _logger.LogInformation("Seeded {N} commission rules, {St} sales targets",
                    commissionRules.Length, salesTargets.Length);

                // ?? 7. Discount Schemes + Coupons ?????????????????????????????
                var discountSchemes = SeedDiscountSchemes(T);
                _ctx.DiscountSchemes.AddRange(discountSchemes);
                await _ctx.SaveChangesAsync();

                coupons = SeedCoupons(T);
                _ctx.Coupons.AddRange(coupons);
                await _ctx.SaveChangesAsync();
                _logger.LogInformation("Seeded {D} discount schemes, {C} coupons",
                    discountSchemes.Length, coupons.Length);

                // ?? 7b. Promotions (auto-applied combo deal) ??????????????????
                // A combo deal is defined entirely in terms of the demo items it bundles,
                // so the whole promotion is sample data — skipped along with the catalog.
                if (includeSampleData)
                {
                    var (promotions, promotionItems) = SeedPromotions(T);
                    _ctx.Promotions.AddRange(promotions);
                    await _ctx.SaveChangesAsync();
                    _ctx.PromotionItems.AddRange(promotionItems);
                    await _ctx.SaveChangesAsync();
                    _logger.LogInformation("Seeded {N} promotions with {I} items",
                        promotions.Length, promotionItems.Length);
                }

                // ?? 8. Loyalty Program ????????????????????????????????????????
                loyaltyProgram = SeedLoyaltyProgram(T);
                _ctx.LoyaltyPrograms.Add(loyaltyProgram);
                await _ctx.SaveChangesAsync();
                _logger.LogInformation("Seeded loyalty program");

                // ?? 9. POS Store + Schedule + Holidays + Zones ????????????????
                (posStore, var schedule, var holidays, var deliveryZones) =
                    SeedPosStore(T, priceLists, receiptTemplate);
                _ctx.PosStores.Add(posStore);
                await _ctx.SaveChangesAsync();
                _ctx.PosStoreSchedules.AddRange(schedule);
                _ctx.PosStoreHolidays.AddRange(holidays);
                _ctx.DeliveryZones.AddRange(deliveryZones);
                await _ctx.SaveChangesAsync();
                _logger.LogInformation("Seeded POS store + {S} schedule + {H} holidays + {Z} zones",
                    schedule.Length, holidays.Length, deliveryZones.Length);

                // ?? 10. POS Terminals + Cash Drawers ??????????????????????????
                (terminals, var cashDrawers) = SeedPosTerminals(T, posStore);
                _ctx.PosCashDrawers.AddRange(cashDrawers);
                await _ctx.SaveChangesAsync();
                _ctx.PosTerminals.AddRange(terminals);
                await _ctx.SaveChangesAsync();
                _logger.LogInformation("Seeded {N} terminals + {D} drawers",
                    terminals.Length, cashDrawers.Length);

                // ?? 11. POS Cashiers ??????????????????????????????????????????
                cashiers = SeedPosCashiers(T, posStore);
                _ctx.PosCashiers.AddRange(cashiers);
                await _ctx.SaveChangesAsync();
                _logger.LogInformation("Seeded {N} cashiers", cashiers.Length);

                // ?? 12. POS Session + Cash Movements ??????????????????????????
                (posSession, var cashMovements, var drawerEvents) =
                    SeedPosSession(T, terminals[0], cashiers[0]);
                _ctx.PosSessions.Add(posSession);
                await _ctx.SaveChangesAsync();
                _ctx.PosCashMovements.AddRange(cashMovements);
                _ctx.PosCashDrawerEvents.AddRange(drawerEvents);
                await _ctx.SaveChangesAsync();
                _logger.LogInformation("Seeded POS session + {M} cash movements", cashMovements.Length);

                    await masterTx.CommitAsync();
                    _logger.LogInformation("Sales master data committed for Company:{CompanyId}", companyId);
                }
                catch
                {
                    await masterTx.RollbackAsync();
                    throw;
                }
            });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Sales master-data seeding failed for Company:{CompanyId}", companyId);
                return Result.Fail($"Failed to initialize Sales master data: {ex.Message}");
            }

            // ═══ PHASE 2 — DEMO / SAMPLE DATA (opt-in; best-effort; never costs the master data) ═══
            if (!includeSampleData)
            {
                _logger.LogInformation(
                    "Sales sample data skipped (IncludeSampleData=false) for Company:{CompanyId}", companyId);
                return Result.Ok("Sales master data initialized successfully");
            }

            var demoStrategy = _ctx.Database.CreateExecutionStrategy();
            try
            {
                await demoStrategy.ExecuteAsync(async () =>
                {
                await using var demoTx = await _ctx.Database.BeginTransactionAsync();
                try
                {
                // ?? 13. Quotation ?????????????????????????????????????????????
                var (quotation, quotationLines, quotationApprovals) = SeedQuotation(T, priceLists);
                _ctx.Quotations.Add(quotation);
                await _ctx.SaveChangesAsync();
                _ctx.QuotationLines.AddRange(quotationLines);
                _ctx.QuotationApprovals.AddRange(quotationApprovals);
                await _ctx.SaveChangesAsync();
                _logger.LogInformation("Seeded quotation with {N} lines", quotationLines.Length);

                // ?? 14. Sales Agreement ???????????????????????????????????????
                var (salesAgreement, agreementLines) = SeedSalesAgreement(T, priceLists);
                _ctx.SalesAgreements.Add(salesAgreement);
                await _ctx.SaveChangesAsync();
                _ctx.SalesAgreementLines.AddRange(agreementLines);
                await _ctx.SaveChangesAsync();
                _logger.LogInformation("Seeded sales agreement with {N} lines", agreementLines.Length);

                // ?? 15. Sales Orders ??????????????????????????????????????????
                var (salesOrders, orderLines, orderLineAddons, orderStatusHistory, orderApprovals, orderAttachments) =
                    SeedSalesOrders(T, priceLists, posStore, terminals[0], cashiers[0],
                        posSession, quotation, salesAgreement, coupons);
                _ctx.SalesOrders.AddRange(salesOrders);
                await _ctx.SaveChangesAsync();
                _ctx.SalesOrderLines.AddRange(orderLines);
                await _ctx.SaveChangesAsync();
                _ctx.SalesOrderLineAddons.AddRange(orderLineAddons);
                _ctx.SalesOrderStatusHistories.AddRange(orderStatusHistory);
                _ctx.SalesOrderApprovals.AddRange(orderApprovals);
                _ctx.SalesOrderAttachments.AddRange(orderAttachments);
                await _ctx.SaveChangesAsync();
                _logger.LogInformation("Seeded {N} sales orders with {L} lines",
                    salesOrders.Length, orderLines.Length);

                // ?? 16. POS Transactions ??????????????????????????????????????
                var (posTxns, posTxnLines, posPayments) =
                    SeedPosTransactions(T, posStore, terminals[0], cashiers[0],
                        posSession, salesOrders);
                _ctx.PosTransactions.AddRange(posTxns);
                await _ctx.SaveChangesAsync();
                _ctx.PosTransactionLines.AddRange(posTxnLines);
                _ctx.PosPayments.AddRange(posPayments);
                await _ctx.SaveChangesAsync();
                _logger.LogInformation("Seeded {N} POS transactions", posTxns.Length);

                // ?? 17. Deliveries ????????????????????????????????????????????
                var (deliveries, deliveryLines) = SeedDeliveries(T, salesOrders, orderLines);
                _ctx.Deliveries.AddRange(deliveries);
                await _ctx.SaveChangesAsync();
                _ctx.DeliveryLines.AddRange(deliveryLines);
                await _ctx.SaveChangesAsync();
                _logger.LogInformation("Seeded {N} deliveries with {L} lines",
                    deliveries.Length, deliveryLines.Length);

                // ?? 18. Invoices + Payments + Credit Notes ????????????????????
                var (invoices, invoiceLines, salesPayments, creditNotes, creditNoteLines) =
                    SeedInvoicesAndPayments(T, salesOrders, orderLines, deliveries);
                _ctx.SalesInvoices.AddRange(invoices);
                await _ctx.SaveChangesAsync();
                _ctx.SalesInvoiceLines.AddRange(invoiceLines);
                _ctx.SalesPayments.AddRange(salesPayments);
                await _ctx.SaveChangesAsync();
                _ctx.CreditNotes.AddRange(creditNotes);
                await _ctx.SaveChangesAsync();
                _ctx.CreditNoteLines.AddRange(creditNoteLines);
                await _ctx.SaveChangesAsync();
                _logger.LogInformation("Seeded {I} invoices, {P} payments, {CN} credit notes",
                    invoices.Length, salesPayments.Length, creditNotes.Length);

                // ?? 19. Returns ???????????????????????????????????????????????
                var (returns, returnLines) = SeedReturns(T, salesOrders, orderLines, invoices);
                _ctx.SalesReturns.AddRange(returns);
                await _ctx.SaveChangesAsync();
                _ctx.SalesReturnLines.AddRange(returnLines);
                await _ctx.SaveChangesAsync();
                _logger.LogInformation("Seeded {N} returns", returns.Length);

                // ?? 20. Riders ????????????????????????????????????????????????????????????
                var (riders, riderShifts, riderAssignments, riderRatings, locationLogs) =
                    SeedRiders(T, posStore, salesOrders);
                _ctx.Riders.AddRange(riders);
                await _ctx.SaveChangesAsync();
                _ctx.RiderShifts.AddRange(riderShifts);
                _ctx.RiderAssignments.AddRange(riderAssignments);
                await _ctx.SaveChangesAsync();
                _ctx.RiderRatings.AddRange(riderRatings);
                _ctx.RiderLocationLogs.AddRange(locationLogs);
                await _ctx.SaveChangesAsync();
                _logger.LogInformation("Seeded {N} riders", riders.Length);

                // ?? 21. Gift Cards ????????????????????????????????????????????
                var (giftCards, giftCardTxns) = SeedGiftCards(T);
                _ctx.PosGiftCards.AddRange(giftCards);
                await _ctx.SaveChangesAsync();
                _ctx.PosGiftCardTransactions.AddRange(giftCardTxns);
                await _ctx.SaveChangesAsync();
                _logger.LogInformation("Seeded {N} gift cards", giftCards.Length);

                // ?? 22. Loyalty Accounts + Transactions ???????????????????????
                var (loyaltyAccounts, loyaltyTxns) = SeedLoyaltyAccounts(T, loyaltyProgram);
                _ctx.LoyaltyAccounts.AddRange(loyaltyAccounts);
                await _ctx.SaveChangesAsync();
                _ctx.LoyaltyTransactions.AddRange(loyaltyTxns);
                await _ctx.SaveChangesAsync();
                _logger.LogInformation("Seeded {N} loyalty accounts", loyaltyAccounts.Length);

                // ?? 23. Product Reviews ???????????????????????????????????????
                var (reviews, reviewImages) = SeedProductReviews(T);
                _ctx.ProductReviews.AddRange(reviews);
                await _ctx.SaveChangesAsync();
                _ctx.ProductReviewImages.AddRange(reviewImages);
                await _ctx.SaveChangesAsync();
                _logger.LogInformation("Seeded {N} product reviews", reviews.Length);

                // ?? 24. Store Menu ????????????????????????????????????????????
                var (storeMenus, menuSections) = SeedStoreMenus(T, posStore);
                _ctx.StoreMenus.AddRange(storeMenus);
                await _ctx.SaveChangesAsync();
                _ctx.StoreMenuSections.AddRange(menuSections);
                await _ctx.SaveChangesAsync();
                _logger.LogInformation("Seeded {N} store menus", storeMenus.Length);

                // ?? 25. Wishlist + App Notifications ??????????????????????????
                var wishlistItems = SeedWishlistItems(T);
                var notifications = SeedAppNotifications(T);
                _ctx.WishlistItems.AddRange(wishlistItems);
                _ctx.AppNotifications.AddRange(notifications);
                await _ctx.SaveChangesAsync();
                _logger.LogInformation("Seeded {W} wishlist items, {N} notifications",
                    wishlistItems.Length, notifications.Length);

                    await demoTx.CommitAsync();
                    _logger.LogInformation("Sales demo data committed for Company:{CompanyId}", companyId);
                }
                catch
                {
                    await demoTx.RollbackAsync();
                    throw;
                }
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Sales demo-data seeding failed for Company:{CompanyId} — master/config data preserved",
                    companyId);
            }

            _logger.LogInformation("Sales initialization complete for Company:{CompanyId}", companyId);
            return Result.Ok("Sales data initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize Sales for Company:{CompanyId}", companyId);
            return Result.Fail($"Failed to initialize Sales data: {ex.Message}");
        }
    }

    public async Task<bool> SalesDataExistsAsync(Guid companyId) =>
        await _ctx.PriceLists.AnyAsync(p => p.CompanyId == companyId);
}
