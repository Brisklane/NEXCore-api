using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Nexcore.SharedKernel.Api;
using Nexcore.SharedKernel.Helpers;
using Procurement.Application.DTOs;
using Procurement.Application.Services.Interfaces;
using Procurement.Domain.Entities;
using Procurement.Domain.Enums;
using Procurement.Infrastructure.Persistence;
using Procurement.Infrastructure.Repositories.Interfaces;

namespace Procurement.Infrastructure.Services;

public class VendorService : IVendorService
{
    private readonly IVendorRepository _vendors;
    private readonly IDocumentSequenceService _sequences;
    private readonly ProcurementDbContext _ctx;
    private readonly ILogger<VendorService> _logger;

    public VendorService(
        IVendorRepository vendors,
        IDocumentSequenceService sequences,
        ProcurementDbContext ctx,
        ILogger<VendorService> logger)
    {
        _vendors   = vendors;
        _sequences = sequences;
        _ctx       = ctx;
        _logger    = logger;
    }

    public async Task<PaginatedResponse<VendorDto>> GetAllAsync(PaginationParams pagination)
    {
        var search = pagination.SearchTerm?.Trim().ToLower();
        var (items, total) = await _vendors.GetPagedAsync(
            pagination.PageNumber, pagination.PageSize,
            predicate: v =>
                (string.IsNullOrEmpty(search) ||
                    v.Name.ToLower().Contains(search) ||
                    v.VendorNumber.ToLower().Contains(search) ||
                    (v.PrimaryEmail != null && v.PrimaryEmail.ToLower().Contains(search))) &&
                (pagination.Status == null || (int)v.Status == pagination.Status),
            orderBy: q => q.ApplyOrderNewestFirst(pagination.SortBy,
                string.IsNullOrWhiteSpace(pagination.SortBy) ? "asc" : pagination.SortDirection, "Name"));
        return PaginatedResponse<VendorDto>.Ok(items.Select(MapToDto), total, pagination.PageNumber, pagination.PageSize);
    }

    public async Task<VendorDto?> GetByIdAsync(Guid id)
    {
        var vendor = await _vendors.GetWithFullDetailsAsync(id);
        return vendor is null ? null : MapToDto(vendor);
    }

    public async Task<VendorDto?> GetByNumberAsync(string vendorNumber)
    {
        var vendor = await _vendors.GetByNumberAsync(vendorNumber);
        return vendor is null ? null : MapToDto(vendor);
    }

    public async Task<List<VendorDto>> GetByStatusAsync(VendorStatus status)
        => (await _vendors.GetByStatusAsync(status)).Select(MapToDto).ToList();

    public async Task<List<VendorDto>> GetByOnboardingStatusAsync(VendorOnboardingStatus status)
        => (await _vendors.GetByOnboardingStatusAsync(status)).Select(MapToDto).ToList();

    public async Task<List<VendorDto>> GetPreferredVendorsAsync()
        => (await _vendors.GetPreferredVendorsAsync()).Select(MapToDto).ToList();

    public async Task<List<VendorDto>> GetByCategoryAsync(Guid categoryId)
        => (await _vendors.GetByCategoryAsync(categoryId)).Select(MapToDto).ToList();

    public async Task<VendorDto> CreateAsync(CreateVendorDto dto)
    {
        var vendor = new Vendor
        {
            VendorNumber              = await _sequences.GetNextNumberAsync(ProcurementDocumentType.Vendor),
            Name                      = dto.Name,
            ShortName                 = dto.ShortName,
            Type                      = dto.Type,
            TaxRegistrationNumber     = dto.TaxRegistrationNumber,
            CompanyRegistrationNumber = dto.CompanyRegistrationNumber,
            VATNumber                 = dto.VATNumber,
            Website                   = dto.Website,
            VendorCategoryId          = dto.VendorCategoryId,
            PrimaryEmail              = dto.PrimaryEmail,
            PrimaryPhone              = dto.PrimaryPhone,
            PrimaryMobile             = dto.PrimaryMobile,
            CurrencyCode              = dto.CurrencyCode,
            PaymentTerms              = dto.PaymentTerms,
            LeadTimeDays              = dto.LeadTimeDays,
            CreditLimit               = dto.CreditLimit,
            IsPreferredVendor         = dto.IsPreferredVendor,
            ApLedgerAccountId         = dto.ApLedgerAccountId,
            Notes                     = dto.Notes,
            InternalNotes             = dto.InternalNotes,
            Status                    = VendorStatus.PendingApproval,
            OnboardingStatus          = VendorOnboardingStatus.Draft
        };

        await _vendors.AddAsync(vendor);
        await _vendors.SaveChangesAsync();
        return MapToDto(vendor);
    }

