using Accounting.Application.DTOs;

namespace Accounting.Tests.Unit.Builders;

/// <summary>
/// Builder for creating FiscalPeriod test data
/// </summary>
public class FiscalPeriodDtoBuilder
{
    private Guid _fiscalCalendarId = Guid.NewGuid();
    private string _periodName = "January";
    private DateTime _startDate = new DateTime(2024, 1, 1);
    private DateTime _endDate = new DateTime(2024, 1, 31);
    private bool _isClosed = false;
    private string? _description = "Test Period";

    public FiscalPeriodDtoBuilder WithFiscalCalendarId(Guid fiscalCalendarId)
    {
        _fiscalCalendarId = fiscalCalendarId;
        return this;
    }

    public FiscalPeriodDtoBuilder WithPeriodName(string periodName)
    {
        _periodName = periodName;
        return this;
    }

    public FiscalPeriodDtoBuilder WithStartDate(DateTime startDate)
    {
        _startDate = startDate;
        return this;
    }

    public FiscalPeriodDtoBuilder WithEndDate(DateTime endDate)
    {
        _endDate = endDate;
        return this;
    }

    public FiscalPeriodDtoBuilder WithIsClosed(bool isClosed)
    {
        _isClosed = isClosed;
        return this;
    }

    public FiscalPeriodDtoBuilder WithDescription(string? description)
    {
        _description = description;
        return this;
    }

    public FiscalPeriodDto Build()
    {
        return new FiscalPeriodDto
        {
            Id = Guid.NewGuid(),
            CompanyId = Guid.NewGuid(),
            FiscalCalendarId = _fiscalCalendarId,
            PeriodName = _periodName,
            StartDate = _startDate,
            EndDate = _endDate,
            IsClosed = _isClosed,
            Description = _description
        };
    }
}
