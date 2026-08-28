using Microsoft.EntityFrameworkCore;
using Nexcore.SharedKernel.Api;
using RealEstate.Application.DTOs;
using RealEstate.Domain.Entities;
using RealEstate.Domain.Enums;
using RealEstate.Infrastructure.Persistence;

namespace RealEstate.Infrastructure.Services;

/// <summary>
/// Portal accounts and the customer's own view of what they have bought.
///
/// The portal earns its keep by removing telephone calls. A buyer rings up to ask three things —
/// what do I owe, when is it due, and how far along is the building — so the home screen answers
/// exactly those three before anything else, from the same figures the office sees. A portal that
/// shows a number the accounts department disagrees with generates more calls than it saves.
/// </summary>
public partial class CommunicationService
{
    public async Task<PaginatedResponse<PortalUserDto>> GetPortalUsersAsync(ListQueryDto query, PortalAudience? audience)
    {
        var q = Db.PortalUsers.ForCompany(Tenant)
            .WhereIf(audience.HasValue, u => u.Audience == audience)
            .WhereIf(!string.IsNullOrWhiteSpace(query.Search),
                u => u.DisplayName.Contains(query.Search!)
                  || u.LoginIdentifier.Contains(query.Search!)
                  || (u.Email ?? "").Contains(query.Search!)
                  || (u.Phone ?? "").Contains(query.Search!))
            .OrderBy(u => u.Audience)
            .ThenBy(u => u.DisplayName);

        return await PageAsync(q, query, (PortalUser u) => MapPortalUser(u));
    }

    private static PortalUserDto MapPortalUser(PortalUser u) => new()
    {
        Id = u.Id,
        Audience = u.Audience,
        PartyId = u.PartyId,
        LandlordId = u.LandlordId,
        ChannelPartnerId = u.ChannelPartnerId,
        ResidentId = u.ResidentId,
        LoginIdentifier = u.LoginIdentifier,
        DisplayName = u.DisplayName,
        Email = u.Email,
        Phone = u.Phone,
        IsEmailVerified = u.IsEmailVerified,
        IsPhoneVerified = u.IsPhoneVerified,
        IsActive = u.IsActive,
        MustChangePassword = u.MustChangePassword,
        LastLoginAt = u.LastLoginAt,
        InvitedAt = u.InvitedAt,
        ActivatedAt = u.ActivatedAt,
        IsLocked = u.LockedUntil is not null && u.LockedUntil > DateTime.UtcNow,
        PreferredLanguage = u.PreferredLanguage,
        NotifyByEmail = u.NotifyByEmail,
        NotifyBySms = u.NotifyBySms,
        NotifyByWhatsApp = u.NotifyByWhatsApp,
        NotifyByPush = u.NotifyByPush,
    };

    public async Task<PortalUserDto> SavePortalUserAsync(PortalUserDto dto, Guid userId)
    {
        var isNew = dto.Id == Guid.Empty;

        var user = isNew
            ? new PortalUser()
            : await Db.PortalUsers.ForCompany(Tenant).FirstOrDefaultAsync(u => u.Id == dto.Id)
              ?? throw new InvalidOperationException("That portal account does not exist.");

        if (string.IsNullOrWhiteSpace(dto.LoginIdentifier))
            throw new InvalidOperationException("A portal account needs a login — an email address or a phone number.");

        var identifier = dto.LoginIdentifier.Trim().ToLowerInvariant();

        var duplicate = await Db.PortalUsers.ForCompany(Tenant)
            .AnyAsync(u => u.LoginIdentifier == identifier && u.Id != dto.Id);

        if (duplicate)
            throw new InvalidOperationException($"“{identifier}” is already in use by another portal account.");

        // Every audience has to point at the record it represents, or the portal cannot work out
        // whose data to show once somebody logs in.
        var linked = dto.Audience switch
        {
            PortalAudience.Customer or PortalAudience.Tenant => dto.PartyId is not null,
            PortalAudience.Owner => dto.LandlordId is not null,
            PortalAudience.Partner => dto.ChannelPartnerId is not null,
            PortalAudience.Resident => dto.ResidentId is not null,
            _ => false,
        };

        if (!linked)
            throw new InvalidOperationException(
                $"A {dto.Audience} portal account has to be linked to the corresponding record.");

        user.Audience = dto.Audience;
        user.PartyId = dto.PartyId;
        user.LandlordId = dto.LandlordId;
        user.ChannelPartnerId = dto.ChannelPartnerId;
        user.ResidentId = dto.ResidentId;
        user.LoginIdentifier = identifier;
        user.DisplayName = dto.DisplayName;
        user.Email = dto.Email;
        user.Phone = dto.Phone;
        user.PreferredLanguage = dto.PreferredLanguage;
        user.NotifyByEmail = dto.NotifyByEmail;
        user.NotifyBySms = dto.NotifyBySms;
        user.NotifyByWhatsApp = dto.NotifyByWhatsApp;
        user.NotifyByPush = dto.NotifyByPush;
        user.IsActive = dto.IsActive;

        if (!dto.IsLocked && user.LockedUntil is not null) user.LockedUntil = null;

        if (isNew)
        {
            user.MustChangePassword = true;
            user.StampNew(Tenant, userId);
            Db.PortalUsers.Add(user);
        }
        else
        {
            user.StampUpdated(userId);
        }

        await Db.SaveChangesAsync();

        return MapPortalUser(user);
    }

