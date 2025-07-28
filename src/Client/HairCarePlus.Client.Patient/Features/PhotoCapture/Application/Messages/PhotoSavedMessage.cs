using CommunityToolkit.Mvvm.Messaging.Messages;

namespace HairCarePlus.Client.Patient.Features.PhotoCapture.Application.Messages;

/// <summary>
/// Отправляется, когда фото сохранено локально и запись добавлена в БД.
/// Value — имя файла (без абсолютного пути).
/// </summary>
public sealed class PhotoSavedMessage : ValueChangedMessage<string>
{
    public PhotoSavedMessage(string fileName) : base(fileName) { }
} 