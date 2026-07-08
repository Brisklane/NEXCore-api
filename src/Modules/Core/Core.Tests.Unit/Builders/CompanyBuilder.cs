using Core.Domain.Entities;
using Nexcore.SharedKernel.ValueObjects;

namespace Core.Tests.Unit.Builders;

public class CompanyBuilder
{
    private Guid _id = Guid.NewGuid();
    private string _code = "COMP001";
    private string _companyName = "Test Company";
    private string _legalName = "Test Company Legal";
    private string _registrationNumber = "REG123456";
    private string _baseCurrencyCode = "USD";
    private Address _address = new Address("123 Main St", "New York", "NY", "10001");
    private string _phoneNumber = "+1234567890";
    private string _mobileNumber = "+1234567891";
    private string _contactPerson = "John Doe";
    private string _email = "contact@company.com";
    private string _websiteUrl = "https://company.com";
    private byte[] _companyLogo = new byte[] { 1, 2, 3 };
    private decimal? _latitude = 40.7128m;
    private decimal? _longitude = -74.0060m;
    private int _radiusInMeters = 500;
    private bool _isActive = true;
    private bool _isDeleted = false;
    private int _fiscalYearStartMonth = 1;
    private DateTime _createdAt = DateTime.UtcNow;
    private Guid _createdByUserId = Guid.NewGuid();
    private List<Branch> _branches = new();
    private List<BusinessUnit> _businessUnits = new();

    public CompanyBuilder WithId(Guid id) { _id = id; return this; }
    public CompanyBuilder WithCode(string code) { _code = code; return this; }
    public CompanyBuilder WithCompanyName(string companyName) { _companyName = companyName; return this; }
    public CompanyBuilder WithLegalName(string legalName) { _legalName = legalName; return this; }
    public CompanyBuilder WithEmail(string email) { _email = email; return this; }
    public CompanyBuilder WithIsActive(bool isActive) { _isActive = isActive; return this; }
    public CompanyBuilder WithIsDeleted(bool isDeleted) { _isDeleted = isDeleted; return this; }
    public CompanyBuilder WithAddress(Address address) { _address = address; return this; }
    public CompanyBuilder WithBranches(List<Branch> branches) { _branches = branches; return this; }

    public Company Build()
    {
        return new Company
        {
            Id = _id,
            Slug = _code.ToLowerInvariant(),
            Code = _code,
            CompanyName = _companyName,
            LegalName = _legalName,
            RegistrationNumber = _registrationNumber,
            BaseCurrencyCode = _baseCurrencyCode,
            Address = _address,
            PhoneNumber = _phoneNumber,
            MobileNumber = _mobileNumber,
            ContactPerson = _contactPerson,
            Email = _email,
            WebsiteUrl = _websiteUrl,
            CompanyLogo = _companyLogo,
            Latitude = _latitude,
            Longitude = _longitude,
            RadiusInMeters = _radiusInMeters,
            IsActive = _isActive,
            IsDeleted = _isDeleted,
            FiscalYearStartMonth = _fiscalYearStartMonth,
            CreatedAt = _createdAt,
            CreatedByUserId = _createdByUserId,
            Branches = _branches,
            BusinessUnits = _businessUnits
        };
    }
}
