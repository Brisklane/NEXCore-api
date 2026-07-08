using System.ComponentModel.DataAnnotations;

namespace Accounting.Application.DTOs;

/// <summary>
/// Request DTO for trial balance report
/// </summary>
public class TrialBalanceRequestDto
{
    [Required]
    public Guid LedgerId { get; set; }

    [Required]
    public Guid FiscalPeriodId { get; set; }
}
