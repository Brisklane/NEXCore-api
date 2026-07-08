namespace Inventory.Domain.Constants;

/// <summary>
/// Item types - used in Item.ItemType
/// </summary>
public static class ItemType
{
    public const string Inventory    = "Inventory";
    public const string Service      = "Service";
    public const string Bundle       = "Bundle";
    public const string Digital      = "Digital";
    public const string RawMaterial  = "RawMaterial";
    public const string FinishedGood = "FinishedGood";

    public static readonly string[] All =
        [Inventory, Service, Bundle, Digital, RawMaterial, FinishedGood];
}

/// <summary>
/// Item condition - used in Item.Condition
/// </summary>
public static class ItemCondition
{
    public const string New         = "New";
    public const string Refurbished = "Refurbished";
    public const string Used        = "Used";
    public const string OpenBox     = "OpenBox";

    public static readonly string[] All = [New, Refurbished, Used, OpenBox];
}

/// <summary>
/// Costing methods - used in Item.CostingMethod and InventoryBalance.CostingMethod
/// </summary>
public static class CostingMethod
{
    public const string MovingAverage = "MovingAverage";
    public const string FIFO          = "FIFO";
    public const string LIFO          = "LIFO";
    public const string Standard      = "Standard";

    public static readonly string[] All = [MovingAverage, FIFO, LIFO, Standard];
}

/// <summary>
/// Warehouse types - used in Warehouse.WarehouseType
/// </summary>
public static class WarehouseType
{
    public const string Main        = "Main";
    public const string Raw         = "Raw";
    public const string Finished    = "Finished";
    public const string Retail      = "Retail";
    public const string Transit     = "Transit";
    public const string Quarantine  = "Quarantine";
    public const string Consignment = "Consignment";
    public const string Virtual     = "Virtual";

    public static readonly string[] All =
        [Main, Raw, Finished, Retail, Transit, Quarantine, Consignment, Virtual];
}

/// <summary>
/// Barcode types - used in ItemBarcode.BarcodeType
/// </summary>
public static class BarcodeType
{
    public const string EAN13      = "EAN13";
    public const string EAN8       = "EAN8";
    public const string UPCA       = "UPCA";
    public const string UPCE       = "UPCE";
    public const string Code128    = "Code128";
    public const string Code39     = "Code39";
    public const string QR         = "QR";
    public const string DataMatrix = "DataMatrix";

    public static readonly string[] All =
        [EAN13, EAN8, UPCA, UPCE, Code128, Code39, QR, DataMatrix];
}

/// <summary>
/// Price list names - used in ItemPrice.PriceList
/// </summary>
public static class PriceList
{
    public const string Default   = "Default";
    public const string Retail    = "Retail";
    public const string Wholesale = "Wholesale";
    public const string VIP       = "VIP";
    public const string B2B       = "B2B";
    public const string Staff     = "Staff";

    public static readonly string[] All =
        [Default, Retail, Wholesale, VIP, B2B, Staff];
}

/// <summary>
/// Item comment types - used in ItemComment.CommentType
/// </summary>
public static class CommentType
{
    public const string Internal = "Internal";
    public const string Supplier = "Supplier";
    public const string Customer = "Customer";
    public const string Quality  = "Quality";
    public const string Handling = "Handling";

    public static readonly string[] All =
        [Internal, Supplier, Customer, Quality, Handling];
}

/// <summary>
/// Sales channels - used in ItemChannelListing.Channel
/// </summary>
public static class SalesChannel
{
    public const string POS        = "POS";
    public const string Daraz      = "DARAZ";
    public const string AliExpress = "ALIEXPRESS";
    public const string CashCarry  = "CASH_CARRY";
    public const string B2B        = "B2B";
    public const string Website    = "WEBSITE";
    public const string MobileApp  = "MOBILE_APP";

    public static readonly string[] All =
        [POS, Daraz, AliExpress, CashCarry, B2B, Website, MobileApp];
}

