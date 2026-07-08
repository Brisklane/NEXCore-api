using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Nexcore.SharedKernel.Api;
using Sales.Application.Constants;
using Sales.Application.DTOs;
using Sales.Domain.Enums;
using System.Text.RegularExpressions;

namespace Sales.Api.Controllers;

[ApiController]
[Route("api/sales/[controller]")]
[Authorize]
public class SalesLookupController : ControllerBase
{
    // ── Sales Order ───────────────────────────────────────────────────────────

    [HttpGet("order-statuses")]
    [ProducesResponseType(typeof(ApiResponse<List<LookupItemDto>>), StatusCodes.Status200OK)]
    public IActionResult GetOrderStatuses() => OkEnum<SalesOrderStatus>();

    [HttpGet("sales-channels")]
    [ProducesResponseType(typeof(ApiResponse<List<LookupItemDto>>), StatusCodes.Status200OK)]
    public IActionResult GetSalesChannels() => OkEnum<SalesChannel>();

    [HttpGet("payment-terms")]
    [ProducesResponseType(typeof(ApiResponse<List<LookupItemDto>>), StatusCodes.Status200OK)]
    public IActionResult GetPaymentTerms() => OkEnum<PaymentTerms>();

    [HttpGet("fulfillment-types")]
    [ProducesResponseType(typeof(ApiResponse<List<LookupItemDto>>), StatusCodes.Status200OK)]
    public IActionResult GetFulfillmentTypes() => OkEnum<FulfillmentType>();

    [HttpGet("discount-types")]
    [ProducesResponseType(typeof(ApiResponse<List<LookupItemDto>>), StatusCodes.Status200OK)]
    public IActionResult GetDiscountTypes() => OkEnum<DiscountType>();

    [HttpGet("incoterms")]
    [ProducesResponseType(typeof(ApiResponse<List<LookupItemDto>>), StatusCodes.Status200OK)]
    public IActionResult GetIncoterms() => OkEnum<Incoterm>();

    [HttpGet("tax-categories")]
    [ProducesResponseType(typeof(ApiResponse<List<LookupItemDto>>), StatusCodes.Status200OK)]
    public IActionResult GetTaxCategories() => OkEnum<TaxCategory>();

    // ── Quotation / Agreement ─────────────────────────────────────────────────

    [HttpGet("quotation-statuses")]
    [ProducesResponseType(typeof(ApiResponse<List<LookupItemDto>>), StatusCodes.Status200OK)]
    public IActionResult GetQuotationStatuses() => OkEnum<QuotationStatus>();

    [HttpGet("sales-agreement-statuses")]
    [ProducesResponseType(typeof(ApiResponse<List<LookupItemDto>>), StatusCodes.Status200OK)]
    public IActionResult GetSalesAgreementStatuses() => OkEnum<SalesAgreementStatus>();

    // ── Invoice / Payment ─────────────────────────────────────────────────────

    [HttpGet("invoice-statuses")]
    [ProducesResponseType(typeof(ApiResponse<List<LookupItemDto>>), StatusCodes.Status200OK)]
    public IActionResult GetInvoiceStatuses() => OkEnum<InvoiceStatus>();

    [HttpGet("price-list-types")]
    [ProducesResponseType(typeof(ApiResponse<List<LookupItemDto>>), StatusCodes.Status200OK)]
    public IActionResult GetPriceListTypes() => OkEnum<PriceListType>();

    // ── Delivery / Returns ────────────────────────────────────────────────────

    [HttpGet("delivery-statuses")]
    [ProducesResponseType(typeof(ApiResponse<List<LookupItemDto>>), StatusCodes.Status200OK)]
    public IActionResult GetDeliveryStatuses() => OkEnum<DeliveryStatus>();

    [HttpGet("return-statuses")]
    [ProducesResponseType(typeof(ApiResponse<List<LookupItemDto>>), StatusCodes.Status200OK)]
    public IActionResult GetReturnStatuses() => OkEnum<ReturnStatus>();

    // ── Rider ─────────────────────────────────────────────────────────────────

    [HttpGet("rider-statuses")]
    [ProducesResponseType(typeof(ApiResponse<List<LookupItemDto>>), StatusCodes.Status200OK)]
    public IActionResult GetRiderStatuses() => OkEnum<RiderStatus>();

    [HttpGet("rider-assignment-statuses")]
    [ProducesResponseType(typeof(ApiResponse<List<LookupItemDto>>), StatusCodes.Status200OK)]
    public IActionResult GetRiderAssignmentStatuses() => OkEnum<RiderAssignmentStatus>();

