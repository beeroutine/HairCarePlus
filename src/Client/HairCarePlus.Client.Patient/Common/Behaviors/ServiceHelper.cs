namespace HairCarePlus.Client.Patient.Common.Behaviors;

public static class ServiceHelper
{
    public static IServiceProvider? Services { get; private set; }

    public static void Initialize(IServiceProvider serviceProvider)
    {
        Services = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    public static T GetService<T>() where T : class
    {
        if (Services is null)
        {
            throw new InvalidOperationException("ServiceHelper is not initialized");
        }

        var service = Services.GetService(typeof(T)) as T;
        if (service is null)
        {
            throw new InvalidOperationException($"Service of type {typeof(T).FullName} is not registered");
        }
        return service;
    }
}