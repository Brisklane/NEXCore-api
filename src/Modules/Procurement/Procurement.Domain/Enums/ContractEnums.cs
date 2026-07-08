namespace Procurement.Domain.Enums;

public enum PurchaseContractStatus
{
    Draft,
    UnderReview,
    Active,
    Suspended,
    Expired,
    Terminated
}

/// <summary>
/// Blanket Order = commit to buy a quantity over time at agreed price.
/// Framework Agreement = pre-agreed terms, no quantity commitment.
/// </summary>
public enum PurchaseContractType
{
    FrameworkAgreement,
    BlanketOrder,
    ServiceAgreement,
    SupplyAgreement,
    MaintenanceContract
}
