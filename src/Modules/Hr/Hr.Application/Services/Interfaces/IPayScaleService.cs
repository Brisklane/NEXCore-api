using Hr.Application.DTOs;

namespace Hr.Application.Services.Interfaces;

public interface IPayScaleService
{
    Task<IEnumerable<PayScaleDto>> GetAllAsync();
    Task<PayScaleDto?> GetByIdAsync(Guid id);
    Task<PayScaleDto> CreateAsync(CreatePayScaleDto request, Guid userId);
    Task<PayScaleDto> UpdateAsync(Guid id, UpdatePayScaleDto request, Guid userId);
    Task DeleteAsync(Guid id, Guid userId);
}
