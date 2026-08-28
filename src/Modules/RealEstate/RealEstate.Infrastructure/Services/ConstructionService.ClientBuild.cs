using Microsoft.EntityFrameworkCore;
using Nexcore.SharedKernel.Api;
using RealEstate.Application.DTOs;
using RealEstate.Domain.Entities;
using RealEstate.Domain.Enums;
using RealEstate.Infrastructure.Persistence;

namespace RealEstate.Infrastructure.Services;

/// <summary>
/// Client build: the turnkey contract where a customer owns the land and we build on it.
///
/// This is the third shape of real estate business and the one nobody's software handles. Its
/// characteristic failure is margin erosion — a hundred small unpriced favours over eighteen
/// months — so the contract's cost sheet attributes the erosion to its actual causes rather than
/// reporting one gloomy number, and a client variation is not billable until the client has
/// approved it in a way that can be shown to them later.
/// </summary>
public partial class ConstructionService
{
    // ═══ Contracts ═══════════════════════════════════════════════════════════

    public async Task<PaginatedResponse<ClientBuildContractListItemDto>> GetClientBuildsAsync(
        ListQueryDto query, string? status)
    {
        var partyIds = string.IsNullOrWhiteSpace(query.Search)
            ? null
            : Db.Parties.ForCompany(Tenant)
                .Where(p => p.DisplayName != null && p.DisplayName.Contains(query.Search!))
                .Select(p => p.Id);

        var q = Db.ClientBuildContracts.ForCompany(Tenant)
            .WhereIf(!string.IsNullOrWhiteSpace(status), c => c.Status == status)
            .WhereIf(query.OfficeId.HasValue, c => c.OfficeId == query.OfficeId)
            .WhereIf(partyIds is not null,
                c => partyIds!.Contains(c.ClientPartyId) || c.Reference.Contains(query.Search!) || c.Name.Contains(query.Search!))
            .OrderByDescending(c => c.SignedOn);

        return await PageAsync(q, query, MapClientBuildListAsync);
    }

