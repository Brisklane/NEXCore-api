using Fitness.Application.DTOs;
using Fitness.Domain.Entities;
using Fitness.Domain.Enums;
using AppointmentServiceEntity = Fitness.Domain.Entities.AppointmentService;

namespace Fitness.Infrastructure.Services;

/// <summary>
/// Entity → DTO projection, by hand.
///
/// No AutoMapper on purpose. Half of these mappings compute something — a member's age, days
/// since their last visit, whether a change in body fat is an improvement, a score formatted as
/// "4:32" — and a convention-based mapper either cannot do that or hides it in a profile nobody
/// reads. Written out, every derived field is visible next to the field it derives from.
/// </summary>
public static class FitnessMapper
{
    // ═══ Club ════════════════════════════════════════════════════════════════

    public static ClubDto ToDto(FitnessClub c) => new()
    {
        Id = c.Id,
        Code = c.Code,
        Name = c.Name,
        ClubType = c.ClubType,
        Phone = c.Phone,
        Email = c.Email,
        AddressLine = c.AddressLine,
        City = c.City,
        PostCode = c.PostCode,
        CountryCode = c.CountryCode,
        Latitude = c.Latitude,
        Longitude = c.Longitude,
        TimeZoneId = c.TimeZoneId,
        CurrencyCode = c.CurrencyCode,
        UnitSystem = c.UnitSystem,
        WarehouseId = c.WarehouseId,
        PosStoreId = c.PosStoreId,
        DefaultTaxGroupId = c.DefaultTaxGroupId,
        DefaultTaxPercent = c.DefaultTaxPercent,
        SoftCapacity = c.SoftCapacity,
        HardCapacity = c.HardCapacity,
        CurrentOccupancy = c.CurrentOccupancy,
        DefaultBookingPolicyId = c.DefaultBookingPolicyId,
        DefaultCancellationPolicyId = c.DefaultCancellationPolicyId,
        DefaultDunningPolicyId = c.DefaultDunningPolicyId,
        AccessBalanceThreshold = c.AccessBalanceThreshold,
        AntiPassback = c.AntiPassback,
        AntiPassbackMinutes = c.AntiPassbackMinutes,
        OfflinePolicy = c.OfflinePolicy,
        MinimumAge = c.MinimumAge,
        GuardianRequiredBelowAge = c.GuardianRequiredBelowAge,
        RequiresWaiver = c.RequiresWaiver,
        RequiresHealthScreening = c.RequiresHealthScreening,
        AllowsCrossClubVisits = c.AllowsCrossClubVisits,
        CrossClubVisitFee = c.CrossClubVisitFee,
        IsTemporarilyClosed = c.IsTemporarilyClosed,
        ClosureNote = c.ClosureNote,
        LogoUrl = c.LogoUrl,
        ReceiptFooter = c.ReceiptFooter,
        BrandCode = c.BrandCode,
        IsActive = c.IsActive,
        Description = c.Description,
        Schedules = [.. c.Schedules.Select(ToDto).OrderBy(s => s.DayOfWeek)],
    };

    public static ClubScheduleDto ToDto(ClubSchedule s) => new()
    {
        Id = s.Id,
        ClubId = s.ClubId,
        DayOfWeek = s.DayOfWeek,
        OverrideDate = s.OverrideDate,
        OpensAt = s.OpensAt,
        ClosesAt = s.ClosesAt,
        StaffedFrom = s.StaffedFrom,
        StaffedTo = s.StaffedTo,
        IsClosed = s.IsClosed,
        Note = s.Note,
    };

    public static ClubClosureDto ToDto(ClubClosure c) => new()
    {
        Id = c.Id,
        ClubId = c.ClubId,
        StartsOn = c.StartsOn,
        EndsOn = c.EndsOn,
        Reason = c.Reason,
        MemberNotice = c.MemberNotice,
        CancelsClasses = c.CancelsClasses,
        ExtendsAgreements = c.ExtendsAgreements,
        BlocksAccess = c.BlocksAccess,
    };

    public static ClubAreaDto ToDto(ClubArea a) => new()
    {
        Id = a.Id,
        ClubId = a.ClubId,
        Name = a.Name,
        Kind = a.Kind,
        DisplayOrder = a.DisplayOrder,
        Capacity = a.Capacity,
        CurrentOccupancy = a.CurrentOccupancy,
        RequiresEntitlement = a.RequiresEntitlement,
        MinimumAge = a.MinimumAge,
        MaxParticipantsPerStaff = a.MaxParticipantsPerStaff,
        IsOutOfService = a.IsOutOfService,
        OutOfServiceNote = a.OutOfServiceNote,
        IsActive = a.IsActive,
    };

    public static RoomDto ToDto(Room r) => new()
    {
        Id = r.Id,
        ClubId = r.ClubId,
        AreaId = r.AreaId,
        AreaName = r.Area?.Name,
        Name = r.Name,
        Capacity = r.Capacity,
        DisplayOrder = r.DisplayOrder,
        HasSpotMap = r.HasSpotMap,
        GridColumns = r.GridColumns,
        GridRows = r.GridRows,
        EquipmentNote = r.EquipmentNote,
        IsOutOfService = r.IsOutOfService,
        IsActive = r.IsActive,
        Spots = [.. r.Spots.Where(s => !s.IsDeleted).Select(ToDto).OrderBy(s => s.GridRow).ThenBy(s => s.GridColumn)],
    };

    public static RoomSpotDto ToDto(RoomSpot s) => new()
    {
        Id = s.Id,
        RoomId = s.RoomId,
        Label = s.Label,
        GridColumn = s.GridColumn,
        GridRow = s.GridRow,
        EquipmentAssetId = s.EquipmentAssetId,
        IsReserved = s.IsReserved,
        ReservedNote = s.ReservedNote,
        IsOutOfService = s.IsOutOfService,
    };

    public static FitnessSettingsDto ToDto(FitnessSettings s) => new()
    {
        Id = s.Id,
        MemberNumberPrefix = s.MemberNumberPrefix,
        DefaultNoticePeriodDays = s.DefaultNoticePeriodDays,
        DefaultCoolingOffDays = s.DefaultCoolingOffDays,
        MaxFreezeDaysPerYear = s.MaxFreezeDaysPerYear,
        DefaultFreezeFeePerMonth = s.DefaultFreezeFeePerMonth,
        DefaultBillingAnchor = s.DefaultBillingAnchor,
        FixedBillingDayOfMonth = s.FixedBillingDayOfMonth,
        DefaultProration = s.DefaultProration,
        InvoiceGraceDays = s.InvoiceGraceDays,
        DefaultLateFee = s.DefaultLateFee,
        AutoRunBilling = s.AutoRunBilling,
        BillingRunTime = s.BillingRunTime,
        AccessCacheSeconds = s.AccessCacheSeconds,
        CaptureImageOnDenial = s.CaptureImageOnDenial,
        AbsenceRiskDays = s.AbsenceRiskDays,
        CriticalAbsenceDays = s.CriticalAbsenceDays,
        AutoScoreChurn = s.AutoScoreChurn,
        QuietHoursFrom = s.QuietHoursFrom,
        QuietHoursTo = s.QuietHoursTo,
        RespectQuietHours = s.RespectQuietHours,
        FromEmail = s.FromEmail,
        FromName = s.FromName,
        SmsSenderId = s.SmsSenderId,
        LeadResponseSlaMinutes = s.LeadResponseSlaMinutes,
        DiscountApprovalThresholdPercent = s.DiscountApprovalThresholdPercent,
        RefundApprovalThreshold = s.RefundApprovalThreshold,
        WriteOffApprovalThreshold = s.WriteOffApprovalThreshold,
        RequirePinForOverrides = s.RequirePinForOverrides,
    };

    // ═══ Member ══════════════════════════════════════════════════════════════

    public static MemberSummaryDto ToSummary(Member m, DateTime now) => new()
    {
        Id = m.Id,
        MemberNumber = m.MemberNumber,
        FullName = FullName(m),
        PreferredName = m.PreferredName,
        PhotoUrl = m.PhotoUrl,
        Phone = m.Phone,
        Email = m.Email,
        Status = m.Status,
        HomeClubId = m.HomeClubId,
        HomeClubName = m.HomeClub?.Name,
        JoinedOn = m.JoinedOn,
        NextBillingOn = m.NextBillingOn,
        AccountBalance = m.AccountBalance,
        LastVisitOn = m.LastVisitOn,
        DaysSinceLastVisit = m.LastVisitOn is null ? int.MaxValue : (int)(now.Date - m.LastVisitOn.Value.Date).TotalDays,
        TotalVisits = m.TotalVisits,
        RiskBand = m.RiskBand,
        IsBanned = m.IsBanned,
    };

    public static MemberDetailDto ToDetail(Member m, DateTime now)
    {
        var dto = new MemberDetailDto
        {
            Id = m.Id,
            MemberNumber = m.MemberNumber,
            ContactId = m.ContactId,
            FirstName = m.FirstName,
            LastName = m.LastName,
            FullName = FullName(m),
            PreferredName = m.PreferredName,
            DateOfBirth = m.DateOfBirth,
            Age = AgeOn(m.DateOfBirth, now),
            Gender = m.Gender,
            NationalId = m.NationalId,
            Occupation = m.Occupation,
            PhotoUrl = m.PhotoUrl,
            Phone = m.Phone,
            AlternatePhone = m.AlternatePhone,
            Email = m.Email,
            AddressLine = m.AddressLine,
            City = m.City,
            PostCode = m.PostCode,
            CountryCode = m.CountryCode,
            PreferredLanguage = m.PreferredLanguage,
            PreferredChannel = m.PreferredChannel,
            Status = m.Status,
            HomeClubId = m.HomeClubId,
            HomeClubName = m.HomeClub?.Name,
            JoinedOn = m.JoinedOn,
            FirstJoinedOn = m.FirstJoinedOn,
            LeftOn = m.LeftOn,
            HouseholdId = m.HouseholdId,
            HouseholdName = m.Household?.Name,
            CorporateAccountId = m.CorporateAccountId,
            AssignedCoachId = m.AssignedCoachId,
            ReferredByMemberId = m.ReferredByMemberId,
            AccountBalance = m.AccountBalance,
            CreditBalance = m.CreditBalance,
            NextBillingOn = m.NextBillingOn,
            LastVisitOn = m.LastVisitOn,
            TotalVisits = m.TotalVisits,
            VisitsThisMonth = m.VisitsThisMonth,
            VisitFrequencyBaseline = m.VisitFrequencyBaseline,
            CurrentStreakDays = m.CurrentStreakDays,
            LoyaltyPoints = m.LoyaltyPoints,
            RiskBand = m.RiskBand,
            WaiverSigned = m.WaiverSigned,
            WaiverSignedOn = m.WaiverSignedOn,
            MedicalClearance = m.MedicalClearance,
            MedicalSummary = m.MedicalSummary,
            IsBanned = m.IsBanned,
            BanReason = m.BanReason,
            BanUntil = m.BanUntil,
            PhotoConsent = m.PhotoConsent,
            LeaderboardOptIn = m.LeaderboardOptIn,
            IsAnonymised = m.IsAnonymised,
            Tags = [.. m.Tags.Where(t => !t.IsDeleted).Select(t => t.Tag)],
            EmergencyContacts = [.. m.EmergencyContacts.Where(c => !c.IsDeleted).Select(ToDto)],
            Credentials = [.. m.Credentials.Where(c => !c.IsDeleted).Select(ToDto)],
        };

        dto.DaysSinceLastVisit = m.LastVisitOn is null
            ? int.MaxValue
            : (int)(now.Date - m.LastVisitOn.Value.Date).TotalDays;

        var since = m.FirstJoinedOn ?? m.JoinedOn;
        dto.TenureDays = since is null ? 0 : (int)(now.Date - since.Value.Date).TotalDays;

        return dto;
    }

    public static EmergencyContactDto ToDto(EmergencyContact c) => new()
    {
        Id = c.Id,
        MemberId = c.MemberId,
        Name = c.Name,
        Relationship = c.Relationship,
        Phone = c.Phone,
        AlternatePhone = c.AlternatePhone,
        Email = c.Email,
        IsPrimary = c.IsPrimary,
    };

    public static MedicalFlagDto ToDto(MedicalFlag f) => new()
    {
        Id = f.Id,
        MemberId = f.MemberId,
        Category = f.Category,
        Detail = f.Detail,
        Severity = f.Severity,
        VisibleToInstructors = f.VisibleToInstructors,
        ReviewOn = f.ReviewOn,
        ResolvedOn = f.ResolvedOn,
    };

    public static MemberNoteDto ToDto(MemberNote n) => new()
    {
        Id = n.Id,
        MemberId = n.MemberId,
        Kind = n.Kind,
        Body = n.Body,
        OccurredAt = n.OccurredAt,
        IsPrivate = n.IsPrivate,
        IsPinned = n.IsPinned,
        StaffId = n.StaffId,
        AuthorName = n.AuthorName,
        RelatedEntityId = n.RelatedEntityId,
        RelatedEntityType = n.RelatedEntityType,
    };

    public static MemberAlertDto ToDto(MemberAlert a) => new()
    {
        Id = a.Id,
        MemberId = a.MemberId,
        Kind = a.Kind,
        Severity = a.Severity,
        Message = a.Message,
        ActionLabel = a.ActionLabel,
        ActionRoute = a.ActionRoute,
        BlocksAccess = a.BlocksAccess,
        ExpiresOn = a.ExpiresOn,
        AcknowledgedAt = a.AcknowledgedAt,
    };

    public static MemberCredentialDto ToDto(MemberCredential c) => new()
    {
        Id = c.Id,
        MemberId = c.MemberId,
        Type = c.Type,
        Identifier = MaskCredential(c.Identifier, c.Type),
        Status = c.Status,
        IssuedOn = c.IssuedOn,
        ExpiresOn = c.ExpiresOn,
        DeactivatedOn = c.DeactivatedOn,
        DeactivationReason = c.DeactivationReason,
        ReplacementFee = c.ReplacementFee,
        LastUsedAt = c.LastUsedAt,
    };

    public static MemberConsentDto ToDto(MemberConsent c) => new()
    {
        Id = c.Id,
        MemberId = c.MemberId,
        Channel = c.Channel,
        Purpose = c.Purpose,
        Granted = c.Granted,
        DecidedAt = c.DecidedAt,
        ConsentText = c.ConsentText,
        CapturedVia = c.CapturedVia,
    };

    public static MemberDocumentDto ToDto(MemberDocument d) => new()
    {
        Id = d.Id,
        MemberId = d.MemberId,
        Kind = d.Kind,
        FileName = d.FileName,
        FileUrl = d.FileUrl,
        ContentType = d.ContentType,
        SizeBytes = d.SizeBytes,
        ValidFrom = d.ValidFrom,
        ExpiresOn = d.ExpiresOn,
        IsSensitive = d.IsSensitive,
        CreatedAt = d.CreatedAt,
    };

    public static MemberStatusHistoryDto ToDto(MemberStatusHistory h) => new()
    {
        Id = h.Id,
        FromStatus = h.FromStatus,
        ToStatus = h.ToStatus,
        ChangedAt = h.ChangedAt,
        Reason = h.Reason,
    };

    public static HouseholdMemberDto ToDto(HouseholdMember hm, DateTime now) => new()
    {
        Id = hm.Id,
        HouseholdId = hm.HouseholdId,
        MemberId = hm.MemberId,
        MemberName = hm.Member is null ? string.Empty : FullName(hm.Member),
        PhotoUrl = hm.Member?.PhotoUrl,
        MemberStatus = hm.Member?.Status ?? MemberStatus.Lead,
        Age = AgeOn(hm.Member?.DateOfBirth, now),
        Role = hm.Role,
        MayCollectChildren = hm.MayCollectChildren,
        AgesOutOn = hm.AgesOutOn,
        AgesOutSoon = hm.AgesOutOn is not null && hm.AgesOutOn.Value <= now.AddDays(60),
    };

    // ═══ Catalogue ═══════════════════════════════════════════════════════════

    public static MembershipPlanDto ToDto(MembershipPlan p) => new()
    {
        Id = p.Id,
        Code = p.Code,
        Name = p.Name,
        Kind = p.Kind,
        MarketingBlurb = p.MarketingBlurb,
        ImageUrl = p.ImageUrl,
        ColourHex = p.ColourHex,
        DisplayOrder = p.DisplayOrder,
        Price = p.Price,
        CurrencyCode = p.CurrencyCode,
        TaxPercent = p.TaxPercent,
        PriceIncludesTax = p.PriceIncludesTax,
        BillingPeriod = p.BillingPeriod,
        BillingAnchor = p.BillingAnchor,
        JoinProration = p.JoinProration,
        CancelProration = p.CancelProration,
        JoiningFee = p.JoiningFee,
        AdminFee = p.AdminFee,
        CardFee = p.CardFee,
        AnnualMaintenanceFee = p.AnnualMaintenanceFee,
        AnnualFeeMonth = p.AnnualFeeMonth,
        MinimumTermMonths = p.MinimumTermMonths,
        DurationMonths = p.DurationMonths,
        NoticePeriodDays = p.NoticePeriodDays,
        AutoRenews = p.AutoRenews,
        EarlyTerminationFee = p.EarlyTerminationFee,
        EarlyTerminationPercentOfRemaining = p.EarlyTerminationPercentOfRemaining,
        CreditCount = p.CreditCount,
        ValidForDays = p.ValidForDays,
        CreditsTransferable = p.CreditsTransferable,
        CreditsRefundable = p.CreditsRefundable,
        RestrictedToClubId = p.RestrictedToClubId,
        AllowsCrossClubAccess = p.AllowsCrossClubAccess,
        VisitsPerPeriod = p.VisitsPerPeriod,
        VisitLimitBasis = p.VisitLimitBasis,
        GuestPassesPerPeriod = p.GuestPassesPerPeriod,
        BookingWindowDays = p.BookingWindowDays,
        MaxConcurrentBookings = p.MaxConcurrentBookings,
        SellableFrom = p.SellableFrom,
        SellableTo = p.SellableTo,
        SellableAtDesk = p.SellableAtDesk,
        SellableOnline = p.SellableOnline,
        SellableInApp = p.SellableInApp,
        SellableAtKiosk = p.SellableAtKiosk,
        IsPrivate = p.IsPrivate,
        MinimumAge = p.MinimumAge,
        MaximumAge = p.MaximumAge,
        RequiredProof = p.RequiredProof,
        WaiverTemplateId = p.WaiverTemplateId,
        AgreementTemplateId = p.AgreementTemplateId,
        RequiresHealthScreening = p.RequiresHealthScreening,
        InventoryItemId = p.InventoryItemId,
        RecognitionBasis = p.RecognitionBasis,
        RevenueAccountId = p.RevenueAccountId,
        DeferredRevenueAccountId = p.DeferredRevenueAccountId,
        Version = p.Version,
        IsActive = p.IsActive,
        Description = p.Description,
        ClubPrices = [.. p.ClubPrices.Where(x => !x.IsDeleted).Select(ToDto)],
        Entitlements = [.. p.Entitlements.Where(x => !x.IsDeleted).Select(ToDto)],
    };

