namespace Inventory.Application.DTOs;

/// <summary>
/// Unit of Measure DTO
/// </summary>
public class UnitDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public int DisplayOrder { get; set; }
}

/// <summary>
/// Create Unit DTO
/// </summary>
public class CreateUnitDto
{
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public int DisplayOrder { get; set; } = 0;
}

/// <summary>
/// Update Unit DTO
/// </summary>
public class UpdateUnitDto
{
    public string? Code { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public bool? IsActive { get; set; }
    public int? DisplayOrder { get; set; }
}

// ?? UOM Conversion ?????????????????????????????????????????????????????????

public class ItemUomConversionDto
{
    public Guid Id { get; set; }
    public Guid ItemId { get; set; }
    public Guid FromUnitId { get; set; }
    public string FromUnitCode { get; set; } = null!;
    public Guid ToUnitId { get; set; }
    public string ToUnitCode { get; set; } = null!;
    /// <summary>How many ToUnits equal 1 FromUnit (e.g., 1 BOX = 12 PCS ? factor = 12)</summary>
    public decimal ConversionFactor { get; set; }
    public bool IsActive { get; set; }
}

public class CreateItemUomConversionDto
{
    public Guid FromUnitId { get; set; }
    public Guid ToUnitId { get; set; }
    public decimal ConversionFactor { get; set; }
}

public class UpdateItemUomConversionDto
{
    public decimal? ConversionFactor { get; set; }
    public bool? IsActive { get; set; }
}
