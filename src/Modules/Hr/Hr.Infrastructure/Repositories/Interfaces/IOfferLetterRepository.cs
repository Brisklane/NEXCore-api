using Hr.Domain.Entities;
using Nexcore.SharedKernel.Repository;

namespace Hr.Infrastructure.Repositories.Interfaces;

public interface IOfferLetterRepository : IRepository<OfferLetter>
{
    Task<IEnumerable<OfferLetter>> GetAllByTenantAsync();
    Task<IEnumerable<OfferLetter>> GetByApplicationIdAsync(Guid applicationId);
}
