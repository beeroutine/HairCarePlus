using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using HairCarePlus.Client.Patient.Features.Chat.Domain.Repositories;
using HairCarePlus.Client.Patient.Infrastructure.Features.Chat.Repositories;
using HairCarePlus.Client.Patient.Features.Chat.Services;
using HairCarePlus.Client.Patient.Infrastructure.Connectivity;
using HairCarePlus.Client.Patient.Infrastructure.Storage;
using HairCarePlus.Shared.Common.CQRS;
using System.Collections.Generic;

using Commands = HairCarePlus.Client.Patient.Features.Chat.Application.Commands;
using QueriesNs = HairCarePlus.Client.Patient.Features.Chat.Application.Queries;

namespace HairCarePlus.Client.Patient.Features.Chat;

public static class DependencyInjection
{
    public static IServiceCollection AddChatFeature(this IServiceCollection services)
    {
        // Infrastructure
        services.AddSingleton<IConnectivityService, ConnectivityService>();
        
        // Database
        services.AddPooledDbContextFactory<AppDbContext>(options =>
        {
            var dbPath = Path.Combine(FileSystem.AppDataDirectory, "chat.db");
            options.UseSqlite($"Data Source={dbPath}");
        });
        
        // Repositories
        services.AddScoped<IChatRepository, ChatRepository>();
        
        // Services
        services.AddScoped<IChatSyncService, ChatSyncService>();

        // CQRS
        services.AddCqrs();
        services.AddScoped<ICommandHandler<Commands.SendChatMessageCommand>, Commands.SendChatMessageHandler>();
        services.AddScoped<IQueryHandler<QueriesNs.GetChatMessagesQuery, IReadOnlyList<HairCarePlus.Shared.Communication.ChatMessageDto>>, QueriesNs.GetChatMessagesHandler>();
        services.AddScoped<ICommandHandler<Commands.UpdateChatMessageCommand>, Commands.UpdateChatMessageHandler>();
        services.AddScoped<ICommandHandler<Commands.DeleteChatMessageCommand>, Commands.DeleteChatMessageHandler>();
        services.AddScoped<ICommandHandler<Commands.SendChatImageCommand>, Commands.SendChatImageHandler>();

        return services;
    }
} 