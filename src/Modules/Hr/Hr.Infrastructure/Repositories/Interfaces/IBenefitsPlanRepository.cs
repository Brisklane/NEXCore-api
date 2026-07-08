using Hr.Domain.Entities;
using Nexcore.SharedKernel.Repository;

namespace Hr.Infrastructure.Repositories.Interfaces;

public interface IBenefitsPlanRepository : IRepository<BenefitsPlan>
{
    Task<IEnumerable<BenefitsPlan>> GetAllByTenantAsync();
}
