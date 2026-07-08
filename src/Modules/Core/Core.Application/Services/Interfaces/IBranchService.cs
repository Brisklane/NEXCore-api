using Core.Application.DTOs;
using Nexcore.SharedKernel;

namespace Core.Application.Services.Interfaces;

public interface IBranchService
{
    Task<Result<BranchResponseDto>> CreateBranchAsync(CreateBranchRequestDto request);
    Task<Result<BranchResponseDto>> GetBranchByIdAsync(Guid branchId);
    Task<Result<IEnumerable<BranchResponseDto>>> GetAllBranchesAsync(Guid? companyId = null);
    Task<Result<BranchResponseDto>> UpdateBranchAsync(Guid branchId, UpdateBranchRequestDto request);
    Task<Result> DeactivateBranchAsync(Guid branchId);
}
