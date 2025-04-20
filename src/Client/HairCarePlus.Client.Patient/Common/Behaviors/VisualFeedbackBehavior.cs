using System;
using System.Linq;
using Microsoft.Maui.Controls;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace HairCarePlus.Client.Patient.Common.Behaviors
{
    public class VisualFeedbackBehavior : Behavior<Frame>
    {
        private static readonly ILogger<VisualFeedbackBehavior> _logger =
            ServiceHelper.GetService<ILogger<VisualFeedbackBehavior>>() ?? NullLogger<VisualFeedbackBehavior>.Instance;

        protected override void OnAttachedTo(Frame bindable)
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

        protected override void OnDetachingFrom(Frame bindable)
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
                // Получаем Frame
                Frame? frame = null;
                
                if (sender is TapGestureRecognizer tap && tap.Parent is Frame parentFrame)
                {
                    frame = parentFrame;
                }
                else if (sender is Frame senderFrame)
                {
                    frame = senderFrame;
                }
                
                if (frame == null)
                {
                    _logger.LogDebug("Could not determine Frame from sender");
                    return;
                }
                
                // Логирование события тапа
                _logger.LogDebug("Tapped event fired on Frame hash: {Hash}", frame.GetHashCode());
                
                // Сохраняем исходный масштаб
                double originalScale = frame.Scale;
                
                // Быстрая анимация нажатия
                await frame.ScaleTo(0.95, 50, Easing.CubicOut);
                await frame.ScaleTo(originalScale, 150, Easing.SpringOut);
                
                // Логирование успешного завершения анимации
                _logger.LogDebug("Animation completed for Frame hash: {Hash}", frame.GetHashCode());
            }
            catch (Exception ex)
            {
                // Логирование ошибок
                _logger.LogError(ex, "VisualFeedbackBehavior error");
            }
        }
    }
} 