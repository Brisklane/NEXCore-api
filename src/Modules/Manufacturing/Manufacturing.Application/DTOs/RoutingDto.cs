namespace Manufacturing.Application.DTOs;

public class RoutingDto
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public required string Name { get; set; }
    public int Version { get; set; }
    public bool IsActive { get; set; }
    public string? Description { get; set; }
    public List<RoutingOperationDto> Operations { get; set; } = [];
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateRoutingDto
{
    public Guid ProductId { get; set; }
    public required string Name { get; set; }
    public int Version { get; set; } = 1;
    public string? Description { get; set; }
    public List<CreateRoutingOperationDto> Operations { get; set; } = [];
}

public class UpdateRoutingDto
{
    public string? Name { get; set; }
    public int? Version { get; set; }
    public bool? IsActive { get; set; }
    public string? Description { get; set; }
}

// ---- Routing Operation ----
public class RoutingOperationDto
{
    public Guid Id { get; set; }
    public Guid RoutingId { get; set; }
    public int SequenceNo { get; set; }
    public required string OperationName { get; set; }
    public Guid WorkCenterId { get; set; }
    public decimal StandardHours { get; set; }
    public decimal? SetupHours { get; set; }
    public decimal? LaborHours { get; set; }
    public decimal? MachineHours { get; set; }
    public string? Notes { get; set; }
}

public class CreateRoutingOperationDto
{
    public int SequenceNo { get; set; }
    public required string OperationName { get; set; }
    public Guid WorkCenterId { get; set; }
    public decimal StandardHours { get; set; }
    public decimal? SetupHours { get; set; }
    public decimal? LaborHours { get; set; }
    public decimal? MachineHours { get; set; }
    public string? Notes { get; set; }
}

public class UpdateRoutingOperationDto
{
    public int? SequenceNo { get; set; }
    public string? OperationName { get; set; }
    public Guid? WorkCenterId { get; set; }
    public decimal? StandardHours { get; set; }
    public decimal? SetupHours { get; set; }
    public decimal? LaborHours { get; set; }
    public decimal? MachineHours { get; set; }
    public string? Notes { get; set; }
}