    public static PlanPriceDto ToDto(PlanPrice p) => new()
    {
        Id = p.Id,
        PlanId = p.PlanId,
        ClubId = p.ClubId,
        Price = p.Price,
        JoiningFee = p.JoiningFee,
        CurrencyCode = p.CurrencyCode,
        EffectiveFrom = p.EffectiveFrom,
        EffectiveTo = p.EffectiveTo,
        IsAvailable = p.IsAvailable,
    };

    public static PlanEntitlementDto ToDto(PlanEntitlement e) => new()
    {
        Id = e.Id,
        PlanId = e.PlanId,
        Kind = e.Kind,
        TargetId = e.TargetId,
        TargetName = e.TargetName,
        Limit = e.Limit,
        Quantity = e.Quantity,
        OverageFee = e.OverageFee,
        AllowOverage = e.AllowOverage,
        TimeBands = [.. e.TimeBands.Where(t => !t.IsDeleted).Select(ToDto)],
    };

    public static AccessTimeBandDto ToDto(AccessTimeBand t) => new()
    {
        Id = t.Id,
        Name = t.Name,
        DaysOfWeekMask = t.DaysOfWeekMask,
        StartsAt = t.StartsAt,
        EndsAt = t.EndsAt,
    };

    public static PromotionRuleDto ToDto(PromotionRule p) => new()
    {
        Id = p.Id,
        Name = p.Name,
        PlanId = p.PlanId,
        ClubId = p.ClubId,
        DiscountKind = p.DiscountKind,
        Value = p.Value,
        PeriodCount = p.PeriodCount,
        ActiveFrom = p.ActiveFrom,
        ActiveTo = p.ActiveTo,
        NewMembersOnly = p.NewMembersOnly,
        MaxRedemptions = p.MaxRedemptions,
        RedemptionCount = p.RedemptionCount,
        CampaignId = p.CampaignId,
        DisplayOrder = p.DisplayOrder,
        IsActive = p.IsActive,
    };

    public static PromoCodeDto ToDto(PromoCode c) => new()
    {
        Id = c.Id,
        PromotionRuleId = c.PromotionRuleId,
        CodeText = c.CodeText,
        MaxUses = c.MaxUses,
        UseCount = c.UseCount,
        OnePerMember = c.OnePerMember,
        ExpiresOn = c.ExpiresOn,
        IssuedToMemberId = c.IssuedToMemberId,
        IsActive = c.IsActive,
    };

    public static AppointmentServiceDto ToDto(AppointmentServiceEntity s) => new()
    {
        Id = s.Id,
        Name = s.Name,
        Kind = s.Kind,
        DurationMinutes = s.DurationMinutes,
        BufferMinutes = s.BufferMinutes,
        Price = s.Price,
        TaxPercent = s.TaxPercent,
        MaxParticipants = s.MaxParticipants,
        RequiredResourceId = s.RequiredResourceId,
        RequiredRoomId = s.RequiredRoomId,
        FreeCancelHours = s.FreeCancelHours,
        LateCancelOutcome = s.LateCancelOutcome,
        NoShowOutcome = s.NoShowOutcome,
        ColourHex = s.ColourHex,
        Description = s.Description,
        DisplayOrder = s.DisplayOrder,
        BookableOnline = s.BookableOnline,
        IsActive = s.IsActive,
    };

    // ═══ Agreement ═══════════════════════════════════════════════════════════

    public static AgreementSummaryDto ToSummary(Agreement a, DateTime now) => new()
    {
        Id = a.Id,
        AgreementNumber = a.AgreementNumber,
        MemberId = a.MemberId,
        MemberName = a.Member is null ? null : FullName(a.Member),
        MemberNumber = a.Member?.MemberNumber,
        PlanId = a.PlanId,
        PlanName = a.Plan?.Name ?? string.Empty,
        PlanKind = a.Plan?.Kind ?? PlanKind.RecurringMembership,
        ClubId = a.ClubId,
        Status = a.Status,
        StartsOn = a.StartsOn,
        MinimumTermEndsOn = a.MinimumTermEndsOn,
        EndsOn = a.EndsOn,
        CancellationEffectiveOn = a.CancellationEffectiveOn,
        Price = a.Price,
        PromotionalPrice = a.PromotionalPrice,
        PromotionalPeriodsRemaining = a.PromotionalPeriodsRemaining,
        CurrencyCode = a.CurrencyCode,
        BillingPeriod = a.BillingPeriod,
        NextBillingOn = a.NextBillingOn,
        NextBillingAmount = a.PromotionalPeriodsRemaining > 0 ? a.PromotionalPrice ?? a.Price : a.Price,
        CreditsRemaining = a.CreditsRemaining,
        CreditsGranted = a.CreditsGranted,
        CreditsExpireOn = a.CreditsExpireOn,
        IsFrozen = a.Status == AgreementStatus.Frozen,
        FrozenUntil = a.Freezes.Where(f => !f.IsReleased).Select(f => (DateTime?)f.EndsOn).FirstOrDefault(),
        IsInMinimumTerm = a.MinimumTermEndsOn is not null && a.MinimumTermEndsOn > now,
        PriceLocked = a.PriceLocked,
    };

    public static AgreementDetailDto ToDetail(Agreement a, DateTime now)
    {
        var s = ToSummary(a, now);
        return new AgreementDetailDto
        {
            Id = s.Id,
            AgreementNumber = s.AgreementNumber,
            MemberId = s.MemberId,
            MemberName = s.MemberName,
            MemberNumber = s.MemberNumber,
            PlanId = s.PlanId,
            PlanName = s.PlanName,
            PlanKind = s.PlanKind,
            ClubId = s.ClubId,
            Status = s.Status,
            StartsOn = s.StartsOn,
            MinimumTermEndsOn = s.MinimumTermEndsOn,
            EndsOn = s.EndsOn,
            CancellationEffectiveOn = s.CancellationEffectiveOn,
            Price = s.Price,
            PromotionalPrice = s.PromotionalPrice,
            PromotionalPeriodsRemaining = s.PromotionalPeriodsRemaining,
            CurrencyCode = s.CurrencyCode,
            BillingPeriod = s.BillingPeriod,
            NextBillingOn = s.NextBillingOn,
            NextBillingAmount = s.NextBillingAmount,
            CreditsRemaining = s.CreditsRemaining,
            CreditsGranted = s.CreditsGranted,
            CreditsExpireOn = s.CreditsExpireOn,
            IsFrozen = s.IsFrozen,
            FrozenUntil = s.FrozenUntil,
            IsInMinimumTerm = s.IsInMinimumTerm,
            PriceLocked = s.PriceLocked,

            PlanVersion = a.PlanVersion,
            SignedOn = a.SignedOn,
            CancelledOn = a.CancelledOn,
            CoolingOffEndsOn = a.CoolingOffEndsOn,
            IsInCoolingOff = a.CoolingOffEndsOn is not null && a.CoolingOffEndsOn > now,
            TaxPercent = a.TaxPercent,
            BillingAnchor = a.BillingAnchor,
            BillingDayOfMonth = a.BillingDayOfMonth,
            NoticePeriodDays = a.NoticePeriodDays,
            AutoRenews = a.AutoRenews,
            PromotionRuleId = a.PromotionRuleId,
            PeriodsBilled = a.PeriodsBilled,
            TotalInstalments = a.TotalInstalments,
            LastBilledOn = a.LastBilledOn,
            PaymentMethodRefId = a.PaymentMethodRefId,
            PayerMemberId = a.PayerMemberId,
            CorporateAccountId = a.CorporateAccountId,
            ThirdPartyPayerId = a.ThirdPartyPayerId,
            LeaveReason = a.LeaveReason,
            LeaveNote = a.LeaveNote,
            EarlyTerminationFeeCharged = a.EarlyTerminationFeeCharged,
            SupersedesAgreementId = a.SupersedesAgreementId,
            SupersededByAgreementId = a.SupersededByAgreementId,
            SoldByStaffId = a.SoldByStaffId,
            SignatureImageUrl = a.SignatureImageUrl,
            DocumentUrl = a.DocumentUrl,
            Amendments = [.. a.Amendments.Where(x => !x.IsDeleted).OrderByDescending(x => x.EffectiveOn).Select(ToDto)],
            Freezes = [.. a.Freezes.Where(x => !x.IsDeleted).OrderByDescending(x => x.StartsOn).Select(f => ToDto(f, now))],
        };
    }

    public static AgreementAmendmentDto ToDto(AgreementAmendment a) => new()
    {
        Id = a.Id,
        AgreementId = a.AgreementId,
        Kind = a.Kind,
        EffectiveOn = a.EffectiveOn,
        PreviousPrice = a.PreviousPrice,
        NewPrice = a.NewPrice,
        PreviousPlanId = a.PreviousPlanId,
        NewPlanId = a.NewPlanId,
        ChangeFee = a.ChangeFee,
        ProrationAmount = a.ProrationAmount,
        Reason = a.Reason,
        DocumentUrl = a.DocumentUrl,
        CreatedAt = a.CreatedAt,
    };

    public static MembershipFreezeDto ToDto(MembershipFreeze f, DateTime now) => new()
    {
        Id = f.Id,
        AgreementId = f.AgreementId,
        MemberId = f.MemberId,
        StartsOn = f.StartsOn,
        EndsOn = f.EndsOn,
        ActuallyEndedOn = f.ActuallyEndedOn,
        Reason = f.Reason,
        ReasonNote = f.ReasonNote,
        FeePerPeriod = f.FeePerPeriod,
        TotalFeeCharged = f.TotalFeeCharged,
        DaysExtended = f.DaysExtended,
        IsMedical = f.IsMedical,
        CountsAgainstAllowance = f.CountsAgainstAllowance,
        SupportingDocumentId = f.SupportingDocumentId,
        ApprovedAt = f.ApprovedAt,
        IsReleased = f.IsReleased,
        IsCurrentlyActive = !f.IsReleased && f.StartsOn <= now && f.EndsOn >= now,
    };

    public static MembershipSuspensionDto ToDto(MembershipSuspension s, DateTime now) => new()
    {
        Id = s.Id,
        MemberId = s.MemberId,
        MemberName = s.Member is null ? null : FullName(s.Member),
        AgreementId = s.AgreementId,
        StartsOn = s.StartsOn,
        EndsOn = s.EndsOn,
        LiftedOn = s.LiftedOn,
        Reason = s.Reason,
        ReasonNote = s.ReasonNote,
        ContinuesBilling = s.ContinuesBilling,
        AutoLiftsWhenResolved = s.AutoLiftsWhenResolved,
        IsCurrentlyActive = s.LiftedOn is null && s.StartsOn <= now && (s.EndsOn is null || s.EndsOn >= now),
    };

    public static CancellationRequestDto ToDto(CancellationRequest c) => new()
    {
        Id = c.Id,
        AgreementId = c.AgreementId,
        AgreementNumber = c.Agreement?.AgreementNumber,
        MemberId = c.MemberId,
        RequestedOn = c.RequestedOn,
        EffectiveOn = c.EffectiveOn,
        Reason = c.Reason,
        ReasonNote = c.ReasonNote,
        Channel = c.Channel,
        EarlyTerminationFee = c.EarlyTerminationFee,
        RefundDue = c.RefundDue,
        OutstandingBalance = c.OutstandingBalance,
        WasSaved = c.WasSaved,
        SavedOn = c.SavedOn,
        IsProcessed = c.IsProcessed,
        Offers = [.. c.Offers.Where(o => !o.IsDeleted).Select(ToDto)],
    };

    public static SaveOfferDto ToDto(SaveOffer o) => new()
    {
        Id = o.Id,
        CancellationRequestId = o.CancellationRequestId,
        Kind = o.Kind,
        Summary = o.Summary,
        DiscountValue = o.DiscountValue,
        PeriodCount = o.PeriodCount,
        AlternativePlanId = o.AlternativePlanId,
        OfferedAt = o.OfferedAt,
        WasAccepted = o.WasAccepted,
        RespondedAt = o.RespondedAt,
        DeclineNote = o.DeclineNote,
    };

    // ═══ Money ═══════════════════════════════════════════════════════════════

    public static BillingScheduleDto ToDto(BillingSchedule s) => new()
    {
        Id = s.Id,
        AgreementId = s.AgreementId,
        MemberId = s.MemberId,
        ClubId = s.ClubId,
        DueOn = s.DueOn,
        PeriodNumber = s.PeriodNumber,
        PeriodStart = s.PeriodStart,
        PeriodEnd = s.PeriodEnd,
        ChargeKind = s.ChargeKind,
        Amount = s.Amount,
        TaxAmount = s.TaxAmount,
        CurrencyCode = s.CurrencyCode,
        InvoiceId = s.InvoiceId,
        IsBilled = s.IsBilled,
        IsSkipped = s.IsSkipped,
        SkipReason = s.SkipReason,
        OriginalAmount = s.OriginalAmount,
        AdjustmentNote = s.AdjustmentNote,
    };

    public static BillingRunDto ToDto(BillingRun r) => new()
    {
        Id = r.Id,
        RunNumber = r.RunNumber,
        BillingDate = r.BillingDate,
        ClubId = r.ClubId,
        PlanId = r.PlanId,
        Status = r.Status,
        IsPreview = r.IsPreview,
        IsAutomatic = r.IsAutomatic,
        StartedAt = r.StartedAt,
        CompletedAt = r.CompletedAt,
        DurationSeconds = r.StartedAt is not null && r.CompletedAt is not null
            ? (int)(r.CompletedAt.Value - r.StartedAt.Value).TotalSeconds
            : null,
        TotalScheduled = r.TotalScheduled,
        InvoicesCreated = r.InvoicesCreated,
        PaymentsCollected = r.PaymentsCollected,
        PaymentsFailed = r.PaymentsFailed,
        Skipped = r.Skipped,
        Errors = r.Errors,
        TotalBilled = r.TotalBilled,
        TotalCollected = r.TotalCollected,
        TotalFailed = r.TotalFailed,
        CollectionRatePercent = Percent(r.TotalCollected, r.TotalBilled),
        ErrorSummary = r.ErrorSummary,
    };

    public static BillingRunLineDto ToDto(BillingRunLine l) => new()
    {
        Id = l.Id,
        MemberId = l.MemberId,
        AgreementId = l.AgreementId,
        InvoiceId = l.InvoiceId,
        Amount = l.Amount,
        Outcome = l.Outcome,
        FailureReason = l.FailureReason,
        Message = l.Message,
    };

    public static InvoiceSummaryDto ToSummary(FitnessInvoice i, DateTime now) => new()
    {
        Id = i.Id,
        InvoiceNumber = i.InvoiceNumber,
        MemberId = i.MemberId,
        MemberName = i.Member is null ? null : FullName(i.Member),
        MemberNumber = i.Member?.MemberNumber,
        ClubId = i.ClubId,
        Status = i.Status,
        IssuedOn = i.IssuedOn,
        DueOn = i.DueOn,
        PaidOn = i.PaidOn,
        Total = i.Total,
        AmountPaid = i.AmountPaid,
        BalanceDue = i.BalanceDue,
        CurrencyCode = i.CurrencyCode,
        DaysOverdue = i.BalanceDue > 0 && i.DueOn < now ? (int)(now.Date - i.DueOn.Date).TotalDays : 0,
        HasDunningCase = i.DunningCaseId is not null,
        SummaryLine = i.Lines.Count > 0 ? i.Lines.OrderBy(l => l.DisplayOrder).First().LineDescription : null,
    };

    public static InvoiceDetailDto ToDetail(FitnessInvoice i, DateTime now)
    {
        var s = ToSummary(i, now);
        return new InvoiceDetailDto
        {
            Id = s.Id,
            InvoiceNumber = s.InvoiceNumber,
            MemberId = s.MemberId,
            MemberName = s.MemberName,
            MemberNumber = s.MemberNumber,
            ClubId = s.ClubId,
            Status = s.Status,
            IssuedOn = s.IssuedOn,
            DueOn = s.DueOn,
            PaidOn = s.PaidOn,
            Total = s.Total,
            AmountPaid = s.AmountPaid,
            BalanceDue = s.BalanceDue,
            CurrencyCode = s.CurrencyCode,
            DaysOverdue = s.DaysOverdue,
            HasDunningCase = s.HasDunningCase,

            AgreementId = i.AgreementId,
            CorporateAccountId = i.CorporateAccountId,
            PayerMemberId = i.PayerMemberId,
            Subtotal = i.Subtotal,
            DiscountTotal = i.DiscountTotal,
            TaxTotal = i.TaxTotal,
            AmountRefunded = i.AmountRefunded,
            ExchangeRate = i.ExchangeRate,
            BillingRunId = i.BillingRunId,
            DunningCaseId = i.DunningCaseId,
            DocumentUrl = i.DocumentUrl,
            Notes = i.Notes,
            RemindersSuppressed = i.RemindersSuppressed,
            Lines = [.. i.Lines.Where(l => !l.IsDeleted).OrderBy(l => l.DisplayOrder).Select(ToDto)],
            Payments = [.. i.Payments.Where(p => !p.IsDeleted).OrderByDescending(p => p.ReceivedOn).Select(ToDto)],
        };
    }

    public static InvoiceLineDto ToDto(FitnessInvoiceLine l) => new()
    {
        Id = l.Id,
        ChargeKind = l.ChargeKind,
        LineDescription = l.LineDescription,
        PeriodStart = l.PeriodStart,
        PeriodEnd = l.PeriodEnd,
        Quantity = l.Quantity,
        UnitPrice = l.UnitPrice,
        DiscountAmount = l.DiscountAmount,
        TaxPercent = l.TaxPercent,
        TaxAmount = l.TaxAmount,
        LineTotal = l.LineTotal,
        ProrationExplanation = l.ProrationExplanation,
        PlanId = l.PlanId,
        DisplayOrder = l.DisplayOrder,
    };

