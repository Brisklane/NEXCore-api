namespace Inventory.Application.DTOs;

/// <summary>
/// A generic key/label pair used in frontend dropdown lists.
/// </summary>
public class LookupItemDto
{
    /// <summary>The stored value (passed to the API on save)</summary>
    public string Value { get; set; } = null!;

    /// <summary>Human-readable label shown in the dropdown</summary>
    public string Label { get; set; } = null!;
}

/// <summary>
/// All inventory lookup lists bundled in a single response.
/// Call GET /api/inventory-lookup to pre-load all dropdowns in one request.
/// </summary>
public class InventoryLookupsDto
{
    public List<LookupItemDto> ItemTypes          { get; set; } = [];
    public List<LookupItemDto> ItemConditions     { get; set; } = [];
    public List<LookupItemDto> CostingMethods     { get; set; } = [];
    public List<LookupItemDto> WarehouseTypes     { get; set; } = [];
    public List<LookupItemDto> BarcodeTypes       { get; set; } = [];
    public List<LookupItemDto> PriceLists         { get; set; } = [];
    public List<LookupItemDto> CommentTypes       { get; set; } = [];
    public List<LookupItemDto> SalesChannels      { get; set; } = [];
    public List<LookupItemDto> ListingStatuses    { get; set; } = [];
    public List<LookupItemDto> DiscountTypes      { get; set; } = [];
    public List<LookupItemDto> WarrantyTypes      { get; set; } = [];
    public List<LookupItemDto> AttributeDataTypes { get; set; } = [];
    public List<LookupItemDto> TaxTypes           { get; set; } = [];
    public List<LookupItemDto> SizeCharts         { get; set; } = [];
    public List<LookupItemDto> ImageResolutions   { get; set; } = [];
    public List<LookupItemDto> DocumentTypes      { get; set; } = [];
    public List<LookupItemDto> DocumentStatuses   { get; set; } = [];
    public List<LookupItemDto> TransactionTypes   { get; set; } = [];
}
