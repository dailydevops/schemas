namespace NetEvolve.Schemas.Benchmarks.Xml.Dtd;

using System.Xml;

/// <summary>Serves the external subset from memory to <c>System.Xml</c>, which would otherwise request it over the network.</summary>
internal sealed class InMemoryXmlResolver : XmlResolver
{
    private readonly byte[] _subset;

    public InMemoryXmlResolver(byte[] subset) => _subset = subset;

    /// <summary>Gets the number of requests since the last <see cref="Reset"/>.</summary>
    public int Requests { get; private set; }

    public void Reset() => Requests = 0;

    public override object? GetEntity(Uri absoluteUri, string? role, Type? ofObjectToReturn)
    {
        Requests++;

        return string.Equals(absoluteUri.AbsoluteUri, DtdBenchmarkInput.SubsetUri, StringComparison.Ordinal)
            ? new MemoryStream(_subset, writable: false)
            : throw new InvalidOperationException("Only the subset of the benchmark is served.");
    }
}
