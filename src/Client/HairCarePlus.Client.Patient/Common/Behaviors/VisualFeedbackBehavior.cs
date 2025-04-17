using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;

namespace HairCarePlus.Client.Patient.Common.Behaviors
{
    public class VisualFeedbackBehavior : Behavior<Frame>
    {
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
            
            System.Diagnostics.Debug.WriteLine($"=== VisualFeedbackBehavior: Присоединен к Frame");
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
            System.Diagnostics.Debug.WriteLine($"=== VisualFeedbackBehavior: Отсоединен от Frame");
        }
        
        private void OnFrameChildAdded(object? sender, ElementEventArgs? args)
        {
            if (args?.Element is TapGestureRecognizer tapGesture)
            {
                tapGesture.Tapped -= OnTapped;
                tapGesture.Tapped += OnTapped;
                System.Diagnostics.Debug.WriteLine("=== VisualFeedbackBehavior: Добавлен новый TapGestureRecognizer");
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
                    System.Diagnostics.Debug.WriteLine("=== VisualFeedbackBehavior: Не удалось определить Frame из sender.");
                    return;
                }
                
                // Логирование события тапа
                System.Diagnostics.Debug.WriteLine($"=== VisualFeedbackBehavior: Tapped event fired на Frame hash: {frame.GetHashCode()}");
                
                // Сохраняем исходный масштаб
                double originalScale = frame.Scale;
                
                // Быстрая анимация нажатия
                await frame.ScaleTo(0.95, 50, Easing.CubicOut);
                await frame.ScaleTo(originalScale, 150, Easing.SpringOut);
                
                // Логирование успешного завершения анимации
                System.Diagnostics.Debug.WriteLine($"=== VisualFeedbackBehavior: Animation completed for Frame hash: {frame.GetHashCode()}");
            }
            catch (Exception ex)
            {
                // Логирование ошибок
                System.Diagnostics.Debug.WriteLine($"=== VisualFeedbackBehavior ERROR: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"=== {ex.StackTrace}");
            }
        }
    }
} 