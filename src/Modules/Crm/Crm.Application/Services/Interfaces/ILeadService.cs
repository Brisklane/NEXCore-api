using Crm.Application.DTOs;
using Nexcore.SharedKernel.Api;

namespace Crm.Application.Services.Interfaces;

public interface ILeadService
{
    Task<LeadDto> CreateAsync(CreateLeadDto dto, Guid userId);
    Task<LeadDto?> GetByIdAsync(Guid id);
    Task<PaginatedResponse<LeadDto>> GetPagedAsync(PaginationParams pagination, string? status = null);
    Task<LeadDto> UpdateAsync(Guid id, UpdateLeadDto dto, Guid userId);
    Task DeleteAsync(Guid id, Guid userId);
    Task<LeadDto> ConvertAsync(Guid id, ConvertLeadDto dto, Guid userId);
}
