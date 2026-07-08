using Core.Domain.Entities;

namespace Core.Tests.Unit.Builders;

public class BusinessUnitBuilder
{
    private Guid _id = Guid.NewGuid();
    private Guid _companyId = Guid.NewGuid();
    private Guid _branchId = Guid.NewGuid();
    private string _code = "BU001";
    private string _name = "Main Business Unit";
    private string _unitType = "Department";
    private string _description = "Main department";
    private string _managerName = "Unit Manager";
    private string _managerEmail = "manager@company.com";
    private bool _isActive = true;
    private bool _isDeleted = false;
    private DateTime _createdAt = DateTime.UtcNow;
    private Guid _createdByUserId = Guid.NewGuid();

    public BusinessUnitBuilder WithId(Guid id) { _id = id; return this; }
    public BusinessUnitBuilder WithCompanyId(Guid companyId) { _companyId = companyId; return this; }
    public BusinessUnitBuilder WithBranchId(Guid branchId) { _branchId = branchId; return this; }
    public BusinessUnitBuilder WithCode(string code) { _code = code; return this; }
    public BusinessUnitBuilder WithName(string name) { _name = name; return this; }
    public BusinessUnitBuilder WithUnitType(string unitType) { _unitType = unitType; return this; }
    public BusinessUnitBuilder WithIsActive(bool isActive) { _isActive = isActive; return this; }
    public BusinessUnitBuilder WithIsDeleted(bool isDeleted) { _isDeleted = isDeleted; return this; }

    public BusinessUnit Build()
    {
        return new BusinessUnit
        {
            Id = _id,
            CompanyId = _companyId,
            BranchId = _branchId,
            Code = _code,
            Name = _name,
            UnitType = _unitType,
            Description = _description,
            ManagerName = _managerName,
            ManagerEmail = _managerEmail,
            IsActive = _isActive,
            IsDeleted = _isDeleted,
            CreatedAt = _createdAt,
            CreatedByUserId = _createdByUserId
        };
    }
}
