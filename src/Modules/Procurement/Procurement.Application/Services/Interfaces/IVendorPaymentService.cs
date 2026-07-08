using Nexcore.SharedKernel.Api;
using Procurement.Application.DTOs;
using Procurement.Domain.Enums;

namespace Procurement.Application.Services.Interfaces;

public interface IVendorPaymentService
{
    Task<PaginatedResponse<VendorPaymentDto>> GetAllAsync(PaginationParams pagination);
    Task<VendorPaymentDto?> GetByIdAsync(Guid id);
    Task<VendorPaymentDto?> GetByNumberAsync(string paymentNumber);
    Task<List<VendorPaymentDto>> GetByStatusAsync(VendorPaymentStatus status);
    Task<List<VendorPaymentDto>> GetByVendorAsync(Guid vendorId);
    Task<VendorPaymentDto> CreateAsync(CreateVendorPaymentDto dto);
    Task<VendorPaymentDto> UpdateAsync(Guid id, UpdateVendorPaymentDto dto);
    Task<VendorPaymentDto> ApproveAsync(Guid id, Guid approvedByUserId);
    Task<VendorPaymentDto> MarkSentAsync(Guid id, string? transactionReference = null);
    Task<VendorPaymentDto> ClearAsync(Guid id, string? bankReferenceNumber = null);
    Task<VendorPaymentDto> CancelAsync(Guid id);
    Task DeleteAsync(Guid id);
}
