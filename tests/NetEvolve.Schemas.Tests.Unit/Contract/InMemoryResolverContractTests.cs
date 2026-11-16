namespace NetEvolve.Schemas.Tests.Unit.Contract;

using System.Threading;
using System.Threading.Tasks;
using NetEvolve.Schemas.Resources;
using NetEvolve.Schemas.Testing.Fakes;

public class InMemoryResolverContractTests
{
    private static readonly byte[] Content = [1, 2, 3];

    [Test]
    public async Task Resolve_RegisteredResource_ReturnsContent(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        IResourceResolver resolver = new InMemoryResourceResolver().Add("file:///x/a.xsd", Content);
        var request = new ResourceRequest(ResourceKind.SchemaDocument, "a.xsd", "file:///x/", "file:///x/a.xsd");

        using var result = resolver.Resolve(in request, cancellationToken);

        await Assert.That(result.Status).IsEqualTo(ResourceStatus.Found);
        await Assert.That(result.Content.ToArray()).IsEquivalentTo(Content);
        await Assert.That(result.SystemId).IsEqualTo("file:///x/a.xsd");
    }

    [Test]
    public async Task Resolve_UnknownResource_ReturnsNotFound(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        IResourceResolver resolver = new InMemoryResourceResolver();
        var request = new ResourceRequest(ResourceKind.ExternalEntity, "missing.ent", null, null);

        using var result = resolver.Resolve(in request, cancellationToken);

        await Assert.That(result.Status).IsEqualTo(ResourceStatus.NotFound);
    }
}
