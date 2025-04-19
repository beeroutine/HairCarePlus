using Microsoft.Maui.Controls;
using HairCarePlus.Client.Patient.Features.Chat.ViewModels;
using Microsoft.Extensions.Logging;

namespace HairCarePlus.Client.Patient.Features.Chat.Views;

public partial class ChatPage : ContentPage
{
    private readonly ChatViewModel _viewModel;
    private readonly ILogger<ChatPage> _logger;

    public ChatPage(ChatViewModel viewModel, ILogger<ChatPage> logger)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _logger = logger;
        BindingContext = _viewModel;
        
        _logger.LogDebug("ChatPage constructed");

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

        // Передаем ссылку на CollectionView в ViewModel для возможности прокрутки
        _viewModel.MessagesCollectionView = MessagesCollection;
        
        // Добавляем обработчик событий для отладки взаимодействия с сообщениями
        MessagesCollection.SelectionChanged += (sender, e) =>
        {
            _logger.LogDebug("MessagesCollection SelectionChanged");
        };
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _logger.LogDebug("ChatPage OnAppearing");
        
        if (_viewModel != null)
        {
            await _viewModel.LoadDataCommand.ExecuteAsync(null);
            _logger.LogDebug("ChatPage data loaded");

            // Убедимся, что ViewModel имеет доступ к CollectionView
            if (_viewModel.MessagesCollectionView == null)
            {
                _viewModel.MessagesCollectionView = MessagesCollection;
                _logger.LogDebug("MessagesCollectionView assigned in OnAppearing");
            }
        }
    }
} 