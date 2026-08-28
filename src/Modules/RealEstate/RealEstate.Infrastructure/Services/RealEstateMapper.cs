using System.Globalization;
using RealEstate.Application.DTOs;
using RealEstate.Domain.Entities;
using RealEstate.Domain.Enums;

namespace RealEstate.Infrastructure.Services;

/// <summary>
/// Conversions and formatting every service needs, in one place.
///
/// Two of these carry real risk if they drift. **Area** is stored in square feet and read in
/// whatever the office uses, so a single wrong factor mis-prices an entire scheme. **Amount in
/// words** appears on allotment letters, receipts and transfer deeds, and in most of this market
/// the words are what a court reads when the figures are disputed.
/// </summary>
public static class RealEstateMapper
{
    // ── Area ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Square feet in one of each unit. The South Asian ones are the traditional definitions —
    /// a marla is 272.25 sq ft (a kanal being 20 marla, or one-eighth of an acre).
    /// </summary>
    private static readonly Dictionary<AreaUnit, decimal> SquareFeetPerUnit = new()
    {
        [AreaUnit.SquareFeet] = 1m,
        [AreaUnit.SquareMetre] = 10.7639104167m,
        [AreaUnit.SquareYard] = 9m,
        [AreaUnit.Marla] = 272.25m,
        [AreaUnit.Kanal] = 5445m,
        [AreaUnit.Acre] = 43560m,
        [AreaUnit.Hectare] = 107639.104167m,
        [AreaUnit.Bigha] = 27000m,
        [AreaUnit.Cent] = 435.6m,
        [AreaUnit.Guntha] = 1089m,
    };

    private static readonly Dictionary<AreaUnit, string> AreaUnitLabels = new()
    {
        [AreaUnit.SquareFeet] = "sq ft",
        [AreaUnit.SquareMetre] = "sq m",
        [AreaUnit.SquareYard] = "sq yd",
        [AreaUnit.Marla] = "marla",
        [AreaUnit.Kanal] = "kanal",
        [AreaUnit.Acre] = "acre",
        [AreaUnit.Hectare] = "hectare",
        [AreaUnit.Bigha] = "bigha",
        [AreaUnit.Cent] = "cent",
        [AreaUnit.Guntha] = "guntha",
    };

    /// <summary>Converts an operator-entered area into the canonical square feet the app stores.</summary>
    public static decimal ToSquareFeet(decimal value, AreaUnit from)
        => value * SquareFeetPerUnit[from];

    /// <summary>Converts stored square feet into whatever the office reads.</summary>
    public static decimal FromSquareFeet(decimal squareFeet, AreaUnit to)
        => squareFeet / SquareFeetPerUnit[to];

    public static string AreaUnitLabel(AreaUnit unit) => AreaUnitLabels[unit];

    /// <summary>
    /// An area in both units, formatted. Traditional units get two decimals ("10.5 marla") and
    /// square measures get none, because nobody quotes half a square foot.
    /// </summary>
    public static AreaDto Area(decimal? squareFeet, AreaUnit displayUnit)
    {
        var sqft = squareFeet ?? 0m;
        var display = FromSquareFeet(sqft, displayUnit);
        var decimals = displayUnit is AreaUnit.SquareFeet or AreaUnit.SquareMetre or AreaUnit.SquareYard ? 0 : 2;
        var rounded = Math.Round(display, decimals, MidpointRounding.AwayFromZero);

        return new AreaDto
        {
            SquareFeet = Math.Round(sqft, 2),
            DisplayValue = rounded,
            DisplayUnit = displayUnit,
            DisplayText = $"{rounded.ToString("N" + decimals, CultureInfo.InvariantCulture)} {AreaUnitLabel(displayUnit)}",
        };
    }

    public static AreaDto? AreaOrNull(decimal? squareFeet, AreaUnit displayUnit)
        => squareFeet is null or 0m ? null : Area(squareFeet, displayUnit);

    // ── Money in words ───────────────────────────────────────────────────────

    private static readonly string[] Ones =
    [
        "", "One", "Two", "Three", "Four", "Five", "Six", "Seven", "Eight", "Nine", "Ten",
        "Eleven", "Twelve", "Thirteen", "Fourteen", "Fifteen", "Sixteen", "Seventeen",
        "Eighteen", "Nineteen",
    ];

    private static readonly string[] Tens =
    [
        "", "", "Twenty", "Thirty", "Forty", "Fifty", "Sixty", "Seventy", "Eighty", "Ninety",
    ];

    /// <summary>
    /// The amount in words, printed on every document that carries a figure.
    ///
    /// Two numbering systems, because both are legally normal in this app's markets: the
    /// international scale (thousand / million / billion) and the South Asian one (thousand /
    /// lakh / crore). A Pakistani allotment letter that says "one million" instead of "ten lakh"
    /// reads as a foreign document.
    /// </summary>
    public static string AmountInWords(decimal amount, string currencyCode, bool useIndianSystem = false)
    {
        if (amount < 0) return "Minus " + AmountInWords(-amount, currencyCode, useIndianSystem);

        var whole = (long)Math.Truncate(amount);
        var fraction = (int)Math.Round((amount - whole) * 100, MidpointRounding.AwayFromZero);

        var words = whole == 0
            ? "Zero"
            : useIndianSystem ? IndianScale(whole) : InternationalScale(whole);

        var result = $"{CurrencyWord(currencyCode)} {words}";
        if (fraction > 0) result += $" and {InternationalScale(fraction)} {SubUnitWord(currencyCode)}";

        return result + " only";
    }

