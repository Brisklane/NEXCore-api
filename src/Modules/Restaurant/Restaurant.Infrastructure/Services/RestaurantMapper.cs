using Restaurant.Application.DTOs;
using Restaurant.Domain.Entities;
using Restaurant.Domain.Enums;

namespace Restaurant.Infrastructure.Services;

/// <summary>
/// Entity → DTO projections, kept in one place so a field added to an entity has exactly one
/// place it has to be surfaced. Hand-written rather than reflection-mapped: these run on the
/// KDS poll and the floor-plan refresh, which are the hottest paths in the module.
/// </summary>
public static class RestaurantMapper
{
    // ── Venue ────────────────────────────────────────────────────────────────

    public static OutletDto ToDto(RestaurantOutlet e) => new()
    {
        Id = e.Id,
        Code = e.Code,
        Name = e.Name,
        ServiceStyle = e.ServiceStyle,
        CuisineType = e.CuisineType,
        Phone = e.Phone,
        Email = e.Email,
        AddressLine = e.AddressLine,
        City = e.City,
        CountryCode = e.CountryCode,
        TimeZoneId = e.TimeZoneId,
        CurrencyCode = e.CurrencyCode,
        WarehouseId = e.WarehouseId,
        PosStoreId = e.PosStoreId,
        DefaultMenuId = e.DefaultMenuId,
        DefaultTaxGroupId = e.DefaultTaxGroupId,
        ServiceChargeRuleId = e.ServiceChargeRuleId,
        DefaultTaxPercent = e.DefaultTaxPercent,
        TakeawayTaxPercent = e.TakeawayTaxPercent,
        SeatingCapacity = e.SeatingCapacity,
        AverageDiningMinutes = e.AverageDiningMinutes,
        AcceptsReservations = e.AcceptsReservations,
        AcceptsDelivery = e.AcceptsDelivery,
        AcceptsTakeaway = e.AcceptsTakeaway,
        HasDriveThru = e.HasDriveThru,
        QrOrderingEnabled = e.QrOrderingEnabled,
        IsTemporarilyClosed = e.IsTemporarilyClosed,
        ClosureNote = e.ClosureNote,
        LogoUrl = e.LogoUrl,
        ReceiptFooter = e.ReceiptFooter,
        IsActive = e.IsActive,
        Description = e.Description,
        Schedules = e.Schedules?.Select(ToDto).OrderBy(s => s.DayOfWeek).ToList() ?? [],
    };

    public static OutletScheduleDto ToDto(OutletSchedule e) => new()
    {
        Id = e.Id,
        OutletId = e.OutletId,
        DayOfWeek = e.DayOfWeek,
        OverrideDate = e.OverrideDate,
        OpensAt = e.OpensAt,
        ClosesAt = e.ClosesAt,
        IsClosed = e.IsClosed,
        Note = e.Note,
    };

    public static SectionDto ToDto(TableSection e) => new()
    {
        Id = e.Id,
        FloorId = e.FloorId,
        Name = e.Name,
        DisplayOrder = e.DisplayOrder,
        ColorHex = e.ColorHex,
        IsSmoking = e.IsSmoking,
        IsOutdoor = e.IsOutdoor,
        IsPrivate = e.IsPrivate,
        MinimumSpend = e.MinimumSpend,
        IsActive = e.IsActive,
    };

    public static FixtureDto ToDto(FloorFixture e) => new()
    {
        Id = e.Id,
        FloorId = e.FloorId,
        Kind = e.Kind,
        Label = e.Label,
        PositionX = e.PositionX,
        PositionY = e.PositionY,
        Width = e.Width,
        Height = e.Height,
        Rotation = e.Rotation,
        ColorHex = e.ColorHex,
    };

    public static TableDto ToDto(DiningTable e, DateTime now) => new()
    {
        Id = e.Id,
        OutletId = e.OutletId,
        FloorId = e.FloorId,
        SectionId = e.SectionId,
        TableNumber = e.TableNumber,
        Shape = e.Shape,
        Seats = e.Seats,
        MinPartySize = e.MinPartySize,
        MaxPartySize = e.MaxPartySize,
        PositionX = e.PositionX,
        PositionY = e.PositionY,
        Width = e.Width,
        Height = e.Height,
        Rotation = e.Rotation,
        State = e.State,
        CurrentOrderId = e.CurrentOrderId,
        AssignedWaiterId = e.AssignedWaiterId,
        CurrentGuestCount = e.CurrentGuestCount,
        SeatedAt = e.SeatedAt,
        StateChangedAt = e.StateChangedAt,
        MergedIntoTableId = e.MergedIntoTableId,
        QrToken = e.QrToken,
        Note = e.Note,
        IsActive = e.IsActive,
        MinutesInState = e.StateChangedAt is null ? 0 : (int)Math.Max(0, (now - e.StateChangedAt.Value).TotalMinutes),
    };

    // ── Menu ─────────────────────────────────────────────────────────────────

    public static MenuCardDto ToDto(MenuCard e) => new()
    {
        Id = e.Id,
        OutletId = e.OutletId,
        Name = e.Name,
        Daypart = e.Daypart,
        AvailableFrom = e.AvailableFrom,
        AvailableTo = e.AvailableTo,
        ActiveDays = e.ActiveDays,
        EffectiveFrom = e.EffectiveFrom,
        EffectiveTo = e.EffectiveTo,
        DisplayOrder = e.DisplayOrder,
        IsDefault = e.IsDefault,
        IsActive = e.IsActive,
        Description = e.Description,
    };

