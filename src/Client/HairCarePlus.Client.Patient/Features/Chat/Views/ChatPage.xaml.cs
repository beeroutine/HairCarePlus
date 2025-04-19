using Microsoft.Maui.Controls;
using HairCarePlus.Client.Patient.Features.Chat.ViewModels;
using System.Diagnostics;

namespace HairCarePlus.Client.Patient.Features.Chat.Views;

public partial class ChatPage : ContentPage
{
    private readonly ChatViewModel _viewModel;

    public ChatPage(ChatViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
        
        // Логирование для отладки
#if DEBUG
        Debug.WriteLine("=== ChatPage: Инициализация страницы");
#endif

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
#if DEBUG
        MessagesCollection.SelectionChanged += (sender, e) => {
            Debug.WriteLine("=== SelectionChanged в CollectionView");
        };
#else
        MessagesCollection.SelectionChanged += (sender, e) => { };
#endif
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
#if DEBUG
        Debug.WriteLine("=== ChatPage: OnAppearing вызван");
#endif
        
        if (_viewModel != null)
        {
            await _viewModel.LoadDataCommand.ExecuteAsync(null);
#if DEBUG
            Debug.WriteLine("=== ChatPage: Данные загружены");
#endif

            // Убедимся, что ViewModel имеет доступ к CollectionView
            if (_viewModel.MessagesCollectionView == null)
            {
                _viewModel.MessagesCollectionView = MessagesCollection;
#if DEBUG
                Debug.WriteLine("=== ChatPage: MessagesCollectionView присвоен в OnAppearing");
#endif
            }
        }
    }
} 