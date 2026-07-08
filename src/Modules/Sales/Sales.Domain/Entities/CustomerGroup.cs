using Nexcore.SharedKernel;

namespace Sales.Domain.Entities;

/// <summary>
/// Customer group / account group for sales segmentation and pricing.
/// Contacts are linked to groups via Crm.Contact.CustomerGroupId (cross-module Guid).
/// </summary>
public class CustomerGroup : BaseEntity
{
    public new string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool AllowCreditSales { get; set; } = true;
    public decimal DefaultCreditLimit { get; set; }
}
