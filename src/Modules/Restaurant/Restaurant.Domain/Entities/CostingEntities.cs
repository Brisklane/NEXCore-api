using Nexcore.SharedKernel;
using Restaurant.Domain.Enums;

namespace Restaurant.Domain.Entities;

/// <summary>
/// What a dish is made of. This is the bridge between selling a burger and depleting a bun:
/// closing a check explodes each line through its recipe and issues ingredient-level stock
/// movements to Inventory, so one stock ledger serves the whole company.
/// </summary>
public class Recipe : BaseEntity
{
    /// <summary>Null for a sub-recipe (a sauce), which is consumed by other recipes, not sold.</summary>
    public Guid? MenuItemId { get; set; }
    public MenuItem? MenuItem { get; set; }

    /// <summary>Set when a size has its own recipe — a large pizza is not 1.5 small ones.</summary>
    public Guid? VariantId { get; set; }

    public string Name { get; set; } = string.Empty;

    /// <summary>How many portions one batch of this recipe produces.</summary>
    public decimal YieldQuantity { get; set; } = 1;
    public string YieldUom { get; set; } = "portion";

    /// <summary>Prepared in bulk and drawn down by other recipes rather than sold directly.</summary>
    public bool IsSubRecipe { get; set; }

    /// <summary>Inventory item this sub-recipe produces, so its own stock can be tracked.</summary>
    public Guid? OutputInventoryItemId { get; set; }

    /// <summary>Rolled-up ingredient cost per portion. Recomputed when ingredients change.</summary>
    public decimal TotalCost { get; set; }

    public int? PrepTimeMinutes { get; set; }
    public int? CookTimeMinutes { get; set; }
    public string? Instructions { get; set; }
    public string? PlatingNotes { get; set; }
    public int Version { get; set; } = 1;

    public ICollection<RecipeIngredient> Ingredients { get; set; } = [];
}

/// <summary>One ingredient line — an inventory item, or another recipe used as a component.</summary>
public class RecipeIngredient : BaseEntity
{
    public Guid RecipeId { get; set; }
    public Recipe? Recipe { get; set; }

    /// <summary>Inventory item consumed. Null when this line is a sub-recipe instead.</summary>
    public Guid? InventoryItemId { get; set; }
    public Guid? SubRecipeId { get; set; }

    public string IngredientName { get; set; } = string.Empty;

    /// <summary>Gross quantity before yield and waste are applied.</summary>
    public decimal Quantity { get; set; }
    public string Uom { get; set; } = "g";

    /// <summary>Usable share after trimming and peeling. 100 means nothing is lost.</summary>
    public decimal YieldPercent { get; set; } = 100m;

    /// <summary>Expected loss in preparation, on top of yield.</summary>
    public decimal WastePercent { get; set; }

    public decimal UnitCost { get; set; }
    public decimal LineCost { get; set; }

    /// <summary>Garnish that can be left out without changing the dish.</summary>
    public bool IsOptional { get; set; }

    public int DisplayOrder { get; set; }
    public string? Note { get; set; }
}

/// <summary>
/// Food thrown away, costed. Recorded from three places: the chef logging spoilage, a void of
/// an already-fired line, and a guest sending a dish back.
/// </summary>
public class WastageLog : BaseEntity
{
    public Guid OutletId { get; set; }

    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    public WastageReason Reason { get; set; } = WastageReason.Spoilage;

    /// <summary>Set when a whole dish was wasted; otherwise the waste is at ingredient level.</summary>
    public Guid? MenuItemId { get; set; }
    public Guid? InventoryItemId { get; set; }

    public string ItemName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string Uom { get; set; } = "portion";

    public decimal UnitCost { get; set; }
    public decimal TotalCost { get; set; }

    public Guid? StaffId { get; set; }
    public string? StaffName { get; set; }

    /// <summary>Set when this row came from voiding a line the kitchen had already cooked.</summary>
    public Guid? OrderLineId { get; set; }
    public Guid? StationId { get; set; }

    /// <summary>True once the write-off has been pushed to Inventory as a stock issue.</summary>
    public bool IsStockAdjusted { get; set; }

    public string? Note { get; set; }
}

/// <summary>
/// One settings row per company. Everything here changes how service behaves, so it is data
/// rather than configuration files — a manager changes it from the Settings screen.
/// </summary>
public class RestaurantSettings : BaseEntity
{
    // ── Service ──────────────────────────────────────────────────────────────
    public bool RequireWaiterPin { get; set; } = true;
    public bool RequireGuestCountOnSeat { get; set; } = true;
    public bool RequireSeatNumbers { get; set; }
    public bool AutoFireOnSend { get; set; } = true;

    /// <summary>Minutes a seated table may go without an order before the floor plan flags it.</summary>
    public int SeatedAttentionMinutes { get; set; } = 10;

    /// <summary>Minutes a served table may sit before it is flagged for a bill drop.</summary>
    public int ServedAttentionMinutes { get; set; } = 20;

    public bool AllowTableMerge { get; set; } = true;
    public bool AllowTableTransfer { get; set; } = true;
    public bool AllowSplitBill { get; set; } = true;

    // ── Money ────────────────────────────────────────────────────────────────
    public bool PricesIncludeTax { get; set; }
    public bool TipsEnabled { get; set; } = true;
    public string? TipPresetPercents { get; set; } = "10,15,20";
    public bool TipPoolingEnabled { get; set; }
    public TipDistributionBasis TipDistributionBasis { get; set; } = TipDistributionBasis.ByHoursWorked;
    public decimal KitchenTipSharePercent { get; set; }

    /// <summary>Nearest unit cash totals are rounded to. Zero disables rounding.</summary>
    public decimal CashRoundingIncrement { get; set; }

    public decimal PackagingChargePerOrder { get; set; }
    public bool VoidRequiresReason { get; set; } = true;
    public bool DiscountRequiresReason { get; set; } = true;
    public decimal DiscountApprovalThreshold { get; set; }

    // ── Kitchen ──────────────────────────────────────────────────────────────
    public bool KitchenDisplayEnabled { get; set; } = true;
    public bool PrintKitchenTickets { get; set; }
    public bool ExpoScreenEnabled { get; set; } = true;

    /// <summary>Ticket age at which a KDS card turns amber; the station SLA turns it red.</summary>
    public int KdsWarningMinutes { get; set; } = 8;
    public bool AutoBumpOnServe { get; set; }
    public bool ShowAllergenWarnings { get; set; } = true;

    // ── Stock ────────────────────────────────────────────────────────────────
    public bool DepleteStockOnCheckClose { get; set; } = true;

    /// <summary>Take an item off sale automatically when an ingredient reaches zero.</summary>
    public bool Auto86OnZeroStock { get; set; }
    public bool TrackWastage { get; set; } = true;

    // ── Front of house ───────────────────────────────────────────────────────
    public bool ReservationsEnabled { get; set; } = true;
    public bool WaitlistEnabled { get; set; } = true;
    public int ReservationHoldMinutes { get; set; } = 15;
    public int DefaultReservationDuration { get; set; } = 90;
    public bool RequireDepositForLargeParty { get; set; }
    public int LargePartyThreshold { get; set; } = 8;

    // ── Receipts ─────────────────────────────────────────────────────────────
    public bool PrintReceiptAutomatically { get; set; } = true;
    public bool EmailReceiptEnabled { get; set; }
    public string? ReceiptHeader { get; set; }
    public string? ReceiptFooter { get; set; }
    public bool ShowCalories { get; set; }
    public bool FeedbackPromptEnabled { get; set; }
}
