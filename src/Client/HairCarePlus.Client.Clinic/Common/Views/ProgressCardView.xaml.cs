using System.Windows.Input;
namespace HairCarePlus.Client.Clinic.Common.Views;

public partial class ProgressCardView : ContentView
{
    public static readonly BindableProperty CommentCommandProperty = BindableProperty.Create(
        nameof(CommentCommand), typeof(ICommand), typeof(ProgressCardView));

    public ICommand? CommentCommand
    {
        get => (ICommand?)GetValue(CommentCommandProperty);
        set => SetValue(CommentCommandProperty, value);
    }

    public ProgressCardView() => InitializeComponent();

    // Ensure event fallback triggers the command even if binding fails
    private void OnDoctorCommentTapped(object? sender, EventArgs e)
    {
        var item = BindingContext;
        if (CommentCommand?.CanExecute(item) == true)
        {
            CommentCommand.Execute(item);
            return;
        }
        // Fallback: try to resolve VM from current page and execute directly
        var page = Application.Current?.Windows.FirstOrDefault()?.Page;
        var vm = page?.BindingContext as HairCarePlus.Client.Clinic.Features.Patient.ViewModels.PatientPageViewModel;
        vm?.StartCommentCommand.Execute(item as HairCarePlus.Client.Clinic.Features.Patient.Models.ProgressFeedItem);
    }
} 