    public async Task<PortalUserDto> InvitePortalUserAsync(Guid id, Guid userId)
    {
        var user = await RequireAsync<PortalUser>(id, "That portal account does not exist.");

        if (!user.IsActive)
            throw new InvalidOperationException("That account is deactivated. Reactivate it before inviting.");

        if (string.IsNullOrWhiteSpace(user.Email) && string.IsNullOrWhiteSpace(user.Phone))
            throw new InvalidOperationException(
                "There is nowhere to send the invitation. Add an email address or a phone number first.");

        user.InvitedAt = DateTime.UtcNow;
        user.MustChangePassword = true;
        user.FailedLoginCount = 0;
        user.LockedUntil = null;
        user.StampUpdated(userId);

        await QueueNotificationAsync(
            "portal.invited",
            "Your account is ready",
            $"An account has been created for you. Sign in with {user.LoginIdentifier} to see your statements, "
            + "payment schedule and documents.",
            "/portal",
            recipientPartyId: user.PartyId,
            entityType: nameof(PortalUser),
            entityId: user.Id);

        await Db.SaveChangesAsync();

        return MapPortalUser(user);
    }

    // ═══ The customer's home screen ══════════════════════════════════════════

    public async Task<CustomerPortalHomeDto> GetCustomerPortalHomeAsync(Guid partyId)
    {
        var currency = await CurrencyAsync();
        var unit = await AreaUnitAsync();

        var names = await PartyNamesAsync([partyId]);

        var home = new CustomerPortalHomeDto
        {
            CustomerName = names.GetValueOrDefault(partyId, "—"),
            CurrencyCode = currency,
        };

        var bookings = await Db.Bookings.ForCompany(Tenant)
            .Where(b => b.PrimaryApplicantPartyId == partyId && b.Status != BookingStatus.Cancelled)
            .OrderByDescending(b => b.BookingDate)
            .ToListAsync();

        var projectNames = await ProjectNamesAsync(bookings.Select(b => (Guid?)b.ProjectId));

        var unitIds = bookings.Where(b => b.UnitId != null).Select(b => b.UnitId!.Value).ToList();

        var units = unitIds.Count == 0
            ? []
            : await Db.Units.ForCompany(Tenant)
                .Where(u => unitIds.Contains(u.Id))
                .Include(u => u.Property)
                .ToListAsync();

        var nodeIds = units.Where(u => u.ProjectNodeId != null).Select(u => u.ProjectNodeId!.Value).Distinct().ToList();

        var nodeNames = nodeIds.Count == 0
            ? []
            : await Db.ProjectNodes.ForCompany(Tenant)
                .Where(n => nodeIds.Contains(n.Id)).ToDictionaryAsync(n => n.Id, n => n.Name);

        home.Bookings = bookings.Select(b =>
        {
            var theUnit = b.UnitId is null ? null : units.FirstOrDefault(u => u.Id == b.UnitId);

            return new BookingListItemDto
            {
                Id = b.Id,
                Reference = b.Reference,
                Status = b.Status,
                BookingDate = b.BookingDate,
                ProjectId = b.ProjectId,
                ProjectName = projectNames.GetValueOrDefault(b.ProjectId, "—"),
                UnitId = b.UnitId,
                UnitNumber = theUnit?.UnitNumber,
                BlockName = theUnit?.ProjectNodeId is null ? null : nodeNames.GetValueOrDefault(theUnit.ProjectNodeId.Value),
                SubType = theUnit?.Property?.SubType,
                Area = RealEstateMapper.AreaOrNull(b.AreaSqFt, unit),
                PrimaryApplicantPartyId = b.PrimaryApplicantPartyId,
                ApplicantName = home.CustomerName,
                SourcingChannel = b.SourcingChannel,
                TotalConsideration = b.TotalConsideration,
                TotalPaid = b.TotalPaid,
                Outstanding = b.Outstanding,
                OverdueAmount = b.OverdueAmount,
                CollectionPercent = b.CollectionPercent,
                CurrencyCode = currency,
                NextDueDate = b.NextDueDate,
                NextDueAmount = b.NextDueAmount,
                DaysOverdue = b.DaysOverdue,
                IsDefaulting = b.Status == BookingStatus.Defaulting,
                IsUnderLitigation = b.IsUnderLitigation,
            };
        }).ToList();

        home.TotalInvested = RealEstateMapper.Money(bookings.Sum(b => b.TotalConsideration));
        home.TotalPaid = RealEstateMapper.Money(bookings.Sum(b => b.TotalPaid));
        home.TotalOutstanding = RealEstateMapper.Money(bookings.Sum(b => b.Outstanding));

        var next = bookings.Where(b => b.NextDueDate is not null).OrderBy(b => b.NextDueDate).FirstOrDefault();

        home.NextDueDate = next?.NextDueDate;
        home.NextDueAmount = next?.NextDueAmount ?? 0m;

        var bookingIds = bookings.Select(b => b.Id).ToList();

        // ── What is owed ─────────────────────────────────────────────────────

        var demands = bookingIds.Count == 0
            ? []
            : await Db.Demands.ForCompany(Tenant)
                .Where(d => bookingIds.Contains(d.BookingId) && d.Balance > 0m
                         && d.Status != DemandStatus.Cancelled && d.Status != DemandStatus.Draft)
                .OrderBy(d => d.DueDate)
                .ToListAsync();

        home.OpenDemands = demands.Select(d =>
        {
            var booking = bookings.First(b => b.Id == d.BookingId);
            var theUnit = booking.UnitId is null ? null : units.FirstOrDefault(u => u.Id == booking.UnitId);

            return new DemandListItemDto
            {
                Id = d.Id,
                DemandNumber = d.DemandNumber,
                BookingId = d.BookingId,
                BookingReference = booking.Reference,
                ApplicantName = home.CustomerName,
                UnitNumber = theUnit?.UnitNumber,
                ProjectName = projectNames.GetValueOrDefault(booking.ProjectId, "—"),
                Status = d.Status,
                IssuedOn = d.IssuedOn,
                DueDate = d.DueDate,
                DaysOverdue = RealEstateMapper.DaysOverdue(d.DueDate, Today),
                PrincipalAmount = d.PrincipalAmount,
                SurchargeAmount = d.SurchargeAmount,
                ArrearsAmount = d.ArrearsAmount,
                TaxAmount = d.TaxAmount,
                TotalAmount = d.TotalAmount,
                PaidAmount = d.PaidAmount,
                Balance = d.Balance,
                CurrencyCode = currency,
                IsReminder = d.IsReminder,
                ReminderNumber = d.ReminderNumber,
                SentAt = d.SentAt,
                DocumentUrl = d.DocumentUrl,
            };
        }).ToList();

        // ── What has been paid ───────────────────────────────────────────────

        var receipts = await Db.Receipts.ForCompany(Tenant)
            .Where(r => r.PartyId == partyId && r.Status == ReceiptStatus.Posted)
            .OrderByDescending(r => r.ReceivedOn)
            .Take(20)
            .ToListAsync();

        home.RecentReceipts = receipts.Select(r => new ReceiptListItemDto
        {
            Id = r.Id,
            ReceiptNumber = r.ReceiptNumber,
            Status = r.Status,
            ReceivedOn = r.ReceivedOn,
            PartyId = r.PartyId,
            PartyName = home.CustomerName,
            BookingId = r.BookingId,
            BookingReference = r.BookingId is null
                ? null
                : bookings.FirstOrDefault(b => b.Id == r.BookingId)?.Reference,
            ProjectName = r.ProjectId is null ? null : projectNames.GetValueOrDefault(r.ProjectId.Value),
            Amount = r.Amount,
            AllocatedAmount = r.AllocatedAmount,
            UnallocatedAmount = r.UnallocatedAmount,
            CurrencyCode = currency,
            Instrument = r.Instrument,
            BankName = r.BankName,
            InstrumentNumber = r.InstrumentNumber,
            InstrumentDate = r.InstrumentDate,
            TransactionReference = r.TransactionReference,
            Narration = r.Narration,
        }).ToList();

        // ── Build contracts, where this customer has one ─────────────────────

        var contracts = await Db.ClientBuildContracts.ForCompany(Tenant)
            .Where(c => c.ClientPartyId == partyId || c.CoClientPartyId == partyId)
            .Where(c => c.Status != "Cancelled")
            .OrderByDescending(c => c.StartDate)
            .ToListAsync();

        home.Builds = contracts.Select(c =>
        {
            var value = c.RevisedContractValue > 0m ? c.RevisedContractValue : c.ContractValue;

            return new ClientBuildContractListItemDto
            {
                Id = c.Id,
                Reference = c.Reference,
                Name = c.Name,
                ClientPartyId = c.ClientPartyId,
                ClientName = home.CustomerName,
                SiteAddress = c.SiteAddress,
                Kind = c.Kind,
                Grade = c.Grade,
                PlotArea = RealEstateMapper.Area(c.PlotAreaSqFt, unit),
                CoveredArea = RealEstateMapper.Area(c.CoveredAreaSqFt, unit),
                RatePerSqFt = c.RatePerSqFt,
                ContractValue = c.ContractValue,
                ApprovedVariations = c.ApprovedVariations,
                RevisedContractValue = value,
                CurrencyCode = currency,
                Status = c.Status,
                SignedOn = c.SignedOn,
                StartDate = c.StartDate,
                PlannedCompletionDate = c.PlannedCompletionDate,
                ForecastCompletionDate = c.ForecastCompletionDate,
                SlipDays = c.ForecastCompletionDate is not null && c.PlannedCompletionDate is not null
                    && c.ForecastCompletionDate > c.PlannedCompletionDate
                        ? c.ForecastCompletionDate.Value.DayNumber - c.PlannedCompletionDate.Value.DayNumber
                        : null,
                TotalDemanded = c.TotalDemanded,
                TotalReceived = c.TotalReceived,
                Outstanding = c.Outstanding,
                RetentionHeldByClient = c.RetentionHeldByClient,
                ProgressPercent = c.ProgressPercent,
            };
        }).ToList();

        // ── How far along the building is ────────────────────────────────────

        var projectIds = bookings.Select(b => b.ProjectId).Distinct().ToList();

        if (projectIds.Count > 0)
        {
            var milestones = await Db.ProjectMilestones.ForCompany(Tenant)
                .Where(m => projectIds.Contains(m.ProjectId))
                .OrderBy(m => m.SortOrder)
                .ToListAsync();

            var certifiers = await AgentUserNamesAsync(milestones.Select(m => m.CertifiedByUserId));

            var milestoneNodeIds = milestones.Where(m => m.ProjectNodeId != null)
                .Select(m => m.ProjectNodeId!.Value).Distinct().ToList();

            var milestoneNodes = milestoneNodeIds.Count == 0
                ? []
                : await Db.ProjectNodes.ForCompany(Tenant)
                    .Where(n => milestoneNodeIds.Contains(n.Id)).ToDictionaryAsync(n => n.Id, n => n.Name);

            home.ConstructionProgress = milestones.Select(m => new ProjectMilestoneDto
            {
                Id = m.Id,
                ProjectId = m.ProjectId,
                ProjectNodeId = m.ProjectNodeId,
                BlockName = m.ProjectNodeId is null ? null : milestoneNodes.GetValueOrDefault(m.ProjectNodeId.Value),
                Name = m.Name,
                Code = m.Code,
                SortOrder = m.SortOrder,
                Status = m.Status,
                PlannedDate = m.PlannedDate,
                ForecastDate = m.ForecastDate,
                ReachedOn = m.ReachedOn,
                CertifiedOn = m.CertifiedOn,
                CertifiedByName = m.CertifiedByUserId is null
                    ? null
                    : certifiers.GetValueOrDefault(m.CertifiedByUserId.Value),
                CertificateUrl = m.CertificateUrl,
                WeightPercent = m.WeightPercent,
                ProgressPercent = m.ProgressPercent,
                DemandsRaised = m.DemandsRaised,
                SlipDays = m.ForecastDate is not null && m.PlannedDate is not null
                    && m.ForecastDate > m.PlannedDate
                        ? m.ForecastDate.Value.DayNumber - m.PlannedDate.Value.DayNumber
                        : null,
            }).ToList();

            // Site photographs are what a buyer actually looks at, so the most recent ones are
            // pulled through rather than left on an internal screen.
            home.ProgressPhotoUrls = await Db.ContentAssets.ForCompany(Tenant)
                .Where(a => a.ProjectId != null && projectIds.Contains(a.ProjectId.Value))
                .Where(a => a.AssetType == "ConstructionProgress" && a.IsCurrent)
                .OrderByDescending(a => a.CreatedAt)
                .Select(a => a.Url)
                .Take(12)
                .ToListAsync();
        }

        // ── Documents ────────────────────────────────────────────────────────

        home.Documents = await Db.GeneratedDocuments.ForCompany(Tenant)
            .Where(d => d.PartyId == partyId
                     || (d.BookingId != null && bookingIds.Contains(d.BookingId.Value))
                     || (d.ClientBuildContractId != null
                         && contracts.Select(c => c.Id).Contains(d.ClientBuildContractId.Value)))
            .Where(d => !d.IsSuperseded)
            .OrderByDescending(d => d.GeneratedAt)
            .Take(50)
            .Select(d => new GeneratedDocumentDto
            {
                Id = d.Id,
                DocumentNumber = d.DocumentNumber,
                DocumentType = d.DocumentType,
                Title = d.Title,
                Url = d.Url,
                GeneratedAt = d.GeneratedAt,
                LanguageCode = d.LanguageCode,
                VerificationCode = d.VerificationCode,
                IsSent = d.IsSent,
                IsSigned = d.IsSigned,
                IsSuperseded = d.IsSuperseded,
                PageCount = d.PageCount,
            })
            .ToListAsync();

        // ── Notices, where this customer lives in a society we manage ────────

        var societyIds = await Db.Residents.ForCompany(Tenant)
            .Where(r => r.PartyId == partyId)
            .Select(r => r.SocietyId)
            .Distinct()
            .ToListAsync();

        if (societyIds.Count > 0)
        {
            var notices = await Db.SocietyNotices.ForCompany(Tenant)
                .Where(n => societyIds.Contains(n.SocietyId) && n.IsActive)
                .Where(n => n.ExpiresOn == null || n.ExpiresOn >= Today)
                .OrderByDescending(n => n.IsPinned)
                .ThenByDescending(n => n.PublishedAt)
                .Take(10)
                .ToListAsync();

            var publishers = await AgentUserNamesAsync(notices.Select(n => n.PublishedByUserId));

            home.Notices = notices.Select(n => new SocietyNoticeDto
            {
                Id = n.Id,
                SocietyId = n.SocietyId,
                Title = n.Title,
                Body = n.Body,
                NoticeType = n.NoticeType,
                Severity = n.Severity,
                PublishedAt = n.PublishedAt,
                ExpiresOn = n.ExpiresOn,
                PublishedByName = n.PublishedByUserId is null
                    ? null
                    : publishers.GetValueOrDefault(n.PublishedByUserId.Value),
                IsPinned = n.IsPinned,
                SendAsBroadcast = n.SendAsBroadcast,
                AttachmentUrl = n.AttachmentUrl,
                TargetBlocks = n.TargetBlocks,
                ReadCount = n.ReadCount,
                IsActive = n.IsActive,
                IsExpired = n.ExpiresOn is not null && n.ExpiresOn < Today,
            }).ToList();
        }

        home.UnreadMessages = await Db.PortalMessages.ForCompany(Tenant)
            .CountAsync(m => m.PartyId == partyId && m.Direction == "Outbound" && m.ReadAt == null);

        return home;
    }
}
