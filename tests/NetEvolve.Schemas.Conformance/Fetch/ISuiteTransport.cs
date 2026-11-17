namespace NetEvolve.Schemas.Conformance.Fetch;

/// <summary>Opens the byte stream of a suite archive; the seam that lets tests count downloads.</summary>
public interface ISuiteTransport
{
    /// <summary>Opens the archive at <paramref name="source"/>.</summary>
    /// <param name="source">The archive URL.</param>
    /// <param name="cancellationToken">A token to cancel the download.</param>
    /// <returns>A readable stream the caller disposes.</returns>
    Task<Stream> OpenAsync(Uri source, CancellationToken cancellationToken);
}
