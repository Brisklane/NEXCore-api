
using Nexcore.SharedKernel;

namespace Hr.Domain.Entities;

public class LookupType : BaseEntity
{
    public new string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? ModuleName { get; set; }
    public string? EntityName { get; set; }
    public bool IsSystem { get; set; }
    public int SortOrder { get; set; }

    public ICollection<LookupValue>? Values { get; set; }
}