    public static MenuCategoryDto ToDto(MenuCategory e) => new()
    {
        Id = e.Id,
        MenuId = e.MenuId,
        ParentCategoryId = e.ParentCategoryId,
        Name = e.Name,
        DisplayOrder = e.DisplayOrder,
        ColorHex = e.ColorHex,
        IconName = e.IconName,
        ImageUrl = e.ImageUrl,
        DefaultStationId = e.DefaultStationId,
        IsActive = e.IsActive,
        Description = e.Description,
    };

    public static MenuItemDto ToDto(MenuItem e)
    {
        var price = e.BasePrice;
        return new MenuItemDto
        {
            Id = e.Id,
            Code = e.Code,
            CategoryId = e.CategoryId,
            Name = e.Name,
            ShortName = e.ShortName,
            ImageUrl = e.ImageUrl,
            Description = e.Description,
            DisplayOrder = e.DisplayOrder,
            BasePrice = e.BasePrice,
            StandardCost = e.StandardCost,
            TaxGroupId = e.TaxGroupId,
            TaxPercent = e.TaxPercent,
            InventoryItemId = e.InventoryItemId,
            StationId = e.StationId,
            DefaultCourse = e.DefaultCourse,
            PrepTimeMinutes = e.PrepTimeMinutes,
            IsVegetarian = e.IsVegetarian,
            IsVegan = e.IsVegan,
            IsHalal = e.IsHalal,
            IsGlutenFree = e.IsGlutenFree,
            ContainsNuts = e.ContainsNuts,
            ContainsDairy = e.ContainsDairy,
            ContainsShellfish = e.ContainsShellfish,
            SpiceLevel = e.SpiceLevel,
            Calories = e.Calories,
            Allergens = e.Allergens,
            IsAlcohol = e.IsAlcohol,
            IsSoldByWeight = e.IsSoldByWeight,
            IsOpenPrice = e.IsOpenPrice,
            IsFeatured = e.IsFeatured,
            IsCombo = e.IsCombo,
            IsAvailable = e.IsAvailable,
            IsActive = e.IsActive,
            KitchenNote = e.KitchenNote,
            Barcode = e.Barcode,
            FoodCostPercent = price > 0 ? Math.Round(e.StandardCost / price * 100m, 2) : 0m,
            ContributionMargin = price - e.StandardCost,
            Variants = e.Variants?.Where(v => !v.IsDeleted).OrderBy(v => v.DisplayOrder).Select(ToDto).ToList() ?? [],
            Prices = e.Prices?.Where(p => !p.IsDeleted).Select(ToDto).ToList() ?? [],
        };
    }

    public static MenuItemVariantDto ToDto(MenuItemVariant e) => new()
    {
        Id = e.Id,
        MenuItemId = e.MenuItemId,
        Name = e.Name,
        DisplayOrder = e.DisplayOrder,
        Price = e.Price,
        StandardCost = e.StandardCost,
        IsDefault = e.IsDefault,
        Barcode = e.Barcode,
        InventoryItemId = e.InventoryItemId,
        IsAvailable = e.IsAvailable,
        IsActive = e.IsActive,
    };

    public static MenuItemPriceDto ToDto(MenuItemPrice e) => new()
    {
        Id = e.Id,
        MenuItemId = e.MenuItemId,
        VariantId = e.VariantId,
        OutletId = e.OutletId,
        Scope = e.Scope,
        Price = e.Price,
    };

    public static ModifierGroupDto ToDto(ModifierGroup e) => new()
    {
        Id = e.Id,
        Name = e.Name,
        PromptText = e.PromptText,
        SelectionMode = e.SelectionMode,
        IsRequired = e.IsRequired,
        MinSelections = e.MinSelections,
        MaxSelections = e.MaxSelections,
        FreeSelections = e.FreeSelections,
        DisplayOrder = e.DisplayOrder,
        IsActive = e.IsActive,
        Description = e.Description,
        Modifiers = e.Modifiers?.Where(m => !m.IsDeleted).OrderBy(m => m.DisplayOrder).Select(ToDto).ToList() ?? [],
    };

    public static ModifierDto ToDto(Modifier e) => new()
    {
        Id = e.Id,
        ModifierGroupId = e.ModifierGroupId,
        Name = e.Name,
        PriceDelta = e.PriceDelta,
        CostDelta = e.CostDelta,
        DisplayOrder = e.DisplayOrder,
        IsDefault = e.IsDefault,
        IsAvailable = e.IsAvailable,
        IsRemoval = e.IsRemoval,
        InventoryItemId = e.InventoryItemId,
        ConsumptionQuantity = e.ConsumptionQuantity,
        ConsumptionUom = e.ConsumptionUom,
        IsActive = e.IsActive,
    };

    public static ComboMealDto ToDto(ComboMeal e) => new()
    {
        Id = e.Id,
        Code = e.Code,
        OutletId = e.OutletId,
        MenuItemId = e.MenuItemId,
        Name = e.Name,
        ImageUrl = e.ImageUrl,
        Description = e.Description,
        Price = e.Price,
        StandardCost = e.StandardCost,
        TaxGroupId = e.TaxGroupId,
        DisplayOrder = e.DisplayOrder,
        IsAvailable = e.IsAvailable,
        IsActive = e.IsActive,
        Components = e.Components?.Where(c => !c.IsDeleted).OrderBy(c => c.DisplayOrder).Select(ToDto).ToList() ?? [],
    };

    public static ComboComponentDto ToDto(ComboComponent e) => new()
    {
        Id = e.Id,
        ComboMealId = e.ComboMealId,
        Name = e.Name,
        Mode = e.Mode,
        Quantity = e.Quantity,
        MinChoices = e.MinChoices,
        MaxChoices = e.MaxChoices,
        DisplayOrder = e.DisplayOrder,
        Options = e.Options?.Where(o => !o.IsDeleted).OrderBy(o => o.DisplayOrder).Select(ToDto).ToList() ?? [],
    };

