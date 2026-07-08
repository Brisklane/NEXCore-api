namespace Accounting.Domain.Entities;

/// <summary>Lifecycle state of a journal entry through the multi-level approval and posting workflow.</summary>
public enum JournalEntryStatus
{
    Draft = 1,                 // editable; can be modified or deleted
    Submitted = 2,             // awaiting first-level approval; locked to the submitter
    ApprovedByManager = 3,     // first level cleared; awaiting finance or ready to post
    ApprovedByFinance = 4,     // finance cleared; ready to post to the GL
    Posted = 5,                // in the GL and financial statements; reversal-only
    ReversalPending = 6,       // original still active while a reversal is prepared
    Reversed = 7,              // a reversal entry has been created against it
    Rejected = 8,              // rejected in approval; returns to Draft or can be deleted
    OnHold = 9,                // temporarily blocked from posting
    EscalatedForApproval = 10, // routed to higher authority (unusual/high-value)
    Cancelled = 11             // cancelled before posting
}
