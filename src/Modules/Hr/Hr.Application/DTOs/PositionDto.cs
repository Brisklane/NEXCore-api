namespace Hr.Application.DTOs;

public class PositionDto
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public string PositionCode { get; set; } = string.Empty;
    public string PositionName { get; set; } = string.Empty;
    public Guid DepartmentId { get; set; }
    public Guid DesignationId { get; set; }
    public Guid? JobFamilyId { get; set; }
    public Guid? JobFunctionId { get; set; }
    public Guid? GradeId { get; set; }
    public Guid? PayScaleId { get; set; }
    public Guid? ReportsToPositionId { get; set; }
    public bool IsVacant { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreatePositionDto
{
    public string PositionCode { get; set; } = string.Empty;
    public string PositionName { get; set; } = string.Empty;
    public Guid DepartmentId { get; set; }
    public Guid DesignationId { get; set; }
    public Guid? JobFamilyId { get; set; }
    public Guid? JobFunctionId { get; set; }
    public Guid? GradeId { get; set; }
    public Guid? PayScaleId { get; set; }
    public Guid? ReportsToPositionId { get; set; }
}

public class UpdatePositionDto
{
    public string? PositionName { get; set; }
    public Guid? DepartmentId { get; set; }
    public Guid? DesignationId { get; set; }
    public Guid? JobFamilyId { get; set; }
    public Guid? JobFunctionId { get; set; }
    public Guid? GradeId { get; set; }
    public Guid? PayScaleId { get; set; }
    public Guid? ReportsToPositionId { get; set; }
    public bool? IsVacant { get; set; }
    public bool? IsActive { get; set; }
}
