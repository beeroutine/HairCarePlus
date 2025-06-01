using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using SkiaSharp.Extended.UI.Controls;

namespace HairCarePlus.Client.Patient.Common.Services
{
    /// <summary>
    /// Оптимизированный менеджер конфетти анимаций
    /// </summary>
    public class ConfettiManager : IConfettiManager
    {
        private readonly ILogger<ConfettiManager> _logger;
        private readonly IDispatcher _dispatcher;
        
        // Пул частиц для переиспользования
        private readonly ParticlePool _particlePool;
        
        // Текущая анимация
        private CancellationTokenSource? _animationCts;
        private SKConfettiView? _activeConfettiView;
        
        // Настройки производительности
        private ConfettiPerformanceLevel _performanceLevel = ConfettiPerformanceLevel.Medium;
        private readonly Dictionary<ConfettiPerformanceLevel, PerformanceSettings> _performanceSettings;
        
        public bool IsAnimating => _animationCts != null && !_animationCts.IsCancellationRequested;
        
        public ConfettiManager(ILogger<ConfettiManager> logger, IDispatcher dispatcher)
        {
            _logger = logger;
            _dispatcher = dispatcher;
            _particlePool = new ParticlePool(maxPoolSize: 200);
            
            // Настройки для разных уровней производительности
            _performanceSettings = new Dictionary<ConfettiPerformanceLevel, PerformanceSettings>
            {
                [ConfettiPerformanceLevel.Low] = new PerformanceSettings 
                { 
                    MaxParticles = 30, 
                    ParticleLifetime = 2000, 
                    UpdateInterval = 50,
                    ParticleSize = 8
                },
                [ConfettiPerformanceLevel.Medium] = new PerformanceSettings 
                { 
                    MaxParticles = 60, 
                    ParticleLifetime = 3000, 
                    UpdateInterval = 33,
                    ParticleSize = 10
                },
                [ConfettiPerformanceLevel.High] = new PerformanceSettings 
                { 
                    MaxParticles = 100, 
                    ParticleLifetime = 3500, 
                    UpdateInterval = 16,
                    ParticleSize = 12
                }
            };
        }
        
        public async Task ShowConfettiAsync(int duration = 3000, int particleCount = 100)
        {
            try
            {
                // Останавливаем предыдущую анимацию
                StopConfetti();
                
                _animationCts = new CancellationTokenSource();
                var token = _animationCts.Token;
                
                var settings = _performanceSettings[_performanceLevel];
                var actualParticleCount = Math.Min(particleCount, settings.MaxParticles);
                
                _logger.LogDebug("Starting confetti animation with {ParticleCount} particles for {Duration}ms", 
                    actualParticleCount, duration);
                
                await _dispatcher.DispatchAsync(async () =>
                {
                    // Находим SKConfettiView на текущей странице
                    var currentPage = Application.Current?.MainPage;
                    if (currentPage is NavigationPage navPage)
                    {
                        currentPage = navPage.CurrentPage;
                    }
                    
                    _activeConfettiView = FindConfettiView(currentPage);
                    if (_activeConfettiView == null)
                    {
                        _logger.LogWarning("SKConfettiView not found on current page");
                        return;
                    }
                    
                    // Настраиваем и запускаем анимацию
                    ConfigureConfettiView(_activeConfettiView, settings, actualParticleCount);
                    _activeConfettiView.IsAnimationEnabled = true;
                    
                    // Автоматически останавливаем после заданного времени
                    await Task.Delay(duration, token);
                    
                    if (!token.IsCancellationRequested && _activeConfettiView != null)
                    {
                        _activeConfettiView.IsAnimationEnabled = false;
                        _logger.LogDebug("Confetti animation completed");
                    }
                });
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("Confetti animation was cancelled");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error showing confetti animation");
            }
            finally
            {
                CleanupAnimation();
            }
        }
        
        public void StopConfetti()
        {
            try
            {
                _animationCts?.Cancel();
                
                _dispatcher.Dispatch(() =>
                {
                    if (_activeConfettiView != null)
                    {
                        _activeConfettiView.IsAnimationEnabled = false;
                        _activeConfettiView = null;
                    }
                });
                
                CleanupAnimation();
                _logger.LogDebug("Confetti animation stopped");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping confetti animation");
            }
        }
        
        public void ConfigurePerformance(ConfettiPerformanceLevel level)
        {
            _performanceLevel = level;
            _logger.LogInformation("Confetti performance level set to {Level}", level);
            
            // Если анимация активна, применяем новые настройки
            if (IsAnimating && _activeConfettiView != null)
            {
                var settings = _performanceSettings[level];
                _dispatcher.Dispatch(() =>
                {
                    ConfigureConfettiView(_activeConfettiView, settings, settings.MaxParticles);
                });
            }
        }
        
