using Nexcore.SharedKernel;

namespace Crm.Domain.Entities
{
    public class Attachment : BaseEntity
    {
        public string FileName { get; set; } = string.Empty;
        public string? FileExtension { get; set; }
        public long FileSizeBytes { get; set; }
        public string? ContentType { get; set; }

        public string StoragePath { get; set; } = string.Empty;  // Azure Blob URL or local path

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