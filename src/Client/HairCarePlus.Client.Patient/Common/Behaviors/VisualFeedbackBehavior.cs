using System;
using System.Linq;
using Microsoft.Maui.Controls;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace HairCarePlus.Client.Patient.Common.Behaviors
{
    public class VisualFeedbackBehavior : Behavior<Border>
    {
        private static readonly ILogger<VisualFeedbackBehavior> _logger =
            ServiceHelper.GetService<ILogger<VisualFeedbackBehavior>>() ?? NullLogger<VisualFeedbackBehavior>.Instance;

        protected override void OnAttachedTo(Border bindable)
        {
            base.OnAttachedTo(bindable);
            
            // Подписываемся на изменения в GestureRecognizers
            bindable.ChildAdded += OnFrameChildAdded;
            
            // Проверяем существующие распознаватели жестов
            foreach (var gesture in bindable.GestureRecognizers.ToList())
            {
                if (gesture is TapGestureRecognizer tapGesture)
                {
                    tapGesture.Tapped -= OnTapped;
                    tapGesture.Tapped += OnTapped;
                }
            }
            
            _logger.LogDebug("Attached to Frame");
        }

        protected override void OnDetachingFrom(Border bindable)
        {
            bindable.ChildAdded -= OnFrameChildAdded;
            
            // Отписываемся от всех жестов
            foreach (var gesture in bindable.GestureRecognizers.ToList())
            {
                if (gesture is TapGestureRecognizer tapGesture)
                {
                    tapGesture.Tapped -= OnTapped;
                }
            }
            
            base.OnDetachingFrom(bindable);
            _logger.LogDebug("Detached from Frame");
        }
        
        private void OnFrameChildAdded(object? sender, ElementEventArgs? args)
        {
            if (args?.Element is TapGestureRecognizer tapGesture)
            {
                tapGesture.Tapped -= OnTapped;
                tapGesture.Tapped += OnTapped;
                _logger.LogDebug("Added new TapGestureRecognizer");
            }
        }

        private async void OnTapped(object? sender, EventArgs? e)
        {
            if (sender == null) return;
            try
            {
                Border? border = null;
                if (sender is TapGestureRecognizer tap && tap.Parent is Border parentBorder)
                {
                    border = parentBorder;
                }
                else if (sender is Border b)
                {
                    border = b;
                }
                if (border == null)
                {
                    _logger.LogDebug("Could not determine Border from sender");
                    return;
                }
                double originalScale = border.Scale;
                await border.ScaleTo(0.95, 50, Easing.CubicOut);
                await border.ScaleTo(originalScale, 150, Easing.SpringOut);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "VisualFeedbackBehavior error");
            }
        }
    }
} 