using Crm.Domain.Enums;

namespace Crm.Application.DTOs;

public class ContractDto
{
    public Guid Id { get; set; }
    public string ContractNumber { get; set; } = string.Empty;
    public Guid AccountId { get; set; }
    public string? AccountName { get; set; }
    public Guid? OwnerId { get; set; }
    public ContractStatus Status { get; set; }
    public DateTime? StartDate { get; set; }
    public int? ContractTermMonths { get; set; }
    public DateTime? EndDate { get; set; }
    public decimal? ContractValue { get; set; }
    public string? CurrencyCode { get; set; }
    public Guid? BillingContactId { get; set; }
    public string? SpecialTerms { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateContractDto
{
    public Guid AccountId { get; set; }
    public Guid? OwnerId { get; set; }
    public DateTime? StartDate { get; set; }
    public int? ContractTermMonths { get; set; }
    public DateTime? EndDate { get; set; }
    public decimal? ContractValue { get; set; }
    public string? CurrencyCode { get; set; } = "USD";
    public Guid? BillingContactId { get; set; }
    public string? SpecialTerms { get; set; }
    public string? Description { get; set; }
}

public class UpdateContractDto : CreateContractDto { }
