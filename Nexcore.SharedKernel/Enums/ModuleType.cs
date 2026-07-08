namespace Nexcore.SharedKernel.Enums;

/// <summary>Identifies the module that originated a transaction, for cross-module tracing and GL posting.</summary>
public enum ModuleType
{
    Sales = 1,
    Inventory = 2,
    HR = 3,
    Purchasing = 4,
    AccountsReceivable = 5,
    AccountsPayable = 6,
    FixedAssets = 7,
    Accounting = 8,   // GL / manual journal entries
    General = 9       // miscellaneous
}
