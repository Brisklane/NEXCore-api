using Nexcore.SharedKernel.Api;
using Procurement.Application.DTOs;

namespace Procurement.Application.Services.Interfaces;

/// <summary>Vendor Debit Notes — AP credit claims raised against a posted purchase return.</summary>
public interface IVendorDebitNoteService
{
    Task<PaginatedResponse<VendorDebitNoteDto>> GetAllAsync(PaginationParams pagination);
    Task<VendorDebitNoteDto?> GetByIdAsync(Guid id);
    Task<List<VendorDebitNoteDto>> GetByVendorAsync(Guid vendorId);
    /// <summary>Posted returns that don't yet have a debit note — eligible sources.</summary>
    Task<List<PurchaseReturnDto>> GetEligibleReturnsAsync();
    Task<VendorDebitNoteDto> CreateAsync(CreateVendorDebitNoteDto dto);
    Task<VendorDebitNoteDto> SendAsync(Guid id);
    Task<VendorDebitNoteDto> AcknowledgeAsync(Guid id);
    Task<VendorDebitNoteDto> SettleAsync(Guid id);
    Task<VendorDebitNoteDto> CancelAsync(Guid id);
    Task DeleteAsync(Guid id);
}