    [HttpGet("vehicle-types")]
    [ProducesResponseType(typeof(ApiResponse<List<LookupItemDto>>), StatusCodes.Status200OK)]
    public IActionResult GetVehicleTypes() => OkEnum<VehicleType>();

    // ── Vendor Onboarding ─────────────────────────────────────────────────────

    [HttpGet("vendor-onboarding-statuses")]
    [ProducesResponseType(typeof(ApiResponse<List<LookupItemDto>>), StatusCodes.Status200OK)]
    public IActionResult GetVendorOnboardingStatuses() => OkEnum<VendorOnboardingStatus>();

    // ── Store Offers ──────────────────────────────────────────────────────────

    [HttpGet("store-offer-types")]
    [ProducesResponseType(typeof(ApiResponse<List<LookupItemDto>>), StatusCodes.Status200OK)]
    public IActionResult GetStoreOfferTypes() => OkEnum<StoreOfferType>();

    // ── POS ───────────────────────────────────────────────────────────────────

    [HttpGet("pos-store-types")]
    [ProducesResponseType(typeof(ApiResponse<List<LookupItemDto>>), StatusCodes.Status200OK)]
    public IActionResult GetPosStoreTypes() => OkEnum<PosStoreType>();

    [HttpGet("pos-store-formats")]
    [ProducesResponseType(typeof(ApiResponse<List<LookupItemDto>>), StatusCodes.Status200OK)]
    public IActionResult GetPosStoreFormats() => OkEnum<PosStoreFormat>();

    [HttpGet("pos-session-statuses")]
    [ProducesResponseType(typeof(ApiResponse<List<LookupItemDto>>), StatusCodes.Status200OK)]
    public IActionResult GetPosSessionStatuses() => OkEnum<PosSessionStatus>();

    [HttpGet("pos-transaction-types")]
    [ProducesResponseType(typeof(ApiResponse<List<LookupItemDto>>), StatusCodes.Status200OK)]
    public IActionResult GetPosTransactionTypes() => OkEnum<PosTransactionType>();

    [HttpGet("pos-transaction-statuses")]
    [ProducesResponseType(typeof(ApiResponse<List<LookupItemDto>>), StatusCodes.Status200OK)]
    public IActionResult GetPosTransactionStatuses() => OkEnum<PosTransactionStatus>();

    [HttpGet("pos-tender-types")]
    [ProducesResponseType(typeof(ApiResponse<List<LookupItemDto>>), StatusCodes.Status200OK)]
    public IActionResult GetPosTenderTypes() => OkEnum<PosTenderType>();

    [HttpGet("pos-cash-movement-types")]
    [ProducesResponseType(typeof(ApiResponse<List<LookupItemDto>>), StatusCodes.Status200OK)]
    public IActionResult GetPosCashMovementTypes() => OkEnum<PosCashMovementType>();

    [HttpGet("store-online-statuses")]
    [ProducesResponseType(typeof(ApiResponse<List<LookupItemDto>>), StatusCodes.Status200OK)]
    public IActionResult GetStoreOnlineStatuses() => OkEnum<StoreOnlineStatus>();

    // ── Coupon / Gift Card / Loyalty ──────────────────────────────────────────

    [HttpGet("coupon-statuses")]
    [ProducesResponseType(typeof(ApiResponse<List<LookupItemDto>>), StatusCodes.Status200OK)]
    public IActionResult GetCouponStatuses() => OkEnum<CouponStatus>();

    [HttpGet("gift-card-statuses")]
    [ProducesResponseType(typeof(ApiResponse<List<LookupItemDto>>), StatusCodes.Status200OK)]
    public IActionResult GetGiftCardStatuses() => OkEnum<GiftCardStatus>();

    [HttpGet("loyalty-tiers")]
    [ProducesResponseType(typeof(ApiResponse<List<LookupItemDto>>), StatusCodes.Status200OK)]
    public IActionResult GetLoyaltyTiers() => OkEnum<LoyaltyTier>();

    [HttpGet("loyalty-transaction-types")]
    [ProducesResponseType(typeof(ApiResponse<List<LookupItemDto>>), StatusCodes.Status200OK)]
    public IActionResult GetLoyaltyTransactionTypes() => OkEnum<LoyaltyTransactionType>();

    // ── Commission ────────────────────────────────────────────────────────────

