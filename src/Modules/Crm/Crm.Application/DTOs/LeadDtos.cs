namespace Crm.Application.DTOs;

public class LeadDto
{
    public Guid Id { get; set; }
    public string? Salutation { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Company { get; set; } = string.Empty;
    public string? Title { get; set; }
    public string? Website { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Street { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? PostalCode { get; set; }
    public string? Country { get; set; }
    public int? NumberOfEmployees { get; set; }
    public decimal? AnnualRevenue { get; set; }
    public string? LeadSource { get; set; }
    public string? Industry { get; set; }
    public string Status { get; set; } = "New";
    public Guid? OwnerId { get; set; }
    public Guid? AssignedEmployeeId { get; set; }
    public string? Description { get; set; }
    public bool EmailOptOut { get; set; }
    public bool IsConverted { get; set; }
    public DateTime? ConvertedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateLeadDto
{
    public string? Salutation { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Company { get; set; } = string.Empty;
    public string? Title { get; set; }
    public string? Website { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Street { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? PostalCode { get; set; }
    public string? Country { get; set; }
    public int? NumberOfEmployees { get; set; }
    public decimal? AnnualRevenue { get; set; }
    public string? LeadSource { get; set; }
    public string? Industry { get; set; }
    public string Status { get; set; } = "New";
    public Guid? OwnerId { get; set; }
    public Guid? AssignedEmployeeId { get; set; }
    public string? Description { get; set; }
    public bool EmailOptOut { get; set; }
}

public class UpdateLeadDto : CreateLeadDto { }

public class ConvertLeadDto
{
    public bool CreateAccount { get; set; } = true;
    public bool CreateContact { get; set; } = true;
    public bool CreateDeal { get; set; }
    public string? DealName { get; set; }
    public decimal? DealAmount { get; set; }
    public DateTime? DealCloseDate { get; set; }
}
