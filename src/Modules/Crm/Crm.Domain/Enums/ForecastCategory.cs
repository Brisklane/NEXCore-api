namespace Crm.Domain.Enums
{
    /// <summary>
    /// Forecast category aligned with Salesforce/Dynamics forecast models
    /// </summary>
    public enum ForecastCategory
    {
        Omitted = 0,
        Pipeline = 1,
        BestCase = 2,
        Commit = 3,
        Closed = 4
    }
}