    [HttpGet("commission-bases")]
    [ProducesResponseType(typeof(ApiResponse<List<LookupItemDto>>), StatusCodes.Status200OK)]
    public IActionResult GetCommissionBases() => OkEnum<CommissionBasis>();

    [HttpGet("commission-statuses")]
    [ProducesResponseType(typeof(ApiResponse<List<LookupItemDto>>), StatusCodes.Status200OK)]
    public IActionResult GetCommissionStatuses() => OkEnum<CommissionStatus>();

    [HttpGet("target-periods")]
    [ProducesResponseType(typeof(ApiResponse<List<LookupItemDto>>), StatusCodes.Status200OK)]
    public IActionResult GetTargetPeriods() => OkEnum<TargetPeriod>();

    // ── Approval ──────────────────────────────────────────────────────────────

    [HttpGet("approval-condition-fields")]
    [ProducesResponseType(typeof(ApiResponse<List<LookupItemDto>>), StatusCodes.Status200OK)]
    public IActionResult GetApprovalConditionFields() => OkEnum<ApprovalConditionField>();

    [HttpGet("approval-condition-operators")]
    [ProducesResponseType(typeof(ApiResponse<List<LookupItemDto>>), StatusCodes.Status200OK)]
    public IActionResult GetApprovalConditionOperators() => OkEnum<ApprovalConditionOperator>();

    [HttpGet("approval-decisions")]
    [ProducesResponseType(typeof(ApiResponse<List<LookupItemDto>>), StatusCodes.Status200OK)]
    public IActionResult GetApprovalDecisions() => OkEnum<ApprovalDecision>();

    // ── Notification ──────────────────────────────────────────────────────────

    [HttpGet("notification-channels")]
    [ProducesResponseType(typeof(ApiResponse<List<LookupItemDto>>), StatusCodes.Status200OK)]
    public IActionResult GetNotificationChannels() => OkEnum<NotificationChannel>();

    [HttpGet("notification-types")]
    [ProducesResponseType(typeof(ApiResponse<List<LookupItemDto>>), StatusCodes.Status200OK)]
    public IActionResult GetNotificationTypes() => OkEnum<NotificationType>();

    // ── Promotions ────────────────────────────────────────────────────────────

    [HttpGet("promotion-statuses")]
    [ProducesResponseType(typeof(ApiResponse<List<LookupItemDto>>), StatusCodes.Status200OK)]
    public IActionResult GetPromotionStatuses() => OkEnum<PromotionStatus>();

    [HttpGet("promotion-discount-types")]
    [ProducesResponseType(typeof(ApiResponse<List<LookupItemDto>>), StatusCodes.Status200OK)]
    public IActionResult GetPromotionDiscountTypes() => OkEnum<PromotionDiscountType>();

    [HttpGet("promotion-condition-types")]
    [ProducesResponseType(typeof(ApiResponse<List<LookupItemDto>>), StatusCodes.Status200OK)]
    public IActionResult GetPromotionConditionTypes() => OkEnum<PromotionConditionType>();

    [HttpGet("promotion-target-types")]
    [ProducesResponseType(typeof(ApiResponse<List<LookupItemDto>>), StatusCodes.Status200OK)]
    public IActionResult GetPromotionTargetTypes() => OkEnum<PromotionTargetType>();

    [HttpGet("promotion-price-targets")]
    [ProducesResponseType(typeof(ApiResponse<List<LookupItemDto>>), StatusCodes.Status200OK)]
    public IActionResult GetPromotionPriceTargets() => OkEnum<PromotionPriceTarget>();

    [HttpGet("scheduled-days")]
    [ProducesResponseType(typeof(ApiResponse<List<LookupItemDto>>), StatusCodes.Status200OK)]
    public IActionResult GetScheduledDays() => OkEnum<ScheduledDays>();

    // ── Barcode / Label Designer ──────────────────────────────────────────────

    /// <summary>Supported barcode symbologies for the "Barcode type" dropdown.</summary>
    [HttpGet("barcode-symbologies")]
    [ProducesResponseType(typeof(ApiResponse<List<LookupItemDto>>), StatusCodes.Status200OK)]
    public IActionResult GetBarcodeSymbologies() =>
        Ok(new ApiResponse<List<LookupItemDto>> { Success = true, Data = Symbologies, Message = "Lookup values retrieved successfully" });

    /// <summary>Label / paper-stock presets (name + dimensions) for the "Paper type" dropdown.</summary>
    [HttpGet("label-paper-presets")]
    [ProducesResponseType(typeof(ApiResponse<List<LabelPaperPresetDto>>), StatusCodes.Status200OK)]
    public IActionResult GetLabelPaperPresets() =>
        Ok(new ApiResponse<List<LabelPaperPresetDto>> { Success = true, Data = PaperPresets, Message = "Lookup values retrieved successfully" });

