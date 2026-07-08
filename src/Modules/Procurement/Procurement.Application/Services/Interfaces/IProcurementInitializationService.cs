namespace Procurement.Application.Services.Interfaces;

public interface IProcurementInitializationService
{
    Task InitializeAsync(Guid companyId);
}
