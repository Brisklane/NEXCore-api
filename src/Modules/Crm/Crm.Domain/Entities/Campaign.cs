using Crm.Domain.Enums;
using Nexcore.SharedKernel;

namespace Crm.Domain.Entities
{
    public class Campaign : BaseEntity
    {
        // ─── Basic Info ────────────────────────────────────────────────
        public string CampaignName { get; set; } = string.Empty;  // Required
        public bool Active { get; set; }
        public string Status { get; set; } = "Planned";  // Dropdown: Planned, In Progress, Completed, Aborted
        public string? Type { get; set; }  // Dropdown: Advertisement, Banner Ads, Direct Mail, Email, Telemarketing, Webinar, Conference, Trade Show, Public Relations, Partners, Referral Program, Other

        // ─── Hierarchy ─────────────────────────────────────────────────
        public Guid? ParentCampaignId { get; set; }
        public Campaign? ParentCampaign { get; set; }

        // ─── Planning ──────────────────────────────────────────────────
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public decimal? ExpectedRevenue { get; set; }
        public decimal? BudgetedCost { get; set; }
        public decimal? ActualCost { get; set; }
        public int? NumSent { get; set; }
        public decimal? ExpectedResponsePercent { get; set; }

        // ─── Ownership ─────────────────────────────────────────────────
        public Guid? OwnerId { get; set; }

        // ─── Navigation ────────────────────────────────────────────────
        public ICollection<Campaign> ChildCampaigns { get; set; } = new List<Campaign>();
        public ICollection<CampaignMember> Members { get; set; } = new List<CampaignMember>();
    }
}