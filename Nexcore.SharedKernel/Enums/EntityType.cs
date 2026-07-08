namespace Nexcore.SharedKernel.Enums;

/// <summary>Type of source document a journal entry traces back to, for cross-module drill-down.</summary>
public enum EntityType
{
    SalesInvoice = 1,          // Sales
    PurchaseOrder = 2,         // Purchasing
    GoodsReceipt = 3,          // Inventory
    StockTransfer = 4,         // Inventory
    StockAdjustment = 5,       // Inventory
    PayrollEntry = 6,          // HR
    EmployeeExpense = 7,       // HR
    AssetAcquisition = 8,      // Fixed Assets
    DepreciationSchedule = 9,  // Fixed Assets
    PaymentReceipt = 10,       // customer payment
    VendorPayment = 11,
    ManualJournalEntry = 12,   // Accounting
    BankReconciliation = 13,
    General = 14               // catch-all
}
