using Hr.Domain.Entities;
using Nexcore.SharedKernel.Repository;

namespace Hr.Infrastructure.Repositories.Interfaces;

public interface ITalentPoolRepository : IRepository<TalentPool>
{
    Task<IEnumerable<TalentPool>> GetAllByTenantAsync();
}
