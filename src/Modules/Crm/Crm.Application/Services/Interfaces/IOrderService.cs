using Crm.Application.DTOs;

namespace Crm.Application.Services.Interfaces;

public interface IOrderService
{
    Task<OrderDto> CreateAsync(CreateOrderDto dto, Guid userId);
    Task<OrderDto?> GetByIdAsync(Guid id);
    Task<(IEnumerable<OrderDto> Items, int Total)> GetPagedAsync(int page, int pageSize);
    Task<IEnumerable<OrderDto>> GetByAccountIdAsync(Guid accountId);
    Task<OrderDto> UpdateAsync(Guid id, UpdateOrderDto dto, Guid userId);
    Task DeleteAsync(Guid id, Guid userId);
}
