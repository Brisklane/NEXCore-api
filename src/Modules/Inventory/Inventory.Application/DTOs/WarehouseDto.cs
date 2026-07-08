namespace Inventory.Application.DTOs;

/// <summary>
/// Warehouse DTO
/// </summary>
public class WarehouseDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? Region { get; set; }
    public string? PostalCode { get; set; }
    public string? Country { get; set; }
    public bool IsActive { get; set; }
    public string WarehouseType { get; set; } = null!;
}

/// <summary>
/// Create Warehouse DTO
/// </summary>
public class CreateWarehouseDto
{
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? Region { get; set; }
    public string? PostalCode { get; set; }
    public string? Country { get; set; }
    public string WarehouseType { get; set; } = "Main";
}

/// <summary>
/// Update Warehouse DTO
/// </summary>
public class UpdateWarehouseDto
{
    public string? Code { get; set; }
    public string? Name { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? Region { get; set; }
    public string? PostalCode { get; set; }
    public string? Country { get; set; }
    public bool? IsActive { get; set; }
    public string? WarehouseType { get; set; }
}

// ?? Bin ???????????????????????????????????????????????????????????????????????

/// <summary>
/// Bin DTO
/// </summary>
public class BinDto
{
    public Guid Id { get; set; }
    public Guid WarehouseId { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Aisle { get; set; }
    public string? Rack { get; set; }
    public string? Level { get; set; }
    public string? Position { get; set; }
    public decimal? Capacity { get; set; }
    public bool IsActive { get; set; }
}

/// <summary>
/// Create Bin DTO
/// </summary>
public class CreateBinDto
{
    public Guid WarehouseId { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Aisle { get; set; }
    public string? Rack { get; set; }
    public string? Level { get; set; }
    public string? Position { get; set; }
    public decimal? Capacity { get; set; }
}

/// <summary>
/// Update Bin DTO
/// </summary>
public class UpdateBinDto
{
    public string? Code { get; set; }
    public string? Name { get; set; }
    public string? Aisle { get; set; }
    public string? Rack { get; set; }
    public string? Level { get; set; }
    public string? Position { get; set; }
    public decimal? Capacity { get; set; }
    public bool? IsActive { get; set; }
}
