using Nexcore.SharedKernel;

namespace Sales.Domain.Entities;

/// <summary>
/// Branch-level POS configuration. One row per company/branch/business-unit.
/// Covers every knob the frontend may need: general behaviour, payment methods,
/// cart/product display, session rules, loyalty, receipt printing, and UI theme.
/// </summary>
public class PosSettings : BaseEntity
{
    // ── General ───────────────────────────────────────────────────────────────
    public bool   RequireCashierPin       { get; set; } = true;
    /// <summary>Minutes of inactivity before the terminal locks. 0 = disabled.</summary>
    public int    AutoLockMinutes         { get; set; } = 0;
    public bool   AllowPriceOverride      { get; set; } = false;
    public bool   AllowDiscount           { get; set; } = true;
    /// <summary>Maximum discount a cashier may apply (0–100).</summary>
    public decimal MaxDiscountPercent     { get; set; } = 100m;
    public bool   RequireCustomer         { get; set; } = false;
    /// <summary>Pre-selected walk-in customer. Null = no default.</summary>
    public Guid?  DefaultCustomerId       { get; set; }
    /// <summary>
    /// Allow selling items whose on-hand stock would go at or below zero.
    /// Default true = oversell allowed (POS does not block a paid sale; the shortage is logged and
    /// shows as negative on-hand). Set false to enforce: POS checkout blocks with a 400 before payment
    /// when a line exceeds available stock. See PosCheckoutService stock-availability check.
    /// </summary>
    public bool   AllowNegativeStock      { get; set; } = true;
    /// <summary>Prices already include tax; do not add tax on top.</summary>
    public bool   TaxInclusivePricing     { get; set; } = false;
    public bool   AutoApplyTax            { get; set; } = false;
    public string? DefaultCurrencyCode    { get; set; }
    /// <summary>Round totals to nearest value (e.g. 0.05, 0.25, 1.00). 0 = no rounding.</summary>
    public decimal RoundingValue          { get; set; } = 0m;
    /// <summary>None | Up | Down | Nearest</summary>
    public string  RoundingMode           { get; set; } = "None";

    // ── Payment ───────────────────────────────────────────────────────────────
    public bool   AcceptCash              { get; set; } = true;
    public bool   AcceptCard              { get; set; } = true;
    public bool   AcceptMobilePayment     { get; set; } = true;
    /// <summary>Allow posting a sale on the customer's credit account.</summary>
    public bool   AcceptCreditOnAccount   { get; set; } = false;
    public bool   AllowSplitPayment       { get; set; } = true;
    /// <summary>Allow a sale to be only partially paid (remaining becomes an open balance).</summary>
    public bool   AllowPartialPayment     { get; set; } = false;
    /// <summary>Cash | Card | Mobile | CreditOnAccount. Null = no default.</summary>
    public string? DefaultPaymentMethod   { get; set; }
    /// <summary>Automatically trigger the cash-drawer open signal on cash sales.</summary>
    public bool   AutoOpenCashDrawer      { get; set; } = true;
    public decimal? MinOrderAmount        { get; set; }
    public decimal? MaxOrderAmount        { get; set; }
    /// <summary>
    /// GL account number for cash receipts (e.g. "1111"). Null = use PostingProfile fallback.
    /// </summary>
    public string? CashGlAccountNumber   { get; set; }
    /// <summary>
    /// GL account number for card/bank/mobile receipts (e.g. "1112"). Null = use PostingProfile fallback.
    /// </summary>
    public string? BankGlAccountNumber   { get; set; }

    // ── Cart & Products ───────────────────────────────────────────────────────
    public bool   ShowProductImages       { get; set; } = true;
    public bool   ShowProductDescription  { get; set; } = false;
    /// <summary>Let cashiers add free-text notes or modifiers to individual cart lines.</summary>
    public bool   AllowItemNotes          { get; set; } = true;
    /// <summary>Allow fractional quantities (e.g. 1.5 kg). False = whole numbers only.</summary>
    public bool   AllowDecimalQuantity    { get; set; } = false;
    public bool   BarcodeScanSound        { get; set; } = true;
    public bool   ShowStockLevel          { get; set; } = false;
    public int    LowStockThreshold       { get; set; } = 5;
    public int    ItemsPerPage            { get; set; } = 20;
    public bool   ShowCategoryFilter      { get; set; } = true;
    /// <summary>Optimise layout for touch screens (larger tap targets).</summary>
    public bool   TouchMode               { get; set; } = true;

