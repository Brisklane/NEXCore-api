using Hr.Application.DTOs;

namespace Hr.Application.Services.Interfaces;

public interface IChannelTemplateService
{
    Task<IEnumerable<ChannelTemplateDto>> GetAllAsync();
    Task<ChannelTemplateDto?> GetByIdAsync(Guid id);
    Task<ChannelTemplateDto> CreateAsync(CreateChannelTemplateDto request, Guid userId);
    Task<ChannelTemplateDto> UpdateAsync(Guid id, UpdateChannelTemplateDto request, Guid userId);
    Task DeleteAsync(Guid id, Guid userId);
}