    public static ComboOptionDto ToDto(ComboComponentOption e) => new()
    {
        Id = e.Id,
        ComboComponentId = e.ComboComponentId,
        MenuItemId = e.MenuItemId,
        VariantId = e.VariantId,
        UpchargeAmount = e.UpchargeAmount,
        IsDefault = e.IsDefault,
        DisplayOrder = e.DisplayOrder,
    };

    public static AvailabilityDto ToDto(MenuItemAvailability e) => new()
    {
        Id = e.Id,
        OutletId = e.OutletId,
        MenuItemId = e.MenuItemId,
        VariantId = e.VariantId,
        IsAvailable = e.IsAvailable,
        Reason = e.Reason,
        IsAutomatic = e.IsAutomatic,
        AvailableAgainAt = e.AvailableAgainAt,
        MarkedAt = e.MarkedAt,
        MarkedByStaffId = e.MarkedByStaffId,
    };

    public static HappyHourRuleDto ToDto(HappyHourRule e) => new()
    {
        Id = e.Id,
        OutletId = e.OutletId,
        Name = e.Name,
        StartTime = e.StartTime,
        EndTime = e.EndTime,
        ActiveDays = e.ActiveDays,
        EffectiveFrom = e.EffectiveFrom,
        EffectiveTo = e.EffectiveTo,
        DiscountKind = e.DiscountKind,
        DiscountValue = e.DiscountValue,
        CategoryId = e.CategoryId,
        MenuItemId = e.MenuItemId,
        ApplicableOrderTypes = e.ApplicableOrderTypes,
        Priority = e.Priority,
        IsActive = e.IsActive,
    };

    // ── Kitchen ──────────────────────────────────────────────────────────────

    public static KitchenStationDto ToDto(KitchenStation e) => new()
    {
        Id = e.Id,
        OutletId = e.OutletId,
        Name = e.Name,
        StationType = e.StationType,
        DisplayOrder = e.DisplayOrder,
        ColorHex = e.ColorHex,
        IsExpo = e.IsExpo,
        SlaMinutes = e.SlaMinutes,
        MaxConcurrentTickets = e.MaxConcurrentTickets,
        PrintsTickets = e.PrintsTickets,
        PrinterProfileId = e.PrinterProfileId,
        IsActive = e.IsActive,
        Description = e.Description,
        RoutingRules = e.RoutingRules?.Where(r => !r.IsDeleted).OrderBy(r => r.Priority).Select(ToDto).ToList() ?? [],
    };

    public static StationRoutingRuleDto ToDto(StationRoutingRule e) => new()
    {
        Id = e.Id,
        StationId = e.StationId,
        OutletId = e.OutletId,
        MatchType = e.MatchType,
        CategoryId = e.CategoryId,
        MenuItemId = e.MenuItemId,
        OrderType = e.OrderType,
        Priority = e.Priority,
        IsAdditional = e.IsAdditional,
        IsActive = e.IsActive,
    };

    public static KitchenTicketDto ToDto(KitchenTicket e, DateTime now, int warningMinutes, int slaMinutes)
    {
        var age = (int)Math.Max(0, (now - e.FiredAt).TotalSeconds);
        return new KitchenTicketDto
        {
            Id = e.Id,
            OutletId = e.OutletId,
            StationId = e.StationId,
            OrderId = e.OrderId,
            TicketNumber = e.TicketNumber,
            Status = e.Status,
            Course = e.Course,
            OrderType = e.OrderType,
            TableNumber = e.TableNumber,
            WaiterName = e.WaiterName,
            GuestCount = e.GuestCount,
            IsPriority = e.IsPriority,
            IsRemake = e.IsRemake,
            FiredAt = e.FiredAt,
            AcknowledgedAt = e.AcknowledgedAt,
            StartedAt = e.StartedAt,
            ReadyAt = e.ReadyAt,
            BumpedAt = e.BumpedAt,
            PrepSeconds = e.PrepSeconds,
            RecallCount = e.RecallCount,
            Note = e.Note,
            AgeSeconds = age,
            UrgencyLevel = age >= slaMinutes * 60 ? "overdue"
                         : age >= warningMinutes * 60 ? "warning"
                         : "ok",
            Lines = e.Lines?.Where(l => !l.IsDeleted).OrderBy(l => l.DisplayOrder).Select(ToDto).ToList() ?? [],
        };
    }

    public static KitchenTicketLineDto ToDto(KitchenTicketLine e) => new()
    {
        Id = e.Id,
        TicketId = e.TicketId,
        OrderLineId = e.OrderLineId,
        MenuItemId = e.MenuItemId,
        ItemName = e.ItemName,
        VariantName = e.VariantName,
        Quantity = e.Quantity,
        ModifierSummary = e.ModifierSummary,
        SpecialInstructions = e.SpecialInstructions,
        AllergenWarning = e.AllergenWarning,
        SeatNumber = e.SeatNumber,
        Status = e.Status,
        ReadyAt = e.ReadyAt,
        DisplayOrder = e.DisplayOrder,
    };

    public static PrinterProfileDto ToDto(PrinterProfile e) => new()
    {
        Id = e.Id,
        OutletId = e.OutletId,
        Name = e.Name,
        Target = e.Target,
        PaperWidthMm = e.PaperWidthMm,
        IsReceiptPrinter = e.IsReceiptPrinter,
        IsKitchenPrinter = e.IsKitchenPrinter,
        IsLabelPrinter = e.IsLabelPrinter,
        OpensCashDrawer = e.OpensCashDrawer,
        CopiesPerTicket = e.CopiesPerTicket,
        HeaderText = e.HeaderText,
        FooterText = e.FooterText,
        IsActive = e.IsActive,
    };

    // ── Orders ───────────────────────────────────────────────────────────────

