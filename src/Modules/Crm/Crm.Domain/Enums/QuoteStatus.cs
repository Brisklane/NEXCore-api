namespace Crm.Domain.Enums
{
    /// <summary>
    /// Status of a quote
    /// </summary>
    public enum QuoteStatus
    {
        Draft = 0,
        NeedsReview = 1,
        InReview = 2,
        Approved = 3,
        Rejected = 4,
        Presented = 5,
        Accepted = 6,
        Denied = 7
    }
}
