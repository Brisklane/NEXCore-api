using System;
using System.Collections.Generic;

namespace Accounting.Infrastructure.Reports.DTOs;

public class DailyTransactionLineDto
{
    public string JournalNumber { get; set; } = string.Empty;
    public string AccountNumber { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public decimal DebitAmount { get; set; }
    public decimal CreditAmount { get; set; }
    public string Description { get; set; } = string.Empty;
}
