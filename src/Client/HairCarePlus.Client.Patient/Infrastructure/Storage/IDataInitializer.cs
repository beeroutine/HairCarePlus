using System.Threading.Tasks;

namespace HairCarePlus.Client.Patient.Infrastructure.Storage;

/// <summary>
/// Interface for data initialization services
/// </summary>
public interface IDataInitializer
{
    /// <summary>
    /// Checks if data needs to be initialized
    /// </summary>
    Task<bool> NeedsInitializationAsync();
    
    /// <summary>
    /// Initializes data in the database
    /// </summary>
    Task InitializeDataAsync();
} 