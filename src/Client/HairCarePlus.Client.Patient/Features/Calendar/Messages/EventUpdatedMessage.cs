using System;
using CommunityToolkit.Mvvm.Messaging.Messages;

namespace HairCarePlus.Client.Patient.Features.Calendar.Messages
{
    public sealed class EventUpdatedMessage : ValueChangedMessage<Guid>
    {
        public EventUpdatedMessage(Guid value) : base(value) { }
    }
} 