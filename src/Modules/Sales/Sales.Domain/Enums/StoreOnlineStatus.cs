namespace Sales.Domain.Enums;

/// <summary>
/// Real-time online ordering availability of a PosStore.
/// Shown to the customer on the app store listing.
/// </summary>
public enum StoreOnlineStatus
{
    /// <summary>Store is open and accepting orders normally.</summary>
    Open = 0,

    /// <summary>
    /// Store is open but operating at high capacity.
    /// Orders are accepted but prep time is extended.
    /// </summary>
    Busy = 1,

    /// <summary>
    /// Store is temporarily closed — outside schedule or manually set by staff.
    /// </summary>
    Closed = 2,

    /// <summary>
    /// Store is closed for the day but will reopen.
    /// App shows next opening time.
    /// </summary>
    ClosedForToday = 3
}
