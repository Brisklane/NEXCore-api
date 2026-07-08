using Crm.Application.DTOs;
using Nexcore.SharedKernel.Api;

namespace Crm.Application.Services.Interfaces;

public interface ICampaignService
{
    Task<CampaignDto> CreateAsync(CreateCampaignDto dto, Guid userId);
    Task<CampaignDto?> GetByIdAsync(Guid id);
    Task<PaginatedResponse<CampaignDto>> GetPagedAsync(PaginationParams pagination, string? status = null);
    Task<CampaignDto> UpdateAsync(Guid id, UpdateCampaignDto dto, Guid userId);
    Task DeleteAsync(Guid id, Guid userId);

    // Campaign Members
    Task<CampaignMemberDto> AddMemberAsync(Guid campaignId, CreateCampaignMemberDto dto, Guid userId);
    Task<IEnumerable<CampaignMemberDto>> GetMembersAsync(Guid campaignId);
    Task RemoveMemberAsync(Guid campaignId, Guid memberId, Guid userId);
}
