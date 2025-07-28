namespace HairCarePlus.Shared.Common;

public static class EnvironmentHelper
{
    private const string DefaultBaseUrl = "http://192.168.1.6:5281";

    /// <summary>
    /// Returns base URL for HairCare+ API in the following order:
    /// 1. Value injected at build time via <see cref="BuildConfig"/> (run-script passes -p:CHAT_BASE_URL).
    /// 2. Runtime environment variable CHAT_BASE_URL (for simulator / desktop).
    /// 3. Hard-coded fallback (useful when nothing was provided).
    /// </summary>
    public static string GetBaseApiUrl()
    {
        // 1) Environment variable (desktop / simulator / mobile) – always wins to allow overriding without rebuild
        var env = Environment.GetEnvironmentVariable("CHAT_BASE_URL");
        if (!string.IsNullOrEmpty(env))
            return env;

        // 2) Same-assembly generated partial (Shared.Common) ─ works for backend/service scenarios
        if (!string.IsNullOrEmpty(BuildConfig.BaseApiUrl))
            return BuildConfig.BaseApiUrl;

        // 3) At runtime on iOS / Android the client assemblies may contain *their own* generated partial
        //    BuildConfig with the value baked in. Scan loaded assemblies to find the first non-null value.
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            try
            {
                var cfgType = asm.GetType("HairCarePlus.Shared.Common.BuildConfig", throwOnError: false, ignoreCase: false);
                if (cfgType == null || cfgType == typeof(BuildConfig))
                    continue; // skip this assembly / not found

                var prop = cfgType.GetProperty("BaseApiUrl", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                if (prop == null)
                    continue;

                var otherVal = prop.GetValue(null) as string;
                if (!string.IsNullOrEmpty(otherVal))
                    return otherVal!;
            }
            catch
            {
                // ignore reflection errors, continue search
            }
        }

        // 4) Hard-coded fallback (should never be used in production)
        return DefaultBaseUrl;
    }
} 