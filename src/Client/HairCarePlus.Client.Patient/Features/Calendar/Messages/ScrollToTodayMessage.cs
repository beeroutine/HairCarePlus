using CommunityToolkit.Mvvm.Messaging.Messages;

namespace HairCarePlus.Client.Patient.Features.Calendar.Messages;

// A simple message class to signal the view to scroll to today's date.
// It doesn't need to carry any data, its type is enough.
public class ScrollToTodayMessage : ValueChangedMessage<bool>
{
    // Using ValueChangedMessage<bool> just to have a base class, the value itself isn't used.
    public ScrollToTodayMessage() : base(true) { } 
} 