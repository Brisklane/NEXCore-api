using Core.Domain.Entities;
using Nexcore.SharedKernel.ValueObjects;

namespace Core.Tests.Unit.Builders;

public class BranchBuilder
{
    private Guid _id = Guid.NewGuid();
    private Guid _companyId = Guid.NewGuid();
    private string _code = "BR001";
    private string _name = "Main Branch";
    private string _branchType = "Headquarters";
    private string _phoneNumber = "+1234567890";
    private string _email = "branch@company.com";
    private string _managerName = "Branch Manager";
    private byte[] _branchLogo = new byte[] { 1, 2, 3 };
    private Address _address = new Address("123 Main St", "New York", "NY", "10001");
    private decimal? _latitude = 40.7128m;
    private decimal? _longitude = -74.0060m;
    private bool _isActive = true;
    private bool _isDeleted = false;
    private DateTime _createdAt = DateTime.UtcNow;
    private Guid _createdByUserId = Guid.NewGuid();

    public BranchBuilder WithId(Guid id) { _id = id; return this; }
    public BranchBuilder WithCompanyId(Guid companyId) { _companyId = companyId; return this; }
    public BranchBuilder WithCode(string code) { _code = code; return this; }
    public BranchBuilder WithName(string name) { _name = name; return this; }
    public BranchBuilder WithBranchType(string branchType) { _branchType = branchType; return this; }
    public BranchBuilder WithIsActive(bool isActive) { _isActive = isActive; return this; }
    public BranchBuilder WithIsDeleted(bool isDeleted) { _isDeleted = isDeleted; return this; }
    public BranchBuilder WithAddress(Address address) { _address = address; return this; }

    public Branch Build()
    {
        return new Branch
        {
            Id = _id,
            CompanyId = _companyId,
            Code = _code,
            Name = _name,
            BranchType = _branchType,
            PhoneNumber = _phoneNumber,
            Email = _email,
            ManagerName = _managerName,
            BranchLogo = _branchLogo,
            Address = _address,
            Latitude = _latitude,
            Longitude = _longitude,
            IsActive = _isActive,
            IsDeleted = _isDeleted,
            CreatedAt = _createdAt,
            CreatedByUserId = _createdByUserId,
            BusinessUnits = new List<BusinessUnit>()
        };
    }
}
