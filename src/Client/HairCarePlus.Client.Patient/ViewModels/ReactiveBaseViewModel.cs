using System;
using ReactiveUI;
using System.Reactive.Disposables;
using Microsoft.Extensions.Logging;

namespace HairCarePlus.Client.Patient.ViewModels
{
    /// <summary>
    /// Base class for all ViewModels using ReactiveUI pattern
    /// </summary>
    public abstract class ReactiveBaseViewModel : ReactiveObject, IActivatableViewModel
    {
        private string _title = string.Empty;
        
        /// <summary>
        /// Gets the view model activator for managing lifecycle
        /// </summary>
        public ViewModelActivator Activator { get; }
        
        /// <summary>
        /// Gets or sets the page title
        /// </summary>
        public string Title
        {
            get => _title;
            set => this.RaiseAndSetIfChanged(ref _title, value);
        }
        
        /// <summary>
        /// Logger instance for derived classes
        /// </summary>
        protected ILogger? Logger { get; }
        
        protected ReactiveBaseViewModel(ILogger? logger = null)
        {
            Logger = logger;
            Activator = new ViewModelActivator();
            
            // Setup common subscriptions
            this.WhenActivated(disposables =>
            {
                HandleActivation(disposables);
            });
        }
        
        /// <summary>
        /// Override this method to handle view model activation
        /// All subscriptions should be registered with the disposables parameter
        /// </summary>
        /// <param name="disposables">Composite disposable for managing subscriptions</param>
        protected virtual void HandleActivation(CompositeDisposable disposables)
        {
            Logger?.LogDebug("{ViewModelName} activated", GetType().Name);
        }
    }
} 