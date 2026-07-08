using Microsoft.Extensions.Logging;
using Sales.Application.DTOs;
using Sales.Application.Services.Interfaces;
using Sales.Domain.Entities;
using Sales.Infrastructure.Repositories.Interfaces;

namespace Sales.Infrastructure.Services;

public class PosSettingsService : IPosSettingsService
{
    private readonly IPosSettingsRepository _repo;
    private readonly ILogger<PosSettingsService> _logger;

    public PosSettingsService(IPosSettingsRepository repo, ILogger<PosSettingsService> logger)
    {
        _repo   = repo;
        _logger = logger;
    }

    public async Task<PosSettingsDto> GetAsync()
    {
        var settings = await _repo.GetCurrentAsync();
        if (settings is null)
        {
            settings = new PosSettings();
            await _repo.AddAsync(settings);
            await _repo.SaveChangesAsync();
            _logger.LogInformation("POS settings auto-created with defaults");
        }
        return MapToDto(settings);
    }

    public async Task<PosSettingsDto> UpdateAsync(UpdatePosSettingsDto dto)
    {
        var settings = await _repo.GetCurrentAsync();
        if (settings is null)
        {
            settings = new PosSettings();
            await _repo.AddAsync(settings);
        }

        // General
        if (dto.RequireCashierPin     != null) settings.RequireCashierPin     = dto.RequireCashierPin.Value;
        if (dto.AutoLockMinutes       != null) settings.AutoLockMinutes       = dto.AutoLockMinutes.Value;
        if (dto.AllowPriceOverride    != null) settings.AllowPriceOverride    = dto.AllowPriceOverride.Value;
        if (dto.AllowDiscount         != null) settings.AllowDiscount         = dto.AllowDiscount.Value;
        if (dto.MaxDiscountPercent    != null) settings.MaxDiscountPercent    = dto.MaxDiscountPercent.Value;
        if (dto.RequireCustomer       != null) settings.RequireCustomer       = dto.RequireCustomer.Value;
        if (dto.DefaultCustomerId     != null) settings.DefaultCustomerId     = dto.DefaultCustomerId;
        if (dto.AllowNegativeStock    != null) settings.AllowNegativeStock    = dto.AllowNegativeStock.Value;
        if (dto.TaxInclusivePricing   != null) settings.TaxInclusivePricing   = dto.TaxInclusivePricing.Value;
        if (dto.AutoApplyTax          != null) settings.AutoApplyTax          = dto.AutoApplyTax.Value;
        if (dto.DefaultCurrencyCode   != null) settings.DefaultCurrencyCode   = dto.DefaultCurrencyCode;
        if (dto.RoundingValue         != null) settings.RoundingValue         = dto.RoundingValue.Value;
        if (dto.RoundingMode          != null) settings.RoundingMode          = dto.RoundingMode;

        // Payment
        if (dto.AcceptCash            != null) settings.AcceptCash            = dto.AcceptCash.Value;
        if (dto.AcceptCard            != null) settings.AcceptCard            = dto.AcceptCard.Value;
        if (dto.AcceptMobilePayment   != null) settings.AcceptMobilePayment   = dto.AcceptMobilePayment.Value;
        if (dto.AcceptCreditOnAccount != null) settings.AcceptCreditOnAccount = dto.AcceptCreditOnAccount.Value;
        if (dto.AllowSplitPayment     != null) settings.AllowSplitPayment     = dto.AllowSplitPayment.Value;
        if (dto.AllowPartialPayment   != null) settings.AllowPartialPayment   = dto.AllowPartialPayment.Value;
        if (dto.DefaultPaymentMethod  != null) settings.DefaultPaymentMethod  = dto.DefaultPaymentMethod;
        if (dto.AutoOpenCashDrawer    != null) settings.AutoOpenCashDrawer    = dto.AutoOpenCashDrawer.Value;
        if (dto.MinOrderAmount        != null) settings.MinOrderAmount        = dto.MinOrderAmount;
        if (dto.MaxOrderAmount        != null) settings.MaxOrderAmount        = dto.MaxOrderAmount;
        if (dto.CashGlAccountNumber   != null) settings.CashGlAccountNumber   = dto.CashGlAccountNumber == "" ? null : dto.CashGlAccountNumber;
        if (dto.BankGlAccountNumber   != null) settings.BankGlAccountNumber   = dto.BankGlAccountNumber == "" ? null : dto.BankGlAccountNumber;

        // Cart & Products
        if (dto.ShowProductImages     != null) settings.ShowProductImages     = dto.ShowProductImages.Value;
        if (dto.ShowProductDescription!= null) settings.ShowProductDescription= dto.ShowProductDescription.Value;
        if (dto.AllowItemNotes        != null) settings.AllowItemNotes        = dto.AllowItemNotes.Value;
        if (dto.AllowDecimalQuantity  != null) settings.AllowDecimalQuantity  = dto.AllowDecimalQuantity.Value;
        if (dto.BarcodeScanSound      != null) settings.BarcodeScanSound      = dto.BarcodeScanSound.Value;
        if (dto.ShowStockLevel        != null) settings.ShowStockLevel        = dto.ShowStockLevel.Value;
        if (dto.LowStockThreshold     != null) settings.LowStockThreshold     = dto.LowStockThreshold.Value;
        if (dto.ItemsPerPage          != null) settings.ItemsPerPage          = dto.ItemsPerPage.Value;
        if (dto.ShowCategoryFilter    != null) settings.ShowCategoryFilter    = dto.ShowCategoryFilter.Value;
        if (dto.TouchMode             != null) settings.TouchMode             = dto.TouchMode.Value;

        // Connectivity / Operating Mode
        if (dto.OperatingMode            != null) settings.OperatingMode            = dto.OperatingMode;

        // Session
        if (dto.RequireOpeningFloat      != null) settings.RequireOpeningFloat      = dto.RequireOpeningFloat.Value;
        if (dto.RequireCashCountOnClose  != null) settings.RequireCashCountOnClose  = dto.RequireCashCountOnClose.Value;
        if (dto.AutoCloseSession         != null) settings.AutoCloseSession         = dto.AutoCloseSession.Value;
        if (dto.AutoCloseTime            != null) settings.AutoCloseTime            = dto.AutoCloseTime;

        // Loyalty & Promotions
        if (dto.EnableLoyaltyPoints   != null) settings.EnableLoyaltyPoints   = dto.EnableLoyaltyPoints.Value;
        if (dto.EnablePromotions      != null) settings.EnablePromotions      = dto.EnablePromotions.Value;
        if (dto.EnableCoupons         != null) settings.EnableCoupons         = dto.EnableCoupons.Value;
        if (dto.AutoApplyPromotions   != null) settings.AutoApplyPromotions   = dto.AutoApplyPromotions.Value;

        // Receipt
        if (dto.DefaultPaperSize         != null) settings.DefaultPaperSize         = dto.DefaultPaperSize;
        if (dto.ReceiptCopies            != null) settings.ReceiptCopies            = dto.ReceiptCopies.Value;
        if (dto.AutoPrintReceipt         != null) settings.AutoPrintReceipt         = dto.AutoPrintReceipt.Value;
        if (dto.AskToPrintReceipt        != null) settings.AskToPrintReceipt        = dto.AskToPrintReceipt.Value;
        if (dto.SkipReceiptScreen        != null) settings.SkipReceiptScreen        = dto.SkipReceiptScreen.Value;
        if (dto.ReceiptShowLogo          != null) settings.ReceiptShowLogo          = dto.ReceiptShowLogo.Value;
        if (dto.ReceiptShowBusinessName  != null) settings.ReceiptShowBusinessName  = dto.ReceiptShowBusinessName.Value;
        if (dto.ReceiptShowAddress       != null) settings.ReceiptShowAddress       = dto.ReceiptShowAddress.Value;
        if (dto.ReceiptShowContact       != null) settings.ReceiptShowContact       = dto.ReceiptShowContact.Value;
        if (dto.ReceiptShowCashierName   != null) settings.ReceiptShowCashierName   = dto.ReceiptShowCashierName.Value;
        if (dto.ReceiptShowCustomerName  != null) settings.ReceiptShowCustomerName  = dto.ReceiptShowCustomerName.Value;
        if (dto.ReceiptShowOrderNumber   != null) settings.ReceiptShowOrderNumber   = dto.ReceiptShowOrderNumber.Value;
        if (dto.ReceiptShowDateTime      != null) settings.ReceiptShowDateTime      = dto.ReceiptShowDateTime.Value;
        if (dto.ReceiptShowItemCodes     != null) settings.ReceiptShowItemCodes     = dto.ReceiptShowItemCodes.Value;
        if (dto.ReceiptShowUnitPrice     != null) settings.ReceiptShowUnitPrice     = dto.ReceiptShowUnitPrice.Value;
        if (dto.ReceiptShowLineDiscount  != null) settings.ReceiptShowLineDiscount  = dto.ReceiptShowLineDiscount.Value;
        if (dto.ReceiptShowTaxBreakdown  != null) settings.ReceiptShowTaxBreakdown  = dto.ReceiptShowTaxBreakdown.Value;
        if (dto.ReceiptShowDiscountLine  != null) settings.ReceiptShowDiscountLine  = dto.ReceiptShowDiscountLine.Value;
        if (dto.ReceiptShowBarcode       != null) settings.ReceiptShowBarcode       = dto.ReceiptShowBarcode.Value;
        if (dto.ReceiptShowQrCode        != null) settings.ReceiptShowQrCode        = dto.ReceiptShowQrCode.Value;
        if (dto.ReceiptBarcodeSymbology  != null) settings.ReceiptBarcodeSymbology  = dto.ReceiptBarcodeSymbology;
        if (dto.ReceiptAutoCutPaper      != null) settings.ReceiptAutoCutPaper      = dto.ReceiptAutoCutPaper.Value;
        if (dto.ReceiptHeaderNote        != null) settings.ReceiptHeaderNote        = dto.ReceiptHeaderNote;
        if (dto.ReceiptFooterMessage     != null) settings.ReceiptFooterMessage     = dto.ReceiptFooterMessage;
        if (dto.ReceiptReturnPolicy      != null) settings.ReceiptReturnPolicy      = dto.ReceiptReturnPolicy;

        // UI / Display
        if (dto.Theme              != null) settings.Theme              = dto.Theme;
        if (dto.PrimaryColor       != null) settings.PrimaryColor       = dto.PrimaryColor;
        if (dto.DefaultProductView != null) settings.DefaultProductView = dto.DefaultProductView;
        if (dto.ShowNumpad         != null) settings.ShowNumpad         = dto.ShowNumpad.Value;
        if (dto.ShowFavouritesBar  != null) settings.ShowFavouritesBar  = dto.ShowFavouritesBar.Value;

        settings.UpdatedAt = DateTime.UtcNow;
        _repo.Update(settings);
        await _repo.SaveChangesAsync();

        return MapToDto(settings);
    }

