using Distribution.Application.DTOs;
using Distribution.Application.Services.Interfaces;
using Distribution.Domain.Entities;
using Distribution.Domain.Enums;
using Distribution.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Nexcore.SharedKernel.Api;

namespace Distribution.Infrastructure.Services;

/// <summary>
/// Reason codes, settings and notifications — the module's own administration.
///
/// Reason codes sit in one registry keyed by the surface they appear on, rather than as fifteen
/// hard-coded dropdowns. A warehouse can add "pallet damaged in transit" to short-pick reasons
/// without a deploy, and a damage reason never turns up in a no-order list.
/// </summary>
public class DistributionAdminService(DistributionDbContext db, IDistributionTenant tenant)
    : IDistributionAdminService
{
    // ═══ Reason codes ════════════════════════════════════════════════════════

    public async Task<List<ReasonCodeDto>> ListReasonCodesAsync(ReasonSurface? surface, bool? activeOnly)
        => (await db.ReasonCodes.ForCompany(tenant)
                .WhereIf(surface.HasValue, r => r.Surface == surface)
                .WhereIf(activeOnly == true, r => r.IsActive)
                .OrderBy(r => r.Surface).ThenBy(r => r.DisplayOrder).ThenBy(r => r.Name)
                .ToListAsync())
            .Select(r => r.ToDto()).ToList();

    public async Task<ReasonCodeDto> SaveReasonCodeAsync(Guid? id, ReasonCodeDto request, Guid userId)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new InvalidOperationException("A reason needs a name.");

        ReasonCode entity;
        if (id.HasValue)
        {
            entity = await db.ReasonCodes.ForCompany(tenant).FirstOrDefaultAsync(r => r.Id == id)
                ?? throw new InvalidOperationException("That reason no longer exists.");

            // System reasons are referenced by the services themselves; renaming one is fine,
            // moving it to a different surface would break the dropdown that depends on it.
            if (entity.IsSystem && entity.Surface != request.Surface)
                throw new InvalidOperationException("A built-in reason cannot be moved to another surface.");

            entity.StampUpdated(userId);
        }
        else
        {
            entity = new ReasonCode().StampNew(tenant, userId);
            db.ReasonCodes.Add(entity);
        }

        entity.Name = request.Name.Trim();
        entity.Code = request.Code;
        entity.Surface = request.Surface;
        entity.DisplayOrder = request.DisplayOrder;
        entity.RequiresNote = request.RequiresNote;
        entity.RequiresApproval = request.RequiresApproval;
        entity.IsRecoverable = request.IsRecoverable;
        entity.IsNegative = request.IsNegative;
        entity.ColorHex = request.ColorHex;
        entity.IconName = request.IconName;
        entity.IsActive = request.IsActive;
        entity.Description = request.Description;

        await db.SaveChangesAsync();
        return entity.ToDto();
    }

    public async Task DeleteReasonCodeAsync(Guid id, Guid userId)
    {
        var entity = await db.ReasonCodes.ForCompany(tenant).FirstOrDefaultAsync(r => r.Id == id)
            ?? throw new InvalidOperationException("That reason no longer exists.");

        if (entity.IsSystem)
            throw new InvalidOperationException(
                "This is a built-in reason the system depends on. Deactivate it instead of deleting it.");

        // Reasons are stamped onto history. Deleting one that has been used would leave a
        // variance explained by a dangling id.
        var inUse = await db.SettlementVariances.ForTenant(tenant).AnyAsync(v => v.ReasonCodeId == id)
                    || await db.Visits.ForTenant(tenant).AnyAsync(v => v.NoOrderReasonId == id)
                    || await db.Returns.ForTenant(tenant).AnyAsync(r => r.ReasonCodeId == id)
                    || await db.TripStops.ForTenant(tenant).AnyAsync(s => s.FailureReasonCodeId == id);

        if (inUse)
            throw new InvalidOperationException(
                "This reason has been used on existing records. Deactivate it rather than deleting it.");

        entity.StampDeleted(userId);
        await db.SaveChangesAsync();
    }

    // ═══ Settings ════════════════════════════════════════════════════════════

    public async Task<DistributionSettingsDto> GetSettingsAsync()
    {
        var entity = await db.Settings.ForTenant(tenant).FirstOrDefaultAsync();

        // Never return null: the field terminal needs thresholds before anyone has visited the
        // settings screen, and a defaults-shaped record is a better answer than an empty one.
        return entity is null ? MapSettings(new DistributionSettings()) : MapSettings(entity);
    }

    public async Task<DistributionSettingsDto> SaveSettingsAsync(DistributionSettingsDto request, Guid userId)
    {
        Validate(request);

        var entity = await db.Settings.ForTenant(tenant).FirstOrDefaultAsync();

        if (entity is null)
        {
            entity = new DistributionSettings().StampNew(tenant, userId);
            db.Settings.Add(entity);
        }
        else
        {
            entity.StampUpdated(userId);
        }

        entity.DefaultGeofenceRadiusMetres = request.DefaultGeofenceRadiusMetres;
        entity.RequireGeoOnCheckIn = request.RequireGeoOnCheckIn;
        entity.AllowOutOfFenceCheckIn = request.AllowOutOfFenceCheckIn;
        entity.RequireReasonOnNoOrder = request.RequireReasonOnNoOrder;
        entity.RequireStartSelfie = request.RequireStartSelfie;
        entity.MaxUnplannedVisitsPerDay = request.MaxUnplannedVisitsPerDay;
        entity.BlockDayCloseWithUnsynced = request.BlockDayCloseWithUnsynced;
        entity.DayStartDeadline = request.DayStartDeadline;

        entity.MinimumOrderValue = request.MinimumOrderValue;
        entity.DiscountApprovalThreshold = request.DiscountApprovalThreshold;
        entity.MarginFloorPercent = request.MarginFloorPercent;
        entity.AllowBackorders = request.AllowBackorders;
        entity.OrderEditWindowMinutes = request.OrderEditWindowMinutes;
        entity.DefaultAllocationStrategy = request.DefaultAllocationStrategy;
        entity.SoftAllocationHoldHours = request.SoftAllocationHoldHours;

        entity.CreditEnforcementAtOrder = request.CreditEnforcementAtOrder;
        entity.CreditEnforcementAtDispatch = request.CreditEnforcementAtDispatch;
        entity.CreditEnforcementAtVanSale = request.CreditEnforcementAtVanSale;
        entity.AgeingBucket1Days = request.AgeingBucket1Days;
        entity.AgeingBucket2Days = request.AgeingBucket2Days;
        entity.AgeingBucket3Days = request.AgeingBucket3Days;
        entity.AutoBlockOnBouncedCheque = request.AutoBlockOnBouncedCheque;
        entity.ChequeBounceCharge = request.ChequeBounceCharge;

        entity.EnforceFefo = request.EnforceFefo;
        entity.AllowFefoOverride = request.AllowFefoOverride;
        entity.NearExpiryWarningDays = request.NearExpiryWarningDays;
        entity.NearExpiryCriticalDays = request.NearExpiryCriticalDays;
        entity.MinimumShelfLifePercentOnDespatch = request.MinimumShelfLifePercentOnDespatch;
        entity.AutoQuarantineExpired = request.AutoQuarantineExpired;

        entity.CashVarianceTolerance = request.CashVarianceTolerance;
        entity.StockVarianceTolerancePercent = request.StockVarianceTolerancePercent;
        entity.BlockSettlementOnUnexplainedVariance = request.BlockSettlementOnUnexplainedVariance;
        entity.VarianceApprovalThreshold = request.VarianceApprovalThreshold;
        entity.SettlementCutOff = request.SettlementCutOff;

        entity.AutoApplySchemes = request.AutoApplySchemes;
        entity.ShowNextSlabPrompt = request.ShowNextSlabPrompt;
        entity.StopSchemeOnBudgetExhausted = request.StopSchemeOnBudgetExhausted;

        entity.ClaimSubmissionWindowDays = request.ClaimSubmissionWindowDays;
        entity.ClaimSettlementSlaDays = request.ClaimSettlementSlaDays;
        entity.AutoGenerateDeferredSchemeClaims = request.AutoGenerateDeferredSchemeClaims;

        entity.SecondaryUploadDueDayOfMonth = request.SecondaryUploadDueDayOfMonth;
        entity.MinimumMappingAccuracyPercent = request.MinimumMappingAccuracyPercent;
        entity.ReconciliationTolerancePercent = request.ReconciliationTolerancePercent;

        entity.BaseCurrencyCode = request.BaseCurrencyCode;
        entity.PrintThermalInvoices = request.PrintThermalInvoices;
        entity.InvoiceFooter = request.InvoiceFooter;
        entity.SendDigitalReceipts = request.SendDigitalReceipts;

        await db.SaveChangesAsync();
        return MapSettings(entity);
    }

    // ═══ Notifications ═══════════════════════════════════════════════════════

    public async Task<PaginatedResponse<NotificationDto>> ListNotificationsAsync(
        Guid? userId, bool? unreadOnly, PaginationParams pagination)
    {
        var query = db.Notifications.ForTenant(tenant)
            .WhereIf(userId.HasValue, n => n.TargetUserId == userId || n.TargetUserId == null)
            .WhereIf(unreadOnly == true, n => n.ReadAt == null)
            .Where(n => n.DismissedAt == null);

        var total = await query.CountAsync();
        var rows = await query
            // Critical first, then newest: an alert list that buries the important one is noise.
            .OrderByDescending(n => n.Severity).ThenByDescending(n => n.RaisedAt)
            .Skip((pagination.PageNumber - 1) * pagination.PageSize)
            .Take(pagination.PageSize).ToListAsync();

        return PaginatedResponse<NotificationDto>.Ok(
            rows.Select(r => r.ToDto()), total, pagination.PageNumber, pagination.PageSize);
    }

    public async Task<NotificationDto> MarkReadAsync(Guid notificationId, Guid userId)
    {
        var entity = await db.Notifications.ForTenant(tenant)
            .FirstOrDefaultAsync(n => n.Id == notificationId)
            ?? throw new InvalidOperationException("That notification no longer exists.");

        entity.ReadAt ??= DateTime.UtcNow;
        entity.StampUpdated(userId);

        await db.SaveChangesAsync();
        return entity.ToDto();
    }

    public async Task MarkAllReadAsync(Guid userId)
        => await db.Notifications.ForTenant(tenant)
            .Where(n => (n.TargetUserId == userId || n.TargetUserId == null) && n.ReadAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.ReadAt, DateTime.UtcNow));

    public async Task<int> GetUnreadCountAsync(Guid userId)
        => await db.Notifications.ForTenant(tenant)
            .CountAsync(n => (n.TargetUserId == userId || n.TargetUserId == null)
                             && n.ReadAt == null && n.DismissedAt == null);

    // ═══ Internals ═══════════════════════════════════════════════════════════

    private static void Validate(DistributionSettingsDto request)
    {
        if (request.AgeingBucket1Days >= request.AgeingBucket2Days
            || request.AgeingBucket2Days >= request.AgeingBucket3Days)
            throw new InvalidOperationException("The ageing buckets must increase in order.");

        if (request.NearExpiryCriticalDays >= request.NearExpiryWarningDays)
            throw new InvalidOperationException(
                "The critical expiry horizon must be shorter than the warning horizon.");

        if (request.MinimumShelfLifePercentOnDespatch is < 0 or > 100)
            throw new InvalidOperationException("Minimum shelf life on despatch must be between 0 and 100 percent.");

        if (request.SecondaryUploadDueDayOfMonth is < 1 or > 28)
            throw new InvalidOperationException("The upload due day must be between 1 and 28.");

        if (request.ClaimSettlementSlaDays <= 0)
            throw new InvalidOperationException("The claim settlement SLA must be at least one day.");

        if (string.IsNullOrWhiteSpace(request.BaseCurrencyCode) || request.BaseCurrencyCode.Length != 3)
            throw new InvalidOperationException("The base currency must be a three-letter code.");
    }

    private static DistributionSettingsDto MapSettings(DistributionSettings e) => new()
    {
        Id = e.Id,
        DefaultGeofenceRadiusMetres = e.DefaultGeofenceRadiusMetres,
        RequireGeoOnCheckIn = e.RequireGeoOnCheckIn,
        AllowOutOfFenceCheckIn = e.AllowOutOfFenceCheckIn,
        RequireReasonOnNoOrder = e.RequireReasonOnNoOrder,
        RequireStartSelfie = e.RequireStartSelfie,
        MaxUnplannedVisitsPerDay = e.MaxUnplannedVisitsPerDay,
        BlockDayCloseWithUnsynced = e.BlockDayCloseWithUnsynced,
        DayStartDeadline = e.DayStartDeadline,
        MinimumOrderValue = e.MinimumOrderValue,
        DiscountApprovalThreshold = e.DiscountApprovalThreshold,
        MarginFloorPercent = e.MarginFloorPercent,
        AllowBackorders = e.AllowBackorders,
        OrderEditWindowMinutes = e.OrderEditWindowMinutes,
        DefaultAllocationStrategy = e.DefaultAllocationStrategy,
        SoftAllocationHoldHours = e.SoftAllocationHoldHours,
        CreditEnforcementAtOrder = e.CreditEnforcementAtOrder,
        CreditEnforcementAtDispatch = e.CreditEnforcementAtDispatch,
        CreditEnforcementAtVanSale = e.CreditEnforcementAtVanSale,
        AgeingBucket1Days = e.AgeingBucket1Days,
        AgeingBucket2Days = e.AgeingBucket2Days,
        AgeingBucket3Days = e.AgeingBucket3Days,
        AutoBlockOnBouncedCheque = e.AutoBlockOnBouncedCheque,
        ChequeBounceCharge = e.ChequeBounceCharge,
        EnforceFefo = e.EnforceFefo,
        AllowFefoOverride = e.AllowFefoOverride,
        NearExpiryWarningDays = e.NearExpiryWarningDays,
        NearExpiryCriticalDays = e.NearExpiryCriticalDays,
        MinimumShelfLifePercentOnDespatch = e.MinimumShelfLifePercentOnDespatch,
        AutoQuarantineExpired = e.AutoQuarantineExpired,
        CashVarianceTolerance = e.CashVarianceTolerance,
        StockVarianceTolerancePercent = e.StockVarianceTolerancePercent,
        BlockSettlementOnUnexplainedVariance = e.BlockSettlementOnUnexplainedVariance,
        VarianceApprovalThreshold = e.VarianceApprovalThreshold,
        SettlementCutOff = e.SettlementCutOff,
        AutoApplySchemes = e.AutoApplySchemes,
        ShowNextSlabPrompt = e.ShowNextSlabPrompt,
        StopSchemeOnBudgetExhausted = e.StopSchemeOnBudgetExhausted,
        ClaimSubmissionWindowDays = e.ClaimSubmissionWindowDays,
        ClaimSettlementSlaDays = e.ClaimSettlementSlaDays,
        AutoGenerateDeferredSchemeClaims = e.AutoGenerateDeferredSchemeClaims,
        SecondaryUploadDueDayOfMonth = e.SecondaryUploadDueDayOfMonth,
        MinimumMappingAccuracyPercent = e.MinimumMappingAccuracyPercent,
        ReconciliationTolerancePercent = e.ReconciliationTolerancePercent,
        BaseCurrencyCode = e.BaseCurrencyCode,
        PrintThermalInvoices = e.PrintThermalInvoices,
        InvoiceFooter = e.InvoiceFooter,
        SendDigitalReceipts = e.SendDigitalReceipts,
    };
}