    public static RestaurantOrderDto ToDto(RestaurantOrder e, DateTime now)
    {
        var lines = e.Lines?.Where(l => !l.IsDeleted).OrderBy(l => l.DisplayOrder).ToList() ?? [];
        return new RestaurantOrderDto
        {
            Id = e.Id,
            OutletId = e.OutletId,
            OrderNumber = e.OrderNumber,
            TokenNumber = e.TokenNumber,
            OrderType = e.OrderType,
            Channel = e.Channel,
            Status = e.Status,
            TableId = e.TableId,
            TableNumber = e.TableNumber,
            SectionId = e.SectionId,
            GuestCount = e.GuestCount,
            WaiterId = e.WaiterId,
            WaiterName = e.WaiterName,
            SessionId = e.SessionId,
            GuestProfileId = e.GuestProfileId,
            CustomerId = e.CustomerId,
            CustomerName = e.CustomerName,
            CustomerPhone = e.CustomerPhone,
            SubTotal = e.SubTotal,
            DiscountAmount = e.DiscountAmount,
            ServiceChargeAmount = e.ServiceChargeAmount,
            PackagingChargeAmount = e.PackagingChargeAmount,
            DeliveryFeeAmount = e.DeliveryFeeAmount,
            TaxAmount = e.TaxAmount,
            TipAmount = e.TipAmount,
            RoundingAmount = e.RoundingAmount,
            TotalAmount = e.TotalAmount,
            PaidAmount = e.PaidAmount,
            BalanceDue = Math.Round(e.TotalAmount - e.PaidAmount, 2),
            CostAmount = e.CostAmount,
            CurrencyCode = e.CurrencyCode,
            OpenedAt = e.OpenedAt,
            FirstFiredAt = e.FirstFiredAt,
            ServedAt = e.ServedAt,
            BilledAt = e.BilledAt,
            ClosedAt = e.ClosedAt,
            PromisedAt = e.PromisedAt,
            ExternalSource = e.ExternalSource,
            ExternalReference = e.ExternalReference,
            WasOffline = e.WasOffline,
            Note = e.Note,
            CancelReason = e.CancelReason,
            MinutesOpen = (int)Math.Max(0, ((e.ClosedAt ?? now) - e.OpenedAt).TotalMinutes),
            HeldLineCount = lines.Count(l => l.IsHeld && !l.IsVoided),
            UnservedLineCount = lines.Count(l => !l.IsVoided && l.Status != OrderLineStatus.Served),
            Lines = lines.Select(ToDto).ToList(),
            StatusHistory = e.StatusHistory?.OrderBy(h => h.OccurredAt).Select(ToDto).ToList() ?? [],
        };
    }

    public static RestaurantOrderLineDto ToDto(RestaurantOrderLine e) => new()
    {
        Id = e.Id,
        OrderId = e.OrderId,
        MenuItemId = e.MenuItemId,
        VariantId = e.VariantId,
        ComboMealId = e.ComboMealId,
        ParentLineId = e.ParentLineId,
        ItemName = e.ItemName,
        VariantName = e.VariantName,
        Quantity = e.Quantity,
        UnitPrice = e.UnitPrice,
        ModifierAmount = e.ModifierAmount,
        DiscountAmount = e.DiscountAmount,
        TaxAmount = e.TaxAmount,
        LineTotal = e.LineTotal,
        UnitCost = e.UnitCost,
        TaxGroupId = e.TaxGroupId,
        TaxPercent = e.TaxPercent,
        SeatNumber = e.SeatNumber,
        Course = e.Course,
        CourseSequence = e.CourseSequence,
        Status = e.Status,
        IsHeld = e.IsHeld,
        StationId = e.StationId,
        FiredAt = e.FiredAt,
        ReadyAt = e.ReadyAt,
        ServedAt = e.ServedAt,
        IsVoided = e.IsVoided,
        VoidReasonId = e.VoidReasonId,
        VoidNote = e.VoidNote,
        IsComped = e.IsComped,
        SpecialInstructions = e.SpecialInstructions,
        DisplayOrder = e.DisplayOrder,
        Modifiers = e.Modifiers?.Where(m => !m.IsDeleted).Select(ToDto).ToList() ?? [],
    };

    public static OrderLineModifierDto ToDto(RestaurantOrderLineModifier e) => new()
    {
        Id = e.Id,
        OrderLineId = e.OrderLineId,
        ModifierId = e.ModifierId,
        ModifierGroupId = e.ModifierGroupId,
        ModifierName = e.ModifierName,
        GroupName = e.GroupName,
        Quantity = e.Quantity,
        PriceDelta = e.PriceDelta,
        CostDelta = e.CostDelta,
        IsRemoval = e.IsRemoval,
    };

    public static OrderStatusHistoryDto ToDto(OrderStatusHistory e) => new()
    {
        Id = e.Id,
        FromStatus = e.FromStatus,
        ToStatus = e.ToStatus,
        OccurredAt = e.OccurredAt,
        StaffId = e.StaffId,
        StaffName = e.StaffName,
        Note = e.Note,
    };

    public static RestaurantDeliveryDto ToDto(RestaurantDelivery e) => new()
    {
        Id = e.Id,
        OrderId = e.OrderId,
        OutletId = e.OutletId,
        Status = e.Status,
        RecipientName = e.RecipientName,
        Phone = e.Phone,
        AddressLine = e.AddressLine,
        Landmark = e.Landmark,
        City = e.City,
        Latitude = e.Latitude,
        Longitude = e.Longitude,
        ZoneName = e.ZoneName,
        DeliveryFee = e.DeliveryFee,
        DistanceKm = e.DistanceKm,
        RiderId = e.RiderId,
        RiderName = e.RiderName,
        RiderPhone = e.RiderPhone,
        AssignedAt = e.AssignedAt,
        PickedUpAt = e.PickedUpAt,
        DeliveredAt = e.DeliveredAt,
        EstimatedArrivalAt = e.EstimatedArrivalAt,
        FailureReason = e.FailureReason,
        DeliveryNote = e.DeliveryNote,
    };

