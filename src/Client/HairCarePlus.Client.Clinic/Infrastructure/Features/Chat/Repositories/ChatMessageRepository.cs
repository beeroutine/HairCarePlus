using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HairCarePlus.Client.Clinic.Features.Chat.Models;
using HairCarePlus.Client.Clinic.Infrastructure.Storage;
using HairCarePlus.Client.Clinic.Infrastructure.Storage.Repositories;
using HairCarePlus.Client.Clinic.Features.Chat.Domain;
using Microsoft.EntityFrameworkCore;
using System;
using HairCarePlus.Shared.Communication;
using HairCarePlus.Client.Clinic.Features.Sync.Domain.Entities;

namespace HairCarePlus.Client.Clinic.Infrastructure.Features.Chat.Repositories;

public class ChatMessageRepository : BaseRepository<ChatMessage>, IChatMessageRepository
{
    public ChatMessageRepository(AppDbContext ctx) : base(ctx) {}

    public override async Task AddAsync(ChatMessage entity)
    {
        await base.AddAsync(entity);

        if (entity.SyncStatus == HairCarePlus.Client.Clinic.Features.Chat.Models.SyncStatus.NotSynced)
        {
            var dto = new HairCarePlus.Shared.Communication.ChatMessageDto
            {
                LocalId = entity.LocalId,
                ConversationId = entity.ConversationId,
                SenderId = entity.SenderId,
                Content = entity.Content,
                SentAt = entity.SentAt.UtcDateTime,
                Status = HairCarePlus.Shared.Communication.MessageStatus.Sent
            };

            Context.OutboxItems.Add(new OutboxItem
            {
                EntityType = "ChatMessage",
                Payload = System.Text.Json.JsonSerializer.Serialize(dto),
                CreatedAtUtc = DateTime.UtcNow,
                Status = HairCarePlus.Shared.Communication.OutboxStatus.Pending,
                LocalEntityId = entity.LocalId.ToString()
            });
            await Context.SaveChangesAsync();
        }
    }

    public async Task<IEnumerable<ChatMessage>> GetLastMessagesAsync(int limit = 100)
    {
        return await DbSet
            .OrderByDescending(m => m.SentAt)
            .Take(limit)
            .ToListAsync();
    }
} 