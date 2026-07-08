namespace Nexcore.SharedKernel.Enums;

/// <summary>Standard payment terms shared across Sales, AR and AP. "Net N" = due N days after invoice.</summary>
public enum PaymentTerms
{
    Cash = 1,                        // due immediately / on delivery
    Net15 = 2,
    Net30 = 3,                       // most common
    Net45 = 4,
    Net60 = 5,
    Net90 = 6,
    TwoPercentTenDaysNet30 = 7,      // 2/10 net 30: 2% off if paid within 10 days, else net 30
    OnePercentFifteenDaysNet60 = 8,  // 1/15 net 60: 1% off if paid within 15 days, else net 60
    EndOfMonth = 9,
    Custom = 10                      // terms described in a free-text field
}
