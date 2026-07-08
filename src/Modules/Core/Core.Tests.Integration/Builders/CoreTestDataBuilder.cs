using Core.Domain.Entities;
using Core.Infrastructure.Persistence;
using Nexcore.SharedKernel.ValueObjects;

namespace Core.Tests.Integration.Builders;

/// <summary>
/// Seeds test entities directly into the DbContext for integration test setup.
/// </summary>
public class CoreTestDataBuilder
{
    private readonly CoreDbContext _dbContext;

    public CoreTestDataBuilder(CoreDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Company> CreateCompanyAsync(
        string code = "COMP001",
        string companyName = "Test Company",
        string legalName = "Test Company Legal")
    {
        var company = new Company
        {
            Id = Guid.NewGuid(),
            Slug = code.ToLowerInvariant(),
            Code = code,
            CompanyName = companyName,
            LegalName = legalName,
            RegistrationNumber = "REG123456",
            BaseCurrencyCode = "USD",
            Address = new Address
            {
                StreetAddress = "123 Test Street",
                City = "Test City",
                State = "Test State",
                PostalCode = "12345"
            },
            PhoneNumber = "+1234567890",
            MobileNumber = "+1234567890",
            ContactPerson = "Test Contact",
            Email = $"{code.ToLower()}@example.com",
            WebsiteUrl = "https://example.com",
            CompanyLogo = new byte[] { 1, 2, 3 },
            Latitude = 40.7128m,
            Longitude = -74.0060m,
            RadiusInMeters = 100,
            IsActive = true,
            IsDeleted = false,
            FiscalYearStartMonth = 1,
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = Guid.NewGuid()
        };

        _dbContext.Companies.Add(company);
        await _dbContext.SaveChangesAsync();
        return company;
    }

    public async Task<Branch> CreateBranchAsync(
        Guid companyId,
        string code = "BR001",
        string name = "Test Branch")
    {
        var branch = new Branch
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            Code = code,
            Name = name,
            BranchType = "Headquarters",
            PhoneNumber = "+1234567890",
            Email = $"{code.ToLower()}@example.com",
            ManagerName = "Test Manager",
            BranchLogo = new byte[] { 1, 2, 3 },
            Address = new Address
            {
                StreetAddress = "123 Test Street",
                City = "Test City",
                State = "Test State",
                PostalCode = "12345"
            },
            Latitude = 40.7128m,
            Longitude = -74.0060m,
            IsActive = true,
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = Guid.NewGuid(),
            BusinessUnits = new List<BusinessUnit>()
        };

        _dbContext.Branches.Add(branch);
        await _dbContext.SaveChangesAsync();
        return branch;
    }

    public async Task<BusinessUnit> CreateBusinessUnitAsync(
        Guid companyId,
        Guid branchId,
        string code = "BU001",
        string name = "Test Business Unit")
    {
        var businessUnit = new BusinessUnit
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            BranchId = branchId,
            Code = code,
            Name = name,
            UnitType = "Department",
            Description = "Test business unit description",
            ManagerName = "Test Manager",
            ManagerEmail = $"{code.ToLower()}@example.com",
            IsActive = true,
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = Guid.NewGuid()
        };

        _dbContext.BusinessUnits.Add(businessUnit);
        await _dbContext.SaveChangesAsync();
        return businessUnit;
    }
}
