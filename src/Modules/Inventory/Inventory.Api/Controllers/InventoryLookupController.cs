using Inventory.Application.DTOs;
using Inv = Inventory.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Nexcore.SharedKernel.Api;

namespace Inventory.Api.Controllers;

/// <summary>
/// Returns all inventory constant/lookup lists so the frontend can populate
/// dropdowns without embedding magic strings in the UI.
/// </summary>
[ApiController]
[Route("api/inventory-lookup")]
[Authorize]
public class InventoryLookupController : ControllerBase
{
    /// <summary>
    /// Get all inventory lookup lists in a single request.
    /// Ideal for bootstrapping a form that uses multiple dropdowns.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<InventoryLookupsDto>), StatusCodes.Status200OK)]
    public IActionResult GetAll()
    {
        var dto = new InventoryLookupsDto
        {
            ItemTypes          = ToLookup(Inv.ItemType.All,                 FormatItemType),
            ItemConditions     = ToLookup(Inv.ItemCondition.All,            FormatLabel),
            CostingMethods     = ToLookup(Inv.CostingMethod.All,            FormatCostingMethod),
            WarehouseTypes     = ToLookup(Inv.WarehouseType.All,            FormatLabel),
            BarcodeTypes       = ToLookup(Inv.BarcodeType.All,              v => v),
            PriceLists         = ToLookup(Inv.PriceList.All,                FormatLabel),
            CommentTypes       = ToLookup(Inv.CommentType.All,              FormatLabel),
            SalesChannels      = ToLookup(Inv.SalesChannel.All,             FormatChannel),
            ListingStatuses    = ToLookup(Inv.ListingStatus.All,            FormatLabel),
            DiscountTypes      = ToLookup(Inv.DiscountType.All,             FormatDiscountType),
            WarrantyTypes      = ToLookup(Inv.WarrantyType.All,             FormatLabel),
            AttributeDataTypes = ToLookup(Inv.AttributeDataType.All,        FormatLabel),
            TaxTypes           = ToLookup(Inv.TaxType.All,                  FormatLabel),
            SizeCharts         = ToLookup(Inv.SizeChart.All,                FormatSizeChart),
            ImageResolutions   = ToLookup(Inv.ImageResolution.All,          FormatLabel),
            DocumentTypes      = ToLookup(Inv.InventoryDocumentType.All,    FormatDocumentType),
            DocumentStatuses   = ToLookup(Inv.InventoryDocumentStatus.All,  FormatLabel),
            TransactionTypes   = ToLookup(Inv.InventoryTransactionType.All, FormatLabel),
        };

        return Ok(new ApiResponse<InventoryLookupsDto>
        {
            Success = true,
            Data    = dto,
            Message = "Inventory lookups retrieved successfully"
        });
    }

    // Individual endpoints (useful when only one list is needed)

    [HttpGet("item-types")]
    [ProducesResponseType(typeof(ApiResponse<List<LookupItemDto>>), StatusCodes.Status200OK)]
    public IActionResult GetItemTypes() =>
        OkList(Inv.ItemType.All, FormatItemType);

    [HttpGet("item-conditions")]
    [ProducesResponseType(typeof(ApiResponse<List<LookupItemDto>>), StatusCodes.Status200OK)]
    public IActionResult GetItemConditions() =>
        OkList(Inv.ItemCondition.All, FormatLabel);

    [HttpGet("costing-methods")]
    [ProducesResponseType(typeof(ApiResponse<List<LookupItemDto>>), StatusCodes.Status200OK)]
    public IActionResult GetCostingMethods() =>
        OkList(Inv.CostingMethod.All, FormatCostingMethod);

    [HttpGet("warehouse-types")]
    [ProducesResponseType(typeof(ApiResponse<List<LookupItemDto>>), StatusCodes.Status200OK)]
    public IActionResult GetWarehouseTypes() =>
        OkList(Inv.WarehouseType.All, FormatLabel);

    [HttpGet("barcode-types")]
    [ProducesResponseType(typeof(ApiResponse<List<LookupItemDto>>), StatusCodes.Status200OK)]
    public IActionResult GetBarcodeTypes() =>
        OkList(Inv.BarcodeType.All, v => v);

    [HttpGet("price-lists")]
    [ProducesResponseType(typeof(ApiResponse<List<LookupItemDto>>), StatusCodes.Status200OK)]
    public IActionResult GetPriceLists() =>
        OkList(Inv.PriceList.All, FormatLabel);