/// <summary>
/// Channel listing statuses - used in ItemChannelListing.ListingStatus
/// </summary>
public static class ListingStatus
{
    public const string Draft        = "Draft";
    public const string Active       = "Active";
    public const string Paused       = "Paused";
    public const string Rejected     = "Rejected";
    public const string OutOfStock   = "OutOfStock";
    public const string Discontinued = "Discontinued";

    public static readonly string[] All =
        [Draft, Active, Paused, Rejected, OutOfStock, Discontinued];
}

/// <summary>
/// Discount types - used in ItemDiscount.DiscountType and ItemChannelListing.DiscountType
/// </summary>
public static class DiscountType
{
    public const string Percentage  = "Percentage";
    public const string FixedAmount = "FixedAmount";
    public const string BuyXGetY    = "BuyXGetY";
    public const string VolumePrice = "VolumePrice";

    public static readonly string[] All =
        [Percentage, FixedAmount, BuyXGetY, VolumePrice];
}

/// <summary>
/// Warranty types - used in ItemWarranty.WarrantyType
/// </summary>
public static class WarrantyType
{
    public const string Seller        = "Seller";
    public const string Brand         = "Brand";
    public const string NoWarranty    = "NoWarranty";
    public const string International = "International";
    public const string Local         = "Local";

    public static readonly string[] All =
        [Seller, Brand, NoWarranty, International, Local];
}

/// <summary>
/// Attribute data types - used in AttributeDefinition.DataType
/// </summary>
public static class AttributeDataType
{
    public const string Text    = "Text";
    public const string Decimal = "Decimal";
    public const string Integer = "Integer";
    public const string Boolean = "Boolean";
    public const string List    = "List";

    public static readonly string[] All = [Text, Decimal, Integer, Boolean, List];
}

/// <summary>
/// Tax types - used in TaxDefinition.TaxType
/// </summary>
public static class TaxType
{
    public const string Percentage = "Percentage";
    public const string Fixed      = "Fixed";

    public static readonly string[] All = [Percentage, Fixed];
}

/// <summary>
/// Size chart names - used in Size.SizeChart
/// </summary>
public static class SizeChart
{
    public const string Apparel        = "Apparel";
    public const string ApparelNumeric = "ApparelNumeric";
    public const string FootwearUK     = "Footwear-UK";
    public const string FootwearUS     = "Footwear-US";
    public const string FootwearEU     = "Footwear-EU";
    public const string Ring           = "Ring";
    public const string ChainLength    = "ChainLength";
    public const string Screen         = "Screen";
    public const string Volume         = "Volume";
    public const string Weight         = "Weight";

    public static readonly string[] All =
        [Apparel, ApparelNumeric, FootwearUK, FootwearUS, FootwearEU,
         Ring, ChainLength, Screen, Volume, Weight];
}

/// <summary>
/// Image resolution labels - used in ItemImage.Resolution
/// </summary>
public static class ImageResolution
{
    public const string Thumbnail = "Thumbnail";
    public const string Small     = "Small";
    public const string Medium    = "Medium";
    public const string Large     = "Large";
    public const string Original  = "Original";

    public static readonly string[] All =
        [Thumbnail, Small, Medium, Large, Original];
}

/// <summary>
/// Inventory document types - used in InventoryDocument.DocumentType
/// </summary>
public static class InventoryDocumentType
{
    public const string GRN          = "GRN";           // Goods Receipt Note
    public const string Delivery     = "Delivery";      // Goods Issue / Delivery
    public const string Transfer     = "Transfer";       // Inter-warehouse transfer
    public const string Adjustment   = "Adjustment";    // Stock adjustment
    public const string ReturnIn     = "ReturnIn";      // Purchase return
    public const string ReturnOut    = "ReturnOut";     // Sales return
    public const string Opening      = "Opening";       // Opening balance
    public const string Scrap        = "Scrap";         // Scrap / write-off

