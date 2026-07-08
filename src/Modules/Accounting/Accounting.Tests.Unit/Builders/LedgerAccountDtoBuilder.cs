using Accounting.Application.DTOs;

namespace Accounting.Tests.Unit.Builders;

/// <summary>
/// Builder for creating LedgerAccount test data
/// </summary>
public class LedgerAccountDtoBuilder
{
    private Guid _companyId = Guid.NewGuid();
    private string _accountNumber = "1000";
    private string _accountName = "Cash";
    private Guid _categoryId = Guid.NewGuid();
    private Guid? _parentAccountId = null;
    private bool _isPostingAllowed = true;
    private bool _isControlAccount = false;
    private string? _currencyCode = "USD";
    private bool _allowManualEntry = true;
    private bool _isActive = true;
    private string? _description = "Test Account";

    public LedgerAccountDtoBuilder WithCompanyId(Guid companyId)
    {
        _companyId = companyId;
        return this;
    }

    public LedgerAccountDtoBuilder WithAccountNumber(string accountNumber)
    {
        _accountNumber = accountNumber;
        return this;
    }

    public LedgerAccountDtoBuilder WithAccountName(string accountName)
    {
        _accountName = accountName;
        return this;
    }

    public LedgerAccountDtoBuilder WithCategoryId(Guid categoryId)
    {
        _categoryId = categoryId;
        return this;
    }

    public LedgerAccountDtoBuilder WithParentAccountId(Guid? parentAccountId)
    {
        _parentAccountId = parentAccountId;
        return this;
    }

    public LedgerAccountDtoBuilder WithIsPostingAllowed(bool isPostingAllowed)
    {
        _isPostingAllowed = isPostingAllowed;
        return this;
    }

    public LedgerAccountDtoBuilder WithIsControlAccount(bool isControlAccount)
    {
        _isControlAccount = isControlAccount;
        return this;
    }

    public LedgerAccountDtoBuilder WithIsActive(bool isActive)
    {
        _isActive = isActive;
        return this;
    }

    public LedgerAccountDtoBuilder WithDescription(string? description)
    {
        _description = description;
        return this;
    }

    public LedgerAccountDto Build()
    {
        return new LedgerAccountDto
        {
            Id = Guid.NewGuid(),
            CompanyId = _companyId,
            AccountNumber = _accountNumber,
            AccountName = _accountName,
            CategoryId = _categoryId,
            ParentAccountId = _parentAccountId,
            IsPostingAllowed = _isPostingAllowed,
            IsControlAccount = _isControlAccount,
            CurrencyCode = _currencyCode,
            AllowManualEntry = _allowManualEntry,
            IsActive = _isActive,
            Description = _description
        };
    }
}
