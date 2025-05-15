using CommunityToolkit.Mvvm.Messaging.Messages;

namespace HairCarePlus.Client.Patient.Features.Progress.Application.Messages;

/// <summary>
/// Сообщение публикуется, когда список активных ограничений изменился
/// (например, после обновления календаря или синхронизации с сервером).
/// Значение не используется; достаточно факта публикации.
/// </summary>
public sealed class RestrictionsChangedMessage : ValueChangedMessage<bool>
{
    public RestrictionsChangedMessage() : base(true) { }
} 