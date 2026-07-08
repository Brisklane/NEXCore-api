using Hr.Domain.Entities;
using Nexcore.SharedKernel.Repository;

namespace Hr.Infrastructure.Repositories.Interfaces;

public interface ICommunicationTemplateRepository : IRepository<CommunicationTemplate>
{
    Task<IEnumerable<CommunicationTemplate>> GetAllByTenantAsync();
    Task<CommunicationTemplate?> GetByCodeAsync(string templateCode);
}