    // ── Money ────────────────────────────────────────────────────────────────

    public static RestaurantCheckDto ToDto(RestaurantCheck e) => new()
    {
        Id = e.Id,
        OrderId = e.OrderId,
        OutletId = e.OutletId,
        CheckNumber = e.CheckNumber,
        Status = e.Status,
        SplitMethod = e.SplitMethod,
        SplitIndex = e.SplitIndex,
        SplitCount = e.SplitCount,
        SeatNumbers = e.SeatNumbers,
        SubTotal = e.SubTotal,
        DiscountAmount = e.DiscountAmount,
        ServiceChargeAmount = e.ServiceChargeAmount,
        PackagingChargeAmount = e.PackagingChargeAmount,
        DeliveryFeeAmount = e.DeliveryFeeAmount,
        TaxAmount = e.TaxAmount,
        TipAmount = e.TipAmount,
        RoundingAmount = e.RoundingAmount,
        TotalAmount = e.TotalAmount,
        PaidAmount = e.PaidAmount,
        BalanceDue = Math.Round(e.TotalAmount - e.PaidAmount, 2),
        ChangeAmount = e.ChangeAmount,
        CurrencyCode = e.CurrencyCode,
        SessionId = e.SessionId,
        CashierId = e.CashierId,
        CashierName = e.CashierName,
        WaiterId = e.WaiterId,
        PrintedAt = e.PrintedAt,
        PaidAt = e.PaidAt,
        PrintCount = e.PrintCount,
        SalesInvoiceId = e.SalesInvoiceId,
        FiscalReference = e.FiscalReference,
        IsVoided = e.IsVoided,
        VoidNote = e.VoidNote,
        VoidedAt = e.VoidedAt,
        Lines = e.Lines?.Where(l => !l.IsDeleted).OrderBy(l => l.DisplayOrder).Select(ToDto).ToList() ?? [],
        Payments = e.Payments?.Where(p => !p.IsDeleted).OrderBy(p => p.PaidAt).Select(ToDto).ToList() ?? [],
        Discounts = e.Discounts?.Where(d => !d.IsDeleted).Select(ToDto).ToList() ?? [],
    };

    public static CheckLineDto ToDto(CheckLine e) => new()
    {
        Id = e.Id,
        CheckId = e.CheckId,
        OrderLineId = e.OrderLineId,
        MenuItemId = e.MenuItemId,
        ItemName = e.ItemName,
        VariantName = e.VariantName,
        ModifierSummary = e.ModifierSummary,
        SeatNumber = e.SeatNumber,
        Quantity = e.Quantity,
        UnitPrice = e.UnitPrice,
        DiscountAmount = e.DiscountAmount,
        TaxAmount = e.TaxAmount,
        LineTotal = e.LineTotal,
        UnitCost = e.UnitCost,
        DisplayOrder = e.DisplayOrder,
    };

    public static CheckPaymentDto ToDto(CheckPayment e) => new()
    {
        Id = e.Id,
        CheckId = e.CheckId,
        TenderType = e.TenderType,
        Amount = e.Amount,
        TenderedAmount = e.TenderedAmount,
        ChangeAmount = e.ChangeAmount,
        TipAmount = e.TipAmount,
        CurrencyCode = e.CurrencyCode,
        ExchangeRate = e.ExchangeRate,
        Reference = e.Reference,
        CardLast4 = e.CardLast4,
        CardScheme = e.CardScheme,
        AuthCode = e.AuthCode,
        GiftCardId = e.GiftCardId,
        LoyaltyPointsUsed = e.LoyaltyPointsUsed,
        PaidAt = e.PaidAt,
        StaffId = e.StaffId,
        IsRefund = e.IsRefund,
        RefundReason = e.RefundReason,
    };

    public static CheckDiscountDto ToDto(CheckDiscount e) => new()
    {
        Id = e.Id,
        CheckId = e.CheckId,
        OrderLineId = e.OrderLineId,
        DiscountReasonId = e.DiscountReasonId,
        ReasonName = e.ReasonName,
        Kind = e.Kind,
        Value = e.Value,
        Amount = e.Amount,
        PromoCode = e.PromoCode,
        AppliedByStaffId = e.AppliedByStaffId,
        ApprovedByStaffId = e.ApprovedByStaffId,
        AppliedAt = e.AppliedAt,
    };

    public static VoidReasonDto ToDto(VoidReason e) => new()
    {
        Id = e.Id,
        Name = e.Name,
        DisplayOrder = e.DisplayOrder,
        RequiresApproval = e.RequiresApproval,
        CountsAsWastage = e.CountsAsWastage,
        IsActive = e.IsActive,
    };

    public static DiscountReasonDto ToDto(DiscountReason e) => new()
    {
        Id = e.Id,
        Name = e.Name,
        DisplayOrder = e.DisplayOrder,
        RequiresApproval = e.RequiresApproval,
        MaxAmountWithoutApproval = e.MaxAmountWithoutApproval,
        IsComp = e.IsComp,
        IsActive = e.IsActive,
    };

    public static ServiceChargeRuleDto ToDto(ServiceChargeRule e) => new()
    {
        Id = e.Id,
        OutletId = e.OutletId,
        Name = e.Name,
        Basis = e.Basis,
        Value = e.Value,
        MinPartySize = e.MinPartySize,
        ApplicableOrderTypes = e.ApplicableOrderTypes,
        IsTaxable = e.IsTaxable,
        TaxGroupId = e.TaxGroupId,
        IsWaivable = e.IsWaivable,
        RequiresApprovalToWaive = e.RequiresApprovalToWaive,
        Priority = e.Priority,
        IsActive = e.IsActive,
    };

