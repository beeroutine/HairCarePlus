using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using AndroidX.Core.View;

namespace HairCarePlus.Client.Patient;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnCreate(Bundle savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        MakeEdgeToEdge();
    }

    /// <summary>
    /// Полностью скрываем системные кнопки Android и создаем Instagram-стиль с нашим TabBar внизу
    /// </summary>
    void MakeEdgeToEdge()
    {
        var window = Window;

        // 1. Разрешаем окну занимать всю область экрана
        WindowCompat.SetDecorFitsSystemWindows(window, false);

        // 2. Полностью скрываем системную навигацию
        var controller = WindowCompat.GetInsetsController(window, window.DecorView);
        if (controller != null)
        {
            // Скрываем системные кнопки навигации полностью
            controller.Hide(WindowInsetsCompat.Type.NavigationBars());
            
            // Устанавливаем поведение: показывать системные кнопки только при свайпе от края
            controller.SystemBarsBehavior = WindowInsetsControllerCompat.BehaviorShowTransientBarsBySwipe;
        }

        // 3. Делаем status bar прозрачным
        window.SetStatusBarColor(Android.Graphics.Color.Transparent);
        window.SetNavigationBarColor(Android.Graphics.Color.Transparent);
        
        // 4. Устанавливаем layout для работы с вырезами экрана (notch)
        if (Build.VERSION.SdkInt >= BuildVersionCodes.P)
        {
            window.Attributes.LayoutInDisplayCutoutMode = LayoutInDisplayCutoutMode.ShortEdges;
        }
        
        // 5. Настраиваем светлые/темные иконки status bar
        UpdateSystemBarsAppearance();
    }

    protected override void OnResume()
    {
        base.OnResume();
        // Переустанавливаем edge-to-edge режим при возврате в приложение
        MakeEdgeToEdge();
    }

    void UpdateSystemBarsAppearance()
    {
        var isDarkTheme = (Resources.Configuration.UiMode & Android.Content.Res.UiMode.NightMask) == Android.Content.Res.UiMode.NightYes;
        
        var window = Window;
        var controller = WindowCompat.GetInsetsController(window, window.DecorView);
        
        if (controller != null)
        {
            // Устанавливаем цвет иконок в status bar
            controller.AppearanceLightStatusBars = !isDarkTheme;
        }
    }
}
