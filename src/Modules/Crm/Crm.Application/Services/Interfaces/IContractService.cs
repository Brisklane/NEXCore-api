using Crm.Application.DTOs;

namespace Crm.Application.Services.Interfaces;

public interface IContractService
{
    Task<ContractDto> CreateAsync(CreateContractDto dto, Guid userId);
    Task<ContractDto?> GetByIdAsync(Guid id);
    Task<(IEnumerable<ContractDto> Items, int Total)> GetPagedAsync(int page, int pageSize);
    Task<IEnumerable<ContractDto>> GetByAccountIdAsync(Guid accountId);
    Task<ContractDto> UpdateAsync(Guid id, UpdateContractDto dto, Guid userId);
    Task DeleteAsync(Guid id, Guid userId);
    Task<ContractDto> ActivateAsync(Guid id, Guid userId);
}