    public static TipRecordDto ToDto(TipRecord e) => new()
    {
        Id = e.Id,
        OutletId = e.OutletId,
        CheckId = e.CheckId,
        WaiterId = e.WaiterId,
        WaiterName = e.WaiterName,
        Amount = e.Amount,
        TenderType = e.TenderType,
        IsDeclared = e.IsDeclared,
        ReceivedAt = e.ReceivedAt,
        TipPoolId = e.TipPoolId,
    };

    public static TipPoolDto ToDto(TipPool e) => new()
    {
        Id = e.Id,
        OutletId = e.OutletId,
        Name = e.Name,
        PeriodStart = e.PeriodStart,
        PeriodEnd = e.PeriodEnd,
        Basis = e.Basis,
        TotalAmount = e.TotalAmount,
        DistributedAmount = e.DistributedAmount,
        KitchenSharePercent = e.KitchenSharePercent,
        IsFinalised = e.IsFinalised,
        FinalisedAt = e.FinalisedAt,
        Distributions = e.Distributions?.Where(d => !d.IsDeleted).Select(ToDto).ToList() ?? [],
    };

    public static TipDistributionDto ToDto(TipDistribution e) => new()
    {
        Id = e.Id,
        TipPoolId = e.TipPoolId,
        StaffId = e.StaffId,
        StaffName = e.StaffName,
        Role = e.Role,
        HoursWorked = e.HoursWorked,
        SalesAmount = e.SalesAmount,
        SharePercent = e.SharePercent,
        Amount = e.Amount,
        IsPaidOut = e.IsPaidOut,
        PaidOutAt = e.PaidOutAt,
    };

    // ── Front of house ───────────────────────────────────────────────────────

    public static ReservationDto ToDto(Reservation e, DateTime now) => new()
    {
        Id = e.Id,
        OutletId = e.OutletId,
        ReservationNumber = e.ReservationNumber,
        Status = e.Status,
        GuestProfileId = e.GuestProfileId,
        GuestName = e.GuestName,
        Phone = e.Phone,
        Email = e.Email,
        PartySize = e.PartySize,
        ReservedFor = e.ReservedFor,
        DurationMinutes = e.DurationMinutes,
        TableId = e.TableId,
        SectionId = e.SectionId,
        FloorId = e.FloorId,
        Occasion = e.Occasion,
        SpecialRequests = e.SpecialRequests,
        AllergyNotes = e.AllergyNotes,
        IsHighChairNeeded = e.IsHighChairNeeded,
        IsWheelchairAccess = e.IsWheelchairAccess,
        DepositAmount = e.DepositAmount,
        IsDepositPaid = e.IsDepositPaid,
        ConfirmedAt = e.ConfirmedAt,
        SeatedAt = e.SeatedAt,
        CompletedAt = e.CompletedAt,
        CancelledAt = e.CancelledAt,
        CancelReason = e.CancelReason,
        OrderId = e.OrderId,
        ReminderSent = e.ReminderSent,
        Source = e.Source,
        Note = e.Note,
        MinutesUntil = (int)(e.ReservedFor - now).TotalMinutes,
        IsLate = e.Status is ReservationStatus.Confirmed or ReservationStatus.Requested
                 && e.ReservedFor < now.AddMinutes(-10),
    };

    public static WaitlistEntryDto ToDto(WaitlistEntry e, DateTime now)
    {
        var waited = (int)Math.Max(0, ((e.SeatedAt ?? e.LeftAt ?? now) - e.JoinedAt).TotalMinutes);
        return new WaitlistEntryDto
        {
            Id = e.Id,
            OutletId = e.OutletId,
            GuestName = e.GuestName,
            Phone = e.Phone,
            PartySize = e.PartySize,
            Status = e.Status,
            JoinedAt = e.JoinedAt,
            QuotedWaitMinutes = e.QuotedWaitMinutes,
            NotifiedAt = e.NotifiedAt,
            SeatedAt = e.SeatedAt,
            LeftAt = e.LeftAt,
            TableId = e.TableId,
            OrderId = e.OrderId,
            GuestProfileId = e.GuestProfileId,
            PreferredSectionId = e.PreferredSectionId,
            PagerNumber = e.PagerNumber,
            Note = e.Note,
            WaitedMinutes = waited,
            IsOverQuote = e.QuotedWaitMinutes > 0 && waited > e.QuotedWaitMinutes,
        };
    }

    public static GuestProfileDto ToDto(GuestProfile e) => new()
    {
        Id = e.Id,
        FullName = e.FullName,
        Phone = e.Phone,
        Email = e.Email,
        CustomerId = e.CustomerId,
        Birthday = e.Birthday,
        Anniversary = e.Anniversary,
        DietaryPreferences = e.DietaryPreferences,
        Allergies = e.Allergies,
        FavouriteItems = e.FavouriteItems,
        PreferredSeating = e.PreferredSeating,
        VisitCount = e.VisitCount,
        LifetimeSpend = e.LifetimeSpend,
        AverageCheck = e.AverageCheck,
        FirstVisitAt = e.FirstVisitAt,
        LastVisitAt = e.LastVisitAt,
        NoShowCount = e.NoShowCount,
        IsVip = e.IsVip,
        IsBlacklisted = e.IsBlacklisted,
        BlacklistReason = e.BlacklistReason,
        LoyaltyPoints = e.LoyaltyPoints,
        LoyaltyTier = e.LoyaltyTier,
        Notes = e.Notes,
        IsActive = e.IsActive,
    };

