using System.Windows.Input;
using Microsoft.Maui.Controls;
using HairCarePlus.Client.Clinic.Features.Chat.Models;

namespace HairCarePlus.Client.Clinic.Common.Views;

public partial class ChatMessageBubble : ContentView
{
    public static readonly BindableProperty ReplyCommandProperty = BindableProperty.Create(
        nameof(ReplyCommand), typeof(ICommand), typeof(ChatMessageBubble));

    public static readonly BindableProperty MessageProperty = BindableProperty.Create(
        nameof(Message), typeof(ChatMessage), typeof(ChatMessageBubble), propertyChanged: OnMessageChanged);

    public ICommand? ReplyCommand
    {
        get => (ICommand?)GetValue(ReplyCommandProperty);
        set => SetValue(ReplyCommandProperty, value);
    }

    public ChatMessage? Message
    {
        get => (ChatMessage?)GetValue(MessageProperty);
        set => SetValue(MessageProperty, value);
    }

    public ChatMessageBubble()
    {
        InitializeComponent();
    }

    private static void OnMessageChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is ChatMessageBubble bubble)
        {
            // Rebind internal layout to new message
            bubble.Content.BindingContext = newValue;
        }
    }
}


