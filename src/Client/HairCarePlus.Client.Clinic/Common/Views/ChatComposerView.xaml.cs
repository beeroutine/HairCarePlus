using System.Windows.Input;
using Microsoft.Maui.Controls;

namespace HairCarePlus.Client.Clinic.Common.Views;

public partial class ChatComposerView : ContentView
{
    public static readonly BindableProperty MessageTextProperty = BindableProperty.Create(
        nameof(MessageText), typeof(string), typeof(ChatComposerView), defaultBindingMode: BindingMode.TwoWay);

    public static readonly BindableProperty SendCommandProperty = BindableProperty.Create(
        nameof(SendCommand), typeof(ICommand), typeof(ChatComposerView));

    public static readonly BindableProperty CancelReplyCommandProperty = BindableProperty.Create(
        nameof(CancelReplyCommand), typeof(ICommand), typeof(ChatComposerView));

    public static readonly BindableProperty ReplyToProperty = BindableProperty.Create(
        nameof(ReplyTo), typeof(object), typeof(ChatComposerView));

    public static readonly BindableProperty ReplyHeaderTextProperty = BindableProperty.Create(
        nameof(ReplyHeaderText), typeof(string), typeof(ChatComposerView), default(string));

    public static readonly BindableProperty ReplyPreviewTextProperty = BindableProperty.Create(
        nameof(ReplyPreviewText), typeof(string), typeof(ChatComposerView), default(string));

    public string? MessageText
    {
        get => (string?)GetValue(MessageTextProperty);
        set => SetValue(MessageTextProperty, value);
    }

    public ICommand? SendCommand
    {
        get => (ICommand?)GetValue(SendCommandProperty);
        set => SetValue(SendCommandProperty, value);
    }

    public ICommand? CancelReplyCommand
    {
        get => (ICommand?)GetValue(CancelReplyCommandProperty);
        set => SetValue(CancelReplyCommandProperty, value);
    }

    public object? ReplyTo
    {
        get => GetValue(ReplyToProperty);
        set => SetValue(ReplyToProperty, value);
    }

    public string? ReplyHeaderText
    {
        get => (string?)GetValue(ReplyHeaderTextProperty);
        set => SetValue(ReplyHeaderTextProperty, value);
    }

    public string? ReplyPreviewText
    {
        get => (string?)GetValue(ReplyPreviewTextProperty);
        set => SetValue(ReplyPreviewTextProperty, value);
    }

    public ChatComposerView()
    {
        InitializeComponent();
    }
}


