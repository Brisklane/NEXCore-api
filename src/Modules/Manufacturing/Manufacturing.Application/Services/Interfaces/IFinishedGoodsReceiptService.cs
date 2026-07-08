using Manufacturing.Application.DTOs;
using Nexcore.SharedKernel.Api;

namespace Manufacturing.Application.Services.Interfaces;

public interface IFinishedGoodsReceiptService
{
    Task<FinishedGoodsReceiptDto> CreateAsync(CreateFinishedGoodsReceiptDto request, Guid userId);
    Task<PaginatedResponse<FinishedGoodsReceiptDto>> GetAllAsync(PaginationParams pagination);
    Task<FinishedGoodsReceiptDto?> GetByIdAsync(Guid id);
    Task<PaginatedResponse<FinishedGoodsReceiptDto>> GetByProductionOrderAsync(Guid productionOrderId, PaginationParams pagination);
    Task<FinishedGoodsReceiptDto> UpdateAsync(Guid id, UpdateFinishedGoodsReceiptDto request, Guid userId);
    Task DeleteAsync(Guid id, Guid userId);
}
