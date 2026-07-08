using Xunit;
using Inventory.Domain.Services;
using Inventory.Domain.Entities;

namespace Inventory.Tests.Unit.Domain;

/// <summary>
/// Unit tests for ItemPriceCalculator covering:
///   - Tax-exclusive entry (IsTaxInclusive = false) ? add tax on top
///   - Tax-inclusive entry (IsTaxInclusive = true)  ? extract tax from price
///   - Zero tax rate
///   - Multiple taxes (combined rate)
///   - FixedAmount tax type
///   - RecalculateEntity convenience overload
///   - Rounding (4 decimal places, AwayFromZero)
/// </summary>
public class ItemPriceCalculatorTests
{
    // ?? Helpers ??????????????????????????????????????????????????????????????

    private static ItemPriceRecord ExclusiveRecord(decimal salePrice, decimal purchasePrice) => new()
    {
        SalePrice     = salePrice,
        PurchasePrice = purchasePrice,
        IsTaxInclusive = false
    };

    private static ItemPriceRecord InclusiveRecord(decimal salePrice, decimal purchasePrice) => new()
    {
        SalePrice     = salePrice,
        PurchasePrice = purchasePrice,
        IsTaxInclusive = true
    };

    // ?? CombinedRateFromTaxes ?????????????????????????????????????????????

    [Fact]
    public void CombinedRate_SinglePercentageTax_ReturnsFraction()
    {
        var taxes = new[] { (15m, true, false) };
        var rate  = ItemPriceCalculator.CombinedRateFromTaxes(taxes, 100m);
        Assert.Equal(0.15m, rate);
    }

    [Fact]
    public void CombinedRate_MultipleTaxes_SumsRates()
    {
        // VAT 15% + Excise 5% = 20%
        var taxes = new[]
        {
            (15m, true, false),
            (5m,  true, false)
        };
        var rate = ItemPriceCalculator.CombinedRateFromTaxes(taxes, 100m);
        Assert.Equal(0.20m, rate);
    }

    [Fact]
    public void CombinedRate_FixedAmountTax_NormalisesToRate()
    {
        // $10 fixed tax on $100 item = 10% effective rate
        var taxes = new[] { (10m, false, false) };   // isPercentage = false ? fixed amount
        var rate  = ItemPriceCalculator.CombinedRateFromTaxes(taxes, 100m);
        Assert.Equal(0.10m, rate);
    }

    [Fact]
    public void CombinedRate_EmptyTaxes_ReturnsZero()
    {
        var rate = ItemPriceCalculator.CombinedRateFromTaxes([], 100m);
        Assert.Equal(0m, rate);
    }

    // ?? ExtractTax (inclusive ? exclusive) ???????????????????????????????

    [Fact]
    public void ExtractTax_15Percent_CorrectNetAndTax()
    {
        var (net, tax) = ItemPriceCalculator.ExtractTax(115m, 0.15m);
        Assert.Equal(100m, net);
        Assert.Equal(15m,  tax);
    }

    [Fact]
    public void ExtractTax_ZeroRate_ReturnsPriceUnchanged()
    {
        var (net, tax) = ItemPriceCalculator.ExtractTax(100m, 0m);
        Assert.Equal(100m, net);
        Assert.Equal(0m,   tax);
    }

    [Fact]
    public void ExtractTax_20Percent_CorrectNetAndTax()
    {
        // gross = 120, rate = 20% ? net = 100, tax = 20
        var (net, tax) = ItemPriceCalculator.ExtractTax(120m, 0.20m);
        Assert.Equal(100m, net);
        Assert.Equal(20m,  tax);
    }

    [Fact]
    public void ExtractTax_NetPlusTaxEqualsGross()
    {
        var gross = 230.50m;
        var (net, tax) = ItemPriceCalculator.ExtractTax(gross, 0.15m);
        Assert.Equal(gross, Math.Round(net + tax, 4));
    }

    // ?? AddTax (exclusive ? inclusive) ???????????????????????????????????

    [Fact]
    public void AddTax_15Percent_CorrectGrossAndTax()
    {
        var (gross, tax) = ItemPriceCalculator.AddTax(100m, 0.15m);
        Assert.Equal(115m, gross);
        Assert.Equal(15m,  tax);
    }

    [Fact]
    public void AddTax_ZeroRate_ReturnsPriceUnchanged()
    {
        var (gross, tax) = ItemPriceCalculator.AddTax(100m, 0m);
        Assert.Equal(100m, gross);
        Assert.Equal(0m,   tax);
    }

    [Fact]
    public void AddTax_20Percent_CorrectGrossAndTax()
    {
        var (gross, tax) = ItemPriceCalculator.AddTax(100m, 0.20m);
        Assert.Equal(120m, gross);
        Assert.Equal(20m,  tax);
    }

