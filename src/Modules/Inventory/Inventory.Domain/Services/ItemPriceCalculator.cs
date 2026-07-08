namespace Inventory.Domain.Services;

/// <summary>
/// Item Price Calculator
/// Centralises all inclusive / exclusive tax arithmetic for item prices.
///
/// Two entry points:
///   CalculateSalePrice()     ? fills sale-side computed fields on an ItemPrice
///   CalculatePurchasePrice() ? fills purchase-side computed fields on an ItemPrice
///   Calculate()              ? fills ALL fields in one call
///
/// Tax extraction  (inclusive ? exclusive):
///   Net  = Gross / (1 + rate)          ? extract tax from gross price
///   Tax  = Gross ? Net
///
/// Tax addition    (exclusive ? inclusive):
///   Gross = Net × (1 + rate)           ? add tax on top of net price
///   Tax   = Gross ? Net
///
/// Multiple taxes are treated as a combined rate (sum of effective rates).
/// Example: VAT 15% + Excise 5% = 20% combined.
///
/// Compound / cascaded taxes (tax-on-tax) are NOT modelled here — all taxes
/// are applied to the base price only (non-compounding).
/// </summary>
public static class ItemPriceCalculator
{
    /// <summary>
    /// Returns the combined effective tax rate from a collection of
    /// (rate, taxType, isInclusive) tuples for a given direction
    /// (ApplyOnSales or ApplyOnPurchases).
    ///
    /// Only taxes whose isInclusive matches the price's IsTaxInclusive flag
    /// are included — mixed inclusive/exclusive taxes on the same item are
    /// handled by treating each tax independently and then summing.
    /// </summary>
    /// <param name="taxes">
    /// Sequence of (effectiveRate, taxType, isInclusive) for every active ItemTax.
    /// taxType: "Percentage" | "FixedAmount"
    /// </param>
    /// <param name="basePrice">Net price used for FixedAmount rate normalisation</param>
    public static decimal CombinedRateFromTaxes(
        IEnumerable<(decimal EffectiveRate, bool IsPercentage, bool IsInclusive)> taxes,
        decimal basePrice)
    {
        decimal combined = 0m;
        foreach (var (rate, isPercentage, _) in taxes)
        {
            combined += !isPercentage && basePrice > 0
                ? rate / basePrice          // normalise fixed amount to a "rate" for uniform arithmetic
                : rate / 100m;              // percentage ? decimal fraction
        }
        return combined;
    }

    // ?? Core arithmetic ?????????????????????????????????????????????????????

    /// <summary>
    /// Given a tax-inclusive gross price, extract the net (pre-tax) price and tax amount.
    /// </summary>
    public static (decimal Net, decimal TaxAmount) ExtractTax(decimal grossPrice, decimal combinedRate)
    {
        if (combinedRate <= 0m) return (grossPrice, 0m);
        var net = grossPrice / (1m + combinedRate);
        return (Round(net), Round(grossPrice - net));
    }

    /// <summary>
    /// Given a tax-exclusive net price, calculate the gross (tax-inclusive) price and tax amount.
    /// </summary>
    public static (decimal Gross, decimal TaxAmount) AddTax(decimal netPrice, decimal combinedRate)
    {
        if (combinedRate <= 0m) return (netPrice, 0m);
        var taxAmount = netPrice * combinedRate;
        return (Round(netPrice + taxAmount), Round(taxAmount));
    }

    // ?? High-level helpers ??????????????????????????????????????????????????

    /// <summary>
    /// Calculate all three sale-price fields and write them back to the price record.
    /// Also stores the EffectiveTaxRate on the record.
    /// </summary>
    /// <param name="price">The ItemPrice record to update (mutated in-place).</param>
    /// <param name="combinedTaxRate">
    /// Combined effective tax rate as a decimal fraction (e.g., 0.15 for 15%).
    /// Pass the result of <see cref="CombinedRateFromTaxes"/> for the sale direction.
    /// </param>
    public static void CalculateSalePrice(ItemPriceRecord price, decimal combinedTaxRate)
    {
        price.EffectiveTaxRate = Round(combinedTaxRate * 100m); // store as percentage for display

        if (price.IsTaxInclusive)
        {
            // entered price is gross — extract tax to get net
            var (net, tax) = ExtractTax(price.SalePrice, combinedTaxRate);
            price.SalePriceExcludingTax = net;
            price.SaleTaxAmount         = tax;
            price.SalePriceIncludingTax = price.SalePrice;
        }
        else
        {
            // entered price is net — add tax to get gross
            var (gross, tax) = AddTax(price.SalePrice, combinedTaxRate);
            price.SalePriceExcludingTax = price.SalePrice;
            price.SaleTaxAmount         = tax;
            price.SalePriceIncludingTax = gross;
        }
    }

