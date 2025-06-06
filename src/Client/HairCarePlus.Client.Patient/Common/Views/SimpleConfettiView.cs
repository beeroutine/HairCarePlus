using Microsoft.Maui.Controls;
using System;
using System.Threading.Tasks;

namespace HairCarePlus.Client.Patient.Common.Views
{
    public class SimpleConfettiView : Grid
    {
        private bool _isAnimating = false;

        public SimpleConfettiView()
        {
            IsVisible = false;
            InputTransparent = true;
        }

        public async Task ShowConfettiAsync(int particleCount = 20, int duration = 1000)
        {
            if (_isAnimating) return;

            _isAnimating = true;
            IsVisible = true;
            Children.Clear();

            var random = new Random();
            var tasks = new Task[particleCount];

            for (int i = 0; i < particleCount; i++)
            {
                // Create emoji particles
                var emojis = new[] { "🎉", "✨", "🎊", "⭐", "💫" };
                var particle = new Label
                {
                    Text = emojis[random.Next(emojis.Length)],
                    FontSize = random.Next(16, 24),
                    HorizontalOptions = LayoutOptions.Start,
                    VerticalOptions = LayoutOptions.Start,
                    TranslationX = random.Next(-50, 50),
                    TranslationY = random.Next(-50, 50),
                    Opacity = 0
                };

                Children.Add(particle);

                // Animate each particle
                tasks[i] = AnimateParticleAsync(particle, random, duration);
            }

            await Task.WhenAll(tasks);

            IsVisible = false;
            _isAnimating = false;
            Children.Clear();
        }

        private async Task AnimateParticleAsync(Label particle, Random random, int duration)
        {
            // Random direction and speed
            var endX = random.Next(-150, 150);
            var endY = random.Next(100, 300); // Fall down
            var rotationEnd = random.Next(-720, 720);

            // Fade in
            await particle.FadeTo(1, 100);

            // Move and rotate
            var moveTask = particle.TranslateTo(
                particle.TranslationX + endX,
                particle.TranslationY + endY,
                (uint)duration,
                Easing.CubicOut);

            var rotateTask = particle.RotateTo(rotationEnd, (uint)duration, Easing.Linear);
            var fadeTask = particle.FadeTo(0, (uint)(duration * 0.8), Easing.CubicIn);

            await Task.WhenAll(moveTask, rotateTask, fadeTask);
        }
    }
} 