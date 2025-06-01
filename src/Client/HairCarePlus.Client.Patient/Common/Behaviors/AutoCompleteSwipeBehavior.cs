using System.Windows.Input;
using Microsoft.Maui.Controls;

namespace HairCarePlus.Client.Patient.Common.Behaviors
{
    /// <summary>
    /// Behavior that automatically executes the provided command when the user swipes the <see cref="SwipeView"/>
    /// far enough so that it becomes <c>IsOpen == true</c>. After executing the command the swipe view is closed
    /// to provide a smooth UX similar to the native iOS Mail delete gesture.
    /// </summary>
    public sealed class AutoCompleteSwipeBehavior : Behavior<SwipeView>
    {
        private SwipeView? _associatedObject;

        public static readonly BindableProperty CompleteCommandProperty = BindableProperty.Create(
            nameof(CompleteCommand), typeof(ICommand), typeof(AutoCompleteSwipeBehavior));

        public ICommand? CompleteCommand
        {
            get => (ICommand?)GetValue(CompleteCommandProperty);
            set => SetValue(CompleteCommandProperty, value);
        }

        public static readonly BindableProperty CommandParameterProperty = BindableProperty.Create(
            nameof(CommandParameter), typeof(object), typeof(AutoCompleteSwipeBehavior));

        public object? CommandParameter
        {
            get => GetValue(CommandParameterProperty);
            set => SetValue(CommandParameterProperty, value);
        }

        protected override void OnAttachedTo(SwipeView bindable)
        {
            base.OnAttachedTo(bindable);
            _associatedObject = bindable;
            bindable.SwipeEnded += OnSwipeEnded;
        }

        protected override void OnDetachingFrom(SwipeView bindable)
        {
            base.OnDetachingFrom(bindable);
            bindable.SwipeEnded -= OnSwipeEnded;
            _associatedObject = null;
        }

        private void OnSwipeEnded(object? sender, SwipeEndedEventArgs e)
        {
            // We consider the swipe "completed" when the SwipeView is fully open (IsOpen == true)
            // for the direction that contains actionable items. For our use-case we listen for both
            // left and right directions – the UI will define which side contains the action.
            if (!e.IsOpen)
                return;

            if (CompleteCommand?.CanExecute(CommandParameter) == true)
            {
                CompleteCommand.Execute(CommandParameter);
            }

            // Close the SwipeView to provide visual feedback that the action has been applied.
            _associatedObject?.Close();
        }
    }
} 