namespace Crm.Domain.Enums;

public enum CustomerType
{
    /// <summary>B2C walk-in, no account required (POS counter).</summary>
    WalkIn = 0,

    /// <summary>Mobile/web app consumer — phone or email login.</summary>
    AppConsumer = 1,

    /// <summary>Business-to-business customer — credit terms, PO numbers.</summary>
    B2B = 2,

    /// <summary>Wholesale buyer — price lists, sales agreements.</summary>
    Wholesale = 3,

    /// <summary>Internal department or warehouse (inter-company transfers).</summary>
    Internal = 4,
}
