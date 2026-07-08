using Nexcore.SharedKernel;

namespace Crm.Domain.Entities
{
    /// <summary>
    /// A static or dynamic list of contacts/leads for marketing use.
    /// Aligned with HubSpot Contact List, Dynamics Marketing List, Salesforce Campaign member list.
    /// </summary>
    public class ContactList : BaseEntity
    {
        public string ListName { get; set; } = string.Empty;
        public bool IsDynamic { get; set; }
        public string? FilterCriteria { get; set; }
        public Guid? OwnerId { get; set; }

        public ICollection<ContactListMember> Members { get; set; } = new List<ContactListMember>();
    }
}
