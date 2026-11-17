namespace NetEvolve.Schemas.Conformance.Fetch;

/// <summary>Downloads suite archives with an <see cref="HttpClient"/>.</summary>
/// <param name="client">The client to download with.</param>
public sealed class HttpSuiteTransport(HttpClient client) : ISuiteTransport
{
    /// <inheritdoc />
    public async Task<Stream> OpenAsync(Uri source, CancellationToken cancellationToken) =>
        await client.GetStreamAsync(source, cancellationToken).ConfigureAwait(false);
}
