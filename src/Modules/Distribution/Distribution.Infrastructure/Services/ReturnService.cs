using Distribution.Application.DTOs;
using Distribution.Application.Services.Interfaces;
using Distribution.Domain.Entities;
using Distribution.Domain.Enums;
using Distribution.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Nexcore.SharedKernel.Api;
using DomainReturnStatus = Distribution.Domain.Enums.ReturnStatus;

namespace Distribution.Infrastructure.Services;

/// <summary>
/// Returns: authorisation, collection, receipt, inspection and disposition.
///
/// The authorisation and the receipt are separate records because in distribution they are days
/// and kilometres apart — a rep agrees a return at the counter on Tuesday, a van collects it on
/// Thursday, the warehouse inspects it on Friday. What comes back is routinely not what was
/// agreed, and the gap between the two is a number worth reporting rather than hiding.
/// </summary>
public class ReturnService(
    DistributionDbContext db,
    IDistributionTenant tenant,
    DistributionNumbering numbering) : IReturnService
{
    public async Task<ReturnDto> RequestAsync(RequestReturnDto request, Guid userId)
    {
        if (request.Lines.Count == 0)
            throw new InvalidOperationException("A return needs at least one line.");
        if (request.OutletId is null && request.PartnerId is null)
            throw new InvalidOperationException("A return must name an outlet or a partner.");

        if (!string.IsNullOrWhiteSpace(request.IdempotencyKey))
        {
            var replay = await db.Returns.ForTenant(tenant)
                .Include(r => r.Outlet).Include(r => r.Lines.Where(l => !l.IsDeleted))
                .FirstOrDefaultAsync(r => r.Note == request.IdempotencyKey);
            if (replay is not null) return replay.ToDto();
        }

        var settings = await db.Settings.ForTenant(tenant).FirstOrDefaultAsync();

        var entity = new ReturnAuthorisation
        {
            ReturnNumber = await numbering.NextReturnNumberAsync(DateTime.UtcNow),
            Kind = request.Kind,
            Status = DomainReturnStatus.Requested,
            OutletId = request.OutletId,
            PartnerId = request.PartnerId,
            VisitId = request.VisitId,
            FieldRepId = request.FieldRepId,
            RouteId = request.RouteId,
            OriginalOrderId = request.OriginalOrderId,
            OriginalInvoiceId = request.OriginalInvoiceId,
            RequestedOn = DateTime.UtcNow,
            // An authorisation that never lapses means stale goods arriving months later with
            // nobody able to say whether they were ever agreed.
            ValidUntil = request.ValidUntil ?? DateTime.UtcNow.Date.AddDays(30),
            ReasonCodeId = request.ReasonCodeId,
            ReasonNote = request.ReasonNote,
            ValuationBasis = request.ValuationBasis,
            ValuationPercent = request.ValuationPercent <= 0 ? 100 : request.ValuationPercent,
            CurrencyCode = settings?.BaseCurrencyCode ?? "USD",
            PhotoUrl = request.PhotoUrl,
            Note = request.Note ?? request.IdempotencyKey,
        }.StampNew(tenant, userId);

        if (request.ReasonCodeId.HasValue)
        {
            var reason = await db.ReasonCodes.ForCompany(tenant)
                .FirstOrDefaultAsync(r => r.Id == request.ReasonCodeId)
                ?? throw new InvalidOperationException("That reason is not recognised.");

            if (reason.RequiresNote && string.IsNullOrWhiteSpace(request.ReasonNote))
                throw new InvalidOperationException($"'{reason.Name}' needs a note.");
        }

        var order = 0;
        foreach (var line in request.Lines)
        {
            if (line.RequestedQuantity <= 0) continue;

            var unitPrice = await ResolveReturnPriceAsync(entity, line);

            entity.Lines.Add(new ReturnAuthorisationLine
            {
                ReturnId = entity.Id,
                DisplayOrder = order++,
                ItemId = line.ItemId,
                ItemName = line.ItemName,
                ItemCode = line.ItemCode,
                BatchId = line.BatchId,
                BatchNumber = line.BatchNumber,
                ExpiryDate = line.ExpiryDate,
                Uom = string.IsNullOrWhiteSpace(line.Uom) ? "PCS" : line.Uom,
                UomFactor = line.UomFactor <= 0 ? 1 : line.UomFactor,
                RequestedQuantity = line.RequestedQuantity,
                UnitPrice = unitPrice,
                UnitCost = line.UnitCost,
                LineValue = Math.Round(line.RequestedQuantity * unitPrice, 4),
                ReasonCodeId = line.ReasonCodeId ?? request.ReasonCodeId,
                PhotoUrl = line.PhotoUrl,
                Note = line.Note,
            }.StampNew(tenant, userId));
        }

        if (entity.Lines.Count == 0)
            throw new InvalidOperationException("A return needs at least one line with a quantity.");

        entity.ClaimedValue = entity.Lines.Sum(l => l.LineValue);

        db.Returns.Add(entity);
        await db.SaveChangesAsync();

        // Van sales take goods back on the spot. They land in the returns compartment, never in
        // sellable stock, so nothing dented can be resold at the next stop.
        if (request.CollectOnVanNow && request.VanUnitId.HasValue)
            await CollectOnVanAsync(entity, request.VanUnitId.Value, userId);

        if (request.VisitId.HasValue)
            await db.Visits.ForTenant(tenant).Where(v => v.Id == request.VisitId)
                .ExecuteUpdateAsync(s => s.SetProperty(v => v.ReturnValue, v => v.ReturnValue + entity.ClaimedValue));

        return (await GetAsync(entity.Id))!;
    }

    public async Task<ReturnDto?> GetAsync(Guid returnId)
    {
        var entity = await db.Returns.ForTenant(tenant)
            .Include(r => r.Outlet)
            .Include(r => r.Lines.Where(l => !l.IsDeleted))
            .FirstOrDefaultAsync(r => r.Id == returnId);

        if (entity is null) return null;

        var dto = entity.ToDto();

        if (entity.PartnerId.HasValue)
            dto.PartnerName = await db.Partners.ForTenant(tenant)
                .Where(p => p.Id == entity.PartnerId).Select(p => p.Name).FirstOrDefaultAsync();

        if (entity.FieldRepId.HasValue)
            dto.FieldRepName = await db.FieldReps.ForTenant(tenant)
                .Where(r => r.Id == entity.FieldRepId).Select(r => r.FullName).FirstOrDefaultAsync();

        var reasonIds = entity.Lines.Where(l => l.ReasonCodeId.HasValue)
            .Select(l => l.ReasonCodeId!.Value)
            .Concat(entity.ReasonCodeId.HasValue ? [entity.ReasonCodeId.Value] : [])
            .Distinct().ToList();

        if (reasonIds.Count > 0)
        {
            var reasons = await db.ReasonCodes.ForCompany(tenant)
                .Where(r => reasonIds.Contains(r.Id)).ToDictionaryAsync(r => r.Id, r => r.Name);

            if (entity.ReasonCodeId.HasValue)
                dto.ReasonCodeName = reasons.GetValueOrDefault(entity.ReasonCodeId.Value);

            foreach (var line in dto.Lines.Where(l => l.ReasonCodeId.HasValue))
                line.ReasonCodeName = reasons.GetValueOrDefault(line.ReasonCodeId!.Value);
        }

        dto.Receipts = (await db.ReturnReceipts.ForTenant(tenant)
                .Include(r => r.Lines.Where(l => !l.IsDeleted))
                .Where(r => r.ReturnId == returnId).ToListAsync())
            .Select(r => r.ToDto()).ToList();

        return dto;
    }

    public async Task<PaginatedResponse<ReturnSummaryDto>> ListAsync(
        string? search, ReturnKind? kind, DomainReturnStatus? status, Guid? outletId,
        Guid? partnerId, DateTime? from, DateTime? to, PaginationParams pagination)
    {
        var query = db.Returns.ForTenant(tenant)
            .Include(r => r.Outlet)
            .Include(r => r.Lines.Where(l => !l.IsDeleted))
            .WhereIf(kind.HasValue, r => r.Kind == kind)
            .WhereIf(status.HasValue, r => r.Status == status)
            .WhereIf(outletId.HasValue, r => r.OutletId == outletId)
            .WhereIf(partnerId.HasValue, r => r.PartnerId == partnerId)
            .WhereIf(from.HasValue, r => r.RequestedOn >= from)
            .WhereIf(to.HasValue, r => r.RequestedOn <= to);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = $"%{search.Trim()}%";
            query = query.Where(r => EF.Functions.ILike(r.ReturnNumber, term)
                                     || (r.Outlet != null && EF.Functions.ILike(r.Outlet.Name, term)));
        }

        var total = await query.CountAsync();
        var rows = await query
            .OrderByDescending(r => r.RequestedOn)
            .Skip((pagination.PageNumber - 1) * pagination.PageSize)
            .Take(pagination.PageSize).ToListAsync();

        var dtos = rows.Select(r => r.ToSummary()).ToList();

        var partnerIds = rows.Where(r => r.PartnerId.HasValue).Select(r => r.PartnerId!.Value).Distinct().ToList();
        var reasonIds = rows.Where(r => r.ReasonCodeId.HasValue).Select(r => r.ReasonCodeId!.Value).Distinct().ToList();

        var partners = await db.Partners.ForTenant(tenant)
            .Where(p => partnerIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id, p => p.Name);
        var reasons = await db.ReasonCodes.ForCompany(tenant)
            .Where(r => reasonIds.Contains(r.Id)).ToDictionaryAsync(r => r.Id, r => r.Name);

        for (var i = 0; i < rows.Count; i++)
        {
            if (rows[i].PartnerId.HasValue) dtos[i].PartnerName = partners.GetValueOrDefault(rows[i].PartnerId!.Value);
            if (rows[i].ReasonCodeId.HasValue) dtos[i].ReasonCodeName = reasons.GetValueOrDefault(rows[i].ReasonCodeId!.Value);
        }

        return PaginatedResponse<ReturnSummaryDto>.Ok(dtos, total, pagination.PageNumber, pagination.PageSize);
    }

    public async Task<ReturnDto> DecideAsync(Guid returnId, DecideReturnDto request, Guid userId)
    {
        var entity = await db.Returns.ForTenant(tenant)
            .Include(r => r.Lines.Where(l => !l.IsDeleted))
            .FirstOrDefaultAsync(r => r.Id == returnId)
            ?? throw new InvalidOperationException("That return no longer exists.");

        if (entity.Status is not (DomainReturnStatus.Requested or DomainReturnStatus.Collected))
            throw new InvalidOperationException("This return has already been decided.");

        if (!request.IsApproved)
        {
            if (string.IsNullOrWhiteSpace(request.RejectionReason))
                throw new InvalidOperationException("Rejecting a return needs a reason.");

            entity.Status = DomainReturnStatus.Rejected;
            entity.RejectionReason = request.RejectionReason;
            entity.StampUpdated(userId);
            await db.SaveChangesAsync();
            return (await GetAsync(returnId))!;
        }

        foreach (var line in entity.Lines)
        {
            var decision = request.Lines.FirstOrDefault(l => l.LineId == line.Id);
            var approved = decision?.ApprovedQuantity ?? line.RequestedQuantity;

            if (approved > line.RequestedQuantity)
                throw new InvalidOperationException(
                    $"{line.ItemName}: cannot approve more than the {line.RequestedQuantity:N0} requested.");

            line.ApprovedQuantity = approved;
            line.LineValue = Math.Round(approved * line.UnitPrice, 4);
            if (!string.IsNullOrWhiteSpace(decision?.Note)) line.Note = decision.Note;
            line.StampUpdated(userId);
        }

        entity.ApprovedValue = entity.Lines.Sum(l => l.LineValue);
        entity.Status = DomainReturnStatus.Approved;
        entity.ApprovedAt = DateTime.UtcNow;
        entity.ApprovedByUserId = userId;
        entity.StampUpdated(userId);

        await db.SaveChangesAsync();
        return (await GetAsync(returnId))!;
    }

    public async Task<ReturnDto> CancelAsync(Guid returnId, string reason, Guid userId)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new InvalidOperationException("Cancelling a return needs a reason.");

        var entity = await db.Returns.ForTenant(tenant).FirstOrDefaultAsync(r => r.Id == returnId)
            ?? throw new InvalidOperationException("That return no longer exists.");

        if (entity.Status >= DomainReturnStatus.Received)
            throw new InvalidOperationException("The goods have already been received back.");

        entity.Status = DomainReturnStatus.Cancelled;
        entity.RejectionReason = reason;
        entity.StampUpdated(userId);

        await db.SaveChangesAsync();
        return (await GetAsync(returnId))!;
    }

    public async Task<ReturnReceiptDto> ReceiveAsync(ReceiveReturnDto request, Guid userId)
    {
        ReturnAuthorisation? authorisation = null;

        if (request.ReturnId.HasValue)
        {
            authorisation = await db.Returns.ForTenant(tenant)
                .Include(r => r.Lines.Where(l => !l.IsDeleted))
                .FirstOrDefaultAsync(r => r.Id == request.ReturnId)
                ?? throw new InvalidOperationException("That return no longer exists.");

            if (authorisation.Status is DomainReturnStatus.Rejected or DomainReturnStatus.Cancelled)
                throw new InvalidOperationException("This return was not approved.");

            if (authorisation.ValidUntil is not null
                && authorisation.ValidUntil.Value.Date < DateTime.UtcNow.Date
                && authorisation.Status == DomainReturnStatus.Requested)
                throw new InvalidOperationException(
                    $"This authorisation lapsed on {authorisation.ValidUntil:d}. Raise a new one.");
        }

        var receipt = new ReturnReceipt
        {
            ReceiptNumber = await numbering.NextReturnReceiptNumberAsync(DateTime.UtcNow),
            ReturnId = request.ReturnId,
            WarehouseId = request.WarehouseId,
            VanUnitId = request.VanUnitId,
            TripId = request.TripId,
            ReceivedAt = request.ReceivedAt ?? DateTime.UtcNow,
            ReceivedByUserId = userId,
            Note = request.Note,
        }.StampNew(tenant, userId);

        foreach (var line in request.Lines)
        {
            var source = authorisation?.Lines.FirstOrDefault(l => l.Id == line.ReturnLineId);

            var accepted = line.AcceptedQuantity > 0 ? line.AcceptedQuantity : line.ReceivedQuantity;
            var rejected = Math.Max(0, line.ReceivedQuantity - accepted);

            receipt.Lines.Add(new ReturnReceiptLine
            {
                ReceiptId = receipt.Id,
                ReturnLineId = line.ReturnLineId,
                ItemId = line.ItemId,
                ItemName = line.ItemName,
                BatchId = line.BatchId,
                BatchNumber = line.BatchNumber,
                ExpiryDate = line.ExpiryDate,
                Uom = string.IsNullOrWhiteSpace(line.Uom) ? "PCS" : line.Uom,
                ExpectedQuantity = source?.ApprovedQuantity ?? line.ExpectedQuantity,
                ReceivedQuantity = line.ReceivedQuantity,
                AcceptedQuantity = accepted,
                RejectedQuantity = rejected,
                UnitPrice = source?.UnitPrice ?? line.UnitPrice,
                UnitCost = line.UnitCost,
                LineValue = Math.Round(accepted * (source?.UnitPrice ?? line.UnitPrice), 4),
                // Expired goods can never go back to sellable stock, whatever anybody types.
                Disposition = line.ExpiryDate is not null && line.ExpiryDate.Value.Date < DateTime.UtcNow.Date
                    ? ReturnDispositionKind.Scrap
                    : line.Disposition,
                PutawayBinId = line.PutawayBinId,
                ReasonCodeId = line.ReasonCodeId,
                Note = line.Note,
            }.StampNew(tenant, userId));

            if (source is not null)
            {
                source.ReceivedQuantity = line.ReceivedQuantity;
                source.StampUpdated(userId);
            }
        }

        receipt.TotalValue = receipt.Lines.Sum(l => l.LineValue);
        receipt.RestockedValue = receipt.Lines
            .Where(l => l.Disposition == ReturnDispositionKind.Restock).Sum(l => l.LineValue);
        receipt.ScrappedValue = receipt.Lines
            .Where(l => l.Disposition == ReturnDispositionKind.Scrap).Sum(l => l.LineValue);
        receipt.InspectedAt = DateTime.UtcNow;
        receipt.InspectedByUserId = userId;

        db.ReturnReceipts.Add(receipt);

        if (authorisation is not null)
        {
            authorisation.Status = DomainReturnStatus.Inspected;
            authorisation.StampUpdated(userId);
        }

        await db.SaveChangesAsync();
        return receipt.ToDto();
    }

    public async Task<ReturnReceiptDto> DispositionAsync(DispositionReturnDto request, Guid userId)
    {
        var line = await db.ReturnReceiptLines.ForTenant(tenant)
            .Include(l => l.Receipt)
            .FirstOrDefaultAsync(l => l.Id == request.ReceiptLineId)
            ?? throw new InvalidOperationException("That receipt line no longer exists.");

        if (request.Quantity <= 0)
            throw new InvalidOperationException("A disposition needs a quantity.");

        var alreadyDisposed = await db.ReturnDispositions.ForTenant(tenant)
            .Where(d => d.ReceiptLineId == request.ReceiptLineId)
            .SumAsync(d => (decimal?)d.Quantity) ?? 0;

        if (alreadyDisposed + request.Quantity > line.AcceptedQuantity)
            throw new InvalidOperationException(
                $"Only {line.AcceptedQuantity - alreadyDisposed:N0} remains undisposed on this line.");

        if (request.Kind == ReturnDispositionKind.Restock
            && line.ExpiryDate is not null && line.ExpiryDate.Value.Date < DateTime.UtcNow.Date)
            throw new InvalidOperationException("Expired goods cannot be restocked.");

        db.ReturnDispositions.Add(new ReturnDisposition
        {
            ReceiptLineId = request.ReceiptLineId,
            Kind = request.Kind,
            Quantity = request.Quantity,
            Value = Math.Round(request.Quantity * line.UnitPrice, 4),
            DecidedAt = DateTime.UtcNow,
            DecidedByUserId = userId,
            TargetBinId = request.TargetBinId,
            TargetSupplierId = request.TargetSupplierId,
            LiquidationSchemeId = request.LiquidationSchemeId,
            Note = request.Note,
        }.StampNew(tenant, userId));

        line.Disposition = request.Kind;
        line.StampUpdated(userId);

        await db.SaveChangesAsync();

        var receipt = await db.ReturnReceipts.ForTenant(tenant)
            .Include(r => r.Lines.Where(l => !l.IsDeleted))
            .FirstOrDefaultAsync(r => r.Id == line.ReceiptId);

        return receipt!.ToDto();
    }

    public async Task<ReturnReceiptDto> RecordDestructionAsync(
        Guid receiptId, string certificateNumber, string? certificateUrl, DateTime destroyedOn, Guid userId)
    {
        if (string.IsNullOrWhiteSpace(certificateNumber))
            throw new InvalidOperationException("A destruction record needs a certificate number.");

        var receipt = await db.ReturnReceipts.ForTenant(tenant)
            .Include(r => r.Lines.Where(l => !l.IsDeleted))
            .FirstOrDefaultAsync(r => r.Id == receiptId)
            ?? throw new InvalidOperationException("That receipt no longer exists.");

        var scrapped = receipt.Lines.Count(l => l.Disposition == ReturnDispositionKind.Scrap);
        if (scrapped == 0)
            throw new InvalidOperationException("Nothing on this receipt is marked for destruction.");

        receipt.DestructionCertificateNumber = certificateNumber;
        receipt.DestructionCertificateUrl = certificateUrl;
        receipt.DestroyedOn = destroyedOn == default ? DateTime.UtcNow : destroyedOn;
        receipt.StampUpdated(userId);

        await db.SaveChangesAsync();
        return receipt.ToDto();
    }

    public async Task<ReturnDto> CreditAsync(Guid returnId, bool raiseClaim, Guid userId)
    {
        var entity = await db.Returns.ForTenant(tenant)
            .Include(r => r.Lines.Where(l => !l.IsDeleted))
            .FirstOrDefaultAsync(r => r.Id == returnId)
            ?? throw new InvalidOperationException("That return no longer exists.");

        if (entity.Status is not (DomainReturnStatus.Inspected or DomainReturnStatus.Received
            or DomainReturnStatus.Approved))
            throw new InvalidOperationException("This return is not ready to be credited.");

        // Credit is on what came back and was accepted, not on what was asked for. Valuation
        // policy is applied here, once, rather than being argued about per line.
        var creditable = entity.Lines.Sum(l =>
            (l.ReceivedQuantity > 0 ? l.ReceivedQuantity : l.ApprovedQuantity) * l.UnitPrice);

        entity.CreditedValue = entity.ValuationBasis == ReturnValuationBasis.PolicyPercentage
            ? Math.Round(creditable * entity.ValuationPercent / 100m, 4)
            : creditable;

        entity.Status = DomainReturnStatus.Credited;
        entity.StampUpdated(userId);

        // Reduce the original order's balance so the outlet's ledger reflects the credit.
        if (entity.OriginalOrderId.HasValue)
        {
            var order = await db.Orders.ForTenant(tenant)
                .Include(o => o.Lines.Where(l => !l.IsDeleted))
                .FirstOrDefaultAsync(o => o.Id == entity.OriginalOrderId);

            if (order is not null)
            {
                foreach (var line in entity.Lines)
                {
                    var orderLine = order.Lines.FirstOrDefault(l => l.ItemId == line.ItemId);
                    if (orderLine is null) continue;
                    orderLine.ReturnedQuantity += line.ReceivedQuantity > 0
                        ? line.ReceivedQuantity : line.ApprovedQuantity;
                    orderLine.StampUpdated(userId);
                }

                order.TotalAmount = Math.Max(0, order.TotalAmount - entity.CreditedValue);
                order.StampUpdated(userId);
            }
        }

        // Damage and expiry are recoverable from the principal; a market return usually is not.
        if (raiseClaim && entity.Kind is ReturnKind.Damaged or ReturnKind.Expired
            or ReturnKind.QualityComplaint or ReturnKind.RecallReturn)
        {
            var claim = new ChannelClaim
            {
                ClaimNumber = await numbering.NextClaimNumberAsync(DateTime.UtcNow),
                Kind = entity.Kind == ReturnKind.Expired ? ClaimKind.Expiry : ClaimKind.Damage,
                Status = ClaimStatus.Submitted,
                PartnerId = entity.PartnerId,
                OutletId = entity.OutletId,
                ReturnId = entity.Id,
                PeriodStart = entity.RequestedOn.Date,
                PeriodEnd = entity.RequestedOn.Date,
                SubmittedOn = DateTime.UtcNow,
                CurrencyCode = entity.CurrencyCode,
                ClaimedAmount = entity.CreditedValue,
                ComputedAmount = entity.CreditedValue,
                IsSystemGenerated = true,
                Note = $"Raised from return {entity.ReturnNumber}",
            }.StampNew(tenant, userId);

            var order = 0;
            foreach (var line in entity.Lines)
                claim.Lines.Add(new ChannelClaimLine
                {
                    ClaimId = claim.Id,
                    DisplayOrder = order++,
                    ItemId = line.ItemId,
                    ItemName = line.ItemName,
                    BatchId = line.BatchId,
                    BatchNumber = line.BatchNumber,
                    Uom = line.Uom,
                    Quantity = line.ReceivedQuantity > 0 ? line.ReceivedQuantity : line.ApprovedQuantity,
                    UnitRate = line.UnitPrice,
                    ClaimedAmount = line.LineValue,
                    ComputedAmount = line.LineValue,
                }.StampNew(tenant, userId));

            claim.StatusEvents.Add(new ClaimStatusEvent
            {
                ClaimId = claim.Id,
                FromStatus = ClaimStatus.Draft,
                ToStatus = ClaimStatus.Submitted,
                OccurredAt = DateTime.UtcNow,
                ActorName = "System",
                Note = $"Generated from return {entity.ReturnNumber}",
            }.StampNew(tenant, userId));

            db.Claims.Add(claim);
            entity.LinkedClaimId = claim.Id;
        }

        await db.SaveChangesAsync();
        return (await GetAsync(returnId))!;
    }

    // ═══ Internals ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Values a return line. The basis matters commercially: crediting expired goods at today's
    /// price when they were bought at last year's is how a distributor turns expiry into profit.
    /// </summary>
    private async Task<decimal> ResolveReturnPriceAsync(ReturnAuthorisation entity, ReturnLineDto line)
    {
        if (line.UnitPrice > 0 && entity.ValuationBasis != ReturnValuationBasis.OriginalInvoicePrice)
            return line.UnitPrice;

        if (entity.ValuationBasis == ReturnValuationBasis.OriginalInvoicePrice && entity.OriginalOrderId.HasValue)
        {
            var original = await db.OrderLines.ForTenant(tenant)
                .Where(l => l.OrderId == entity.OriginalOrderId && l.ItemId == line.ItemId)
                .Select(l => (decimal?)l.UnitPrice)
                .FirstOrDefaultAsync();

            if (original is > 0) return original.Value;
        }

        return line.UnitPrice;
    }

    private async Task CollectOnVanAsync(ReturnAuthorisation entity, Guid vanUnitId, Guid userId)
    {
        var compartment = entity.Kind switch
        {
            ReturnKind.SaleableMarketReturn => VanCompartment.SaleableReturn,
            ReturnKind.Expired or ReturnKind.NearExpiryBuyback => VanCompartment.Expired,
            _ => VanCompartment.Damaged,
        };

        foreach (var line in entity.Lines)
        {
            var quantity = line.RequestedQuantity * line.UomFactor;

            var balance = await db.VanStockBalances.ForTenant(tenant)
                .FirstOrDefaultAsync(b => b.VanUnitId == vanUnitId && b.ItemId == line.ItemId
                                          && b.BatchId == line.BatchId && b.Compartment == compartment);

            if (balance is null)
            {
                balance = new VanStockBalance
                {
                    VanUnitId = vanUnitId,
                    ItemId = line.ItemId,
                    ItemName = line.ItemName,
                    BatchId = line.BatchId,
                    BatchNumber = line.BatchNumber,
                    ExpiryDate = line.ExpiryDate,
                    Compartment = compartment,
                    Uom = line.Uom,
                    UnitCost = line.UnitCost,
                    UnitPrice = line.UnitPrice,
                }.StampNew(tenant, userId);
                db.VanStockBalances.Add(balance);
            }

            balance.Quantity += quantity;
            balance.LastMovementAt = DateTime.UtcNow;

            db.VanStockMovements.Add(new VanStockMovement
            {
                VanUnitId = vanUnitId,
                FieldDayId = null,
                VisitId = entity.VisitId,
                Kind = VanMovementKind.CustomerReturn,
                OccurredAt = DateTime.UtcNow,
                ItemId = line.ItemId,
                ItemName = line.ItemName,
                BatchId = line.BatchId,
                BatchNumber = line.BatchNumber,
                ExpiryDate = line.ExpiryDate,
                Compartment = compartment,
                Uom = line.Uom,
                Quantity = quantity,
                UnitCost = line.UnitCost,
                UnitPrice = line.UnitPrice,
                Value = quantity * line.UnitPrice,
                BalanceAfter = balance.Quantity,
                ReferenceId = entity.Id,
                ReferenceType = "ReturnAuthorisation",
                ReferenceNumber = entity.ReturnNumber,
                FieldRepId = entity.FieldRepId,
                Reason = entity.ReasonNote,
            }.StampNew(tenant, userId));
        }

        entity.Status = DomainReturnStatus.Collected;
        entity.CollectionVanUnitId = vanUnitId;
        entity.CollectedAt = DateTime.UtcNow;
        entity.StampUpdated(userId);

        await db.SaveChangesAsync();
    }
}
