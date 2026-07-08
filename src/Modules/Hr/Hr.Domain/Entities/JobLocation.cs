using Nexcore.SharedKernel;

namespace Hr.Domain.Entities;

public class JobLocation : BaseEntity
{
    public string LocationCode { get; set; } = string.Empty;
    public string LocationName { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? StateProvince { get; set; }
    public string? PostalCode { get; set; }
    public string? Country { get; set; }
    public ICollection<Employee>? Employees { get; set; }
}