    public async Task<VendorDto> UpdateAsync(Guid id, UpdateVendorDto dto)
    {
        var vendor = await _vendors.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Vendor {id} not found");

        if (dto.Name is not null) vendor.Name = dto.Name;
        if (dto.ShortName is not null) vendor.ShortName = dto.ShortName;
        if (dto.Type is not null) vendor.Type = dto.Type.Value;
        if (dto.TaxRegistrationNumber is not null) vendor.TaxRegistrationNumber = dto.TaxRegistrationNumber;
        if (dto.CompanyRegistrationNumber is not null) vendor.CompanyRegistrationNumber = dto.CompanyRegistrationNumber;
        if (dto.VATNumber is not null) vendor.VATNumber = dto.VATNumber;
        if (dto.Website is not null) vendor.Website = dto.Website;
        if (dto.VendorCategoryId is not null) vendor.VendorCategoryId = dto.VendorCategoryId;
        if (dto.PrimaryEmail is not null) vendor.PrimaryEmail = dto.PrimaryEmail;
        if (dto.PrimaryPhone is not null) vendor.PrimaryPhone = dto.PrimaryPhone;
        if (dto.PrimaryMobile is not null) vendor.PrimaryMobile = dto.PrimaryMobile;
        if (dto.CurrencyCode is not null) vendor.CurrencyCode = dto.CurrencyCode;
        if (dto.PaymentTerms is not null) vendor.PaymentTerms = dto.PaymentTerms.Value;
        if (dto.LeadTimeDays is not null) vendor.LeadTimeDays = dto.LeadTimeDays.Value;
        if (dto.CreditLimit is not null) vendor.CreditLimit = dto.CreditLimit.Value;
        if (dto.IsPreferredVendor is not null) vendor.IsPreferredVendor = dto.IsPreferredVendor.Value;
        if (dto.ApLedgerAccountId is not null) vendor.ApLedgerAccountId = dto.ApLedgerAccountId;
        if (dto.Notes is not null) vendor.Notes = dto.Notes;
        if (dto.InternalNotes is not null) vendor.InternalNotes = dto.InternalNotes;

        _vendors.Update(vendor);
        await _vendors.SaveChangesAsync();
        return MapToDto(vendor);
    }

    public async Task<VendorDto> ApproveAsync(Guid id, Guid approvedByUserId)
    {
        var vendor = await _vendors.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Vendor {id} not found");

        vendor.Status            = VendorStatus.Active;
        vendor.OnboardingStatus  = VendorOnboardingStatus.Approved;

        _vendors.Update(vendor);
        await _vendors.SaveChangesAsync();
        _logger.LogInformation("Vendor {VendorNumber} approved by {UserId}", vendor.VendorNumber, approvedByUserId);
        return MapToDto(vendor);
    }

    public async Task<VendorDto> DeactivateAsync(Guid id)
    {
        var vendor = await _vendors.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Vendor {id} not found");

        if (vendor.Status is not (VendorStatus.Active or VendorStatus.Blocked))
            throw new InvalidOperationException("Only an active or blocked vendor can be deactivated.");

        vendor.Status    = VendorStatus.Inactive;
        vendor.IsBlocked = false;

        _vendors.Update(vendor);
        await _vendors.SaveChangesAsync();
        _logger.LogInformation("Vendor {VendorNumber} deactivated", vendor.VendorNumber);
        return MapToDto(vendor);
    }

    public async Task<VendorDto> ReactivateAsync(Guid id)
    {
        var vendor = await _vendors.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Vendor {id} not found");

        if (vendor.Status != VendorStatus.Inactive)
            throw new InvalidOperationException("Only an inactive vendor can be reactivated.");

        vendor.Status = VendorStatus.Active;

        _vendors.Update(vendor);
        await _vendors.SaveChangesAsync();
        _logger.LogInformation("Vendor {VendorNumber} reactivated", vendor.VendorNumber);
        return MapToDto(vendor);
    }

