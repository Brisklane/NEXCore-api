using Crm.Application.DTOs;

namespace Crm.Application.Services.Interfaces;

public interface IPipelineService
{
    Task<PipelineDto> CreateAsync(CreatePipelineDto dto, Guid userId);
    Task<PipelineDto?> GetByIdAsync(Guid id);
    Task<IEnumerable<PipelineDto>> GetAllAsync();
    Task<PipelineDto> UpdateAsync(Guid id, UpdatePipelineDto dto, Guid userId);
    Task DeleteAsync(Guid id, Guid userId);

    // Stages
    Task<PipelineStageDto> AddStageAsync(Guid pipelineId, CreatePipelineStageDto dto, Guid userId);
    Task<IEnumerable<PipelineStageDto>> GetStagesAsync(Guid pipelineId);
    Task<PipelineStageDto> UpdateStageAsync(Guid pipelineId, Guid stageId, UpdatePipelineStageDto dto, Guid userId);
    Task RemoveStageAsync(Guid pipelineId, Guid stageId, Guid userId);
}