    /// <summary>Slider min/max/default/step for the designer's numeric controls (so the UI hardcodes nothing).</summary>
    [HttpGet("label-slider-ranges")]
    [ProducesResponseType(typeof(ApiResponse<List<LabelFieldRangeDto>>), StatusCodes.Status200OK)]
    public IActionResult GetLabelSliderRanges() =>
        Ok(new ApiResponse<List<LabelFieldRangeDto>> { Success = true, Data = SliderRanges, Message = "Lookup values retrieved successfully" });

    /// <summary>All option sets the price-tag / barcode-label designer needs (symbologies + paper presets + slider ranges).</summary>
    [HttpGet("label-designer-options")]
    [ProducesResponseType(typeof(ApiResponse<LabelDesignerOptionsDto>), StatusCodes.Status200OK)]
    public IActionResult GetLabelDesignerOptions() =>
        Ok(new ApiResponse<LabelDesignerOptionsDto>
        {
            Success = true,
            Data    = new LabelDesignerOptionsDto
            {
                BarcodeSymbologies = Symbologies,
                PaperPresets       = PaperPresets,
                SliderRanges       = SliderRanges,
            },
            Message = "Lookup values retrieved successfully",
        });

    private static readonly List<LabelFieldRangeDto> SliderRanges =
    [
        new() { Field = "ProductNameFontPt", Min = LabelDesignerDefaults.ProductNameFontMin, Max = LabelDesignerDefaults.ProductNameFontMax, Default = LabelDesignerDefaults.ProductNameFontDefault, Step = 1 },
        new() { Field = "PriceFontPt",       Min = LabelDesignerDefaults.PriceFontMin,       Max = LabelDesignerDefaults.PriceFontMax,       Default = LabelDesignerDefaults.PriceFontDefault,       Step = 1 },
        new() { Field = "BarcodeHeightPt",   Min = LabelDesignerDefaults.BarcodeHeightMin,   Max = LabelDesignerDefaults.BarcodeHeightMax,   Default = LabelDesignerDefaults.BarcodeHeightDefault,   Step = 1 },
        new() { Field = "Copies",            Min = LabelDesignerDefaults.CopiesMin,          Max = LabelDesignerDefaults.CopiesMax,          Default = LabelDesignerDefaults.CopiesDefault,          Step = 1 },
    ];

    /// <summary>Barcode symbologies supported by the rendering engine (BarcodeService).</summary>
    private static readonly List<LookupItemDto> Symbologies =
    [
        new() { Value = "Code128", Label = "Code 128" },
        new() { Value = "EAN13",   Label = "EAN-13" },
        new() { Value = "EAN8",    Label = "EAN-8" },
        new() { Value = "UPCA",    Label = "UPC-A" },
        new() { Value = "Code39",  Label = "Code 39" },
        new() { Value = "QR",      Label = "QR Code" },
    ];

    private static readonly List<LabelPaperPresetDto> PaperPresets =
    [
        new() { Name = "Zebra LP2844 (30x27.5mm)", WidthMm = 30,  HeightMm = 27.5, RollPaper = false },
        new() { Name = "Roll Paper (58mm)",        WidthMm = 58,  HeightMm = 40,   RollPaper = true  },
        new() { Name = "Roll Paper (80mm)",        WidthMm = 80,  HeightMm = 50,   RollPaper = true  },
        new() { Name = "Roll Paper (Custom)",      WidthMm = 50,  HeightMm = 30,   RollPaper = true  },
    ];

    // ── helpers ───────────────────────────────────────────────────────────────

    private IActionResult OkEnum<TEnum>() where TEnum : struct, Enum =>
        Ok(new ApiResponse<List<LookupItemDto>>
        {
            Success = true,
            Data    = EnumToLookup<TEnum>(),
            Message = "Lookup values retrieved successfully"
        });

    private static List<LookupItemDto> EnumToLookup<TEnum>() where TEnum : struct, Enum =>
        Enum.GetValues<TEnum>()
            .Select(e => new LookupItemDto
            {
                Value = Convert.ToInt32(e).ToString(),
                Label = FormatEnumLabel(e.ToString())
            })
            .ToList();

    private static string FormatEnumLabel(string name) =>
        Regex.Replace(name, "([a-z])([A-Z])", "$1 $2");
}