    private async Task<List<ClientBuildContractListItemDto>> MapClientBuildListAsync(List<ClientBuildContract> contracts)
    {
        if (contracts.Count == 0) return [];

        var unit = await AreaUnitAsync();
        var ids = contracts.Select(c => c.Id).ToList();

        var clients = await Db.Parties.ForCompany(Tenant)
            .Where(p => contracts.Select(c => c.ClientPartyId).Contains(p.Id))
            .ToListAsync();

        var managers = await AgentUserNamesAsync(contracts.Select(c => c.ProjectManagerUserId));

        var variations = await Db.ClientVariations.ForCompany(Tenant)
            .Where(v => ids.Contains(v.ClientBuildContractId))
            .Select(v => new { v.ClientBuildContractId, v.Status, v.ClientApproved })
            .ToListAsync();

        var specIds = contracts.Where(c => c.SpecificationScheduleId.HasValue)
            .Select(c => c.SpecificationScheduleId!.Value).Distinct().ToList();

        var pendingSelections = specIds.Count == 0
            ? []
            : await Db.SpecificationItems.ForCompany(Tenant)
                .Where(i => specIds.Contains(i.SpecificationScheduleId)
                         && i.IsClientSelectable
                         && (i.SelectedOption == null || i.SelectedOption == ""))
                .GroupBy(i => i.SpecificationScheduleId)
                .Select(g => new { ScheduleId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.ScheduleId, x => x.Count);

        return contracts.Select(c =>
        {
            var client = clients.FirstOrDefault(p => p.Id == c.ClientPartyId);
            var mine = variations.Where(v => v.ClientBuildContractId == c.Id).ToList();

            var revised = c.RevisedContractValue > 0m ? c.RevisedContractValue : c.ContractValue + c.ApprovedVariations;
            var forecast = c.ForecastFinalCost > 0m ? c.ForecastFinalCost : c.BudgetCost;
            var margin = revised - forecast;

            var slip = c.PlannedCompletionDate is null || c.ForecastCompletionDate is null
                ? (int?)null
                : c.ForecastCompletionDate.Value.DayNumber
                  - c.PlannedCompletionDate.Value.AddDays(c.ExtensionDaysGranted).DayNumber;

            return new ClientBuildContractListItemDto
            {
                Id = c.Id,
                Reference = c.Reference,
                Name = c.Name,
                ClientPartyId = c.ClientPartyId,
                ClientName = client is null ? "—" : RealEstateMapper.DisplayName(client),
                ClientPhone = client?.PrimaryPhone,
                SiteAddress = c.SiteAddress,
                Kind = c.Kind,
                Grade = c.Grade,
                PlotArea = RealEstateMapper.Area(c.PlotAreaSqFt, unit),
                CoveredArea = RealEstateMapper.Area(c.CoveredAreaSqFt, unit),
                RatePerSqFt = c.RatePerSqFt,
                ContractValue = c.ContractValue,
                ApprovedVariations = c.ApprovedVariations,
                RevisedContractValue = revised,
                CurrencyCode = c.CurrencyCode,
                Status = c.Status,
                SignedOn = c.SignedOn,
                StartDate = c.StartDate,
                PlannedCompletionDate = c.PlannedCompletionDate,
                ForecastCompletionDate = c.ForecastCompletionDate,
                SlipDays = slip,
                TotalDemanded = c.TotalDemanded,
                TotalReceived = c.TotalReceived,
                Outstanding = c.Outstanding,
                RetentionHeldByClient = c.RetentionHeldByClient,
                ProgressPercent = c.ProgressPercent,
                BudgetCost = c.BudgetCost,
                ActualCost = c.ActualCost,
                ForecastFinalCost = forecast,
                ForecastMargin = RealEstateMapper.Money(margin),
                MarginPercent = RealEstateMapper.Percent(margin, revised),
                ProjectManagerName = c.ProjectManagerUserId is null ? null : managers.GetValueOrDefault(c.ProjectManagerUserId.Value),

                OpenVariationCount = mine.Count(v => v.Status is not (VariationStatus.Approved or VariationStatus.Rejected)),

                // Every unmade selection is a decision the site is waiting on. It is the honest
                // measure of whether a turnkey job can actually proceed.
                PendingClientDecisions = c.SpecificationScheduleId is null
                    ? 0
                    : pendingSelections.GetValueOrDefault(c.SpecificationScheduleId.Value),

                // Margin at risk means it has already fallen below a fifth of what was budgeted,
                // which on a fixed-price turnkey job is the point of no return.
                IsMarginAtRisk = margin < (revised - c.BudgetCost) * 0.8m || margin < 0m,
            };
        }).ToList();
    }

    public async Task<ClientBuildContractDetailDto?> GetClientBuildAsync(Guid id)
    {
        var contract = await Db.ClientBuildContracts.ForCompany(Tenant)
            .Include(c => c.ScopeItems)
            .Include(c => c.ClientSuppliedMaterials)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (contract is null) return null;

        var head = (await MapClientBuildListAsync([contract]))[0];
        var today = Today;

        var detail = new ClientBuildContractDetailDto
        {
            Id = head.Id,
            Reference = head.Reference,
            Name = head.Name,
            ClientPartyId = head.ClientPartyId,
            ClientName = head.ClientName,
            ClientPhone = head.ClientPhone,
            SiteAddress = head.SiteAddress,
            Kind = head.Kind,
            Grade = head.Grade,
            PlotArea = head.PlotArea,
            CoveredArea = head.CoveredArea,
            RatePerSqFt = head.RatePerSqFt,
            ContractValue = head.ContractValue,
            ApprovedVariations = head.ApprovedVariations,
            RevisedContractValue = head.RevisedContractValue,
            CurrencyCode = head.CurrencyCode,
            Status = head.Status,
            SignedOn = head.SignedOn,
            StartDate = head.StartDate,
            PlannedCompletionDate = head.PlannedCompletionDate,
            ForecastCompletionDate = head.ForecastCompletionDate,
            SlipDays = head.SlipDays,
            TotalDemanded = head.TotalDemanded,
            TotalReceived = head.TotalReceived,
            Outstanding = head.Outstanding,
            RetentionHeldByClient = head.RetentionHeldByClient,
            ProgressPercent = head.ProgressPercent,
            BudgetCost = head.BudgetCost,
            ActualCost = head.ActualCost,
            ForecastFinalCost = head.ForecastFinalCost,
            ForecastMargin = head.ForecastMargin,
            MarginPercent = head.MarginPercent,
            ProjectManagerName = head.ProjectManagerName,
            OpenVariationCount = head.OpenVariationCount,
            PendingClientDecisions = head.PendingClientDecisions,
            IsMarginAtRisk = head.IsMarginAtRisk,

            CoClientPartyId = contract.CoClientPartyId,
            PropertyId = contract.PropertyId,
            LandParcelId = contract.LandParcelId,
            SourceBookingId = contract.SourceBookingId,
            EnquiryId = contract.EnquiryId,
            EstimateId = contract.EstimateId,
            SpecificationScheduleId = contract.SpecificationScheduleId,
            ConstructionProjectId = contract.ConstructionProjectId,
            FeePercent = contract.FeePercent,
            FixedFee = contract.FixedFee,
            GuaranteedMaximumPrice = contract.GuaranteedMaximumPrice,
            ActualCompletionDate = contract.ActualCompletionDate,
            ExtensionDaysGranted = contract.ExtensionDaysGranted,
            LiquidatedDamagesPerDay = contract.LiquidatedDamagesPerDay,
            LiquidatedDamagesCapPercent = contract.LiquidatedDamagesCapPercent,
            PaymentPlanId = contract.PaymentPlanId,
            AdvanceReceived = contract.AdvanceReceived,
            RetentionPercent = contract.RetentionPercent,
            RetentionReleased = contract.RetentionReleased,
            DefectsPeriodMonths = contract.DefectsPeriodMonths,
            HandoverId = contract.HandoverId,
            SnagInspectionId = contract.SnagInspectionId,
            DocumentUrl = null,
            ClientPortalEnabled = contract.ClientPortalEnabled,
            Notes = contract.Notes,

            ScopeItems = contract.ScopeItems.OrderBy(s => s.SortOrder).Select(s => new ContractScopeItemDto
            {
                Id = s.Id,
                Category = s.Category,
                Description = s.Description ?? string.Empty,
                IsIncluded = s.IsIncluded,
                Value = s.Value,
                Note = s.Note,
                SortOrder = s.SortOrder,
            }).ToList(),

            ClientSuppliedMaterials = contract.ClientSuppliedMaterials.Select(m => new ClientSuppliedMaterialDto
            {
                Id = m.Id,
                ItemId = m.ItemId,
                Description = m.Description ?? string.Empty,
                Uom = m.Uom,
                AgreedQuantity = m.AgreedQuantity,
                ReceivedQuantity = m.ReceivedQuantity,
                ConsumedQuantity = m.ConsumedQuantity,
                OutstandingQuantity = RealEstateMapper.Money(Math.Max(0m, m.AgreedQuantity - m.ReceivedQuantity), 4),
                EstimatedValue = m.EstimatedValue,
                RateExclusionAmount = m.RateExclusionAmount,
                ExpectedBy = m.ExpectedBy,
                LastReceivedOn = m.LastReceivedOn,
                IsDelayingWork = m.IsDelayingWork,

                // Client-supplied material that has not arrived is the commonest cause of delay on
                // a turnkey job, and the one the builder is least able to control.
                IsOverdue = m.ReceivedQuantity < m.AgreedQuantity && m.ExpectedBy is not null && m.ExpectedBy < today,

                Note = m.Note,
            }).ToList(),
        };

        if (contract.CoClientPartyId is not null || contract.ArchitectPartyId is not null)
        {
            var names = await PartyNamesAsync(
                new[] { contract.CoClientPartyId, contract.ArchitectPartyId }
                    .Where(i => i.HasValue).Select(i => i!.Value));

            detail.CoClientName = contract.CoClientPartyId is null ? null : names.GetValueOrDefault(contract.CoClientPartyId.Value);
            detail.ArchitectName = contract.ArchitectPartyId is null ? null : names.GetValueOrDefault(contract.ArchitectPartyId.Value);
        }

        detail.Variations = await MapClientVariationsAsync(
            await Db.ClientVariations.ForCompany(Tenant)
                .Where(v => v.ClientBuildContractId == id)
                .OrderByDescending(v => v.RequestedOn)
                .ToListAsync());

        if (contract.SpecificationScheduleId is not null)
        {
            detail.Specification = (await GetSpecificationsAsync(null, id)).FirstOrDefault();
        }

        if (contract.ConstructionProjectId is not null)
        {
            detail.Certificates = await MapIpcListAsync(
                await Db.InterimPaymentCertificates.ForCompany(Tenant)
                    .Where(c => c.ClientBuildContractId == id)
                    .OrderByDescending(c => c.IssuedOn)
                    .ToListAsync());
        }

        detail.Drawings = await GetDrawingsAsync(id, contract.ConstructionProjectId);

        var costSheet = await Db.ContractCostSheets.ForCompany(Tenant)
            .Include(s => s.Lines)
            .Where(s => s.ClientBuildContractId == id)
            .OrderByDescending(s => s.AsOfDate)
            .FirstOrDefaultAsync();

        if (costSheet is not null) detail.CostSheet = await MapCostSheetAsync(costSheet);

        return detail;
    }

    public async Task<ClientBuildContractDetailDto> SaveClientBuildAsync(ClientBuildContractUpsertDto dto, Guid userId)
    {
        var settings = await SettingsAsync();

        var contract = dto.Id is not null && dto.Id != Guid.Empty
            ? await Db.ClientBuildContracts.ForCompany(Tenant)
                .Include(c => c.ScopeItems)
                .Include(c => c.ClientSuppliedMaterials)
                .FirstOrDefaultAsync(c => c.Id == dto.Id)
            : null;

        var clientPartyId = dto.ClientPartyId ?? Guid.Empty;

        if (clientPartyId == Guid.Empty && dto.NewClient is not null)
        {
            var party = new Party
            {
                Reference = await numbering.NextPartyReferenceAsync(),
                Kind = dto.NewClient.Kind,
                FirstName = dto.NewClient.FirstName,
                LastName = dto.NewClient.LastName,
                DisplayName = $"{dto.NewClient.FirstName} {dto.NewClient.LastName}".Trim(),
                FatherOrGuardianName = dto.NewClient.FatherOrGuardianName,
                PrimaryPhone = dto.NewClient.PrimaryPhone,
                PrimaryEmail = dto.NewClient.PrimaryEmail,
                PreferredChannel = dto.NewClient.PreferredChannel,
                PreferredLanguage = dto.NewClient.PreferredLanguage,
            }.StampNew(Tenant, userId);

            Db.Parties.Add(party);
            await Db.SaveChangesAsync();

            clientPartyId = party.Id;
        }

        if (clientPartyId == Guid.Empty)
            throw new InvalidOperationException("Name the client, or supply enough detail to create them.");

        if (contract is null)
        {
            contract = new ClientBuildContract
            {
                Reference = await numbering.NextClientBuildNumberAsync(DateTime.UtcNow),
                ClientPartyId = clientPartyId,
                EnquiryId = dto.EnquiryId,
                EstimateId = dto.EstimateId,
                OfficeId = dto.OfficeId,
            }.StampNew(Tenant, userId);

            Db.ClientBuildContracts.Add(contract);

            var hasRole = await Db.PartyRoles.ForCompany(Tenant)
                .AnyAsync(r => r.PartyId == clientPartyId && r.Kind == PartyRoleKind.Client && r.IsActive);

            if (!hasRole)
            {
                Db.PartyRoles.Add(new PartyRole
                {
                    PartyId = clientPartyId,
                    Kind = PartyRoleKind.Client,
                    FromDate = Today,
                    IsActive = true,
                    ContextType = "ClientBuildContract",
                    ContextId = contract.Id,
                }.StampNew(Tenant, userId));
            }
        }
        else
        {
            // Once signed, the price and the specification are the contract. Everything after is
            // a variation the client approves, which is the whole discipline of turnkey work.
            if (contract.SignedOn is not null && contract.ContractValue != (dto.ContractValue ?? contract.ContractValue))
                throw new InvalidOperationException("This contract is signed. Raise a client variation rather than changing the value.");

            contract.StampUpdated(userId);
        }

        contract.Name = dto.Name;
        contract.CoClientPartyId = dto.CoClientPartyId;
        contract.PropertyId = dto.PropertyId;
        contract.LandParcelId = dto.LandParcelId;
        contract.SourceBookingId = dto.SourceBookingId;
        contract.SiteAddress = dto.SiteAddress;
        contract.PlotAreaSqFt = RealEstateMapper.ToSquareFeet(dto.PlotArea, dto.InputAreaUnit);
        contract.CoveredAreaSqFt = RealEstateMapper.ToSquareFeet(dto.CoveredArea, dto.InputAreaUnit);
        contract.Kind = dto.Kind;
        contract.Grade = dto.Grade;
        contract.SpecificationScheduleId = dto.SpecificationScheduleId;
        contract.RatePerSqFt = dto.RatePerSqFt;
        contract.FeePercent = dto.FeePercent;
        contract.FixedFee = dto.FixedFee;
        contract.GuaranteedMaximumPrice = dto.GuaranteedMaximumPrice;
        contract.CurrencyCode = dto.CurrencyCode ?? settings.CurrencyCode;
        contract.SignedOn = dto.SignedOn;
        contract.StartDate = dto.StartDate;
        contract.PlannedCompletionDate = dto.PlannedCompletionDate;
        contract.ForecastCompletionDate ??= dto.PlannedCompletionDate;
        contract.LiquidatedDamagesPerDay = dto.LiquidatedDamagesPerDay;
        contract.LiquidatedDamagesCapPercent = dto.LiquidatedDamagesCapPercent;
        contract.RetentionPercent = dto.RetentionPercent;
        contract.DefectsPeriodMonths = dto.DefectsPeriodMonths;
        contract.ProjectManagerUserId = dto.ProjectManagerUserId;
        contract.ArchitectPartyId = dto.ArchitectPartyId;
        contract.ClientPortalEnabled = dto.ClientPortalEnabled;
        contract.Notes = dto.Notes;

        // A per-area contract's value is rate times area. Letting both be typed guarantees they
        // eventually disagree, and the client will have the one that favours them.
        contract.ContractValue = dto.Kind == ContractKind.PerAreaUnitRate && contract.CoveredAreaSqFt > 0m
            ? RealEstateMapper.Money(contract.CoveredAreaSqFt * dto.RatePerSqFt)
            : dto.ContractValue ?? contract.ContractValue;

        contract.RevisedContractValue = RealEstateMapper.Money(contract.ContractValue + contract.ApprovedVariations);

        if (dto.ScopeItems.Count > 0)
        {
            Db.ContractScopeItems.RemoveRange(contract.ScopeItems);

            var order = 0;

            foreach (var s in dto.ScopeItems)
            {
                contract.ScopeItems.Add(new ContractScopeItem
                {
                    Category = s.Category,
                    Description = s.Description,
                    IsIncluded = s.IsIncluded,
                    Value = s.Value,
                    Note = s.Note,
                    SortOrder = order += 10,
                }.StampNew(Tenant, userId));
            }
        }

        if (dto.ClientSuppliedMaterials.Count > 0)
        {
            Db.ClientSuppliedMaterials.RemoveRange(contract.ClientSuppliedMaterials);

            foreach (var m in dto.ClientSuppliedMaterials)
            {
                contract.ClientSuppliedMaterials.Add(new ClientSuppliedMaterial
                {
                    ItemId = m.ItemId,
                    Description = m.Description,
                    Uom = m.Uom,
                    AgreedQuantity = m.AgreedQuantity,
                    ReceivedQuantity = m.ReceivedQuantity,
                    EstimatedValue = m.EstimatedValue,

                    // What comes out of our rate because the client is supplying it. Forgetting
                    // this is how a builder charges for cement the client already paid for.
                    RateExclusionAmount = m.RateExclusionAmount,

                    ExpectedBy = m.ExpectedBy,
                    Note = m.Note,
                }.StampNew(Tenant, userId));
            }
        }

        await Db.SaveChangesAsync();

        // Signing opens the construction project, so site work has somewhere to be recorded from
        // day one rather than being retrofitted a month in.
        if (contract.SignedOn is not null && contract.ConstructionProjectId is null)
        {
            var project = new ConstructionProject
            {
                Code = await numbering.NextMasterCodeAsync(Db.ConstructionProjects, "CPR"),
                Name = contract.Name,
                ClientBuildContractId = contract.Id,
                PropertyId = contract.PropertyId,
                Status = ProjectStatus.Planning,
                StartDate = contract.StartDate,
                PlannedCompletionDate = contract.PlannedCompletionDate,
                ForecastCompletionDate = contract.PlannedCompletionDate,
                ContractValue = contract.ContractValue,
                RevisedContractValue = contract.RevisedContractValue,
                CurrencyCode = contract.CurrencyCode,
                ProjectManagerUserId = contract.ProjectManagerUserId,
                DefectsPeriodMonths = contract.DefectsPeriodMonths,
            }.StampNew(Tenant, userId);

            Db.ConstructionProjects.Add(project);
            await Db.SaveChangesAsync();

            contract.ConstructionProjectId = project.Id;
            await Db.SaveChangesAsync();
        }

        return (await GetClientBuildAsync(contract.Id))!;
    }

    public async Task<ClientBuildContractDetailDto> ChangeClientBuildStatusAsync(Guid id, string status, Guid userId)
    {
        var contract = await RequireAsync<ClientBuildContract>(id, "That contract does not exist.");

        var today = Today;

        if (status == "Completed")
        {
            if (contract.ConstructionProjectId is not null)
            {
                var openSnags = await Db.SnagInspections.ForCompany(Tenant)
                    .Where(i => i.ClientBuildContractId == id && i.BlocksHandover)
                    .CountAsync();

                if (openSnags > 0)
                    throw new InvalidOperationException($"{openSnags} inspections still block handover. Close the critical snags first.");
            }

            contract.ActualCompletionDate ??= today;
        }

        contract.Status = status;
        contract.StampUpdated(userId);

        await Db.SaveChangesAsync();
        return (await GetClientBuildAsync(id))!;
    }

    // ═══ Client variations ═══════════════════════════════════════════════════

    public async Task<ClientVariationDto> SaveClientVariationAsync(ClientVariationUpsertDto dto, Guid userId)
    {
        var contract = await RequireAsync<ClientBuildContract>(dto.ClientBuildContractId, "That contract does not exist.");

        var variation = dto.Id is not null && dto.Id != Guid.Empty
            ? await Db.ClientVariations.ForCompany(Tenant).FirstOrDefaultAsync(v => v.Id == dto.Id)
            : null;

        if (variation is null)
        {
            variation = new ClientVariation
            {
                Reference = await numbering.NextVariationNumberAsync(DateTime.UtcNow),
                ClientBuildContractId = dto.ClientBuildContractId,
                RequestedOn = dto.RequestedOn == default ? Today : dto.RequestedOn,
                RequestedByPartyId = dto.RequestedByPartyId ?? contract.ClientPartyId,
                RaisedViaPortal = dto.RaisedViaPortal,
            }.StampNew(Tenant, userId);

            Db.ClientVariations.Add(variation);
        }
        else
        {
            if (variation.ClientApproved)
                throw new InvalidOperationException("This variation has been approved by the client and cannot be re-quoted.");

            variation.StampUpdated(userId);
        }

        variation.SpecificationItemId = dto.SpecificationItemId;
        variation.Title = dto.Title;
        variation.Description = dto.Description;
        variation.Origin = dto.Origin;
        variation.QuotedAmount = dto.QuotedAmount;
        variation.OmissionCredit = dto.OmissionCredit;
        variation.NetAmount = RealEstateMapper.Money(dto.QuotedAmount - (dto.OmissionCredit ?? 0m));
        variation.TimeImpactDays = dto.TimeImpactDays;

        if (dto.QuotedAmount > 0m || dto.OmissionCredit > 0m)
        {
            variation.QuotedOn = Today;
            variation.Status = VariationStatus.Quoted;

            // A quote with no expiry is a quote a client accepts nine months later at last year's
            // material prices. Thirty days is the norm and it is set rather than left blank.
            variation.QuoteValidUntil = dto.QuoteValidUntil ?? Today.AddDays(30);
        }

        await Db.SaveChangesAsync();

        if (variation.Status == VariationStatus.Quoted && variation.RaisedViaPortal)
        {
            await QueueNotificationAsync(
                "ClientVariationQuoted",
                $"Your change request has been priced — {variation.Reference}",
                $"{variation.Title}: {variation.NetAmount:N0}" +
                (variation.TimeImpactDays > 0 ? $", adding {variation.TimeImpactDays} days." : ".") +
                $" This quote holds until {variation.QuoteValidUntil:dd MMM yyyy}.",
                $"/realestate/client-variations/{variation.Id}",
                recipientPartyId: contract.ClientPartyId,
                entityType: "ClientVariation",
                entityId: variation.Id);

            await Db.SaveChangesAsync();
        }

        return (await MapClientVariationsAsync([variation]))[0];
    }

    /// <summary>
    /// The client's decision. Approval is what makes the change billable and what moves the
    /// contract value — and it needs evidence, because "they told me on site" is not something
    /// that survives a final-account meeting eighteen months later.
    /// </summary>
    public async Task<ClientVariationDto> DecideClientVariationAsync(
        Guid id, bool approved, string? reason, string? evidenceUrl, Guid userId)
    {
        var variation = await RequireAsync<ClientVariation>(id, "That variation does not exist.");

        if (variation.ClientApproved)
            throw new InvalidOperationException($"This variation was approved on {variation.ApprovedOn:dd MMM yyyy}.");

        var contract = await RequireAsync<ClientBuildContract>(variation.ClientBuildContractId, "The contract is missing.");
        var today = Today;

        if (!approved)
        {
            if (string.IsNullOrWhiteSpace(reason))
                throw new InvalidOperationException("A declined variation has to record the client's reason.");

            variation.Status = VariationStatus.Rejected;
            variation.RejectionReason = reason;
            variation.StampUpdated(userId);

            await Db.SaveChangesAsync();
            return (await MapClientVariationsAsync([variation]))[0];
        }

        if (variation.QuoteValidUntil is not null && variation.QuoteValidUntil < today)
            throw new InvalidOperationException($"This quote expired on {variation.QuoteValidUntil:dd MMM yyyy}. Re-price it before the client approves.");

        if (string.IsNullOrWhiteSpace(evidenceUrl) && variation.SignatureSessionId is null && !variation.RaisedViaPortal)
        {
            throw new InvalidOperationException(
                "A client approval needs evidence — a signed instruction, a portal acceptance, or an attached document. " +
                "Without it the variation is not collectable.");
        }

        await using var transaction = await Db.Database.BeginTransactionAsync();

        variation.ClientApproved = true;
        variation.ApprovedOn = today;
        variation.ApprovalEvidenceUrl = evidenceUrl;
        variation.Status = VariationStatus.Approved;
        variation.StampUpdated(userId);

        contract.ApprovedVariations = RealEstateMapper.Money(contract.ApprovedVariations + variation.NetAmount);
        contract.RevisedContractValue = RealEstateMapper.Money(contract.ContractValue + contract.ApprovedVariations);

        if (variation.TimeImpactDays > 0)
        {
            contract.ExtensionDaysGranted += variation.TimeImpactDays;

            if (contract.ForecastCompletionDate is not null)
                contract.ForecastCompletionDate = contract.ForecastCompletionDate.Value.AddDays(variation.TimeImpactDays);
        }

        contract.StampUpdated(userId);

        // The specification item it changes is updated, so the frozen spec and the built reality
        // stay the same document.
        if (variation.SpecificationItemId is not null)
        {
            var item = await Db.SpecificationItems.ForCompany(Tenant)
                .FirstOrDefaultAsync(i => i.Id == variation.SpecificationItemId);

            if (item is not null)
            {
                item.ClientVariationId = variation.Id;
                item.UpgradeCost = variation.NetAmount;
                item.StampUpdated(userId);
            }
        }

        // The construction side gets its own variation order so the extra work is measurable.
        if (contract.ConstructionProjectId is not null && variation.VariationOrderId is null)
        {
            var order = new VariationOrder
            {
                VariationNumber = await numbering.NextVariationNumberAsync(DateTime.UtcNow),
                ConstructionProjectId = contract.ConstructionProjectId.Value,
                ClientBuildContractId = contract.Id,
                Origin = variation.Origin,
                Status = VariationStatus.Approved,
                Title = variation.Title,
                Description = variation.Description,
                Justification = $"Client variation {variation.Reference}, approved {today:dd MMM yyyy}.",
                RaisedOn = variation.RequestedOn,
                RaisedByUserId = userId,
                AdditionAmount = variation.QuotedAmount,
                OmissionAmount = variation.OmissionCredit ?? 0m,
                NetAmount = variation.NetAmount,
                TimeImpactDays = variation.TimeImpactDays,
                ApprovedOn = today,
                ApprovedByPartyId = contract.ClientPartyId,
                ClientApproved = true,
                RevisedContractValue = contract.RevisedContractValue,
            }.StampNew(Tenant, userId);

            Db.VariationOrders.Add(order);
            await Db.SaveChangesAsync();

            variation.VariationOrderId = order.Id;
        }

        await WriteAuditNoteAsync(
            "ClientVariation", id, "ClientVariationApproved", Guid.Empty, userId,
            amountImpact: variation.NetAmount,
            before: contract.ContractValue.ToString("N0"),
            after: contract.RevisedContractValue.ToString("N0"),
            note: variation.Title,
            entityReference: variation.Reference);

        await Db.SaveChangesAsync();
        await transaction.CommitAsync();

        return (await MapClientVariationsAsync([variation]))[0];
    }

    public async Task<PaginatedResponse<ClientVariationDto>> GetClientVariationsAsync(
        ListQueryDto query, Guid? contractId, VariationStatus? status)
    {
        var q = Db.ClientVariations.ForCompany(Tenant)
            .WhereIf(contractId.HasValue, v => v.ClientBuildContractId == contractId)
            .WhereIf(status.HasValue, v => v.Status == status)
            .WhereIf(!string.IsNullOrWhiteSpace(query.Search),
                v => v.Reference.Contains(query.Search!) || v.Title.Contains(query.Search!))
            .OrderByDescending(v => v.RequestedOn);

        return await PageAsync(q, query, MapClientVariationsAsync);
    }

    private async Task<List<ClientVariationDto>> MapClientVariationsAsync(List<ClientVariation> variations)
    {
        if (variations.Count == 0) return [];

        var today = Today;
        var currency = await CurrencyAsync();

        var contractIds = variations.Select(v => v.ClientBuildContractId).Distinct().ToList();

        var contracts = await Db.ClientBuildContracts.ForCompany(Tenant)
            .Where(c => contractIds.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, c => c.Reference);

        var requesters = await PartyNamesAsync(
            variations.Where(v => v.RequestedByPartyId.HasValue).Select(v => v.RequestedByPartyId!.Value));

        var itemIds = variations.Where(v => v.SpecificationItemId.HasValue)
            .Select(v => v.SpecificationItemId!.Value).Distinct().ToList();

        var items = itemIds.Count == 0
            ? []
            : await Db.SpecificationItems.ForCompany(Tenant)
                .Where(i => itemIds.Contains(i.Id))
                .ToDictionaryAsync(i => i.Id, i => i.ItemName);

        return variations.Select(v => new ClientVariationDto
        {
            Id = v.Id,
            Reference = v.Reference,
            ClientBuildContractId = v.ClientBuildContractId,
            ContractReference = contracts.GetValueOrDefault(v.ClientBuildContractId),
            VariationOrderId = v.VariationOrderId,
            SpecificationItemId = v.SpecificationItemId,
            SpecificationItemName = v.SpecificationItemId is null ? null : items.GetValueOrDefault(v.SpecificationItemId.Value),
            Title = v.Title,
            Description = v.Description ?? string.Empty,
            Origin = v.Origin,
            Status = v.Status,
            RequestedOn = v.RequestedOn,
            RequestedByName = v.RequestedByPartyId is null ? null : requesters.GetValueOrDefault(v.RequestedByPartyId.Value),
            RaisedViaPortal = v.RaisedViaPortal,
            QuotedAmount = v.QuotedAmount,
            OmissionCredit = v.OmissionCredit,
            NetAmount = v.NetAmount,
            TimeImpactDays = v.TimeImpactDays,
            CurrencyCode = currency,
            QuotedOn = v.QuotedOn,
            QuoteValidUntil = v.QuoteValidUntil,
            QuoteExpired = !v.ClientApproved && v.QuoteValidUntil is not null && v.QuoteValidUntil < today,
            ClientApproved = v.ClientApproved,
            ApprovedOn = v.ApprovedOn,
            ApprovalEvidenceUrl = v.ApprovalEvidenceUrl,
            RejectionReason = v.RejectionReason,
            IsExecuted = v.IsExecuted,
            ExecutedOn = v.ExecutedOn,
            IsBilled = v.IsBilled,
            DemandId = v.DemandId,
            BeforeAfterPhotoUrls = string.IsNullOrWhiteSpace(v.BeforeAfterPhotoUrls)
                ? []
                : v.BeforeAfterPhotoUrls.Split('\n', StringSplitOptions.RemoveEmptyEntries).ToList(),
        }).ToList();
    }

    // ═══ Contract cost sheet ═════════════════════════════════════════════════

    public async Task<ContractCostSheetDto> GetContractCostSheetAsync(Guid contractId, DateOnly? asOf)
    {
        var sheet = await Db.ContractCostSheets.ForCompany(Tenant)
            .Include(s => s.Lines)
            .Where(s => s.ClientBuildContractId == contractId)
            .WhereIf(asOf.HasValue, s => s.AsOfDate <= asOf)
            .OrderByDescending(s => s.AsOfDate)
            .FirstOrDefaultAsync();

        if (sheet is null)
            return await RecalculateCostSheetAsync(contractId, Guid.Empty);

        return await MapCostSheetAsync(sheet);
    }

    /// <summary>
    /// Rebuilds the cost sheet and attributes the margin erosion. This is the point of the whole
    /// module: a builder who knows their margin fell eight points knows nothing useful, but one
    /// who knows five of those points came from absorbed variations and two from wastage on one
    /// trade knows exactly what to change on the next contract.
    /// </summary>
    public async Task<ContractCostSheetDto> RecalculateCostSheetAsync(Guid contractId, Guid userId)
    {
        var contract = await RequireAsync<ClientBuildContract>(contractId, "That contract does not exist.");
        var today = Today;

        var revised = RealEstateMapper.Money(contract.ContractValue + contract.ApprovedVariations);

        var nodes = contract.ConstructionProjectId is null
            ? []
            : await Db.WbsNodes.ForCompany(Tenant)
                .Where(n => n.ConstructionProjectId == contract.ConstructionProjectId)
                .ToListAsync();

        var budget = nodes.Count > 0 ? nodes.Sum(n => n.BudgetAmount) : contract.BudgetCost;
        var committed = nodes.Sum(n => n.CommittedAmount);
        var actual = nodes.Count > 0 ? nodes.Sum(n => n.ActualAmount) : contract.ActualCost;

        var progress = contract.ProgressPercent;

        var costToComplete = progress > 0m && progress < 100m
            ? RealEstateMapper.Money(Math.Max(0m, (actual / (progress / 100m)) - actual))
            : RealEstateMapper.Money(Math.Max(0m, budget - actual));

        var forecast = RealEstateMapper.Money(actual + costToComplete);

        var budgetMargin = RealEstateMapper.Money(contract.ContractValue - budget);
        var forecastMargin = RealEstateMapper.Money(revised - forecast);
        var erosion = RealEstateMapper.Money(budgetMargin - forecastMargin);

        var sheet = new ContractCostSheet
        {
            ClientBuildContractId = contractId,
            ConstructionProjectId = contract.ConstructionProjectId,
            AsOfDate = today,
            ContractValue = contract.ContractValue,
            VariationsApproved = contract.ApprovedVariations,
            RevisedValue = revised,
            BudgetCost = RealEstateMapper.Money(budget),
            CommittedCost = RealEstateMapper.Money(committed),
            ActualCost = RealEstateMapper.Money(actual),
            CostToComplete = costToComplete,
            ForecastFinalCost = forecast,
            BudgetMargin = budgetMargin,
            ForecastMargin = forecastMargin,
            MarginPercent = RealEstateMapper.Percent(forecastMargin, revised),
            MarginErosion = erosion,
            ProgressPercent = progress,
            CertifiedValue = contract.TotalDemanded,
            CollectedValue = contract.TotalReceived,

            // Cash position: what we have collected against what we have spent. On a turnkey job
            // this is what determines whether the builder can pay next month's subcontractors.
            CashPosition = RealEstateMapper.Money(contract.TotalReceived - actual),

            PreparedByUserId = userId == Guid.Empty ? null : userId,
        }.StampNew(Tenant, userId);

        // Attribution. Each cause is measured from its own records rather than apportioned.
        if (contract.ConstructionProjectId is not null)
        {
            // Variations done but never approved by the client — work given away.
            var absorbed = await Db.ClientVariations.ForCompany(Tenant)
                .Where(v => v.ClientBuildContractId == contractId && v.IsExecuted && !v.ClientApproved)
                .SumAsync(v => v.QuotedAmount);

            sheet.ErosionFromVariationsAbsorbed = RealEstateMapper.Money(absorbed);

            var wastage = await Db.WastageRecords.ForCompany(Tenant)
                .Where(w => w.ConstructionProjectId == contract.ConstructionProjectId)
                .SumAsync(w => w.ExcessValue);

            sheet.ErosionFromWastage = RealEstateMapper.Money(wastage);

            // Rework: work orders raised against the contract that somebody had to redo.
            var rework = await Db.WorkOrders.ForCompany(Tenant)
                .Where(w => w.ProjectId == contract.ConstructionProjectId
                         && w.Source == WorkOrderSource.Snag
                         && w.CostBearer == CostBearer.Contractor)
                .SumAsync(w => w.TotalCost);

            sheet.ErosionFromRework = RealEstateMapper.Money(rework);

            // Delay: idle plant plus prolongation the client did not pay for.
            var idlePlant = await Db.PlantAllocations.ForCompany(Tenant)
                .Where(a => a.ConstructionProjectId == contract.ConstructionProjectId)
                .SumAsync(a => a.IdleHours * a.Rate);

            var prolongation = await Db.ExtensionOfTimes.ForCompany(Tenant)
                .Where(e => e.ClientBuildContractId == contractId && !e.ProlongationCostGranted && e.ProlongationCost != null)
                .SumAsync(e => e.ProlongationCost ?? 0m);

            sheet.ErosionFromDelay = RealEstateMapper.Money(idlePlant + prolongation);

            // Rate increase: what materials actually cost against what the BOQ assumed.
            var boqIds = await Db.BillsOfQuantities.ForCompany(Tenant)
                .Where(b => b.ConstructionProjectId == contract.ConstructionProjectId && b.IsCurrent)
                .Select(b => b.Id)
                .ToListAsync();

            if (boqIds.Count > 0)
            {
                var lines = await Db.BoqLines.ForCompany(Tenant)
                    .Where(l => boqIds.Contains(l.BillOfQuantitiesId) && l.ExecutedQuantity > 0m && l.BudgetCostRate > 0m)
                    .Select(l => new { l.ExecutedQuantity, l.BudgetCostRate, l.ActualCost })
                    .ToListAsync();

                var expected = lines.Sum(l => l.ExecutedQuantity * l.BudgetCostRate);
                var incurred = lines.Sum(l => l.ActualCost);

                sheet.ErosionFromRateIncrease = RealEstateMapper.Money(Math.Max(0m, incurred - expected));
            }
        }

        var attributed = sheet.ErosionFromVariationsAbsorbed + sheet.ErosionFromWastage
                       + sheet.ErosionFromRework + sheet.ErosionFromDelay + sheet.ErosionFromRateIncrease;

        // What is left is honestly unexplained rather than being forced into a bucket.
        sheet.ErosionOther = RealEstateMapper.Money(Math.Max(0m, erosion - attributed));

        Db.ContractCostSheets.Add(sheet);

        var order = 0;

        foreach (var node in nodes.Where(n => n.BudgetAmount > 0m).OrderBy(n => n.SortOrder))
        {
            var nodeForecast = node.ForecastAmount > 0m ? node.ForecastAmount : node.CommittedAmount + node.ActualAmount;

            sheet.Lines.Add(new ContractCostLine
            {
                WbsNodeId = node.Id,
                CostHead = node.Name,
                BudgetAmount = node.BudgetAmount,
                CommittedAmount = node.CommittedAmount,
                ActualAmount = node.ActualAmount,
                ForecastAmount = nodeForecast,
                VarianceAmount = RealEstateMapper.Money(nodeForecast - node.BudgetAmount),
                VariancePercent = RealEstateMapper.Percent(nodeForecast - node.BudgetAmount, node.BudgetAmount),
                SortOrder = order += 10,
            }.StampNew(Tenant, userId));
        }

        contract.BudgetCost = sheet.BudgetCost;
        contract.ActualCost = sheet.ActualCost;
        contract.ForecastFinalCost = sheet.ForecastFinalCost;
        contract.ForecastMargin = sheet.ForecastMargin;
        contract.RevisedContractValue = revised;
        contract.StampUpdated(userId);

        await Db.SaveChangesAsync();
        return await MapCostSheetAsync(sheet);
    }

    private async Task<ContractCostSheetDto> MapCostSheetAsync(ContractCostSheet s)
    {
        var currency = await CurrencyAsync();
        var users = await AgentUserNamesAsync([s.PreparedByUserId]);

        var reference = await Db.ClientBuildContracts.ForCompany(Tenant)
            .Where(c => c.Id == s.ClientBuildContractId)
            .Select(c => c.Reference)
            .FirstOrDefaultAsync();

        var nodeIds = s.Lines.Where(l => l.WbsNodeId.HasValue).Select(l => l.WbsNodeId!.Value).Distinct().ToList();

        return new ContractCostSheetDto
        {
            Id = s.Id,
            ClientBuildContractId = s.ClientBuildContractId,
            ContractReference = reference,
            ConstructionProjectId = s.ConstructionProjectId,
            AsOfDate = s.AsOfDate,
            CurrencyCode = currency,
            ContractValue = s.ContractValue,
            VariationsApproved = s.VariationsApproved,
            RevisedValue = s.RevisedValue,
            BudgetCost = s.BudgetCost,
            CommittedCost = s.CommittedCost,
            ActualCost = s.ActualCost,
            CostToComplete = s.CostToComplete,
            ForecastFinalCost = s.ForecastFinalCost,
            BudgetMargin = s.BudgetMargin,
            ForecastMargin = s.ForecastMargin,
            MarginPercent = s.MarginPercent,
            MarginErosion = s.MarginErosion,
            ErosionFromVariationsAbsorbed = s.ErosionFromVariationsAbsorbed,
            ErosionFromWastage = s.ErosionFromWastage,
            ErosionFromRework = s.ErosionFromRework,
            ErosionFromDelay = s.ErosionFromDelay,
            ErosionFromRateIncrease = s.ErosionFromRateIncrease,
            ErosionOther = s.ErosionOther,
            ProgressPercent = s.ProgressPercent,
            CertifiedValue = s.CertifiedValue,
            CollectedValue = s.CollectedValue,
            CashPosition = s.CashPosition,
            PreparedByName = s.PreparedByUserId is null ? null : users.GetValueOrDefault(s.PreparedByUserId.Value),
            Commentary = s.Commentary,

            Lines = s.Lines.OrderBy(l => l.SortOrder).Select(l => new ContractCostLineDto
            {
                Id = l.Id,
                WbsNodeId = l.WbsNodeId,
                CostHead = l.CostHead,
                Description = l.Description,
                BudgetAmount = l.BudgetAmount,
                CommittedAmount = l.CommittedAmount,
                ActualAmount = l.ActualAmount,
                ForecastAmount = l.ForecastAmount,
                VarianceAmount = l.VarianceAmount,
                VariancePercent = l.VariancePercent,
                VarianceExplanation = l.VarianceExplanation,
                IsOverrunning = l.VarianceAmount > 0m,
                SortOrder = l.SortOrder,
            }).ToList(),
        };
    }

    // ═══ Drawings ════════════════════════════════════════════════════════════

    public async Task<List<DrawingRegisterDto>> GetDrawingsAsync(Guid? clientBuildContractId, Guid? constructionProjectId)
    {
        var drawings = await Db.DrawingRegisters.ForCompany(Tenant)
            .Include(d => d.Revisions)
            .WhereIf(clientBuildContractId.HasValue, d => d.ClientBuildContractId == clientBuildContractId)
            .WhereIf(constructionProjectId.HasValue, d => d.ConstructionProjectId == constructionProjectId)
            .OrderBy(d => d.Discipline).ThenBy(d => d.DrawingNumber)
            .ToListAsync();

        if (drawings.Count == 0) return [];

        var preparers = await PartyNamesAsync(drawings.Where(d => d.PreparedByPartyId.HasValue).Select(d => d.PreparedByPartyId!.Value));

        var issuers = await AgentUserNamesAsync(drawings.SelectMany(d => d.Revisions).Select(r => r.IssuedByUserId));

        return drawings.Select(d => new DrawingRegisterDto
        {
            Id = d.Id,
            DrawingNumber = d.DrawingNumber,
            Title = d.Title,
            ClientBuildContractId = d.ClientBuildContractId,
            ConstructionProjectId = d.ConstructionProjectId,
            ProjectId = d.ProjectId,
            Discipline = d.Discipline,
            Scale = d.Scale,
            PreparedByName = d.PreparedByPartyId is null ? null : preparers.GetValueOrDefault(d.PreparedByPartyId.Value),
            CurrentRevision = d.CurrentRevision,
            CurrentRevisionDate = d.CurrentRevisionDate,
            Status = d.Status,
            ClientApproved = d.ClientApproved,
            ClientApprovedOn = d.ClientApprovedOn,
            IsFrozen = d.IsFrozen,
            CurrentFileUrl = d.CurrentFileUrl,
            IsIssuedToSite = d.IsIssuedToSite,
            IssuedToSiteOn = d.IssuedToSiteOn,
            RevisionCount = d.Revisions.Count,

            Revisions = d.Revisions.OrderByDescending(r => r.RevisionDate).Select(r => new DrawingRevisionDto
            {
                Id = r.Id,
                Revision = r.Revision,
                RevisionDate = r.RevisionDate,
                ChangeDescription = r.ChangeDescription,
                FileUrl = r.FileUrl,
                IssuedByName = r.IssuedByUserId is null ? null : issuers.GetValueOrDefault(r.IssuedByUserId.Value),
                IssuedOn = r.IssuedOn,
                IsSuperseded = r.IsSuperseded,
                HasCostImpact = r.HasCostImpact,
                VariationOrderId = r.VariationOrderId,
            }).ToList(),
        }).ToList();
    }

    public async Task<DrawingRegisterDto> SaveDrawingAsync(DrawingRegisterDto dto, Guid userId)
    {
        var drawing = dto.Id != Guid.Empty
            ? await Db.DrawingRegisters.ForCompany(Tenant).Include(d => d.Revisions).FirstOrDefaultAsync(d => d.Id == dto.Id)
            : null;

        if (drawing is null)
        {
            drawing = new DrawingRegister
            {
                DrawingNumber = dto.DrawingNumber,
                ClientBuildContractId = dto.ClientBuildContractId,
                ConstructionProjectId = dto.ConstructionProjectId,
                ProjectId = dto.ProjectId,
            }.StampNew(Tenant, userId);

            Db.DrawingRegisters.Add(drawing);
        }
        else
        {
            if (drawing.IsFrozen)
                throw new InvalidOperationException("This drawing is frozen. Issue a revision rather than editing it.");

            drawing.StampUpdated(userId);
        }

        drawing.Title = dto.Title;
        drawing.Discipline = dto.Discipline;
        drawing.Scale = dto.Scale;
        drawing.Status = dto.Status;
        drawing.CurrentFileUrl = dto.CurrentFileUrl;
        drawing.IsFrozen = dto.IsFrozen;

        if (dto.ClientApproved && !drawing.ClientApproved)
        {
            drawing.ClientApproved = true;
            drawing.ClientApprovedOn = Today;
        }

        // A drawing on site that the client has not approved is a rebuild waiting to happen.
        if (dto.IsIssuedToSite && !drawing.IsIssuedToSite)
        {
            if (!drawing.ClientApproved && drawing.ClientBuildContractId is not null)
                throw new InvalidOperationException("This drawing has not been approved by the client and cannot be issued to site.");

            drawing.IsIssuedToSite = true;
            drawing.IssuedToSiteOn = Today;
        }

        await Db.SaveChangesAsync();
        return (await GetDrawingsAsync(drawing.ClientBuildContractId, drawing.ConstructionProjectId))
            .First(d => d.Id == drawing.Id);
    }

    /// <summary>
    /// Issues a new revision. The previous one is superseded rather than replaced, because a
    /// wall built to revision C is a fact and revision D does not change what happened.
    /// </summary>
    public async Task<DrawingRegisterDto> AddRevisionAsync(Guid drawingId, DrawingRevisionDto dto, Guid userId)
    {
        var drawing = await Db.DrawingRegisters.ForCompany(Tenant)
            .Include(d => d.Revisions)
            .FirstOrDefaultAsync(d => d.Id == drawingId)
            ?? throw new InvalidOperationException("That drawing does not exist.");

        if (string.IsNullOrWhiteSpace(dto.ChangeDescription))
            throw new InvalidOperationException("A revision has to say what changed — site will need to know what to look at.");

        foreach (var previous in drawing.Revisions.Where(r => !r.IsSuperseded))
        {
            previous.IsSuperseded = true;
            previous.StampUpdated(userId);
        }

        var revision = new DrawingRevision
        {
            DrawingRegisterId = drawingId,
            Revision = dto.Revision,
            RevisionDate = dto.RevisionDate == default ? Today : dto.RevisionDate,
            ChangeDescription = dto.ChangeDescription,
            FileUrl = dto.FileUrl,
            IssuedByUserId = userId,
            IssuedOn = dto.IssuedOn,
            HasCostImpact = dto.HasCostImpact,
            VariationOrderId = dto.VariationOrderId,
        }.StampNew(Tenant, userId);

        Db.DrawingRevisions.Add(revision);

        drawing.CurrentRevision = revision.Revision;
        drawing.CurrentRevisionDate = revision.RevisionDate;
        drawing.CurrentFileUrl = revision.FileUrl ?? drawing.CurrentFileUrl;

        // A revision resets both approvals. Site cannot keep building to the old sheet, and the
        // client has not yet seen the new one.
        drawing.ClientApproved = false;
        drawing.ClientApprovedOn = null;
        drawing.IsIssuedToSite = false;
        drawing.StampUpdated(userId);

        if (dto.HasCostImpact && dto.VariationOrderId is null && drawing.ClientBuildContractId is not null)
        {
            await WriteAuditNoteAsync(
                "DrawingRegister", drawingId, "RevisionWithUnpricedCostImpact", Guid.Empty, userId,
                note: $"Revision {revision.Revision} carries a cost impact with no variation raised against it.",
                entityReference: drawing.DrawingNumber,
                highRisk: true);
        }

        await Db.SaveChangesAsync();
        return (await GetDrawingsAsync(drawing.ClientBuildContractId, drawing.ConstructionProjectId))
            .First(d => d.Id == drawingId);
    }

    /// <summary>The client's own view of their build: progress, money, decisions and drawings.</summary>
    public async Task<CustomerPortalHomeDto> GetClientPortalHomeAsync(Guid partyId)
    {
        var names = await PartyNamesAsync([partyId]);
        var currency = await CurrencyAsync();
        var today = Today;

        var contracts = await Db.ClientBuildContracts.ForCompany(Tenant)
            .Where(c => c.ClientPartyId == partyId || c.CoClientPartyId == partyId)
            .Where(c => c.ClientPortalEnabled)
            .ToListAsync();

        var home = new CustomerPortalHomeDto
        {
            CustomerName = names.GetValueOrDefault(partyId, "—"),
            CurrencyCode = currency,
        };

        if (contracts.Count == 0) return home;

        var ids = contracts.Select(c => c.Id).ToList();

        home.TotalInvested = RealEstateMapper.Money(contracts.Sum(c => c.RevisedContractValue));
        home.TotalPaid = RealEstateMapper.Money(contracts.Sum(c => c.TotalReceived));
        home.TotalOutstanding = RealEstateMapper.Money(contracts.Sum(c => c.Outstanding));

        home.Builds = await MapClientBuildListAsync(contracts);

        // Pending decisions and priced quotes waiting on the client — the two things the portal
        // exists to unblock, surfaced as the unread count the home screen leads with.
        var pending = await Db.ClientVariations.ForCompany(Tenant)
            .CountAsync(v => ids.Contains(v.ClientBuildContractId)
                          && v.Status == VariationStatus.Quoted
                          && !v.ClientApproved);

        var specIds = contracts.Where(c => c.SpecificationScheduleId.HasValue)
            .Select(c => c.SpecificationScheduleId!.Value).ToList();

        if (specIds.Count > 0)
        {
            pending += await Db.SpecificationItems.ForCompany(Tenant)
                .CountAsync(i => specIds.Contains(i.SpecificationScheduleId)
                              && i.IsClientSelectable
                              && (i.SelectedOption == null || i.SelectedOption == ""));
        }

        home.UnreadMessages = pending;

        home.ConstructionProgress = contracts
            .Where(c => c.ConstructionProjectId is not null)
            .Select(c => new ProjectMilestoneDto
            {
                ProjectId = c.ConstructionProjectId!.Value,
                Name = c.Name,
                ProgressPercent = c.ProgressPercent,
                PlannedDate = c.PlannedCompletionDate,
                ForecastDate = c.ForecastCompletionDate,
                Status = c.ActualCompletionDate is not null ? MilestoneStatus.Certified : MilestoneStatus.InProgress,
            })
            .ToList();

        return home;
    }
}
