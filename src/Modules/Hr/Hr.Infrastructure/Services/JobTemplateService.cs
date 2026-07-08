using Hr.Application.DTOs;
using Hr.Application.Services.Interfaces;
using Hr.Domain.Entities;
using Hr.Infrastructure.Repositories.Interfaces;
using Microsoft.Extensions.Logging;

namespace Hr.Infrastructure.Services;

public class JobTemplateService : IJobTemplateService
{
    private readonly IJobTemplateRepository _repository;
    private readonly ILogger<JobTemplateService> _logger;

    public JobTemplateService(IJobTemplateRepository repository, ILogger<JobTemplateService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<IEnumerable<JobTemplateDto>> GetAllAsync()
    {
        var items = await _repository.GetAllByTenantAsync();
        return items.Where(i => !i.IsDeleted).Select(MapToDto);
    }

    public async Task<JobTemplateDto?> GetByIdAsync(Guid id)
    {
        var item = await _repository.GetByIdAsync(id);
        return item == null ? null : MapToDto(item);
    }

    public async Task<JobTemplateDto> CreateAsync(CreateJobTemplateDto request, Guid userId)
    {
        var entity = new JobTemplate
        {
            TemplateCode = request.TemplateCode,
            TemplateName = request.TemplateName,
            JobTitle = request.JobTitle,
            DepartmentId = request.DepartmentId,
            DesignationId = request.DesignationId,
            EmploymentType = request.EmploymentType,
            Description = request.Description,
            Requirements = request.Requirements,
            RequiredSkills = request.RequiredSkills,
            Responsibilities = request.Responsibilities,
            IsActive = request.IsActive,
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = userId
        };

        await _repository.AddAsync(entity);
        await _repository.SaveChangesAsync();

        return MapToDto(entity);
    }

    public async Task<JobTemplateDto> UpdateAsync(Guid id, UpdateJobTemplateDto request, Guid userId)
    {
        var entity = await _repository.GetByIdAsync(id);
        if (entity == null || entity.IsDeleted) throw new InvalidOperationException("JobTemplate not found");

        if (request.TemplateName != null) entity.TemplateName = request.TemplateName;
        if (request.JobTitle != null) entity.JobTitle = request.JobTitle;
        if (request.DepartmentId.HasValue) entity.DepartmentId = request.DepartmentId.Value;
        if (request.DesignationId.HasValue) entity.DesignationId = request.DesignationId.Value;
        if (request.EmploymentType != null) entity.EmploymentType = request.EmploymentType;
        if (request.Description != null) entity.Description = request.Description;
        if (request.Requirements != null) entity.Requirements = request.Requirements;
        if (request.RequiredSkills != null) entity.RequiredSkills = request.RequiredSkills;
        if (request.Responsibilities != null) entity.Responsibilities = request.Responsibilities;
        if (request.IsActive.HasValue) entity.IsActive = request.IsActive.Value;

        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedByUserId = userId;

        _repository.Update(entity);
        await _repository.SaveChangesAsync();

        return MapToDto(entity);
    }

    public async Task DeleteAsync(Guid id, Guid userId)
    {
        var entity = await _repository.GetByIdAsync(id);
        if (entity == null || entity.IsDeleted) throw new InvalidOperationException("JobTemplate not found");

        entity.IsDeleted = true;
        entity.DeletedAt = DateTime.UtcNow;
        entity.DeletedByUserId = userId;

        _repository.Update(entity);
        await _repository.SaveChangesAsync();
    }

    private static JobTemplateDto MapToDto(JobTemplate j) => new()
    {
        Id = j.Id,
        CompanyId = j.CompanyId,
        TemplateCode = j.TemplateCode,
        TemplateName = j.TemplateName,
        JobTitle = j.JobTitle,
        DepartmentId = j.JobFamilyId ?? Guid.Empty,
        DesignationId = j.GradeId ?? Guid.Empty,
        EmploymentType = j.EmploymentType,
        Description = j.Description,
        Requirements = j.Requirements,
        RequiredSkills = j.RequiredSkills,
        Responsibilities = j.Responsibilities,
        IsActive = j.IsActive,
        VersionNumber = j.VersionNumber,
        CreatedAt = j.CreatedAt,
        UpdatedAt = j.UpdatedAt
    };
}