    // ── Connectivity / Operating Mode ────────────────────────────────────────
    /// <summary>Online | OfflineFallback | LocalFirst</summary>
    public string OperatingMode           { get; set; } = "Online";

    // ── Session ───────────────────────────────────────────────────────────────
    /// <summary>Cashier must enter opening float before first sale.</summary>
    public bool   RequireOpeningFloat     { get; set; } = false;
    /// <summary>Cashier must count and declare cash when closing a session.</summary>
    public bool   RequireCashCountOnClose { get; set; } = false;
    /// <summary>Automatically close the active session at <see cref="AutoCloseTime"/>.</summary>
    public bool   AutoCloseSession        { get; set; } = false;
    /// <summary>HH:mm (UTC) at which the session is auto-closed. Null when AutoCloseSession is false.</summary>
    public string? AutoCloseTime          { get; set; }

    // ── Loyalty & Promotions ──────────────────────────────────────────────────
    public bool   EnableLoyaltyPoints     { get; set; } = false;
    public bool   EnablePromotions        { get; set; } = true;
    public bool   EnableCoupons           { get; set; } = true;
    /// <summary>Automatically apply the best eligible promotion without cashier action.</summary>
    public bool   AutoApplyPromotions     { get; set; } = true;

    // ── Receipt ───────────────────────────────────────────────────────────────
    /// <summary>Thermal58mm | Thermal80mm | A4</summary>
    public string DefaultPaperSize        { get; set; } = "Thermal80mm";
    public int    ReceiptCopies           { get; set; } = 1;
    /// <summary>Silently print receipt after payment without asking the cashier.</summary>
    public bool   AutoPrintReceipt        { get; set; } = false;
    /// <summary>Show a "Print receipt?" prompt after payment.</summary>
    public bool   AskToPrintReceipt       { get; set; } = true;
    /// <summary>Skip the on-screen receipt view entirely after payment (no print, no prompt).</summary>
    public bool   SkipReceiptScreen       { get; set; } = false;
    public bool   ReceiptShowLogo         { get; set; } = true;
    public bool   ReceiptShowBusinessName { get; set; } = true;
    public bool   ReceiptShowAddress      { get; set; } = true;
    public bool   ReceiptShowContact      { get; set; } = true;
    public bool   ReceiptShowCashierName  { get; set; } = true;
    public bool   ReceiptShowCustomerName { get; set; } = true;
    public bool   ReceiptShowOrderNumber  { get; set; } = true;
    public bool   ReceiptShowDateTime     { get; set; } = true;
    public bool   ReceiptShowItemCodes    { get; set; } = false;
    public bool   ReceiptShowUnitPrice    { get; set; } = true;
    public bool   ReceiptShowLineDiscount { get; set; } = true;
    public bool   ReceiptShowTaxBreakdown { get; set; } = true;
    public bool   ReceiptShowDiscountLine { get; set; } = true;
    public bool   ReceiptShowBarcode      { get; set; } = true;
    public bool   ReceiptShowQrCode       { get; set; } = false;
    /// <summary>Barcode symbology printed on receipts: Code128 | Code39 | EAN13 | QR</summary>
    public string ReceiptBarcodeSymbology { get; set; } = "Code128";
    public bool   ReceiptAutoCutPaper     { get; set; } = true;
    public string? ReceiptHeaderNote      { get; set; }
    public string? ReceiptFooterMessage   { get; set; }
    public string? ReceiptReturnPolicy    { get; set; }

    // ── UI / Display ──────────────────────────────────────────────────────────
    /// <summary>light | dark</summary>
    public string  Theme                  { get; set; } = "light";
    /// <summary>Hex colour applied to the POS shell (e.g. "#1976D2"). Null = use default.</summary>
    public string? PrimaryColor           { get; set; }
    /// <summary>grid | list</summary>
    public string  DefaultProductView     { get; set; } = "grid";
    /// <summary>Show the on-screen numeric keypad for quantity/price entry.</summary>
    public bool    ShowNumpad             { get; set; } = true;
    /// <summary>Show the quick-access favourites bar above the product grid.</summary>
    public bool    ShowFavouritesBar      { get; set; } = true;
}