    public async Task<PosSettingsDto> ResetAsync()
    {
        var settings = await _repo.GetCurrentAsync();
        if (settings is null)
        {
            settings = new PosSettings();
            await _repo.AddAsync(settings);
        }
        else
        {
            // Replace all fields with a fresh default instance's values
            var defaults = new PosSettings();
            settings.RequireCashierPin      = defaults.RequireCashierPin;
            settings.AutoLockMinutes        = defaults.AutoLockMinutes;
            settings.AllowPriceOverride     = defaults.AllowPriceOverride;
            settings.AllowDiscount          = defaults.AllowDiscount;
            settings.MaxDiscountPercent     = defaults.MaxDiscountPercent;
            settings.RequireCustomer        = defaults.RequireCustomer;
            settings.DefaultCustomerId      = defaults.DefaultCustomerId;
            settings.AllowNegativeStock     = defaults.AllowNegativeStock;
            settings.TaxInclusivePricing    = defaults.TaxInclusivePricing;
            settings.AutoApplyTax           = defaults.AutoApplyTax;
            settings.DefaultCurrencyCode    = defaults.DefaultCurrencyCode;
            settings.RoundingValue          = defaults.RoundingValue;
            settings.RoundingMode           = defaults.RoundingMode;
            settings.AcceptCash             = defaults.AcceptCash;
            settings.AcceptCard             = defaults.AcceptCard;
            settings.AcceptMobilePayment    = defaults.AcceptMobilePayment;
            settings.AcceptCreditOnAccount  = defaults.AcceptCreditOnAccount;
            settings.AllowSplitPayment      = defaults.AllowSplitPayment;
            settings.AllowPartialPayment    = defaults.AllowPartialPayment;
            settings.DefaultPaymentMethod   = defaults.DefaultPaymentMethod;
            settings.AutoOpenCashDrawer     = defaults.AutoOpenCashDrawer;
            settings.MinOrderAmount         = defaults.MinOrderAmount;
            settings.MaxOrderAmount         = defaults.MaxOrderAmount;
            settings.ShowProductImages      = defaults.ShowProductImages;
            settings.ShowProductDescription = defaults.ShowProductDescription;
            settings.AllowItemNotes         = defaults.AllowItemNotes;
            settings.AllowDecimalQuantity   = defaults.AllowDecimalQuantity;
            settings.BarcodeScanSound       = defaults.BarcodeScanSound;
            settings.ShowStockLevel         = defaults.ShowStockLevel;
            settings.LowStockThreshold      = defaults.LowStockThreshold;
            settings.ItemsPerPage           = defaults.ItemsPerPage;
            settings.ShowCategoryFilter     = defaults.ShowCategoryFilter;
            settings.TouchMode              = defaults.TouchMode;
            settings.OperatingMode          = defaults.OperatingMode;
            settings.RequireOpeningFloat    = defaults.RequireOpeningFloat;
            settings.RequireCashCountOnClose= defaults.RequireCashCountOnClose;
            settings.AutoCloseSession       = defaults.AutoCloseSession;
            settings.AutoCloseTime          = defaults.AutoCloseTime;
            settings.EnableLoyaltyPoints    = defaults.EnableLoyaltyPoints;
            settings.EnablePromotions       = defaults.EnablePromotions;
            settings.EnableCoupons          = defaults.EnableCoupons;
            settings.AutoApplyPromotions    = defaults.AutoApplyPromotions;
            settings.DefaultPaperSize       = defaults.DefaultPaperSize;
            settings.ReceiptCopies          = defaults.ReceiptCopies;
            settings.AutoPrintReceipt       = defaults.AutoPrintReceipt;
            settings.AskToPrintReceipt      = defaults.AskToPrintReceipt;
            settings.SkipReceiptScreen      = defaults.SkipReceiptScreen;
            settings.ReceiptShowLogo        = defaults.ReceiptShowLogo;
            settings.ReceiptShowBusinessName= defaults.ReceiptShowBusinessName;
            settings.ReceiptShowAddress     = defaults.ReceiptShowAddress;
            settings.ReceiptShowContact     = defaults.ReceiptShowContact;
            settings.ReceiptShowCashierName = defaults.ReceiptShowCashierName;
            settings.ReceiptShowCustomerName= defaults.ReceiptShowCustomerName;
            settings.ReceiptShowOrderNumber = defaults.ReceiptShowOrderNumber;
            settings.ReceiptShowDateTime    = defaults.ReceiptShowDateTime;
            settings.ReceiptShowItemCodes   = defaults.ReceiptShowItemCodes;
            settings.ReceiptShowUnitPrice   = defaults.ReceiptShowUnitPrice;
            settings.ReceiptShowLineDiscount= defaults.ReceiptShowLineDiscount;
            settings.ReceiptShowTaxBreakdown= defaults.ReceiptShowTaxBreakdown;
            settings.ReceiptShowDiscountLine= defaults.ReceiptShowDiscountLine;
            settings.ReceiptShowBarcode     = defaults.ReceiptShowBarcode;
            settings.ReceiptShowQrCode      = defaults.ReceiptShowQrCode;
            settings.ReceiptBarcodeSymbology= defaults.ReceiptBarcodeSymbology;
            settings.ReceiptAutoCutPaper    = defaults.ReceiptAutoCutPaper;
            settings.ReceiptHeaderNote      = defaults.ReceiptHeaderNote;
            settings.ReceiptFooterMessage   = defaults.ReceiptFooterMessage;
            settings.ReceiptReturnPolicy    = defaults.ReceiptReturnPolicy;
            settings.Theme                  = defaults.Theme;
            settings.PrimaryColor           = defaults.PrimaryColor;
            settings.DefaultProductView     = defaults.DefaultProductView;
            settings.ShowNumpad             = defaults.ShowNumpad;
            settings.ShowFavouritesBar      = defaults.ShowFavouritesBar;
            settings.UpdatedAt             = DateTime.UtcNow;
            _repo.Update(settings);
        }

        await _repo.SaveChangesAsync();
        _logger.LogInformation("POS settings reset to factory defaults");
        return MapToDto(settings);
    }

