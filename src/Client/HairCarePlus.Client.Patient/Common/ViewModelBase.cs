using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;

namespace HairCarePlus.Client.Patient.Common
{
    public abstract partial class ViewModelBase : ObservableObject
    {
        [ObservableProperty]
        private bool _isBusy;

        [ObservableProperty]
        private string? _title;

        [ObservableProperty]
        private string? _errorMessage;

        public virtual Task LoadDataAsync()
        {
            return Task.CompletedTask;
        }

        protected async Task ExecuteAsync(Func<Task> operation)
        {
            try
            {
                IsBusy = true;
                ErrorMessage = null;
                await operation();
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                System.Diagnostics.Debug.WriteLine($"Error: {ex}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        protected async Task<T?> ExecuteAsync<T>(Func<Task<T>> operation)
        {
            try
            {
                IsBusy = true;
                ErrorMessage = null;
                return await operation();
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                System.Diagnostics.Debug.WriteLine($"Error: {ex}");
                return default;
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
} 