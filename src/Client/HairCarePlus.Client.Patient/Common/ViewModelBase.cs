using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace HairCarePlus.Client.Patient.Common
{
    public abstract class ViewModelBase : INotifyPropertyChanged
    {
        private bool _isBusy;
        private string? _title;
        private string? _errorMessage;

        public bool IsBusy
        {
            get => _isBusy;
            set => SetProperty(ref _isBusy, value);
        }

        public string? Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        public string? ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public virtual Task LoadDataAsync()
        {
            return Task.CompletedTask;
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
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