    private static PosSettingsDto MapToDto(PosSettings s) => new()
    {
        Id                      = s.Id,
        RequireCashierPin       = s.RequireCashierPin,
        AutoLockMinutes         = s.AutoLockMinutes,
        AllowPriceOverride      = s.AllowPriceOverride,
        AllowDiscount           = s.AllowDiscount,
        MaxDiscountPercent      = s.MaxDiscountPercent,
        RequireCustomer         = s.RequireCustomer,
        DefaultCustomerId       = s.DefaultCustomerId,
        AllowNegativeStock      = s.AllowNegativeStock,
        TaxInclusivePricing     = s.TaxInclusivePricing,
        AutoApplyTax            = s.AutoApplyTax,
        DefaultCurrencyCode     = s.DefaultCurrencyCode,
        RoundingValue           = s.RoundingValue,
        RoundingMode            = s.RoundingMode,
        AcceptCash              = s.AcceptCash,
        AcceptCard              = s.AcceptCard,
        AcceptMobilePayment     = s.AcceptMobilePayment,
        AcceptCreditOnAccount   = s.AcceptCreditOnAccount,
        AllowSplitPayment       = s.AllowSplitPayment,
        AllowPartialPayment     = s.AllowPartialPayment,
        DefaultPaymentMethod    = s.DefaultPaymentMethod,
        AutoOpenCashDrawer      = s.AutoOpenCashDrawer,
        MinOrderAmount          = s.MinOrderAmount,
        MaxOrderAmount          = s.MaxOrderAmount,
        CashGlAccountNumber     = s.CashGlAccountNumber,
        BankGlAccountNumber     = s.BankGlAccountNumber,
        ShowProductImages       = s.ShowProductImages,
        ShowProductDescription  = s.ShowProductDescription,
        AllowItemNotes          = s.AllowItemNotes,
        AllowDecimalQuantity    = s.AllowDecimalQuantity,
        BarcodeScanSound        = s.BarcodeScanSound,
        ShowStockLevel          = s.ShowStockLevel,
        LowStockThreshold       = s.LowStockThreshold,
        ItemsPerPage            = s.ItemsPerPage,
        ShowCategoryFilter      = s.ShowCategoryFilter,
        TouchMode               = s.TouchMode,
        OperatingMode           = s.OperatingMode,
        RequireOpeningFloat     = s.RequireOpeningFloat,
        RequireCashCountOnClose = s.RequireCashCountOnClose,
        AutoCloseSession        = s.AutoCloseSession,
        AutoCloseTime           = s.AutoCloseTime,
        EnableLoyaltyPoints     = s.EnableLoyaltyPoints,
        EnablePromotions        = s.EnablePromotions,
        EnableCoupons           = s.EnableCoupons,
        AutoApplyPromotions     = s.AutoApplyPromotions,
        DefaultPaperSize        = s.DefaultPaperSize,
        ReceiptCopies           = s.ReceiptCopies,
        AutoPrintReceipt        = s.AutoPrintReceipt,
        AskToPrintReceipt       = s.AskToPrintReceipt,
        SkipReceiptScreen       = s.SkipReceiptScreen,
        ReceiptShowLogo         = s.ReceiptShowLogo,
        ReceiptShowBusinessName = s.ReceiptShowBusinessName,
        ReceiptShowAddress      = s.ReceiptShowAddress,
        ReceiptShowContact      = s.ReceiptShowContact,
        ReceiptShowCashierName  = s.ReceiptShowCashierName,
        ReceiptShowCustomerName = s.ReceiptShowCustomerName,
        ReceiptShowOrderNumber  = s.ReceiptShowOrderNumber,
        ReceiptShowDateTime     = s.ReceiptShowDateTime,
        ReceiptShowItemCodes    = s.ReceiptShowItemCodes,
        ReceiptShowUnitPrice    = s.ReceiptShowUnitPrice,
        ReceiptShowLineDiscount = s.ReceiptShowLineDiscount,
        ReceiptShowTaxBreakdown = s.ReceiptShowTaxBreakdown,
        ReceiptShowDiscountLine = s.ReceiptShowDiscountLine,
        ReceiptShowBarcode      = s.ReceiptShowBarcode,
        ReceiptShowQrCode       = s.ReceiptShowQrCode,
        ReceiptBarcodeSymbology = s.ReceiptBarcodeSymbology,
        ReceiptAutoCutPaper     = s.ReceiptAutoCutPaper,
        ReceiptHeaderNote       = s.ReceiptHeaderNote,
        ReceiptFooterMessage    = s.ReceiptFooterMessage,
        ReceiptReturnPolicy     = s.ReceiptReturnPolicy,
        Theme                   = s.Theme,
        PrimaryColor            = s.PrimaryColor,
        DefaultProductView      = s.DefaultProductView,
        ShowNumpad              = s.ShowNumpad,
        ShowFavouritesBar       = s.ShowFavouritesBar,
    };
}