    public static CustomerFeedbackDto ToDto(CustomerFeedback e) => new()
    {
        Id = e.Id,
        OutletId = e.OutletId,
        OrderId = e.OrderId,
        CheckId = e.CheckId,
        GuestProfileId = e.GuestProfileId,
        WaiterId = e.WaiterId,
        TableId = e.TableId,
        OverallRating = e.OverallRating,
        FoodRating = e.FoodRating,
        ServiceRating = e.ServiceRating,
        AmbienceRating = e.AmbienceRating,
        ValueRating = e.ValueRating,
        Comment = e.Comment,
        GuestName = e.GuestName,
        Phone = e.Phone,
        SubmittedAt = e.SubmittedAt,
        IsResolved = e.IsResolved,
        ResolutionNote = e.ResolutionNote,
        ResolvedAt = e.ResolvedAt,
    };

    // ── Staff & cash ─────────────────────────────────────────────────────────

    public static RestaurantStaffDto ToDto(RestaurantStaff e, DateTime now) => new()
    {
        Id = e.Id,
        Code = e.Code,
        OutletId = e.OutletId,
        FullName = e.FullName,
        DisplayName = e.DisplayName,
        Role = e.Role,
        UserId = e.UserId,
        EmployeeId = e.EmployeeId,
        Phone = e.Phone,
        Email = e.Email,
        PhotoUrl = e.PhotoUrl,
        HasPin = !string.IsNullOrEmpty(e.PinHash),
        IsLocked = e.LockedUntil.HasValue && e.LockedUntil > now,
        CanTakeOrders = e.CanTakeOrders,
        CanVoidLines = e.CanVoidLines,
        CanApplyDiscounts = e.CanApplyDiscounts,
        CanApproveDiscounts = e.CanApproveDiscounts,
        CanOpenCashDrawer = e.CanOpenCashDrawer,
        CanCloseSession = e.CanCloseSession,
        CanRunReports = e.CanRunReports,
        CanEditMenu = e.CanEditMenu,
        CanManageTables = e.CanManageTables,
        CanServeAlcohol = e.CanServeAlcohol,
        DefaultSectionId = e.DefaultSectionId,
        HourlyRate = e.HourlyRate,
        TipSharePercent = e.TipSharePercent,
        HiredOn = e.HiredOn,
        IsActive = e.IsActive,
        Note = e.Note,
    };

    public static StaffSessionDto ToSession(RestaurantStaff e) => new()
    {
        StaffId = e.Id,
        FullName = e.FullName,
        DisplayName = e.DisplayName,
        Role = e.Role,
        OutletId = e.OutletId,
        SectionId = e.DefaultSectionId,
        CanTakeOrders = e.CanTakeOrders,
        CanVoidLines = e.CanVoidLines,
        CanApplyDiscounts = e.CanApplyDiscounts,
        CanApproveDiscounts = e.CanApproveDiscounts,
        CanOpenCashDrawer = e.CanOpenCashDrawer,
        CanCloseSession = e.CanCloseSession,
        CanRunReports = e.CanRunReports,
        CanEditMenu = e.CanEditMenu,
        CanManageTables = e.CanManageTables,
        CanServeAlcohol = e.CanServeAlcohol,
    };

    public static StaffShiftDto ToDto(StaffShift e) => new()
    {
        Id = e.Id,
        OutletId = e.OutletId,
        StaffId = e.StaffId,
        ShiftDate = e.ShiftDate,
        ScheduledStart = e.ScheduledStart,
        ScheduledEnd = e.ScheduledEnd,
        Status = e.Status,
        ActualStart = e.ActualStart,
        ActualEnd = e.ActualEnd,
        BreakMinutes = e.BreakMinutes,
        HoursWorked = e.HoursWorked,
        SectionId = e.SectionId,
        Role = e.Role,
        SalesAmount = e.SalesAmount,
        OrdersHandled = e.OrdersHandled,
        CoversServed = e.CoversServed,
        TipsEarned = e.TipsEarned,
        Note = e.Note,
    };

    public static TimeClockEntryDto ToDto(TimeClockEntry e) => new()
    {
        Id = e.Id,
        StaffId = e.StaffId,
        ShiftId = e.ShiftId,
        ClockedInAt = e.ClockedInAt,
        ClockedOutAt = e.ClockedOutAt,
        IsBreak = e.IsBreak,
        Hours = e.Hours,
        IsAdjusted = e.IsAdjusted,
        Note = e.Note,
    };

    public static RestaurantSessionDto ToDto(RestaurantSession e) => new()
    {
        Id = e.Id,
        OutletId = e.OutletId,
        SessionNumber = e.SessionNumber,
        Status = e.Status,
        CashierId = e.CashierId,
        CashierName = e.CashierName,
        TerminalName = e.TerminalName,
        OpenedAt = e.OpenedAt,
        ClosedAt = e.ClosedAt,
        OpeningFloat = e.OpeningFloat,
        ExpectedCash = e.ExpectedCash,
        ExpectedCard = e.ExpectedCard,
        ExpectedOther = e.ExpectedOther,
        CountedCash = e.CountedCash,
        CountedCard = e.CountedCard,
        CountedOther = e.CountedOther,
        CashVariance = e.CashVariance,
        IsBlindClose = e.IsBlindClose,
        TotalSales = e.TotalSales,
        TotalDiscounts = e.TotalDiscounts,
        TotalVoids = e.TotalVoids,
        TotalRefunds = e.TotalRefunds,
        TotalTips = e.TotalTips,
        TotalTax = e.TotalTax,
        TotalServiceCharge = e.TotalServiceCharge,
        OrderCount = e.OrderCount,
        CoverCount = e.CoverCount,
        CheckCount = e.CheckCount,
        XReadCount = e.XReadCount,
        ZReadAt = e.ZReadAt,
        ClosingNote = e.ClosingNote,
        CashMovements = e.CashMovements?.Where(m => !m.IsDeleted).OrderBy(m => m.OccurredAt).Select(ToDto).ToList() ?? [],
    };

