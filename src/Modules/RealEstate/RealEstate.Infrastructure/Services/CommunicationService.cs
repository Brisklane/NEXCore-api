using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Nexcore.SharedKernel.Api;
using RealEstate.Application.DTOs;
using RealEstate.Application.Services.Interfaces;
using RealEstate.Domain.Entities;
using RealEstate.Domain.Enums;
using RealEstate.Infrastructure.Persistence;

namespace RealEstate.Infrastructure.Services;

/// <summary>
/// Conversations, templates, broadcasts, notifications and the customer portal.
///
/// The governing idea is restraint. A property company can message every customer it has in one
/// click, and the moment it does that twice in a week those customers stop reading anything it
/// sends — including the demand notice that matters. So consent is checked, quiet hours are
/// honoured, a daily cap applies per person, and a broadcast always runs as a dry run first with
/// the suppressions shown before anybody commits to sending.
/// </summary>
public partial class CommunicationService(
    RealEstateDbContext db,
    IRealEstateTenant tenant,
    RealEstateNumbering numbering)
    : RealEstateServiceBase(db, tenant), ICommunicationService
{
    // ═══ Conversations ═══════════════════════════════════════════════════════

    public async Task<PaginatedResponse<ConversationDto>> GetConversationsAsync(
        ListQueryDto query, NotificationChannel? channel, string? status, Guid? assignedToUserId)
    {
        var q = Db.Conversations.ForCompany(Tenant)
            .WhereIf(channel.HasValue, c => c.Channel == channel)
            .WhereIf(!string.IsNullOrWhiteSpace(status), c => c.Status == status)
            .WhereIf(assignedToUserId.HasValue, c => c.AssignedToUserId == assignedToUserId)
            .WhereIf(!string.IsNullOrWhiteSpace(query.Search),
                c => (c.Subject ?? "").Contains(query.Search!) || (c.ContactAddress ?? "").Contains(query.Search!))

            // Unanswered first, oldest unanswered at the top. An inbox sorted by recency buries the
            // customer who has been waiting longest, which is precisely the wrong way round.
            .OrderByDescending(c => c.AwaitingReply)
            .ThenBy(c => c.AwaitingReply ? c.LastMessageAt : null)
            .ThenByDescending(c => c.LastMessageAt);

        return await PageAsync(q, query, rows => MapConversationsAsync(rows, includeMessages: false));
    }

    public async Task<ConversationDto?> GetConversationAsync(Guid id)
    {
        var conversation = await Db.Conversations.ForCompany(Tenant)
            .Include(c => c.Messages)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (conversation is null) return null;

        return (await MapConversationsAsync([conversation], includeMessages: true))[0];
    }

    private async Task<List<ConversationDto>> MapConversationsAsync(
        List<Conversation> conversations, bool includeMessages)
    {
        if (conversations.Count == 0) return [];

        var partyIds = conversations.Where(c => c.PartyId != null).Select(c => c.PartyId!.Value).ToList();
        var names = await PartyNamesAsync(partyIds);

        var phones = partyIds.Count == 0
            ? []
            : await Db.Parties.ForCompany(Tenant)
                .Where(p => partyIds.Contains(p.Id))
                .Select(p => new { p.Id, p.PrimaryPhone })
                .ToDictionaryAsync(x => x.Id, x => x.PrimaryPhone);

        var assignees = await AgentUserNamesAsync(conversations.Select(c => c.AssignedToUserId));

        var messages = includeMessages
            ? conversations.SelectMany(c => c.Messages).ToList()
            : await Db.ConversationMessages.ForCompany(Tenant)
                .Where(m => conversations.Select(c => c.Id).Contains(m.ConversationId))
                .GroupBy(m => m.ConversationId)
                .Select(g => g.OrderByDescending(m => m.SentAt).First())
                .ToListAsync();

        var senders = await AgentUserNamesAsync(messages.Select(m => m.SentByUserId));

        return conversations.Select(c =>
        {
            var mine = messages.Where(m => m.ConversationId == c.Id).OrderBy(m => m.SentAt).ToList();
            var last = mine.LastOrDefault();

            return new ConversationDto
            {
                Id = c.Id,
                PartyId = c.PartyId,
                PartyName = c.PartyId is null ? null : names.GetValueOrDefault(c.PartyId.Value),
                PartyPhone = c.ContactAddress ?? (c.PartyId is null ? null : phones.GetValueOrDefault(c.PartyId.Value)),
                EnquiryId = c.EnquiryId,
                ChannelPartnerId = c.ChannelPartnerId,
                Channel = c.Channel,
                Subject = c.Subject,
                StartedAt = c.StartedAt,
                LastMessageAt = c.LastMessageAt,
                MessageCount = c.MessageCount,
                UnreadCount = c.UnreadCount,
                AssignedToName = c.AssignedToUserId is null ? null : assignees.GetValueOrDefault(c.AssignedToUserId.Value),
                Status = c.Status,
                SnoozedUntil = c.SnoozedUntil,
                AwaitingReply = c.AwaitingReply,
                MinutesAwaitingReply = c.AwaitingReply && c.LastMessageAt is not null
                    ? (int)(DateTime.UtcNow - c.LastMessageAt.Value).TotalMinutes
                    : null,
                LastMessagePreview = Preview(last?.Body ?? last?.Caption),
                Messages = includeMessages
                    ? mine.Select(m => new ConversationMessageDto
                    {
                        Id = m.Id,
                        Direction = m.Direction,
                        SentAt = m.SentAt,
                        Body = m.Body,
                        MediaUrl = m.MediaUrl,
                        MediaType = m.MediaType,
                        Caption = m.Caption,
                        SentByName = m.SentByUserId is null ? null : senders.GetValueOrDefault(m.SentByUserId.Value),
                        DeliveredAt = m.DeliveredAt,
                        ReadAt = m.ReadAt,
                        Failed = m.Failed,
                        FailureReason = m.FailureReason,
                        IsAutomated = m.IsAutomated,
                    }).ToList()
                    : [],
            };
        }).ToList();
    }

    private static string? Preview(string? body)
    {
        if (string.IsNullOrWhiteSpace(body)) return null;

        var flattened = Regex.Replace(body.Trim(), @"\s+", " ");

        return flattened.Length <= 120 ? flattened : flattened[..117] + "…";
    }

    protected async Task<Dictionary<Guid, string>> AgentUserNamesAsync(IEnumerable<Guid?> userIds)
    {
        var ids = userIds.Where(i => i.HasValue).Select(i => i!.Value).Distinct().ToList();
        if (ids.Count == 0) return [];

        return await Db.AgentProfiles.ForCompany(Tenant)
            .Where(a => a.UserId != null && ids.Contains(a.UserId.Value))
            .ToDictionaryAsync(a => a.UserId!.Value, a => a.DisplayName);
    }

    public async Task<ConversationMessageDto> SendAsync(SendMessageDto dto, Guid userId)
    {
        var settings = await SettingsAsync();

        Conversation? conversation = dto.ConversationId is null
            ? null
            : await Db.Conversations.ForCompany(Tenant).FirstOrDefaultAsync(c => c.Id == dto.ConversationId);

        var partyId = dto.PartyId ?? conversation?.PartyId;

        // A reply inside an open conversation is a human answering a human, and consent rules do
        // not apply to that. Starting a new thread is outbound marketing until proven otherwise.
        var isReply = conversation is not null && conversation.AwaitingReply;

        if (!isReply && partyId is not null)
        {
            var block = await SuppressionReasonAsync(partyId.Value, dto.Channel, settings);

            if (block is not null)
                throw new InvalidOperationException(block);
        }

        var body = dto.Body;

        if (dto.MessageTemplateId is not null)
        {
            var template = await Db.MessageTemplates.ForCompany(Tenant)
                .FirstOrDefaultAsync(t => t.Id == dto.MessageTemplateId)
                ?? throw new InvalidOperationException("That message template does not exist.");

            if (template.Channel != dto.Channel)
                throw new InvalidOperationException(
                    $"“{template.Name}” is a {template.Channel} template and cannot be sent over {dto.Channel}.");

            // Providers such as WhatsApp will simply reject an unapproved template. Refusing here,
            // with the reason, beats a silent failure the sender never sees.
            if (dto.Channel == NotificationChannel.WhatsApp && !template.IsProviderApproved)
                throw new InvalidOperationException(
                    $"“{template.Name}” has not been approved by the messaging provider yet, so it cannot be sent.");

            body = Merge(template.Body, dto.MergeValues);

            var unresolved = MergeFieldsIn(body);

            if (unresolved.Count > 0)
                throw new InvalidOperationException(
                    "These merge fields were not supplied: " + string.Join(", ", unresolved) + ".");
        }

        if (string.IsNullOrWhiteSpace(body) && string.IsNullOrWhiteSpace(dto.MediaUrl))
            throw new InvalidOperationException("There is nothing to send.");

        if (conversation is null)
        {
            conversation = new Conversation
            {
                PartyId = partyId,
                EnquiryId = dto.EnquiryId,
                Channel = dto.Channel,
                ContactAddress = dto.To,
                Subject = dto.Subject,
                StartedAt = DateTime.UtcNow,
                Status = "Open",
                AssignedToUserId = userId,
            }.StampNew(Tenant, userId);

            Db.Conversations.Add(conversation);
        }

        var message = new ConversationMessage
        {
            ConversationId = conversation.Id,
            Direction = ActivityDirection.Outbound,
            SentAt = DateTime.UtcNow,
            Body = body,
            MediaUrl = dto.MediaUrl,
            Caption = dto.MediaUrl is null ? null : dto.Subject,
            SentByUserId = userId,
            MessageTemplateId = dto.MessageTemplateId,
        }.StampNew(Tenant, userId);

        Db.ConversationMessages.Add(message);

        conversation.MessageCount++;
        conversation.LastMessageAt = message.SentAt;
        conversation.AwaitingReply = false;
        conversation.UnreadCount = 0;
        conversation.StampUpdated(userId);

        if (dto.AttachDocumentId is not null)
        {
            var document = await Db.GeneratedDocuments.ForCompany(Tenant)
                .FirstOrDefaultAsync(d => d.Id == dto.AttachDocumentId);

            if (document is not null)
            {
                document.IsSent = true;
                document.SentAt = DateTime.UtcNow;
                document.SentVia = dto.Channel;
                document.StampUpdated(userId);

                message.MediaUrl ??= document.Url;
                message.MediaType ??= "application/pdf";
            }
        }

        await Db.SaveChangesAsync();

        var senders = await AgentUserNamesAsync([userId]);

        return new ConversationMessageDto
        {
            Id = message.Id,
            Direction = message.Direction,
            SentAt = message.SentAt,
            Body = message.Body,
            MediaUrl = message.MediaUrl,
            MediaType = message.MediaType,
            Caption = message.Caption,
            SentByName = senders.GetValueOrDefault(userId),
            IsAutomated = false,
        };
    }

    /// <summary>
    /// Why a message to this person on this channel must not go out, or null if it may.
    ///
    /// Three tests, in the order that matters: consent, then quiet hours, then the daily cap. Each
    /// returns a sentence a person can act on rather than a code.
    /// </summary>
    private async Task<string?> SuppressionReasonAsync(
        Guid partyId, NotificationChannel channel, RealEstateSettings settings)
    {
        var consent = await Db.PartyConsents.ForCompany(Tenant)
            .Where(c => c.PartyId == partyId && c.Channel == channel)
            .OrderByDescending(c => c.RecordedAt)
            .FirstOrDefaultAsync();

        if (consent is not null && (!consent.IsGranted || consent.WithdrawnAt is not null))
            return $"This contact has withdrawn consent to be contacted by {channel}.";

        var caution = await Db.CautionListEntries.ForCompany(Tenant)
            .AnyAsync(c => c.PartyId == partyId && c.IsActive && c.ClearedOn == null && c.BlocksNewBusiness);

        if (caution)
            return "This contact is on the caution list with new business blocked. Clear the entry first.";

        var hour = DateTime.UtcNow.Hour;

        var inQuietHours = settings.QuietHoursStart > settings.QuietHoursEnd
            ? hour >= settings.QuietHoursStart || hour < settings.QuietHoursEnd
            : hour >= settings.QuietHoursStart && hour < settings.QuietHoursEnd;

        // In-app never wakes anybody up, so quiet hours do not apply to it.
        if (inQuietHours && channel != NotificationChannel.InApp)
            return $"It is quiet hours ({settings.QuietHoursStart:00}:00–{settings.QuietHoursEnd:00}:00). "
                 + "Schedule this for the morning instead.";

        var since = DateTime.UtcNow.AddDays(-1);

        var sentToday = await Db.NotificationLogs.ForCompany(Tenant)
            .CountAsync(n => n.RecipientPartyId == partyId && n.QueuedAt >= since && !n.WasSuppressed);

        if (sentToday >= settings.MaxAutomatedMessagesPerDay)
            return $"This contact has already had {sentToday} messages in the last 24 hours, which is the daily limit.";

        return null;
    }

    private static string Merge(string body, Dictionary<string, string> values)
        => Regex.Replace(body, @"\{\{\s*([A-Za-z0-9_.]+)\s*\}\}",
            m => values.TryGetValue(m.Groups[1].Value, out var value) ? value : m.Value);

    private static List<string> MergeFieldsIn(string body)
        => Regex.Matches(body, @"\{\{\s*([A-Za-z0-9_.]+)\s*\}\}")
            .Select(m => m.Groups[1].Value)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

    public async Task<ConversationDto> AssignConversationAsync(Guid id, Guid userId2, Guid userId)
    {
        var conversation = await RequireAsync<Conversation>(id, "That conversation does not exist.");

        if (conversation.Status == "Closed")
            throw new InvalidOperationException("That conversation is closed. Reopen it before reassigning.");

        conversation.AssignedToUserId = userId2;
        conversation.StampUpdated(userId);

        await QueueNotificationAsync(
            "conversation.assigned",
            "A conversation has been assigned to you",
            conversation.Subject ?? $"{conversation.Channel} conversation",
            $"/realestate/inbox/{conversation.Id}",
            recipientUserId: userId2,
            entityType: nameof(Conversation),
            entityId: conversation.Id);

        await Db.SaveChangesAsync();

        return (await GetConversationAsync(conversation.Id))!;
    }

    public async Task<ConversationDto> CloseConversationAsync(Guid id, Guid userId)
    {
        var conversation = await RequireAsync<Conversation>(id, "That conversation does not exist.");

        // Closing a thread the customer is still waiting on is how complaints become reviews.
        if (conversation.AwaitingReply)
            throw new InvalidOperationException(
                "The customer's last message has not been answered. Reply before closing, or reassign it.");

        conversation.Status = "Closed";
        conversation.UnreadCount = 0;
        conversation.StampUpdated(userId);

        await Db.SaveChangesAsync();

        return (await GetConversationAsync(conversation.Id))!;
    }

    // ═══ Message templates ═══════════════════════════════════════════════════

    public async Task<List<MessageTemplateDto>> GetMessageTemplatesAsync(NotificationChannel? channel, string? category)
    {
        var templates = await Db.MessageTemplates.ForCompany(Tenant)
            .WhereIf(channel.HasValue, t => t.Channel == channel)
            .WhereIf(!string.IsNullOrWhiteSpace(category), t => t.Category == category)
            .OrderBy(t => t.Category)
            .ThenBy(t => t.Name)
            .ToListAsync();

        if (templates.Count == 0) return [];

        var usage = await Db.ConversationMessages.ForCompany(Tenant)
            .Where(m => m.MessageTemplateId != null && templates.Select(t => t.Id).Contains(m.MessageTemplateId.Value))
            .GroupBy(m => m.MessageTemplateId!.Value)
            .Select(g => new { TemplateId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.TemplateId, x => x.Count);

        return templates.Select(t => new MessageTemplateDto
        {
            Id = t.Id,
            Code = t.Code ?? string.Empty,
            Name = t.Name,
            Channel = t.Channel,
            LanguageCode = t.LanguageCode,
            IsRightToLeft = t.IsRightToLeft,
            Subject = t.Subject,
            Body = t.Body,
            ProviderTemplateName = t.ProviderTemplateName,
            IsProviderApproved = t.IsProviderApproved,
            MergeFields = MergeFieldsIn(t.Body),
            Version = t.Version,
            IsActive = t.IsActive,
            Category = t.Category,
            SentCount = usage.GetValueOrDefault(t.Id),
        }).ToList();
    }

    public async Task<MessageTemplateDto> SaveMessageTemplateAsync(MessageTemplateDto dto, Guid userId)
    {
        var isNew = dto.Id == Guid.Empty;

        var template = isNew
            ? new MessageTemplate { Code = await numbering.NextMasterCodeAsync(Db.MessageTemplates, "MSG") }
            : await Db.MessageTemplates.ForCompany(Tenant).FirstOrDefaultAsync(t => t.Id == dto.Id)
              ?? throw new InvalidOperationException("That template does not exist.");

        if (string.IsNullOrWhiteSpace(dto.Body))
            throw new InvalidOperationException("A message template needs a body.");

        if (dto.Channel == NotificationChannel.Sms && dto.Body.Length > 1000)
            throw new InvalidOperationException(
                $"That is {dto.Body.Length} characters, which is several text messages and several times the cost. "
                + "Shorten it, or send it by another channel.");

        // Changing the wording of a provider-approved template silently invalidates the approval,
        // so the flag is cleared and the sender is told rather than finding out at send time.
        if (!isNew && template.Body != dto.Body && template.IsProviderApproved)
        {
            template.IsProviderApproved = false;
            template.Version++;
        }
        else if (isNew)
        {
            template.Version = 1;
        }

        template.Name = dto.Name;
        template.Channel = dto.Channel;
        template.LanguageCode = dto.LanguageCode;
        template.IsRightToLeft = dto.IsRightToLeft;
        template.Subject = dto.Subject;
        template.Body = dto.Body;
        template.ProviderTemplateName = dto.ProviderTemplateName;
        template.MergeFieldsJson = System.Text.Json.JsonSerializer.Serialize(MergeFieldsIn(dto.Body));
        template.Category = dto.Category;
        template.IsActive = dto.IsActive;

        if (isNew)
        {
            template.StampNew(Tenant, userId);
            Db.MessageTemplates.Add(template);
        }
        else
        {
            template.StampUpdated(userId);
        }

        await Db.SaveChangesAsync();

        return (await GetMessageTemplatesAsync(template.Channel, template.Category)).First(t => t.Id == template.Id);
    }
}
