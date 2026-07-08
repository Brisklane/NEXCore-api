namespace Nexcore.SharedKernel.Enums;

/// <summary>Roles authorised to approve at each level of an approval workflow (roughly ascending authority).</summary>
public enum ApprovalRole
{
    Supervisor = 1,          // first-level / team lead
    Manager = 2,             // department head
    Director = 3,
    FinanceManager = 4,      // controller / finance review
    CFO = 5,                 // Chief Financial Officer
    CEO = 6,                 // Chief Executive Officer
    AccountingManager = 7,
    ComplianceOfficer = 8,
    InternalAuditor = 9,
    SystemAdministrator = 10
}
