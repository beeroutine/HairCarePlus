namespace HairCarePlus.Client.Clinic.Features.Chat.Models;

public enum MessageType
{
    Text,
    Image,
    Video,
    File,
    System
}

public enum SyncStatus
{
    NotSynced,
    Syncing,
    Synced,
    Failed
}