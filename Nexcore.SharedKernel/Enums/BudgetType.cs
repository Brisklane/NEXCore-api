namespace Nexcore.SharedKernel.Enums;

/// <summary>Distinguishes budget versions used for financial planning across modules.</summary>
public enum BudgetType
{
    Annual = 1,       // main yearly budget
    Revised = 2,      // adjusted version of the annual budget
    Rolling = 3,      // continuously updated forecast
    Forecast = 4,     // projection-based
    Departmental = 5,
    Project = 6
}
