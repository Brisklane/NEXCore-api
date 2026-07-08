using System.ComponentModel.DataAnnotations;

namespace Accounting.Application.DTOs;

/// <summary>
/// DTO for creating ledger account
/// </summary>
public class CreateLedgerAccountDto
{
    [Required]
    public required Guid LedgerId { get; set; }

    [Required]
    [StringLength(20)]
    public required string AccountNumber { get; set; }

    [Required]
    [StringLength(255)]
    public required string AccountName { get; set; }

    [Required]
    public required Guid CategoryId { get; set; }

    public Guid? ParentAccountId { get; set; }

    public bool IsPostingAllowed { get; set; } = true;

    public bool IsControlAccount { get; set; } = false;

    [StringLength(3)]
    public string? CurrencyCode { get; set; }

    public bool AllowManualEntry { get; set; } = true;

    public bool IsSubledgerAccount { get; set; } = false;

    public Guid? SubledgerMasterAccountId { get; set; }

    public string? Description { get; set; }
}

/// <summary>
/// DTO for updating ledger account
/// </summary>
public class UpdateLedgerAccountDto
{
    [StringLength(255)]
    public string? AccountName { get; set; }

    public Guid? CategoryId { get; set; }

    public Guid? ParentAccountId { get; set; }

    [StringLength(3)]
    public string? CurrencyCode { get; set; }

    public bool? IsPostingAllowed { get; set; }

    public bool? IsControlAccount { get; set; }

    public bool? AllowManualEntry { get; set; }

    public bool? IsActive { get; set; }

    public string? Description { get; set; }
}

/// <summary>
/// DTO for creating tax code
/// </summary>
public class CreateTaxCodeDto
{
    [Required]
    [StringLength(50)]
    public required string Code { get; set; }

    [Required]
    [StringLength(255)]
    public required string Name { get; set; }

    public decimal TaxRate { get; set; }

    public Guid? LedgerAccountId { get; set; }

    public string? Description { get; set; }
}

/// <summary>
/// DTO for updating tax code
/// </summary>
public class UpdateTaxCodeDto
{
    [StringLength(255)]
    public string? Name { get; set; }

    public decimal? TaxRate { get; set; }

    public Guid? LedgerAccountId { get; set; }

    public string? Description { get; set; }
}

/// <summary>
/// DTO for creating posting profile
/// </summary>
public class CreatePostingProfileDto
{
    [Required]
    [StringLength(50)]
    public required string ModuleName { get; set; }

    [Required]
    [StringLength(50)]
    public required string TransactionType { get; set; }

    [Required]
    public required Guid DebitAccountId { get; set; }

    [Required]
    public required Guid CreditAccountId { get; set; }

    public Guid? TaxAccountId { get; set; }

    public string? Description { get; set; }
}

/// <summary>
/// DTO for updating posting profile
/// </summary>
public class UpdatePostingProfileDto
{
    [StringLength(50)]
    public string? ModuleName { get; set; }

    [StringLength(50)]
    public string? TransactionType { get; set; }

    public Guid? DebitAccountId { get; set; }

    public Guid? CreditAccountId { get; set; }

    public Guid? TaxAccountId { get; set; }

    public string? Description { get; set; }
}

/// <summary>
/// DTO for creating account balance
/// </summary>
public class CreateAccountBalanceDto
{
    [Required]
    public required Guid LedgerAccountId { get; set; }

    [Required]
    public required Guid FiscalPeriodId { get; set; }

    [Required]
    public required decimal Amount { get; set; }

    [StringLength(50)]
    public string? BalanceType { get; set; }

    public string? Description { get; set; }
}

/// <summary>
/// DTO for updating account balance
/// </summary>
public class UpdateAccountBalanceDto
{
    public decimal? Amount { get; set; }

    [StringLength(50)]
    public string? BalanceType { get; set; }

    public string? Description { get; set; }
}