    [HttpGet("comment-types")]
    [ProducesResponseType(typeof(ApiResponse<List<LookupItemDto>>), StatusCodes.Status200OK)]
    public IActionResult GetCommentTypes() =>
        OkList(Inv.CommentType.All, FormatLabel);

    [HttpGet("sales-channels")]
    [ProducesResponseType(typeof(ApiResponse<List<LookupItemDto>>), StatusCodes.Status200OK)]
    public IActionResult GetSalesChannels() =>
        OkList(Inv.SalesChannel.All, FormatChannel);

    [HttpGet("listing-statuses")]
    [ProducesResponseType(typeof(ApiResponse<List<LookupItemDto>>), StatusCodes.Status200OK)]
    public IActionResult GetListingStatuses() =>
        OkList(Inv.ListingStatus.All, FormatLabel);

    [HttpGet("discount-types")]
    [ProducesResponseType(typeof(ApiResponse<List<LookupItemDto>>), StatusCodes.Status200OK)]
    public IActionResult GetDiscountTypes() =>
        OkList(Inv.DiscountType.All, FormatDiscountType);

    [HttpGet("warranty-types")]
    [ProducesResponseType(typeof(ApiResponse<List<LookupItemDto>>), StatusCodes.Status200OK)]
    public IActionResult GetWarrantyTypes() =>
        OkList(Inv.WarrantyType.All, FormatLabel);

    [HttpGet("attribute-data-types")]
    [ProducesResponseType(typeof(ApiResponse<List<LookupItemDto>>), StatusCodes.Status200OK)]
    public IActionResult GetAttributeDataTypes() =>
        OkList(Inv.AttributeDataType.All, FormatLabel);

    [HttpGet("tax-types")]
    [ProducesResponseType(typeof(ApiResponse<List<LookupItemDto>>), StatusCodes.Status200OK)]
    public IActionResult GetTaxTypes() =>
        OkList(Inv.TaxType.All, FormatLabel);

    [HttpGet("size-charts")]
    [ProducesResponseType(typeof(ApiResponse<List<LookupItemDto>>), StatusCodes.Status200OK)]
    public IActionResult GetSizeCharts() =>
        OkList(Inv.SizeChart.All, FormatSizeChart);

    [HttpGet("image-resolutions")]
    [ProducesResponseType(typeof(ApiResponse<List<LookupItemDto>>), StatusCodes.Status200OK)]
    public IActionResult GetImageResolutions() =>
        OkList(Inv.ImageResolution.All, FormatLabel);

    [HttpGet("document-types")]
    [ProducesResponseType(typeof(ApiResponse<List<LookupItemDto>>), StatusCodes.Status200OK)]
    public IActionResult GetDocumentTypes() =>
        OkList(Inv.InventoryDocumentType.All, FormatDocumentType);

    [HttpGet("document-statuses")]
    [ProducesResponseType(typeof(ApiResponse<List<LookupItemDto>>), StatusCodes.Status200OK)]
    public IActionResult GetDocumentStatuses() =>
        OkList(Inv.InventoryDocumentStatus.All, FormatLabel);

    [HttpGet("transaction-types")]
    [ProducesResponseType(typeof(ApiResponse<List<LookupItemDto>>), StatusCodes.Status200OK)]
    public IActionResult GetTransactionTypes() =>
        OkList(Inv.InventoryTransactionType.All, FormatLabel);

    [HttpGet("tracking-types")]
    [ProducesResponseType(typeof(ApiResponse<List<LookupItemDto>>), StatusCodes.Status200OK)]
    public IActionResult GetTrackingTypes() =>
        OkList(Inv.ItemTrackingType.All, FormatTrackingType);

    [HttpGet("serial-statuses")]
    [ProducesResponseType(typeof(ApiResponse<List<LookupItemDto>>), StatusCodes.Status200OK)]
    public IActionResult GetSerialStatuses() =>
        OkList(Inv.SerialStatus.All, FormatLabel);

    [HttpGet("batch-statuses")]
    [ProducesResponseType(typeof(ApiResponse<List<LookupItemDto>>), StatusCodes.Status200OK)]
    public IActionResult GetBatchStatuses() =>
        OkList(Inv.BatchStatus.All, FormatLabel);

