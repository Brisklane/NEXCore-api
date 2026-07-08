namespace Sales.Domain.Enums;

/// <summary>
/// Type of cash movement in a POS session.
/// </summary>
public enum PosCashMovementType
{
    CashIn = 0,       // additional cash added to drawer
    CashOut = 1,      // cash removed for expenses
    SafeDrop = 2,     // cash moved to safe mid-session to reduce drawer float
    PettyCash = 3,    // small expense paid out of drawer
    OpeningFloat = 4, // initial float placed at session open
    ClosingFloat = 5  // float counted at session close
}
