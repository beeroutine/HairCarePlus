using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Devices;

namespace HairCarePlus.Client.Patient.Common.Views
{
    public class ProgressRing : ContentView
    {
        public static readonly BindableProperty ProgressProperty =
            BindableProperty.Create(nameof(Progress), typeof(double), typeof(ProgressRing),
                0d, propertyChanged: (bindable, oldValue, newValue) => ((ProgressRing)bindable).Invalidate());

        public double Progress
        {
            get => (double)GetValue(ProgressProperty);
            set => SetValue(ProgressProperty, value);
        }

        private bool _isPulsing = false;

        public static readonly BindableProperty ThicknessProperty =
            BindableProperty.Create(nameof(Thickness), typeof(double), typeof(ProgressRing),
                20d, propertyChanged: (bindable, oldValue, newValue) => ((ProgressRing)bindable).Invalidate());

        public double Thickness
        {
            get => (double)GetValue(ThicknessProperty);
            set => SetValue(ThicknessProperty, value);
        }

        readonly GraphicsView _ringView;
        readonly GraphicsView _fireworkView;
        readonly RingDrawable _ringDrawable;
        readonly FireworkDrawable _fwDrawable;

        public event EventHandler? Completed;

        public ProgressRing()
        {
            _ringDrawable = new RingDrawable(this);
            _ringView = new GraphicsView { Drawable = _ringDrawable, BackgroundColor = Colors.Transparent };
            // Card-style shadow for floating effect
            _ringView.Shadow = new Shadow
            {
                Brush = new SolidColorBrush(Application.Current.RequestedTheme == AppTheme.Dark
                    ? (Color)Application.Current.Resources["CardShadowDark"]
                    : (Color)Application.Current.Resources["CardShadowLight"]),
                Offset = new Point(0, 2),
                Radius = 12,
                Opacity = 0.15f
            };

            _fwDrawable = new FireworkDrawable(this);
            _fireworkView = new GraphicsView { Drawable = _fwDrawable, InputTransparent = true, IsVisible = false, BackgroundColor = Colors.Transparent };

            Content = new Grid { Children = { _ringView, _fireworkView } };

            // react on system theme switch
            Application.Current.RequestedThemeChanged += OnThemeChanged;
        }

        void Invalidate() => _ringView.Invalidate();

        void OnThemeChanged(object? sender, AppThemeChangedEventArgs e)
        {
            // update card-style shadow to match theme
            _ringView.Shadow = new Shadow
            {
                Brush = new SolidColorBrush(e.RequestedTheme == AppTheme.Dark
                    ? (Color)Application.Current.Resources["CardShadowDark"]
                    : (Color)Application.Current.Resources["CardShadowLight"]),
                Offset = new Point(0, 2),
                Radius = 12,
                Opacity = 0.15f
            };

            // redraw ring with palette
            _ringView.Invalidate();
        }

        protected override void OnParentSet()
        {
            base.OnParentSet();
            // detach event when removed from visual tree
            if (Parent == null)
            {
                Application.Current.RequestedThemeChanged -= OnThemeChanged;
            }
        }

        public Task AnimateToAsync(double target, uint duration = 700)
        {
            var from = Math.Clamp(Progress, 0, 1);
            var tcs = new TaskCompletionSource<bool>();
            
            // Start pulse animation when approaching completion
            if (target >= 0.9 && !_isPulsing)
            {
                StartPulseAnimation();
            }
            else if (target < 0.9 && _isPulsing)
            {
                StopPulseAnimation();
            }

            // Smooth linear animation
            var anim = new Animation(v => Progress = v, from, target, Easing.Linear);

            anim.Commit(this, "RingAnim", 16, duration, finished: (_, __) =>
            {
                if (target >= 1.0)
                {
                    // haptic feedback on complete
                    HapticFeedback.Default.Perform(HapticFeedbackType.Click);
                    StopPulseAnimation();
                    TriggerFirework();
                    Completed?.Invoke(this, EventArgs.Empty);
                }
                tcs.SetResult(true);
            });

            return tcs.Task;
        }

        private void StartPulseAnimation()
        {
            _isPulsing = true;
            // Pulsing is handled via invalidation 
            Dispatcher.StartTimer(TimeSpan.FromMilliseconds(50), () =>
            {
                if (_isPulsing)
                {
                    _ringView.Invalidate();
                }
                return _isPulsing;
            });
        }

        private void StopPulseAnimation()
        {
            _isPulsing = false;
            _ringView.Invalidate();
        }

        void TriggerFirework()
        {
            _fwDrawable.Reset();
            _fireworkView.IsVisible = true;

            new Animation(v =>
            {
                _fwDrawable.Tick(v);
                _fireworkView.Invalidate();
            }, 0, 1, Easing.SinOut)
            .Commit(this, "Firework", 16, 350, finished: (_, __) => _fireworkView.IsVisible = false);
        }

        internal Color RingColor =>
            Application.Current.RequestedTheme == AppTheme.Dark
                ? (Color)Application.Current.Resources["RingPrimaryDark"]
                : (Color)Application.Current.Resources["RingPrimaryLight"];

        // Base card color used for firework palette
        internal Color CardColor =>
            Application.Current.RequestedTheme == AppTheme.Dark
                ? (Color)Application.Current.Resources["TaskCardBackgroundDark"]
                : (Color)Application.Current.Resources["TaskCardBackgroundLight"];

        internal bool IsPulsing => _isPulsing;
    }
} 