    [Fact]
    public void AddTax_NetPlusTaxEqualsGross()
    {
        var net = 199.99m;
        var (gross, tax) = ItemPriceCalculator.AddTax(net, 0.15m);
        Assert.Equal(gross, Math.Round(net + tax, 4));
    }

    // ?? CalculateSalePrice — exclusive entry (add tax) ????????????????????

    [Fact]
    public void CalculateSalePrice_Exclusive_15Pct_FieldsCorrect()
    {
        var record = ExclusiveRecord(salePrice: 100m, purchasePrice: 80m);
        ItemPriceCalculator.CalculateSalePrice(record, 0.15m);

        Assert.Equal(100m,  record.SalePriceExcludingTax);
        Assert.Equal(15m,   record.SaleTaxAmount);
        Assert.Equal(115m,  record.SalePriceIncludingTax);
        Assert.Equal(15m,   record.EffectiveTaxRate);   // stored as percentage
    }

    [Fact]
    public void CalculateSalePrice_Exclusive_ZeroTax_AllThreeSame()
    {
        var record = ExclusiveRecord(salePrice: 100m, purchasePrice: 80m);
        ItemPriceCalculator.CalculateSalePrice(record, 0m);

        Assert.Equal(100m, record.SalePriceExcludingTax);
        Assert.Equal(0m,   record.SaleTaxAmount);
        Assert.Equal(100m, record.SalePriceIncludingTax);
    }

    // ?? CalculateSalePrice — inclusive entry (extract tax) ????????????????

    [Fact]
    public void CalculateSalePrice_Inclusive_15Pct_FieldsCorrect()
    {
        var record = InclusiveRecord(salePrice: 115m, purchasePrice: 92m);
        ItemPriceCalculator.CalculateSalePrice(record, 0.15m);

        Assert.Equal(100m,  record.SalePriceExcludingTax);
        Assert.Equal(15m,   record.SaleTaxAmount);
        Assert.Equal(115m,  record.SalePriceIncludingTax);
    }

    [Fact]
    public void CalculateSalePrice_Inclusive_EnteredPriceIsGross()
    {
        // SalePriceIncludingTax must equal the entered SalePrice when inclusive
        var record = InclusiveRecord(salePrice: 230m, purchasePrice: 0m);
        ItemPriceCalculator.CalculateSalePrice(record, 0.15m);
        Assert.Equal(record.SalePrice, record.SalePriceIncludingTax);
    }

    // ?? CalculatePurchasePrice ?????????????????????????????????????????????

    [Fact]
    public void CalculatePurchasePrice_Exclusive_15Pct_FieldsCorrect()
    {
        var record = ExclusiveRecord(salePrice: 100m, purchasePrice: 80m);
        ItemPriceCalculator.CalculatePurchasePrice(record, 0.15m);

        Assert.Equal(80m,   record.PurchasePriceExcludingTax);
        Assert.Equal(12m,   record.PurchaseTaxAmount);
        Assert.Equal(92m,   record.PurchasePriceIncludingTax);
    }

    [Fact]
    public void CalculatePurchasePrice_Inclusive_15Pct_FieldsCorrect()
    {
        var record = InclusiveRecord(salePrice: 0m, purchasePrice: 92m);
        ItemPriceCalculator.CalculatePurchasePrice(record, 0.15m);

        // 92 / 1.15 = 80
        Assert.Equal(80m,  record.PurchasePriceExcludingTax);
        Assert.Equal(12m,  record.PurchaseTaxAmount);
        Assert.Equal(92m,  record.PurchasePriceIncludingTax);
    }

    // ?? Calculate (both sale + purchase) ?????????????????????????????????

    [Fact]
    public void Calculate_Exclusive_PopulatesAllSixFields()
    {
        var record = ExclusiveRecord(salePrice: 200m, purchasePrice: 160m);
        ItemPriceCalculator.Calculate(record, 0.10m);

        // Sale
        Assert.Equal(200m, record.SalePriceExcludingTax);
        Assert.Equal(20m,  record.SaleTaxAmount);
        Assert.Equal(220m, record.SalePriceIncludingTax);

        // Purchase
        Assert.Equal(160m, record.PurchasePriceExcludingTax);
        Assert.Equal(16m,  record.PurchaseTaxAmount);
        Assert.Equal(176m, record.PurchasePriceIncludingTax);
    }

