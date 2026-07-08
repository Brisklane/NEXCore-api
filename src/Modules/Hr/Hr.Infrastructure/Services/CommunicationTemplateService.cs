using Hr.Application.DTOs;
using Hr.Application.Services.Interfaces;
using Hr.Domain.Entities;
using Hr.Infrastructure.Repositories.Interfaces;
using Microsoft.Extensions.Logging;

namespace Hr.Infrastructure.Services;

public class CommunicationTemplateService : ICommunicationTemplateService
{
    private readonly ICommunicationTemplateRepository _repo;
    private readonly ILogger<CommunicationTemplateService> _logger;

    public CommunicationTemplateService(ICommunicationTemplateRepository repo, ILogger<CommunicationTemplateService> logger)
    { _repo = repo; _logger = logger; }

    public async Task<IEnumerable<CommunicationTemplateDto>> GetAllAsync()
    {
        try { return (await _repo.GetAllByTenantAsync()).Where(e => !e.IsDeleted).Select(Map); }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving communication templates"); throw; }
    }

    public async Task<CommunicationTemplateDto?> GetByIdAsync(Guid id)
    {
        try { var e = await _repo.GetByIdAsync(id); return e is null ? null : Map(e); }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving communication template {Id}", id); throw; }
    }

    public async Task<CommunicationTemplateDto?> GetByCodeAsync(string templateCode)
    {
        try { var e = await _repo.GetByCodeAsync(templateCode); return e is null ? null : Map(e); }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving communication template {Code}", templateCode); throw; }
    }

    public async Task<CommunicationTemplateDto> CreateAsync(CreateCommunicationTemplateDto request, Guid userId)
    {
        try
        {
            var entity = new CommunicationTemplate
            {
                TemplateCode = request.TemplateCode, TemplateName = request.TemplateName,
                TemplateTypeLookupValueId = request.TemplateTypeLookupValueId,
                Subject = request.Subject, Body = request.Body,
                PlaceholdersJson = request.PlaceholdersJson, LanguageCode = request.LanguageCode,
                Version = 1, IsActive = true, IsSystemTemplate = false,
                CreatedAt = DateTime.UtcNow, CreatedByUserId = userId
            };
            await _repo.AddAsync(entity); await _repo.SaveChangesAsync();
            _logger.LogInformation("Communication template created: {Code} ({Id})", entity.TemplateCode, entity.Id);
            return Map(entity);
        }
        catch (Exception ex) { _logger.LogError(ex, "Error creating communication template"); throw; }
    }

    public async Task<CommunicationTemplateDto> UpdateAsync(Guid id, UpdateCommunicationTemplateDto request, Guid userId)
    {
        try
        {
            var entity = await _repo.GetByIdAsync(id) ?? throw new InvalidOperationException("Communication template not found");
            if (entity.IsSystemTemplate) throw new InvalidOperationException("System templates cannot be modified");
            if (!string.IsNullOrWhiteSpace(request.TemplateName)) entity.TemplateName = request.TemplateName;
            if (request.TemplateTypeLookupValueId.HasValue) entity.TemplateTypeLookupValueId = request.TemplateTypeLookupValueId.Value;
            if (request.Subject != null) entity.Subject = request.Subject;
            if (!string.IsNullOrWhiteSpace(request.Body)) { entity.Body = request.Body; entity.Version++; }
            if (request.PlaceholdersJson != null) entity.PlaceholdersJson = request.PlaceholdersJson;
            if (request.LanguageCode != null) entity.LanguageCode = request.LanguageCode;
            if (request.IsActive.HasValue) entity.IsActive = request.IsActive.Value;
            entity.UpdatedAt = DateTime.UtcNow; entity.UpdatedByUserId = userId;
            _repo.Update(entity); await _repo.SaveChangesAsync();
            return Map(entity);
        }
        catch (Exception ex) { _logger.LogError(ex, "Error updating communication template {Id}", id); throw; }
    }

    public async Task DeleteAsync(Guid id, Guid userId)
    {
        try
        {
            var entity = await _repo.GetByIdAsync(id) ?? throw new InvalidOperationException("Communication template not found");
            if (entity.IsSystemTemplate) throw new InvalidOperationException("System templates cannot be deleted");
            entity.IsDeleted = true; entity.DeletedAt = DateTime.UtcNow; entity.DeletedByUserId = userId;
            _repo.Update(entity); await _repo.SaveChangesAsync();
        }
        catch (Exception ex) { _logger.LogError(ex, "Error deleting communication template {Id}", id); throw; }
    }

    private static CommunicationTemplateDto Map(CommunicationTemplate e) => new()
    {
        Id = e.Id, CompanyId = e.CompanyId, TemplateCode = e.TemplateCode,
        TemplateName = e.TemplateName, TemplateTypeLookupValueId = e.TemplateTypeLookupValueId,
        Subject = e.Subject, Body = e.Body, PlaceholdersJson = e.PlaceholdersJson,
        LanguageCode = e.LanguageCode, Version = e.Version,
        IsActive = e.IsActive, IsSystemTemplate = e.IsSystemTemplate,
        CreatedAt = e.CreatedAt, UpdatedAt = e.UpdatedAt
    };
}
