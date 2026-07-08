namespace Accounting.Application.DTOs;

/// <summary>
/// DTO for account balance
/// </summary>
public class AccountBalanceDto
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid LedgerAccountId { get; set; }
    public Guid FiscalPeriodId { get; set; }
    public decimal OpeningBalance { get; set; }
    public decimal DebitTotal { get; set; }
    public decimal CreditTotal { get; set; }
    public decimal ClosingBalance { get; set; }
    public string BalanceType { get; set; } = "Actual";
}
