namespace Crm.Application.DTOs;

public class PipelineDto
{
    public Guid Id { get; set; }
    public string PipelineName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<PipelineStageDto> Stages { get; set; } = new();
}

public class CreatePipelineDto
{
    public string PipelineName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; } = true;
    public List<CreatePipelineStageDto> Stages { get; set; } = new();
}

public class UpdatePipelineDto
{
    public string PipelineName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; }
}

public class PipelineStageDto
{
    public Guid Id { get; set; }
    public Guid PipelineId { get; set; }
    public string StageName { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public decimal ProbabilityPercent { get; set; }
    public bool IsWon { get; set; }
    public bool IsLost { get; set; }
    public string? ForecastCategory { get; set; }
}

public class CreatePipelineStageDto
{
    public string StageName { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public decimal ProbabilityPercent { get; set; }
    public bool IsWon { get; set; }
    public bool IsLost { get; set; }
    public string? ForecastCategory { get; set; }
}

public class UpdatePipelineStageDto : CreatePipelineStageDto { }
