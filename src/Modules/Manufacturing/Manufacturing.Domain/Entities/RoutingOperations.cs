using Nexcore.SharedKernel;

namespace Manufacturing.Domain.Entities;

/// <summary>
/// Routing Operation - individual step in the manufacturing process
/// </summary>
public class RoutingOperation : BaseEntity
{
    /// <summary>
    /// Routing reference
    /// </summary>
    public Guid RoutingId { get; set; }

    /// <summary>
    /// Sequence number of the operation
    /// </summary>
    public int SequenceNo { get; set; }

    /// <summary>
    /// Operation name
    /// </summary>
    public required string OperationName { get; set; }

    /// <summary>
    /// Work Center where operation is performed
    /// </summary>
    public Guid WorkCenterId { get; set; }

    /// <summary>
    /// Standard hours for operation
    /// </summary>
    public decimal StandardHours { get; set; }

    /// <summary>
    /// Setup hours for operation
    /// </summary>
    public decimal? SetupHours { get; set; }

    /// <summary>
    /// Labor hours for operation
    /// </summary>
    public decimal? LaborHours { get; set; }

    /// <summary>
    /// Machine hours for operation
    /// </summary>
    public decimal? MachineHours { get; set; }

    /// <summary>
    /// Notes for this operation
    /// </summary>
    public string? Notes { get; set; }

    // Navigation properties
    public Routing? Routing { get; set; }
    public WorkCenter? WorkCenter { get; set; }
}