    public static PaymentDto ToDto(FitnessPayment p) => new()
    {
        Id = p.Id,
        PaymentNumber = p.PaymentNumber,
        MemberId = p.MemberId,
        InvoiceId = p.InvoiceId,
        InvoiceNumber = p.Invoice?.InvoiceNumber,
        ClubId = p.ClubId,
        Method = p.Method,
        Status = p.Status,
        Amount = p.Amount,
        RefundedAmount = p.RefundedAmount,
        CurrencyCode = p.CurrencyCode,
        ReceivedOn = p.ReceivedOn,
        SettledOn = p.SettledOn,
        ProviderReference = p.ProviderReference,
        AuthorisationCode = p.AuthorisationCode,
        CardBrand = p.CardBrand,
        CardLastFour = p.CardLastFour,
        MandateReference = p.MandateReference,
        FailureReason = p.FailureReason,
        FailureMessage = p.FailureMessage,
        AttemptNumber = p.AttemptNumber,
        Notes = p.Notes,
    };

    public static PaymentMethodRefDto ToDto(PaymentMethodRef m, DateTime now)
    {
        var expired = m.ExpiryYear is not null && m.ExpiryMonth is not null &&
                      new DateTime(m.ExpiryYear.Value, m.ExpiryMonth.Value, 1).AddMonths(1) <= now;

        return new PaymentMethodRefDto
        {
            Id = m.Id,
            MemberId = m.MemberId,
            Method = m.Method,
            ProviderName = m.ProviderName,
            CardBrand = m.CardBrand,
            CardLastFour = m.CardLastFour,
            ExpiryMonth = m.ExpiryMonth,
            ExpiryYear = m.ExpiryYear,
            BankName = m.BankName,
            AccountLastFour = m.AccountLastFour,
            AccountHolderName = m.AccountHolderName,
            IsDefault = m.IsDefault,
            IsActive = m.IsActive,
            IsExpiringSoon = m.IsExpiringSoon,
            IsExpired = expired,
            LastFailedOn = m.LastFailedOn,
            ConsecutiveFailures = m.ConsecutiveFailures,
            DisplayLabel = DescribePaymentMethod(m),
        };
    }

    public static DunningCaseDto ToDto(DunningCase c, DateTime now, int totalSteps) => new()
    {
        Id = c.Id,
        CaseNumber = c.CaseNumber,
        MemberId = c.MemberId,
        MemberName = c.Member is null ? null : FullName(c.Member),
        MemberNumber = c.Member?.MemberNumber,
        MemberPhone = c.Member?.Phone,
        InvoiceId = c.InvoiceId,
        ClubId = c.ClubId,
        Status = c.Status,
        OpenedOn = c.OpenedOn,
        ClosedOn = c.ClosedOn,
        DaysOpen = (int)((c.ClosedOn ?? now).Date - c.OpenedOn.Date).TotalDays,
        AmountOutstanding = c.AmountOutstanding,
        AmountRecovered = c.AmountRecovered,
        LateFeesAdded = c.LateFeesAdded,
        InitialFailureReason = c.InitialFailureReason,
        CurrentStep = c.CurrentStep,
        TotalSteps = totalSteps,
        NextStepDueOn = c.NextStepDueOn,
        RetryAttempts = c.RetryAttempts,
        LastRetryOn = c.LastRetryOn,
        IsPaused = c.IsPaused,
        PauseReason = c.PauseReason,
        AssignedToStaffId = c.AssignedToStaffId,
        Events = [.. c.Events.Where(e => !e.IsDeleted).OrderByDescending(e => e.OccurredAt).Select(ToDto)],
    };

    public static DunningEventDto ToDto(DunningEvent e) => new()
    {
        Id = e.Id,
        StepNumber = e.StepNumber,
        Action = e.Action,
        OccurredAt = e.OccurredAt,
        Succeeded = e.Succeeded,
        Detail = e.Detail,
        AmountCollected = e.AmountCollected,
    };

    public static DunningPolicyDto ToDto(DunningPolicy p) => new()
    {
        Id = p.Id,
        Name = p.Name,
        ClubId = p.ClubId,
        IsDefault = p.IsDefault,
        WriteOffAfterDays = p.WriteOffAfterDays,
        SuspendAccessAfterDays = p.SuspendAccessAfterDays,
        IsActive = p.IsActive,
        Steps = [.. p.Steps.Where(s => !s.IsDeleted).OrderBy(s => s.StepNumber).Select(ToDto)],
    };

    public static DunningStepDto ToDto(DunningStep s) => new()
    {
        Id = s.Id,
        DunningPolicyId = s.DunningPolicyId,
        StepNumber = s.StepNumber,
        DelayDays = s.DelayDays,
        Action = s.Action,
        Channel = s.Channel,
        MessageTemplateId = s.MessageTemplateId,
        FeeAmount = s.FeeAmount,
        SkipOnTechnicalFailure = s.SkipOnTechnicalFailure,
    };

    public static MemberLedgerEntryDto ToDto(MemberLedgerEntry l) => new()
    {
        Id = l.Id,
        Kind = l.Kind,
        OccurredAt = l.OccurredAt,
        Amount = l.Amount,
        BalanceAfter = l.BalanceAfter,
        EntryDescription = l.EntryDescription,
        CurrencyCode = l.CurrencyCode,
        InvoiceId = l.InvoiceId,
        PaymentId = l.PaymentId,
        CreditNoteId = l.CreditNoteId,
        RefundId = l.RefundId,
        DrillRoute = l.InvoiceId is not null ? $"/fitness/invoices/{l.InvoiceId}" : null,
    };

    public static CreditNoteDto ToDto(CreditNote c) => new()
    {
        Id = c.Id,
        CreditNoteNumber = c.CreditNoteNumber,
        MemberId = c.MemberId,
        InvoiceId = c.InvoiceId,
        Amount = c.Amount,
        TaxAmount = c.TaxAmount,
        CurrencyCode = c.CurrencyCode,
        IssuedOn = c.IssuedOn,
        Reason = c.Reason,
        AppliedToBalance = c.AppliedToBalance,
        DocumentUrl = c.DocumentUrl,
    };

    public static RefundDto ToDto(Refund r) => new()
    {
        Id = r.Id,
        RefundNumber = r.RefundNumber,
        MemberId = r.MemberId,
        PaymentId = r.PaymentId,
        InvoiceId = r.InvoiceId,
        Amount = r.Amount,
        CurrencyCode = r.CurrencyCode,
        Method = r.Method,
        Status = r.Status,
        RequestedOn = r.RequestedOn,
        ProcessedOn = r.ProcessedOn,
        Reason = r.Reason,
        ToOriginalMethod = r.ToOriginalMethod,
    };

    public static CashSessionDto ToDto(CashSession s) => new()
    {
        Id = s.Id,
        SessionNumber = s.SessionNumber,
        ClubId = s.ClubId,
        Status = s.Status,
        OpenedAt = s.OpenedAt,
        ClosedAt = s.ClosedAt,
        OpeningFloat = s.OpeningFloat,
        CashSales = s.CashSales,
        CardSales = s.CardSales,
        OtherSales = s.OtherSales,
        Refunds = s.Refunds,
        PaidIn = s.PaidIn,
        PaidOut = s.PaidOut,
        Drops = s.Drops,
        ExpectedCash = s.ExpectedCash,
        CountedCash = s.CountedCash,
        Variance = s.Variance,
        WasBlindCount = s.WasBlindCount,
        TransactionCount = s.TransactionCount,
        VarianceNote = s.VarianceNote,
        CurrencyCode = s.CurrencyCode,
        Movements = [.. s.Movements.Where(m => !m.IsDeleted).OrderBy(m => m.OccurredAt).Select(ToDto)],
    };

    public static CashMovementDto ToDto(CashMovement m) => new()
    {
        Id = m.Id,
        Kind = m.Kind,
        OccurredAt = m.OccurredAt,
        Amount = m.Amount,
        Reason = m.Reason,
        Reference = m.Reference,
    };

    // ═══ Access ══════════════════════════════════════════════════════════════

    public static DoorDto ToDto(Door d) => new()
    {
        Id = d.Id,
        ClubId = d.ClubId,
        ClubName = d.Club?.Name,
        AreaId = d.AreaId,
        AreaName = d.Area?.Name,
        Name = d.Name,
        Direction = d.Direction,
        ControllerId = d.ControllerId,
        ControllerName = d.Controller?.Name,
        ControllerOnline = d.Controller?.IsOnline ?? true,
        ReaderAddress = d.ReaderAddress,
        HardwareKind = d.HardwareKind,
        CountsOccupancy = d.CountsOccupancy,
        RequiresClassBooking = d.RequiresClassBooking,
        ClassBookingWindowMinutes = d.ClassBookingWindowMinutes,
        StaffOnly = d.StaffOnly,
        AntiPassbackOverride = d.AntiPassbackOverride,
        IsActive = d.IsActive,
        IsHeldOpen = d.IsHeldOpen,
        HeldOpenReason = d.HeldOpenReason,
    };

    public static AccessControllerDto ToDto(AccessController c, DateTime now) => new()
    {
        Id = c.Id,
        ClubId = c.ClubId,
        Name = c.Name,
        Vendor = c.Vendor,
        Model = c.Model,
        FirmwareVersion = c.FirmwareVersion,
        IpAddress = c.IpAddress,
        SerialNumber = c.SerialNumber,
        OfflinePolicy = c.OfflinePolicy,
        CacheSeconds = c.CacheSeconds,
        LastHeartbeatAt = c.LastHeartbeatAt,
        LastSyncAt = c.LastSyncAt,
        PendingEventCount = c.PendingEventCount,
        IsOnline = c.LastHeartbeatAt is not null &&
                   c.LastHeartbeatAt.Value.AddMinutes(c.HeartbeatTimeoutMinutes) > now,
        HeartbeatTimeoutMinutes = c.HeartbeatTimeoutMinutes,
        SecondsSinceHeartbeat = c.LastHeartbeatAt is null ? null : (int)(now - c.LastHeartbeatAt.Value).TotalSeconds,
        IsActive = c.IsActive,
        Doors = [.. c.Doors.Where(d => !d.IsDeleted).Select(ToDto)],
    };

    public static AccessRuleDto ToDto(AccessRule r) => new()
    {
        Id = r.Id,
        Name = r.Name,
        ClubId = r.ClubId,
        BalanceThreshold = r.BalanceThreshold,
        RequiresWaiver = r.RequiresWaiver,
        RequiresMedicalClearance = r.RequiresMedicalClearance,
        RespectsOccupancyCap = r.RespectsOccupancyCap,
        AntiPassback = r.AntiPassback,
        MinimumAge = r.MinimumAge,
        RequiresGuardian = r.RequiresGuardian,
        MaxVisitsPerPeriod = r.MaxVisitsPerPeriod,
        VisitLimitBasis = r.VisitLimitBasis,
        IsDefault = r.IsDefault,
        IsActive = r.IsActive,
        Windows = [.. r.Windows.Where(w => !w.IsDeleted).Select(ToDto)],
    };

    public static AccessRuleWindowDto ToDto(AccessRuleWindow w) => new()
    {
        Id = w.Id,
        DaysOfWeekMask = w.DaysOfWeekMask,
        StartsAt = w.StartsAt,
        EndsAt = w.EndsAt,
        Label = w.Label,
    };

    public static CheckInDto ToDto(CheckIn c) => new()
    {
        Id = c.Id,
        ClubId = c.ClubId,
        MemberId = c.MemberId,
        MemberName = c.Member is null ? null : FullName(c.Member),
        MemberNumber = c.Member?.MemberNumber,
        PhotoUrl = c.Member?.PhotoUrl,
        Kind = c.Kind,
        CheckedInAt = c.CheckedInAt,
        CheckedOutAt = c.CheckedOutAt,
        DurationMinutes = c.DurationMinutes,
        AutoClosed = c.AutoClosed,
        Method = c.Method,
        DoorId = c.DoorId,
        AreaId = c.AreaId,
        ClassBookingId = c.ClassBookingId,
        AppointmentId = c.AppointmentId,
        HostMemberId = c.HostMemberId,
        WasManualEntry = c.WasManualEntry,
        FeeCharged = c.FeeCharged,
    };

    public static AccessEventDto ToDto(AccessEvent e) => new()
    {
        Id = e.Id,
        ClubId = e.ClubId,
        DoorId = e.DoorId,
        OccurredAt = e.OccurredAt,
        MemberId = e.MemberId,
        StaffId = e.StaffId,
        CredentialIdentifier = e.CredentialIdentifier is null ? null : MaskCredential(e.CredentialIdentifier, e.Method),
        Method = e.Method,
        Decision = e.Decision,
        DenialReason = e.DenialReason,
        DecisionMessage = e.DecisionMessage,
        Direction = e.Direction,
        ImageUrl = e.ImageUrl,
        WasOfflineDecision = e.WasOfflineDecision,
        ReplayedAt = e.ReplayedAt,
        OverrideReason = e.OverrideReason,
        DecisionMs = e.DecisionMs,
    };

    public static VisitHistoryDto ToVisitHistory(CheckIn c, string clubName) => new()
    {
        Id = c.Id,
        CheckedInAt = c.CheckedInAt,
        CheckedOutAt = c.CheckedOutAt,
        DurationMinutes = c.DurationMinutes,
        ClubName = clubName,
        Kind = c.Kind,
        Method = c.Method,
    };

    public static DayPassDto ToDto(DayPass p, DateTime now) => new()
    {
        Id = p.Id,
        PassNumber = p.PassNumber,
        ClubId = p.ClubId,
        MemberId = p.MemberId,
        VisitorName = p.VisitorName,
        VisitorPhone = p.VisitorPhone,
        VisitorEmail = p.VisitorEmail,
        PlanId = p.PlanId,
        ValidFrom = p.ValidFrom,
        ValidTo = p.ValidTo,
        MaxEntries = p.MaxEntries,
        EntriesUsed = p.EntriesUsed,
        IsExpired = p.ValidTo < now,
        IsExhausted = p.EntriesUsed >= p.MaxEntries,
        AmountPaid = p.AmountPaid,
        WaiverSigned = p.WaiverSigned,
        IsTrial = p.IsTrial,
        CreatedLeadId = p.CreatedLeadId,
    };

    public static GuestVisitDto ToDto(GuestVisit g) => new()
    {
        Id = g.Id,
        ClubId = g.ClubId,
        HostMemberId = g.HostMemberId,
        HostMemberName = g.HostMember is null ? null : FullName(g.HostMember),
        GuestName = g.GuestName,
        GuestPhone = g.GuestPhone,
        GuestEmail = g.GuestEmail,
        GuestDateOfBirth = g.GuestDateOfBirth,
        VisitedOn = g.VisitedOn,
        WaiverSigned = g.WaiverSigned,
        UsedHostAllowance = g.UsedHostAllowance,
        FeeCharged = g.FeeCharged,
        CreatedLeadId = g.CreatedLeadId,
    };

    // ═══ Classes ═════════════════════════════════════════════════════════════

    public static ClassTypeDto ToDto(ClassType c) => new()
    {
        Id = c.Id,
        Code = c.Code,
        Name = c.Name,
        Discipline = c.Discipline,
        MarketingBlurb = c.MarketingBlurb,
        ImageUrl = c.ImageUrl,
        ColourHex = c.ColourHex,
        DisplayOrder = c.DisplayOrder,
        DefaultDurationMinutes = c.DefaultDurationMinutes,
        DefaultCapacity = c.DefaultCapacity,
        Intensity = c.Intensity,
        EquipmentNeeded = c.EquipmentNeeded,
        MinimumAge = c.MinimumAge,
        MaximumAge = c.MaximumAge,
        RequiresSkillClearance = c.RequiresSkillClearance,
        RequiredSkillId = c.RequiredSkillId,
        AllowsDropIn = c.AllowsDropIn,
        DropInPrice = c.DropInPrice,
        CreditCost = c.CreditCost,
        BookingPolicyId = c.BookingPolicyId,
        CancellationPolicyId = c.CancellationPolicyId,
        AvailableToMarketplace = c.AvailableToMarketplace,
        IsBookable = c.IsBookable,
        IsActive = c.IsActive,
    };

    public static ClassScheduleDto ToDto(ClassSchedule s) => new()
    {
        Id = s.Id,
        ClubId = s.ClubId,
        ClassTypeId = s.ClassTypeId,
        ClassTypeName = s.ClassType?.Name ?? string.Empty,
        ColourHex = s.ClassType?.ColourHex,
        RoomId = s.RoomId,
        RoomName = s.Room?.Name,
        InstructorStaffId = s.InstructorStaffId,
        DaysOfWeekMask = s.DaysOfWeekMask,
        StartsAt = s.StartsAt,
        DurationMinutes = s.DurationMinutes,
        Capacity = s.Capacity,
        MarketplaceCapacity = s.MarketplaceCapacity,
        EffectiveFrom = s.EffectiveFrom,
        EffectiveTo = s.EffectiveTo,
        RepeatEveryWeeks = s.RepeatEveryWeeks,
        GenerateAheadDays = s.GenerateAheadDays,
        GeneratedThrough = s.GeneratedThrough,
        IsPublished = s.IsPublished,
        SeasonCode = s.SeasonCode,
        IsActive = s.IsActive,
    };

    public static ClassOccurrenceSummaryDto ToSummary(ClassOccurrence o, DateTime now) => new()
    {
        Id = o.Id,
        ClubId = o.ClubId,
        ClassTypeId = o.ClassTypeId,
        ClassTypeName = o.ClassType?.Name ?? string.Empty,
        ColourHex = o.ClassType?.ColourHex,
        Discipline = o.ClassType?.Discipline,
        Intensity = o.ClassType?.Intensity ?? 3,
        RoomId = o.RoomId,
        RoomName = o.Room?.Name,
        InstructorStaffId = o.SubstituteStaffId ?? o.InstructorStaffId,
        HasSubstitute = o.SubstituteStaffId is not null,
        StartsAt = o.StartsAt,
        EndsAt = o.EndsAt,
        DurationMinutes = (int)(o.EndsAt - o.StartsAt).TotalMinutes,
        Status = o.Status,
        Capacity = o.Capacity,
        BookedCount = o.BookedCount,
        WaitlistCount = o.WaitlistCount,
        AttendedCount = o.AttendedCount,
        NoShowCount = o.NoShowCount,
        SpacesLeft = Math.Max(0, o.Capacity - o.BookedCount),
        FillPercent = Percent(o.BookedCount, o.Capacity),
        HasSpotMap = o.Room?.HasSpotMap ?? false,
        AllowsDropIn = o.ClassType?.AllowsDropIn ?? false,
        DropInPrice = o.ClassType?.DropInPrice ?? 0,
        BookingOpensAt = o.BookingOpensAt,
        BookingClosesAt = o.BookingClosesAt,
        BookingIsOpen = (o.BookingOpensAt is null || o.BookingOpensAt <= now)
                     && (o.BookingClosesAt is null || o.BookingClosesAt >= now)
                     && o.Status is ClassOccurrenceStatus.Scheduled or ClassOccurrenceStatus.Open or ClassOccurrenceStatus.Full,
        CancellationReason = o.CancellationReason,
    };

