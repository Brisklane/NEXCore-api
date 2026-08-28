using Microsoft.EntityFrameworkCore;
using Nexcore.SharedKernel.Api;
using RealEstate.Application.DTOs;
using RealEstate.Application.Services.Interfaces;
using RealEstate.Domain.Entities;
using RealEstate.Domain.Enums;
using RealEstate.Infrastructure.Persistence;

namespace RealEstate.Infrastructure.Services;

/// <summary>
/// People, KYC, enquiries, matching, activities and the day's work.
///
/// Two things here decide whether a sales floor trusts the system. **Speed to lead** is a clock
/// that starts the second an enquiry lands and is visible on every board. **Deduplication** means
/// the same phone number is one person with many enquiries, not seven leads chased by seven agents.
/// </summary>
public partial class CrmService(
    RealEstateDbContext db,
    IRealEstateTenant tenant,
    RealEstateNumbering numbering)
    : RealEstateServiceBase(db, tenant), ICrmService
{
    // ═══ Parties ═════════════════════════════════════════════════════════════

    public async Task<PaginatedResponse<PartyListItemDto>> GetPartiesAsync(PartySearchDto query)
    {
        var q = Db.Parties.ForCompany(Tenant)
            .WhereIf(query.Kind.HasValue, p => p.Kind == query.Kind)
            .WhereIf(query.KycStatus.HasValue, p => p.KycStatus == query.KycStatus)
            .WhereIf(query.CautionedOnly == true, p => p.IsCautioned)
            .WhereIf(query.WithOutstandingOnly == true, p => p.TotalOutstanding > 0)
            .WhereIf(query.OwnerAgentId.HasValue, p => p.OwnerAgentId == query.OwnerAgentId)
            .WhereIf(!string.IsNullOrWhiteSpace(query.Search),
                p => (p.DisplayName != null && p.DisplayName.Contains(query.Search!))
                     || (p.FirstName != null && p.FirstName.Contains(query.Search!))
                     || (p.LastName != null && p.LastName.Contains(query.Search!))
                     || (p.OrganisationName != null && p.OrganisationName.Contains(query.Search!))
                     || (p.PrimaryPhone != null && p.PrimaryPhone.Contains(query.Search!))
                     || (p.PrimaryEmail != null && p.PrimaryEmail.Contains(query.Search!))
                     || p.Reference.Contains(query.Search!));

        if (query.Role.HasValue)
        {
            var partyIds = Db.PartyRoles.ForCompany(Tenant)
                .Where(r => r.Kind == query.Role && r.IsActive)
                .Select(r => r.PartyId);
            q = q.Where(p => partyIds.Contains(p.Id));
        }

        var ordered = q.OrderBy(p => p.DisplayName ?? p.OrganisationName ?? p.LastName);

        return await PageAsync(ordered, query, async rows =>
        {
            var ids = rows.Select(r => r.Id).ToList();

            var roles = await Db.PartyRoles.ForCompany(Tenant)
                .Where(r => ids.Contains(r.PartyId) && r.IsActive)
                .Select(r => new { r.PartyId, r.Kind })
                .ToListAsync();

            var owned = await Db.PropertyOwnerships.ForCompany(Tenant)
                .Where(o => ids.Contains(o.PartyId) && o.ToDate == null)
                .GroupBy(o => o.PartyId)
                .Select(g => new { PartyId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.PartyId, x => x.Count);

            var agents = await Db.AgentProfiles.ForCompany(Tenant).ToDictionaryAsync(a => a.Id, a => a.DisplayName);
            var addresses = await Db.PartyAddresses.ForCompany(Tenant)
                .Where(a => ids.Contains(a.PartyId) && a.IsMailingAddress)
                .ToDictionaryAsync(a => a.PartyId, a => a.City);

            return rows.Select(p => new PartyListItemDto
            {
                Id = p.Id,
                Reference = p.Reference,
                Kind = p.Kind,
                DisplayName = RealEstateMapper.DisplayName(p),
                FatherOrGuardianName = p.FatherOrGuardianName,
                PrimaryPhone = p.PrimaryPhone,
                PrimaryEmail = p.PrimaryEmail,
                City = addresses.GetValueOrDefault(p.Id),
                PhotoUrl = p.PhotoUrl,
                Roles = roles.Where(r => r.PartyId == p.Id).Select(r => r.Kind).Distinct().ToList(),
                KycStatus = p.KycStatus,
                RiskRating = p.RiskRating,
                IsCautioned = p.IsCautioned,
                TotalInvested = p.TotalInvested,
                TotalOutstanding = p.TotalOutstanding,
                NextDueDate = p.NextDueDate,
                OwnerAgentName = p.OwnerAgentId is null ? null : agents.GetValueOrDefault(p.OwnerAgentId.Value),
                PropertyCount = owned.GetValueOrDefault(p.Id),
            }).ToList();
        });
    }

    public async Task<PartyDetailDto?> GetPartyAsync(Guid id)
    {
        var party = await Db.Parties.ForCompany(Tenant)
            .Include(p => p.Roles)
            .Include(p => p.Contacts)
            .Include(p => p.Addresses)
            .Include(p => p.Identities)
            .Include(p => p.Consents)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (party is null) return null;

        var today = Today;
        var agents = await Db.AgentProfiles.ForCompany(Tenant).ToDictionaryAsync(a => a.Id, a => a.DisplayName);

        var detail = new PartyDetailDto
        {
            Id = party.Id,
            Reference = party.Reference,
            Kind = party.Kind,
            Salutation = party.Salutation,
            FirstName = party.FirstName,
            MiddleName = party.MiddleName,
            LastName = party.LastName,
            DisplayName = RealEstateMapper.DisplayName(party),
            FatherOrGuardianName = party.FatherOrGuardianName,
            DateOfBirth = party.DateOfBirth,
            Nationality = party.Nationality,
            ResidencyStatus = party.ResidencyStatus,
            Occupation = party.Occupation,
            Employer = party.Employer,
            PhotoUrl = party.PhotoUrl,
            SignatureSpecimenUrl = party.SignatureSpecimenUrl,
            OrganisationName = party.OrganisationName,
            TradingName = party.TradingName,
            RegistrationNumber = party.RegistrationNumber,
            TaxNumber = party.TaxNumber,
            IncorporatedOn = party.IncorporatedOn,
            Industry = party.Industry,
            PreferredChannel = party.PreferredChannel,
            PreferredLanguage = party.PreferredLanguage,
            KycStatus = party.KycStatus,
            RiskRating = party.RiskRating,
            KycVerifiedOn = party.KycVerifiedOn,
            KycExpiresOn = party.KycExpiresOn,
            IsPoliticallyExposed = party.IsPoliticallyExposed,
            IsCautioned = party.IsCautioned,
            OwnerAgentName = party.OwnerAgentId is null ? null : agents.GetValueOrDefault(party.OwnerAgentId.Value),
            Notes = party.Notes,

            Roles = party.Roles.Select(r => new PartyRoleDto
            {
                Id = r.Id,
                Kind = r.Kind,
                FromDate = r.FromDate,
                ToDate = r.ToDate,
                IsActive = r.IsActive,
                ContextType = r.ContextType,
                ContextId = r.ContextId,
            }).ToList(),

            Contacts = party.Contacts.Select(c => new PartyContactDto
            {
                Id = c.Id,
                ContactType = c.ContactType,
                Value = c.Value,
                Label = c.Label,
                IsWhatsApp = c.IsWhatsApp,
                IsPrimary = c.IsPrimary,
                IsVerified = c.IsVerified,
                IsUnreachable = c.IsUnreachable,
                PersonName = c.PersonName,
                Designation = c.Designation,
                IsAuthorisedSignatory = c.IsAuthorisedSignatory,
            }).ToList(),

            Addresses = party.Addresses.Select(a => new PartyAddressDto
            {
                Id = a.Id,
                AddressType = a.AddressType,
                Line1 = a.Line1,
                Line2 = a.Line2,
                Street = a.Street,
                City = a.City,
                State = a.State,
                PostCode = a.PostCode,
                CountryCode = a.CountryCode,
                GeoAreaId = a.GeoAreaId,
                IsMailingAddress = a.IsMailingAddress,
                IsVerified = a.IsVerified,
                OneLine = RealEstateMapper.OneLineAddress(a),
            }).ToList(),

            Identities = party.Identities.Select(i => new PartyIdentityDto
            {
                Id = i.Id,
                Kind = i.Kind,
                LocalLabel = i.LocalLabel,
                Number = i.Number,
                IssuingCountry = i.IssuingCountry,
                IssuingAuthority = i.IssuingAuthority,
                IssuedOn = i.IssuedOn,
                ExpiresOn = i.ExpiresOn,
                FrontImageUrl = i.FrontImageUrl,
                BackImageUrl = i.BackImageUrl,
                State = i.State,
                IsPrimary = i.IsPrimary,
                IsExpired = i.ExpiresOn is not null && i.ExpiresOn < today,
            }).ToList(),

            Consents = party.Consents.Select(c => new PartyConsentDto
            {
                Id = c.Id,
                Channel = c.Channel,
                Purpose = c.Purpose,
                IsGranted = c.IsGranted,
                RecordedAt = c.RecordedAt,
                Source = c.Source,
                WithdrawnAt = c.WithdrawnAt,
            }).ToList(),
        };

        var relationships = await Db.PartyRelationships.ForCompany(Tenant)
            .Where(r => r.PartyId == id)
            .ToListAsync();

        var relatedNames = await PartyNamesAsync(relationships.Select(r => r.RelatedPartyId));

        detail.Relationships = relationships.Select(r => new PartyRelationshipDto
        {
            Id = r.Id,
            RelatedPartyId = r.RelatedPartyId,
            RelatedPartyName = relatedNames.GetValueOrDefault(r.RelatedPartyId, "—"),
            RelationshipType = r.RelationshipType,
            FromDate = r.FromDate,
            ToDate = r.ToDate,
            PowerScope = r.PowerScope,
            PoaDocumentNumber = r.PoaDocumentNumber,
            PoaValidFrom = r.PoaValidFrom,
            PoaValidTo = r.PoaValidTo,
            PoaIsRegistered = r.PoaIsRegistered,
            PoaIsExpired = r.PoaValidTo is not null && r.PoaValidTo < today,
            DocumentUrl = r.DocumentUrl,
            SharePercent = r.SharePercent,
            IsVerified = r.IsVerified,
        }).ToList();

        detail.Cautions = await Db.CautionListEntries.ForCompany(Tenant)
            .Where(c => c.PartyId == id)
            .Select(c => new CautionListEntryDto
            {
                Id = c.Id,
                Category = c.Category,
                Severity = c.Severity,
                Reason = c.Reason,
                EvidenceUrl = c.EvidenceUrl,
                RaisedOn = c.RaisedOn,
                ExpiresOn = c.ExpiresOn,
                BlocksNewBusiness = c.BlocksNewBusiness,
                IsActive = c.IsActive,
            })
            .ToListAsync();

        // The money summary: everything they owe us across every commitment.
        var bookings = await Db.Bookings.ForCompany(Tenant)
            .Where(b => b.PrimaryApplicantPartyId == id && b.Status != BookingStatus.Cancelled)
            .ToListAsync();

        var tenancies = await Db.Tenancies.ForCompany(Tenant)
            .Where(t => Db.TenancyParties.ForCompany(Tenant).Any(p => p.TenancyId == t.Id && p.PartyId == id))
            .ToListAsync();

        var maintenanceArrears = await Db.MaintenanceBills.ForCompany(Tenant)
            .Where(m => m.PartyId == id && m.Balance > 0)
            .SumAsync(m => (decimal?)m.Balance) ?? 0m;

        detail.Money = new PartyMoneySummaryDto
        {
            CurrencyCode = await CurrencyAsync(),
            TotalInvested = bookings.Sum(b => b.TotalConsideration),
            TotalPaid = bookings.Sum(b => b.TotalPaid),
            TotalOutstanding = bookings.Sum(b => b.Outstanding),
            OverdueAmount = bookings.Sum(b => b.OverdueAmount),
            SurchargeAccrued = bookings.Sum(b => b.TotalSurcharge - b.TotalWaived),
            NextDueDate = bookings.Where(b => b.NextDueDate.HasValue).Min(b => b.NextDueDate),
            NextDueAmount = bookings.OrderBy(b => b.NextDueDate).FirstOrDefault()?.NextDueAmount ?? 0m,
            RentArrears = tenancies.Sum(t => t.ArrearsAmount),
            MaintenanceArrears = maintenanceArrears,
            DaysOverdue = bookings.Count == 0 ? 0 : bookings.Max(b => b.DaysOverdue),
        };

        detail.Money.IsDefaulter = detail.Money.OverdueAmount > 0m || detail.Money.RentArrears > 0m;

        var enquiries = await Db.Enquiries.ForCompany(Tenant)
            .Where(e => e.PartyId == id)
            .OrderByDescending(e => e.ReceivedAt)
            .Take(20)
            .ToListAsync();

        detail.Enquiries = await MapEnquiryListAsync(enquiries);

        var ownedIds = await Db.PropertyOwnerships.ForCompany(Tenant)
            .Where(o => o.PartyId == id && o.ToDate == null)
            .Select(o => o.PropertyId)
            .ToListAsync();

        if (ownedIds.Count > 0)
        {
            var areaUnit = await AreaUnitAsync();
            detail.OwnedProperties = await Db.Properties.ForCompany(Tenant)
                .Where(p => ownedIds.Contains(p.Id))
                .Select(p => new PropertyListItemDto
                {
                    Id = p.Id,
                    Reference = p.Reference,
                    Name = p.Name,
                    Category = p.Category,
                    SubType = p.SubType,
                    Status = p.Status,
                    Occupancy = p.Occupancy,
                    City = p.PostCode,
                    AskingPrice = p.AskingPrice,
                    MonthlyRent = p.MonthlyRent,
                    HasLitigation = p.HasLitigation,
                    IsMortgaged = p.IsMortgaged,
                })
                .ToListAsync();
        }

        detail.Timeline = await BuildPartyTimelineAsync(id);

        return detail;
    }

    private async Task<List<TimelineEntryDto>> BuildPartyTimelineAsync(Guid partyId)
    {
        var activities = await Db.Activities.ForCompany(Tenant)
            .Where(a => a.PartyId == partyId)
            .OrderByDescending(a => a.OccurredAt)
            .Take(100)
            .ToListAsync();

        var receipts = await Db.Receipts.ForCompany(Tenant)
            .Where(r => r.PartyId == partyId && r.Status != ReceiptStatus.Reversed)
            .OrderByDescending(r => r.ReceivedOn)
            .Take(50)
            .ToListAsync();

        var entries = activities.Select(a => new TimelineEntryDto
        {
            Id = a.Id,
            OccurredAt = a.OccurredAt,
            Kind = "activity",
            Title = a.Subject ?? a.Kind.ToString(),
            Detail = a.Body,
            Icon = IconFor(a.Kind),
            Tone = a.Direction == ActivityDirection.Inbound ? "cyan" : null,
        }).ToList();

        entries.AddRange(receipts.Select(r => new TimelineEntryDto
        {
            Id = r.Id,
            OccurredAt = r.ReceivedOn.ToDateTime(TimeOnly.MinValue),
            Kind = "money",
            Title = $"Receipt {r.ReceiptNumber}",
            Detail = r.Instrument.ToString(),
            Icon = "payments",
            Tone = "success",
            Amount = r.Amount,
        }));

        return entries.OrderByDescending(e => e.OccurredAt).Take(120).ToList();
    }

    private static string IconFor(ActivityKind kind) => kind switch
    {
        ActivityKind.Call => "call",
        ActivityKind.WhatsApp => "chat",
        ActivityKind.Email => "mail",
        ActivityKind.Sms => "sms",
        ActivityKind.Meeting => "groups",
        ActivityKind.Viewing => "visibility",
        ActivityKind.SiteVisit => "directions_car",
        ActivityKind.DocumentSent => "description",
        ActivityKind.StatusChange => "swap_horiz",
        ActivityKind.Task => "task_alt",
        _ => "notes",
    };

    public async Task<PartyDetailDto> SavePartyAsync(PartyUpsertDto dto, Guid userId)
    {
        if (!dto.AcknowledgeDuplicate && dto.Id is null)
        {
            var duplicates = await FindDuplicatePartiesAsync(dto);
            if (duplicates.Count > 0)
            {
                throw new InvalidOperationException(
                    $"{duplicates[0].DisplayName} already exists with the same phone or email. " +
                    "Open that record, or confirm they are a different person.");
            }
        }

        var party = dto.Id is not null && dto.Id != Guid.Empty
            ? await Db.Parties.ForCompany(Tenant)
                .Include(p => p.Contacts).Include(p => p.Addresses).Include(p => p.Identities).Include(p => p.Roles)
                .FirstOrDefaultAsync(p => p.Id == dto.Id)
            : null;

        if (party is null)
        {
            party = new Party { Reference = await numbering.NextPartyReferenceAsync() }.StampNew(Tenant, userId);
            Db.Parties.Add(party);
        }
        else party.StampUpdated(userId);

        party.Kind = dto.Kind;
        party.Salutation = dto.Salutation;
        party.FirstName = dto.FirstName;
        party.MiddleName = dto.MiddleName;
        party.LastName = dto.LastName;
        party.FatherOrGuardianName = dto.FatherOrGuardianName;
        party.DateOfBirth = dto.DateOfBirth;
        party.Nationality = dto.Nationality;
        party.ResidencyStatus = dto.ResidencyStatus;
        party.Occupation = dto.Occupation;
        party.Employer = dto.Employer;
        party.PhotoUrl = dto.PhotoUrl;
        party.SignatureSpecimenUrl = dto.SignatureSpecimenUrl;
        party.OrganisationName = dto.OrganisationName;
        party.TradingName = dto.TradingName;
        party.RegistrationNumber = dto.RegistrationNumber;
        party.TaxNumber = dto.TaxNumber;
        party.IncorporatedOn = dto.IncorporatedOn;
        party.Industry = dto.Industry;
        party.PrimaryPhone = dto.PrimaryPhone;
        party.PrimaryEmail = dto.PrimaryEmail;
        party.PreferredChannel = dto.PreferredChannel;
        party.PreferredLanguage = dto.PreferredLanguage;
        party.OwnerAgentId = dto.OwnerAgentId;
        party.OfficeId = dto.OfficeId;
        party.Notes = dto.Notes;
        party.DisplayName = RealEstateMapper.DisplayName(party);

        SyncContacts(party, dto, userId);
        SyncAddresses(party, dto, userId);
        SyncIdentities(party, dto, userId);

        foreach (var role in dto.AddRoles.Distinct())
        {
            if (party.Roles.Any(r => r.Kind == role && r.IsActive)) continue;

            party.Roles.Add(new PartyRole
            {
                Kind = role,
                FromDate = Today,
                IsActive = true,
            }.StampNew(Tenant, userId));
        }

        await Db.SaveChangesAsync();

        // Relationships reference other parties, so they are written after the party has an id.
        if (dto.Relationships.Count > 0)
        {
            var existing = await Db.PartyRelationships.ForCompany(Tenant)
                .Where(r => r.PartyId == party.Id)
                .ToListAsync();

            Db.PartyRelationships.RemoveRange(existing);

            foreach (var r in dto.Relationships)
            {
                Db.PartyRelationships.Add(new PartyRelationship
                {
                    PartyId = party.Id,
                    RelatedPartyId = r.RelatedPartyId,
                    RelationshipType = r.RelationshipType,
                    FromDate = r.FromDate,
                    ToDate = r.ToDate,
                    PowerScope = r.PowerScope,
                    PoaDocumentNumber = r.PoaDocumentNumber,
                    PoaValidFrom = r.PoaValidFrom,
                    PoaValidTo = r.PoaValidTo,
                    PoaIsRegistered = r.PoaIsRegistered,
                    DocumentUrl = r.DocumentUrl,
                    SharePercent = r.SharePercent,
                    IsVerified = r.IsVerified,
                }.StampNew(Tenant, userId));
            }

            await Db.SaveChangesAsync();
        }

        return (await GetPartyAsync(party.Id))!;
    }

    private void SyncContacts(Party party, PartyUpsertDto dto, Guid userId)
    {
        if (dto.Contacts.Count == 0) return;

        Db.PartyContacts.RemoveRange(party.Contacts);

        foreach (var c in dto.Contacts.Where(c => !string.IsNullOrWhiteSpace(c.Value)))
        {
            party.Contacts.Add(new PartyContact
            {
                ContactType = c.ContactType,
                Value = c.Value.Trim(),
                Label = c.Label,
                IsWhatsApp = c.IsWhatsApp,
                IsPrimary = c.IsPrimary,
                IsVerified = c.IsVerified,
                IsUnreachable = c.IsUnreachable,
                PersonName = c.PersonName,
                Designation = c.Designation,
                IsAuthorisedSignatory = c.IsAuthorisedSignatory,
            }.StampNew(Tenant, userId));
        }
    }

    private void SyncAddresses(Party party, PartyUpsertDto dto, Guid userId)
    {
        if (dto.Addresses.Count == 0) return;

        Db.PartyAddresses.RemoveRange(party.Addresses);

        // Exactly one mailing address, because that is where the demand letter goes.
        var hasMailing = dto.Addresses.Any(a => a.IsMailingAddress);

        for (var i = 0; i < dto.Addresses.Count; i++)
        {
            var a = dto.Addresses[i];

            party.Addresses.Add(new PartyAddress
            {
                AddressType = a.AddressType,
                Line1 = a.Line1,
                Line2 = a.Line2,
                Street = a.Street,
                City = a.City,
                State = a.State,
                PostCode = a.PostCode,
                CountryCode = a.CountryCode,
                GeoAreaId = a.GeoAreaId,
                IsMailingAddress = hasMailing ? a.IsMailingAddress : i == 0,
                IsVerified = a.IsVerified,
            }.StampNew(Tenant, userId));
        }
    }

    private void SyncIdentities(Party party, PartyUpsertDto dto, Guid userId)
    {
        if (dto.Identities.Count == 0) return;

        Db.PartyIdentities.RemoveRange(party.Identities);

        var hasPrimary = dto.Identities.Any(i => i.IsPrimary);

        for (var i = 0; i < dto.Identities.Count; i++)
        {
            var id = dto.Identities[i];

            party.Identities.Add(new PartyIdentity
            {
                Kind = id.Kind,
                LocalLabel = id.LocalLabel,
                Number = id.Number,
                IssuingCountry = id.IssuingCountry,
                IssuingAuthority = id.IssuingAuthority,
                IssuedOn = id.IssuedOn,
                ExpiresOn = id.ExpiresOn,
                FrontImageUrl = id.FrontImageUrl,
                BackImageUrl = id.BackImageUrl,
                State = id.State,
                IsPrimary = hasPrimary ? id.IsPrimary : i == 0,
            }.StampNew(Tenant, userId));
        }
    }

    public async Task<List<LookupDto>> SearchPartiesAsync(string search, PartyRoleKind? role, int take)
    {
        var q = Db.Parties.ForCompany(Tenant)
            .Where(p => (p.DisplayName != null && p.DisplayName.Contains(search))
                        || (p.PrimaryPhone != null && p.PrimaryPhone.Contains(search))
                        || (p.OrganisationName != null && p.OrganisationName.Contains(search))
                        || p.Reference.Contains(search));

        if (role.HasValue)
        {
            var ids = Db.PartyRoles.ForCompany(Tenant).Where(r => r.Kind == role && r.IsActive).Select(r => r.PartyId);
            q = q.Where(p => ids.Contains(p.Id));
        }

        return await q
            .OrderBy(p => p.DisplayName)
            .Take(take <= 0 ? 20 : take)
            .Select(p => new LookupDto
            {
                Id = p.Id,
                Label = p.DisplayName ?? p.OrganisationName ?? p.Reference,
                SubLabel = p.PrimaryPhone,
                Code = p.Reference,
            })
            .ToListAsync();
    }

    /// <summary>
    /// The duplicate check. Phone and email, because those are what people actually re-enter — and
    /// identity number, because a scheme's transfer desk lives on it.
    /// </summary>
    public async Task<List<PartyListItemDto>> FindDuplicatePartiesAsync(PartyUpsertDto dto)
    {
        var phone = dto.PrimaryPhone?.Trim();
        var email = dto.PrimaryEmail?.Trim();
        var identity = dto.Identities.FirstOrDefault()?.Number?.Trim();

        if (string.IsNullOrWhiteSpace(phone) && string.IsNullOrWhiteSpace(email) && string.IsNullOrWhiteSpace(identity))
            return [];

        var q = Db.Parties.ForCompany(Tenant).AsQueryable();

        if (!string.IsNullOrWhiteSpace(identity))
        {
            var identityMatches = Db.PartyIdentities.ForCompany(Tenant)
                .Where(i => i.Number == identity)
                .Select(i => i.PartyId);

            q = q.Where(p =>
                (phone != null && p.PrimaryPhone == phone)
                || (email != null && p.PrimaryEmail == email)
                || identityMatches.Contains(p.Id));
        }
        else
        {
            q = q.Where(p => (phone != null && p.PrimaryPhone == phone) || (email != null && p.PrimaryEmail == email));
        }

        if (dto.Id is not null && dto.Id != Guid.Empty) q = q.Where(p => p.Id != dto.Id);

        var matches = await q.Take(5).ToListAsync();

        return matches.Select(p => new PartyListItemDto
        {
            Id = p.Id,
            Reference = p.Reference,
            Kind = p.Kind,
            DisplayName = RealEstateMapper.DisplayName(p),
            PrimaryPhone = p.PrimaryPhone,
            PrimaryEmail = p.PrimaryEmail,
            KycStatus = p.KycStatus,
            IsCautioned = p.IsCautioned,
            TotalOutstanding = p.TotalOutstanding,
        }).ToList();
    }

    /// <summary>
    /// Folds one party into another. Everything that pointed at the duplicate is re-pointed, and
    /// the duplicate is soft-deleted rather than destroyed so the trail survives.
    /// </summary>
    public async Task<PartyDetailDto> MergePartiesAsync(Guid keepId, Guid mergeId, Guid userId)
    {
        if (keepId == mergeId) throw new InvalidOperationException("Those are the same record.");

        var keep = await Db.Parties.ForCompany(Tenant).FirstOrDefaultAsync(p => p.Id == keepId)
            ?? throw new InvalidOperationException("The record to keep does not exist.");

        var merge = await Db.Parties.ForCompany(Tenant).FirstOrDefaultAsync(p => p.Id == mergeId)
            ?? throw new InvalidOperationException("The record to merge does not exist.");

        var bookings = await Db.Bookings.ForCompany(Tenant).Where(b => b.PrimaryApplicantPartyId == mergeId).ToListAsync();
        foreach (var b in bookings) { b.PrimaryApplicantPartyId = keepId; b.StampUpdated(userId); }

        var enquiries = await Db.Enquiries.ForCompany(Tenant).Where(e => e.PartyId == mergeId).ToListAsync();
        foreach (var e in enquiries) { e.PartyId = keepId; e.StampUpdated(userId); }

        var receipts = await Db.Receipts.ForCompany(Tenant).Where(r => r.PartyId == mergeId).ToListAsync();
        foreach (var r in receipts) { r.PartyId = keepId; r.StampUpdated(userId); }

        var ownerships = await Db.PropertyOwnerships.ForCompany(Tenant).Where(o => o.PartyId == mergeId).ToListAsync();
        foreach (var o in ownerships) { o.PartyId = keepId; o.StampUpdated(userId); }

        var activities = await Db.Activities.ForCompany(Tenant).Where(a => a.PartyId == mergeId).ToListAsync();
        foreach (var a in activities) { a.PartyId = keepId; a.StampUpdated(userId); }

        merge.Notes = $"Merged into {keep.Reference} on {Today:d MMM yyyy}. {merge.Notes}".Trim();
        merge.StampDeleted(userId);

        await Db.SaveChangesAsync();
        return (await GetPartyAsync(keepId))!;
    }
}
