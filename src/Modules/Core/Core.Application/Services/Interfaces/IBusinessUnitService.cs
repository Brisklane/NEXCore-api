using Core.Application.DTOs;
using Nexcore.SharedKernel;
namespace Core.Application.Services.Interfaces;

public interface IBusinessUnitService
{
    Task<Result<BusinessUnitResponseDto>> CreateBusinessUnitAsync(CreateBusinessUnitRequestDto request);
    Task<Result<BusinessUnitResponseDto>> GetBusinessUnitByIdAsync(Guid businessUnitId);
    Task<Result<IEnumerable<BusinessUnitResponseDto>>> GetAllBusinessUnitsAsync(Guid? branchId = null);
    Task<Result<BusinessUnitResponseDto>> UpdateBusinessUnitAsync(Guid businessUnitId, UpdateBusinessUnitRequestDto request);
    Task<Result> DeactivateBusinessUnitAsync(Guid businessUnitId);
}
