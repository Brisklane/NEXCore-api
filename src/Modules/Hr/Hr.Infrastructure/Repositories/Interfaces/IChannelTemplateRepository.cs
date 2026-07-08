using Hr.Domain.Entities;
using Nexcore.SharedKernel.Repository;

namespace Hr.Infrastructure.Repositories.Interfaces;

public interface IChannelTemplateRepository : IRepository<ChannelTemplate>
{
    Task<IEnumerable<ChannelTemplate>> GetAllByTenantAsync();
}
