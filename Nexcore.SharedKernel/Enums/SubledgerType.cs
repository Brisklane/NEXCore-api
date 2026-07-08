namespace Nexcore.SharedKernel.Enums;

/// <summary>Classifies the detail (subledger) dimension behind a GL master account.</summary>
public enum SubledgerType
{
    AccountsReceivable = 1,  // customer detail
    AccountsPayable = 2,     // vendor detail
    Inventory = 3,           // product / warehouse detail
    FixedAssets = 4,
    CostCenter = 5,          // department / cost-centre detail
    Project = 6,
    Employee = 7,            // payroll detail
    Bank = 8,
    Customer = 9,            // customer detail outside AR
    Vendor = 10              // vendor detail outside AP
}