    public async Task<VendorDto> BlockAsync(Guid id, BlockVendorDto dto, Guid blockedByUserId)
    {
        var vendor = await _vendors.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Vendor {id} not found");

        vendor.IsBlocked       = true;
        vendor.BlockReason     = dto.BlockReason;
        vendor.BlockedAt       = DateTime.UtcNow;
        vendor.BlockedByUserId = blockedByUserId;
        vendor.Status          = VendorStatus.Blocked;

        _vendors.Update(vendor);
        await _vendors.SaveChangesAsync();
        return MapToDto(vendor);
    }

    public async Task<VendorDto> UnblockAsync(Guid id)
    {
        var vendor = await _vendors.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Vendor {id} not found");

        vendor.IsBlocked       = false;
        vendor.BlockReason     = null;
        vendor.BlockedAt       = null;
        vendor.BlockedByUserId = null;
        vendor.Status          = VendorStatus.Active;

        _vendors.Update(vendor);
        await _vendors.SaveChangesAsync();
        return MapToDto(vendor);
    }

    public async Task DeleteAsync(Guid id)
    {
        var vendor = await _vendors.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Vendor {id} not found");

        // An active or blocked vendor must be deactivated before deletion.
        if (vendor.Status is not (VendorStatus.PendingApproval or VendorStatus.Inactive))
            throw new InvalidOperationException("Only a draft/pending or deactivated vendor can be deleted. Deactivate the vendor first.");

        _vendors.SoftDelete(vendor);
        await _vendors.SaveChangesAsync();
    }

    // ── Contacts ──────────────────────────────────────────────────────────────

    public async Task<VendorContactDto> AddContactAsync(Guid vendorId, VendorContactDto dto)
    {
        if (!await _ctx.Vendors.AnyAsync(v => v.Id == vendorId && !v.IsDeleted))
            throw new KeyNotFoundException($"Vendor {vendorId} not found");

        var existing = await _ctx.VendorContacts.Where(c => c.VendorId == vendorId).ToListAsync();

        // First contact is always primary; only one contact may be primary at a time.
        var makePrimary = dto.IsPrimary || existing.Count == 0;
        if (makePrimary)
            foreach (var c in existing.Where(c => c.IsPrimary)) c.IsPrimary = false;

        var contact = new VendorContact
        {
            VendorId   = vendorId,
            FirstName  = dto.FirstName,
            LastName   = dto.LastName,
            JobTitle   = dto.JobTitle,
            Email      = dto.Email,
            Phone      = dto.Phone,
            Mobile     = dto.Mobile,
            IsPrimary  = makePrimary
        };

        // Insert the child directly as an Added entity (the proven pattern in this module).
        // Loading + re-saving the whole vendor aggregate mis-states the new child and throws
        // a phantom concurrency error.
        _ctx.VendorContacts.Add(contact);
        await _ctx.SaveChangesAsync();
        dto.Id = contact.Id;
        dto.IsPrimary = makePrimary;
        return dto;
    }

    public async Task<VendorContactDto> UpdateContactAsync(Guid vendorId, Guid contactId, VendorContactDto dto)
    {
        var vendor = await _vendors.GetWithFullDetailsAsync(vendorId)
            ?? throw new KeyNotFoundException($"Vendor {vendorId} not found");

        var contact = vendor.Contacts.FirstOrDefault(c => c.Id == contactId)
            ?? throw new KeyNotFoundException($"Contact {contactId} not found");

        contact.FirstName = dto.FirstName;
        contact.LastName  = dto.LastName;
        contact.JobTitle  = dto.JobTitle;
        contact.Email     = dto.Email;
        contact.Phone     = dto.Phone;
        contact.Mobile    = dto.Mobile;

        if (dto.IsPrimary)
        {
            foreach (var c in vendor.Contacts) c.IsPrimary = false;
            contact.IsPrimary = true;
        }

        _vendors.Update(vendor);
        await _vendors.SaveChangesAsync();
        return dto;
    }

    public async Task DeleteContactAsync(Guid vendorId, Guid contactId)
    {
        var vendor = await _vendors.GetWithFullDetailsAsync(vendorId)
            ?? throw new KeyNotFoundException($"Vendor {vendorId} not found");

        var contact = vendor.Contacts.FirstOrDefault(c => c.Id == contactId)
            ?? throw new KeyNotFoundException($"Contact {contactId} not found");

        vendor.Contacts.Remove(contact);
        _vendors.Update(vendor);
        await _vendors.SaveChangesAsync();
    }

    // ── Addresses ─────────────────────────────────────────────────────────────

