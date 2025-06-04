using System;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using HairCarePlus.Client.Patient.Features.Calendar.ViewModels;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using ReactiveUI.Maui;
using SkiaSharp.Extended.UI.Controls;
using MauiApplication = Microsoft.Maui.Controls.Application;
using System.Reactive;
using Microsoft.Maui.Controls;

namespace HairCarePlus.Client.Patient.Features.Calendar.Views
{
    /// <summary>
    /// Reactive implementation of Today Page using ReactiveUI patterns
    /// </summary>
    public partial class TodayPageReactive : ReactiveContentPage<TodayViewModelReactive>
    {
        private readonly ILogger<TodayPageReactive> _logger;
        private Random _confettiRandom = new Random();

        public TodayPageReactive(TodayViewModelReactive viewModel, ILogger<TodayPageReactive> logger)
        {
            _logger = logger;
            
            try
            {
                _logger.LogDebug("TodayPageReactive constructor start");
                InitializeComponent();
                _logger.LogDebug("TodayPageReactive InitializeComponent completed");
                
                ViewModel = viewModel;
                _logger.LogDebug("ViewModel set");
                
                // Setup reactive bindings
                this.WhenActivated(disposables =>
                {
                    _logger.LogDebug("TodayPageReactive activated");
                    
                    // Bind confetti animation to progress completion
                    if (ProgressRingRef != null)
                    {
                        Observable.FromEventPattern(
                                h => ProgressRingRef.Completed += h,
                                h => ProgressRingRef.Completed -= h)
                            .Subscribe(_ => OnRingCompleted())
                            .DisposeWith(disposables);
                    }
                    
                    // Handle theme changes
                    Observable.FromEventPattern<AppThemeChangedEventArgs>(
                            h => MauiApplication.Current.RequestedThemeChanged += h,
                            h => MauiApplication.Current.RequestedThemeChanged -= h)
                        .Subscribe(args => OnThemeChanged(args.EventArgs))
                        .DisposeWith(disposables);
                    
                    // Clean up iOS/Android specific selection overlays
                    SetupPlatformSpecificHandlers(disposables);
                });
                
                _logger.LogInformation("TodayPageReactive instance created");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Exception in TodayPageReactive constructor");
            }
        }
        
        private void SetupPlatformSpecificHandlers(CompositeDisposable disposables)
        {
#if IOS
            // iOS: Clear selection overlay
            if (DateSelectorView != null)
            {
                Observable.FromEventPattern(
                        h => DateSelectorView.HandlerChanged += h,
                        h => DateSelectorView.HandlerChanged -= h)
                    .Subscribe(_ =>
                    {
                        if (DateSelectorView.Handler?.PlatformView is UIKit.UICollectionView ui)
                        {
                            // Initial clear
                            ClearIOSSelectionOverlay(ui);
                            
                            // Clear on scroll and selection
                            var scrollObs = Observable.FromEventPattern<EventHandler<ItemsViewScrolledEventArgs>, ItemsViewScrolledEventArgs>(
                                        h => DateSelectorView.Scrolled += h,
                                        h => DateSelectorView.Scrolled -= h);
                            var selectObs = Observable.FromEventPattern<EventHandler<SelectionChangedEventArgs>, SelectionChangedEventArgs>(
                                        h => DateSelectorView.SelectionChanged += h,
                                        h => DateSelectorView.SelectionChanged -= h);
                            Observable.Merge(scrollObs.Select(_ => Unit.Default), selectObs.Select(_ => Unit.Default))
                                .Subscribe(__ => ClearIOSSelectionOverlay(ui))
                                .DisposeWith(disposables);
                        }
                    })
                    .DisposeWith(disposables);
            }
#endif

#if ANDROID
            // Android: Clear selection highlight
            if (DateSelectorView != null)
            {
                Observable.FromEventPattern(
                        h => DateSelectorView.HandlerChanged += h,
                        h => DateSelectorView.HandlerChanged -= h)
                    .Subscribe(_ =>
                    {
                        if (DateSelectorView.Handler?.PlatformView is AndroidX.RecyclerView.Widget.RecyclerView rv)
                        {
                            rv.AddOnChildAttachStateChangeListener(new ClearHighlightAttachListener());
                            
                            // Clear existing children
                            for (int i = 0; i < rv.ChildCount; i++)
                            {
                                ClearAndroidHighlight(rv.GetChildAt(i));
                            }
                        }
                    })
                    .DisposeWith(disposables);
            }
#endif
        }

#if IOS
        private void ClearIOSSelectionOverlay(UIKit.UICollectionView collectionView)
        {
            foreach (var cell in collectionView.VisibleCells)
            {
                cell.SelectedBackgroundView = new UIKit.UIView { BackgroundColor = UIKit.UIColor.Clear };
            }
        }
#endif

#if ANDROID
        private static void ClearAndroidHighlight(Android.Views.View? view)
        {
            if (view == null) return;
            
            view.Foreground = null;
            view.StateListAnimator = null;
            view.SetBackgroundColor(Android.Graphics.Color.Transparent);
        }
        
