namespace HairCarePlus.Client.Patient.Common.Behaviors;

public static class ServiceHelper
{
    public static IServiceProvider Services { get; private set; }

    public static void Initialize(IServiceProvider serviceProvider)
    {
        Services = serviceProvider;
    }

    public static T GetService<T>() where T : class
    {
        if (Services == null)
        {
            throw new InvalidOperationException("ServiceHelper is not initialized");
        }

        return Services.GetService(typeof(T)) as T;
    }
} 