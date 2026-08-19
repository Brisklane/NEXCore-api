using System.Globalization;
using System.Text.Json;
using Distribution.Application.DTOs;
using Distribution.Application.Services.Interfaces;
using Distribution.Domain.Entities;
using Distribution.Domain.Enums;
using Distribution.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Nexcore.SharedKernel.Api;

namespace Distribution.Infrastructure.Services;

/// <summary>
/// The secondary layer — what makes this a DMS rather than a sales module.
///
/// Three capture modes exist because distributors sit at three levels of maturity, and forcing
/// all of them onto one path is how DMS rollouts stall in month two. The mapping profile is the
/// piece that makes the upload path survive contact with reality: every distributor names things
/// their own way and none of them will change, so resolving an exception once teaches the profile
/// and the same code maps itself next month.
///
/// The reconciliation is the payoff: opening + primary − secondary − returns = closing. When it
/// does not balance, the gap is either unrecorded sales, unrecorded stock, or leakage — and all
/// three are urgent, which is why an unexplained variance is surfaced as an exception rather than
/// averaged into a report.
/// </summary>
public class SecondarySalesService(
    DistributionDbContext db,
    IDistributionTenant tenant,
    DistributionNumbering numbering) : ISecondarySalesService
{
    public async Task<PaginatedResponse<SecondarySaleDto>> ListAsync(
        Guid? partnerId, Guid? outletId, DateTime? from, DateTime? to, bool? unmappedOnly,
        PaginationParams pagination)
    {
        var query = db.SecondarySales.ForTenant(tenant)
            .Include(s => s.Partner).Include(s => s.Outlet)
            .Include(s => s.Lines.Where(l => !l.IsDeleted))
            .WhereIf(partnerId.HasValue, s => s.PartnerId == partnerId)
            .WhereIf(outletId.HasValue, s => s.OutletId == outletId)
            .WhereIf(from.HasValue, s => s.SaleDate >= from)
            .WhereIf(to.HasValue, s => s.SaleDate <= to)
            .WhereIf(unmappedOnly == true, s => !s.IsMapped);

        var total = await query.CountAsync();
        var rows = await query
            .OrderByDescending(s => s.SaleDate)
            .Skip((pagination.PageNumber - 1) * pagination.PageSize)
            .Take(pagination.PageSize).ToListAsync();

        return PaginatedResponse<SecondarySaleDto>.Ok(
            rows.Select(r => r.ToDto()), total, pagination.PageNumber, pagination.PageSize);
    }

    public async Task<SecondarySaleDto?> GetAsync(Guid id)
    {
        var entity = await db.SecondarySales.ForTenant(tenant)
            .Include(s => s.Partner).Include(s => s.Outlet)
            .Include(s => s.Lines.Where(l => !l.IsDeleted))
            .FirstOrDefaultAsync(s => s.Id == id);

        return entity?.ToDto();
    }

    public async Task<List<SecondarySaleDto>> DeclareAsync(DeclareSecondarySalesDto request, Guid userId)
    {
        if (request.Lines.Count == 0)
            throw new InvalidOperationException("A declaration needs at least one line.");

        var partner = await db.Partners.ForTenant(tenant).FirstOrDefaultAsync(p => p.Id == request.PartnerId)
            ?? throw new InvalidOperationException("That partner no longer exists.");

        var settings = await db.Settings.ForTenant(tenant).FirstOrDefaultAsync();
        var currency = request.CurrencyCode ?? partner.CurrencyCode;

        // Declared data arrives as a flat list; grouping by outlet and date gives it the shape of
        // the transactional path so downstream reporting does not need two code paths.
        var results = new List<SecondarySale>();

        foreach (var group in request.Lines.GroupBy(l => new
        {
            l.OutletId,
            OutletName = l.OutletNameRaw,
            Date = (l.SaleDate ?? request.PeriodEnd).Date,
        }))
        {
            var sale = new SecondarySale
            {
                DocumentNumber = $"DEC-{group.Key.Date:yyMMdd}-{results.Count + 1:D4}",
                PartnerId = request.PartnerId,
                OutletId = group.Key.OutletId,
                OutletNameRaw = group.Key.OutletName,
                CaptureMode = SecondaryCaptureMode.Declared,
                SaleDate = group.Key.Date,
                TerritoryId = partner.TerritoryId,
                CurrencyCode = currency,
                IsMapped = group.Key.OutletId is not null,
            }.StampNew(tenant, userId);

            foreach (var line in group)
            {
                var resolved = await ResolveItemAsync(line.ItemId, line.ItemCodeRaw, request.PartnerId);

                sale.Lines.Add(new SecondarySaleLine
                {
                    SecondarySaleId = sale.Id,
                    ItemId = resolved?.ItemId,
                    ItemName = resolved?.ItemName,
                    ItemCodeRaw = line.ItemCodeRaw,
                    Uom = string.IsNullOrWhiteSpace(line.Uom) ? "PCS" : line.Uom,
                    UomFactor = 1,
                    Quantity = line.Quantity,
                    BaseQuantity = line.Quantity,
                    UnitPrice = line.Quantity == 0 ? 0 : Math.Round(line.Value / line.Quantity, 4),
                    LineTotal = line.Value,
                    IsMapped = resolved is not null,
                    MappingNote = resolved is null ? $"No item matched '{line.ItemCodeRaw}'" : null,
                }.StampNew(tenant, userId));
            }

            Total(sale);
            sale.IsMapped = sale.OutletId is not null && sale.Lines.All(l => l.IsMapped);
            sale.IsPosted = sale.IsMapped;
            sale.PostedAt = sale.IsMapped ? DateTime.UtcNow : null;

            db.SecondarySales.Add(sale);
            results.Add(sale);
        }

        await db.SaveChangesAsync();

        return results.Select(r => r.ToDto()).ToList();
    }

    public async Task<SecondaryUploadDto> UploadAsync(
        Guid partnerId, DateTime periodStart, DateTime periodEnd, string fileName,
        Stream content, Guid? mappingProfileId, Guid userId)
    {
        var partner = await db.Partners.ForTenant(tenant).FirstOrDefaultAsync(p => p.Id == partnerId)
            ?? throw new InvalidOperationException("That partner no longer exists.");

        var profile = mappingProfileId.HasValue
            ? await db.MappingProfiles.ForTenant(tenant).FirstOrDefaultAsync(m => m.Id == mappingProfileId)
            : await db.MappingProfiles.ForTenant(tenant)
                .Where(m => m.PartnerId == partnerId).OrderByDescending(m => m.LastUsedAt).FirstOrDefaultAsync();

        if (profile is null)
            throw new InvalidOperationException(
                $"No mapping profile exists for {partner.Name}. Set one up before uploading a file.");

        var settings = await db.Settings.ForTenant(tenant).FirstOrDefaultAsync();

        var dueDay = settings?.SecondaryUploadDueDayOfMonth ?? 5;
        var dueOn = new DateTime(periodEnd.Year, periodEnd.Month, 1, 0, 0, 0, DateTimeKind.Utc)
            .AddMonths(1).AddDays(dueDay - 1);

        var batch = new SecondaryUploadBatch
        {
            BatchNumber = await numbering.NextUploadNumberAsync(DateTime.UtcNow),
            PartnerId = partnerId,
            PeriodStart = periodStart,
            PeriodEnd = periodEnd,
            SubmittedAt = DateTime.UtcNow,
            DueOn = dueOn,
            LatenessDays = Math.Max(0, (int)(DateTime.UtcNow.Date - dueOn.Date).TotalDays),
            Status = UploadBatchStatus.Validating,
            FileName = fileName,
            MappingProfileId = profile.Id,
        }.StampNew(tenant, userId);

        db.SecondaryUploads.Add(batch);
        await db.SaveChangesAsync();

        var rows = ParseDelimited(content, profile);
        batch.TotalRows = rows.Count;

        if (rows.Count == 0)
        {
            batch.Status = UploadBatchStatus.Rejected;
            batch.RejectionReason = "The file had no readable data rows.";
            await db.SaveChangesAsync();
            return batch.ToDto();
        }

        var itemMap = Deserialise(profile.ItemCodeMap);
        var outletMap = Deserialise(profile.OutletCodeMap);

        var sales = new List<SecondarySale>();
        var index = 0;

        foreach (var group in rows.GroupBy(r => new { r.OutletRaw, r.SaleDate }))
        {
            var outletId = await ResolveOutletAsync(group.Key.OutletRaw, outletMap, partnerId);

            var sale = new SecondarySale
            {
                DocumentNumber = $"{batch.BatchNumber}-{++index:D4}",
                PartnerId = partnerId,
                OutletId = outletId,
                OutletNameRaw = group.Key.OutletRaw,
                CaptureMode = SecondaryCaptureMode.Uploaded,
                UploadBatchId = batch.Id,
                SaleDate = group.Key.SaleDate,
                TerritoryId = partner.TerritoryId,
                CurrencyCode = partner.CurrencyCode,
            }.StampNew(tenant, userId);

            foreach (var row in group)
            {
                var resolved = await ResolveItemAsync(null, row.ItemRaw, partnerId, itemMap);

                sale.Lines.Add(new SecondarySaleLine
                {
                    SecondarySaleId = sale.Id,
                    ItemId = resolved?.ItemId,
                    ItemName = resolved?.ItemName,
                    ItemCodeRaw = row.ItemRaw,
                    BatchNumber = row.BatchNumber,
                    Uom = string.IsNullOrWhiteSpace(row.Uom) ? "PCS" : row.Uom,
                    UomFactor = 1,
                    Quantity = row.Quantity,
                    BaseQuantity = row.Quantity,
                    UnitPrice = row.Quantity == 0 ? 0 : Math.Round(row.Value / row.Quantity, 4),
                    LineTotal = row.Value,
                    IsMapped = resolved is not null,
                    MappingNote = resolved is null ? $"No item matched '{row.ItemRaw}'" : null,
                }.StampNew(tenant, userId));
            }

            Total(sale);
            sale.IsMapped = outletId is not null && sale.Lines.All(l => l.IsMapped);
            if (!sale.IsMapped)
                sale.MappingNote = outletId is null
                    ? $"No outlet matched '{group.Key.OutletRaw}'"
                    : "One or more items did not map";

            db.SecondarySales.Add(sale);
            sales.Add(sale);
        }

        batch.MappedRows = sales.SelectMany(s => s.Lines).Count(l => l.IsMapped);
        batch.UnmappedRows = sales.SelectMany(s => s.Lines).Count(l => !l.IsMapped);
        batch.TotalValue = sales.Sum(s => s.TotalAmount);
        batch.MappedValue = sales.Where(s => s.IsMapped).Sum(s => s.TotalAmount);
        batch.MappingAccuracyPercent = DistributionMapper.Percent(batch.MappedRows, batch.TotalRows);
        batch.Status = batch.UnmappedRows == 0 ? UploadBatchStatus.Mapped : UploadBatchStatus.PartiallyMapped;

        var minimum = settings?.MinimumMappingAccuracyPercent ?? 90;
        batch.ValidationSummary = batch.MappingAccuracyPercent < minimum
            ? $"Mapping accuracy {batch.MappingAccuracyPercent:N1}% is below the {minimum:N0}% threshold. Resolve the exceptions before posting."
            : $"{batch.MappedRows} of {batch.TotalRows} rows mapped.";

        profile.LastUsedAt = DateTime.UtcNow;
        profile.StampUpdated(userId);

        await db.SaveChangesAsync();
        return batch.ToDto();
    }

    public async Task<SecondaryUploadDto?> GetUploadAsync(Guid uploadId)
    {
        var entity = await db.SecondaryUploads.ForTenant(tenant)
            .Include(u => u.Partner)
            .FirstOrDefaultAsync(u => u.Id == uploadId);

        if (entity is null) return null;

        var dto = entity.ToDto();

        if (entity.MappingProfileId.HasValue)
            dto.MappingProfileName = await db.MappingProfiles.ForTenant(tenant)
                .Where(m => m.Id == entity.MappingProfileId).Select(m => m.Name).FirstOrDefaultAsync();

        return dto;
    }

    public async Task<PaginatedResponse<SecondaryUploadDto>> ListUploadsAsync(
        Guid? partnerId, UploadBatchStatus? status, DateTime? from, DateTime? to, PaginationParams pagination)
    {
        var query = db.SecondaryUploads.ForTenant(tenant)
            .Include(u => u.Partner)
            .WhereIf(partnerId.HasValue, u => u.PartnerId == partnerId)
            .WhereIf(status.HasValue, u => u.Status == status)
            .WhereIf(from.HasValue, u => u.PeriodStart >= from)
            .WhereIf(to.HasValue, u => u.PeriodEnd <= to);

        var total = await query.CountAsync();
        var rows = await query
            .OrderByDescending(u => u.SubmittedAt)
            .Skip((pagination.PageNumber - 1) * pagination.PageSize)
            .Take(pagination.PageSize).ToListAsync();

        return PaginatedResponse<SecondaryUploadDto>.Ok(
            rows.Select(r => r.ToDto()), total, pagination.PageNumber, pagination.PageSize);
    }

    public async Task<SecondaryUploadDto> PostUploadAsync(Guid uploadId, Guid userId)
    {
        var batch = await db.SecondaryUploads.ForTenant(tenant)
            .Include(u => u.Partner)
            .FirstOrDefaultAsync(u => u.Id == uploadId)
            ?? throw new InvalidOperationException("That upload no longer exists.");

        if (batch.Status == UploadBatchStatus.Posted)
            throw new InvalidOperationException("This batch has already been posted.");
        if (batch.Status == UploadBatchStatus.Rejected)
            throw new InvalidOperationException("This batch was rejected.");

        var settings = await db.Settings.ForTenant(tenant).FirstOrDefaultAsync();
        var minimum = settings?.MinimumMappingAccuracyPercent ?? 90;

        // Posting a batch that mostly did not map would pour noise into the one dataset the whole
        // module's insight rests on.
        if (batch.MappingAccuracyPercent < minimum)
            throw new InvalidOperationException(
                $"Only {batch.MappingAccuracyPercent:N1}% of rows mapped. Resolve the exceptions before posting.");

        await db.SecondarySales.ForTenant(tenant)
            .Where(s => s.UploadBatchId == uploadId && s.IsMapped)
            .ExecuteUpdateAsync(s => s
                .SetProperty(x => x.IsPosted, true)
                .SetProperty(x => x.PostedAt, DateTime.UtcNow));

        batch.Status = UploadBatchStatus.Posted;
        batch.PostedAt = DateTime.UtcNow;
        batch.PostedByUserId = userId;
        batch.StampUpdated(userId);

        await db.SaveChangesAsync();
        return batch.ToDto();
    }

    public async Task<SecondaryUploadDto> RejectUploadAsync(Guid uploadId, string reason, Guid userId)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new InvalidOperationException("Rejecting an upload needs a reason.");

        var batch = await db.SecondaryUploads.ForTenant(tenant)
            .Include(u => u.Partner)
            .FirstOrDefaultAsync(u => u.Id == uploadId)
            ?? throw new InvalidOperationException("That upload no longer exists.");

        if (batch.Status == UploadBatchStatus.Posted)
            throw new InvalidOperationException("A posted batch cannot be rejected.");

        batch.Status = UploadBatchStatus.Rejected;
        batch.RejectionReason = reason;
        batch.StampUpdated(userId);

        await db.SecondarySales.ForTenant(tenant)
            .Where(s => s.UploadBatchId == uploadId)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.IsDeleted, true));

        await db.SaveChangesAsync();
        return batch.ToDto();
    }

    public async Task<List<MappingExceptionDto>> GetMappingExceptionsAsync(Guid? uploadId, Guid? partnerId)
    {
        var lines = await db.SecondarySaleLines.ForTenant(tenant)
            .Include(l => l.SecondarySale)
            .Where(l => !l.IsMapped)
            .Where(l => l.SecondarySale != null
                        && (uploadId == null || l.SecondarySale.UploadBatchId == uploadId)
                        && (partnerId == null || l.SecondarySale.PartnerId == partnerId))
            .Take(500)
            .ToListAsync();

        var results = new List<MappingExceptionDto>();

        foreach (var line in lines)
        {
            var exception = new MappingExceptionDto
            {
                LineId = line.Id,
                SecondarySaleId = line.SecondarySaleId,
                RawItemCode = line.ItemCodeRaw,
                RawOutletName = line.SecondarySale?.OutletNameRaw,
                Quantity = line.Quantity,
                Value = line.LineTotal,
                SaleDate = line.SecondarySale?.SaleDate ?? DateTime.UtcNow,
                Reason = line.MappingNote,
            };

            if (!string.IsNullOrWhiteSpace(line.ItemCodeRaw))
                exception.Suggestions.AddRange(await SuggestItemsAsync(line.ItemCodeRaw));

            if (line.SecondarySale?.OutletId is null && !string.IsNullOrWhiteSpace(line.SecondarySale?.OutletNameRaw))
                exception.Suggestions.AddRange(
                    await SuggestOutletsAsync(line.SecondarySale.OutletNameRaw, line.SecondarySale.PartnerId));

            results.Add(exception);
        }

        return results;
    }

    public async Task<MappingExceptionDto?> ResolveMappingAsync(ResolveMappingDto request, Guid userId)
    {
        var line = await db.SecondarySaleLines.ForTenant(tenant)
            .Include(l => l.SecondarySale)
            .FirstOrDefaultAsync(l => l.Id == request.LineId)
            ?? throw new InvalidOperationException("That row no longer exists.");

        if (request.Reject)
        {
            line.StampDeleted(userId);
            await db.SaveChangesAsync();
            return null;
        }

        if (request.ItemId.HasValue)
        {
            var name = await db.PriceListLines.ForTenant(tenant)
                .Where(p => p.ItemId == request.ItemId).Select(p => p.ItemName).FirstOrDefaultAsync();

            line.ItemId = request.ItemId;
            line.ItemName = name;
            line.IsMapped = true;
            line.MappingNote = null;
            line.StampUpdated(userId);
        }

        if (request.OutletId.HasValue && line.SecondarySale is not null)
        {
            line.SecondarySale.OutletId = request.OutletId;
            line.SecondarySale.StampUpdated(userId);
        }

        // Teaching the profile is the whole point: resolve a code once, never see it again.
        if (request.RememberForFuture && line.SecondarySale is not null)
            await TeachProfileAsync(line.SecondarySale.PartnerId, line.ItemCodeRaw, request.ItemId,
                line.SecondarySale.OutletNameRaw, request.OutletId, userId);

        if (line.SecondarySale is not null)
        {
            var siblings = await db.SecondarySaleLines.ForTenant(tenant)
                .Where(l => l.SecondarySaleId == line.SecondarySaleId).ToListAsync();

            line.SecondarySale.IsMapped = line.SecondarySale.OutletId is not null && siblings.All(l => l.IsMapped);
            line.SecondarySale.MappingNote = line.SecondarySale.IsMapped ? null : line.SecondarySale.MappingNote;
        }

        await db.SaveChangesAsync();
        await RefreshBatchAccuracyAsync(line.SecondarySale?.UploadBatchId, userId);

        return new MappingExceptionDto
        {
            LineId = line.Id,
            SecondarySaleId = line.SecondarySaleId,
            RawItemCode = line.ItemCodeRaw,
            Quantity = line.Quantity,
            Value = line.LineTotal,
            Reason = line.IsMapped ? null : line.MappingNote,
        };
    }

    public async Task<List<MappingProfileDto>> ListMappingProfilesAsync(Guid? partnerId)
    {
        var rows = await db.MappingProfiles.ForTenant(tenant)
            .Include(m => m.Partner)
            .WhereIf(partnerId.HasValue, m => m.PartnerId == partnerId)
            .OrderBy(m => m.Name).ToListAsync();

        return rows.Select(r =>
        {
            var dto = r.ToDto();
            dto.MappedCodeCount = Deserialise(r.ItemCodeMap).Count;
            return dto;
        }).ToList();
    }

    public async Task<MappingProfileDto> SaveMappingProfileAsync(Guid? id, MappingProfileDto request, Guid userId)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new InvalidOperationException("A mapping profile needs a name.");

        SecondaryMappingProfile entity;
        if (id.HasValue)
        {
            entity = await db.MappingProfiles.ForTenant(tenant).FirstOrDefaultAsync(m => m.Id == id)
                ?? throw new InvalidOperationException("That mapping profile no longer exists.");
            entity.StampUpdated(userId);
        }
        else
        {
            entity = new SecondaryMappingProfile().StampNew(tenant, userId);
            db.MappingProfiles.Add(entity);
        }

        entity.Name = request.Name.Trim();
        entity.PartnerId = request.PartnerId;
        entity.FileFormat = string.IsNullOrWhiteSpace(request.FileFormat) ? "Csv" : request.FileFormat;
        entity.OutletColumn = request.OutletColumn;
        entity.ItemColumn = request.ItemColumn;
        entity.QuantityColumn = request.QuantityColumn;
        entity.ValueColumn = request.ValueColumn;
        entity.DateColumn = request.DateColumn;
        entity.UomColumn = request.UomColumn;
        entity.BatchColumn = request.BatchColumn;
        entity.InvoiceColumn = request.InvoiceColumn;
        entity.DateFormat = request.DateFormat;
        entity.HeaderRowIndex = request.HeaderRowIndex;
        entity.IsActive = request.IsActive;

        await db.SaveChangesAsync();
        return entity.ToDto();
    }

    // ═══ Stock declarations & norms ══════════════════════════════════════════

    public async Task<StockDeclarationDto> SubmitStockDeclarationAsync(
        SubmitStockDeclarationDto request, Guid userId)
    {
        if (request.Lines.Count == 0)
            throw new InvalidOperationException("A stock declaration needs at least one line.");

        var partner = await db.Partners.ForTenant(tenant).FirstOrDefaultAsync(p => p.Id == request.PartnerId)
            ?? throw new InvalidOperationException("That partner no longer exists.");

        var asOf = (request.AsOfDate == default ? DateTime.UtcNow : request.AsOfDate).Date;
        var settings = await db.Settings.ForTenant(tenant).FirstOrDefaultAsync();

        var existing = await db.StockDeclarations.ForTenant(tenant)
            .Include(d => d.Lines)
            .FirstOrDefaultAsync(d => d.PartnerId == request.PartnerId && d.AsOfDate == asOf);

        var declaration = existing;
        if (declaration is null)
        {
            declaration = new DistributorStockDeclaration
            {
                DeclarationNumber = await numbering.NextDeclarationNumberAsync(DateTime.UtcNow),
                PartnerId = request.PartnerId,
                AsOfDate = asOf,
            }.StampNew(tenant, userId);
            db.StockDeclarations.Add(declaration);
        }
        else
        {
            if (declaration.IsVerified)
                throw new InvalidOperationException("This declaration has been verified and cannot be replaced.");

            foreach (var line in declaration.Lines.Where(l => !l.IsDeleted).ToList())
                line.StampDeleted(userId);

            declaration.StampUpdated(userId);
        }

        declaration.SubmittedAt = DateTime.UtcNow;
        declaration.CurrencyCode = request.CurrencyCode ?? partner.CurrencyCode;
        declaration.Note = request.Note;

        var dueDay = settings?.SecondaryUploadDueDayOfMonth ?? 5;
        declaration.DueOn = new DateTime(asOf.Year, asOf.Month, 1, 0, 0, 0, DateTimeKind.Utc)
            .AddMonths(1).AddDays(dueDay - 1);
        declaration.LatenessDays = Math.Max(0, (int)(DateTime.UtcNow.Date - declaration.DueOn.Value.Date).TotalDays);

        var nearHorizon = DateTime.UtcNow.Date.AddDays(settings?.NearExpiryWarningDays ?? 90);

        foreach (var line in request.Lines)
        {
            var resolved = await ResolveItemAsync(line.ItemId, line.ItemCodeRaw, request.PartnerId);
            var factor = line.UomFactor <= 0 ? 1 : line.UomFactor;

            var nearExpiry = line.ExpiryDate is not null && line.ExpiryDate <= nearHorizon
                             && line.ExpiryDate >= DateTime.UtcNow.Date
                ? line.Quantity : line.NearExpiryQuantity;

            var expired = line.ExpiryDate is not null && line.ExpiryDate < DateTime.UtcNow.Date
                ? line.Quantity : line.ExpiredQuantity;

            declaration.Lines.Add(new DistributorStockLine
            {
                DeclarationId = declaration.Id,
                ItemId = resolved?.ItemId ?? line.ItemId,
                ItemName = resolved?.ItemName ?? line.ItemName,
                ItemCodeRaw = line.ItemCodeRaw,
                BrandId = line.BrandId,
                BatchId = line.BatchId,
                BatchNumber = line.BatchNumber,
                ExpiryDate = line.ExpiryDate,
                Uom = string.IsNullOrWhiteSpace(line.Uom) ? "PCS" : line.Uom,
                UomFactor = factor,
                Quantity = line.Quantity,
                BaseQuantity = line.Quantity * factor,
                UnitValue = line.UnitValue,
                TotalValue = Math.Round(line.Quantity * line.UnitValue, 4),
                Age0To30 = line.Age0To30,
                Age31To60 = line.Age31To60,
                Age61To90 = line.Age61To90,
                Age90Plus = line.Age90Plus,
                NearExpiryQuantity = nearExpiry,
                ExpiredQuantity = expired,
                DamagedQuantity = line.DamagedQuantity,
                IsMapped = resolved is not null || line.ItemId.HasValue,
            }.StampNew(tenant, userId));
        }

        declaration.LineCount = declaration.Lines.Count(l => !l.IsDeleted);
        declaration.TotalValue = declaration.Lines.Where(l => !l.IsDeleted).Sum(l => l.TotalValue);
        declaration.NearExpiryValue = declaration.Lines.Where(l => !l.IsDeleted)
            .Sum(l => l.NearExpiryQuantity * l.UnitValue);
        declaration.ExpiredValue = declaration.Lines.Where(l => !l.IsDeleted)
            .Sum(l => l.ExpiredQuantity * l.UnitValue);
        declaration.DamagedValue = declaration.Lines.Where(l => !l.IsDeleted)
            .Sum(l => l.DamagedQuantity * l.UnitValue);

        await db.SaveChangesAsync();
        await ComputeCoverAsync(declaration, userId);
        await EvaluateNormsAsync(declaration, userId);

        return (await GetStockDeclarationAsync(declaration.Id))!;
    }

    public async Task<StockDeclarationDto?> GetStockDeclarationAsync(Guid id)
    {
        var entity = await db.StockDeclarations.ForTenant(tenant)
            .Include(d => d.Partner)
            .Include(d => d.Lines.Where(l => !l.IsDeleted))
            .FirstOrDefaultAsync(d => d.Id == id);

        if (entity is null) return null;

        var dto = entity.ToDto();

        var norms = await db.StockNorms.ForTenant(tenant)
            .Where(n => n.PartnerId == entity.PartnerId)
            .ToDictionaryAsync(n => n.ItemId);

        foreach (var line in dto.Lines.Where(l => l.ItemId.HasValue))
            if (norms.TryGetValue(line.ItemId!.Value, out var norm))
            {
                line.NormDaysOfCover = norm.TargetDaysOfCover;
                line.IsUnderStocked = norm.TargetDaysOfCover > 0 && line.DaysOfCover < norm.TargetDaysOfCover * 0.5m;
                line.IsOverStocked = norm.TargetDaysOfCover > 0 && line.DaysOfCover > norm.TargetDaysOfCover * 1.5m;
            }

        return dto;
    }

    public async Task<PaginatedResponse<StockDeclarationDto>> ListStockDeclarationsAsync(
        Guid? partnerId, DateTime? from, DateTime? to, PaginationParams pagination)
    {
        var query = db.StockDeclarations.ForTenant(tenant)
            .Include(d => d.Partner)
            .WhereIf(partnerId.HasValue, d => d.PartnerId == partnerId)
            .WhereIf(from.HasValue, d => d.AsOfDate >= from)
            .WhereIf(to.HasValue, d => d.AsOfDate <= to);

        var total = await query.CountAsync();
        var rows = await query
            .OrderByDescending(d => d.AsOfDate)
            .Skip((pagination.PageNumber - 1) * pagination.PageSize)
            .Take(pagination.PageSize).ToListAsync();

        var dtos = rows.Select(r =>
        {
            var dto = r.ToDto();
            dto.Lines = [];
            return dto;
        });

        return PaginatedResponse<StockDeclarationDto>.Ok(dtos, total, pagination.PageNumber, pagination.PageSize);
    }

    public async Task<StockDeclarationDto> VerifyStockDeclarationAsync(Guid id, Guid userId)
    {
        var entity = await db.StockDeclarations.ForTenant(tenant).FirstOrDefaultAsync(d => d.Id == id)
            ?? throw new InvalidOperationException("That declaration no longer exists.");

        entity.IsVerified = true;
        entity.VerifiedAt = DateTime.UtcNow;
        entity.VerifiedByUserId = userId;
        entity.StampUpdated(userId);

        await db.SaveChangesAsync();
        return (await GetStockDeclarationAsync(id))!;
    }

    public async Task<List<StockNormDto>> ListNormsAsync(Guid? partnerId, bool? exceptionsOnly)
    {
        var rows = await db.StockNorms.ForTenant(tenant)
            .Include(n => n.Partner)
            .WhereIf(partnerId.HasValue, n => n.PartnerId == partnerId)
            .WhereIf(exceptionsOnly == true, n => n.IsUnderStocked || n.IsOverStocked)
            .OrderBy(n => n.ItemName)
            .ToListAsync();

        return rows.Select(r => r.ToDto()).ToList();
    }

    public async Task<StockNormDto> SaveNormAsync(Guid? id, StockNormDto request, Guid userId)
    {
        StockNorm entity;
        if (id.HasValue)
        {
            entity = await db.StockNorms.ForTenant(tenant).FirstOrDefaultAsync(n => n.Id == id)
                ?? throw new InvalidOperationException("That norm no longer exists.");
            entity.StampUpdated(userId);
        }
        else
        {
            var duplicate = await db.StockNorms.ForTenant(tenant)
                .FirstOrDefaultAsync(n => n.PartnerId == request.PartnerId && n.ItemId == request.ItemId);

            if (duplicate is not null)
            {
                entity = duplicate;
                entity.StampUpdated(userId);
            }
            else
            {
                entity = new StockNorm().StampNew(tenant, userId);
                db.StockNorms.Add(entity);
            }
        }

        entity.PartnerId = request.PartnerId;
        entity.ItemId = request.ItemId;
        entity.ItemName = request.ItemName;
        entity.Uom = string.IsNullOrWhiteSpace(request.Uom) ? "PCS" : request.Uom;
        entity.TargetDaysOfCover = request.TargetDaysOfCover;
        entity.MinQuantity = request.MinQuantity;
        entity.MaxQuantity = request.MaxQuantity;
        entity.ReorderQuantity = request.ReorderQuantity;
        entity.EffectiveFrom = request.EffectiveFrom == default ? DateTime.UtcNow.Date : request.EffectiveFrom;
        entity.EffectiveTo = request.EffectiveTo;

        await db.SaveChangesAsync();
        return entity.ToDto();
    }

    // ═══ Reconciliation ══════════════════════════════════════════════════════

    public async Task<List<ReconciliationDto>> ReconcileAsync(
        Guid? partnerId, DateTime periodStart, DateTime periodEnd, Guid userId)
    {
        var settings = await db.Settings.ForTenant(tenant).FirstOrDefaultAsync();
        var tolerance = settings?.ReconciliationTolerancePercent ?? 2;

        var partners = partnerId.HasValue
            ? await db.Partners.ForTenant(tenant).Where(p => p.Id == partnerId).ToListAsync()
            : await db.Partners.ForTenant(tenant).Where(p => p.Status == PartnerStatus.Active).ToListAsync();

        var results = new List<SellInSellOutReconciliation>();

        foreach (var partner in partners)
        {
            // Opening comes from the declaration immediately before the period; without one, the
            // identity cannot be checked and the row is marked Missing rather than assumed zero.
            var opening = await db.StockDeclarationLines.ForTenant(tenant)
                .Where(l => db.StockDeclarations.ForTenant(tenant).Any(d =>
                    d.Id == l.DeclarationId && d.PartnerId == partner.Id && d.AsOfDate < periodStart))
                .GroupBy(l => l.ItemId)
                .Select(g => new { ItemId = g.Key, Quantity = g.Sum(x => x.BaseQuantity) })
                .ToListAsync();

            var closing = await db.StockDeclarationLines.ForTenant(tenant)
                .Where(l => db.StockDeclarations.ForTenant(tenant).Any(d =>
                    d.Id == l.DeclarationId && d.PartnerId == partner.Id
                    && d.AsOfDate >= periodStart && d.AsOfDate <= periodEnd))
                .GroupBy(l => l.ItemId)
                .Select(g => new { ItemId = g.Key, Quantity = g.Sum(x => x.BaseQuantity) })
                .ToListAsync();

            var primary = await db.OrderLines.ForTenant(tenant)
                .Where(l => db.Orders.ForTenant(tenant).Any(o =>
                    o.Id == l.OrderId && o.PartnerId == partner.Id
                    && o.OrderDate >= periodStart && o.OrderDate <= periodEnd
                    && o.Status != DistributionOrderStatus.Cancelled
                    && o.Status != DistributionOrderStatus.Rejected
                    && o.Status != DistributionOrderStatus.Draft))
                .GroupBy(l => new { l.ItemId, l.ItemName })
                .Select(g => new
                {
                    g.Key.ItemId,
                    g.Key.ItemName,
                    Quantity = g.Sum(x => x.BaseQuantity),
                    UnitPrice = g.Average(x => x.UnitPrice),
                })
                .ToListAsync();

            var secondary = await db.SecondarySaleLines.ForTenant(tenant)
                .Where(l => l.ItemId != null && db.SecondarySales.ForTenant(tenant).Any(s =>
                    s.Id == l.SecondarySaleId && s.PartnerId == partner.Id
                    && s.SaleDate >= periodStart && s.SaleDate <= periodEnd))
                .GroupBy(l => l.ItemId!.Value)
                .Select(g => new { ItemId = g.Key, Quantity = g.Sum(x => x.BaseQuantity) })
                .ToListAsync();

            var returns = await db.ReturnLines.ForTenant(tenant)
                .Where(l => db.Returns.ForTenant(tenant).Any(r =>
                    r.Id == l.ReturnId && r.PartnerId == partner.Id
                    && r.RequestedOn >= periodStart && r.RequestedOn <= periodEnd))
                .GroupBy(l => l.ItemId)
                .Select(g => new { ItemId = g.Key, Quantity = g.Sum(x => x.ApprovedQuantity * x.UomFactor) })
                .ToListAsync();

            var itemIds = primary.Select(p => p.ItemId)
                .Concat(secondary.Select(s => s.ItemId))
                .Concat(opening.Where(o => o.ItemId.HasValue).Select(o => o.ItemId!.Value))
                .Distinct().ToList();

            foreach (var itemId in itemIds)
            {
                var openQty = opening.FirstOrDefault(o => o.ItemId == itemId)?.Quantity ?? 0;
                var primaryRow = primary.FirstOrDefault(p => p.ItemId == itemId);
                var primaryQty = primaryRow?.Quantity ?? 0;
                var secondaryQty = secondary.FirstOrDefault(s => s.ItemId == itemId)?.Quantity ?? 0;
                var returnQty = returns.FirstOrDefault(r => r.ItemId == itemId)?.Quantity ?? 0;
                var closingRow = closing.FirstOrDefault(c => c.ItemId == itemId);

                var computed = openQty + primaryQty - secondaryQty - returnQty;
                var declared = closingRow?.Quantity ?? 0;
                var variance = declared - computed;

                var outcome = closingRow is null
                    ? ReconciliationOutcome.Missing
                    : Math.Abs(computed) > 0 && Math.Abs(variance / Math.Max(1, computed) * 100) <= tolerance
                        ? ReconciliationOutcome.Balanced
                        : variance < 0 ? ReconciliationOutcome.ShortDeclared : ReconciliationOutcome.OverDeclared;

                var existing = await db.Reconciliations.ForTenant(tenant)
                    .FirstOrDefaultAsync(r => r.PartnerId == partner.Id && r.ItemId == itemId
                                              && r.PeriodStart == periodStart);

                var row = existing;
                if (row is null)
                {
                    row = new SellInSellOutReconciliation
                    {
                        PartnerId = partner.Id,
                        ItemId = itemId,
                        PeriodStart = periodStart,
                        PeriodEnd = periodEnd,
                    }.StampNew(tenant, userId);
                    db.Reconciliations.Add(row);
                }
                else
                {
                    row.StampUpdated(userId);
                }

                row.ItemName = primaryRow?.ItemName ?? row.ItemName;
                row.OpeningQuantity = openQty;
                row.PrimaryQuantity = primaryQty;
                row.SecondaryQuantity = secondaryQty;
                row.ReturnQuantity = returnQty;
                row.DeclaredClosingQuantity = declared;
                row.ComputedClosingQuantity = computed;
                row.VarianceQuantity = variance;
                row.VariancePercent = computed == 0 ? 0 : Math.Round(variance / computed * 100, 4);
                row.VarianceValue = Math.Round(variance * (primaryRow?.UnitPrice ?? 0), 4);
                row.Outcome = outcome;
                row.ComputedAt = DateTime.UtcNow;

                results.Add(row);
            }
        }

        await db.SaveChangesAsync();
        return results.Select(r => r.ToDto()).ToList();
    }

    public async Task<PaginatedResponse<ReconciliationDto>> ListReconciliationsAsync(
        Guid? partnerId, ReconciliationOutcome? outcome, bool? unexplainedOnly,
        DateTime? from, DateTime? to, PaginationParams pagination)
    {
        var query = db.Reconciliations.ForTenant(tenant)
            .Include(r => r.Partner)
            .WhereIf(partnerId.HasValue, r => r.PartnerId == partnerId)
            .WhereIf(outcome.HasValue, r => r.Outcome == outcome)
            .WhereIf(unexplainedOnly == true, r => !r.IsExplained && r.Outcome != ReconciliationOutcome.Balanced)
            .WhereIf(from.HasValue, r => r.PeriodStart >= from)
            .WhereIf(to.HasValue, r => r.PeriodEnd <= to);

        var total = await query.CountAsync();
        var rows = await query
            .OrderByDescending(r => Math.Abs(r.VarianceValue))
            .Skip((pagination.PageNumber - 1) * pagination.PageSize)
            .Take(pagination.PageSize).ToListAsync();

        return PaginatedResponse<ReconciliationDto>.Ok(
            rows.Select(r => r.ToDto()), total, pagination.PageNumber, pagination.PageSize);
    }

    public async Task<ReconciliationDto> ExplainAsync(ExplainReconciliationDto request, Guid userId)
    {
        var row = await db.Reconciliations.ForTenant(tenant)
            .Include(r => r.Partner)
            .FirstOrDefaultAsync(r => r.Id == request.ReconciliationId)
            ?? throw new InvalidOperationException("That reconciliation row no longer exists.");

        var reason = await db.ReasonCodes.ForCompany(tenant)
            .FirstOrDefaultAsync(r => r.Id == request.ReasonCodeId)
            ?? throw new InvalidOperationException("That reason is not recognised.");

        if (reason.RequiresNote && string.IsNullOrWhiteSpace(request.Note))
            throw new InvalidOperationException($"'{reason.Name}' needs a note.");

        row.IsExplained = true;
        row.ReasonCodeId = request.ReasonCodeId;
        row.ExplanationNote = request.Note;
        row.ResolvedAt = DateTime.UtcNow;
        row.ResolvedByUserId = userId;
        row.StampUpdated(userId);

        await db.SaveChangesAsync();
        return row.ToDto();
    }

    public async Task<ChannelInventoryDto> GetChannelInventoryAsync(
        DateTime periodStart, DateTime periodEnd, Guid? territoryId)
    {
        var settings = await db.Settings.ForTenant(tenant).FirstOrDefaultAsync();

        var primary = await db.Orders.ForTenant(tenant)
            .Where(o => o.PartnerId != null && o.OrderDate >= periodStart && o.OrderDate <= periodEnd
                        && o.Status != DistributionOrderStatus.Cancelled
                        && o.Status != DistributionOrderStatus.Rejected
                        && o.Status != DistributionOrderStatus.Draft)
            .WhereIf(territoryId.HasValue, o => o.TerritoryId == territoryId)
            .SumAsync(o => (decimal?)o.TotalAmount) ?? 0;

        var secondary = await db.SecondarySales.ForTenant(tenant)
            .Where(s => s.SaleDate >= periodStart && s.SaleDate <= periodEnd && s.IsMapped)
            .WhereIf(territoryId.HasValue, s => s.TerritoryId == territoryId)
            .SumAsync(s => (decimal?)s.TotalAmount) ?? 0;

        var declarations = await db.StockDeclarations.ForTenant(tenant)
            .Include(d => d.Partner)
            .Where(d => d.AsOfDate >= periodStart && d.AsOfDate <= periodEnd)
            .ToListAsync();

        var stock = declarations.Sum(d => d.TotalValue);
        var dailySecondary = secondary / Math.Max(1, (periodEnd - periodStart).Days + 1);

        var dto = new ChannelInventoryDto
        {
            PeriodStart = periodStart,
            PeriodEnd = periodEnd,
            CurrencyCode = settings?.BaseCurrencyCode ?? "USD",
            PrimarySalesValue = primary,
            SecondarySalesValue = secondary,
            ChannelStockValue = stock,
            ChannelStockDaysOfCover = dailySecondary == 0 ? 0 : Math.Round(stock / dailySecondary, 1),
            // Below 100% means the pipeline is filling rather than selling. The single most
            // important sentence a consumer-goods board can be told.
            SellThroughPercent = DistributionMapper.Percent(secondary, primary),
            NearExpiryValue = declarations.Sum(d => d.NearExpiryValue),
            ExpiredValue = declarations.Sum(d => d.ExpiredValue),
        };

        dto.PartnerCount = await db.Partners.ForTenant(tenant)
            .CountAsync(p => p.Status == PartnerStatus.Active
                             && (territoryId == null || p.TerritoryId == territoryId));

        dto.ReportingPartnerCount = declarations.Select(d => d.PartnerId).Distinct().Count();
        dto.ReportingCompliancePercent = DistributionMapper.Percent(dto.ReportingPartnerCount, dto.PartnerCount);

        dto.UnexplainedVarianceCount = await db.Reconciliations.ForTenant(tenant)
            .CountAsync(r => r.PeriodStart >= periodStart && r.PeriodEnd <= periodEnd
                             && !r.IsExplained && r.Outcome != ReconciliationOutcome.Balanced);

        dto.ByPartner = declarations
            .GroupBy(d => new { d.PartnerId, Name = d.Partner?.Name ?? "Unknown" })
            .Select(g => new ChannelInventoryRowDto
            {
                Id = g.Key.PartnerId,
                Name = g.Key.Name,
                StockValue = g.Sum(x => x.TotalValue),
                NearExpiryValue = g.Sum(x => x.NearExpiryValue),
                DaysOfCover = g.Average(x => x.DaysOfCover),
            })
            .OrderByDescending(r => r.StockValue)
            .Take(50).ToList();

        return dto;
    }

    public async Task<List<PartnerDataQualityDto>> GetDataQualityAsync(DateTime periodStart, DateTime periodEnd)
    {
        var months = Math.Max(1, ((periodEnd.Year - periodStart.Year) * 12) + periodEnd.Month - periodStart.Month + 1);

        var partners = await db.Partners.ForTenant(tenant)
            .Where(p => p.Status == PartnerStatus.Active)
            .Select(p => new { p.Id, p.Name, p.SecondaryCaptureMode })
            .ToListAsync();

        var uploads = await db.SecondaryUploads.ForTenant(tenant)
            .Where(u => u.PeriodStart >= periodStart && u.PeriodEnd <= periodEnd)
            .ToListAsync();

        var declarations = await db.StockDeclarations.ForTenant(tenant)
            .Where(d => d.AsOfDate >= periodStart && d.AsOfDate <= periodEnd)
            .ToListAsync();

        var variances = await db.Reconciliations.ForTenant(tenant)
            .Where(r => r.PeriodStart >= periodStart && r.PeriodEnd <= periodEnd)
            .GroupBy(r => r.PartnerId)
            .Select(g => new { PartnerId = g.Key, Average = g.Average(x => Math.Abs(x.VariancePercent)) })
            .ToDictionaryAsync(x => x.PartnerId, x => x.Average);

        return partners.Select(p =>
        {
            var partnerUploads = uploads.Where(u => u.PartnerId == p.Id).ToList();
            var partnerDeclarations = declarations.Where(d => d.PartnerId == p.Id).ToList();
            var submitted = Math.Max(partnerUploads.Count, partnerDeclarations.Count);

            var submissionRate = DistributionMapper.Percent(submitted, months);
            var lateness = partnerUploads.Count > 0
                ? (decimal)partnerUploads.Average(u => u.LatenessDays)
                : partnerDeclarations.Count > 0
                    ? (decimal)partnerDeclarations.Average(d => d.LatenessDays)
                    : 0;

            var accuracy = partnerUploads.Count > 0
                ? partnerUploads.Average(u => u.MappingAccuracyPercent)
                : p.SecondaryCaptureMode == SecondaryCaptureMode.Transactional ? 100 : 0;

            var variance = variances.GetValueOrDefault(p.Id);

            // A single composite so the list can be sorted worst-first. Timeliness and accuracy
            // matter most; variance is a softer signal because some of it is genuinely the market.
            var score = Math.Round(
                submissionRate * 0.4m
                + Math.Max(0, 100 - lateness * 5) * 0.2m
                + accuracy * 0.3m
                + Math.Max(0, 100 - variance) * 0.1m, 1);

            return new PartnerDataQualityDto
            {
                PartnerId = p.Id,
                PartnerName = p.Name,
                CaptureMode = p.SecondaryCaptureMode,
                PeriodsExpected = months,
                PeriodsSubmitted = submitted,
                SubmissionRatePercent = submissionRate,
                AverageLatenessDays = Math.Round(lateness, 1),
                AverageMappingAccuracyPercent = Math.Round(accuracy, 1),
                AverageVariancePercent = Math.Round(variance, 2),
                QualityScore = Math.Clamp(score, 0, 100),
                LastSubmissionAt = partnerUploads.Count > 0
                    ? partnerUploads.Max(u => u.SubmittedAt)
                    : partnerDeclarations.Count > 0 ? partnerDeclarations.Max(d => d.SubmittedAt) : null,
            };
        })
        .OrderBy(p => p.QualityScore)
        .ToList();
    }

    // ═══ Internals ═══════════════════════════════════════════════════════════

    private record ParsedRow(string? OutletRaw, string? ItemRaw, decimal Quantity, decimal Value,
        DateTime SaleDate, string? Uom, string? BatchNumber);

    /// <summary>
    /// Reads a delimited file through the partner's column mapping. Deliberately forgiving —
    /// a row that cannot be read becomes an exception rather than aborting the whole upload,
    /// because a distributor will not resend a file over one bad line.
    /// </summary>
    private static List<ParsedRow> ParseDelimited(Stream content, SecondaryMappingProfile profile)
    {
        var rows = new List<ParsedRow>();

        using var reader = new StreamReader(content);
        var lines = new List<string>();
        while (reader.ReadLine() is { } line) lines.Add(line);

        if (lines.Count <= profile.HeaderRowIndex) return rows;

        var header = SplitLine(lines[profile.HeaderRowIndex]);

        int IndexOf(string? column) => string.IsNullOrWhiteSpace(column)
            ? -1
            : Array.FindIndex(header, h => h.Trim().Equals(column.Trim(), StringComparison.OrdinalIgnoreCase));

        var outletIndex = IndexOf(profile.OutletColumn);
        var itemIndex = IndexOf(profile.ItemColumn);
        var quantityIndex = IndexOf(profile.QuantityColumn);
        var valueIndex = IndexOf(profile.ValueColumn);
        var dateIndex = IndexOf(profile.DateColumn);
        var uomIndex = IndexOf(profile.UomColumn);
        var batchIndex = IndexOf(profile.BatchColumn);

        for (var i = profile.HeaderRowIndex + 1; i < lines.Count; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;

            var cells = SplitLine(lines[i]);

            string? Cell(int index) => index >= 0 && index < cells.Length ? cells[index].Trim() : null;

            decimal Number(int index)
                => decimal.TryParse(Cell(index), NumberStyles.Any, CultureInfo.InvariantCulture, out var value)
                    ? value : 0;

            var dateText = Cell(dateIndex);
            var saleDate = DateTime.UtcNow.Date;

            if (!string.IsNullOrWhiteSpace(dateText))
            {
                if (!string.IsNullOrWhiteSpace(profile.DateFormat)
                    && DateTime.TryParseExact(dateText, profile.DateFormat, CultureInfo.InvariantCulture,
                        DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var exact))
                    saleDate = exact.Date;
                else if (DateTime.TryParse(dateText, CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var loose))
                    saleDate = loose.Date;
            }

            var quantity = Number(quantityIndex);
            var value = Number(valueIndex);
            if (quantity == 0 && value == 0) continue;

            rows.Add(new ParsedRow(
                Cell(outletIndex), Cell(itemIndex), quantity, value, saleDate,
                Cell(uomIndex), Cell(batchIndex)));
        }

        return rows;
    }

    private static string[] SplitLine(string line)
        => line.Contains('\t') ? line.Split('\t') : line.Split(',');

    private static Dictionary<string, string> Deserialise(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return [];
        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private record ResolvedItem(Guid ItemId, string ItemName);

    private async Task<ResolvedItem?> ResolveItemAsync(
        Guid? itemId, string? rawCode, Guid partnerId, Dictionary<string, string>? map = null)
    {
        if (itemId.HasValue)
        {
            var name = await db.PriceListLines.ForTenant(tenant)
                .Where(p => p.ItemId == itemId).Select(p => p.ItemName).FirstOrDefaultAsync();
            return new ResolvedItem(itemId.Value, name ?? string.Empty);
        }

        if (string.IsNullOrWhiteSpace(rawCode)) return null;

        map ??= Deserialise(await db.MappingProfiles.ForTenant(tenant)
            .Where(m => m.PartnerId == partnerId).Select(m => m.ItemCodeMap).FirstOrDefaultAsync());

        if (map.TryGetValue(rawCode.Trim(), out var mapped) && Guid.TryParse(mapped, out var mappedId))
        {
            var name = await db.PriceListLines.ForTenant(tenant)
                .Where(p => p.ItemId == mappedId).Select(p => p.ItemName).FirstOrDefaultAsync();
            return new ResolvedItem(mappedId, name ?? string.Empty);
        }

        // Fall back to our own item code, which is right often enough to be worth trying.
        var direct = await db.PriceListLines.ForTenant(tenant)
            .Where(p => p.ItemCode == rawCode.Trim())
            .Select(p => new { p.ItemId, p.ItemName })
            .FirstOrDefaultAsync();

        return direct is null ? null : new ResolvedItem(direct.ItemId, direct.ItemName);
    }

    private async Task<Guid?> ResolveOutletAsync(
        string? rawName, Dictionary<string, string> map, Guid partnerId)
    {
        if (string.IsNullOrWhiteSpace(rawName)) return null;

        if (map.TryGetValue(rawName.Trim(), out var mapped) && Guid.TryParse(mapped, out var mappedId))
            return mappedId;

        var byCode = await db.Outlets.ForTenant(tenant)
            .Where(o => o.Code == rawName.Trim()).Select(o => (Guid?)o.Id).FirstOrDefaultAsync();
        if (byCode.HasValue) return byCode;

        // Exact name inside the partner's own network only — a fuzzy match across the whole
        // universe would silently attribute one distributor's sales to another's shop.
        return await db.Outlets.ForTenant(tenant)
            .Where(o => o.PartnerId == partnerId && o.Name == rawName.Trim())
            .Select(o => (Guid?)o.Id).FirstOrDefaultAsync();
    }

    private async Task<List<MappingSuggestionDto>> SuggestItemsAsync(string rawCode)
    {
        var term = $"%{rawCode.Trim()}%";

        return await db.PriceListLines.ForTenant(tenant)
            .Where(p => EF.Functions.ILike(p.ItemCode ?? "", term) || EF.Functions.ILike(p.ItemName, term))
            .Select(p => new MappingSuggestionDto
            {
                ItemId = p.ItemId,
                Name = p.ItemName,
                Code = p.ItemCode,
                Confidence = p.ItemCode == rawCode ? 100 : 60,
            })
            .Distinct()
            .Take(5)
            .ToListAsync();
    }

    private async Task<List<MappingSuggestionDto>> SuggestOutletsAsync(string rawName, Guid partnerId)
    {
        var term = $"%{rawName.Trim()}%";

        return await db.Outlets.ForTenant(tenant)
            .Where(o => o.PartnerId == partnerId
                        && (EF.Functions.ILike(o.Name, term) || EF.Functions.ILike(o.Code ?? "", term)))
            .Select(o => new MappingSuggestionDto
            {
                OutletId = o.Id,
                Name = o.Name,
                Code = o.Code,
                Confidence = o.Name == rawName ? 100 : 55,
            })
            .Take(5)
            .ToListAsync();
    }

    private async Task TeachProfileAsync(
        Guid partnerId, string? rawItemCode, Guid? itemId, string? rawOutlet, Guid? outletId, Guid userId)
    {
        var profile = await db.MappingProfiles.ForTenant(tenant)
            .Where(m => m.PartnerId == partnerId)
            .OrderByDescending(m => m.LastUsedAt)
            .FirstOrDefaultAsync();

        if (profile is null) return;

        if (!string.IsNullOrWhiteSpace(rawItemCode) && itemId.HasValue)
        {
            var map = Deserialise(profile.ItemCodeMap);
            map[rawItemCode.Trim()] = itemId.Value.ToString();
            profile.ItemCodeMap = JsonSerializer.Serialize(map);
        }

        if (!string.IsNullOrWhiteSpace(rawOutlet) && outletId.HasValue)
        {
            var map = Deserialise(profile.OutletCodeMap);
            map[rawOutlet.Trim()] = outletId.Value.ToString();
            profile.OutletCodeMap = JsonSerializer.Serialize(map);
        }

        profile.StampUpdated(userId);
        await db.SaveChangesAsync();
    }

    private async Task RefreshBatchAccuracyAsync(Guid? batchId, Guid userId)
    {
        if (batchId is null) return;

        var batch = await db.SecondaryUploads.ForTenant(tenant).FirstOrDefaultAsync(u => u.Id == batchId);
        if (batch is null) return;

        var lines = await db.SecondarySaleLines.ForTenant(tenant)
            .Where(l => db.SecondarySales.ForTenant(tenant).Any(s => s.Id == l.SecondarySaleId
                                                                     && s.UploadBatchId == batchId))
            .Select(l => l.IsMapped)
            .ToListAsync();

        batch.MappedRows = lines.Count(m => m);
        batch.UnmappedRows = lines.Count(m => !m);
        batch.MappingAccuracyPercent = DistributionMapper.Percent(batch.MappedRows, lines.Count);
        batch.Status = batch.UnmappedRows == 0 && batch.Status != UploadBatchStatus.Posted
            ? UploadBatchStatus.Mapped
            : batch.Status;
        batch.StampUpdated(userId);

        await db.SaveChangesAsync();
    }

    private async Task ComputeCoverAsync(DistributorStockDeclaration declaration, Guid userId)
    {
        // Cover is stock over the partner's own recent daily offtake — theirs, not the market's,
        // because a distributor with two weeks of cover in a slow town is fine and one with two
        // weeks in a fast town is about to stock out.
        var since = declaration.AsOfDate.AddDays(-90);

        var offtake = await db.SecondarySaleLines.ForTenant(tenant)
            .Where(l => l.ItemId != null && db.SecondarySales.ForTenant(tenant).Any(s =>
                s.Id == l.SecondarySaleId && s.PartnerId == declaration.PartnerId
                && s.SaleDate >= since && s.SaleDate <= declaration.AsOfDate))
            .GroupBy(l => l.ItemId!.Value)
            .Select(g => new { ItemId = g.Key, Quantity = g.Sum(x => x.BaseQuantity) })
            .ToDictionaryAsync(x => x.ItemId, x => x.Quantity / 90m);

        decimal totalDaily = 0;

        foreach (var line in declaration.Lines.Where(l => !l.IsDeleted && l.ItemId.HasValue))
        {
            var daily = offtake.GetValueOrDefault(line.ItemId!.Value);
            line.DaysOfCover = daily <= 0 ? 0 : Math.Round(line.BaseQuantity / daily, 1);
            totalDaily += daily * line.UnitValue;
        }

        declaration.DaysOfCover = totalDaily <= 0 ? 0 : Math.Round(declaration.TotalValue / totalDaily, 1);
        declaration.StampUpdated(userId);

        await db.SaveChangesAsync();
    }

    private async Task EvaluateNormsAsync(DistributorStockDeclaration declaration, Guid userId)
    {
        var norms = await db.StockNorms.ForTenant(tenant)
            .Where(n => n.PartnerId == declaration.PartnerId)
            .ToListAsync();

        if (norms.Count == 0) return;

        foreach (var norm in norms)
        {
            var line = declaration.Lines.FirstOrDefault(l => !l.IsDeleted && l.ItemId == norm.ItemId);

            norm.CurrentQuantity = line?.BaseQuantity ?? 0;
            norm.CurrentDaysOfCover = line?.DaysOfCover ?? 0;
            norm.IsUnderStocked = norm.TargetDaysOfCover > 0
                                  && norm.CurrentDaysOfCover < norm.TargetDaysOfCover * 0.5m;
            norm.IsOverStocked = norm.TargetDaysOfCover > 0
                                 && norm.CurrentDaysOfCover > norm.TargetDaysOfCover * 1.5m;
            norm.EvaluatedAt = DateTime.UtcNow;
            norm.StampUpdated(userId);
        }

        await db.SaveChangesAsync();
    }

    private static void Total(SecondarySale sale)
    {
        var lines = sale.Lines.Where(l => !l.IsDeleted).ToList();
        sale.SubTotal = lines.Sum(l => l.LineTotal - l.TaxAmount);
        sale.TaxAmount = lines.Sum(l => l.TaxAmount);
        sale.DiscountAmount = lines.Sum(l => l.DiscountAmount);
        sale.SchemeAmount = lines.Sum(l => l.SchemeAmount);
        sale.TotalAmount = lines.Sum(l => l.LineTotal);
        sale.TotalQuantity = lines.Sum(l => l.BaseQuantity);
        sale.LineCount = lines.Count;
    }
}
