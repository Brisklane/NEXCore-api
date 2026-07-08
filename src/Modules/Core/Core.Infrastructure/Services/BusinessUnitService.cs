using Core.Application.DTOs;
using Core.Application.Services.Interfaces;
using Core.Domain.Entities;
using Core.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Nexcore.SharedKernel;

namespace Core.Infrastructure.Services;

/// <summary>
/// CRUD for business units within a branch, enforcing code/name uniqueness per branch. Deletes are
/// soft; lists come back oldest-first so the primary unit stays on top.
/// </summary>
public class BusinessUnitService : IBusinessUnitService
{
    private readonly CoreDbContext _context;

    public BusinessUnitService(CoreDbContext context)
    {
        _context = context;
    }

    public async Task<Result<BusinessUnitResponseDto>> CreateBusinessUnitAsync(CreateBusinessUnitRequestDto request)
    {
        try
        {
            var codeExists = await _context.BusinessUnits.AnyAsync(bu => bu.BranchId == request.BranchId && bu.Code == request.Code && !bu.IsDeleted);
            if (codeExists)
                return Result<BusinessUnitResponseDto>.Fail("Business unit code already exists for this branch.");

            var nameExists = await _context.BusinessUnits.AnyAsync(bu => bu.BranchId == request.BranchId && bu.Name == request.Name && !bu.IsDeleted);
            if (nameExists)
                return Result<BusinessUnitResponseDto>.Fail("Business unit name already exists for this branch.");

            var businessUnit = new BusinessUnit
            {
                CompanyId = request.CompanyId,
                BranchId = request.BranchId,
                Code = request.Code,
                Name = request.Name,
                UnitType = request.UnitType,
                Description = request.Description,
                ManagerName = request.ManagerName,
                ManagerEmail = request.ManagerEmail,
                IsActive = request.IsActive,
                CreatedAt = DateTime.UtcNow
            };

            _context.BusinessUnits.Add(businessUnit);
            await _context.SaveChangesAsync();

            return Result<BusinessUnitResponseDto>.Ok(MapToDto(businessUnit), "Business unit created successfully");
        }
        catch (Exception ex)
        {
            return Result<BusinessUnitResponseDto>.Fail($"Error creating business unit: {ex.Message}");
        }
    }

    public async Task<Result<BusinessUnitResponseDto>> GetBusinessUnitByIdAsync(Guid businessUnitId)
    {
        try
        {
            var businessUnit = await _context.BusinessUnits.FirstOrDefaultAsync(bu => bu.Id == businessUnitId && !bu.IsDeleted);
            if (businessUnit == null)
                return Result<BusinessUnitResponseDto>.Fail("Business unit not found");

            return Result<BusinessUnitResponseDto>.Ok(MapToDto(businessUnit));
        }
        catch (Exception ex)
        {
            return Result<BusinessUnitResponseDto>.Fail($"Error retrieving business unit: {ex.Message}");
        }
    }

    public async Task<Result<IEnumerable<BusinessUnitResponseDto>>> GetAllBusinessUnitsAsync(Guid? branchId = null)
    {
        try
        {
            var query = _context.BusinessUnits.AsQueryable();
            if (branchId.HasValue)
                query = query.Where(bu => bu.BranchId == branchId.Value);

            // Oldest first: the primary unit stays at the top and newly created units append at the end.
            var businessUnits = await query.Where(bu => !bu.IsDeleted).OrderBy(bu => bu.CreatedAt).ToListAsync();
            return Result<IEnumerable<BusinessUnitResponseDto>>.Ok(businessUnits.Select(MapToDto));
        }
        catch (Exception ex)
        {
            return Result<IEnumerable<BusinessUnitResponseDto>>.Fail($"Error retrieving business units: {ex.Message}");
        }
    }

    public async Task<Result<BusinessUnitResponseDto>> UpdateBusinessUnitAsync(Guid businessUnitId, UpdateBusinessUnitRequestDto request)
    {
        try
        {
            var businessUnit = await _context.BusinessUnits.FirstOrDefaultAsync(bu => bu.Id == businessUnitId && !bu.IsDeleted);
            if (businessUnit == null)
                return Result<BusinessUnitResponseDto>.Fail("Business unit not found");

            var nameExists = await _context.BusinessUnits.AnyAsync(bu =>
                bu.BranchId == businessUnit.BranchId && bu.Id != businessUnitId && bu.Name == request.Name && !bu.IsDeleted);
            if (nameExists)
                return Result<BusinessUnitResponseDto>.Fail("Business unit name already exists for this branch.");

            businessUnit.Name = request.Name;
            businessUnit.UnitType = request.UnitType;
            businessUnit.Description = request.Description;
            businessUnit.ManagerName = request.ManagerName;
            businessUnit.ManagerEmail = request.ManagerEmail;
            businessUnit.IsActive = request.IsActive;
            businessUnit.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Result<BusinessUnitResponseDto>.Ok(MapToDto(businessUnit), "Business unit updated successfully");
        }
        catch (Exception ex)
        {
            return Result<BusinessUnitResponseDto>.Fail($"Error updating business unit: {ex.Message}");
        }
    }

    public async Task<Result> DeactivateBusinessUnitAsync(Guid businessUnitId)
    {
        try
        {
            var businessUnit = await _context.BusinessUnits.FirstOrDefaultAsync(bu => bu.Id == businessUnitId && !bu.IsDeleted);
            if (businessUnit == null)
                return Result.Fail("Business unit not found");

            var now = DateTime.UtcNow;
            businessUnit.IsActive = false;
            businessUnit.IsDeleted = true;
            businessUnit.DeletedAt = now;
            businessUnit.UpdatedAt = now;

            await _context.SaveChangesAsync();

            return Result.Ok("Business unit deleted successfully");
        }
        catch (Exception ex)
        {
            return Result.Fail($"Error deactivating business unit: {ex.Message}");
        }
    }

    private static BusinessUnitResponseDto MapToDto(BusinessUnit bu) => new()
    {
        Id = bu.Id,
        CompanyId = bu.CompanyId,
        BranchId = bu.BranchId,
        Code = bu.Code!,
        Name = bu.Name,
        UnitType = bu.UnitType,
        Description = bu.Description,
        ManagerName = bu.ManagerName,
        ManagerEmail = bu.ManagerEmail,
        IsActive = bu.IsActive,
        CreatedAt = bu.CreatedAt,
        UpdatedAt = bu.UpdatedAt
    };
}
