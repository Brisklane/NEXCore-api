using Nexcore.SharedKernel;

namespace Crm.Domain.Entities
{
    public class CampaignMember : BaseEntity
    {
        public Guid CampaignId { get; set; }
        public Campaign Campaign { get; set; } = null!;

        // Polymorphic - Lead or Contact
        public Guid? LeadId { get; set; }
        public Lead? Lead { get; set; }

        public Guid? ContactId { get; set; }
        public Contact? Contact { get; set; }

        public string? Status { get; set; }  // Sent, Responded, etc.
        public DateTime? FirstRespondedDate { get; set; }
    }
}