using Core.Domain.Entities;
using Core.Domain.Interfaces;
using Moq;

namespace Core.Tests.Unit.Mocks;

/// <summary>
/// Common mocks for Core module unit tests
/// </summary>
public static class CoreMockFactory
{
    public static Mock<IRepository<Company>> CreateCompanyRepositoryMock()
    {
        var mock = new Mock<IRepository<Company>>();
        
        var companies = new List<Company>
        {
            new CompanyBuilder()
                .WithCode("COMP001")
                .WithCompanyName("Company 1")
                .WithIsActive(true)
                .Build(),
            new CompanyBuilder()
                .WithCode("COMP002")
                .WithCompanyName("Company 2")
                .WithIsActive(true)
                .Build()
        };

        mock.Setup(x => x.GetAllAsync())
            .ReturnsAsync(companies);

        mock.Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
            .Returns((Guid id) => Task.FromResult(companies.FirstOrDefault(c => c.Id == id)));

        return mock;
    }

    public static Company CreateValidCompany()
    {
        return new CompanyBuilder()
            .WithCode("COMP001")
            .WithCompanyName("Valid Company")
            .WithLegalName("Valid Company Legal Name")
            .WithIsActive(true)
            .Build();
    }

    public static Company CreateInactiveCompany()
    {
        return new CompanyBuilder()
            .WithCode("COMP002")
            .WithCompanyName("Inactive Company")
            .WithIsActive(false)
            .Build();
    }

    public static Branch CreateValidBranch()
    {
        return new BranchBuilder()
            .WithCode("BR001")
            .WithName("Main Branch")
            .WithIsActive(true)
            .Build();
    }

    public static BusinessUnit CreateValidBusinessUnit()
    {
        return new BusinessUnitBuilder()
            .WithCode("BU001")
            .WithName("Main Business Unit")
            .WithIsActive(true)
            .Build();
    }
}
