using Core.Application.DTOs;
using Core.Application.Services.Interfaces;
using Core.Domain.Entities;
using Core.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Nexcore.SharedKernel;
using Nexcore.SharedKernel.ValueObjects;

namespace Core.Infrastructure.Services;

/// <summary>
/// CRUD for branches, enforcing code/name uniqueness per company. Creating a branch also seeds its
/// default "Main" business unit; deleting a branch soft-deletes it and cascades to its units.
/// </summary>
public class BranchService : IBranchService
{
    private readonly CoreDbContext _context;

    public BranchService(CoreDbContext context)
    {
        _context = context;
    }

    public async Task<Result<BranchResponseDto>> CreateBranchAsync(CreateBranchRequestDto request)
    {
        try
        {
            var codeExists = await _context.Branches.AnyAsync(b => b.CompanyId == request.CompanyId && b.Code == request.Code && !b.IsDeleted);
            if (codeExists)
                return Result<BranchResponseDto>.Fail("Branch code already exists for this company.");

            var nameExists = await _context.Branches.AnyAsync(b => b.CompanyId == request.CompanyId && b.Name == request.Name && !b.IsDeleted);
            if (nameExists)
                return Result<BranchResponseDto>.Fail("Branch name already exists for this company.");

            var branch = new Branch
            {
                CompanyId = request.CompanyId,
                Code = request.Code,
                Name = request.Name,
                BranchType = request.BranchType,
                PhoneNumber = request.PhoneNumber,
                Email = request.Email,
                ManagerName = request.ManagerName,
                BranchLogo = request.BranchLogo,
                Longitude = request.Longitude,
                Latitude = request.Latitude,
                Address = new Address
                {
                    StreetAddress = request.StreetAddress,
                    City = request.City,
                    State = request.State,
                    PostalCode = request.PostalCode
                },
                IsActive = request.IsActive,
                CreatedAt = DateTime.UtcNow
            };

            _context.Branches.Add(branch);
            await _context.SaveChangesAsync();

            // Seed a default "Main" business unit so POS and other records always have a BU context.
            // Its Code stays null: the (CompanyId, Code) unique index is filtered to non-null codes,
            // so every branch can have its own "Main" without colliding.
            _context.BusinessUnits.Add(new BusinessUnit
            {
                CompanyId = branch.CompanyId,
                BranchId = branch.Id,
                Name = "Main Business Unit",
                UnitType = "Main",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();

            return Result<BranchResponseDto>.Ok(MapToDto(branch), "Branch created successfully");
        }
        catch (Exception ex)
        {
            return Result<BranchResponseDto>.Fail($"Error creating branch: {ex.Message}");
        }
    }

    public async Task<Result<BranchResponseDto>> GetBranchByIdAsync(Guid branchId)
    {
        try
        {
            var branch = await _context.Branches.FirstOrDefaultAsync(b => b.Id == branchId && !b.IsDeleted);
            if (branch == null)
                return Result<BranchResponseDto>.Fail("Branch not found");

            return Result<BranchResponseDto>.Ok(MapToDto(branch));
        }
        catch (Exception ex)
        {
            return Result<BranchResponseDto>.Fail($"Error retrieving branch: {ex.Message}");
        }
    }

    public async Task<Result<IEnumerable<BranchResponseDto>>> GetAllBranchesAsync(Guid? companyId = null)
    {
        try
        {
            var query = _context.Branches.AsQueryable();
            if (companyId.HasValue)
                query = query.Where(b => b.CompanyId == companyId.Value);

            // Oldest first: the original/main branch stays on top and newer branches append at the end.
            var branches = await query.Where(b => !b.IsDeleted).OrderBy(b => b.CreatedAt).ToListAsync();
            return Result<IEnumerable<BranchResponseDto>>.Ok(branches.Select(MapToDto));
        }
        catch (Exception ex)
        {
            return Result<IEnumerable<BranchResponseDto>>.Fail($"Error retrieving branches: {ex.Message}");
        }
    }

    public async Task<Result<BranchResponseDto>> UpdateBranchAsync(Guid branchId, UpdateBranchRequestDto request)
    {
        try
        {
            var branch = await _context.Branches.FirstOrDefaultAsync(b => b.Id == branchId && !b.IsDeleted);
            if (branch == null)
                return Result<BranchResponseDto>.Fail("Branch not found");

            var nameExists = await _context.Branches.AnyAsync(b =>
                b.CompanyId == branch.CompanyId && b.Id != branchId && b.Name == request.Name && !b.IsDeleted);
            if (nameExists)
                return Result<BranchResponseDto>.Fail("Branch name already exists for this company.");

            branch.Name = request.Name;
            branch.BranchType = request.BranchType;
            branch.PhoneNumber = request.PhoneNumber;
            branch.Email = request.Email;
            branch.ManagerName = request.ManagerName;
            branch.IsActive = request.IsActive;
            branch.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Result<BranchResponseDto>.Ok(MapToDto(branch), "Branch updated successfully");
        }
        catch (Exception ex)
        {
            return Result<BranchResponseDto>.Fail($"Error updating branch: {ex.Message}");
        }
    }

    public async Task<Result> DeactivateBranchAsync(Guid branchId)
    {
        try
        {
            var branch = await _context.Branches
                .Include(b => b.BusinessUnits)
                .FirstOrDefaultAsync(b => b.Id == branchId && !b.IsDeleted);
            if (branch == null)
                return Result.Fail("Branch not found");

            var now = DateTime.UtcNow;
            branch.IsActive = false;
            branch.IsDeleted = true;
            branch.DeletedAt = now;
            branch.UpdatedAt = now;

            // Cascade the soft-delete to the branch's business units.
            foreach (var bu in branch.BusinessUnits.Where(u => !u.IsDeleted))
            {
                bu.IsActive = false;
                bu.IsDeleted = true;
                bu.DeletedAt = now;
                bu.UpdatedAt = now;
            }

            await _context.SaveChangesAsync();

            return Result.Ok("Branch deleted successfully");
        }
        catch (Exception ex)
        {
            return Result.Fail($"Error deactivating branch: {ex.Message}");
        }
    }

    private static BranchResponseDto MapToDto(Branch branch) => new()
    {
        Id = branch.Id,
        CompanyId = branch.CompanyId,
        Code = branch.Code!,
        Name = branch.Name,
        BranchType = branch.BranchType,
        PhoneNumber = branch.PhoneNumber,
        Email = branch.Email,
        ManagerName = branch.ManagerName,
        BranchLogo = branch.BranchLogo,
        Longitude = branch.Longitude,
        Latitude = branch.Latitude,
        StreetAddress = branch.Address?.StreetAddress ?? string.Empty,
        City = branch.Address?.City ?? string.Empty,
        State = branch.Address?.State ?? string.Empty,
        PostalCode = branch.Address?.PostalCode ?? string.Empty,
        IsActive = branch.IsActive,
        CreatedAt = branch.CreatedAt,
        UpdatedAt = branch.UpdatedAt
    };
}
