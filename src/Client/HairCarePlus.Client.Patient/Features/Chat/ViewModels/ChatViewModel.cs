using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HairCarePlus.Client.Patient.Features.Chat.Models;
using HairCarePlus.Client.Patient.Infrastructure.Services;
using Microsoft.Maui.Controls;

namespace HairCarePlus.Client.Patient.Features.Chat.ViewModels;

public partial class ChatViewModel : ObservableObject
{
    private readonly INavigationService _navigationService;
    private readonly ILocalStorageService _localStorageService;

    [ObservableProperty]
    private ObservableCollection<ChatMessage> _messages;

    [ObservableProperty]
    private string _messageText;

    [ObservableProperty]
    private ChatMessage _replyToMessage;

    public ChatViewModel(
        INavigationService navigationService,
        ILocalStorageService localStorageService)
    {
        _navigationService = navigationService;
        _localStorageService = localStorageService;
        Messages = new ObservableCollection<ChatMessage>();
    }

    [RelayCommand]
    private async Task LoadData()
    {
        try
        {
            // TODO: Load chat messages from local storage or API
            Messages.Clear();
            Messages.Add(new ChatMessage 
            { 
                Content = "Welcome to HairCare+! How can I help you today?",
                SenderId = "doctor",
                Timestamp = DateTime.Now
            });
        }
        catch (Exception ex)
        {
            await Application.Current.MainPage.DisplayAlert("Error", "Failed to load chat messages", "OK");
        }
    }

    [RelayCommand]
    private async Task SendMessage()
    {
        if (string.IsNullOrWhiteSpace(MessageText))
            return;

        try
        {
            var message = new ChatMessage
            {
                Content = MessageText,
                SenderId = "patient",
                Timestamp = DateTime.Now,
                ReplyTo = ReplyToMessage
            };

            Messages.Add(message);
            MessageText = string.Empty;
            ReplyToMessage = null;

            // TODO: Send message to API
        }
        catch (Exception ex)
        {
            await Application.Current.MainPage.DisplayAlert("Error", "Failed to send message", "OK");
        }
    }

    [RelayCommand]
    private void ReplyToMessageCommand(ChatMessage message)
    {
        ReplyToMessage = message;
    }

    [RelayCommand]
    private void CancelReply()
    {
        ReplyToMessage = null;
    }

    [RelayCommand]
    private async Task Back()
    {
        await Shell.Current.GoToAsync("//today");
    }

    [RelayCommand]
    private async Task OpenCamera()
    {
        // TODO: Implement camera functionality
        await Application.Current.MainPage.DisplayAlert("Coming Soon", "Camera functionality will be available soon", "OK");
    }

    [RelayCommand]
    private async Task ChoosePhoto()
    {
        // TODO: Implement photo picker functionality
        await Application.Current.MainPage.DisplayAlert("Coming Soon", "Photo picker functionality will be available soon", "OK");
    }

    [RelayCommand]
    private void HideKeyboard()
    {
        // TODO: Implement keyboard hiding
    }
} 