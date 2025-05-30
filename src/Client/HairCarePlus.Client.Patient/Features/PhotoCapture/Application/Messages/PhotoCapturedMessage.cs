using CommunityToolkit.Mvvm.Messaging.Messages;

namespace HairCarePlus.Client.Patient.Features.PhotoCapture.Application.Messages;

/// <summary>
/// Message отправляется после успешного захвата и сохранения фото.
/// Значение – локальный путь к файлу.
/// </summary>
public sealed class PhotoCapturedMessage : ValueChangedMessage<string>
{
    public PhotoCapturedMessage(string path) : base(path) { }
} 