    // Helpers

    private IActionResult OkList(string[] values, Func<string, string> labelFn) =>
        Ok(new ApiResponse<List<LookupItemDto>>
        {
            Success = true,
            Data    = ToLookup(values, labelFn),
            Message = "Lookup values retrieved successfully"
        });

    private static List<LookupItemDto> ToLookup(string[] values, Func<string, string> labelFn) =>
        values.Select(v => new LookupItemDto { Value = v, Label = labelFn(v) }).ToList();

    private static string FormatLabel(string v) =>
        System.Text.RegularExpressions.Regex.Replace(v, "([a-z])([A-Z])", "$1 $2");

    private static string FormatItemType(string v) => v switch
    {
        Inv.ItemType.Inventory    => "Inventory Item",
        Inv.ItemType.Service      => "Service",
        Inv.ItemType.Bundle       => "Bundle",
        Inv.ItemType.Digital      => "Digital Product",
        Inv.ItemType.RawMaterial  => "Raw Material",
        Inv.ItemType.FinishedGood => "Finished Good",
        _                         => v
    };

    private static string FormatTrackingType(string v) => v switch
    {
        Inv.ItemTrackingType.None   => "None (quantity only)",
        Inv.ItemTrackingType.Lot    => "Lot / Batch tracked",
        Inv.ItemTrackingType.Serial => "Serial / IMEI tracked",
        _                           => v
    };

    private static string FormatCostingMethod(string v) => v switch
    {
        Inv.CostingMethod.MovingAverage => "Moving Average",
        Inv.CostingMethod.FIFO          => "FIFO (First In, First Out)",
        Inv.CostingMethod.LIFO          => "LIFO (Last In, First Out)",
        Inv.CostingMethod.Standard      => "Standard Cost",
        _                               => v
    };

    private static string FormatChannel(string v) => v switch
    {
        Inv.SalesChannel.POS        => "POS (Point of Sale)",
        Inv.SalesChannel.Daraz      => "Daraz",
        Inv.SalesChannel.AliExpress => "AliExpress",
        Inv.SalesChannel.CashCarry  => "Cash & Carry",
        Inv.SalesChannel.B2B        => "B2B Portal",
        Inv.SalesChannel.Website    => "Website",
        Inv.SalesChannel.MobileApp  => "Mobile App",
        _                           => v
    };

    private static string FormatDiscountType(string v) => v switch
    {
        Inv.DiscountType.Percentage  => "Percentage (%)",
        Inv.DiscountType.FixedAmount => "Fixed Amount",
        Inv.DiscountType.BuyXGetY    => "Buy X Get Y Free",
        Inv.DiscountType.VolumePrice => "Volume / Tier Pricing",
        _                            => v
    };

    private static string FormatSizeChart(string v) => v switch
    {
        Inv.SizeChart.Apparel        => "Apparel (XS-6XL)",
        Inv.SizeChart.ApparelNumeric => "Apparel - Numeric Waist",
        Inv.SizeChart.FootwearUK     => "Footwear - UK",
        Inv.SizeChart.FootwearUS     => "Footwear - US",
        Inv.SizeChart.FootwearEU     => "Footwear - EU",
        Inv.SizeChart.Ring           => "Ring Size",
        Inv.SizeChart.ChainLength    => "Chain / Necklace Length",
        Inv.SizeChart.Screen         => "Screen Size (inches)",
        Inv.SizeChart.Volume         => "Volume (ml / L)",
        Inv.SizeChart.Weight         => "Weight (g / kg)",
        _                            => v
    };

    private static string FormatDocumentType(string v) => v switch
    {
        Inv.InventoryDocumentType.GRN        => "Goods Receipt Note (GRN)",
        Inv.InventoryDocumentType.Delivery   => "Goods Issue / Delivery",
        Inv.InventoryDocumentType.Transfer   => "Inter-Warehouse Transfer",
        Inv.InventoryDocumentType.Adjustment => "Stock Adjustment",
        Inv.InventoryDocumentType.ReturnIn   => "Purchase Return (Return In)",
        Inv.InventoryDocumentType.ReturnOut  => "Sales Return (Return Out)",
        Inv.InventoryDocumentType.Opening    => "Opening Balance",
        Inv.InventoryDocumentType.Scrap      => "Scrap / Write-Off",
        _                                    => v
    };
}
