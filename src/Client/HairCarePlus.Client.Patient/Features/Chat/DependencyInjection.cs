using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using HairCarePlus.Client.Patient.Features.Chat.Domain.Repositories;
using HairCarePlus.Client.Patient.Infrastructure.Features.Chat.Repositories;
using HairCarePlus.Client.Patient.Features.Chat.Services;
using HairCarePlus.Client.Patient.Infrastructure.Connectivity;
using HairCarePlus.Client.Patient.Infrastructure.Storage;

namespace HairCarePlus.Client.Patient.Features.Chat;

public static class DependencyInjection
{
    public static IServiceCollection AddChatFeature(this IServiceCollection services)
    {
        // Infrastructure
        services.AddSingleton<IConnectivityService, ConnectivityService>();
        
        // Database
        services.AddDbContext<AppDbContext>(options =>
        {
            var dbPath = Path.Combine(FileSystem.AppDataDirectory, "chat.db");
            options.UseSqlite($"Data Source={dbPath}");
        });
        
        // Repositories
        services.AddScoped<IChatRepository, ChatRepository>();
        
        // Services
        services.AddScoped<IChatSyncService, ChatSyncService>();
        
        return services;
    }
} 