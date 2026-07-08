
using Nexcore.SharedKernel;

namespace Hr.Domain.Entities;

public class LookupValue : BaseEntity
{
    public Guid LookupTypeId { get; set; }
    public new string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool IsDefault { get; set; }
    public bool IsTerminal { get; set; }
    public Guid? ParentLookupValueId { get; set; }
    public DateTime? EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public string? MetadataJson { get; set; }

    public LookupType? LookupType { get; set; }
    public LookupValue? ParentLookupValue { get; set; }
    public ICollection<LookupValue>? Children { get; set; }
}