    public static SessionCashMovementDto ToDto(SessionCashMovement e) => new()
    {
        Id = e.Id,
        SessionId = e.SessionId,
        MovementType = e.MovementType,
        Amount = e.Amount,
        Reason = e.Reason,
        Reference = e.Reference,
        StaffId = e.StaffId,
        StaffName = e.StaffName,
        OccurredAt = e.OccurredAt,
    };

    // ── Costing ──────────────────────────────────────────────────────────────

    public static RecipeDto ToDto(Recipe e) => new()
    {
        Id = e.Id,
        Code = e.Code,
        MenuItemId = e.MenuItemId,
        VariantId = e.VariantId,
        Name = e.Name,
        YieldQuantity = e.YieldQuantity,
        YieldUom = e.YieldUom,
        IsSubRecipe = e.IsSubRecipe,
        OutputInventoryItemId = e.OutputInventoryItemId,
        TotalCost = e.TotalCost,
        PrepTimeMinutes = e.PrepTimeMinutes,
        CookTimeMinutes = e.CookTimeMinutes,
        Instructions = e.Instructions,
        PlatingNotes = e.PlatingNotes,
        Version = e.Version,
        IsActive = e.IsActive,
        Description = e.Description,
        CostPerPortion = e.YieldQuantity > 0 ? Math.Round(e.TotalCost / e.YieldQuantity, 4) : e.TotalCost,
        Ingredients = e.Ingredients?.Where(i => !i.IsDeleted).OrderBy(i => i.DisplayOrder).Select(ToDto).ToList() ?? [],
    };

    public static RecipeIngredientDto ToDto(RecipeIngredient e) => new()
    {
        Id = e.Id,
        RecipeId = e.RecipeId,
        InventoryItemId = e.InventoryItemId,
        SubRecipeId = e.SubRecipeId,
        IngredientName = e.IngredientName,
        Quantity = e.Quantity,
        Uom = e.Uom,
        YieldPercent = e.YieldPercent,
        WastePercent = e.WastePercent,
        UnitCost = e.UnitCost,
        LineCost = e.LineCost,
        IsOptional = e.IsOptional,
        DisplayOrder = e.DisplayOrder,
        Note = e.Note,
    };

    public static WastageLogDto ToDto(WastageLog e) => new()
    {
        Id = e.Id,
        OutletId = e.OutletId,
        OccurredAt = e.OccurredAt,
        Reason = e.Reason,
        MenuItemId = e.MenuItemId,
        InventoryItemId = e.InventoryItemId,
        ItemName = e.ItemName,
        Quantity = e.Quantity,
        Uom = e.Uom,
        UnitCost = e.UnitCost,
        TotalCost = e.TotalCost,
        StaffId = e.StaffId,
        StaffName = e.StaffName,
        OrderLineId = e.OrderLineId,
        StationId = e.StationId,
        IsStockAdjusted = e.IsStockAdjusted,
        Note = e.Note,
    };

    public static RestaurantSettingsDto ToDto(RestaurantSettings e) => new()
    {
        Id = e.Id,
        RequireWaiterPin = e.RequireWaiterPin,
        RequireGuestCountOnSeat = e.RequireGuestCountOnSeat,
        RequireSeatNumbers = e.RequireSeatNumbers,
        AutoFireOnSend = e.AutoFireOnSend,
        SeatedAttentionMinutes = e.SeatedAttentionMinutes,
        ServedAttentionMinutes = e.ServedAttentionMinutes,
        AllowTableMerge = e.AllowTableMerge,
        AllowTableTransfer = e.AllowTableTransfer,
        AllowSplitBill = e.AllowSplitBill,
        PricesIncludeTax = e.PricesIncludeTax,
        TipsEnabled = e.TipsEnabled,
        TipPresetPercents = e.TipPresetPercents,
        TipPoolingEnabled = e.TipPoolingEnabled,
        TipDistributionBasis = e.TipDistributionBasis,
        KitchenTipSharePercent = e.KitchenTipSharePercent,
        CashRoundingIncrement = e.CashRoundingIncrement,
        PackagingChargePerOrder = e.PackagingChargePerOrder,
        VoidRequiresReason = e.VoidRequiresReason,
        DiscountRequiresReason = e.DiscountRequiresReason,
        DiscountApprovalThreshold = e.DiscountApprovalThreshold,
        KitchenDisplayEnabled = e.KitchenDisplayEnabled,
        PrintKitchenTickets = e.PrintKitchenTickets,
        ExpoScreenEnabled = e.ExpoScreenEnabled,
        KdsWarningMinutes = e.KdsWarningMinutes,
        AutoBumpOnServe = e.AutoBumpOnServe,
        ShowAllergenWarnings = e.ShowAllergenWarnings,
        DepleteStockOnCheckClose = e.DepleteStockOnCheckClose,
        Auto86OnZeroStock = e.Auto86OnZeroStock,
        TrackWastage = e.TrackWastage,
        ReservationsEnabled = e.ReservationsEnabled,
        WaitlistEnabled = e.WaitlistEnabled,
        ReservationHoldMinutes = e.ReservationHoldMinutes,
        DefaultReservationDuration = e.DefaultReservationDuration,
        RequireDepositForLargeParty = e.RequireDepositForLargeParty,
        LargePartyThreshold = e.LargePartyThreshold,
        PrintReceiptAutomatically = e.PrintReceiptAutomatically,
        EmailReceiptEnabled = e.EmailReceiptEnabled,
        ReceiptHeader = e.ReceiptHeader,
        ReceiptFooter = e.ReceiptFooter,
        ShowCalories = e.ShowCalories,
        FeedbackPromptEnabled = e.FeedbackPromptEnabled,
    };
}