    public static ClassBookingDto ToDto(ClassBooking k) => new()
    {
        Id = k.Id,
        ClassOccurrenceId = k.ClassOccurrenceId,
        ClassName = k.ClassOccurrence?.ClassType?.Name,
        ClassStartsAt = k.ClassOccurrence?.StartsAt,
        RoomName = k.ClassOccurrence?.Room?.Name,
        MemberId = k.MemberId,
        MemberName = k.Member is null ? null : FullName(k.Member),
        MemberNumber = k.Member?.MemberNumber,
        PhotoUrl = k.Member?.PhotoUrl,
        GuestName = k.GuestName,
        Status = k.Status,
        PaymentKind = k.PaymentKind,
        Channel = k.Channel,
        BookedAt = k.BookedAt,
        CheckedInAt = k.CheckedInAt,
        CancelledAt = k.CancelledAt,
        SpotId = k.SpotId,
        SpotLabel = k.SpotLabel,
        CreditsUsed = k.CreditsUsed,
        AmountPaid = k.AmountPaid,
        PenaltyCharged = k.PenaltyCharged,
        CreditForfeited = k.CreditForfeited,
        StrikeIssued = k.StrikeIssued,
        WaitlistPosition = k.WaitlistPosition,
        PromotedAt = k.PromotedAt,
        Note = k.Note,
        VisitCount = k.Member?.TotalVisits ?? 0,
        IsFirstVisit = (k.Member?.TotalVisits ?? 0) == 0,
    };

    public static BookingPolicyDto ToDto(BookingPolicy p) => new()
    {
        Id = p.Id,
        Name = p.Name,
        ClubId = p.ClubId,
        BookingOpensDaysBefore = p.BookingOpensDaysBefore,
        BookingClosesMinutesBefore = p.BookingClosesMinutesBefore,
        MaxConcurrentBookings = p.MaxConcurrentBookings,
        MaxBookingsPerDay = p.MaxBookingsPerDay,
        MaxBookingsPerWeek = p.MaxBookingsPerWeek,
        WaitlistEnabled = p.WaitlistEnabled,
        MaxWaitlistLength = p.MaxWaitlistLength,
        HoldCreditOnWaitlist = p.HoldCreditOnWaitlist,
        WaitlistConfirmMinutes = p.WaitlistConfirmMinutes,
        PreventDuplicateSameDay = p.PreventDuplicateSameDay,
        RequiresPaymentUpFront = p.RequiresPaymentUpFront,
        IsDefault = p.IsDefault,
        IsActive = p.IsActive,
    };

    public static CancellationPolicyDto ToDto(CancellationPolicy p) => new()
    {
        Id = p.Id,
        Name = p.Name,
        ClubId = p.ClubId,
        FreeCancelHours = p.FreeCancelHours,
        LateCancelOutcome = p.LateCancelOutcome,
        LateCancelFee = p.LateCancelFee,
        NoShowOutcome = p.NoShowOutcome,
        NoShowFee = p.NoShowFee,
        NoShowGraceMinutes = p.NoShowGraceMinutes,
        StrikeThreshold = p.StrikeThreshold,
        StrikeWindowDays = p.StrikeWindowDays,
        BookingBanDays = p.BookingBanDays,
        IsDefault = p.IsDefault,
        IsActive = p.IsActive,
    };

    public static LateCancelStrikeDto ToDto(LateCancelStrike s) => new()
    {
        Id = s.Id,
        MemberId = s.MemberId,
        MemberName = s.Member is null ? null : FullName(s.Member),
        OccurredOn = s.OccurredOn,
        WasNoShow = s.WasNoShow,
        ClassName = s.ClassName,
        FeeCharged = s.FeeCharged,
        ExpiresOn = s.ExpiresOn,
        IsWaived = s.IsWaived,
        WaivedReason = s.WaivedReason,
    };

    // ═══ Appointments ════════════════════════════════════════════════════════

    public static AppointmentSummaryDto ToSummary(Appointment a) => new()
    {
        Id = a.Id,
        AppointmentNumber = a.AppointmentNumber,
        ClubId = a.ClubId,
        ServiceId = a.ServiceId,
        ServiceName = a.Service?.Name ?? string.Empty,
        Kind = a.Service?.Kind ?? AppointmentKind.PersonalTraining,
        ColourHex = a.Service?.ColourHex,
        StaffId = a.StaffId,
        MemberId = a.MemberId,
        MemberName = a.Member is null ? null : FullName(a.Member),
        MemberPhotoUrl = a.Member?.PhotoUrl,
        MemberPhone = a.Member?.Phone,
        Status = a.Status,
        StartsAt = a.StartsAt,
        EndsAt = a.EndsAt,
        DurationMinutes = (int)(a.EndsAt - a.StartsAt).TotalMinutes,
        CheckedInAt = a.CheckedInAt,
        CompletedAt = a.CompletedAt,
        ParticipantCount = 1 + a.Participants.Count(p => !p.IsDeleted),
        IsFirstSession = a.IsFirstSession,
        IsSignedOff = a.CompletedAt is not null,
        CreditsUsed = a.CreditsUsed,
        AmountPaid = a.AmountPaid,
    };

    public static AppointmentParticipantDto ToDto(AppointmentParticipant p) => new()
    {
        Id = p.Id,
        MemberId = p.MemberId,
        MemberName = p.Member is null ? string.Empty : FullName(p.Member),
        PhotoUrl = p.Member?.PhotoUrl,
        Status = p.Status,
        CheckedInAt = p.CheckedInAt,
        CreditsUsed = p.CreditsUsed,
        AmountPaid = p.AmountPaid,
        PenaltyCharged = p.PenaltyCharged,
    };

    public static SessionPackagePurchaseDto ToDto(SessionPackagePurchase p, DateTime now) => new()
    {
        Id = p.Id,
        PurchaseNumber = p.PurchaseNumber,
        MemberId = p.MemberId,
        MemberName = p.Member is null ? null : FullName(p.Member),
        ClubId = p.ClubId,
        PlanId = p.PlanId,
        ServiceId = p.ServiceId,
        StaffId = p.StaffId,
        PurchasedOn = p.PurchasedOn,
        SessionsPurchased = p.SessionsPurchased,
        SessionsUsed = p.SessionsUsed,
        SessionsRemaining = p.SessionsRemaining,
        TotalPrice = p.TotalPrice,
        PricePerSession = p.PricePerSession,
        CurrencyCode = p.CurrencyCode,
        ExpiresOn = p.ExpiresOn,
        IsExpired = p.IsExpired,
        ExpiringSoon = p.ExpiresOn is not null && !p.IsExpired && p.ExpiresOn.Value <= now.AddDays(30),
        DaysToExpiry = p.ExpiresOn is null ? null : (int)(p.ExpiresOn.Value.Date - now.Date).TotalDays,
        InvoiceId = p.InvoiceId,
        IsTransferable = p.IsTransferable,
        IsRefundable = p.IsRefundable,
        UnearnedValue = p.SessionsRemaining * p.PricePerSession,
    };

    public static SessionCreditDto ToDto(SessionCredit c) => new()
    {
        Id = c.Id,
        MemberId = c.MemberId,
        SessionPackagePurchaseId = c.SessionPackagePurchaseId,
        AgreementId = c.AgreementId,
        ServiceId = c.ServiceId,
        ClassTypeId = c.ClassTypeId,
        Kind = c.Kind,
        Granted = c.Granted,
        Used = c.Used,
        Held = c.Held,
        Remaining = c.Remaining,
        ExpiresOn = c.ExpiresOn,
        IsExpired = c.IsExpired,
        UnitValue = c.UnitValue,
    };

    public static SessionCreditSummaryDto ToCreditSummary(SessionCredit c, DateTime now, string label) => new()
    {
        Id = c.Id,
        Kind = c.Kind,
        Label = label,
        Remaining = c.Remaining,
        Held = c.Held,
        Granted = c.Granted,
        ExpiresOn = c.ExpiresOn,
        ExpiringSoon = c.ExpiresOn is not null && !c.IsExpired && c.ExpiresOn.Value <= now.AddDays(30),
    };

    public static SessionCreditMovementDto ToDto(SessionCreditMovement m) => new()
    {
        Id = m.Id,
        Kind = m.Kind,
        OccurredAt = m.OccurredAt,
        Quantity = m.Quantity,
        BalanceAfter = m.BalanceAfter,
        AppointmentId = m.AppointmentId,
        ClassBookingId = m.ClassBookingId,
        Note = m.Note,
    };

    // ═══ Training ════════════════════════════════════════════════════════════

    public static ExerciseDto ToDto(Exercise x) => new()
    {
        Id = x.Id,
        Name = x.Name,
        Category = x.Category,
        MuscleGroups = x.MuscleGroups,
        Equipment = x.Equipment,
        Instructions = x.Instructions,
        VideoUrl = x.VideoUrl,
        ImageUrl = x.ImageUrl,
        ScalingOptions = x.ScalingOptions,
        TracksPersonalRecord = x.TracksPersonalRecord,
        PrScoreType = x.PrScoreType,
        IsSystemExercise = x.IsSystemExercise,
        IsActive = x.IsActive,
    };

    public static WorkoutDto ToDto(Workout w) => new()
    {
        Id = w.Id,
        Name = w.Name,
        ClubId = w.ClubId,
        Summary = w.Summary,
        CoachNotes = w.CoachNotes,
        ScoreType = w.ScoreType,
        ScoreUnit = w.ScoreUnit,
        TimeCapSeconds = w.TimeCapSeconds,
        IsBenchmark = w.IsBenchmark,
        BenchmarkName = w.BenchmarkName,
        EstimatedMinutes = w.EstimatedMinutes,
        IsTemplate = w.IsTemplate,
        IsActive = w.IsActive,
        Sections = [.. w.Sections.Where(s => !s.IsDeleted).OrderBy(s => s.DisplayOrder).Select(ToDto)],
    };

    public static WorkoutSectionDto ToDto(WorkoutSection s) => new()
    {
        Id = s.Id,
        WorkoutId = s.WorkoutId,
        Title = s.Title,
        Kind = s.Kind,
        DisplayOrder = s.DisplayOrder,
        Rounds = s.Rounds,
        DurationSeconds = s.DurationSeconds,
        RestSeconds = s.RestSeconds,
        ScoreType = s.ScoreType,
        Instructions = s.Instructions,
        Movements = [.. s.Movements.Where(m => !m.IsDeleted).OrderBy(m => m.DisplayOrder).Select(ToDto)],
    };

    public static WorkoutMovementDto ToDto(WorkoutMovement m) => new()
    {
        Id = m.Id,
        WorkoutSectionId = m.WorkoutSectionId,
        ExerciseId = m.ExerciseId,
        MovementName = m.MovementName,
        DisplayOrder = m.DisplayOrder,
        Sets = m.Sets,
        Reps = m.Reps,
        LoadKg = m.LoadKg,
        LoadPercentOfMax = m.LoadPercentOfMax,
        DistanceMetres = m.DistanceMetres,
        Calories = m.Calories,
        DurationSeconds = m.DurationSeconds,
        RestSeconds = m.RestSeconds,
        Tempo = m.Tempo,
        ScalingNote = m.ScalingNote,
    };

    public static WorkoutResultDto ToDto(WorkoutResult r) => new()
    {
        Id = r.Id,
        MemberId = r.MemberId,
        MemberName = r.Member is null ? null : FullName(r.Member),
        MemberPhotoUrl = r.Member?.PhotoUrl,
        WorkoutId = r.WorkoutId,
        WorkoutName = r.Workout?.Name,
        ClassOccurrenceId = r.ClassOccurrenceId,
        ProgramTrackId = r.ProgramTrackId,
        ClubId = r.ClubId,
        PerformedOn = r.PerformedOn,
        ScoreType = r.ScoreType,
        TimeSeconds = r.TimeSeconds,
        Rounds = r.Rounds,
        Reps = r.Reps,
        LoadKg = r.LoadKg,
        DistanceMetres = r.DistanceMetres,
        Calories = r.Calories,
        Points = r.Points,
        Passed = r.Passed,
        NormalisedScore = r.NormalisedScore,
        ScoreDisplay = FormatScore(r),
        WasScaled = r.WasScaled,
        ScalingNote = r.ScalingNote,
        DidNotFinish = r.DidNotFinish,
        MemberNote = r.MemberNote,
        CoachNote = r.CoachNote,
        IsPersonalRecord = r.IsPersonalRecord,
    };

    public static PersonalRecordDto ToDto(PersonalRecord p) => new()
    {
        Id = p.Id,
        MemberId = p.MemberId,
        ExerciseId = p.ExerciseId,
        WorkoutId = p.WorkoutId,
        RecordName = p.RecordName,
        ScoreType = p.ScoreType,
        Value = p.Value,
        Unit = p.Unit,
        ValueDisplay = FormatValue(p.ScoreType, p.Value, p.Unit),
        RepMax = p.RepMax,
        AchievedOn = p.AchievedOn,
        PreviousValue = p.PreviousValue,
        PreviousAchievedOn = p.PreviousAchievedOn,
        Improvement = p.PreviousValue is null ? null : p.Value - p.PreviousValue,
        ImprovementDisplay = p.PreviousValue is null
            ? null
            : FormatImprovement(p.ScoreType, p.Value - p.PreviousValue.Value, p.Unit),
    };

    public static LeaderboardEntryDto ToDto(LeaderboardEntry e) => new()
    {
        Id = e.Id,
        Rank = e.Rank,
        MemberId = e.MemberId,
        MemberDisplayName = e.MemberDisplayName,
        MemberPhotoUrl = e.MemberPhotoUrl,
        Score = e.Score,
        ScoreDisplay = e.ScoreDisplay ?? e.Score.ToString("0.##"),
        WasScaled = e.WasScaled,
        Division = e.Division,
        WorkoutResultId = e.WorkoutResultId,
        AchievedOn = e.ComputedAt,
    };

    public static EffortSessionDto ToDto(EffortSession s) => new()
    {
        Id = s.Id,
        MemberId = s.MemberId,
        MemberName = s.Member is null ? null : FullName(s.Member),
        MemberPhotoUrl = s.Member?.PhotoUrl,
        ClubId = s.ClubId,
        ClassOccurrenceId = s.ClassOccurrenceId,
        StartedAt = s.StartedAt,
        EndedAt = s.EndedAt,
        DurationMinutes = s.DurationMinutes,
        EffortPoints = s.EffortPoints,
        GreyMinutes = s.GreyMinutes,
        BlueMinutes = s.BlueMinutes,
        GreenMinutes = s.GreenMinutes,
        YellowMinutes = s.YellowMinutes,
        RedMinutes = s.RedMinutes,
        AverageHeartRate = s.AverageHeartRate,
        PeakHeartRate = s.PeakHeartRate,
        CaloriesBurned = s.CaloriesBurned,
        PeakZone = s.PeakZone,
        DeviceType = s.DeviceType,
    };

    public static MemberRankDto ToDto(MemberRank r, RankLevel? next, DateTime now)
    {
        var monthsAtRank = (int)((now - r.AwardedOn).TotalDays / 30.44);
        var attendancesRequired = next?.RequiredAttendances ?? 0;
        var monthsRequired = next?.MinimumMonthsAtPrevious ?? 0;

        var attendanceProgress = attendancesRequired == 0 ? 100 : Percent(r.AttendancesAtRank, attendancesRequired);
        var timeProgress = monthsRequired == 0 ? 100 : Percent(monthsAtRank, monthsRequired);

        return new MemberRankDto
        {
            Id = r.Id,
            MemberId = r.MemberId,
            MemberName = r.Member is null ? null : FullName(r.Member),
            MemberPhotoUrl = r.Member?.PhotoUrl,
            RankLadderId = r.RankLadderId,
            RankLevelId = r.RankLevelId,
            RankName = r.RankLevel?.Name ?? string.Empty,
            ColourHex = r.RankLevel?.ColourHex,
            BadgeUrl = r.RankLevel?.BadgeUrl,
            AwardedOn = r.AwardedOn,
            Status = r.Status,
            IsCurrent = r.IsCurrent,
            AttendancesAtRank = r.AttendancesAtRank,
            NextRankName = next?.Name,
            AttendancesRequired = attendancesRequired,
            MonthsAtRank = monthsAtRank,
            MonthsRequired = monthsRequired,

            // Both gates have to be met, so progress is the lower of the two — showing the higher
            // would tell a student they are nearly there when they are months off.
            ProgressPercent = Math.Min(attendanceProgress, timeProgress),
            IsEligibleForGrading = next is not null
                && r.AttendancesAtRank >= attendancesRequired
                && monthsAtRank >= monthsRequired,
            CertificateUrl = r.CertificateUrl,
            Note = r.Note,
        };
    }

    public static SkillClearanceDto ToDto(SkillClearance c, DateTime now) => new()
    {
        Id = c.Id,
        MemberId = c.MemberId,
        MemberName = c.Member is null ? null : FullName(c.Member),
        ClubId = c.ClubId,
        SkillName = c.SkillName,
        ClearedOn = c.ClearedOn,
        ClearedByStaffId = c.ClearedByStaffId,
        ExpiresOn = c.ExpiresOn,
        IsExpired = c.ExpiresOn is not null && c.ExpiresOn < now,
        IsRevoked = c.IsRevoked,
        RevokedReason = c.RevokedReason,
        Note = c.Note,
    };

    // ═══ Assessments ═════════════════════════════════════════════════════════

    public static AssessmentDto ToDto(Assessment a) => new()
    {
        Id = a.Id,
        MemberId = a.MemberId,
        MemberName = a.Member is null ? null : FullName(a.Member),
        ClubId = a.ClubId,
        AssessmentTemplateId = a.AssessmentTemplateId,
        TemplateName = a.AssessmentTemplate?.Name,
        StaffId = a.StaffId,
        AppointmentId = a.AppointmentId,
        PerformedOn = a.PerformedOn,
        Summary = a.Summary,
        Recommendations = a.Recommendations,
        DeviceSource = a.DeviceSource,
        NextDueOn = a.NextDueOn,
        SharedWithMember = a.SharedWithMember,
        ReportUrl = a.ReportUrl,
        Values = [.. a.Values.Where(v => !v.IsDeleted).OrderBy(v => v.DisplayOrder).Select(ToDto)],
    };

