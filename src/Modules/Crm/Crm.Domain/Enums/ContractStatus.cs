namespace Crm.Domain.Enums
{
    /// <summary>
    /// Status of a customer contract
    /// </summary>
    public enum ContractStatus
    {
        Draft = 0,
        InApproval = 1,
        Activated = 2,
        Expired = 3,
        Terminated = 4
    }
}