        private class ClearHighlightAttachListener : Java.Lang.Object, AndroidX.RecyclerView.Widget.RecyclerView.IOnChildAttachStateChangeListener
        {
            public void OnChildViewAttachedToWindow(Android.Views.View view) => ClearAndroidHighlight(view);
            public void OnChildViewDetachedFromWindow(Android.Views.View view) { }
        }
#endif

        private void OnThemeChanged(AppThemeChangedEventArgs e)
        {
            try
            {
                _logger.LogInformation("Theme changed to: {Theme}", e.RequestedTheme);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling theme change");
            }
        }
        
        private void OnRingCompleted()
        {
            try
            {
                if (ConfettiView is SKConfettiView sk && sk.Width > 0 && sk.Height > 0)
                {
                    sk.IsAnimationEnabled = false;
                    sk.Systems.Clear();

                    var baseColors = new[]
                    {
                        Microsoft.Maui.Graphics.Colors.Gold,
                        Microsoft.Maui.Graphics.Colors.White,
                        Microsoft.Maui.Graphics.Colors.Cyan.WithAlpha(0.6f),
                        Microsoft.Maui.Graphics.Colors.LightPink.WithAlpha(0.6f),
                        Microsoft.Maui.Graphics.Colors.LightSkyBlue.WithAlpha(0.6f),
                        Microsoft.Maui.Graphics.Colors.PaleVioletRed.WithAlpha(0.6f)
                    };

                    int numberOfFireworks = _confettiRandom.Next(5, 8);

                    for (int i = 0; i < numberOfFireworks; i++)
                    {
                        // Create firework system
                        var currentFireworkColors = new SKConfettiColorCollection();
                        var shuffledColors = baseColors.OrderBy(c => _confettiRandom.Next()).ToList();
                        int colorCount = _confettiRandom.Next(2, 4);
                        
                        for (int cIdx = 0; cIdx < colorCount; cIdx++)
                        {
                            currentFireworkColors.Add(shuffledColors[cIdx % shuffledColors.Count]);
                        }

                        var shapes = new SKConfettiShapeCollection();
                        shapes.Add(new SKConfettiCircleShape());

                        // Add streak shapes
                        int streakCount = _confettiRandom.Next(2, 5);
                        for (int j = 0; j < streakCount; j++)
                        {
                            var streakPath = new SkiaSharp.SKPath();
                            streakPath.MoveTo(0, 0);
                            streakPath.LineTo(0, (float)(_confettiRandom.NextDouble() * 8.0 + 6.0));
                            shapes.Add(new SKConfettiPathShape(streakPath));
                        }

                        var physicsCollection = new SKConfettiPhysicsCollection();
                        int physicsTypeCount = _confettiRandom.Next(2, 4);
                        for (int k = 0; k < physicsTypeCount; k++)
                        {
                            physicsCollection.Add(new SKConfettiPhysics(
                                size: _confettiRandom.NextDouble() * 0.5 + 0.5,
                                mass: _confettiRandom.NextDouble() * 0.4 + 0.6));
                        }
                        
                        if (!physicsCollection.Any())
                        {
                            physicsCollection.Add(new SKConfettiPhysics(size: 0.7, mass: 0.7));
                        }

                        // Emitter position
                        float emitterX = (float)(_confettiRandom.NextDouble() * sk.Width);
                        float emitterY = (float)(_confettiRandom.NextDouble() * (sk.Height * 0.70) + (sk.Height * 0.05));
                        
                        var fireworkSystem = new SKConfettiSystem
                        {
                            Emitter = SKConfettiEmitter.Burst(_confettiRandom.Next(25, 46)),
                            EmitterBounds = SKConfettiEmitterBounds.Point(new Microsoft.Maui.Graphics.Point(emitterX, emitterY)),
                            Colors = currentFireworkColors,
                            Shapes = shapes,
                            Physics = physicsCollection,
                            Lifetime = _confettiRandom.NextDouble() * 1.2 + 1.3,
                            MinimumInitialVelocity = _confettiRandom.Next(200, 351),
                            MaximumInitialVelocity = _confettiRandom.Next(400, 701),
                            MinimumRotationVelocity = -250,
                            MaximumRotationVelocity = 250,
                            Gravity = new Microsoft.Maui.Graphics.Point(0, _confettiRandom.Next(80, 131)),
                            FadeOut = true,
                            StartAngle = 0,
                            EndAngle = 360
                        };
                        
                        if (fireworkSystem.MaximumInitialVelocity <= fireworkSystem.MinimumInitialVelocity)
                        {
                            fireworkSystem.MaximumInitialVelocity = fireworkSystem.MinimumInitialVelocity + _confettiRandom.Next(50, 101);
                        }

                        sk.Systems.Add(fireworkSystem);
                    }

                    sk.IsAnimationEnabled = true;
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error in OnRingCompleted");
            }
        }
    }
} 