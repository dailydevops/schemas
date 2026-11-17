namespace NetEvolve.Schemas.Tests.Unit.Resources.Default;

using System.Threading;
using System.Threading.Tasks;
using NetEvolve.Schemas.Resources;

internal sealed class DenyAllResourceResolverTests
{
    [Test]
    [Arguments("file:///etc/passwd")]
    [Arguments("http://example.org/a.xsd")]
    [Arguments("https://example.org/a.xsd")]
    [Arguments("ftp://example.org/a.xsd")]
    [Arguments("custom-scheme:opaque")]
    [Arguments("../a.xsd")]
    [Arguments("")]
    public async Task Resolve_AnyReference_DeniesAndOpensNothing(string reference, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var request = new ResourceRequest(ResourceKind.SchemaDocument, reference, "file:///base/", null);

        using var result = DenyAllResourceResolver.Instance.Resolve(in request, cancellationToken);

        await Assert.That(result.Status).IsEqualTo(ResourceStatus.Denied);
        await Assert.That(result.Stream).IsNull();
        await Assert.That(result.Content.IsEmpty).IsTrue();
        await Assert.That(result.SystemId).IsNull();
    }

    [Test]
    [Arguments(ResourceKind.ExternalSubset)]
    [Arguments(ResourceKind.ExternalEntity)]
    [Arguments(ResourceKind.ExternalParameterEntity)]
    [Arguments(ResourceKind.SchemaDocument)]
    [Arguments(ResourceKind.SchemaLocationHint)]
    [Arguments(ResourceKind.XPathDocument)]
    [Arguments(ResourceKind.XPathText)]
    public async Task Resolve_EveryKind_Denies(ResourceKind kind, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var request = new ResourceRequest(kind, "a.xml", "file:///base/", "file:///base/a.xml");

        using var result = DenyAllResourceResolver.Instance.Resolve(in request, cancellationToken);

        await Assert.That(result.Status).IsEqualTo(ResourceStatus.Denied);
    }

    [Test]
    public async Task Resolve_Denial_CarriesCodeRequestedUriBaseUriAndKind(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var request = new ResourceRequest(
            ResourceKind.ExternalEntity,
            "a.ent",
            "http://h/base/",
            "http://h/base/a.ent"
        );

        using var result = DenyAllResourceResolver.Instance.Resolve(in request, cancellationToken);

        await Assert.That(result.Message).IsNotNull();
        await Assert.That(result.Message!).Contains(CoreDiagnosticCodes.ResourceDenied.ToString());
        await Assert.That(result.Message!).Contains("http://h/base/a.ent");
        await Assert.That(result.Message!).Contains("http://h/base/");
        await Assert.That(result.Message!).Contains(nameof(ResourceKind.ExternalEntity));
    }

    [Test]
    public async Task Resolve_DenialWithoutResolvedUri_NamesTheReferenceAsWritten(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var request = new ResourceRequest(ResourceKind.SchemaDocument, "rel/a.xsd", null, null);

        using var result = DenyAllResourceResolver.Instance.Resolve(in request, cancellationToken);

        await Assert.That(result.Message!).Contains("rel/a.xsd");
    }

    [Test]
    public async Task Resolve_DefaultRequest_Denies(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var request = default(ResourceRequest);

        using var result = DenyAllResourceResolver.Instance.Resolve(in request, cancellationToken);

        await Assert.That(result.Status).IsEqualTo(ResourceStatus.Denied);
        await Assert.That(result.Message).IsNotNull();
    }

    [Test]
    public async Task Instance_IsShared() =>
        await Assert.That(DenyAllResourceResolver.Instance).IsSameReferenceAs(DenyAllResourceResolver.Instance);
}
