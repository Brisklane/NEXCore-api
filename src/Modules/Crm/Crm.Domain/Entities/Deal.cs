using Crm.Domain.Enums;
using Nexcore.SharedKernel;

namespace Crm.Domain.Entities
{
    public class Deal : BaseEntity
    {
        // ─── Basic Info ────────────────────────────────────────────────
        public string OpportunityName { get; set; } = string.Empty;  // Required

        // ─── Account Link ──────────────────────────────────────────────
        public Guid AccountId { get; set; }  // Required - Search Accounts
        public Account Account { get; set; } = null!;

        // ─── Financial ─────────────────────────────────────────────────
        public DateTime CloseDate { get; set; }  // Required
        public decimal? Amount { get; set; }

        // ─── Stage ─────────────────────────────────────────────────────
        public string Stage { get; set; } = "--None--";  // Required dropdown: --None--, Qualify, Meet & Present, Propose, Negotiate, Closed Won, Closed Lost

        // ─── Forecast ──────────────────────────────────────────────────
        public string? ForecastCategory { get; set; }  // Dropdown: --None--, Omitted, Pipeline, Best Case, Commit, Closed

        // ─── Pipeline ──────────────────────────────────────────────────
        public Guid? PipelineId { get; set; }
        public Pipeline? Pipeline { get; set; }

        public Guid? PipelineStageId { get; set; }
        public PipelineStage? PipelineStage { get; set; }

        // ─── Ownership ─────────────────────────────────────────────────
        public Guid? OwnerId { get; set; }

        // ─── Navigation ────────────────────────────────────────────────
        public ICollection<DealProduct> DealProducts { get; set; } = new List<DealProduct>();
        public ICollection<DealContact> DealContacts { get; set; } = new List<DealContact>();
        public ICollection<Activity> Activities { get; set; } = new List<Activity>();
        public ICollection<Note> Notes { get; set; } = new List<Note>();
        public ICollection<Attachment> Attachments { get; set; } = new List<Attachment>();
    }
}