using Accounting.Application.DTOs;

namespace Accounting.Tests.Unit.Builders;

/// <summary>
/// Builder for creating FiscalCalendar test data
/// </summary>
public class FiscalCalendarDtoBuilder
{
    private Guid _companyId = Guid.NewGuid();
    private string _name = "Test Calendar";
    private DateTime _startDate = new DateTime(2024, 1, 1);
    private DateTime _endDate = new DateTime(2024, 12, 31);
    private bool _isActive = true;
    private string? _description = "Test Calendar Description";

    public FiscalCalendarDtoBuilder WithCompanyId(Guid companyId)
    {
        _companyId = companyId;
        return this;
    }

    public FiscalCalendarDtoBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    public FiscalCalendarDtoBuilder WithStartDate(DateTime startDate)
    {
        _startDate = startDate;
        return this;
    }

    public FiscalCalendarDtoBuilder WithEndDate(DateTime endDate)
    {
        _endDate = endDate;
        return this;
    }

    public FiscalCalendarDtoBuilder WithIsActive(bool isActive)
    {
        _isActive = isActive;
        return this;
    }

    public FiscalCalendarDtoBuilder WithDescription(string? description)
    {
        _description = description;
        return this;
    }

    public FiscalCalendarDto Build()
    {
        return new FiscalCalendarDto
        {
            Id = Guid.NewGuid(),
            CompanyId = _companyId,
            Name = _name,
            StartDate = _startDate,
            EndDate = _endDate,
            IsActive = _isActive,
            Description = _description
        };
    }
}
