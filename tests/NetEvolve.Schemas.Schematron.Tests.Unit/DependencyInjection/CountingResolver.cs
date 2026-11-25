namespace NetEvolve.Schemas.Schematron.Tests.Unit.DependencyInjection;

using System.Text;
using NetEvolve.Schemas.Resources;
using NetEvolve.Schemas.Testing.Fakes;

internal sealed class CountingResolver : IResourceResolver
{
    private readonly InMemoryResourceResolver _inner = new();
    private int _calls;

    internal int Calls => Volatile.Read(ref _calls);

    internal CountingResolver Add(string key, string content)
    {
        _ = _inner.Add(key, Encoding.UTF8.GetBytes(content));
        return this;
    }

    public ResourceResult Resolve(in ResourceRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _ = Interlocked.Increment(ref _calls);
        return _inner.Resolve(in request, cancellationToken);
    }
}
