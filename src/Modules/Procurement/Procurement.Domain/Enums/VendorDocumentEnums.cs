namespace Procurement.Domain.Enums;

public enum VendorDocumentType
{
    TradeLicense,
    TaxRegistration,
    VATCertificate,
    ISO9001,
    ISO14001,
    ISO45001,
    QualityCertificate,
    InsuranceCertificate,
    BankGuarantee,
    PerformanceBond,
    CompanyRegistration,
    AuditedAccounts,
    Other
}

public enum VendorDocumentStatus
{
    Active,
    Expired,
    PendingRenewal,
    Revoked
}
