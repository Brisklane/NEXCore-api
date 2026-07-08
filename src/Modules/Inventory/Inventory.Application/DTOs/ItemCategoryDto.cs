namespace Inventory.Application.DTOs;

/// <summary>
/// Item Category DTO
/// </summary>
public class ItemCategoryDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public Guid? ParentCategoryId { get; set; }
    public bool IsActive { get; set; }

    // ?? Default GL Accounts ???????????????????????????????????????????????????
    /// <summary>Default inventory asset GL account inherited by items in this category</summary>
    public Guid? InventoryAccountId { get; set; }

    /// <summary>Default Cost of Goods Sold GL account</summary>
    public Guid? CogsAccountId { get; set; }

    /// <summary>Default purchases / accounts-payable clearing GL account</summary>
    public Guid? PurchaseAccountId { get; set; }

    /// <summary>Default sales revenue GL account</summary>
    public Guid? SalesAccountId { get; set; }
}

/// <summary>
/// Create Item Category DTO
/// </summary>
public class CreateItemCategoryDto
{
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public Guid? ParentCategoryId { get; set; }

    // ?? Parent COA account numbers ????????????????????????????????????????????
    // When a category is created the Accounting module automatically creates four
    // dedicated GL sub-accounts (one per posting type) under these parent accounts.
    // Leave null to use the company defaults seeded by AccountingInitializationService.
    // Override only when this category must post to a different section of the COA
    // (e.g., a "Services" category that has no inventory asset account).

    /// <summary>
    /// Parent inventory-asset account number. Default: "1300".
    /// The new sub-account will be numbered "1300-{Code}".
    /// </summary>
    public string? ParentInventoryAccountNo { get; set; }

    /// <summary>
    /// Parent COGS account number. Default: "5100".
    /// The new sub-account will be numbered "5100-{Code}".
    /// </summary>
    public string? ParentCogsAccountNo { get; set; }

    /// <summary>
    /// Parent purchase-clearing account number. Default: "2100".
    /// The new sub-account will be numbered "2100-{Code}".
    /// </summary>
    public string? ParentPurchaseAccountNo { get; set; }

    /// <summary>
    /// Parent sales-revenue account number. Default: "4100".
    /// The new sub-account will be numbered "4100-{Code}".
    /// </summary>
    public string? ParentSalesAccountNo { get; set; }
}

/// <summary>
/// Generate GL accounts for an existing category (retroactive — same logic as create).
/// </summary>
public class GenerateCategoryGlAccountsDto
{
    public string? ParentInventoryAccountNo { get; set; }
    public string? ParentCogsAccountNo { get; set; }
    public string? ParentPurchaseAccountNo { get; set; }
    public string? ParentSalesAccountNo { get; set; }
}

/// <summary>
/// Update Item Category DTO
/// </summary>
public class UpdateItemCategoryDto
{
    public string? Code { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public Guid? ParentCategoryId { get; set; }
    public bool? IsActive { get; set; }

    // ?? Default GL Accounts ???????????????????????????????????????????????????
    /// <summary>Pass a value to update; omit (null) to leave unchanged</summary>
    public Guid? InventoryAccountId { get; set; }
    public Guid? CogsAccountId { get; set; }
    public Guid? PurchaseAccountId { get; set; }
    public Guid? SalesAccountId { get; set; }

    /// <summary>
    /// Set to true to explicitly clear all four GL account defaults on this category.
    /// Items will then fall back to null (no account) unless they have their own override.
    /// </summary>
    public bool ClearGlAccounts { get; set; }
}