    public async Task<VendorAddressDto> AddAddressAsync(Guid vendorId, VendorAddressDto dto)
    {
        if (!await _ctx.Vendors.AnyAsync(v => v.Id == vendorId && !v.IsDeleted))
            throw new KeyNotFoundException($"Vendor {vendorId} not found");

        var existing = await _ctx.VendorAddresses.Where(a => a.VendorId == vendorId).ToListAsync();

        // First address of a given type is the default; only one default per type.
        var makeDefault = dto.IsDefault || !existing.Any(a => a.AddressType == dto.AddressType);
        if (makeDefault)
            foreach (var a in existing.Where(a => a.AddressType == dto.AddressType && a.IsPrimary))
                a.IsPrimary = false;

        var address = new VendorAddress
        {
            VendorId    = vendorId,
            AddressType = dto.AddressType,
            Street      = dto.Street ?? string.Empty,
            City        = dto.City,
            State       = dto.State,
            PostalCode  = dto.PostalCode,
            Country     = dto.Country ?? string.Empty,
            IsPrimary   = makeDefault
        };

        _ctx.VendorAddresses.Add(address);
        await _ctx.SaveChangesAsync();
        dto.Id = address.Id;
        dto.IsDefault = makeDefault;
        return dto;
    }

    public async Task<VendorAddressDto> UpdateAddressAsync(Guid vendorId, Guid addressId, VendorAddressDto dto)
    {
        var vendor = await _vendors.GetWithFullDetailsAsync(vendorId)
            ?? throw new KeyNotFoundException($"Vendor {vendorId} not found");

        var address = vendor.Addresses.FirstOrDefault(a => a.Id == addressId)
            ?? throw new KeyNotFoundException($"Address {addressId} not found");

        address.AddressType = dto.AddressType;
        address.Street      = dto.Street!;
        address.City        = dto.City;
        address.State       = dto.State;
        address.PostalCode  = dto.PostalCode;
        address.Country     = dto.Country!;

        if (dto.IsDefault)
        {
            foreach (var a in vendor.Addresses.Where(a => a.AddressType == dto.AddressType))
                a.IsPrimary = false;
            address.IsPrimary = true;
        }

        _vendors.Update(vendor);
        await _vendors.SaveChangesAsync();
        return dto;
    }

    public async Task DeleteAddressAsync(Guid vendorId, Guid addressId)
    {
        var vendor = await _vendors.GetWithFullDetailsAsync(vendorId)
            ?? throw new KeyNotFoundException($"Vendor {vendorId} not found");

        var address = vendor.Addresses.FirstOrDefault(a => a.Id == addressId)
            ?? throw new KeyNotFoundException($"Address {addressId} not found");

        vendor.Addresses.Remove(address);
        _vendors.Update(vendor);
        await _vendors.SaveChangesAsync();
    }

    // ── Bank Accounts ─────────────────────────────────────────────────────────

    public async Task<VendorBankAccountDto> AddBankAccountAsync(Guid vendorId, VendorBankAccountDto dto)
    {
        if (!await _ctx.Vendors.AnyAsync(v => v.Id == vendorId && !v.IsDeleted))
            throw new KeyNotFoundException($"Vendor {vendorId} not found");

        var existing = await _ctx.VendorBankAccounts.Where(b => b.VendorId == vendorId).ToListAsync();

        // First bank account is the default; only one default at a time.
        var makeDefault = dto.IsDefault || existing.Count == 0;
        if (makeDefault)
            foreach (var b in existing.Where(b => b.IsDefault)) b.IsDefault = false;

        var account = new VendorBankAccount
        {
            VendorId          = vendorId,
            AccountHolderName = dto.AccountName,
            AccountNumber     = dto.AccountNumber,
            BankName          = dto.BankName ?? string.Empty,
            BranchName        = dto.BranchName,
            IBAN              = dto.IBAN,
            SWIFTCode         = dto.SwiftCode,
            CurrencyCode      = dto.CurrencyCode,
            IsDefault         = makeDefault,
            IsActive          = dto.IsActive
        };

        _ctx.VendorBankAccounts.Add(account);
        await _ctx.SaveChangesAsync();
        dto.Id = account.Id;
        dto.IsDefault = makeDefault;
        return dto;
    }

