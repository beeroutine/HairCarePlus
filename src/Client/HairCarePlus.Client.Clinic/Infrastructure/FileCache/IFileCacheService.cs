using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace HairCarePlus.Client.Clinic.Infrastructure.FileCache;

public interface IFileCacheService
{
    /// <summary>
    /// Returns absolute local path for the specified remote URL. If the file is not cached yet it will be downloaded
    /// and stored in application data directory before returning.
    /// </summary>
    /// <param name="remoteUrl">HTTP(S) URL of remote file.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Absolute path to local cached file.</returns>
    Task<string> GetLocalPathAsync(string remoteUrl, CancellationToken cancellationToken = default);

    /// <summary>
    /// Ensures that all provided URLs are cached locally. Method returns when downloads (if any) complete.
    /// </summary>
    Task PrefetchAsync(IEnumerable<string> remoteUrls, CancellationToken cancellationToken = default);
} 