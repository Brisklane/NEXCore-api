using Nexcore.SharedKernel;

namespace Manufacturing.Domain.Entities;

/// <summary>
/// Routing - defines the sequence of production steps and operations required to manufacture a product
/// </summary>
public class Routing : BaseEntity
{
    /// <summary>
    /// Product reference (External - from Inventory module)
    /// </summary>
    public Guid ProductId { get; set; }

    /// <summary>
    /// Name of the routing
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Routing version number
    /// </summary>
    public int Version { get; set; } = 1;

    // Navigation properties
    public ICollection<RoutingOperation>? Operations { get; set; }
}