using Hr.Domain.Entities;
using Nexcore.SharedKernel.Repository;

namespace Hr.Infrastructure.Repositories.Interfaces;

public interface ICandidateRepository : IRepository<Candidate>
{
    Task<IEnumerable<Candidate>> GetAllByTenantAsync();
    Task<Candidate?> GetByEmailAsync(string email);
}