    public static AssessmentValueDto ToDto(AssessmentValue v) => new()
    {
        Id = v.Id,
        AssessmentMeasureId = v.AssessmentMeasureId,
        MeasureName = v.MeasureName,
        MeasureType = v.MeasureType,
        Unit = v.Unit,
        NumericValue = v.NumericValue,
        TextValue = v.TextValue,
        BooleanValue = v.BooleanValue,
        DisplayValue = v.TextValue
                       ?? (v.BooleanValue is not null ? (v.BooleanValue.Value ? "Yes" : "No") : null)
                       ?? (v.NumericValue is not null ? $"{v.NumericValue:0.##} {v.Unit}".Trim() : "—"),
        PreviousValue = v.PreviousValue,
        Change = v.Change,
        ChangePercent = v.ChangePercent,
        NormBand = v.NormBand,
        Percentile = v.Percentile,
        Note = v.Note,
        DisplayOrder = v.DisplayOrder,
    };

    public static AssessmentTemplateDto ToDto(AssessmentTemplate t) => new()
    {
        Id = t.Id,
        Name = t.Name,
        ClubId = t.ClubId,
        Purpose = t.Purpose,
        DisplayOrder = t.DisplayOrder,
        RecommendedIntervalDays = t.RecommendedIntervalDays,
        ServiceId = t.ServiceId,
        IsSystemTemplate = t.IsSystemTemplate,
        IsActive = t.IsActive,
        Measures = [.. t.Measures.Where(m => !m.IsDeleted).OrderBy(m => m.DisplayOrder).Select(ToDto)],
    };

    public static AssessmentMeasureDto ToDto(AssessmentMeasure m) => new()
    {
        Id = m.Id,
        AssessmentTemplateId = m.AssessmentTemplateId,
        Name = m.Name,
        MeasureType = m.MeasureType,
        Direction = m.Direction,
        Unit = m.Unit,
        DisplayOrder = m.DisplayOrder,
        Grouping = m.Grouping,
        MinValue = m.MinValue,
        MaxValue = m.MaxValue,
        NormalLow = m.NormalLow,
        NormalHigh = m.NormalHigh,
        Instructions = m.Instructions,
        IsCalculated = m.IsCalculated,
        CalculationNote = m.CalculationNote,
        IsRequired = m.IsRequired,
        IsDeviceImported = m.IsDeviceImported,
        DeviceFieldName = m.DeviceFieldName,
    };

    public static MemberGoalDto ToDto(MemberGoal g, DateTime now) => new()
    {
        Id = g.Id,
        MemberId = g.MemberId,
        Title = g.Title,
        MeasureName = g.MeasureName,
        Unit = g.Unit,
        StartValue = g.StartValue,
        TargetValue = g.TargetValue,
        CurrentValue = g.CurrentValue,
        SetOn = g.SetOn,
        TargetDate = g.TargetDate,
        AchievedOn = g.AchievedOn,
        Status = g.Status,
        ProgressPercent = g.ProgressPercent,
        DaysRemaining = g.TargetDate is null ? null : (int)(g.TargetDate.Value.Date - now.Date).TotalDays,
        WhyItMatters = g.WhyItMatters,

        // On track when progress has kept pace with the calendar.
        IsOnTrack = g.TargetDate is null || g.ProgressPercent >= ElapsedPercent(g.SetOn, g.TargetDate.Value, now),
    };

    // ═══ Sales ═══════════════════════════════════════════════════════════════

    public static LeadSummaryDto ToSummary(FitnessLead l, DateTime now, int slaMinutes)
    {
        var minutesToBreach = l.FirstContactedAt is not null
            ? (int?)null
            : slaMinutes - (int)(now - l.ReceivedAt).TotalMinutes;

        return new LeadSummaryDto
        {
            Id = l.Id,
            ClubId = l.ClubId,
            FirstName = l.FirstName,
            LastName = l.LastName,
            FullName = $"{l.FirstName} {l.LastName}".Trim(),
            Phone = l.Phone,
            Email = l.Email,
            Status = l.Status,
            SourceName = l.LeadSource?.Name,
            SourceKind = l.LeadSource?.Kind ?? LeadSourceKind.Other,
            ReceivedAt = l.ReceivedAt,
            FirstContactedAt = l.FirstContactedAt,
            ResponseMinutes = l.ResponseMinutes,
            SlaBreached = l.SlaBreached,
            MinutesToSlaBreach = minutesToBreach,
            AssignedStaffId = l.AssignedStaffId,
            LastActivityAt = l.LastActivityAt,
            NextFollowUpOn = l.NextFollowUpOn,
            FollowUpOverdue = l.NextFollowUpOn is not null && l.NextFollowUpOn < now
                              && l.Status is not (LeadStatus.Won or LeadStatus.Lost),
            ContactAttempts = l.ContactAttempts,
            AgeDays = (int)(now.Date - l.ReceivedAt.Date).TotalDays,
            TourBookedFor = l.TourBookedFor,
            TrialEndsOn = l.TrialEndsOn,
            Goal = l.Goal,
            EstimatedValue = l.WonValue,
        };
    }

    public static LeadActivityDto ToDto(LeadActivity a) => new()
    {
        Id = a.Id,
        LeadId = a.LeadId,
        Kind = a.Kind,
        OccurredAt = a.OccurredAt,
        Summary = a.Summary,
        Outcome = a.Outcome,
        StaffId = a.StaffId,
        StaffName = a.StaffName,
        FromStatus = a.FromStatus,
        ToStatus = a.ToStatus,
        WasSuccessfulContact = a.WasSuccessfulContact,
        FollowUpOn = a.FollowUpOn,
    };

    public static TourDto ToDto(Tour t) => new()
    {
        Id = t.Id,
        LeadId = t.LeadId,
        LeadName = t.Lead is null ? null : $"{t.Lead.FirstName} {t.Lead.LastName}".Trim(),
        LeadPhone = t.Lead?.Phone,
        ClubId = t.ClubId,
        StaffId = t.StaffId,
        ScheduledFor = t.ScheduledFor,
        DurationMinutes = t.DurationMinutes,
        ArrivedAt = t.ArrivedAt,
        CompletedAt = t.CompletedAt,
        WasNoShow = t.WasNoShow,
        WasCancelled = t.WasCancelled,
        CancellationReason = t.CancellationReason,
        ConvertedOnDay = t.ConvertedOnDay,
        Notes = t.Notes,
        ReminderSent = t.ReminderSent,
    };

    public static TrialPassDto ToDto(TrialPass t, DateTime now) => new()
    {
        Id = t.Id,
        LeadId = t.LeadId,
        LeadName = t.Lead is null ? null : $"{t.Lead.FirstName} {t.Lead.LastName}".Trim(),
        MemberId = t.MemberId,
        ClubId = t.ClubId,
        PlanId = t.PlanId,
        StartsOn = t.StartsOn,
        EndsOn = t.EndsOn,
        VisitsAllowed = t.VisitsAllowed,
        VisitsUsed = t.VisitsUsed,
        IsExpired = t.EndsOn < now,
        DaysRemaining = (int)(t.EndsOn.Date - now.Date).TotalDays,
        Price = t.Price,
        Converted = t.Converted,
        ConvertedOn = t.ConvertedOn,
    };

    public static ReferralDto ToDto(Referral r) => new()
    {
        Id = r.Id,
        ReferrerMemberId = r.ReferrerMemberId,
        ReferrerName = r.ReferrerMember is null ? null : FullName(r.ReferrerMember),
        ClubId = r.ClubId,
        ReferredName = r.ReferredName,
        ReferredPhone = r.ReferredPhone,
        ReferredEmail = r.ReferredEmail,
        LeadId = r.LeadId,
        ReferredMemberId = r.ReferredMemberId,
        ReferredOn = r.ReferredOn,
        ReferralCode = r.ReferralCode,
        Converted = r.Converted,
        ConvertedOn = r.ConvertedOn,
        ReferrerRewardValue = r.ReferrerRewardValue,
        ReferrerRewardPoints = r.ReferrerRewardPoints,
        ReferrerRewarded = r.ReferrerRewarded,
        ReferredRewardValue = r.ReferredRewardValue,
        ReferredRewarded = r.ReferredRewarded,
    };

    public static LeadSourceDto ToDto(LeadSource s) => new()
    {
        Id = s.Id,
        Name = s.Name,
        Kind = s.Kind,
        ClubId = s.ClubId,
        DisplayOrder = s.DisplayOrder,
        MonthlyCost = s.MonthlyCost,
        TrackingCode = s.TrackingCode,
        IsActive = s.IsActive,
    };

    public static LossReasonDto ToDto(LossReason r) => new()
    {
        Id = r.Id,
        Name = r.Name,
        DisplayOrder = r.DisplayOrder,
        Category = r.Category,
        RequiresNote = r.RequiresNote,
        IsActive = r.IsActive,
    };

    // ═══ Retention ═══════════════════════════════════════════════════════════

    public static ChurnScoreDto ToDto(ChurnScore c) => new()
    {
        Id = c.Id,
        MemberId = c.MemberId,
        MemberName = c.Member is null ? string.Empty : FullName(c.Member),
        MemberNumber = c.Member?.MemberNumber,
        PhotoUrl = c.Member?.PhotoUrl,
        Phone = c.Member?.Phone,
        Email = c.Member?.Email,
        ClubId = c.ClubId,
        ComputedOn = c.ComputedOn,
        Band = c.Band,
        Score = c.Score,
        PreviousBand = c.PreviousBand,
        PreviousScore = c.PreviousScore,
        BandWorsened = c.BandWorsened,
        DaysSinceLastVisit = c.DaysSinceLastVisit,
        VisitsPerWeekNow = c.VisitsPerWeekNow,
        VisitsPerWeekBaseline = c.VisitsPerWeekBaseline,
        TenureDays = c.TenureDays,
        HasUpcomingBooking = c.HasUpcomingBooking,
        HasOutstandingBalance = c.HasOutstandingBalance,
        HasFailedPayment = c.HasFailedPayment,
        DaysToContractEnd = c.DaysToContractEnd,
        OwnerStaffId = c.OwnerStaffId,
        IsActioned = c.IsActioned,
        ActionedOn = c.ActionedOn,
        Factors = [.. c.Factors.Where(f => !f.IsDeleted).OrderByDescending(f => f.Weight).Select(ToDto)],
        HeadlineReason = c.Factors.Where(f => !f.IsDeleted).OrderByDescending(f => f.Weight)
            .Select(f => f.Explanation).FirstOrDefault(),
        SuggestedAction = c.Factors.Where(f => !f.IsDeleted).OrderByDescending(f => f.Weight)
            .Select(f => f.SuggestedAction).FirstOrDefault(a => a is not null),
    };

    public static ChurnFactorDto ToDto(ChurnFactor f) => new()
    {
        Kind = f.Kind,
        Weight = f.Weight,
        Explanation = f.Explanation,
        SuggestedAction = f.SuggestedAction,
    };

    public static RetentionTaskDto ToDto(RetentionTask t, DateTime now) => new()
    {
        Id = t.Id,
        MemberId = t.MemberId,
        MemberName = t.Member is null ? string.Empty : FullName(t.Member),
        MemberPhone = t.Member?.Phone,
        PhotoUrl = t.Member?.PhotoUrl,
        ClubId = t.ClubId,
        AssignedStaffId = t.AssignedStaffId,
        Title = t.Title,
        Detail = t.Detail,
        Trigger = t.Trigger,
        ChurnScoreId = t.ChurnScoreId,
        DueOn = t.DueOn,
        Priority = t.Priority,
        IsOverdue = t.CompletedAt is null && t.DueOn < now,
        CompletedAt = t.CompletedAt,
        Outcome = t.Outcome,
        IsDismissed = t.IsDismissed,
    };

    public static EngagementJourneyDto ToDto(EngagementJourney j) => new()
    {
        Id = j.Id,
        Name = j.Name,
        ClubId = j.ClubId,
        Trigger = j.Trigger,
        TriggerThresholdDays = j.TriggerThresholdDays,
        SegmentId = j.SegmentId,
        IsActive = j.IsActive,
        PreventReEnrolment = j.PreventReEnrolment,
        ReEnrolmentCooldownDays = j.ReEnrolmentCooldownDays,
        EnrolledCount = j.EnrolledCount,
        CompletedCount = j.CompletedCount,
        SuccessMetric = j.SuccessMetric,
        SuccessCount = j.SuccessCount,
        SuccessRatePercent = Percent(j.SuccessCount, j.EnrolledCount),
        Steps = [.. j.Steps.Where(s => !s.IsDeleted).OrderBy(s => s.StepNumber).Select(ToDto)],
    };

    public static JourneyStepDto ToDto(JourneyStep s) => new()
    {
        Id = s.Id,
        EngagementJourneyId = s.EngagementJourneyId,
        StepNumber = s.StepNumber,
        Kind = s.Kind,
        DelayHours = s.DelayHours,
        Channel = s.Channel,
        MessageTemplateId = s.MessageTemplateId,
        ConditionExpression = s.ConditionExpression,
        OnFalseStepNumber = s.OnFalseStepNumber,
        TagToApply = s.TagToApply,
        OfferPromotionRuleId = s.OfferPromotionRuleId,
        LoyaltyPointsToGrant = s.LoyaltyPointsToGrant,
        TaskTitle = s.TaskTitle,
        TaskAssignStaffId = s.TaskAssignStaffId,
    };

    public static CampaignDto ToDto(Campaign c) => new()
    {
        Id = c.Id,
        Name = c.Name,
        ClubId = c.ClubId,
        Channel = c.Channel,
        MessageTemplateId = c.MessageTemplateId,
        SegmentId = c.SegmentId,
        ScheduledFor = c.ScheduledFor,
        SentAt = c.SentAt,
        RecipientCount = c.RecipientCount,
        SentCount = c.SentCount,
        DeliveredCount = c.DeliveredCount,
        OpenedCount = c.OpenedCount,
        ClickedCount = c.ClickedCount,
        FailedCount = c.FailedCount,
        SuppressedCount = c.SuppressedCount,
        OpenRatePercent = Percent(c.OpenedCount, c.DeliveredCount),
        ClickRatePercent = Percent(c.ClickedCount, c.DeliveredCount),
        Cost = c.Cost,
        LeadsGenerated = c.LeadsGenerated,
        JoinsAttributed = c.JoinsAttributed,
        RevenueAttributed = c.RevenueAttributed,
        ReturnOnSpend = c.Cost > 0 ? c.RevenueAttributed / c.Cost : null,
        PromotionRuleId = c.PromotionRuleId,
        IsSent = c.IsSent,
        IsCancelled = c.IsCancelled,
    };

    public static MessageTemplateDto ToDto(MessageTemplate t) => new()
    {
        Id = t.Id,
        Name = t.Name,
        ClubId = t.ClubId,
        Channel = t.Channel,
        Subject = t.Subject,
        Body = t.Body,
        PlainTextBody = t.PlainTextBody,
        LanguageCode = t.LanguageCode,
        Purpose = t.Purpose,
        IsTransactional = t.IsTransactional,
        IsSystemTemplate = t.IsSystemTemplate,
        IsActive = t.IsActive,
        AvailableMergeFields = [
            "member.firstName", "member.lastName", "member.memberNumber", "member.balance",
            "club.name", "club.phone", "club.address",
            "agreement.planName", "agreement.price", "agreement.nextBillingOn",
            "booking.className", "booking.startsAt", "booking.instructor",
            "invoice.number", "invoice.total", "invoice.dueOn", "invoice.payLink",
        ],
    };

    public static MessageLogDto ToDto(MessageLog m) => new()
    {
        Id = m.Id,
        MemberId = m.MemberId,
        LeadId = m.LeadId,
        ClubId = m.ClubId,
        Channel = m.Channel,
        Status = m.Status,
        Recipient = m.Recipient,
        Subject = m.Subject,
        BodyPreview = m.BodyPreview,
        QueuedAt = m.QueuedAt,
        SentAt = m.SentAt,
        DeliveredAt = m.DeliveredAt,
        OpenedAt = m.OpenedAt,
        FailureReason = m.FailureReason,
        Cost = m.Cost,
    };

    public static SegmentDto ToDto(Segment s) => new()
    {
        Id = s.Id,
        Name = s.Name,
        ClubId = s.ClubId,
        DefinitionJson = s.DefinitionJson,
        LastCount = s.LastCount,
        LastCountedAt = s.LastCountedAt,
        IsSystemSegment = s.IsSystemSegment,
        IsActive = s.IsActive,
        Explanation = s.Description,
    };

    public static LoyaltyAccountDto ToDto(LoyaltyAccount a) => new()
    {
        Id = a.Id,
        MemberId = a.MemberId,
        MemberName = a.Member is null ? null : FullName(a.Member),
        ClubId = a.ClubId,
        PointsBalance = a.PointsBalance,
        LifetimePoints = a.LifetimePoints,
        PointsRedeemed = a.PointsRedeemed,
        PointsExpired = a.PointsExpired,
        TierId = a.TierId,
        TierName = a.Tier?.Name,
        TierColour = a.Tier?.ColourHex,
        TierAchievedOn = a.TierAchievedOn,
        PointsToNextTier = a.PointsToNextTier,
        NextExpiryOn = a.NextExpiryOn,
        PointsExpiringSoon = a.PointsExpiringSoon,
    };

    public static LoyaltyTransactionDto ToDto(LoyaltyTransaction t) => new()
    {
        Id = t.Id,
        Kind = t.Kind,
        OccurredAt = t.OccurredAt,
        Points = t.Points,
        BalanceAfter = t.BalanceAfter,
        Reason = t.Reason,
        RedemptionValue = t.RedemptionValue,
        ExpiresOn = t.ExpiresOn,
    };

    public static LoyaltyTierDto ToDto(LoyaltyTier t) => new()
    {
        Id = t.Id,
        Name = t.Name,
        ClubId = t.ClubId,
        Ordinal = t.Ordinal,
        PointsRequired = t.PointsRequired,
        ColourHex = t.ColourHex,
        BadgeUrl = t.BadgeUrl,
        EarnMultiplier = t.EarnMultiplier,
        Benefits = t.Benefits,
        RetentionMonths = t.RetentionMonths,
        IsActive = t.IsActive,
    };

