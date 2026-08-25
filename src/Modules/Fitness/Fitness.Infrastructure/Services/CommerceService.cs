using Fitness.Application.DTOs;
using Fitness.Application.Services.Interfaces;
using Fitness.Domain.Entities;
using Fitness.Domain.Enums;
using Fitness.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Nexcore.SharedKernel.Api;
using Nexcore.SharedKernel.Events;

namespace Fitness.Infrastructure.Services;

/// <summary>
/// The pro shop, the cash drawer, corporate accounts and third-party payers.
///
/// Two things worth noting. Retail **stock lives in Inventory** — this records the transaction and
/// publishes the depletion event exactly as Point of Sale does, because a second item master is a
/// second answer to "how many shakers are in the cupboard". And the cash session matches how POS
/// and Restaurant already work, so a manager who has closed one drawer in NexCore can close all
/// three without being retrained.
/// </summary>
public class CommerceService(
    FitnessDbContext db,
    IFitnessTenant tenant,
    FitnessNumbering numbering,
    IBillingService billing,
    IStaffService staff,
    IEventPublisher events) : ICommerceService
{
    // ── Retail ───────────────────────────────────────────────────────────────

    /// <summary>
    /// A till sale.
    ///
    /// Idempotency-keyed because a till gets double-tapped, and margin is captured at the moment
    /// of sale rather than recomputed later from a cost that has since moved.
    /// </summary>
    public async Task<FitnessSaleDto> CreateSaleAsync(CreateSaleDto request, Guid userId)
    {
        var now = DateTime.UtcNow;

        if (request.Lines.Count == 0)
            throw new InvalidOperationException("A sale needs at least one line.");

        if (!string.IsNullOrWhiteSpace(request.IdempotencyKey))
        {
            var existing = await db.Sales.ForTenant(tenant)
                .Include(s => s.Lines.Where(l => !l.IsDeleted))
                .Include(s => s.Member)
                .FirstOrDefaultAsync(s => s.Code == request.IdempotencyKey);

            if (existing is not null) return FitnessMapper.ToDto(existing);
        }

        var club = await db.Clubs.ForTenant(tenant).FirstOrDefaultAsync(c => c.Id == request.ClubId)
            ?? throw new InvalidOperationException("Club not found.");

        if (request.ChargeToHouseAccount && request.MemberId is null)
            throw new InvalidOperationException("A house-account charge needs a member.");

        var sale = new FitnessSale
        {
            SaleNumber = await numbering.NextSaleNumberAsync(request.ClubId, now),
            Code = request.IdempotencyKey,
            ClubId = request.ClubId,
            MemberId = request.MemberId,
            SoldAt = now,
            CurrencyCode = club.CurrencyCode,
            PaymentMethod = request.ChargeToHouseAccount ? PaymentMethod.MemberCredit : request.PaymentMethod,
            IsHouseAccountCharge = request.ChargeToHouseAccount,
            CashSessionId = request.CashSessionId,
            SoldByStaffId = request.SoldByStaffId ?? (userId == Guid.Empty ? null : userId),
            DiscountReason = request.DiscountReason,
            DiscountApprovedByUserId = request.DiscountApprovedByUserId,
        }.StampNew(tenant, userId);

        var order = 0;
        foreach (var lineDto in request.Lines)
        {
            var lineTotal = lineDto.Quantity * lineDto.UnitPrice - lineDto.DiscountAmount;
            var tax = Math.Round(lineTotal * lineDto.TaxPercent / 100m, 2);

            db.SaleLines.Add(new FitnessSaleLine
            {
                SaleId = sale.Id,
                InventoryItemId = lineDto.InventoryItemId,
                PlanId = lineDto.PlanId,
                ItemName = lineDto.ItemName,
                Barcode = lineDto.Barcode,
                Quantity = lineDto.Quantity,
                UnitPrice = lineDto.UnitPrice,
                DiscountAmount = lineDto.DiscountAmount,
                TaxPercent = lineDto.TaxPercent,
                TaxAmount = tax,
                LineTotal = lineTotal,
                Modifiers = lineDto.Modifiers,
                DisplayOrder = order++,
            }.StampNew(tenant, userId));

            sale.Subtotal += lineTotal;
            sale.DiscountTotal += lineDto.DiscountAmount;
            sale.TaxTotal += tax;
        }

        sale.DiscountTotal += request.DiscountTotal;
        sale.Subtotal -= request.DiscountTotal;
        sale.Total = sale.Subtotal + sale.TaxTotal;

        // A discount above the threshold needs a manager, checked here rather than in the UI.
        var settings = await db.Settings.ForTenant(tenant).FirstOrDefaultAsync() ?? new FitnessSettings();
        var discountPercent = sale.Subtotal + sale.DiscountTotal == 0
            ? 0
            : sale.DiscountTotal / (sale.Subtotal + sale.DiscountTotal) * 100m;

        if (discountPercent > settings.DiscountApprovalThresholdPercent && request.DiscountApprovedByUserId is null)
            throw new InvalidOperationException(
                $"A discount of {discountPercent:0}% needs a manager's approval.");

        db.Sales.Add(sale);

        // ── Money ────────────────────────────────────────────────────────────

        if (request.ChargeToHouseAccount)
        {
            var member = await db.Members.ForTenant(tenant).FirstAsync(m => m.Id == request.MemberId);

            db.HouseAccountCharges.Add(new HouseAccountCharge
            {
                MemberId = member.Id,
                ClubId = request.ClubId,
                SaleId = sale.Id,
                ChargedOn = now,
                Amount = sale.Total,
                ChargeDescription = string.Join(", ", request.Lines.Select(l => l.ItemName).Take(3)),
                AuthorisedByStaffId = userId == Guid.Empty ? null : userId,
            }.StampNew(tenant, userId));

            member.AccountBalance += sale.Total;
            member.StampUpdated(userId);

            db.Ledger.Add(new MemberLedgerEntry
            {
                MemberId = member.Id,
                ClubId = request.ClubId,
                Kind = LedgerEntryKind.Charge,
                OccurredAt = now,
                Amount = sale.Total,
                BalanceAfter = member.AccountBalance,
                EntryDescription = $"Pro shop — {sale.SaleNumber}",
                CurrencyCode = sale.CurrencyCode,
            }.StampNew(tenant, userId));
        }
        else if (request.MemberId is not null && sale.Total > 0)
        {
            var payment = await billing.TakePaymentAsync(new TakePaymentDto
            {
                MemberId = request.MemberId.Value,
                ClubId = request.ClubId,
                Amount = sale.Total,
                Method = request.PaymentMethod,
                CashSessionId = request.CashSessionId,
                AmountTendered = request.AmountTendered,
                Notes = $"Pro shop — {sale.SaleNumber}",
                IdempotencyKey = $"sale:{sale.Id}",
            }, userId);

            sale.PaymentId = payment.PaymentId;
        }
        else if (request.CashSessionId is not null && sale.Total > 0)
        {
            // A walk-in sale still has to land in the drawer.
            var session = await db.CashSessions.ForTenant(tenant)
                .FirstOrDefaultAsync(s => s.Id == request.CashSessionId);

            if (session is not null)
            {
                if (request.PaymentMethod == PaymentMethod.Cash) session.CashSales += sale.Total;
                else if (request.PaymentMethod == PaymentMethod.Card) session.CardSales += sale.Total;
                else session.OtherSales += sale.Total;

                session.TransactionCount++;
                RecomputeExpected(session);

                db.CashMovements.Add(new CashMovement
                {
                    CashSessionId = session.Id,
                    Kind = CashMovementKind.Sale,
                    Amount = request.PaymentMethod == PaymentMethod.Cash ? sale.Total : 0,
                    Reason = $"Sale {sale.SaleNumber}",
                    Reference = sale.SaleNumber,
                    SaleId = sale.Id,
                    StaffId = userId == Guid.Empty ? null : userId,
                }.StampNew(tenant, userId));
            }
        }

        // ── Stock ────────────────────────────────────────────────────────────

        await PublishStockMovementAsync(sale, request.Lines, club, userId);

        // Retail is commissionable in most clubs.
        if (sale.SoldByStaffId is not null && sale.Total > 0)
        {
            await staff.AccrueAsync(sale.SoldByStaffId.Value, CommissionBasis.PercentOfRetailSold,
                sale.Subtotal, 1, sale.Id, nameof(FitnessSale), request.MemberId,
                $"Pro shop sale {sale.SaleNumber}", userId);
        }

        await db.SaveChangesAsync();

        var saved = await db.Sales.ForTenant(tenant)
            .Include(s => s.Lines.Where(l => !l.IsDeleted))
            .Include(s => s.Member)
            .FirstAsync(s => s.Id == sale.Id);

        return FitnessMapper.ToDto(saved);
    }

    public async Task<FitnessSaleDto> ReturnSaleAsync(Guid saleId, string reason, List<Guid>? lineIds, Guid userId)
    {
        var now = DateTime.UtcNow;

        var original = await db.Sales.ForTenant(tenant)
            .Include(s => s.Lines.Where(l => !l.IsDeleted))
            .Include(s => s.Member)
            .FirstOrDefaultAsync(s => s.Id == saleId)
            ?? throw new InvalidOperationException("Sale not found.");

        if (original.IsReturn)
            throw new InvalidOperationException("That transaction is already a return.");

        var alreadyReturned = await db.Sales.ForTenant(tenant)
            .AnyAsync(s => s.ReturnsSaleId == saleId);

        if (alreadyReturned && lineIds is null)
            throw new InvalidOperationException("This sale has already been returned.");

        var lines = lineIds is null
            ? original.Lines.Where(l => !l.IsDeleted).ToList()
            : original.Lines.Where(l => !l.IsDeleted && lineIds.Contains(l.Id)).ToList();

        if (lines.Count == 0) throw new InvalidOperationException("Nothing to return.");

        // A return is its own negative sale rather than an edit to the original, so the day's
        // takings and the audit trail both stay honest.
        var returnSale = new FitnessSale
        {
            SaleNumber = await numbering.NextSaleNumberAsync(original.ClubId, now),
            ClubId = original.ClubId,
            MemberId = original.MemberId,
            SoldAt = now,
            CurrencyCode = original.CurrencyCode,
            PaymentMethod = original.PaymentMethod,
            CashSessionId = original.CashSessionId,
            SoldByStaffId = userId == Guid.Empty ? null : userId,
            IsReturn = true,
            ReturnsSaleId = saleId,
            ReturnReason = reason,
        }.StampNew(tenant, userId);

        var order = 0;
        foreach (var line in lines)
        {
            db.SaleLines.Add(new FitnessSaleLine
            {
                SaleId = returnSale.Id,
                InventoryItemId = line.InventoryItemId,
                PlanId = line.PlanId,
                ItemName = line.ItemName,
                Barcode = line.Barcode,
                Quantity = -line.Quantity,
                UnitPrice = line.UnitPrice,
                DiscountAmount = -line.DiscountAmount,
                TaxPercent = line.TaxPercent,
                TaxAmount = -line.TaxAmount,
                LineTotal = -line.LineTotal,
                UnitCost = line.UnitCost,
                DisplayOrder = order++,
            }.StampNew(tenant, userId));

            returnSale.Subtotal -= line.LineTotal;
            returnSale.TaxTotal -= line.TaxAmount;
        }

        returnSale.Total = returnSale.Subtotal + returnSale.TaxTotal;
        db.Sales.Add(returnSale);

        if (original.MemberId is not null && returnSale.Total < 0)
        {
            await billing.IssueCreditNoteAsync(new IssueCreditNoteDto
            {
                MemberId = original.MemberId.Value,
                Amount = Math.Abs(returnSale.Total),
                Reason = $"Return of {original.SaleNumber} — {reason}",
                AppliedToBalance = true,
            }, userId);
        }

        if (returnSale.CashSessionId is not null)
        {
            var session = await db.CashSessions.ForTenant(tenant)
                .FirstOrDefaultAsync(s => s.Id == returnSale.CashSessionId);

            if (session is not null)
            {
                session.Refunds += Math.Abs(returnSale.Total);
                RecomputeExpected(session);

                db.CashMovements.Add(new CashMovement
                {
                    CashSessionId = session.Id,
                    Kind = CashMovementKind.Refund,
                    Amount = returnSale.Total,
                    Reason = reason,
                    Reference = returnSale.SaleNumber,
                    SaleId = returnSale.Id,
                }.StampNew(tenant, userId));
            }
        }

        await db.SaveChangesAsync();

        var saved = await db.Sales.ForTenant(tenant)
            .Include(s => s.Lines.Where(l => !l.IsDeleted))
            .Include(s => s.Member)
            .FirstAsync(s => s.Id == returnSale.Id);

        return FitnessMapper.ToDto(saved);
    }

    public async Task<PaginatedResponse<FitnessSaleDto>> ListSalesAsync(
        Guid? clubId, Guid? memberId, DateTime? from, DateTime? to, PaginationParams pagination)
    {
        var query = db.Sales.ForTenant(tenant)
            .Include(s => s.Lines.Where(l => !l.IsDeleted))
            .Include(s => s.Member)
            .WhereIf(clubId is not null, s => s.ClubId == clubId)
            .WhereIf(memberId is not null, s => s.MemberId == memberId)
            .WhereIf(from is not null, s => s.SoldAt >= from)
            .WhereIf(to is not null, s => s.SoldAt <= to);

        var total = await query.CountAsync();

        var page = await query
            .OrderByDescending(s => s.SoldAt)
            .Skip((pagination.PageNumber - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .ToListAsync();

        var clubNames = await db.Clubs.ForTenant(tenant)
            .Select(c => new { c.Id, c.Name }).ToDictionaryAsync(c => c.Id, c => c.Name);

        var staffNames = await db.Staff.ForTenant(tenant)
            .Select(s => new { s.Id, Name = s.FirstName + " " + s.LastName })
            .ToDictionaryAsync(s => s.Id, s => s.Name);

        var items = page.Select(s =>
        {
            var dto = FitnessMapper.ToDto(s);
            dto.ClubName = clubNames.GetValueOrDefault(s.ClubId);
            if (s.SoldByStaffId is not null) dto.SoldByName = staffNames.GetValueOrDefault(s.SoldByStaffId.Value);
            return dto;
        }).ToList();

        return PaginatedResponse<FitnessSaleDto>.Ok(
                   items,
                   total,
                   pagination.PageNumber,
                   pagination.PageSize);
    }

    /// <summary>
    /// The pro-shop catalogue.
    ///
    /// Products are plans of kind <see cref="PlanKind.RetailProduct"/> that reference an Inventory
    /// item; the price and the barcode live here, the stock lives there.
    /// </summary>
    public async Task<List<RetailProductDto>> GetProductsAsync(Guid clubId, string? search)
    {
        var products = await db.Plans.ForTenant(tenant)
            .Where(p => p.Kind == PlanKind.RetailProduct && p.IsActive)
            .Include(p => p.ClubPrices.Where(c => !c.IsDeleted))
            .OrderBy(p => p.DisplayOrder).ThenBy(p => p.Name)
            .ToListAsync();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLowerInvariant();
            products = [.. products.Where(p =>
                p.Name.ToLowerInvariant().Contains(term) ||
                (p.Code ?? string.Empty).ToLowerInvariant().Contains(term))];
        }

        return [.. products.Select(p =>
        {
            var clubPrice = p.ClubPrices.FirstOrDefault(c => c.ClubId == clubId && c.IsAvailable);

            return new RetailProductDto
            {
                Id = p.Id,
                InventoryItemId = p.InventoryItemId,
                Name = p.Name,
                Barcode = p.Code,
                Category = "Pro shop",
                ImageUrl = p.ImageUrl,
                Price = clubPrice?.Price ?? p.Price,
                TaxPercent = p.TaxPercent,
                TracksStock = p.InventoryItemId is not null,
                IsActive = p.IsActive,
            };
        })];
    }

    public async Task<List<HouseAccountChargeDto>> GetHouseAccountAsync(Guid memberId, bool unsettledOnly)
    {
        var charges = await db.HouseAccountCharges.ForTenant(tenant)
            .Where(h => h.MemberId == memberId)
            .WhereIf(unsettledOnly, h => !h.IsSettled)
            .Include(h => h.Member)
            .OrderByDescending(h => h.ChargedOn)
            .Take(200)
            .ToListAsync();

        var saleNumbers = await db.Sales.ForTenant(tenant)
            .Select(s => new { s.Id, s.SaleNumber })
            .ToDictionaryAsync(s => s.Id, s => s.SaleNumber);

        var staffNames = await db.Staff.ForTenant(tenant)
            .Select(s => new { s.Id, Name = s.FirstName + " " + s.LastName })
            .ToDictionaryAsync(s => s.Id, s => s.Name);

        return [.. charges.Select(h =>
        {
            var dto = FitnessMapper.ToDto(h);
            if (h.SaleId is not null) dto.SaleNumber = saleNumbers.GetValueOrDefault(h.SaleId.Value);
            if (h.AuthorisedByStaffId is not null) dto.AuthorisedByName = staffNames.GetValueOrDefault(h.AuthorisedByStaffId.Value);
            return dto;
        })];
    }

    // ── Cash ─────────────────────────────────────────────────────────────────

    public async Task<CashSessionDto> OpenSessionAsync(OpenCashSessionDto request, Guid userId)
    {
        var now = DateTime.UtcNow;

        var open = await db.CashSessions.ForTenant(tenant)
            .FirstOrDefaultAsync(s => s.ClubId == request.ClubId && s.Status == CashSessionStatus.Open);

        if (open is not null)
            throw new InvalidOperationException($"Session {open.SessionNumber} is still open. Close it first.");

        var club = await db.Clubs.ForTenant(tenant).FirstOrDefaultAsync(c => c.Id == request.ClubId)
            ?? throw new InvalidOperationException("Club not found.");

        var session = new CashSession
        {
            SessionNumber = await numbering.NextSessionNumberAsync(request.ClubId, now),
            ClubId = request.ClubId,
            OpenedByStaffId = request.StaffId ?? (userId == Guid.Empty ? null : userId),
            Status = CashSessionStatus.Open,
            OpenedAt = now,
            OpeningFloat = request.OpeningFloat,
            ExpectedCash = request.OpeningFloat,
            CurrencyCode = club.CurrencyCode,
        }.StampNew(tenant, userId);

        db.CashSessions.Add(session);

        db.CashMovements.Add(new CashMovement
        {
            CashSessionId = session.Id,
            Kind = CashMovementKind.OpeningFloat,
            OccurredAt = now,
            Amount = request.OpeningFloat,
            Reason = "Opening float",
            StaffId = session.OpenedByStaffId,
        }.StampNew(tenant, userId));

        await db.SaveChangesAsync();
        return FitnessMapper.ToDto(session);
    }

    public async Task<CashSessionDto?> GetOpenSessionAsync(Guid clubId, Guid? staffId)
    {
        var session = await db.CashSessions.ForTenant(tenant)
            .Include(s => s.Movements.Where(m => !m.IsDeleted))
            .Where(s => s.ClubId == clubId && s.Status == CashSessionStatus.Open)
            .WhereIf(staffId is not null, s => s.OpenedByStaffId == staffId)
            .FirstOrDefaultAsync();

        return session is null ? null : await DecorateSessionAsync(session);
    }

    public async Task<CashSessionDto?> GetSessionAsync(Guid sessionId)
    {
        var session = await db.CashSessions.ForTenant(tenant)
            .Include(s => s.Movements.Where(m => !m.IsDeleted))
            .FirstOrDefaultAsync(s => s.Id == sessionId);

        return session is null ? null : await DecorateSessionAsync(session);
    }

    public async Task<CashSessionDto> RecordMovementAsync(CashMovementRequestDto request, Guid userId)
    {
        var now = DateTime.UtcNow;

        var session = await db.CashSessions.ForTenant(tenant)
            .Include(s => s.Movements.Where(m => !m.IsDeleted))
            .FirstOrDefaultAsync(s => s.Id == request.SessionId)
            ?? throw new InvalidOperationException("Session not found.");

        if (session.Status != CashSessionStatus.Open)
            throw new InvalidOperationException("That session has been closed.");

        // Paid-out and drops leave the drawer, so they are negative regardless of what was sent.
        var amount = request.Kind switch
        {
            CashMovementKind.PaidOut or CashMovementKind.Drop => -Math.Abs(request.Amount),
            _ => Math.Abs(request.Amount),
        };

        db.CashMovements.Add(new CashMovement
        {
            CashSessionId = session.Id,
            Kind = request.Kind,
            OccurredAt = now,
            Amount = amount,
            Reason = request.Reason,
            Reference = request.Reference,
            StaffId = userId == Guid.Empty ? null : userId,
        }.StampNew(tenant, userId));

        switch (request.Kind)
        {
            case CashMovementKind.PaidIn: session.PaidIn += Math.Abs(amount); break;
            case CashMovementKind.PaidOut: session.PaidOut += Math.Abs(amount); break;
            case CashMovementKind.Drop: session.Drops += Math.Abs(amount); break;
        }

        RecomputeExpected(session);
        session.StampUpdated(userId);

        await db.SaveChangesAsync();
        return await DecorateSessionAsync(session);
    }

    /// <summary>
    /// Closing a drawer.
    ///
    /// The count is blind by default: the expected figure is not shown before the staff member
    /// enters what they counted. A visible target is a target, and a variance that can be
    /// reverse-engineered tells you nothing.
    /// </summary>
    public async Task<CashSessionDto> CloseSessionAsync(CloseCashSessionDto request, Guid userId)
    {
        var now = DateTime.UtcNow;

        var session = await db.CashSessions.ForTenant(tenant)
            .Include(s => s.Movements.Where(m => !m.IsDeleted))
            .FirstOrDefaultAsync(s => s.Id == request.SessionId)
            ?? throw new InvalidOperationException("Session not found.");

        if (session.Status == CashSessionStatus.Closed)
            throw new InvalidOperationException("That session is already closed.");

        RecomputeExpected(session);

        session.CountedCash = request.CountedCash;
        session.Variance = request.CountedCash - session.ExpectedCash;
        session.WasBlindCount = true;
        session.ClosedAt = now;
        session.ClosedByStaffId = userId == Guid.Empty ? null : userId;
        session.Status = CashSessionStatus.Closed;
        session.VarianceNote = request.VarianceNote;
        session.StampUpdated(userId);

        db.CashMovements.Add(new CashMovement
        {
            CashSessionId = session.Id,
            Kind = CashMovementKind.ClosingCount,
            OccurredAt = now,
            Amount = request.CountedCash,
            Reason = "Counted at close",
            StaffId = session.ClosedByStaffId,
        }.StampNew(tenant, userId));

        if (session.Variance != 0)
        {
            db.CashMovements.Add(new CashMovement
            {
                CashSessionId = session.Id,
                Kind = CashMovementKind.Variance,
                OccurredAt = now,
                Amount = session.Variance,
                Reason = session.Variance > 0 ? "Over" : "Short",
                StaffId = session.ClosedByStaffId,
            }.StampNew(tenant, userId));

            // A material variance is not just a note — somebody looks at it.
            if (Math.Abs(session.Variance) >= 10)
            {
                db.MessageLog.Add(new MessageLog
                {
                    ClubId = session.ClubId,
                    Channel = MessageChannel.DeskAlert,
                    Status = MessageStatus.Queued,
                    Subject = "Cash variance",
                    BodyPreview = $"Session {session.SessionNumber} closed {(session.Variance > 0 ? "over" : "short")} " +
                                  $"by {Math.Abs(session.Variance):0.00}.",
                    QueuedAt = now,
                }.StampNew(tenant, userId));
            }
        }

        await db.SaveChangesAsync();
        return await DecorateSessionAsync(session);
    }

    public async Task<PaginatedResponse<CashSessionDto>> ListSessionsAsync(
        Guid? clubId, DateTime? from, DateTime? to, PaginationParams pagination)
    {
        var query = db.CashSessions.ForTenant(tenant)
            .WhereIf(clubId is not null, s => s.ClubId == clubId)
            .WhereIf(from is not null, s => s.OpenedAt >= from)
            .WhereIf(to is not null, s => s.OpenedAt <= to);

        var total = await query.CountAsync();

        var page = await query
            .OrderByDescending(s => s.OpenedAt)
            .Skip((pagination.PageNumber - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .ToListAsync();

        var items = new List<CashSessionDto>();
        foreach (var session in page) items.Add(await DecorateSessionAsync(session));

        return PaginatedResponse<CashSessionDto>.Ok(
                   items,
                   total,
                   pagination.PageNumber,
                   pagination.PageSize);
    }

    /// <summary>
    /// The day-end read.
    ///
    /// An X-read is a look without closing anything; a Z-read is the end of the day. Both show the
    /// same numbers, which is deliberate — a manager should be able to reconcile at 3pm and
    /// recognise the figures at 10pm.
    /// </summary>
    public async Task<DayEndReadDto> GetDayEndReadAsync(Guid clubId, DateTime forDate, bool isZRead)
    {
        var day = forDate.Date;
        var next = day.AddDays(1);

        var club = await db.Clubs.ForTenant(tenant).FirstOrDefaultAsync(c => c.Id == clubId)
            ?? throw new InvalidOperationException("Club not found.");

        var payments = await db.Payments.ForTenant(tenant)
            .Where(p => p.ClubId == clubId && p.ReceivedOn >= day && p.ReceivedOn < next
                     && p.Status == PaymentStatus.Succeeded)
            .ToListAsync();

        var refunds = await db.Refunds.ForTenant(tenant)
            .Where(r => r.ClubId == clubId && r.ProcessedOn >= day && r.ProcessedOn < next
                     && r.Status == PaymentStatus.Succeeded)
            .SumAsync(r => (decimal?)r.Amount) ?? 0m;

        var sessions = await db.CashSessions.ForTenant(tenant)
            .Where(s => s.ClubId == clubId && s.OpenedAt >= day && s.OpenedAt < next)
            .ToListAsync();

        var read = new DayEndReadDto
        {
            ClubId = clubId,
            ClubName = club.Name,
            ForDate = day,
            IsZRead = isZRead,
            CurrencyCode = club.CurrencyCode,
            CashTakings = payments.Where(p => p.Method == PaymentMethod.Cash).Sum(p => p.Amount),
            CardTakings = payments.Where(p => p.Method == PaymentMethod.Card).Sum(p => p.Amount),
            DirectDebitCollected = payments.Where(p => p.Method == PaymentMethod.DirectDebit).Sum(p => p.Amount),
            OtherTakings = payments.Where(p => p.Method is not (PaymentMethod.Cash or PaymentMethod.Card or PaymentMethod.DirectDebit))
                .Sum(p => p.Amount),
            Refunds = refunds,
            TransactionCount = payments.Count,
            ExpectedCash = sessions.Sum(s => s.ExpectedCash),
            CountedCash = sessions.Sum(s => s.CountedCash),
        };

        read.TotalTakings = read.CashTakings + read.CardTakings + read.DirectDebitCollected + read.OtherTakings;
        read.NetTakings = read.TotalTakings - read.Refunds;
        read.Variance = sessions.Sum(s => s.Variance);

        read.JoinCount = await db.Agreements.ForTenant(tenant)
            .CountAsync(a => a.ClubId == clubId && a.StartsOn >= day && a.StartsOn < next);

        read.CancellationCount = await db.CancellationRequests.ForTenant(tenant)
            .CountAsync(c => c.RequestedOn >= day && c.RequestedOn < next && c.Agreement!.ClubId == clubId);

        read.VisitCount = await db.CheckIns.ForTenant(tenant)
            .CountAsync(c => c.ClubId == clubId && c.CheckedInAt >= day && c.CheckedInAt < next);

        // What the money was actually for, from the invoice lines behind the payments.
        var invoiceIds = payments.Where(p => p.InvoiceId is not null).Select(p => p.InvoiceId!.Value).ToList();

        var lines = await db.InvoiceLines.ForTenant(tenant)
            .Where(l => invoiceIds.Contains(l.InvoiceId))
            .GroupBy(l => l.ChargeKind)
            .Select(g => new { Kind = g.Key, Amount = g.Sum(l => l.LineTotal), Count = g.Count() })
            .ToListAsync();

        read.ByCategory = [.. lines
            .Select(l => new RevenueLineDto
            {
                Label = Describe(l.Kind),
                Amount = l.Amount,
                Count = l.Count,
                PercentOfTotal = read.TotalTakings == 0 ? 0 : Math.Round(l.Amount / read.TotalTakings * 100m, 1),
            })
            .OrderByDescending(l => l.Amount)];

        var retail = await db.Sales.ForTenant(tenant)
            .Where(s => s.ClubId == clubId && s.SoldAt >= day && s.SoldAt < next)
            .SumAsync(s => (decimal?)s.Total) ?? 0m;

        if (retail != 0)
        {
            read.ByCategory.Add(new RevenueLineDto
            {
                Label = "Pro shop",
                Amount = retail,
                PercentOfTotal = read.TotalTakings == 0 ? 0 : Math.Round(retail / read.TotalTakings * 100m, 1),
            });
        }

        return read;
    }

    // ── Gift cards ───────────────────────────────────────────────────────────

    public async Task<GiftCardDto> IssueGiftCardAsync(IssueGiftCardDto request, Guid userId)
    {
        var now = DateTime.UtcNow;

        var club = await db.Clubs.ForTenant(tenant).FirstOrDefaultAsync(c => c.Id == request.ClubId)
            ?? throw new InvalidOperationException("Club not found.");

        var number = string.IsNullOrWhiteSpace(request.CardNumber)
            ? FitnessNumbering.NewGiftCardNumber()
            : request.CardNumber.Trim().ToUpperInvariant();

        var clash = await db.GiftCards.ForTenant(tenant).AnyAsync(g => g.CardNumber == number);
        if (clash) throw new InvalidOperationException("That card number is already in use.");

        var card = new GiftCard
        {
            CardNumber = number,
            ClubId = request.ClubId,
            InitialValue = request.Value,
            Balance = request.Value,
            CurrencyCode = club.CurrencyCode,
            IssuedOn = now,
            ExpiresOn = request.ExpiresOn,
            PurchasedByMemberId = request.PurchasedByMemberId,
            RecipientName = request.RecipientName,
            RecipientEmail = request.RecipientEmail,
            Message = request.Message,
        }.StampNew(tenant, userId);

        db.GiftCards.Add(card);

        db.GiftCardTransactions.Add(new GiftCardTransaction
        {
            GiftCardId = card.Id,
            OccurredAt = now,
            Amount = request.Value,
            BalanceAfter = request.Value,
            MemberId = request.PurchasedByMemberId,
            Note = "Issued",
        }.StampNew(tenant, userId));

        if (request.PurchasedByMemberId is not null && request.Value > 0)
        {
            await billing.TakePaymentAsync(new TakePaymentDto
            {
                MemberId = request.PurchasedByMemberId.Value,
                ClubId = request.ClubId,
                Amount = request.Value,
                Method = request.PaymentMethod,
                CashSessionId = request.CashSessionId,
                Notes = $"Gift card {number}",
                IdempotencyKey = $"giftcard:{card.Id}",
            }, userId);
        }

        await db.SaveChangesAsync();
        return FitnessMapper.ToDto(card, now);
    }

    public async Task<GiftCardDto?> GetGiftCardAsync(string cardNumber)
    {
        var number = cardNumber.Trim().ToUpperInvariant();

        var card = await db.GiftCards.ForTenant(tenant)
            .FirstOrDefaultAsync(g => g.CardNumber == number);

        return card is null ? null : FitnessMapper.ToDto(card, DateTime.UtcNow);
    }

    public async Task<GiftCardDto> RedeemGiftCardAsync(string cardNumber, decimal amount, Guid? saleId, Guid userId)
    {
        var now = DateTime.UtcNow;
        var number = cardNumber.Trim().ToUpperInvariant();

        var card = await db.GiftCards.ForTenant(tenant).FirstOrDefaultAsync(g => g.CardNumber == number)
            ?? throw new InvalidOperationException("That gift card was not recognised.");

        if (card.IsCancelled) throw new InvalidOperationException("That gift card has been cancelled.");
        if (card.ExpiresOn is not null && card.ExpiresOn < now)
            throw new InvalidOperationException($"That gift card expired on {card.ExpiresOn:d MMM yyyy}.");

        if (amount > card.Balance)
            throw new InvalidOperationException($"Only {card.Balance:0.00} is left on that card.");

        card.Balance -= amount;
        card.IsRedeemed = card.Balance <= 0;
        card.StampUpdated(userId);

        db.GiftCardTransactions.Add(new GiftCardTransaction
        {
            GiftCardId = card.Id,
            OccurredAt = now,
            Amount = -amount,
            BalanceAfter = card.Balance,
            SaleId = saleId,
            Note = "Redeemed",
        }.StampNew(tenant, userId));

        await db.SaveChangesAsync();
        return FitnessMapper.ToDto(card, now);
    }

    // ── Corporate ────────────────────────────────────────────────────────────

    public async Task<PaginatedResponse<CorporateAccountDto>> GetCorporateAccountsAsync(
        Guid? clubId, bool activeOnly, PaginationParams pagination)
    {
        var now = DateTime.UtcNow;

        var query = db.CorporateAccounts.ForTenant(tenant)
            .Include(c => c.EligibilityRules.Where(r => !r.IsDeleted))
            .WhereIf(clubId is not null, c => c.ClubId == clubId)
            .WhereIf(activeOnly, c => c.IsActive);

        var total = await query.CountAsync();

        var page = await query
            .OrderBy(c => c.Name)
            .Skip((pagination.PageNumber - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .ToListAsync();

        var ids = page.Select(c => c.Id).ToList();
        var thirtyDaysAgo = now.AddDays(-30);

        var members = await db.CorporateMembers.ForTenant(tenant)
            .Where(m => ids.Contains(m.CorporateAccountId) && m.IsActive)
            .Include(m => m.Member)
            .Select(m => new { m.CorporateAccountId, m.MemberId, m.EmployerContribution, LastVisit = m.Member!.LastVisitOn })
            .ToListAsync();

        var outstanding = await db.CorporateInvoices.ForTenant(tenant)
            .Where(i => ids.Contains(i.CorporateAccountId) && i.BalanceDue > 0)
            .GroupBy(i => i.CorporateAccountId)
            .Select(g => new { AccountId = g.Key, Amount = g.Sum(i => i.BalanceDue) })
            .ToListAsync();

        var clubNames = await db.Clubs.ForTenant(tenant)
            .Select(c => new { c.Id, c.Name }).ToDictionaryAsync(c => c.Id, c => c.Name);

        var planNames = await db.Plans.ForTenant(tenant)
            .Select(p => new { p.Id, p.Name }).ToDictionaryAsync(p => p.Id, p => p.Name);

        var items = page.Select(c =>
        {
            var dto = FitnessMapper.ToDto(c, now);
            dto.ClubName = clubNames.GetValueOrDefault(c.ClubId);
            if (c.DefaultPlanId is not null) dto.DefaultPlanName = planNames.GetValueOrDefault(c.DefaultPlanId.Value);

            var mine = members.Where(m => m.CorporateAccountId == c.Id).ToList();
            dto.CurrentMemberCount = mine.Count;
            dto.MonthlyValue = mine.Sum(m => m.EmployerContribution);
            dto.OutstandingBalance = outstanding.FirstOrDefault(o => o.AccountId == c.Id)?.Amount ?? 0m;

            // Usage is what the employer bought, and the number they will ask about at renewal.
            dto.ActiveUsersLast30Days = mine.Count(m => m.LastVisit >= thirtyDaysAgo);
            dto.UtilisationPercent = FitnessMapper.Percent(dto.ActiveUsersLast30Days, mine.Count);

            return dto;
        }).ToList();

        return PaginatedResponse<CorporateAccountDto>.Ok(
                   items,
                   total,
                   pagination.PageNumber,
                   pagination.PageSize);
    }

    public async Task<CorporateAccountDto?> GetCorporateAccountAsync(Guid id)
    {
        var now = DateTime.UtcNow;

        var account = await db.CorporateAccounts.ForTenant(tenant)
            .Include(c => c.EligibilityRules.Where(r => !r.IsDeleted))
            .FirstOrDefaultAsync(c => c.Id == id);

        if (account is null) return null;

        var dto = FitnessMapper.ToDto(account, now);

        dto.ClubName = await db.Clubs.ForTenant(tenant)
            .Where(c => c.Id == account.ClubId).Select(c => c.Name).FirstOrDefaultAsync();

        dto.CurrentMemberCount = await db.CorporateMembers.ForTenant(tenant)
            .CountAsync(m => m.CorporateAccountId == id && m.IsActive);

        dto.OutstandingBalance = await db.CorporateInvoices.ForTenant(tenant)
            .Where(i => i.CorporateAccountId == id && i.BalanceDue > 0)
            .SumAsync(i => (decimal?)i.BalanceDue) ?? 0m;

        return dto;
    }

    public async Task<CorporateAccountDto> SaveCorporateAccountAsync(Guid? id, CorporateAccountDto request, Guid userId)
    {
        CorporateAccount account;
        if (id is null)
        {
            account = new CorporateAccount { ClubId = request.ClubId }.StampNew(tenant, userId);
            db.CorporateAccounts.Add(account);
        }
        else
        {
            account = await db.CorporateAccounts.ForTenant(tenant)
                .Include(c => c.EligibilityRules.Where(r => !r.IsDeleted))
                .FirstOrDefaultAsync(c => c.Id == id)
                ?? throw new InvalidOperationException("Corporate account not found.");
            account.StampUpdated(userId);
        }

        account.Code = request.Code;
        account.Name = request.Name;
        account.CrmAccountId = request.CrmAccountId;
        account.ContactName = request.ContactName;
        account.ContactEmail = request.ContactEmail;
        account.ContactPhone = request.ContactPhone;
        account.AddressLine = request.AddressLine;
        account.TaxRegistrationNumber = request.TaxRegistrationNumber;
        account.BillingModel = request.BillingModel;
        account.NegotiatedRate = request.NegotiatedRate;
        account.DiscountPercent = request.DiscountPercent;
        account.SubsidyPerMember = request.SubsidyPerMember;
        account.SubsidyPercent = request.SubsidyPercent;
        account.DefaultPlanId = request.DefaultPlanId;
        account.ContractStartsOn = request.ContractStartsOn;
        account.ContractEndsOn = request.ContractEndsOn;
        account.MaxMembers = request.MaxMembers;
        account.InvoiceDayOfMonth = Math.Clamp(request.InvoiceDayOfMonth, 1, 28);
        account.PaymentTermsDays = request.PaymentTermsDays;
        account.ReceivesUsageReport = request.ReceivesUsageReport;
        account.IsActive = request.IsActive;

        foreach (var existing in account.EligibilityRules.Where(r => !r.IsDeleted)) existing.StampDeleted(userId);

        foreach (var ruleDto in request.EligibilityRules)
        {
            db.EligibilityRules.Add(new CorporateEligibilityRule
            {
                CorporateAccountId = account.Id,
                Proof = ruleDto.Proof,
                MatchValue = ruleDto.MatchValue,
                RequiresManualApproval = ruleDto.RequiresManualApproval,
                RevalidateEveryDays = ruleDto.RevalidateEveryDays,
                IsActive = ruleDto.IsActive,
            }.StampNew(tenant, userId));
        }

        await db.SaveChangesAsync();
        return (await GetCorporateAccountAsync(account.Id))!;
    }

    public async Task<List<CorporateMemberDto>> GetCorporateMembersAsync(Guid accountId, bool activeOnly)
    {
        var now = DateTime.UtcNow;
        var thirtyDaysAgo = now.AddDays(-30);

        var members = await db.CorporateMembers.ForTenant(tenant)
            .Where(m => m.CorporateAccountId == accountId)
            .WhereIf(activeOnly, m => m.IsActive)
            .Include(m => m.Member)
            .OrderBy(m => m.Member!.LastName)
            .ToListAsync();

        var memberIds = members.Select(m => m.MemberId).ToList();

        var visits = await db.CheckIns.ForTenant(tenant)
            .Where(c => c.MemberId != null && memberIds.Contains(c.MemberId.Value) && c.CheckedInAt >= thirtyDaysAgo)
            .GroupBy(c => c.MemberId!.Value)
            .Select(g => new { MemberId = g.Key, Count = g.Count() })
            .ToListAsync();

        return [.. members.Select(m =>
        {
            var dto = FitnessMapper.ToDto(m, now);
            dto.VisitsLast30Days = visits.FirstOrDefault(v => v.MemberId == m.MemberId)?.Count ?? 0;
            return dto;
        })];
    }

    public async Task<CorporateMemberDto> AddCorporateMemberAsync(
        Guid accountId, Guid memberId, string? employeeReference, Guid userId)
    {
        var now = DateTime.UtcNow;

        var account = await db.CorporateAccounts.ForTenant(tenant)
            .Include(c => c.EligibilityRules.Where(r => !r.IsDeleted))
            .FirstOrDefaultAsync(c => c.Id == accountId)
            ?? throw new InvalidOperationException("Corporate account not found.");

        var member = await db.Members.ForTenant(tenant).FirstOrDefaultAsync(m => m.Id == memberId)
            ?? throw new InvalidOperationException("Member not found.");

        var count = await db.CorporateMembers.ForTenant(tenant)
            .CountAsync(m => m.CorporateAccountId == accountId && m.IsActive);

        if (account.MaxMembers > 0 && count >= account.MaxMembers)
            throw new InvalidOperationException(
                $"{account.Name}'s scheme covers {account.MaxMembers} people and is full.");

        var existing = await db.CorporateMembers.ForTenant(tenant)
            .Include(m => m.Member)
            .FirstOrDefaultAsync(m => m.CorporateAccountId == accountId && m.MemberId == memberId);

        if (existing is not null)
        {
            existing.IsActive = true;
            existing.LeftSchemeOn = null;
            existing.StampUpdated(userId);
            await db.SaveChangesAsync();
            return FitnessMapper.ToDto(existing, now);
        }

        var agreement = await db.Agreements.ForTenant(tenant)
            .FirstOrDefaultAsync(a => a.MemberId == memberId && a.Status == AgreementStatus.Active);

        var price = agreement?.Price ?? account.NegotiatedRate ?? 0m;

        var (employer, employee) = account.BillingModel switch
        {
            CorporateBillingModel.EmployerPaysAll => (price, 0m),
            CorporateBillingModel.Subsidised when account.SubsidyPerMember > 0 =>
                (account.SubsidyPerMember, Math.Max(0, price - account.SubsidyPerMember)),
            CorporateBillingModel.Subsidised => (
                Math.Round(price * account.SubsidyPercent / 100m, 2),
                Math.Round(price * (100m - account.SubsidyPercent) / 100m, 2)),
            _ => (0m, price),
        };

        var revalidate = account.EligibilityRules.FirstOrDefault(r => r.IsActive)?.RevalidateEveryDays ?? 365;

        var corporateMember = new CorporateMember
        {
            CorporateAccountId = accountId,
            MemberId = memberId,
            AgreementId = agreement?.Id,
            EmployeeReference = employeeReference,
            JoinedSchemeOn = now,
            EligibilityVerifiedOn = now,
            EligibilityExpiresOn = now.AddDays(revalidate),
            EmployerContribution = employer,
            EmployeeContribution = employee,
        }.StampNew(tenant, userId);

        db.CorporateMembers.Add(corporateMember);

        member.CorporateAccountId = accountId;
        member.StampUpdated(userId);

        if (agreement is not null && account.BillingModel == CorporateBillingModel.EmployerPaysAll)
        {
            agreement.CorporateAccountId = accountId;
            agreement.StampUpdated(userId);
        }

        account.CurrentMemberCount = count + 1;
        await db.SaveChangesAsync();

        var saved = await db.CorporateMembers.ForTenant(tenant)
            .Include(m => m.Member)
            .FirstAsync(m => m.Id == corporateMember.Id);

        return FitnessMapper.ToDto(saved, now);
    }

    /// <summary>Checks an email domain, employee id or access code against the scheme's rules.</summary>
    public async Task<bool> CheckEligibilityAsync(Guid accountId, string? email, string? employeeReference, string? code)
    {
        var rules = await db.EligibilityRules.ForTenant(tenant)
            .Where(r => r.CorporateAccountId == accountId && r.IsActive)
            .ToListAsync();

        if (rules.Count == 0) return true;

        foreach (var rule in rules)
        {
            var match = rule.Proof switch
            {
                EligibilityProof.EmailDomain =>
                    email is not null && rule.MatchValue is not null
                        && email.EndsWith(rule.MatchValue.StartsWith('@') ? rule.MatchValue : "@" + rule.MatchValue,
                            StringComparison.OrdinalIgnoreCase),

                EligibilityProof.EmployeeId =>
                    !string.IsNullOrWhiteSpace(employeeReference)
                        && (rule.MatchValue is null || employeeReference.StartsWith(rule.MatchValue, StringComparison.OrdinalIgnoreCase)),

                EligibilityProof.AccessCode =>
                    code is not null && string.Equals(code, rule.MatchValue, StringComparison.OrdinalIgnoreCase),

                EligibilityProof.None => true,

                // A document or a list has to be checked by a person; the code cannot say yes.
                _ => false,
            };

            if (match) return true;
        }

        return false;
    }

    /// <summary>
    /// The consolidated employer invoice, with the per-employee breakdown they will ask for.
    ///
    /// Usage is included because that is usually why the employer bought it — and an invoice with
    /// no evidence of use is an invoice that gets queried at renewal.
    /// </summary>
    public async Task<CorporateInvoiceDto> GenerateCorporateInvoiceAsync(
        Guid accountId, DateTime periodStart, DateTime periodEnd, Guid userId)
    {
        var now = DateTime.UtcNow;

        var account = await db.CorporateAccounts.ForTenant(tenant).FirstOrDefaultAsync(c => c.Id == accountId)
            ?? throw new InvalidOperationException("Corporate account not found.");

        var members = await db.CorporateMembers.ForTenant(tenant)
            .Where(m => m.CorporateAccountId == accountId && m.IsActive)
            .Include(m => m.Member)
            .ToListAsync();

        if (members.Count == 0)
            throw new InvalidOperationException("There are no active members on this scheme to invoice for.");

        var duplicate = await db.CorporateInvoices.ForTenant(tenant)
            .AnyAsync(i => i.CorporateAccountId == accountId
                        && i.PeriodStart == periodStart.Date
                        && i.Status != InvoiceStatus.Cancelled);

        if (duplicate)
            throw new InvalidOperationException("An invoice already exists for that period.");

        var club = await db.Clubs.ForTenant(tenant).FirstOrDefaultAsync(c => c.Id == account.ClubId);

        var memberIds = members.Select(m => m.MemberId).ToList();
        var visits = await db.CheckIns.ForTenant(tenant)
            .Where(c => c.MemberId != null && memberIds.Contains(c.MemberId.Value)
                     && c.CheckedInAt >= periodStart && c.CheckedInAt <= periodEnd)
            .GroupBy(c => c.MemberId!.Value)
            .Select(g => new { MemberId = g.Key, Count = g.Count() })
            .ToListAsync();

        var planNames = await db.Agreements.ForTenant(tenant)
            .Where(a => memberIds.Contains(a.MemberId) && a.Status == AgreementStatus.Active)
            .Select(a => new { a.MemberId, PlanName = a.Plan!.Name })
            .ToListAsync();

        var subtotal = members.Sum(m => m.EmployerContribution);
        var taxPercent = club?.DefaultTaxPercent ?? 0m;
        var tax = Math.Round(subtotal * taxPercent / 100m, 2);

        var invoice = new CorporateInvoice
        {
            InvoiceNumber = await numbering.NextCorporateInvoiceNumberAsync(now),
            CorporateAccountId = accountId,
            ClubId = account.ClubId,
            PeriodStart = periodStart.Date,
            PeriodEnd = periodEnd.Date,
            IssuedOn = now,
            DueOn = now.Date.AddDays(account.PaymentTermsDays),
            Status = InvoiceStatus.Issued,
            MemberCount = members.Count,
            Subtotal = subtotal,
            TaxTotal = tax,
            Total = subtotal + tax,
            BalanceDue = subtotal + tax,
            CurrencyCode = club?.CurrencyCode ?? "USD",
        }.StampNew(tenant, userId);

        db.CorporateInvoices.Add(invoice);
        await db.SaveChangesAsync();

        var dto = new CorporateInvoiceDto
        {
            Id = invoice.Id,
            InvoiceNumber = invoice.InvoiceNumber,
            CorporateAccountId = accountId,
            CorporateAccountName = account.Name,
            ClubId = invoice.ClubId,
            PeriodStart = invoice.PeriodStart,
            PeriodEnd = invoice.PeriodEnd,
            IssuedOn = invoice.IssuedOn,
            DueOn = invoice.DueOn,
            Status = invoice.Status,
            MemberCount = invoice.MemberCount,
            Subtotal = invoice.Subtotal,
            TaxTotal = invoice.TaxTotal,
            Total = invoice.Total,
            BalanceDue = invoice.BalanceDue,
            CurrencyCode = invoice.CurrencyCode,
            Breakdown = [.. members.Select(m => new CorporateInvoiceLineDto
            {
                MemberId = m.MemberId,
                MemberName = m.Member is null ? "" : FitnessMapper.FullName(m.Member),
                EmployeeReference = m.EmployeeReference,
                PlanName = planNames.FirstOrDefault(p => p.MemberId == m.MemberId)?.PlanName,
                Amount = m.EmployerContribution,
                Visits = visits.FirstOrDefault(v => v.MemberId == m.MemberId)?.Count ?? 0,
            })],
        };

        return dto;
    }

    public async Task<PaginatedResponse<CorporateInvoiceDto>> GetCorporateInvoicesAsync(
        Guid? accountId, InvoiceStatus? status, PaginationParams pagination)
    {
        var now = DateTime.UtcNow;

        var query = db.CorporateInvoices.ForTenant(tenant)
            .Include(i => i.CorporateAccount)
            .WhereIf(accountId is not null, i => i.CorporateAccountId == accountId)
            .WhereIf(status is not null, i => i.Status == status);

        var total = await query.CountAsync();

        var page = await query
            .OrderByDescending(i => i.IssuedOn)
            .Skip((pagination.PageNumber - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .ToListAsync();

        return PaginatedResponse<CorporateInvoiceDto>.Ok(
                   [.. page.Select(i => new CorporateInvoiceDto
            {
                Id = i.Id,
                InvoiceNumber = i.InvoiceNumber,
                CorporateAccountId = i.CorporateAccountId,
                CorporateAccountName = i.CorporateAccount?.Name,
                ClubId = i.ClubId,
                PeriodStart = i.PeriodStart,
                PeriodEnd = i.PeriodEnd,
                IssuedOn = i.IssuedOn,
                DueOn = i.DueOn,
                PaidOn = i.PaidOn,
                Status = i.Status,
                DaysOverdue = i.BalanceDue > 0 && i.DueOn < now ? (int)(now.Date - i.DueOn.Date).TotalDays : 0,
                MemberCount = i.MemberCount,
                Subtotal = i.Subtotal,
                TaxTotal = i.TaxTotal,
                Total = i.Total,
                AmountPaid = i.AmountPaid,
                BalanceDue = i.BalanceDue,
                CurrencyCode = i.CurrencyCode,
                BreakdownUrl = i.BreakdownUrl,
                DocumentUrl = i.DocumentUrl,
                PurchaseOrderReference = i.PurchaseOrderReference,
            })],
                   total,
                   pagination.PageNumber,
                   pagination.PageSize);
    }

    // ── Third-party payers ───────────────────────────────────────────────────

    public async Task<List<ThirdPartyPayerDto>> GetPayersAsync(Guid? clubId, bool activeOnly)
    {
        var now = DateTime.UtcNow;

        var payers = await db.Payers.ForTenant(tenant)
            .WhereIf(clubId is not null, p => p.ClubId == clubId)
            .WhereIf(activeOnly, p => p.IsActive)
            .OrderBy(p => p.Name)
            .ToListAsync();

        var ids = payers.Select(p => p.Id).ToList();

        var authorisations = await db.Authorisations.ForTenant(tenant)
            .Where(a => ids.Contains(a.ThirdPartyPayerId) && a.IsActive && a.ValidTo >= now)
            .GroupBy(a => a.ThirdPartyPayerId)
            .Select(g => new
            {
                PayerId = g.Key,
                Count = g.Count(),
                Outstanding = g.Sum(a => a.ApprovedValue - a.InvoicedValue),
            })
            .ToListAsync();

        return [.. payers.Select(p =>
        {
            var dto = FitnessMapper.ToDto(p);
            var stats = authorisations.FirstOrDefault(a => a.PayerId == p.Id);
            dto.ActiveAuthorisations = stats?.Count ?? 0;
            dto.OutstandingValue = stats?.Outstanding ?? 0m;
            return dto;
        })];
    }

    public async Task<ThirdPartyPayerDto> SavePayerAsync(Guid? id, ThirdPartyPayerDto request, Guid userId)
    {
        ThirdPartyPayer payer;
        if (id is null)
        {
            payer = new ThirdPartyPayer { ClubId = request.ClubId }.StampNew(tenant, userId);
            db.Payers.Add(payer);
        }
        else
        {
            payer = await db.Payers.ForTenant(tenant).FirstOrDefaultAsync(p => p.Id == id)
                ?? throw new InvalidOperationException("Payer not found.");
            payer.StampUpdated(userId);
        }

        payer.Name = request.Name;
        payer.PayerType = request.PayerType;
        payer.ContactName = request.ContactName;
        payer.ContactEmail = request.ContactEmail;
        payer.ContactPhone = request.ContactPhone;
        payer.PaymentTermsDays = request.PaymentTermsDays;
        payer.AgreedRate = request.AgreedRate;
        payer.RequiresAuthorisationNumber = request.RequiresAuthorisationNumber;
        payer.IsActive = request.IsActive;

        await db.SaveChangesAsync();
        return FitnessMapper.ToDto(payer);
    }

    public async Task<PayerAuthorisationDto> SaveAuthorisationAsync(
        Guid? id, PayerAuthorisationDto request, Guid userId)
    {
        var now = DateTime.UtcNow;

        var payer = await db.Payers.ForTenant(tenant).FirstOrDefaultAsync(p => p.Id == request.ThirdPartyPayerId)
            ?? throw new InvalidOperationException("Payer not found.");

        if (payer.RequiresAuthorisationNumber && string.IsNullOrWhiteSpace(request.AuthorisationNumber))
            throw new InvalidOperationException($"{payer.Name} requires an authorisation number.");

        PayerAuthorisation authorisation;
        if (id is null)
        {
            authorisation = new PayerAuthorisation
            {
                ThirdPartyPayerId = request.ThirdPartyPayerId,
                MemberId = request.MemberId,
            }.StampNew(tenant, userId);

            db.Authorisations.Add(authorisation);
        }
        else
        {
            authorisation = await db.Authorisations.ForTenant(tenant)
                .Include(a => a.Member)
                .Include(a => a.ThirdPartyPayer)
                .FirstOrDefaultAsync(a => a.Id == id)
                ?? throw new InvalidOperationException("Authorisation not found.");
            authorisation.StampUpdated(userId);
        }

        authorisation.AuthorisationNumber = request.AuthorisationNumber;
        authorisation.ValidFrom = request.ValidFrom;
        authorisation.ValidTo = request.ValidTo;
        authorisation.ApprovedUnits = request.ApprovedUnits;
        authorisation.RatePerUnit = request.RatePerUnit > 0 ? request.RatePerUnit : payer.AgreedRate ?? 0m;
        authorisation.ApprovedValue = authorisation.ApprovedUnits * authorisation.RatePerUnit;
        authorisation.Purpose = request.Purpose;
        authorisation.ReferrerName = request.ReferrerName;
        authorisation.IsActive = request.IsActive;
        authorisation.IsExhausted = authorisation.UsedUnits >= authorisation.ApprovedUnits;

        await db.SaveChangesAsync();

        var saved = await db.Authorisations.ForTenant(tenant)
            .Include(a => a.Member)
            .Include(a => a.ThirdPartyPayer)
            .FirstAsync(a => a.Id == authorisation.Id);

        return FitnessMapper.ToDto(saved, now);
    }

    public async Task<List<PayerAuthorisationDto>> GetAuthorisationsAsync(
        Guid? payerId, Guid? memberId, bool activeOnly)
    {
        var now = DateTime.UtcNow;

        var authorisations = await db.Authorisations.ForTenant(tenant)
            .WhereIf(payerId is not null, a => a.ThirdPartyPayerId == payerId)
            .WhereIf(memberId is not null, a => a.MemberId == memberId)
            .WhereIf(activeOnly, a => a.IsActive && !a.IsExhausted && a.ValidTo >= now)
            .Include(a => a.Member)
            .Include(a => a.ThirdPartyPayer)
            .OrderByDescending(a => a.ValidFrom)
            .ToListAsync();

        return [.. authorisations.Select(a => FitnessMapper.ToDto(a, now))];
    }

    public async Task<VendingRevenueEntryDto> RecordVendingRevenueAsync(VendingRevenueEntryDto request, Guid userId)
    {
        var club = await db.Clubs.ForTenant(tenant).FirstOrDefaultAsync(c => c.Id == request.ClubId)
            ?? throw new InvalidOperationException("Club not found.");

        var entry = new VendingRevenueEntry
        {
            ClubId = request.ClubId,
            PeriodStart = request.PeriodStart.Date,
            PeriodEnd = request.PeriodEnd.Date,
            RevenueSource = request.RevenueSource,
            MachineReference = request.MachineReference,
            GrossRevenue = request.GrossRevenue,
            CommissionPaid = request.CommissionPaid,
            NetRevenue = request.GrossRevenue - request.CommissionPaid,
            CurrencyCode = club.CurrencyCode,
            TransactionCount = request.TransactionCount,
            EnteredByStaffId = userId == Guid.Empty ? null : userId,
            Note = request.Note,
        }.StampNew(tenant, userId);

        db.VendingRevenue.Add(entry);
        await db.SaveChangesAsync();

        return FitnessMapper.ToDto(entry);
    }

    // ═══ Internals ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Tells Inventory what left the shelf.
    ///
    /// Published as an event rather than written directly, so Fitness never touches the stock
    /// ledger — the same contract Point of Sale uses, and the reason a club can run either till
    /// against one set of stock figures.
    /// </summary>
    private async Task PublishStockMovementAsync(
        FitnessSale sale, List<CreateSaleLineDto> lines, FitnessClub club, Guid userId)
    {
        var stocked = lines.Where(l => l.InventoryItemId is not null).ToList();
        if (stocked.Count == 0 || club.WarehouseId is null) return;

        try
        {
            // The same event Point of Sale publishes, so Inventory has one code path for
            // "something was sold over a counter" rather than one per module.
            await events.PublishAsync(new PosTransactionCompletedEvent
            {
                PosTransactionId = sale.Id,
                PosStoreId = club.PosStoreId ?? club.Id,
                WarehouseId = club.WarehouseId.Value,
                CompanyId = tenant.CompanyId,
                BranchId = tenant.BranchId,
                BusinessUnitId = tenant.BusinessUnitId,
                CreatedByUserId = userId,
                CompletedAt = sale.SoldAt,
                Lines = [.. stocked.Select(l => new StockDeductionLine
                {
                    ProductId = l.InventoryItemId!.Value,
                    ProductCode = l.Barcode ?? string.Empty,
                    WarehouseId = club.WarehouseId,
                    Quantity = l.Quantity,
                })],
            });

            sale.StockDepleted = true;
        }
        catch (Exception)
        {
            // A stock ledger that is briefly behind is recoverable; a till that refuses to sell a
            // shaker because Inventory is busy is not. The flag stays false and a later sweep
            // reconciles it.
            sale.StockDepleted = false;
        }
    }

    private static void RecomputeExpected(CashSession session)
        => session.ExpectedCash = session.OpeningFloat + session.CashSales - session.Refunds
                                + session.PaidIn - session.PaidOut - session.Drops;

    private async Task<CashSessionDto> DecorateSessionAsync(CashSession session)
    {
        var dto = FitnessMapper.ToDto(session);

        dto.ClubName = await db.Clubs.ForTenant(tenant)
            .Where(c => c.Id == session.ClubId).Select(c => c.Name).FirstOrDefaultAsync();

        var staffIds = new[] { session.OpenedByStaffId, session.ClosedByStaffId }
            .Where(g => g is not null).Select(g => g!.Value).Distinct().ToList();

        if (staffIds.Count > 0)
        {
            var names = await db.Staff.ForTenant(tenant)
                .Where(s => staffIds.Contains(s.Id))
                .Select(s => new { s.Id, Name = s.FirstName + " " + s.LastName })
                .ToDictionaryAsync(s => s.Id, s => s.Name);

            if (session.OpenedByStaffId is not null) dto.OpenedByName = names.GetValueOrDefault(session.OpenedByStaffId.Value);
            if (session.ClosedByStaffId is not null) dto.ClosedByName = names.GetValueOrDefault(session.ClosedByStaffId.Value);

            var movementStaff = session.Movements.Where(m => m.StaffId is not null).Select(m => m.StaffId!.Value).ToList();
            if (movementStaff.Count > 0)
            {
                var moreNames = await db.Staff.ForTenant(tenant)
                    .Where(s => movementStaff.Contains(s.Id))
                    .Select(s => new { s.Id, Name = s.FirstName + " " + s.LastName })
                    .ToDictionaryAsync(s => s.Id, s => s.Name);

                foreach (var movement in dto.Movements)
                {
                    var entity = session.Movements.FirstOrDefault(m => m.Id == movement.Id);
                    if (entity?.StaffId is not null) movement.StaffName = moreNames.GetValueOrDefault(entity.StaffId.Value);
                }
            }
        }

        return dto;
    }

    private static string Describe(ChargeKind kind) => kind switch
    {
        ChargeKind.MembershipDues or ChargeKind.Instalment => "Membership dues",
        ChargeKind.JoiningFee or ChargeKind.AdminFee => "Joining and admin fees",
        ChargeKind.PersonalTraining or ChargeKind.SessionPackage => "Personal training",
        ChargeKind.ClassDropIn => "Class drop-ins",
        ChargeKind.DayPass or ChargeKind.GuestFee => "Day passes and guests",
        ChargeKind.LockerRental => "Lockers",
        ChargeKind.ResourceBooking => "Court and resource hire",
        ChargeKind.Retail => "Pro shop",
        ChargeKind.LateFee or ChargeKind.NoShowFee or ChargeKind.LateCancelFee => "Fees and penalties",
        ChargeKind.Course => "Courses",
        _ => "Other",
    };
}