    /// <summary>
    /// Calculate all three purchase-price fields and write them back to the price record.
    /// </summary>
    public static void CalculatePurchasePrice(ItemPriceRecord price, decimal combinedTaxRate)
    {
        if (price.IsTaxInclusive)
        {
            var (net, tax) = ExtractTax(price.PurchasePrice, combinedTaxRate);
            price.PurchasePriceExcludingTax = net;
            price.PurchaseTaxAmount          = tax;
            price.PurchasePriceIncludingTax  = price.PurchasePrice;
        }
        else
        {
            var (gross, tax) = AddTax(price.PurchasePrice, combinedTaxRate);
            price.PurchasePriceExcludingTax = price.PurchasePrice;
            price.PurchaseTaxAmount          = tax;
            price.PurchasePriceIncludingTax  = gross;
        }
    }

    /// <summary>
    /// Calculate all six price/tax fields (sale + purchase) in one call.
    /// </summary>
    public static void Calculate(ItemPriceRecord price, decimal combinedTaxRate)
    {
        CalculateSalePrice(price, combinedTaxRate);
        CalculatePurchasePrice(price, combinedTaxRate);
    }

    // ?? Convenience overloads that work directly with ItemPrice entity ??????

    /// <summary>
    /// Populate all computed fields on an <see cref="Inventory.Domain.Entities.ItemPrice"/>
    /// from its associated item taxes.
    /// </summary>
    /// <param name="price">The EF entity to recalculate.</param>
    /// <param name="activeSalesTaxes">
    /// Active ItemTax rows for this item with ApplyOnSales = true.
    /// </param>
    /// <param name="activePurchaseTaxes">
    /// Active ItemTax rows for this item with ApplyOnPurchases = true.
    /// </param>
    public static void RecalculateEntity(
        Inventory.Domain.Entities.ItemPrice price,
        IEnumerable<(decimal EffectiveRate, bool IsPercentage, bool IsInclusive)> activeSalesTaxes,
        IEnumerable<(decimal EffectiveRate, bool IsPercentage, bool IsInclusive)> activePurchaseTaxes)
    {
        var saleRate     = CombinedRateFromTaxes(activeSalesTaxes, price.SalePrice);
        var purchaseRate = CombinedRateFromTaxes(activePurchaseTaxes, price.PurchasePrice);

        var record = ItemPriceRecord.FromEntity(price);

        CalculateSalePrice(record, saleRate);
        CalculatePurchasePrice(record, purchaseRate);

        record.ApplyToEntity(price);
    }

    private static decimal Round(decimal value) => Math.Round(value, 4, MidpointRounding.AwayFromZero);
}

/// <summary>
/// Mutable value object used by the calculator to avoid coupling
/// the pure arithmetic to the EF entity directly.
/// </summary>
public sealed class ItemPriceRecord
{
    public decimal SalePrice                { get; set; }
    public decimal PurchasePrice            { get; set; }
    public bool    IsTaxInclusive           { get; set; }

    // Sale computed
    public decimal SalePriceExcludingTax    { get; set; }
    public decimal SaleTaxAmount            { get; set; }
    public decimal SalePriceIncludingTax    { get; set; }

    // Purchase computed
    public decimal PurchasePriceExcludingTax { get; set; }
    public decimal PurchaseTaxAmount         { get; set; }
    public decimal PurchasePriceIncludingTax { get; set; }

    // Stored rate (percentage, e.g. 15.00 for 15%)
    public decimal EffectiveTaxRate          { get; set; }

    public static ItemPriceRecord FromEntity(Inventory.Domain.Entities.ItemPrice e) => new()
    {
        SalePrice     = e.SalePrice,
        PurchasePrice = e.PurchasePrice,
        IsTaxInclusive = e.IsTaxInclusive
    };

    public void ApplyToEntity(Inventory.Domain.Entities.ItemPrice e)
    {
        e.SalePriceExcludingTax     = SalePriceExcludingTax;
        e.SaleTaxAmount             = SaleTaxAmount;
        e.SalePriceIncludingTax     = SalePriceIncludingTax;
        e.PurchasePriceExcludingTax = PurchasePriceExcludingTax;
        e.PurchaseTaxAmount         = PurchaseTaxAmount;
        e.PurchasePriceIncludingTax = PurchasePriceIncludingTax;
        e.EffectiveTaxRate          = EffectiveTaxRate;
    }
}