    public static readonly string[] All =
        [GRN, Delivery, Transfer, Adjustment, ReturnIn, ReturnOut, Opening, Scrap];
}

/// <summary>
/// Inventory document statuses - used in InventoryDocument.Status
/// </summary>
public static class InventoryDocumentStatus
{
    public const string Draft     = "Draft";
    public const string Posted    = "Posted";
    public const string Cancelled = "Cancelled";

    public static readonly string[] All = [Draft, Posted, Cancelled];
}

/// <summary>
/// Inventory transaction types - used in InventoryTransaction.TransactionType
/// </summary>
public static class InventoryTransactionType
{
    public const string Receipt    = "Receipt";
    public const string Issue      = "Issue";
    public const string Transfer   = "Transfer";
    public const string Adjustment = "Adjustment";
    public const string Return     = "Return";
    public const string Scrap      = "Scrap";
    public const string Opening    = "Opening";

    public static readonly string[] All =
        [Receipt, Issue, Transfer, Adjustment, Return, Scrap, Opening];
}

/// <summary>
/// How an item's stock identity is tracked - used in Item.TrackingType.
/// Single source of truth; the legacy Item.IsSerialTracked / IsBatchTracked booleans are kept in sync
/// (Serial ? IsSerialTracked, Lot ? IsBatchTracked) so existing queries/UI keep working.
///   None   : quantity-only bulk stock (a number that goes up/down).
///   Lot    : batch/lot code shared by many units, with manufacture/expiry (pharma, food, chemicals).
///   Serial : every physical unit has a unique serial/IMEI and its own lifecycle (phones, equipment).
/// </summary>
public static class ItemTrackingType
{
    public const string None   = "None";
    public const string Lot    = "Lot";
    public const string Serial = "Serial";

    public static readonly string[] All = [None, Lot, Serial];
}

/// <summary>
/// Lifecycle status of a single serialized unit - used in ItemSerial.Status.
/// </summary>
public static class SerialStatus
{
    public const string InStock     = "InStock";      // received and available in a warehouse
    public const string Reserved    = "Reserved";     // allocated to an open order, not yet shipped
    public const string Sold        = "Sold";         // issued/delivered to a customer
    public const string Returned    = "Returned";     // came back from a customer (awaiting inspection)
    public const string InTransit   = "InTransit";    // moving between warehouses
    public const string Defective   = "Defective";    // failed QC / faulty
    public const string UnderRepair = "UnderRepair";  // out for RMA / service
    public const string Scrapped    = "Scrapped";     // written off / destroyed
    public const string Lost        = "Lost";         // missing / stolen

    public static readonly string[] All =
        [InStock, Reserved, Sold, Returned, InTransit, Defective, UnderRepair, Scrapped, Lost];

    /// <summary>Statuses that count as physically on-hand and sellable.</summary>
    public static readonly string[] Available = [InStock];
}

/// <summary>
/// Status of a batch/lot - used in ItemBatch.Status.
/// </summary>
public static class BatchStatus
{
    public const string Active     = "Active";      // usable stock
    public const string Quarantine = "Quarantine";  // held pending QC release
    public const string Expired    = "Expired";     // past expiry date
    public const string Consumed   = "Consumed";    // fully depleted
    public const string Recalled   = "Recalled";    // supplier/regulatory recall

    public static readonly string[] All = [Active, Quarantine, Expired, Consumed, Recalled];
}

/// <summary>
/// Event types recorded in ItemSerialHistory - the per-unit audit trail.
/// </summary>
public static class SerialEventType
{
    public const string Received      = "Received";
    public const string Reserved      = "Reserved";
    public const string Sold          = "Sold";
    public const string Returned      = "Returned";
    public const string Transferred   = "Transferred";
    public const string StatusChanged = "StatusChanged";
    public const string Scrapped      = "Scrapped";

    public static readonly string[] All =
        [Received, Reserved, Sold, Returned, Transferred, StatusChanged, Scrapped];
}
