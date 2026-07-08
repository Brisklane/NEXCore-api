using Core.Domain.Entities;
using Nexcore.SharedKernel.ValueObjects;

namespace Core.Tests.Unit.Entities;

public class CompanyEntityTests : CoreUnitTestFixture
{
    [Fact]
    public void Company_WithBuilderDefaults_HasCorrectValues()
    {
        var company = new CompanyBuilder().Build();

        Assert.NotNull(company);
        Assert.Equal("COMP001", company.Code);
        Assert.Equal("Test Company", company.CompanyName);
        Assert.True(company.IsActive);
        Assert.False(company.IsDeleted);
        Assert.Equal(1, company.FiscalYearStartMonth);
    }

    [Fact]
    public void Company_BuiltWithCustomValues_ReflectsThoseValues()
    {
        var id = Guid.NewGuid();
        var company = new CompanyBuilder()
            .WithId(id)
            .WithCode("ACME01")
            .WithCompanyName("Acme Corp")
            .WithLegalName("Acme Corporation Ltd")
            .WithEmail("info@acme.com")
            .WithIsActive(false)
            .Build();

        Assert.Equal(id, company.Id);
        Assert.Equal("ACME01", company.Code);
        Assert.Equal("Acme Corp", company.CompanyName);
        Assert.Equal("Acme Corporation Ltd", company.LegalName);
        Assert.Equal("info@acme.com", company.Email);
        Assert.False(company.IsActive);
    }

    [Fact]
    public void Company_CanBeDeactivated()
    {
        var company = new CompanyBuilder().WithIsActive(true).Build();
        company.IsActive = false;
        Assert.False(company.IsActive);
    }

    [Fact]
    public void Company_CanBeSoftDeleted()
    {
        var company = new CompanyBuilder().Build();
        var userId = Guid.NewGuid();

        company.IsDeleted = true;
        company.DeletedAt = DateTime.UtcNow;
        company.DeletedByUserId = userId;

        Assert.True(company.IsDeleted);
        Assert.NotNull(company.DeletedAt);
        Assert.Equal(userId, company.DeletedByUserId);
    }

    [Fact]
    public void Company_HasEmptyCollectionsByDefault()
    {
        var company = new CompanyBuilder().Build();

        Assert.NotNull(company.Branches);
        Assert.NotNull(company.BusinessUnits);
        Assert.Empty(company.Branches);
        Assert.Empty(company.BusinessUnits);
    }

    [Fact]
    public void Company_HasAddressSet()
    {
        var address = new Address("1 Test St", "Boston", "MA", "02101");
        var company = new CompanyBuilder().WithAddress(address).Build();

        Assert.NotNull(company.Address);
        Assert.Equal("1 Test St", company.Address.StreetAddress);
        Assert.Equal("Boston", company.Address.City);
    }

    [Fact]
    public void Company_HasUniqueIdByDefault()
    {
        var company1 = new CompanyBuilder().Build();
        var company2 = new CompanyBuilder().Build();

        Assert.NotEqual(company1.Id, company2.Id);
    }
}

public class BranchEntityTests : CoreUnitTestFixture
{
    [Fact]
    public void Branch_WithBuilderDefaults_HasCorrectValues()
    {
        var branch = new BranchBuilder().Build();

        Assert.NotNull(branch);
        Assert.Equal("BR001", branch.Code);
        Assert.Equal("Main Branch", branch.Name);
        Assert.True(branch.IsActive);
        Assert.False(branch.IsDeleted);
    }

    [Fact]
    public void Branch_BuiltWithCustomValues_ReflectsThoseValues()
    {
        var companyId = Guid.NewGuid();
        var branch = new BranchBuilder()
            .WithCode("NYC01")
            .WithName("New York Branch")
            .WithCompanyId(companyId)
            .WithIsActive(false)
            .Build();

        Assert.Equal("NYC01", branch.Code);
        Assert.Equal("New York Branch", branch.Name);
        Assert.Equal(companyId, branch.CompanyId);
        Assert.False(branch.IsActive);
    }

    [Fact]
    public void Branch_CanBeDeactivated()
    {
        var branch = new BranchBuilder().WithIsActive(true).Build();
        branch.IsActive = false;
        Assert.False(branch.IsActive);
    }

    [Fact]
    public void Branch_CanBeSoftDeleted()
    {
        var branch = new BranchBuilder().Build();
        var userId = Guid.NewGuid();

        branch.IsDeleted = true;
        branch.DeletedAt = DateTime.UtcNow;
        branch.DeletedByUserId = userId;

        Assert.True(branch.IsDeleted);
        Assert.NotNull(branch.DeletedAt);
        Assert.Equal(userId, branch.DeletedByUserId);
    }

    [Fact]
    public void Branch_HasAddressProperty()
    {
        var branch = new BranchBuilder().Build();
        Assert.NotNull(branch.Address);
    }

    [Fact]
    public void Branch_HasEmptyBusinessUnitsCollectionByDefault()
    {
        var branch = new BranchBuilder().Build();
        Assert.NotNull(branch.BusinessUnits);
        Assert.Empty(branch.BusinessUnits);
    }

    [Fact]
    public void Branch_HasUniqueIdByDefault()
    {
        var branch1 = new BranchBuilder().Build();
        var branch2 = new BranchBuilder().Build();
        Assert.NotEqual(branch1.Id, branch2.Id);
    }
}

public class BusinessUnitEntityTests : CoreUnitTestFixture
{
    [Fact]
    public void BusinessUnit_WithBuilderDefaults_HasCorrectValues()
    {
        var bu = new BusinessUnitBuilder().Build();

        Assert.NotNull(bu);
        Assert.Equal("BU001", bu.Code);
        Assert.Equal("Main Business Unit", bu.Name);
        Assert.True(bu.IsActive);
        Assert.False(bu.IsDeleted);
    }

    [Fact]
    public void BusinessUnit_BuiltWithCustomValues_ReflectsThoseValues()
    {
        var companyId = Guid.NewGuid();
        var branchId = Guid.NewGuid();
        var bu = new BusinessUnitBuilder()
            .WithCode("HR001")
            .WithName("HR Department")
            .WithCompanyId(companyId)
            .WithBranchId(branchId)
            .WithUnitType("Department")
            .WithIsActive(false)
            .Build();

        Assert.Equal("HR001", bu.Code);
        Assert.Equal("HR Department", bu.Name);
        Assert.Equal(companyId, bu.CompanyId);
        Assert.Equal(branchId, bu.BranchId);
        Assert.Equal("Department", bu.UnitType);
        Assert.False(bu.IsActive);
    }

    [Fact]
    public void BusinessUnit_CanBeDeactivated()
    {
        var bu = new BusinessUnitBuilder().WithIsActive(true).Build();
        bu.IsActive = false;
        Assert.False(bu.IsActive);
    }

    [Fact]
    public void BusinessUnit_CanBeSoftDeleted()
    {
        var bu = new BusinessUnitBuilder().Build();
        var userId = Guid.NewGuid();

        bu.IsDeleted = true;
        bu.DeletedAt = DateTime.UtcNow;
        bu.DeletedByUserId = userId;

        Assert.True(bu.IsDeleted);
        Assert.NotNull(bu.DeletedAt);
        Assert.Equal(userId, bu.DeletedByUserId);
    }

    [Fact]
    public void BusinessUnit_HasUniqueIdByDefault()
    {
        var bu1 = new BusinessUnitBuilder().Build();
        var bu2 = new BusinessUnitBuilder().Build();
        Assert.NotEqual(bu1.Id, bu2.Id);
    }
}