    [Fact]
    public void Calculate_Inclusive_PopulatesAllSixFields()
    {
        var record = InclusiveRecord(salePrice: 220m, purchasePrice: 176m);
        ItemPriceCalculator.Calculate(record, 0.10m);

        // Sale: 220 / 1.10 = 200, tax = 20
        Assert.Equal(200m, record.SalePriceExcludingTax);
        Assert.Equal(20m,  record.SaleTaxAmount);
        Assert.Equal(220m, record.SalePriceIncludingTax);

        // Purchase: 176 / 1.10 = 160, tax = 16
        Assert.Equal(160m, record.PurchasePriceExcludingTax);
        Assert.Equal(16m,  record.PurchaseTaxAmount);
        Assert.Equal(176m, record.PurchasePriceIncludingTax);
    }

    // ?? RecalculateEntity ?????????????????????????????????????????????????

    [Fact]
    public void RecalculateEntity_Exclusive_WritesFieldsToEntity()
    {
        var price = new ItemPrice
        {
            SalePrice     = 100m,
            PurchasePrice = 80m,
            IsTaxInclusive = false
        };

        var salesTaxes    = new[] { (15m, true, false) };
        var purchaseTaxes = new[] { (15m, true, false) };

        ItemPriceCalculator.RecalculateEntity(price, salesTaxes, purchaseTaxes);

        Assert.Equal(100m, price.SalePriceExcludingTax);
        Assert.Equal(15m,  price.SaleTaxAmount);
        Assert.Equal(115m, price.SalePriceIncludingTax);

        Assert.Equal(80m,  price.PurchasePriceExcludingTax);
        Assert.Equal(12m,  price.PurchaseTaxAmount);
        Assert.Equal(92m,  price.PurchasePriceIncludingTax);

        Assert.Equal(15m,  price.EffectiveTaxRate);
    }

    [Fact]
    public void RecalculateEntity_Inclusive_ExtractsTaxCorrectly()
    {
        var price = new ItemPrice
        {
            SalePrice     = 115m,
            PurchasePrice = 92m,
            IsTaxInclusive = true
        };

        var taxes = new[] { (15m, true, true) };

        ItemPriceCalculator.RecalculateEntity(price, taxes, taxes);

        Assert.Equal(100m, price.SalePriceExcludingTax);
        Assert.Equal(15m,  price.SaleTaxAmount);
        Assert.Equal(80m,  price.PurchasePriceExcludingTax);
        Assert.Equal(12m,  price.PurchaseTaxAmount);
    }

    [Fact]
    public void RecalculateEntity_NoTaxes_ComputedEqualsEntered()
    {
        var price = new ItemPrice
        {
            SalePrice     = 500m,
            PurchasePrice = 400m,
            IsTaxInclusive = false
        };

        ItemPriceCalculator.RecalculateEntity(price, [], []);

        Assert.Equal(500m, price.SalePriceExcludingTax);
        Assert.Equal(0m,   price.SaleTaxAmount);
        Assert.Equal(500m, price.SalePriceIncludingTax);
        Assert.Equal(400m, price.PurchasePriceExcludingTax);
        Assert.Equal(0m,   price.PurchaseTaxAmount);
        Assert.Equal(400m, price.PurchasePriceIncludingTax);
    }

    // ?? Rounding ??????????????????????????????????????????????????????????

    [Fact]
    public void AddTax_NonRoundPrice_RoundsTo4DecimalPlaces()
    {
        // 99.99 * 0.15 = 14.9985 ? rounds to 14.9985 (4dp)
        var (gross, tax) = ItemPriceCalculator.AddTax(99.99m, 0.15m);
        Assert.Equal(4, BitConverter.GetBytes(decimal.GetBits(tax)[3])[2]); // scale = 4
        Assert.Equal(gross, Math.Round(99.99m + tax, 4));
    }

    [Fact]
    public void ExtractTax_NonRoundPrice_NetPlusTaxEqualsGross()
    {
        var gross = 99.99m;
        var (net, tax) = ItemPriceCalculator.ExtractTax(gross, 0.15m);
        // net + tax must reconstruct gross within 4dp rounding tolerance
        Assert.True(Math.Abs(gross - (net + tax)) < 0.0001m);
    }

    // ?? Combined VAT + Excise scenario ???????????????????????????????????

    [Fact]
    public void Calculate_CombinedVatAndExcise_15Plus5_CorrectTotal()
    {
        var taxes = new[]
        {
            (15m, true, false),
            (5m,  true, false)
        };
        var rate   = ItemPriceCalculator.CombinedRateFromTaxes(taxes, 100m);
        var record = ExclusiveRecord(100m, 80m);
        ItemPriceCalculator.CalculateSalePrice(record, rate);

        Assert.Equal(100m, record.SalePriceExcludingTax);
        Assert.Equal(20m,  record.SaleTaxAmount);
        Assert.Equal(120m, record.SalePriceIncludingTax);
    }
}