    public static ChallengeDto ToDto(Challenge c, DateTime now) => new()
    {
        Id = c.Id,
        Name = c.Name,
        ClubId = c.ClubId,
        Blurb = c.Blurb,
        ImageUrl = c.ImageUrl,
        Metric = c.Metric,
        CustomMetricName = c.CustomMetricName,
        Unit = c.Unit,
        StartsOn = c.StartsOn,
        EndsOn = c.EndsOn,
        IsRunning = c.StartsOn <= now && c.EndsOn >= now,
        DaysRemaining = (int)(c.EndsOn.Date - now.Date).TotalDays,
        TargetValue = c.TargetValue,
        IsTeamBased = c.IsTeamBased,
        IsOpenToAll = c.IsOpenToAll,
        SegmentId = c.SegmentId,
        EntryFee = c.EntryFee,
        Prize = c.Prize,
        PointsForCompletion = c.PointsForCompletion,
        ParticipantCount = c.ParticipantCount,
        CompletedCount = c.CompletedCount,
        IsPublished = c.IsPublished,
        IsActive = c.IsActive,
    };

    public static ChallengeParticipantDto ToDto(ChallengeParticipant p, decimal? target) => new()
    {
        Id = p.Id,
        ChallengeId = p.ChallengeId,
        MemberId = p.MemberId,
        MemberName = p.Member is null ? string.Empty : FullName(p.Member),
        PhotoUrl = p.Member?.PhotoUrl,
        JoinedAt = p.JoinedAt,
        CurrentValue = p.CurrentValue,
        ValueDisplay = p.CurrentValue.ToString("0.##"),
        Rank = p.Rank,
        TeamName = p.TeamName,
        HasCompleted = p.HasCompleted,
        CompletedAt = p.CompletedAt,
        ProgressPercent = target is > 0 ? Percent(p.CurrentValue, target.Value) : 0,
        LastProgressAt = p.LastProgressAt,
    };

    public static BadgeDto ToDto(Badge b) => new()
    {
        Id = b.Id,
        Name = b.Name,
        ClubId = b.ClubId,
        Blurb = b.Blurb,
        IconUrl = b.IconUrl,
        ColourHex = b.ColourHex,
        CriteriaDescription = b.CriteriaDescription,
        PointsAwarded = b.PointsAwarded,
        DisplayOrder = b.DisplayOrder,
        IsAutomatic = b.IsAutomatic,
        IsActive = b.IsActive,
    };

    public static MemberBadgeDto ToDto(MemberBadge m) => new()
    {
        Id = m.Id,
        MemberId = m.MemberId,
        BadgeId = m.BadgeId,
        BadgeName = m.Badge?.Name ?? string.Empty,
        IconUrl = m.Badge?.IconUrl,
        ColourHex = m.Badge?.ColourHex,
        EarnedOn = m.EarnedOn,
        Context = m.Context,
        TimesEarned = m.TimesEarned,
    };

    public static NpsResponseDto ToDto(NpsResponse n) => new()
    {
        Id = n.Id,
        MemberId = n.MemberId,
        MemberName = n.Member is null ? null : FullName(n.Member),
        MemberPhone = n.Member?.Phone,
        ClubId = n.ClubId,
        Score = n.Score,
        Band = n.Band,
        Comment = n.Comment,
        Trigger = n.Trigger,
        ClassOccurrenceId = n.ClassOccurrenceId,
        StaffId = n.StaffId,
        RespondedAt = n.RespondedAt,
        FollowedUp = n.FollowedUp,
        FollowedUpAt = n.FollowedUpAt,
        FollowUpNote = n.FollowUpNote,
        NeedsFollowUp = n.Score <= 6 && !n.FollowedUp,
    };

    public static FeedbackDto ToDto(Feedback f) => new()
    {
        Id = f.Id,
        MemberId = f.MemberId,
        ClubId = f.ClubId,
        Category = f.Category,
        Body = f.Body,
        Rating = f.Rating,
        SubmittedAt = f.SubmittedAt,
        Channel = f.Channel,
        IsAnonymous = f.IsAnonymous,
        IsActioned = f.IsActioned,
        ActionNote = f.ActionNote,
    };

    public static AnnouncementDto ToDto(Announcement a, DateTime now) => new()
    {
        Id = a.Id,
        ClubId = a.ClubId,
        Title = a.Title,
        Body = a.Body,
        ImageUrl = a.ImageUrl,
        ShowFrom = a.ShowFrom,
        ShowUntil = a.ShowUntil,
        ShowOnKiosk = a.ShowOnKiosk,
        ShowInApp = a.ShowInApp,
        ShowOnClubScreens = a.ShowOnClubScreens,
        IsUrgent = a.IsUrgent,
        SegmentId = a.SegmentId,
        IsPublished = a.IsPublished,
        IsLive = a.IsPublished && a.ShowFrom <= now && (a.ShowUntil is null || a.ShowUntil >= now),
    };

    // ═══ Staff ═══════════════════════════════════════════════════════════════

    public static StaffSummaryDto ToSummary(FitnessStaff s, DateTime now)
    {
        var certs = s.Certifications.Where(c => !c.IsDeleted).ToList();
        var worst = certs.Count == 0
            ? CertificationStatus.Valid
            : certs.Select(c => CertStatus(c, now)).Max();

        return new StaffSummaryDto
        {
            Id = s.Id,
            EmployeeId = s.EmployeeId,
            UserId = s.UserId,
            ClubId = s.ClubId,
            ClubName = s.Club?.Name,
            FirstName = s.FirstName,
            LastName = s.LastName,
            FullName = $"{s.FirstName} {s.LastName}".Trim(),
            DisplayName = s.DisplayName,
            PhotoUrl = s.PhotoUrl,
            Phone = s.Phone,
            Email = s.Email,
            RoleKind = s.RoleKind,
            StartedOn = s.StartedOn,
            LeftOn = s.LeftOn,
            IsContractor = s.IsContractor,
            IsBookable = s.IsBookable,
            IsActive = s.IsActive,
            CertificationStatus = worst,
            ExpiringCertifications = certs.Count(c => CertStatus(c, now) is CertificationStatus.ExpiringSoon or CertificationStatus.Expired),
        };
    }

    public static StaffCertificationDto ToDto(StaffCertification c, DateTime now) => new()
    {
        Id = c.Id,
        StaffId = c.StaffId,
        StaffName = c.Staff is null ? null : $"{c.Staff.FirstName} {c.Staff.LastName}".Trim(),
        Name = c.Name,
        Category = c.Category,
        IssuingBody = c.IssuingBody,
        ReferenceNumber = c.ReferenceNumber,
        IssuedOn = c.IssuedOn,
        ExpiresOn = c.ExpiresOn,
        Status = CertStatus(c, now),
        DaysToExpiry = c.ExpiresOn is null ? null : (int)(c.ExpiresOn.Value.Date - now.Date).TotalDays,
        DocumentUrl = c.DocumentUrl,
        BlocksWorkOnExpiry = c.BlocksWorkOnExpiry,
    };

