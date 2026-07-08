namespace Accounting.Application.DTOs;

/// <summary>
/// DTO for tax code
/// </summary>
public class TaxCodeDto
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public required string Code { get; set; }
    public required string Name { get; set; }
    public decimal Percentage { get; set; }
    public bool IsRecoverable { get; set; }
    public bool IsActive { get; set; }
    public string? Description { get; set; }
}
