using Crm.Application.DTOs;
using Crm.Application.Services.Interfaces;
using Crm.Domain.Entities;
using Crm.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Crm.Infrastructure.Services;

public class PipelineService : IPipelineService
{
    private readonly CrmDbContext _db;
    private readonly ILogger<PipelineService> _logger;

    public PipelineService(CrmDbContext db, ILogger<PipelineService> logger) { _db = db; _logger = logger; }

    public async Task<PipelineDto> CreateAsync(CreatePipelineDto dto, Guid userId)
    {
        var stages = dto.Stages.Select(s => new PipelineStage
        {
            StageName = s.StageName, DisplayOrder = s.DisplayOrder,
            ProbabilityPercent = s.ProbabilityPercent, IsWon = s.IsWon,
            IsLost = s.IsLost, ForecastCategory = s.ForecastCategory,
            CreatedByUserId = userId, CreatedAt = DateTime.UtcNow
        }).ToList();

        var entity = new Pipeline
        {
            PipelineName = dto.PipelineName, Description = dto.Description,
            IsDefault = dto.IsDefault, IsActive = dto.IsActive,
            Stages = stages, CreatedByUserId = userId, CreatedAt = DateTime.UtcNow
        };
        _db.Pipelines.Add(entity);
        await _db.SaveChangesAsync();
        return MapToDto(entity);
    }

    public async Task<PipelineDto?> GetByIdAsync(Guid id)
    {
        var e = await _db.Pipelines.AsNoTracking().Include(x => x.Stages)
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
        return e is null ? null : MapToDto(e);
    }

    public async Task<IEnumerable<PipelineDto>> GetAllAsync()
    {
        var items = await _db.Pipelines.AsNoTracking().Include(x => x.Stages)
            .Where(x => !x.IsDeleted).OrderBy(x => x.PipelineName).ToListAsync();
        return items.Select(MapToDto);
    }

    public async Task<PipelineDto> UpdateAsync(Guid id, UpdatePipelineDto dto, Guid userId)
    {
        var entity = await _db.Pipelines.Include(x => x.Stages)
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted)
            ?? throw new InvalidOperationException("Pipeline not found");
        entity.PipelineName = dto.PipelineName; entity.Description = dto.Description;
        entity.IsDefault = dto.IsDefault; entity.IsActive = dto.IsActive;
        entity.UpdatedAt = DateTime.UtcNow; entity.UpdatedByUserId = userId;
        await _db.SaveChangesAsync();
        return MapToDto(entity);
    }

    public async Task DeleteAsync(Guid id, Guid userId)
    {
        var entity = await _db.Pipelines.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted)
            ?? throw new InvalidOperationException("Pipeline not found");
        entity.IsDeleted = true; entity.DeletedAt = DateTime.UtcNow; entity.DeletedByUserId = userId;
        await _db.SaveChangesAsync();
    }

    public async Task<PipelineStageDto> AddStageAsync(Guid pipelineId, CreatePipelineStageDto dto, Guid userId)
    {
        var entity = new PipelineStage
        {
            PipelineId = pipelineId, StageName = dto.StageName, DisplayOrder = dto.DisplayOrder,
            ProbabilityPercent = dto.ProbabilityPercent, IsWon = dto.IsWon,
            IsLost = dto.IsLost, ForecastCategory = dto.ForecastCategory,
            CreatedByUserId = userId, CreatedAt = DateTime.UtcNow
        };
        _db.PipelineStages.Add(entity);
        await _db.SaveChangesAsync();
        return MapStageDto(entity);
    }

    public async Task<IEnumerable<PipelineStageDto>> GetStagesAsync(Guid pipelineId)
    {
        var items = await _db.PipelineStages.AsNoTracking()
            .Where(x => x.PipelineId == pipelineId && !x.IsDeleted)
            .OrderBy(x => x.DisplayOrder).ToListAsync();
        return items.Select(MapStageDto);
    }

    public async Task<PipelineStageDto> UpdateStageAsync(Guid pipelineId, Guid stageId, UpdatePipelineStageDto dto, Guid userId)
    {
        var entity = await _db.PipelineStages
            .FirstOrDefaultAsync(x => x.Id == stageId && x.PipelineId == pipelineId && !x.IsDeleted)
            ?? throw new InvalidOperationException("Pipeline stage not found");
        entity.StageName = dto.StageName; entity.DisplayOrder = dto.DisplayOrder;
        entity.ProbabilityPercent = dto.ProbabilityPercent; entity.IsWon = dto.IsWon;
        entity.IsLost = dto.IsLost; entity.ForecastCategory = dto.ForecastCategory;
        entity.UpdatedAt = DateTime.UtcNow; entity.UpdatedByUserId = userId;
        await _db.SaveChangesAsync();
        return MapStageDto(entity);
    }

    public async Task RemoveStageAsync(Guid pipelineId, Guid stageId, Guid userId)
    {
        var entity = await _db.PipelineStages
            .FirstOrDefaultAsync(x => x.Id == stageId && x.PipelineId == pipelineId && !x.IsDeleted)
            ?? throw new InvalidOperationException("Pipeline stage not found");
        entity.IsDeleted = true; entity.DeletedAt = DateTime.UtcNow; entity.DeletedByUserId = userId;
        await _db.SaveChangesAsync();
    }

    private static PipelineDto MapToDto(Pipeline e) => new()
    {
        Id = e.Id, PipelineName = e.PipelineName, Description = e.Description,
        IsDefault = e.IsDefault, IsActive = e.IsActive, CreatedAt = e.CreatedAt,
        Stages = e.Stages?.Where(s => !s.IsDeleted).Select(MapStageDto).OrderBy(s => s.DisplayOrder).ToList() ?? []
    };

    private static PipelineStageDto MapStageDto(PipelineStage s) => new()
    {
        Id = s.Id, PipelineId = s.PipelineId, StageName = s.StageName,
        DisplayOrder = s.DisplayOrder, ProbabilityPercent = s.ProbabilityPercent,
        IsWon = s.IsWon, IsLost = s.IsLost, ForecastCategory = s.ForecastCategory
    };
}
