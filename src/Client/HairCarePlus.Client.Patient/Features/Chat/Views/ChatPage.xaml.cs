using Microsoft.Maui.Controls;
using HairCarePlus.Client.Patient.Features.Chat.ViewModels;

namespace HairCarePlus.Client.Patient.Features.Chat.Views;

public partial class ChatPage : ContentPage
{
    private readonly ChatViewModel _viewModel;

    public ChatPage(ChatViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;

        // Set up menu items
        var takePhotoItem = new MenuFlyoutItem
        {
            Text = "Take Photo",
            IconImageSource = new FontImageSource
            {
                FontFamily = "FontAwesome",
                Glyph = "📷",
                Color = Colors.Black
            },
            Command = _viewModel.OpenCameraCommand
        };

        var choosePhotoItem = new MenuFlyoutItem
        {
            Text = "Choose Photo",
            IconImageSource = new FontImageSource
            {
                FontFamily = "FontAwesome",
                Glyph = "🖼",
                Color = Colors.Black
            },
            Command = _viewModel.ChoosePhotoCommand
        };

        AttachmentMenu.AddMenuItem(takePhotoItem);
        AttachmentMenu.AddMenuItem(choosePhotoItem);
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is ChatViewModel viewModel)
        {
            await viewModel.LoadDataCommand.ExecuteAsync(null);
        }
    }
} 