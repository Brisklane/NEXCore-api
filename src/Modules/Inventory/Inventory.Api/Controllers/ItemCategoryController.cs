using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Inventory.Application.DTOs;
using Inventory.Infrastructure.Repositories.Interfaces;
using Nexcore.SharedKernel.Api;
using Nexcore.SharedKernel.Events;
using Nexcore.SharedKernel.Helpers;
using System.Security.Claims;

namespace Inventory.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ItemCategoryController : ControllerBase
{
    private readonly IItemCategoryRepository _categoryRepository;
    private readonly IEventPublisher         _eventPublisher;
    private readonly ILogger<ItemCategoryController> _logger;

    public ItemCategoryController(
        IItemCategoryRepository categoryRepository,
        IEventPublisher eventPublisher,
        ILogger<ItemCategoryController> logger)
    {
        _categoryRepository = categoryRepository;
        _eventPublisher     = eventPublisher;
        _logger             = logger;
    }

    /// <summary>Get all categories</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResponse<ItemCategoryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] PaginationParams pagination, [FromQuery] bool? isActive)
    {
        try
        {
            var search = pagination.SearchTerm?.Trim().ToLower();
            var (categories, total) = await _categoryRepository.GetPagedAsync(
                pagination.PageNumber, pagination.PageSize,
                predicate: c =>
                    (isActive == null || c.IsActive == isActive) &&
                    (string.IsNullOrEmpty(search) ||
                        (c.Code != null && c.Code.ToLower().Contains(search)) ||
                        (c.Name != null && c.Name.ToLower().Contains(search)) ||
                        (c.Description != null && c.Description.ToLower().Contains(search))),
                orderBy: q => q.ApplyOrderNewestFirst(pagination.SortBy, pagination.SortDirection, "Code"));
            return Ok(PaginatedResponse<ItemCategoryDto>.Ok(categories.Select(MapToDto), total, pagination.PageNumber, pagination.PageSize));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving categories");
            return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving categories" });
        }
    }

    /// <summary>Get category by ID</summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<ItemCategoryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        try
        {
            var category = await _categoryRepository.GetByIdAsync(id);
            if (category == null)
                return NotFound(new ApiErrorResponse { Message = "Category not found" });

            return Ok(new ApiResponse<ItemCategoryDto>
            {
                Success = true,
                Data = MapToDto(category),
                Message = "Category retrieved successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving category {CategoryId}", id);
            return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving category" });
        }
    }