    private static string InternationalScale(long value)
    {
        if (value == 0) return string.Empty;
        if (value < 20) return Ones[value];
        if (value < 100)
        {
            var rest = value % 10;
            return Tens[value / 10] + (rest > 0 ? " " + Ones[rest] : string.Empty);
        }
        if (value < 1_000) return Group(value, 100, "Hundred", InternationalScale);
        if (value < 1_000_000) return Group(value, 1_000, "Thousand", InternationalScale);
        if (value < 1_000_000_000) return Group(value, 1_000_000, "Million", InternationalScale);
        return Group(value, 1_000_000_000, "Billion", InternationalScale);
    }

    private static string IndianScale(long value)
    {
        if (value == 0) return string.Empty;
        if (value < 20) return Ones[value];
        if (value < 100)
        {
            var rest = value % 10;
            return Tens[value / 10] + (rest > 0 ? " " + Ones[rest] : string.Empty);
        }
        if (value < 1_000) return Group(value, 100, "Hundred", IndianScale);
        if (value < 100_000) return Group(value, 1_000, "Thousand", IndianScale);
        if (value < 10_000_000) return Group(value, 100_000, "Lakh", IndianScale);
        return Group(value, 10_000_000, "Crore", IndianScale);
    }

    private static string Group(long value, long divisor, string label, Func<long, string> recurse)
    {
        var head = recurse(value / divisor);
        var tail = value % divisor;
        return $"{head} {label}" + (tail > 0 ? " " + recurse(tail) : string.Empty);
    }

    private static string CurrencyWord(string code) => code.ToUpperInvariant() switch
    {
        "PKR" => "Rupees",
        "INR" => "Rupees",
        "LKR" => "Rupees",
        "NPR" => "Rupees",
        "AED" => "Dirhams",
        "SAR" => "Riyals",
        "USD" => "Dollars",
        "GBP" => "Pounds",
        "EUR" => "Euros",
        _ => code.ToUpperInvariant(),
    };

    private static string SubUnitWord(string code) => code.ToUpperInvariant() switch
    {
        "PKR" or "INR" or "LKR" or "NPR" => "Paisa",
        "AED" => "Fils",
        "SAR" => "Halalas",
        "USD" => "Cents",
        "GBP" => "Pence",
        "EUR" => "Cents",
        _ => "Cents",
    };

    /// <summary>Indian/Pakistani lakh-crore grouping is the norm for these currencies.</summary>
    public static bool UsesIndianScale(string currencyCode)
        => currencyCode.ToUpperInvariant() is "PKR" or "INR" or "LKR" or "NPR" or "BDT";

    // ── Small shared shapes ──────────────────────────────────────────────────

    public static AddressDto Address(Property p) => new()
    {
        Line1 = p.AddressLine1,
        Line2 = p.AddressLine2,
        Street = p.Street,
        PostCode = p.PostCode,
        Latitude = p.Latitude,
        Longitude = p.Longitude,
        OneLine = OneLineAddress(p),
    };

    public static string OneLineAddress(Property p)
    {
        var parts = new[]
        {
            p.UnitNumber is null ? null : $"Unit {p.UnitNumber}",
            p.AddressLine1,
            p.AddressLine2,
            p.Street,
            p.PostCode,
        };
        return string.Join(", ", parts.Where(x => !string.IsNullOrWhiteSpace(x)));
    }

    public static string OneLineAddress(PartyAddress a)
    {
        var parts = new[] { a.Line1, a.Line2, a.Street, a.City, a.State, a.PostCode };
        return string.Join(", ", parts.Where(x => !string.IsNullOrWhiteSpace(x)));
    }

    /// <summary>A party's display name, whichever kind they are.</summary>
    public static string DisplayName(Party p)
    {
        if (!string.IsNullOrWhiteSpace(p.DisplayName)) return p.DisplayName;
        if (p.Kind == PartyKind.Organisation)
            return p.OrganisationName ?? p.TradingName ?? p.Reference;

        var name = string.Join(" ", new[] { p.Salutation, p.FirstName, p.MiddleName, p.LastName }
            .Where(x => !string.IsNullOrWhiteSpace(x)));

        return string.IsNullOrWhiteSpace(name) ? p.Reference : name;
    }

    /// <summary>Which ageing bucket a number of days overdue falls into.</summary>
    public static string AgeingBucket(int daysOverdue) => daysOverdue switch
    {
        <= 0 => "Current",
        <= 30 => "1-30",
        <= 60 => "31-60",
        <= 90 => "61-90",
        _ => "90+",
    };

    /// <summary>Days between two dates, floored at zero — an instalment cannot be negatively late.</summary>
    public static int DaysOverdue(DateOnly? dueDate, DateOnly asOf)
        => dueDate is null ? 0 : Math.Max(0, asOf.DayNumber - dueDate.Value.DayNumber);

    /// <summary>A percentage guarded against a zero denominator, which is the usual bug here.</summary>
    public static decimal Percent(decimal part, decimal whole)
        => whole == 0m ? 0m : Math.Round(part / whole * 100m, 2);

    /// <summary>Rounds money to the currency's places. Never <c>double</c>, never banker's rounding.</summary>
    public static decimal Money(decimal value, int decimals = 2)
        => Math.Round(value, decimals, MidpointRounding.AwayFromZero);
}
