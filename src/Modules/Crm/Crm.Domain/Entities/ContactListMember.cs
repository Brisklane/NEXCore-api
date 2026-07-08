using Nexcore.SharedKernel;

namespace Crm.Domain.Entities
{
    /// <summary>
    /// A member (Contact or Lead) of a ContactList.
    /// </summary>
    public class ContactListMember : BaseEntity
    {
        public Guid ContactListId { get; set; }
        public ContactList ContactList { get; set; } = null!;

        public Guid? ContactId { get; set; }
        public Contact? Contact { get; set; }

        public Guid? LeadId { get; set; }
        public Lead? Lead { get; set; }

        public string MemberType { get; set; } = "Contact";  // "Contact" or "Lead"
    }
}