    public async Task<VendorBankAccountDto> UpdateBankAccountAsync(Guid vendorId, Guid bankAccountId, VendorBankAccountDto dto)
    {
        var vendor = await _vendors.GetWithFullDetailsAsync(vendorId)
            ?? throw new KeyNotFoundException($"Vendor {vendorId} not found");

        var account = vendor.BankAccounts.FirstOrDefault(b => b.Id == bankAccountId)
            ?? throw new KeyNotFoundException($"Bank account {bankAccountId} not found");

        account.AccountHolderName = dto.AccountName;
        account.AccountNumber     = dto.AccountNumber;
        account.BankName          = dto.BankName ?? account.BankName;
        account.BranchName        = dto.BranchName;
        account.IBAN              = dto.IBAN;
        account.SWIFTCode         = dto.SwiftCode;
        account.CurrencyCode      = dto.CurrencyCode;
        account.IsActive          = dto.IsActive;

        if (dto.IsDefault)
        {
            foreach (var b in vendor.BankAccounts) b.IsDefault = false;
            account.IsDefault = true;
        }

        _vendors.Update(vendor);
        await _vendors.SaveChangesAsync();
        return dto;
    }

    public async Task DeleteBankAccountAsync(Guid vendorId, Guid bankAccountId)
    {
        var vendor = await _vendors.GetWithFullDetailsAsync(vendorId)
            ?? throw new KeyNotFoundException($"Vendor {vendorId} not found");

        var account = vendor.BankAccounts.FirstOrDefault(b => b.Id == bankAccountId)
            ?? throw new KeyNotFoundException($"Bank account {bankAccountId} not found");

        vendor.BankAccounts.Remove(account);
        _vendors.Update(vendor);
        await _vendors.SaveChangesAsync();
    }

    // ── Mapping ───────────────────────────────────────────────────────────────

    private static VendorDto MapToDto(Vendor v) => new()
    {
        Id                        = v.Id,
        VendorNumber              = v.VendorNumber,
        Name                      = v.Name,
        ShortName                 = v.ShortName,
        Type                      = v.Type,
        Status                    = v.Status,
        OnboardingStatus          = v.OnboardingStatus,
        TaxRegistrationNumber     = v.TaxRegistrationNumber,
        CompanyRegistrationNumber = v.CompanyRegistrationNumber,
        VATNumber                 = v.VATNumber,
        Website                   = v.Website,
        VendorCategoryId          = v.VendorCategoryId,
        VendorCategoryName        = v.VendorCategory?.Name,
        PrimaryEmail              = v.PrimaryEmail,
        PrimaryPhone              = v.PrimaryPhone,
        PrimaryMobile             = v.PrimaryMobile,
        CurrencyCode              = v.CurrencyCode,
        PaymentTerms              = v.PaymentTerms,
        LeadTimeDays              = v.LeadTimeDays,
        CreditLimit               = v.CreditLimit,
        IsPreferredVendor         = v.IsPreferredVendor,
        ApLedgerAccountId         = v.ApLedgerAccountId,
        SubledgerType             = v.SubledgerType,
        OverallRating             = v.OverallRating,
        OnTimeDeliveryRate        = v.OnTimeDeliveryRate,
        QualityScore              = v.QualityScore,
        IsBlocked                 = v.IsBlocked,
        BlockReason               = v.BlockReason,
        BlockedAt                 = v.BlockedAt,
        Notes                     = v.Notes,
        InternalNotes             = v.InternalNotes,
        Contacts = v.Contacts.Select(c => new VendorContactDto
        {
            Id        = c.Id,
            FirstName = c.FirstName,
            LastName  = c.LastName,
            JobTitle  = c.JobTitle,
            Email     = c.Email,
            Phone     = c.Phone,
            Mobile    = c.Mobile,
            IsPrimary = c.IsPrimary
        }).ToList(),
        Addresses = v.Addresses.Select(a => new VendorAddressDto
        {
            Id          = a.Id,
            AddressType = a.AddressType,
            Street      = a.Street,
            City        = a.City,
            State       = a.State,
            PostalCode  = a.PostalCode,
            Country     = a.Country,
            IsDefault   = a.IsPrimary
        }).ToList(),
        BankAccounts = v.BankAccounts.Select(b => new VendorBankAccountDto
        {
            Id            = b.Id,
            AccountName   = b.AccountHolderName,
            AccountNumber = b.AccountNumber,
            BankName      = b.BankName,
            BranchName    = b.BranchName,
            IBAN          = b.IBAN,
            SwiftCode     = b.SWIFTCode,
            CurrencyCode  = b.CurrencyCode,
            IsDefault     = b.IsDefault,
            IsActive      = b.IsActive
        }).ToList()
    };
}
