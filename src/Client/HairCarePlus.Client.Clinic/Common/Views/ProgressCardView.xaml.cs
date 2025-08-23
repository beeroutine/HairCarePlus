using System.Windows.Input;
using Microsoft.Extensions.Logging;

namespace HairCarePlus.Client.Clinic.Common.Views;

public partial class ProgressCardView : ContentView
{
    private readonly ILogger<ProgressCardView>? _logger;
    
    public static readonly BindableProperty CommentCommandProperty = BindableProperty.Create(
        nameof(CommentCommand), typeof(ICommand), typeof(ProgressCardView));

    public ICommand? CommentCommand
    {
        get => (ICommand?)GetValue(CommentCommandProperty);
        set => SetValue(CommentCommandProperty, value);
    }

    public ProgressCardView() 
    {
        InitializeComponent();
        try
        {
            _logger = Application.Current?.Handler?.MauiContext?.Services?.GetService(typeof(ILogger<ProgressCardView>)) as ILogger<ProgressCardView>;
        }
        catch { }
    }

    // Ensure event fallback triggers the command even if binding fails
    private void OnDoctorCommentTapped(object? sender, EventArgs e)
    {
        _logger?.LogInformation("[ProgressCardView] OnDoctorCommentTapped called");
        System.Diagnostics.Debug.WriteLine($"[ProgressCardView] OnDoctorCommentTapped called");
        
        var item = BindingContext;
        if (CommentCommand?.CanExecute(item) == true)
        {
            _logger?.LogInformation("[ProgressCardView] Executing CommentCommand");
            System.Diagnostics.Debug.WriteLine($"[ProgressCardView] Executing CommentCommand");
            CommentCommand.Execute(item);
            return;
        }
        
        _logger?.LogInformation("[ProgressCardView] CommentCommand not available, using fallback");
        System.Diagnostics.Debug.WriteLine($"[ProgressCardView] CommentCommand not available, using fallback");
        
        // Fallback: walk up the visual tree to find the nearest Page and its VM
        Element? current = this;
        while (current != null && current is not Page)
        {
            current = current.Parent;
        }
        var vm = (current as Page)?.BindingContext as HairCarePlus.Client.Clinic.Features.Patient.ViewModels.PatientPageViewModel;
        // If already commenting on this item, toggle off
        if (vm != null)
        {
            var typed = item as HairCarePlus.Client.Clinic.Features.Patient.Models.ProgressFeedItem;
            System.Diagnostics.Debug.WriteLine($"[ProgressCardView] VM found, typed item: {typed?.Date}, IsCommenting: {vm.IsCommenting}");
            
            if (vm.IsCommenting && ReferenceEquals(vm.CommentTarget, typed))
            {
                System.Diagnostics.Debug.WriteLine($"[ProgressCardView] Cancelling comment");
                vm.CancelCommentCommand.Execute(null);
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[ProgressCardView] Starting comment for item: {typed?.Date}");
                vm.StartCommentCommand.Execute(typed);
            }
        }
        else
        {
            System.Diagnostics.Debug.WriteLine($"[ProgressCardView] VM not found!");
        }
    }

    private async void OnSendClicked(object? sender, EventArgs e)
    {
        // Prefer the VM command, but also ensure UI deactivates immediately
        Element? current = this;
        while (current != null && current is not Page)
        {
            current = current.Parent;
        }
        var vm = (current as Page)?.BindingContext as HairCarePlus.Client.Clinic.Features.Patient.ViewModels.PatientPageViewModel;
        if (vm == null) return;

        var canSend = vm.IsSendEnabled;
        if (!canSend) return;

        // Disable editor to indicate submitting (support both possible names)
        var editor = this.FindByName<Editor>("InlineEditor") ?? this.FindByName<Editor>("CommentEditor");
        if (editor != null) editor.IsEnabled = false;
        try
        {
            await Microsoft.Maui.ApplicationModel.MainThread.InvokeOnMainThreadAsync(async () =>
            {
                // Await AsyncRelayCommand execution to surface exceptions to our try/catch
                await vm.SendCommentCommand.ExecuteAsync(null);
            });
        }
        catch (Exception)
        {
            try { await Application.Current?.MainPage?.DisplayAlert("Ошибка", "Не удалось отправить комментарий", "OK"); } catch { }
        }
        finally
        {
            // Blur and re-enable for next time; the panel will hide after VM sets IsCommenting=false
            if (editor != null)
            {
                editor.Unfocus();
                editor.IsEnabled = true;
            }
        }
    }

    private void OnEditorFocused(object? sender, FocusEventArgs e)
    {
        // When the editor gains focus, mark this card as the current comment target in the VM
        Element? current = this;
        while (current != null && current is not Page)
        {
            current = current.Parent;
        }
        var vm = (current as Page)?.BindingContext as HairCarePlus.Client.Clinic.Features.Patient.ViewModels.PatientPageViewModel;
        if (vm == null) return;
        var item = BindingContext as HairCarePlus.Client.Clinic.Features.Patient.Models.ProgressFeedItem;
        if (item == null) return;
        if (!ReferenceEquals(vm.CommentTarget, item))
        {
            vm.StartCommentCommand.Execute(item);
        }
    }
} 