    /// <summary>Get active categories</summary>
    [HttpGet("active")]
    [ProducesResponseType(typeof(PaginatedResponse<ItemCategoryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetActive([FromQuery] PaginationParams pagination)
    {
        try
        {
            var (categories, total) = await _categoryRepository.GetPagedAsync(pagination.PageNumber, pagination.PageSize, predicate: c => c.IsActive);
            return Ok(PaginatedResponse<ItemCategoryDto>.Ok(categories.Select(MapToDto), total, pagination.PageNumber, pagination.PageSize));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving active categories");
            return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving active categories" });
        }
    }

    /// <summary>Create new category</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<ItemCategoryDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateItemCategoryDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(new ApiErrorResponse { Message = "Invalid input" });

            var existing = await _categoryRepository.GetByCodeAsync(dto.Code);
            if (existing != null)
                return BadRequest(new ApiErrorResponse { Message = "Category code already exists" });

            // ?? 1. Ask Accounting to create the four GL sub-accounts ??????????
            var companyId      = Guid.Parse(User.FindFirstValue("CompanyId")                  ?? Guid.Empty.ToString());
            var branchId       = Guid.Parse(User.FindFirstValue("BranchId")                   ?? Guid.Empty.ToString());
            var businessUnitId = Guid.TryParse(User.FindFirstValue("BusinessUnitId"), out var buId) ? buId : (Guid?)null;
            var userId         = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)    ?? Guid.Empty.ToString());

            var glEvent = new CategoryGlAccountsRequestedEvent
            {
                CompanyId      = companyId,
                BranchId       = branchId,
                BusinessUnitId = businessUnitId,
                CategoryCode   = dto.Code,
                CategoryName   = dto.Name,
                UserId         = userId,

                // Honour caller overrides; fall back to company defaults.
                ParentInventoryAccountNo = dto.ParentInventoryAccountNo ?? "1300",
                ParentCogsAccountNo      = dto.ParentCogsAccountNo      ?? "5100",
                ParentPurchaseAccountNo  = dto.ParentPurchaseAccountNo  ?? "2100",
                ParentSalesAccountNo     = dto.ParentSalesAccountNo     ?? "4100",
            };

            await _eventPublisher.PublishAsync(glEvent);

            // Wait for the Accounting handler to complete the TCS.
            CategoryGlAccountSet gl;
            if (glEvent.Result.Task.IsCompleted)
            {
                gl = await glEvent.Result.Task;
            }
            else
            {
                _logger.LogWarning(
                    "GL sub-account creation for Category:{Code} returned no result. " +
                    "Category will be saved without GL account links.", dto.Code);
                gl = CategoryGlAccountSet.Empty;
            }

            // ?? 2. Persist the category with the new GL account IDs ???????????
            var category = new Inventory.Domain.Entities.ItemCategory
            {
                Code             = dto.Code,
                Name             = dto.Name,
                Description      = dto.Description,
                ParentCategoryId = dto.ParentCategoryId,
                IsActive         = true,

                // GL accounts created by Accounting module
                InventoryAccountId = gl.Inventory,
                CogsAccountId      = gl.Cogs,
                PurchaseAccountId  = gl.Purchase,
                SalesAccountId     = gl.Sales,
            };

            await _categoryRepository.AddAsync(category);
            await _categoryRepository.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = category.Id },
                new ApiResponse<ItemCategoryDto>
                {
                    Success = true,
                    Data    = MapToDto(category),
                    Message = gl.IsComplete
                        ? "Category created successfully with GL accounts"
                        : "Category created successfully (GL accounts could not be created)",
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating category");
            return StatusCode(500, new ApiErrorResponse { Message = "Error creating category" });
        }
    }

    /// <summary>Update category</summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse<ItemCategoryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateItemCategoryDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(new ApiErrorResponse { Message = "Invalid input" });

            var category = await _categoryRepository.GetByIdAsync(id);
            if (category == null)
                return NotFound(new ApiErrorResponse { Message = "Category not found" });

            if (dto.Code != null && dto.Code != category.Code)
            {
                var dup = await _categoryRepository.GetByCodeAsync(dto.Code);
                if (dup != null)
                    return BadRequest(new ApiErrorResponse { Message = "Category code already exists" });
                category.Code = dto.Code;
            }
            if (dto.Name        != null) category.Name        = dto.Name;
            if (dto.Description != null) category.Description = dto.Description;
            if (dto.ParentCategoryId != null) category.ParentCategoryId = dto.ParentCategoryId;
            if (dto.IsActive    != null) category.IsActive    = dto.IsActive.Value;

            // ?? GL Account defaults ???????????????????????????????????????????
            if (dto.ClearGlAccounts)
            {
                category.InventoryAccountId = null;
                category.CogsAccountId      = null;
                category.PurchaseAccountId  = null;
                category.SalesAccountId     = null;
            }
            else
            {
                if (dto.InventoryAccountId != null) category.InventoryAccountId = dto.InventoryAccountId;
                if (dto.CogsAccountId      != null) category.CogsAccountId      = dto.CogsAccountId;
                if (dto.PurchaseAccountId  != null) category.PurchaseAccountId  = dto.PurchaseAccountId;
                if (dto.SalesAccountId     != null) category.SalesAccountId     = dto.SalesAccountId;
            }

            _categoryRepository.Update(category);
            await _categoryRepository.SaveChangesAsync();
            return Ok(new ApiResponse<ItemCategoryDto>
            {
                Success = true,
                Data = MapToDto(category),
                Message = "Category updated successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating category {Id}", id);
            return StatusCode(500, new ApiErrorResponse { Message = "Error updating category" });
        }
    }

    /// <summary>Generate GL accounts for an existing category (retroactive)</summary>
    [HttpPost("{id}/gl-accounts")]
    [ProducesResponseType(typeof(ApiResponse<ItemCategoryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GenerateGlAccounts(Guid id, [FromBody] GenerateCategoryGlAccountsDto dto)
    {
        try
        {
            var category = await _categoryRepository.GetByIdAsync(id);
            if (category == null)
                return NotFound(new ApiErrorResponse { Message = "Category not found" });

            var companyId      = Guid.Parse(User.FindFirstValue("CompanyId")                  ?? Guid.Empty.ToString());
            var branchId       = Guid.Parse(User.FindFirstValue("BranchId")                   ?? Guid.Empty.ToString());
            var businessUnitId = Guid.TryParse(User.FindFirstValue("BusinessUnitId"), out var buId) ? buId : (Guid?)null;
            var userId         = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)    ?? Guid.Empty.ToString());

            var glEvent = new CategoryGlAccountsRequestedEvent
            {
                CompanyId      = companyId,
                BranchId       = branchId,
                BusinessUnitId = businessUnitId,
                CategoryCode   = category.Code,
                CategoryName   = category.Name,
                UserId         = userId,

                ParentInventoryAccountNo = dto.ParentInventoryAccountNo ?? "1140",
                ParentCogsAccountNo      = dto.ParentCogsAccountNo      ?? "5100",
                ParentPurchaseAccountNo  = dto.ParentPurchaseAccountNo  ?? "2111",
                ParentSalesAccountNo     = dto.ParentSalesAccountNo     ?? "4100",
            };

            await _eventPublisher.PublishAsync(glEvent);

            CategoryGlAccountSet gl;
            try
            {
                gl = await glEvent.Result.Task.WaitAsync(TimeSpan.FromSeconds(15));
            }
            catch (TimeoutException)
            {
                _logger.LogWarning("GL sub-account creation timed out for Category:{Code}", category.Code);
                gl = CategoryGlAccountSet.Empty;
            }

            if (gl.Inventory != null) category.InventoryAccountId = gl.Inventory;
            if (gl.Cogs      != null) category.CogsAccountId      = gl.Cogs;
            if (gl.Purchase  != null) category.PurchaseAccountId  = gl.Purchase;
            if (gl.Sales     != null) category.SalesAccountId     = gl.Sales;

            _categoryRepository.Update(category);
            await _categoryRepository.SaveChangesAsync();

            var anyCreated = gl.Inventory.HasValue || gl.Cogs.HasValue || gl.Purchase.HasValue || gl.Sales.HasValue;
            return Ok(new ApiResponse<ItemCategoryDto>
            {
                Success = anyCreated,
                Data    = MapToDto(category),
                Message = gl.IsComplete
                    ? "GL accounts generated successfully"
                    : anyCreated
                        ? $"GL accounts partially generated (COGS skipped — parent account not found in chart). Inventory, Purchase and Sales accounts are ready."
                        : "GL accounts could not be generated. Check that parent accounts exist in the chart of accounts.",
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating GL accounts for category {Id}", id);
            return StatusCode(500, new ApiErrorResponse { Message = "Error generating GL accounts" });
        }
    }

    /// <summary>Delete category (soft-delete)</summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            var category = await _categoryRepository.GetByIdAsync(id);
            if (category == null) return NotFound(new ApiErrorResponse { Message = "Category not found" });

            _categoryRepository.SoftDelete(category);
            await _categoryRepository.SaveChangesAsync();

            return Ok(new ApiResponse<bool> { Success = true, Data = true, Message = "Category deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting category {Id}", id);
            return StatusCode(500, new ApiErrorResponse { Message = "Error deleting category" });
        }
    }

    /// <summary>Get category hierarchy</summary>
    [HttpGet("hierarchy")]
    [ProducesResponseType(typeof(ApiResponse<List<ItemCategoryDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetHierarchy([FromQuery] Guid? parentId = null)
    {
        try
        {
            var categories = await _categoryRepository.GetCategoryHierarchyAsync(parentId);

            return Ok(new ApiResponse<List<ItemCategoryDto>>
            {
                Success = true,
                Data = categories.Select(MapToDto).ToList(),
                Message = "Category hierarchy retrieved successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving category hierarchy");
            return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving category hierarchy" });
        }
    }

    private static ItemCategoryDto MapToDto(Inventory.Domain.Entities.ItemCategory c) => new()
    {
        Id               = c.Id,
        Code             = c.Code,
        Name             = c.Name,
        Description      = c.Description,
        ParentCategoryId = c.ParentCategoryId,
        IsActive         = c.IsActive,

        // ?? GL Account defaults ???????????????????????????????????????????????
        InventoryAccountId = c.InventoryAccountId,
        CogsAccountId      = c.CogsAccountId,
        PurchaseAccountId  = c.PurchaseAccountId,
        SalesAccountId     = c.SalesAccountId,
    };
}