    public static StaffRoleDto ToDto(StaffRole r) => new()
    {
        Id = r.Id,
        Name = r.Name,
        ClubId = r.ClubId,
        BaseKind = r.BaseKind,
        Permissions = [.. r.Permissions.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)],
        DiscountLimitPercent = r.DiscountLimitPercent,
        RefundLimit = r.RefundLimit,
        WriteOffLimit = r.WriteOffLimit,
        CanOverridePolicies = r.CanOverridePolicies,
        CanViewMedicalData = r.CanViewMedicalData,
        CanExportMemberData = r.CanExportMemberData,
        IsSystemRole = r.IsSystemRole,
        IsActive = r.IsActive,
    };

    public static ShiftDto ToDto(Shift s) => new()
    {
        Id = s.Id,
        ClubId = s.ClubId,
        Title = s.Title,
        Position = s.Position,
        StartsAt = s.StartsAt,
        EndsAt = s.EndsAt,
        BreakMinutes = s.BreakMinutes,
        Hours = Math.Round((decimal)(s.EndsAt - s.StartsAt).TotalHours - s.BreakMinutes / 60m, 2),
        Status = s.Status,
        RequiredHeadcount = s.RequiredHeadcount,
        AssignedHeadcount = s.Assignments.Count(a => !a.IsDeleted),
        IsUnderStaffed = s.Assignments.Count(a => !a.IsDeleted) < s.RequiredHeadcount,
        RequiredCertification = s.RequiredCertification,
        IsPublished = s.IsPublished,
        Note = s.Note,
        Assignments = [.. s.Assignments.Where(a => !a.IsDeleted).Select(ToDto)],
    };

    public static ShiftAssignmentDto ToDto(ShiftAssignment a) => new()
    {
        Id = a.Id,
        ShiftId = a.ShiftId,
        StaffId = a.StaffId,
        StaffName = a.Staff is null ? string.Empty : $"{a.Staff.FirstName} {a.Staff.LastName}".Trim(),
        PhotoUrl = a.Staff?.PhotoUrl,
        Status = a.Status,
        ConfirmedAt = a.ConfirmedAt,
        ClockedInAt = a.ClockedInAt,
        ClockedOutAt = a.ClockedOutAt,
        WasNoShow = a.WasNoShow,
        WasLate = a.WasLate,
        LateMinutes = a.LateMinutes,
        Note = a.Note,
    };

    public static TimeClockEntryDto ToDto(TimeClockEntry t) => new()
    {
        Id = t.Id,
        StaffId = t.StaffId,
        StaffName = t.Staff is null ? string.Empty : $"{t.Staff.FirstName} {t.Staff.LastName}".Trim(),
        ClubId = t.ClubId,
        ShiftAssignmentId = t.ShiftAssignmentId,
        ClockedInAt = t.ClockedInAt,
        ClockedOutAt = t.ClockedOutAt,
        BreakMinutes = t.BreakMinutes,
        WorkedMinutes = t.WorkedMinutes,
        WorkedHours = t.WorkedMinutes is null ? null : Math.Round(t.WorkedMinutes.Value / 60m, 2),
        Device = t.Device,
        GeofencePassed = t.GeofencePassed,
        IsApproved = t.IsApproved,
        WasEdited = t.WasEdited,
        EditNote = t.EditNote,
    };

    public static CommissionRuleDto ToDto(CommissionRule r) => new()
    {
        Id = r.Id,
        Name = r.Name,
        ClubId = r.ClubId,
        StaffId = r.StaffId,
        AppliesToRole = r.AppliesToRole,
        Basis = r.Basis,
        RatePerUnit = r.RatePerUnit,
        Percentage = r.Percentage,
        Threshold = r.Threshold,
        AcceleratedRate = r.AcceleratedRate,
        PeriodCap = r.PeriodCap,
        ServiceId = r.ServiceId,
        ClassTypeId = r.ClassTypeId,
        PlanId = r.PlanId,
        EffectiveFrom = r.EffectiveFrom,
        EffectiveTo = r.EffectiveTo,
        Priority = r.Priority,
        IsActive = r.IsActive,
        Explanation = DescribeCommission(r),
    };

    public static CommissionAccrualDto ToDto(CommissionAccrual a) => new()
    {
        Id = a.Id,
        StaffId = a.StaffId,
        StaffName = a.Staff is null ? null : $"{a.Staff.FirstName} {a.Staff.LastName}".Trim(),
        ClubId = a.ClubId,
        CommissionRuleId = a.CommissionRuleId,
        Basis = a.Basis,
        EarnedOn = a.EarnedOn,
        Amount = a.Amount,
        CurrencyCode = a.CurrencyCode,
        BaseValue = a.BaseValue,
        Quantity = a.Quantity,
        Narrative = a.Narrative,
        MemberId = a.MemberId,
        SourceEntityId = a.SourceEntityId,
        SourceEntityType = a.SourceEntityType,
        DrillRoute = a.SourceEntityType switch
        {
            "Appointment" => $"/fitness/appointments/{a.SourceEntityId}",
            "Agreement" => $"/fitness/agreements/{a.SourceEntityId}",
            "Sale" => $"/fitness/pos/{a.SourceEntityId}",
            _ => null,
        },
        IsReversed = a.IsReversed,
        ReversalReason = a.ReversalReason,
    };

    public static CommissionStatementDto ToDto(CommissionStatement s) => new()
    {
        Id = s.Id,
        StatementNumber = s.StatementNumber,
        StaffId = s.StaffId,
        StaffName = s.Staff is null ? string.Empty : $"{s.Staff.FirstName} {s.Staff.LastName}".Trim(),
        PhotoUrl = s.Staff?.PhotoUrl,
        ClubId = s.ClubId,
        PeriodStart = s.PeriodStart,
        PeriodEnd = s.PeriodEnd,
        Status = s.Status,
        SessionCommission = s.SessionCommission,
        ClassCommission = s.ClassCommission,
        SalesCommission = s.SalesCommission,
        RetailCommission = s.RetailCommission,
        Bonus = s.Bonus,
        Adjustments = s.Adjustments,
        Total = s.Total,
        CurrencyCode = s.CurrencyCode,
        SessionsDelivered = s.SessionsDelivered,
        ClassesTaught = s.ClassesTaught,
        MembershipsSold = s.MembershipsSold,
        PackagesSold = s.PackagesSold,
        SubmittedAt = s.SubmittedAt,
        ApprovedAt = s.ApprovedAt,
        RejectionNote = s.RejectionNote,
        ExportedAt = s.ExportedAt,
        PayrollReference = s.PayrollReference,
    };

    public static BookableStaffDto ToDto(BookableStaff b) => new()
    {
        Id = b.Id,
        StaffId = b.StaffId,
        ClubId = b.ClubId,
        DisplayName = b.DisplayName,
        PhotoUrl = b.PhotoUrl,
        Bio = b.Bio,
        Specialities = b.Specialities,
        HourlyRate = b.HourlyRate,
        BookableOnline = b.BookableOnline,
        DefaultBufferMinutes = b.DefaultBufferMinutes,
        BookingWindowDays = b.BookingWindowDays,
        IsContractor = b.IsContractor,
        MaxClientsPerDay = b.MaxClientsPerDay,
        AcceptingNewClients = b.AcceptingNewClients,
        IsActive = b.IsActive,
        ServiceIds = ParseGuidList(b.ServiceIds),
        Availability = [.. b.Availability.Where(a => !a.IsDeleted).Select(ToDto)],
    };

    public static StaffAvailabilityDto ToDto(StaffAvailability a) => new()
    {
        Id = a.Id,
        BookableStaffId = a.BookableStaffId,
        ClubId = a.ClubId,
        DayOfWeek = a.DayOfWeek,
        StartsAt = a.StartsAt,
        EndsAt = a.EndsAt,
        BreakStartsAt = a.BreakStartsAt,
        BreakEndsAt = a.BreakEndsAt,
        EffectiveFrom = a.EffectiveFrom,
        EffectiveTo = a.EffectiveTo,
    };

    // ═══ Facility ════════════════════════════════════════════════════════════

    public static LockerDto ToDto(Locker l, LockerAssignment? current, DateTime now) => new()
    {
        Id = l.Id,
        LockerBankId = l.LockerBankId,
        BankName = l.LockerBank?.Name,
        ClubId = l.ClubId,
        Number = l.Number,
        Size = l.Size,
        Status = l.Status,
        LockType = l.LockType,
        KeyNumber = l.KeyNumber,
        MonthlyRate = l.MonthlyRate,
        AnnualRate = l.AnnualRate,
        Deposit = l.Deposit,
        OutOfOrderNote = l.OutOfOrderNote,
        LastCleanedOn = l.LastCleanedOn,
        CurrentAssignmentId = current?.Id,
        RentedByMemberId = current?.MemberId,
        RentedByName = current?.Member is null ? null : FullName(current.Member),
        RentalEndsOn = current?.EndsOn,
        RentalExpired = current?.EndsOn is not null && current.EndsOn < now,
        RentalExpiringSoon = current?.EndsOn is not null && current.EndsOn >= now && current.EndsOn <= now.AddDays(14),
    };

    public static LockerAssignmentDto ToDto(LockerAssignment a, DateTime now) => new()
    {
        Id = a.Id,
        LockerId = a.LockerId,
        LockerNumber = a.Locker?.Number,
        MemberId = a.MemberId,
        MemberName = a.Member is null ? null : FullName(a.Member),
        MemberPhone = a.Member?.Phone,
        ClubId = a.ClubId,
        StartsOn = a.StartsOn,
        EndsOn = a.EndsOn,
        ReleasedOn = a.ReleasedOn,
        IsDayUse = a.IsDayUse,
        IsExpired = a.EndsOn is not null && a.EndsOn < now && a.ReleasedOn is null,
        DaysToExpiry = a.EndsOn is null ? null : (int)(a.EndsOn.Value.Date - now.Date).TotalDays,
        Rate = a.Rate,
        DepositHeld = a.DepositHeld,
        DepositReturned = a.DepositReturned,
        AutoRenews = a.AutoRenews,
        NextBillingOn = a.NextBillingOn,
        ExpiryNoticeSent = a.ExpiryNoticeSent,
        WasReclaimed = a.WasReclaimed,
        KeyIssued = a.KeyIssued,
        KeyReturned = a.KeyReturned,
    };

    public static BookableResourceDto ToDto(BookableResource r) => new()
    {
        Id = r.Id,
        ClubId = r.ClubId,
        AreaId = r.AreaId,
        Name = r.Name,
        Kind = r.Kind,
        Capacity = r.Capacity,
        DisplayOrder = r.DisplayOrder,
        ColourHex = r.ColourHex,
        SlotMinutes = r.SlotMinutes,
        BufferMinutes = r.BufferMinutes,
        MemberRate = r.MemberRate,
        NonMemberRate = r.NonMemberRate,
        PeakSurcharge = r.PeakSurcharge,
        BookingWindowDays = r.BookingWindowDays,
        MaxConcurrentBookingsPerMember = r.MaxConcurrentBookingsPerMember,
        FreeCancelHours = r.FreeCancelHours,
        LateCancelOutcome = r.LateCancelOutcome,
        NoShowOutcome = r.NoShowOutcome,
        LinkedDoorId = r.LinkedDoorId,
        EquipmentAssetId = r.EquipmentAssetId,
        BookableOnline = r.BookableOnline,
        IsOutOfService = r.IsOutOfService,
        OutOfServiceNote = r.OutOfServiceNote,
        IsActive = r.IsActive,
        SlotRules = [.. r.SlotRules.Where(s => !s.IsDeleted).Select(ToDto)],
    };

    public static ResourceSlotRuleDto ToDto(ResourceSlotRule s) => new()
    {
        Id = s.Id,
        BookableResourceId = s.BookableResourceId,
        DaysOfWeekMask = s.DaysOfWeekMask,
        StartsAt = s.StartsAt,
        EndsAt = s.EndsAt,
        IsPeak = s.IsPeak,
        RateOverride = s.RateOverride,
        IsBlocked = s.IsBlocked,
        BlockReason = s.BlockReason,
        EffectiveFrom = s.EffectiveFrom,
        EffectiveTo = s.EffectiveTo,
    };

    public static ResourceBookingDto ToDto(ResourceBooking b) => new()
    {
        Id = b.Id,
        BookingNumber = b.BookingNumber,
        BookableResourceId = b.BookableResourceId,
        ResourceName = b.BookableResource?.Name ?? string.Empty,
        ResourceKind = b.BookableResource?.Kind ?? ResourceKind.Other,
        ColourHex = b.BookableResource?.ColourHex,
        ClubId = b.ClubId,
        MemberId = b.MemberId,
        MemberName = b.Member is null ? null : FullName(b.Member),
        MemberPhone = b.Member?.Phone,
        GuestName = b.GuestName,
        StartsAt = b.StartsAt,
        EndsAt = b.EndsAt,
        DurationMinutes = (int)(b.EndsAt - b.StartsAt).TotalMinutes,
        Status = b.Status,
        Channel = b.Channel,
        ParticipantCount = b.ParticipantCount,
        Amount = b.Amount,
        PenaltyCharged = b.PenaltyCharged,
        CheckedInAt = b.CheckedInAt,
        CancelledAt = b.CancelledAt,
        CancellationReason = b.CancellationReason,
        Note = b.Note,
    };

    public static EquipmentAssetDto ToDto(EquipmentAsset a, DateTime now) => new()
    {
        Id = a.Id,
        Code = a.Code,
        ClubId = a.ClubId,
        AreaId = a.AreaId,
        Name = a.Name,
        Category = a.Category,
        Manufacturer = a.Manufacturer,
        Model = a.Model,
        SerialNumber = a.SerialNumber,
        AssetTag = a.AssetTag,
        Status = a.Status,
        PurchasedOn = a.PurchasedOn,
        PurchaseCost = a.PurchaseCost,
        SupplierId = a.SupplierId,
        WarrantyEndsOn = a.WarrantyEndsOn,
        InWarranty = a.WarrantyEndsOn is not null && a.WarrantyEndsOn >= now,
        ServiceContractReference = a.ServiceContractReference,
        ServiceContractEndsOn = a.ServiceContractEndsOn,
        InstalledOn = a.InstalledOn,
        RetiredOn = a.RetiredOn,
        AgeMonths = a.PurchasedOn is null ? null : (int)((now - a.PurchasedOn.Value).TotalDays / 30.44),
        UsageHours = a.UsageHours,
        UsageReadOn = a.UsageReadOn,
        LastServicedOn = a.LastServicedOn,
        NextServiceDueOn = a.NextServiceDueOn,
        ServiceOverdue = a.NextServiceDueOn is not null && a.NextServiceDueOn < now,
        TotalMaintenanceCost = a.TotalMaintenanceCost,
        TotalDowntimeHours = a.TotalDowntimeHours,
        CostOfOwnership = a.PurchaseCost + a.TotalMaintenanceCost,
        QrCode = a.QrCode,
        OutOfServiceNote = a.OutOfServiceNote,
        OutOfServiceSince = a.OutOfServiceSince,
        DaysOutOfService = a.OutOfServiceSince is null ? null : (int)(now.Date - a.OutOfServiceSince.Value.Date).TotalDays,
        IsActive = a.IsActive,
    };

    public static WorkOrderDto ToDto(WorkOrder w, DateTime now) => new()
    {
        Id = w.Id,
        WorkOrderNumber = w.WorkOrderNumber,
        ClubId = w.ClubId,
        EquipmentAssetId = w.EquipmentAssetId,
        EquipmentName = w.EquipmentAsset?.Name,
        MaintenanceScheduleId = w.MaintenanceScheduleId,
        FaultReportId = w.FaultReportId,
        Title = w.Title,
        Detail = w.Detail,
        Status = w.Status,
        Priority = w.Priority,
        RaisedOn = w.RaisedOn,
        DueOn = w.DueOn,
        StartedOn = w.StartedOn,
        CompletedOn = w.CompletedOn,
        IsOverdue = w.CompletedOn is null && w.DueOn is not null && w.DueOn < now,
        AgeDays = (int)((w.CompletedOn ?? now).Date - w.RaisedOn.Date).TotalDays,
        AssignedStaffId = w.AssignedStaffId,
        ContractorName = w.ContractorName,
        ContractorReference = w.ContractorReference,
        LabourCost = w.LabourCost,
        PartsCost = w.PartsCost,
        TotalCost = w.TotalCost,
        PartsUsed = w.PartsUsed,
        DowntimeHours = w.DowntimeHours,
        ResolutionNote = w.ResolutionNote,
        PhotoUrls = SplitList(w.PhotoUrls),
        PurchaseRequestId = w.PurchaseRequestId,
    };

    public static MaintenanceScheduleDto ToDto(MaintenanceSchedule m, DateTime now) => new()
    {
        Id = m.Id,
        EquipmentAssetId = m.EquipmentAssetId,
        EquipmentName = m.EquipmentAsset?.Name,
        ClubId = m.ClubId,
        AppliesToCategory = m.AppliesToCategory,
        TaskName = m.TaskName,
        Instructions = m.Instructions,
        Trigger = m.Trigger,
        IntervalDays = m.IntervalDays,
        IntervalUsageHours = m.IntervalUsageHours,
        LastPerformedOn = m.LastPerformedOn,
        NextDueOn = m.NextDueOn,
        IsOverdue = m.NextDueOn is not null && m.NextDueOn < now,
        DaysUntilDue = m.NextDueOn is null ? null : (int)(m.NextDueOn.Value.Date - now.Date).TotalDays,
        EstimatedMinutes = m.EstimatedMinutes,
        DefaultAssigneeStaffId = m.DefaultAssigneeStaffId,
        AutoCreateWorkOrder = m.AutoCreateWorkOrder,
        IsActive = m.IsActive,
    };

    public static FaultReportDto ToDto(FaultReport f) => new()
    {
        Id = f.Id,
        ClubId = f.ClubId,
        EquipmentAssetId = f.EquipmentAssetId,
        AreaId = f.AreaId,
        FaultDescription = f.FaultDescription,
        Severity = f.Severity,
        ReportedAt = f.ReportedAt,
        ReportedByStaffId = f.ReportedByStaffId,
        ReportedByMemberId = f.ReportedByMemberId,
        PhotoUrl = f.PhotoUrl,
        TakenOutOfService = f.TakenOutOfService,
        SignPrinted = f.SignPrinted,
        WorkOrderId = f.WorkOrderId,
        IsResolved = f.IsResolved,
        ResolvedOn = f.ResolvedOn,
    };

    // ═══ Compliance ══════════════════════════════════════════════════════════

    public static WaiverTemplateDto ToDto(WaiverTemplate w) => new()
    {
        Id = w.Id,
        Name = w.Name,
        Version = w.Version,
        ClubId = w.ClubId,
        CountryCode = w.CountryCode,
        LanguageCode = w.LanguageCode,
        ClassTypeId = w.ClassTypeId,
        ActivityScope = w.ActivityScope,
        BodyHtml = w.BodyHtml,
        ConsentClausesJson = w.ConsentClausesJson,
        RequiresGuardianSignature = w.RequiresGuardianSignature,
        GuardianRequiredBelowAge = w.GuardianRequiredBelowAge,
        ValidForDays = w.ValidForDays,
        BlocksAccess = w.BlocksAccess,
        EffectiveFrom = w.EffectiveFrom,
        EffectiveTo = w.EffectiveTo,
        IsPublished = w.IsPublished,
        RequiresResignOnNewVersion = w.RequiresResignOnNewVersion,
        IsActive = w.IsActive,
    };

    public static WaiverSignatureDto ToDto(WaiverSignature s, DateTime now) => new()
    {
        Id = s.Id,
        WaiverTemplateId = s.WaiverTemplateId,
        TemplateName = s.WaiverTemplate?.Name,
        TemplateVersion = s.TemplateVersion,
        MemberId = s.MemberId,
        MemberName = s.Member is null ? null : FullName(s.Member),
        SignerName = s.SignerName,
        SignerEmail = s.SignerEmail,
        ClubId = s.ClubId,
        Status = s.Status,
        SignedAt = s.SignedAt,
        ExpiresOn = s.ExpiresOn,
        IsExpired = s.ExpiresOn is not null && s.ExpiresOn < now,
        GuardianName = s.GuardianName,
        GuardianRelationship = s.GuardianRelationship,
        SignatureImageUrl = s.SignatureImageUrl,
        DocumentUrl = s.DocumentUrl,
        CapturedVia = s.CapturedVia,
    };

    public static HealthScreeningDto ToDto(HealthScreening h, DateTime now) => new()
    {
        Id = h.Id,
        MemberId = h.MemberId,
        MemberName = h.Member is null ? null : FullName(h.Member),
        ClubId = h.ClubId,
        TemplateName = h.TemplateName,
        TemplateVersion = h.TemplateVersion,
        CompletedAt = h.CompletedAt,
        ExpiresOn = h.ExpiresOn,
        IsExpired = h.ExpiresOn is not null && h.ExpiresOn < now,
        RequiresClearance = h.RequiresClearance,
        ClearanceStatus = h.ClearanceStatus,
        RiskSummary = h.RiskSummary,
        ReviewedAt = h.ReviewedAt,
        ReviewNote = h.ReviewNote,
        CapturedVia = h.CapturedVia,
        Answers = [.. h.Answers.Where(a => !a.IsDeleted).OrderBy(a => a.QuestionNumber).Select(ToDto)],
    };

    public static HealthScreeningAnswerDto ToDto(HealthScreeningAnswer a) => new()
    {
        Id = a.Id,
        QuestionNumber = a.QuestionNumber,
        QuestionText = a.QuestionText,
        AnswerKind = a.AnswerKind,
        BooleanAnswer = a.BooleanAnswer,
        TextAnswer = a.TextAnswer,
        NumericAnswer = a.NumericAnswer,
        DateAnswer = a.DateAnswer,
        IsGatingQuestion = a.IsGatingQuestion,
        FollowUpAnswer = a.FollowUpAnswer,
    };

    public static MedicalClearanceDto ToDto(MedicalClearance c, DateTime now) => new()
    {
        Id = c.Id,
        MemberId = c.MemberId,
        MemberName = c.Member is null ? null : FullName(c.Member),
        ClubId = c.ClubId,
        HealthScreeningId = c.HealthScreeningId,
        Status = c.Status,
        RequestedOn = c.RequestedOn,
        SubmittedOn = c.SubmittedOn,
        ApprovedOn = c.ApprovedOn,
        ExpiresOn = c.ExpiresOn,
        IsExpired = c.ExpiresOn is not null && c.ExpiresOn < now,
        PractitionerName = c.PractitionerName,
        PractitionerRegistration = c.PractitionerRegistration,
        PracticeName = c.PracticeName,
        Restrictions = c.Restrictions,
        DocumentId = c.DocumentId,
        RejectionReason = c.RejectionReason,
        BlocksParticipation = c.BlocksParticipation,
    };

    public static IncidentDto ToDto(Incident i, DateTime now) => new()
    {
        Id = i.Id,
        IncidentNumber = i.IncidentNumber,
        ClubId = i.ClubId,
        AreaId = i.AreaId,
        EquipmentAssetId = i.EquipmentAssetId,
        Kind = i.Kind,
        Severity = i.Severity,
        Status = i.Status,
        OccurredAt = i.OccurredAt,
        ReportedAt = i.ReportedAt,
        ReportingDelayMinutes = (int)(i.ReportedAt - i.OccurredAt).TotalMinutes,
        MemberId = i.MemberId,
        InvolvedPersonName = i.InvolvedPersonName,
        InvolvedPersonPhone = i.InvolvedPersonPhone,
        ReportedByStaffId = i.ReportedByStaffId,
        Summary = i.Summary,
        Detail = i.Detail,
        WitnessNames = i.WitnessNames,
        WitnessStatements = i.WitnessStatements,
        FirstAidGiven = i.FirstAidGiven,
        FirstAiderName = i.FirstAiderName,
        AedUsed = i.AedUsed,
        AmbulanceCalled = i.AmbulanceCalled,
        HospitalAttended = i.HospitalAttended,
        ImmediateAction = i.ImmediateAction,
        PhotoUrls = SplitList(i.PhotoUrls),
        OwnerStaffId = i.OwnerStaffId,
        ReviewDueOn = i.ReviewDueOn,
        ClosedOn = i.ClosedOn,
        RootCause = i.RootCause,
        PreventiveAction = i.PreventiveAction,
        IsReportable = i.IsReportable,
        WasReported = i.WasReported,
        ReportedToAuthorityOn = i.ReportedToAuthorityOn,
        AuthorityReference = i.AuthorityReference,
        InsurerNotified = i.InsurerNotified,
        InsurerReference = i.InsurerReference,
        EstimatedCost = i.EstimatedCost,
        Actions = [.. i.Actions.Where(a => !a.IsDeleted).Select(a => ToDto(a, now))],
        OpenActions = i.Actions.Count(a => !a.IsDeleted && a.CompletedOn is null),
        IsOverdue = i.Status != IncidentStatus.Closed && i.ReviewDueOn is not null && i.ReviewDueOn < now,
    };

    public static IncidentActionDto ToDto(IncidentAction a, DateTime now) => new()
    {
        Id = a.Id,
        IncidentId = a.IncidentId,
        Action = a.Action,
        AssignedStaffId = a.AssignedStaffId,
        RaisedOn = a.RaisedOn,
        DueOn = a.DueOn,
        CompletedOn = a.CompletedOn,
        CompletionNote = a.CompletionNote,
        IsOverdue = a.CompletedOn is null && a.DueOn is not null && a.DueOn < now,
    };

    public static ComplaintDto ToDto(Complaint c, DateTime now) => new()
    {
        Id = c.Id,
        ComplaintNumber = c.ComplaintNumber,
        ClubId = c.ClubId,
        MemberId = c.MemberId,
        MemberName = c.Member is null ? null : FullName(c.Member),
        ComplainantName = c.ComplainantName,
        ComplainantContact = c.ComplainantContact,
        Category = c.Category,
        Summary = c.Summary,
        Detail = c.Detail,
        Status = c.Status,
        Priority = c.Priority,
        RaisedOn = c.RaisedOn,
        AcknowledgedOn = c.AcknowledgedOn,
        TargetResolutionOn = c.TargetResolutionOn,
        ResolvedOn = c.ResolvedOn,
        AgeDays = (int)((c.ResolvedOn ?? now).Date - c.RaisedOn.Date).TotalDays,
        IsOverdue = c.ResolvedOn is null && c.TargetResolutionOn is not null && c.TargetResolutionOn < now,
        ResolutionDays = c.ResolvedOn is null ? null : (int)(c.ResolvedOn.Value.Date - c.RaisedOn.Date).TotalDays,
        OwnerStaffId = c.OwnerStaffId,
        Resolution = c.Resolution,
        CompensationValue = c.CompensationValue,
        CompensationNote = c.CompensationNote,
        ComplainantSatisfied = c.ComplainantSatisfied,
        Channel = c.Channel,
    };

    public static LostPropertyItemDto ToDto(LostPropertyItem l, DateTime now) => new()
    {
        Id = l.Id,
        ClubId = l.ClubId,
        ItemDescription = l.ItemDescription,
        Category = l.Category,
        PhotoUrl = l.PhotoUrl,
        FoundOn = l.FoundOn,
        FoundLocation = l.FoundLocation,
        StorageLocation = l.StorageLocation,
        Status = l.Status,
        ClaimedByMemberId = l.ClaimedByMemberId,
        ClaimedByName = l.ClaimedByName,
        ClaimedOn = l.ClaimedOn,
        DisposeAfter = l.DisposeAfter,
        ReadyForDisposal = l.Status == LostPropertyStatus.Held && l.DisposeAfter is not null && l.DisposeAfter < now,
        DisposedOn = l.DisposedOn,
        DisposalNote = l.DisposalNote,
        DaysHeld = (int)((l.ClaimedOn ?? l.DisposedOn ?? now).Date - l.FoundOn.Date).TotalDays,
    };

    public static FacilityCheckDto ToDto(FacilityCheck c, DateTime now)
    {
        var dueToday = FitnessQueryHelpers.CoversDay(c.DaysOfWeekMask, now.DayOfWeek);
        var lastItem = c.Items.Where(i => !i.IsDeleted).OrderByDescending(i => i.LastCompletedAt).FirstOrDefault();
        var completedToday = lastItem?.LastCompletedAt?.Date == now.Date;

        return new FacilityCheckDto
        {
            Id = c.Id,
            ClubId = c.ClubId,
            AreaId = c.AreaId,
            Name = c.Name,
            Kind = c.Kind,
            DaysOfWeekMask = c.DaysOfWeekMask,
            DueAt = c.DueAt,
            TimesPerDay = c.TimesPerDay,
            DefaultAssigneeRoleId = c.DefaultAssigneeRoleId,
            AlertOnMissed = c.AlertOnMissed,
            MissedAfterMinutes = c.MissedAfterMinutes,
            RequiresSignature = c.RequiresSignature,
            IsActive = c.IsActive,
            Items = [.. c.Items.Where(i => !i.IsDeleted).OrderBy(i => i.DisplayOrder).Select(ToDto)],
            DueToday = dueToday,
            CompletedToday = completedToday,
            IsOverdue = dueToday && !completedToday && now.TimeOfDay > c.DueAt.Add(TimeSpan.FromMinutes(c.MissedAfterMinutes)),
            LastCompletedAt = lastItem?.LastCompletedAt,
            FailedItemCount = c.Items.Count(i => !i.IsDeleted && i.LastPassed == false),
        };
    }

    public static FacilityCheckItemDto ToDto(FacilityCheckItem i) => new()
    {
        Id = i.Id,
        FacilityCheckId = i.FacilityCheckId,
        ItemDescription = i.ItemDescription,
        DisplayOrder = i.DisplayOrder,
        AnswerKind = i.AnswerKind,
        Unit = i.Unit,
        AcceptableLow = i.AcceptableLow,
        AcceptableHigh = i.AcceptableHigh,
        IsCritical = i.IsCritical,
        RequiresPhoto = i.RequiresPhoto,
        LastCompletedAt = i.LastCompletedAt,
        LastPassed = i.LastPassed,
        LastValue = i.LastValue,
        LastNote = i.LastNote,
        OutOfRange = i.LastValue is not null
                     && ((i.AcceptableLow is not null && i.LastValue < i.AcceptableLow)
                      || (i.AcceptableHigh is not null && i.LastValue > i.AcceptableHigh)),
    };

    public static ShiftHandoverDto ToDto(ShiftHandover h) => new()
    {
        Id = h.Id,
        ClubId = h.ClubId,
        ShiftEndedAt = h.ShiftEndedAt,
        FromStaffId = h.FromStaffId,
        ToStaffId = h.ToStaffId,
        Notes = h.Notes,
        OutstandingItems = h.OutstandingItems,
        HasUrgentItems = h.HasUrgentItems,
        AcknowledgedAt = h.AcknowledgedAt,
    };

    public static AuditEntryDto ToDto(AuditEntry a) => new()
    {
        Id = a.Id,
        ClubId = a.ClubId,
        OccurredAt = a.OccurredAt,
        ActorUserId = a.ActorUserId,
        ActorName = a.ActorName,
        Action = a.Action,
        EntityType = a.EntityType,
        EntityId = a.EntityId,
        MemberId = a.MemberId,
        ChangeSummary = a.ChangeSummary,
        IsSensitiveAccess = a.IsSensitiveAccess,
        Reason = a.Reason,
        IpAddress = a.IpAddress,
    };

    // ═══ Commerce ════════════════════════════════════════════════════════════

    public static FitnessSaleDto ToDto(FitnessSale s)
    {
        var lines = s.Lines.Where(l => !l.IsDeleted).OrderBy(l => l.DisplayOrder).ToList();
        return new FitnessSaleDto
        {
            Id = s.Id,
            SaleNumber = s.SaleNumber,
            ClubId = s.ClubId,
            MemberId = s.MemberId,
            MemberName = s.Member is null ? null : FullName(s.Member),
            MemberNumber = s.Member?.MemberNumber,
            SoldAt = s.SoldAt,
            Subtotal = s.Subtotal,
            DiscountTotal = s.DiscountTotal,
            TaxTotal = s.TaxTotal,
            Total = s.Total,
            CurrencyCode = s.CurrencyCode,
            PaymentMethod = s.PaymentMethod,
            IsHouseAccountCharge = s.IsHouseAccountCharge,
            CashSessionId = s.CashSessionId,
            IsReturn = s.IsReturn,
            ReturnsSaleId = s.ReturnsSaleId,
            ReturnReason = s.ReturnReason,
            StockDepleted = s.StockDepleted,
            DiscountReason = s.DiscountReason,
            Lines = [.. lines.Select(ToDto)],
            GrossMargin = lines.Sum(l => l.LineTotal - l.TaxAmount - l.UnitCost * l.Quantity),
        };
    }

    public static FitnessSaleLineDto ToDto(FitnessSaleLine l) => new()
    {
        Id = l.Id,
        InventoryItemId = l.InventoryItemId,
        PlanId = l.PlanId,
        ItemName = l.ItemName,
        Barcode = l.Barcode,
        Quantity = l.Quantity,
        UnitPrice = l.UnitPrice,
        DiscountAmount = l.DiscountAmount,
        TaxPercent = l.TaxPercent,
        TaxAmount = l.TaxAmount,
        LineTotal = l.LineTotal,
        UnitCost = l.UnitCost,
        Modifiers = l.Modifiers,
        DisplayOrder = l.DisplayOrder,
    };

    public static CorporateAccountDto ToDto(CorporateAccount c, DateTime now) => new()
    {
        Id = c.Id,
        Code = c.Code,
        Name = c.Name,
        ClubId = c.ClubId,
        CrmAccountId = c.CrmAccountId,
        ContactName = c.ContactName,
        ContactEmail = c.ContactEmail,
        ContactPhone = c.ContactPhone,
        AddressLine = c.AddressLine,
        TaxRegistrationNumber = c.TaxRegistrationNumber,
        BillingModel = c.BillingModel,
        NegotiatedRate = c.NegotiatedRate,
        DiscountPercent = c.DiscountPercent,
        SubsidyPerMember = c.SubsidyPerMember,
        SubsidyPercent = c.SubsidyPercent,
        DefaultPlanId = c.DefaultPlanId,
        ContractStartsOn = c.ContractStartsOn,
        ContractEndsOn = c.ContractEndsOn,
        ContractExpiringSoon = c.ContractEndsOn is not null && c.ContractEndsOn <= now.AddDays(60),
        MaxMembers = c.MaxMembers,
        CurrentMemberCount = c.CurrentMemberCount,
        SpacesRemaining = c.MaxMembers == 0 ? int.MaxValue : Math.Max(0, c.MaxMembers - c.CurrentMemberCount),
        InvoiceDayOfMonth = c.InvoiceDayOfMonth,
        PaymentTermsDays = c.PaymentTermsDays,
        ReceivesUsageReport = c.ReceivesUsageReport,
        IsActive = c.IsActive,
        EligibilityRules = [.. c.EligibilityRules.Where(r => !r.IsDeleted).Select(ToDto)],
    };

    public static CorporateEligibilityRuleDto ToDto(CorporateEligibilityRule r) => new()
    {
        Id = r.Id,
        CorporateAccountId = r.CorporateAccountId,
        Proof = r.Proof,
        MatchValue = r.MatchValue,
        RequiresManualApproval = r.RequiresManualApproval,
        RevalidateEveryDays = r.RevalidateEveryDays,
        IsActive = r.IsActive,
    };

    public static CorporateMemberDto ToDto(CorporateMember m, DateTime now) => new()
    {
        Id = m.Id,
        CorporateAccountId = m.CorporateAccountId,
        MemberId = m.MemberId,
        MemberName = m.Member is null ? string.Empty : FullName(m.Member),
        MemberNumber = m.Member?.MemberNumber,
        MemberStatus = m.Member?.Status ?? MemberStatus.Lead,
        AgreementId = m.AgreementId,
        EmployeeReference = m.EmployeeReference,
        Department = m.Department,
        JoinedSchemeOn = m.JoinedSchemeOn,
        LeftSchemeOn = m.LeftSchemeOn,
        EligibilityVerifiedOn = m.EligibilityVerifiedOn,
        EligibilityExpiresOn = m.EligibilityExpiresOn,
        EligibilityExpired = m.EligibilityExpiresOn is not null && m.EligibilityExpiresOn < now,
        EmployerContribution = m.EmployerContribution,
        EmployeeContribution = m.EmployeeContribution,
        IsActive = m.IsActive,
        LastVisitOn = m.Member?.LastVisitOn,
    };

    public static ThirdPartyPayerDto ToDto(ThirdPartyPayer p) => new()
    {
        Id = p.Id,
        Name = p.Name,
        ClubId = p.ClubId,
        PayerType = p.PayerType,
        ContactName = p.ContactName,
        ContactEmail = p.ContactEmail,
        ContactPhone = p.ContactPhone,
        PaymentTermsDays = p.PaymentTermsDays,
        AgreedRate = p.AgreedRate,
        RequiresAuthorisationNumber = p.RequiresAuthorisationNumber,
        IsActive = p.IsActive,
    };

    public static PayerAuthorisationDto ToDto(PayerAuthorisation a, DateTime now) => new()
    {
        Id = a.Id,
        ThirdPartyPayerId = a.ThirdPartyPayerId,
        PayerName = a.ThirdPartyPayer?.Name,
        MemberId = a.MemberId,
        MemberName = a.Member is null ? null : FullName(a.Member),
        AuthorisationNumber = a.AuthorisationNumber,
        ValidFrom = a.ValidFrom,
        ValidTo = a.ValidTo,
        IsExpired = a.ValidTo < now,
        ApprovedUnits = a.ApprovedUnits,
        UsedUnits = a.UsedUnits,
        RemainingUnits = Math.Max(0, a.ApprovedUnits - a.UsedUnits),
        RatePerUnit = a.RatePerUnit,
        ApprovedValue = a.ApprovedValue,
        InvoicedValue = a.InvoicedValue,
        Purpose = a.Purpose,
        ReferrerName = a.ReferrerName,
        IsExhausted = a.IsExhausted,
        IsActive = a.IsActive,
    };

    public static HouseAccountChargeDto ToDto(HouseAccountCharge h) => new()
    {
        Id = h.Id,
        MemberId = h.MemberId,
        MemberName = h.Member is null ? null : FullName(h.Member),
        ClubId = h.ClubId,
        SaleId = h.SaleId,
        ChargedOn = h.ChargedOn,
        Amount = h.Amount,
        ChargeDescription = h.ChargeDescription,
        SettledInvoiceId = h.SettledInvoiceId,
        IsSettled = h.IsSettled,
        SettledOn = h.SettledOn,
    };

    public static GiftCardDto ToDto(GiftCard g, DateTime now) => new()
    {
        Id = g.Id,
        CardNumber = g.CardNumber,
        ClubId = g.ClubId,
        InitialValue = g.InitialValue,
        Balance = g.Balance,
        CurrencyCode = g.CurrencyCode,
        IssuedOn = g.IssuedOn,
        ExpiresOn = g.ExpiresOn,
        PurchasedByMemberId = g.PurchasedByMemberId,
        RecipientName = g.RecipientName,
        RecipientEmail = g.RecipientEmail,
        Message = g.Message,
        IsRedeemed = g.IsRedeemed,
        IsCancelled = g.IsCancelled,
        IsExpired = g.ExpiresOn is not null && g.ExpiresOn < now,
    };

    public static VendingRevenueEntryDto ToDto(VendingRevenueEntry v) => new()
    {
        Id = v.Id,
        ClubId = v.ClubId,
        PeriodStart = v.PeriodStart,
        PeriodEnd = v.PeriodEnd,
        RevenueSource = v.RevenueSource,
        MachineReference = v.MachineReference,
        GrossRevenue = v.GrossRevenue,
        CommissionPaid = v.CommissionPaid,
        NetRevenue = v.NetRevenue,
        CurrencyCode = v.CurrencyCode,
        TransactionCount = v.TransactionCount,
        Note = v.Note,
    };

    // ═══ Helpers ═════════════════════════════════════════════════════════════

    public static string FullName(Member m) => $"{m.FirstName} {m.LastName}".Trim();

    public static int? AgeOn(DateTime? dob, DateTime now)
    {
        if (dob is null) return null;
        var age = now.Year - dob.Value.Year;
        if (dob.Value.Date > now.Date.AddYears(-age)) age--;
        return age;
    }

    /// <summary>Percentage, rounded, guarding against a zero denominator.</summary>
    public static int Percent(decimal part, decimal whole)
        => whole == 0 ? 0 : (int)Math.Round(part / whole * 100m, MidpointRounding.AwayFromZero);

    private static int ElapsedPercent(DateTime from, DateTime to, DateTime now)
    {
        var total = (to - from).TotalDays;
        if (total <= 0) return 100;
        return (int)Math.Clamp((now - from).TotalDays / total * 100, 0, 100);
    }

    /// <summary>
    /// A credential shown in a list. Full identifiers are never rendered — a fob number in a
    /// screenshot is a working key, and there is no screen that needs all sixteen digits.
    /// </summary>
    private static string MaskCredential(string identifier, CredentialType type)
    {
        if (type is CredentialType.ManualLookup or CredentialType.Pin) return "••••";
        if (identifier.Length <= 4) return identifier;
        return $"••••{identifier[^4..]}";
    }

    private static CertificationStatus CertStatus(StaffCertification c, DateTime now)
    {
        if (c.Status == CertificationStatus.Suspended) return CertificationStatus.Suspended;
        if (c.ExpiresOn is null) return CertificationStatus.Valid;
        if (c.ExpiresOn < now) return CertificationStatus.Expired;
        if (c.ExpiresOn <= now.AddDays(60)) return CertificationStatus.ExpiringSoon;
        return CertificationStatus.Valid;
    }

    private static string DescribePaymentMethod(PaymentMethodRef m) => m.Method switch
    {
        PaymentMethod.Card when m.CardLastFour is not null =>
            $"{m.CardBrand ?? "Card"} •••• {m.CardLastFour}" +
            (m.ExpiryMonth is not null ? $", expires {m.ExpiryMonth:00}/{m.ExpiryYear % 100:00}" : ""),
        PaymentMethod.DirectDebit when m.AccountLastFour is not null =>
            $"Direct debit •••• {m.AccountLastFour}" + (m.BankName is not null ? $" ({m.BankName})" : ""),
        PaymentMethod.Cash => "Pays at the desk",
        PaymentMethod.CorporateAccount => "Billed to employer",
        _ => m.Method.ToString(),
    };

    /// <summary>
    /// A commission rule in a sentence. Written out because a rule nobody can read is a dispute
    /// waiting to happen, and "PercentOfSessionValue 40 threshold 20" is not readable.
    /// </summary>
    private static string DescribeCommission(CommissionRule r)
    {
        var body = r.Basis switch
        {
            CommissionBasis.PerSessionDelivered => $"{r.RatePerUnit:0.##} per session delivered",
            CommissionBasis.PerClassTaught => $"{r.RatePerUnit:0.##} per class taught",
            CommissionBasis.PerClassHead => $"{r.RatePerUnit:0.##} per head in class",
            CommissionBasis.PercentOfSessionValue => $"{r.Percentage:0.##}% of session value",
            CommissionBasis.PercentOfMembershipSold => $"{r.Percentage:0.##}% of memberships sold",
            CommissionBasis.PercentOfPackageSold => $"{r.Percentage:0.##}% of packages sold",
            CommissionBasis.PercentOfRetailSold => $"{r.Percentage:0.##}% of retail sold",
            CommissionBasis.FlatPerPeriod => $"{r.RatePerUnit:0.##} per period",
            CommissionBasis.TargetBonus => $"{r.RatePerUnit:0.##} bonus on hitting target",
            _ => r.Basis.ToString(),
        };

        if (r.Threshold > 0)
            body += $", after the first {r.Threshold:0.##}";
        if (r.AcceleratedRate > 0)
            body += $", rising to {r.AcceleratedRate:0.##} beyond it";
        if (r.PeriodCap > 0)
            body += $", capped at {r.PeriodCap:0.##} a period";

        return body;
    }

    /// <summary>
    /// A workout result as a person reads it: "4:32", "12 + 8", "102.5 kg".
    ///
    /// Formatted server-side so every surface — the app, the wall leaderboard, an exported PDF —
    /// shows the same string, rather than three clients each inventing their own.
    /// </summary>
    public static string FormatScore(WorkoutResult r)
    {
        if (r.DidNotFinish && r.Reps is not null) return $"DNF ({r.Reps} reps)";
        if (r.DidNotFinish) return "DNF";

        return r.ScoreType switch
        {
            ScoreType.ForTime when r.TimeSeconds is not null => FormatDuration(r.TimeSeconds.Value),
            ScoreType.RoundsAndReps => $"{r.Rounds ?? 0} + {r.Reps ?? 0}",
            ScoreType.Reps => $"{r.Reps ?? 0} reps",
            ScoreType.MaxLoad when r.LoadKg is not null => $"{r.LoadKg:0.##} kg",
            ScoreType.Distance when r.DistanceMetres is not null => FormatDistance(r.DistanceMetres.Value),
            ScoreType.Calories => $"{r.Calories ?? 0} cal",
            ScoreType.TimeUnderLoad when r.TimeSeconds is not null => FormatDuration(r.TimeSeconds.Value),
            ScoreType.PassFail => r.Passed == true ? "Pass" : "Fail",
            ScoreType.Points => $"{r.Points ?? 0} pts",
            _ => "—",
        };
    }

    private static string FormatValue(ScoreType type, decimal value, string? unit) => type switch
    {
        ScoreType.ForTime or ScoreType.TimeUnderLoad => FormatDuration((int)value),
        ScoreType.MaxLoad => $"{value:0.##} {unit ?? "kg"}",
        ScoreType.Distance => FormatDistance(value),
        _ => $"{value:0.##} {unit}".Trim(),
    };

    private static string FormatImprovement(ScoreType type, decimal delta, string? unit)
    {
        // For time, less is better, so a negative delta is the improvement.
        if (type is ScoreType.ForTime or ScoreType.TimeUnderLoad)
            return delta < 0 ? $"−{FormatDuration((int)Math.Abs(delta))} faster" : $"+{FormatDuration((int)delta)} slower";

        return delta > 0 ? $"+{delta:0.##} {unit}".Trim() : $"{delta:0.##} {unit}".Trim();
    }

    private static string FormatDuration(int seconds)
        => seconds >= 3600
            ? $"{seconds / 3600}:{seconds / 60 % 60:00}:{seconds % 60:00}"
            : $"{seconds / 60}:{seconds % 60:00}";

    private static string FormatDistance(decimal metres)
        => metres >= 1000 ? $"{metres / 1000:0.##} km" : $"{metres:0.##} m";

    private static List<string> SplitList(string? csv)
        => string.IsNullOrWhiteSpace(csv)
            ? []
            : [.. csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)];

    private static List<Guid> ParseGuidList(string? csv)
        => SplitList(csv).Select(s => Guid.TryParse(s, out var g) ? g : Guid.Empty).Where(g => g != Guid.Empty).ToList();

    /// <summary>An allowed move between two plans. Names are filled in by the caller.</summary>
    public static PlanChangePathDto ToDto(PlanChangePath p) => new()
    {
        Id = p.Id,
        FromPlanId = p.FromPlanId,
        ToPlanId = p.ToPlanId,
        EffectiveImmediately = p.EffectiveImmediately,
        Proration = p.Proration,
        ChangeFee = p.ChangeFee,
        RestartsMinimumTerm = p.RestartsMinimumTerm,
        RequiresApproval = p.RequiresApproval,
    };

    /// <summary>A programming track. Publication counts are filled in by the caller.</summary>
    public static ProgramTrackDto ToDto(ProgramTrack t) => new()
    {
        Id = t.Id,
        Name = t.Name,
        ClubId = t.ClubId,
        ColourHex = t.ColourHex,
        DisplayOrder = t.DisplayOrder,
        IsPublic = t.IsPublic,
        StartsOn = t.StartsOn,
        EndsOn = t.EndsOn,
        IsActive = t.IsActive,
    };

    /// <summary>One day on a track.</summary>
    public static ProgramDayDto ToDto(ProgramDay d) => new()
    {
        Id = d.Id,
        ProgramTrackId = d.ProgramTrackId,
        TrackName = d.ProgramTrack?.Name,
        TrackColour = d.ProgramTrack?.ColourHex,
        WorkoutId = d.WorkoutId,
        WorkoutName = d.Workout?.Name,
        ScheduledOn = d.ScheduledOn,
        ClubId = d.ClubId,
        IsPublished = d.IsPublished,
        PublishAt = d.PublishAt,
        CoachBrief = d.CoachBrief,
    };
}

/// <summary>
/// Small pure helpers the mapper needs that also belong to the query layer. Kept separate so the
/// mapper has no dependency on the persistence namespace.
/// </summary>
internal static class FitnessQueryHelpers
{
    public static bool CoversDay(int daysOfWeekMask, DayOfWeek day)
        => (daysOfWeekMask & (1 << (int)day)) != 0;
}
