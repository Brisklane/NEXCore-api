namespace Sales.Application.DTOs;

// ── Offline POS sync ──────────────────────────────────────────────────────────
// A device that completed sales while offline replays them here when it reconnects.
// The server re-prices each order (server-authoritative pricing), settles it through the
// SAME checkout pipeline as an online sale, and assigns the real order/receipt numbers.
// Idempotent on OfflineOrderNumber so retries never double-post.

/// <summary>One sale completed offline, ready to replay against the server.</summary>
public class OfflineSaleDto
{
    /// <summary>Client-generated unique id — the idempotency key (stored as SalesOrder.OfflineOrderNumber).</summary>
    public string OfflineOrderNumber { get; set; } = string.Empty;

    /// <summary>The order as it would have been created online. The server re-prices it on ingest.</summary>
    public CreateSalesOrderDto Order { get; set; } = new();

    /// <summary>Tender(s) collected offline. Cash only for now (cards need a live terminal).</summary>
    public List<PosTenderDto> Tenders { get; set; } = [];

    public bool AllowCredit { get; set; }

    /// <summary>
    /// Total the device computed and printed on the offline receipt. Compared against the
    /// server's re-priced total on sync to surface a pricing variance for reconciliation.
    /// </summary>
    public decimal DeviceTotal { get; set; }

    /// <summary>When the sale was completed on the device (audit only).</summary>
    public DateTime CompletedAt { get; set; }

    /// <summary>
    /// Present when the POS session itself was opened offline (no connection at open). The server
    /// reuses an already-open session for this cashier+terminal or opens one, then books the sale
    /// against it — so a till that opened with just a PIN, fully offline, still reconciles.
    /// </summary>
    public OfflineSessionDto? OfflineSession { get; set; }
}

/// <summary>Open-context for a session that was opened on the device while offline.</summary>
public class OfflineSessionDto
{
    /// <summary>Device-generated session reference (audit / correlation).</summary>
    public string? OfflineSessionNumber { get; set; }
    public Guid? TerminalId { get; set; }
    public Guid? CashierId { get; set; }
    public Guid? StoreId { get; set; }
    public decimal OpeningFloat { get; set; }
    public DateTime? OpenedAt { get; set; }
}

public class OfflineSyncRequestDto
{
    /// <summary>The device that captured these sales (audit / future routing).</summary>
    public string? DeviceId { get; set; }
    public List<OfflineSaleDto> Orders { get; set; } = [];
}

public enum OfflineSyncStatus
{
    /// <summary>Created + settled now.</summary>
    Synced,
    /// <summary>Already ingested on a previous attempt — returned with its real numbers.</summary>
    Duplicate,
    /// <summary>Could not be settled — left in the device outbox to retry / reconcile.</summary>
    Failed,
}

public class OfflineSyncResultItem
{
    public string OfflineOrderNumber { get; set; } = string.Empty;
    public OfflineSyncStatus Status { get; set; }
    public string? OrderNumber { get; set; }
    public string? ReceiptNumber { get; set; }
    public Guid? TransactionId { get; set; }
    public string? Error { get; set; }

    // ── Pricing reconciliation ──
    /// <summary>Total the device printed offline (echoed back).</summary>
    public decimal? DeviceTotal { get; set; }
    /// <summary>Total the server computed after re-pricing.</summary>
    public decimal? ServerTotal { get; set; }
    /// <summary>ServerTotal − DeviceTotal. Non-zero means the offline receipt differs from the booked sale.</summary>
    public decimal? Variance { get; set; }
}

public class OfflineSyncResultDto
{
    public List<OfflineSyncResultItem> Results { get; set; } = [];
    public int SyncedCount { get; set; }
    public int DuplicateCount { get; set; }
    public int FailedCount { get; set; }
}
