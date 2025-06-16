using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using AndroidX.Core.View;

namespace HairCarePlus.Client.Clinic;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnCreate(Bundle savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        MakeEdgeToEdge();
    }

    protected override void OnResume()
    {
        base.OnResume();
        UpdateStatusbarAndNavigationBarColors();
    }

    /// <summary>
    /// Прячем status-bar и navigation-bar, разрешаем рисовать под ними.
    /// </summary>
    void MakeEdgeToEdge()
    {
        var window = Window;

        // 1. Разрешаем окну занимать всю область, включая system bars
        WindowCompat.SetDecorFitsSystemWindows(window, false);

#if ANDROID33_OR_GREATER   // API 33+
        var controller = WindowCompat.GetInsetsController(window, window.DecorView);
        if (controller != null)
        {
            controller.Hide(WindowInsetsCompat.Type.SystemBars());
            controller.SystemBarsBehavior = WindowInsetsControllerCompat.BehaviorShowTransientBarsBySwipe;
        }
#else                      // API 21-32
        const SystemUiFlags flags =
            SystemUiFlags.HideNavigation |
            SystemUiFlags.Fullscreen     |
            SystemUiFlags.ImmersiveSticky;

        window.DecorView.SystemUiVisibility = (StatusBarVisibility)
            ((int)window.DecorView.SystemUiVisibility | (int)flags);
#endif

        // 2. Сделаем панели прозрачными, чтобы, когда пользователь их «вытащит» свайпом,
        //    под ними была всё та же белая (или любая ваша) подложка.
        UpdateStatusbarAndNavigationBarColors();
    }

    void UpdateStatusbarAndNavigationBarColors()
    {
        var isDarkTheme = (Resources.Configuration.UiMode & Android.Content.Res.UiMode.NightMask) == Android.Content.Res.UiMode.NightYes;
        var color = isDarkTheme ? Android.Graphics.Color.ParseColor("#121212") : Android.Graphics.Color.White;
        Window.SetStatusBarColor(color);
        Window.SetNavigationBarColor(color);
    }
}