        private void ConfigureConfettiView(SKConfettiView confettiView, PerformanceSettings settings, int particleCount)
        {
            // Оптимизированные настройки для производительности
            var colors = GetOptimizedColors();
            
            // Создаем систему частиц с ограничениями
            var confettiSystems = new SKConfettiSystemCollection();
            
            // Распределяем частицы по нескольким источникам для лучшей производительности
            var systemCount = Math.Min(3, Math.Max(1, particleCount / 20));
            var particlesPerSystem = particleCount / systemCount;
            
            for (int i = 0; i < systemCount; i++)
            {
                var emitterX = (float)(0.2 + (0.6 * i / systemCount)) * (float)confettiView.Width;
                var emitterY = (float)(confettiView.Height * 0.1);
                
                var currentColors = new SKConfettiColorCollection();
                foreach (var color in colors.Take(4)) // Ограничиваем количество цветов
                {
                    currentColors.Add(color);
                }
                
                var shapes = new SKConfettiShapeCollection();
                shapes.Add(new SKConfettiCircleShape());
                shapes.Add(new SKConfettiSquareShape());
                
                var physics = new SKConfettiPhysicsCollection();
                physics.Add(new SKConfettiPhysics(size: 0.8, mass: 0.7));
                
                var confettiSystem = new SKConfettiSystem
                {
                    Emitter = SKConfettiEmitter.Burst(particlesPerSystem),
                    EmitterBounds = SKConfettiEmitterBounds.Point(new Microsoft.Maui.Graphics.Point(emitterX, emitterY)),
                    Colors = currentColors,
                    Shapes = shapes,
                    Physics = physics,
                    Lifetime = settings.ParticleLifetime / 1000.0, // Convert to seconds
                    MinimumInitialVelocity = 200,
                    MaximumInitialVelocity = 400,
                    MinimumRotationVelocity = -180,
                    MaximumRotationVelocity = 180,
                    Gravity = new Microsoft.Maui.Graphics.Point(0, 100),
                    FadeOut = true,
                    StartAngle = 0,
                    EndAngle = 360
                };
                
                confettiSystems.Add(confettiSystem);
            }
            
            confettiView.Systems = confettiSystems;
        }
        
        private SKConfettiView? FindConfettiView(Page? page)
        {
            if (page == null) return null;
            
            // Рекурсивный поиск SKConfettiView в визуальном дереве
            return FindViewInVisualTree<SKConfettiView>(page);
        }
        
        private T? FindViewInVisualTree<T>(Element element) where T : View
        {
            if (element is T view) return view;
            
            if (element is Layout layout)
            {
                foreach (var child in layout.Children)
                {
                    if (child is Element childElement)
                    {
                        var result = FindViewInVisualTree<T>(childElement);
                        if (result != null) return result;
                    }
                }
            }
            else if (element is ContentView contentView && contentView.Content != null)
            {
                return FindViewInVisualTree<T>(contentView.Content);
            }
            else if (element is ContentPage contentPage && contentPage.Content != null)
            {
                return FindViewInVisualTree<T>(contentPage.Content);
            }
            else if (element is Grid grid)
            {
                foreach (var child in grid.Children)
                {
                    if (child is Element childElement)
                    {
                        var result = FindViewInVisualTree<T>(childElement);
                        if (result != null) return result;
                    }
                }
            }
            
            return null;
        }
        
        private List<Color> GetOptimizedColors()
        {
            // Ограниченный набор ярких цветов для лучшей производительности
            return new List<Color>
            {
                Color.FromRgb(255, 87, 51),   // Красный
                Color.FromRgb(255, 195, 0),   // Желтый
                Color.FromRgb(0, 230, 118),   // Зеленый
                Color.FromRgb(0, 123, 255),   // Синий
                Color.FromRgb(255, 59, 48),   // Оранжевый
                Color.FromRgb(88, 86, 214)    // Фиолетовый
            };
        }
        
        private void CleanupAnimation()
        {
            _animationCts?.Dispose();
            _animationCts = null;
            _activeConfettiView = null;
        }
        
        /// <summary>
        /// Настройки производительности
        /// </summary>
        private class PerformanceSettings
        {
            public int MaxParticles { get; set; }
            public double ParticleLifetime { get; set; }
            public int UpdateInterval { get; set; }
            public double ParticleSize { get; set; }
        }
        
        /// <summary>
        /// Пул частиц для переиспользования объектов
        /// </summary>
        private class ParticlePool
        {
            private readonly Stack<ConfettiParticle> _pool;
            private readonly int _maxPoolSize;
            
            public ParticlePool(int maxPoolSize)
            {
                _maxPoolSize = maxPoolSize;
                _pool = new Stack<ConfettiParticle>(maxPoolSize);
            }
            
            public ConfettiParticle Rent()
            {
                return _pool.Count > 0 ? _pool.Pop() : new ConfettiParticle();
            }
            
            public void Return(ConfettiParticle particle)
            {
                if (_pool.Count < _maxPoolSize)
                {
                    particle.Reset();
                    _pool.Push(particle);
                }
            }
        }
        
        /// <summary>
        /// Представление частицы конфетти
        /// </summary>
        private class ConfettiParticle
        {
            public double X { get; set; }
            public double Y { get; set; }
            public double VelocityX { get; set; }
            public double VelocityY { get; set; }
            public double Rotation { get; set; }
            public Color Color { get; set; }
            public double LifeTime { get; set; }
            
            public void Reset()
            {
                X = Y = VelocityX = VelocityY = Rotation = LifeTime = 0;
                Color = Colors.Transparent;
            }
        }
    }
} 