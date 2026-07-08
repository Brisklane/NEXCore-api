using Nexcore.SharedKernel;

namespace Crm.Domain.Entities
{
    public class Note : BaseEntity
    {
        public string? Title { get; set; }
        public string Body { get; set; } = string.Empty;

        // Polymorphic parent
        public Guid ParentId { get; set; }
        public string ParentType { get; set; } = string.Empty;  // "Lead", "Account", "Contact", "Deal", "Case"

        // Navigation
        public Lead? Lead { get; set; }
        public Account? Account { get; set; }
        public Contact? Contact { get; set; }
        public Deal? Deal { get; set; }
        public Case? Case { get; set; }
    }
}