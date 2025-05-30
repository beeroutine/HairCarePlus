using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Runtime;
using Android.Views;
using AndroidX.Core.View;

namespace HairCarePlus.Client.Patient;

[Activity(Theme = "@style/MainTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density,
         WindowSoftInputMode = SoftInput.AdjustResize | SoftInput.StateHidden)]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnCreate(Bundle savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        // Allow content to draw behind system bars for an edge-to-edge experience
        WindowCompat.SetDecorFitsSystemWindows(Window, false);

        // Make status bar and navigation bar backgrounds transparent
        Window.SetStatusBarColor(Android.Graphics.Color.Transparent);
        Window.SetNavigationBarColor(Android.Graphics.Color.Transparent);

        ApplyImmersiveMode();
    }

    public override void OnWindowFocusChanged(bool hasFocus)
    {
        base.OnWindowFocusChanged(hasFocus);
        if (hasFocus)
        {
            ApplyImmersiveMode();
        }
    }

    private void ApplyImmersiveMode()
    {
#if ANDROID30_0_OR_GREATER
        var insetsController = WindowCompat.GetInsetsController(Window, Window.DecorView);
        if (insetsController != null)
        {
            // Hide only the navigation bars (system gestures, back, home, recents)
            insetsController.Hide(WindowInsetsCompat.Type.NavigationBars());
            // When system bars are revealed by swiping, they are transient
            insetsController.SystemBarsBehavior = WindowInsetsControllerCompat.BehaviorShowTransientBarsBySwipe;
        }
#else
        #pragma warning disable CS0618 // Type or member is obsolete
        Window.DecorView.SystemUiVisibility = (StatusBarVisibility)(
            SystemUiFlags.LayoutFullscreen |
            SystemUiFlags.LayoutStable |
            SystemUiFlags.HideNavigation |   // Hide navigation bar
            SystemUiFlags.ImmersiveSticky
        );
        #pragma warning restore CS0618 // Type or member is obsolete
#endif
    }
}
