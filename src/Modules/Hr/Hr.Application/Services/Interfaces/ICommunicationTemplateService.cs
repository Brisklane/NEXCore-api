using Hr.Application.DTOs;

namespace Hr.Application.Services.Interfaces;

public interface ICommunicationTemplateService
{
    Task<IEnumerable<CommunicationTemplateDto>> GetAllAsync();
    Task<CommunicationTemplateDto?> GetByIdAsync(Guid id);
    Task<CommunicationTemplateDto?> GetByCodeAsync(string templateCode);
    Task<CommunicationTemplateDto> CreateAsync(CreateCommunicationTemplateDto request, Guid userId);
    Task<CommunicationTemplateDto> UpdateAsync(Guid id, UpdateCommunicationTemplateDto request, Guid userId);
    Task DeleteAsync(Guid id, Guid userId);
}
