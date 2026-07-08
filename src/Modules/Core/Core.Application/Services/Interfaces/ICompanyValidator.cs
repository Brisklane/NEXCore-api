using Core.Application.DTOs;
using Core.Application.DTOs.ValidateDtos;
using Nexcore.SharedKernel.Api;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Application.Services.Interfaces;

public interface ICompanyValidator
{
    Task<ApiResponse<CompanyDto>> ValidateAsync(CompanyDto request);
    Task<ApiResponse<UpdateCompanyRequest>?> ValidateCompanyUpdateAsync(UpdateCompanyRequest request);
    Task<ApiResponse<UpdateCompanyRequest>?> ValidateBranchUpdateAsync(UpdateBranchRequestDto request, bool isNewBranch);
    Task<ApiResponse<UpdateCompanyRequest>?> ValidateBusinessUnitUpdateAsync(UpdateBusinessUnitRequestDto request, bool isNewUnit);
}
