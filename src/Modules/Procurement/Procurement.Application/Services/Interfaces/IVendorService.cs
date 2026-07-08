using Nexcore.SharedKernel.Api;
using Procurement.Application.DTOs;
using Procurement.Domain.Enums;

namespace Procurement.Application.Services.Interfaces;

public interface IVendorService
{
    Task<PaginatedResponse<VendorDto>> GetAllAsync(PaginationParams pagination);
    Task<VendorDto?> GetByIdAsync(Guid id);
    Task<VendorDto?> GetByNumberAsync(string vendorNumber);
    Task<List<VendorDto>> GetByStatusAsync(VendorStatus status);
    Task<List<VendorDto>> GetByOnboardingStatusAsync(VendorOnboardingStatus status);
    Task<List<VendorDto>> GetPreferredVendorsAsync();
    Task<List<VendorDto>> GetByCategoryAsync(Guid categoryId);
    Task<VendorDto> CreateAsync(CreateVendorDto dto);
    Task<VendorDto> UpdateAsync(Guid id, UpdateVendorDto dto);
    Task<VendorDto> ApproveAsync(Guid id, Guid approvedByUserId);
    Task<VendorDto> DeactivateAsync(Guid id);
    Task<VendorDto> ReactivateAsync(Guid id);
    Task<VendorDto> BlockAsync(Guid id, BlockVendorDto dto, Guid blockedByUserId);
    Task<VendorDto> UnblockAsync(Guid id);
    Task DeleteAsync(Guid id);

    // ── Contacts ──────────────────────────────────────────────────────────────
    Task<VendorContactDto> AddContactAsync(Guid vendorId, VendorContactDto dto);
    Task<VendorContactDto> UpdateContactAsync(Guid vendorId, Guid contactId, VendorContactDto dto);
    Task DeleteContactAsync(Guid vendorId, Guid contactId);

    // ── Addresses ─────────────────────────────────────────────────────────────
    Task<VendorAddressDto> AddAddressAsync(Guid vendorId, VendorAddressDto dto);
    Task<VendorAddressDto> UpdateAddressAsync(Guid vendorId, Guid addressId, VendorAddressDto dto);
    Task DeleteAddressAsync(Guid vendorId, Guid addressId);

    // ── Bank Accounts ─────────────────────────────────────────────────────────
    Task<VendorBankAccountDto> AddBankAccountAsync(Guid vendorId, VendorBankAccountDto dto);
    Task<VendorBankAccountDto> UpdateBankAccountAsync(Guid vendorId, Guid bankAccountId, VendorBankAccountDto dto);
    Task DeleteBankAccountAsync(Guid vendorId, Guid bankAccountId);
}
