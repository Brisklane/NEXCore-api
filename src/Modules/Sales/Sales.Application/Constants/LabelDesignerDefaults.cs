namespace Sales.Application.Constants;

/// <summary>
/// Single source of truth for the price-tag / barcode-label designer's numeric ranges.
/// Served to the UI (so it has zero hardcoded slider limits) and used by the renderer to clamp.
/// </summary>
public static class LabelDesignerDefaults
{
    public const double ProductNameFontMin = 4,  ProductNameFontMax = 12, ProductNameFontDefault = 8;
    public const double PriceFontMin       = 6,  PriceFontMax       = 16, PriceFontDefault       = 11;
    public const double BarcodeHeightMin   = 8,  BarcodeHeightMax   = 20, BarcodeHeightDefault   = 12;
    public const int    CopiesMin          = 1,  CopiesMax          = 500, CopiesDefault         = 1;
}
