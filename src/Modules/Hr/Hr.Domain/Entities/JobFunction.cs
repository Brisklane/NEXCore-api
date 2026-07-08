using Nexcore.SharedKernel;

namespace Hr.Domain.Entities;

public class JobFunction : BaseEntity
{
    public string JobFunctionCode { get; set; } = string.Empty;
    public string JobFunctionName { get; set; } = string.Empty;
    public ICollection<Designation>? Designations { get; set; }
    public ICollection<JobTemplate>? JobTemplates { get; set; }
}
