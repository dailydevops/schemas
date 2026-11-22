namespace NetEvolve.Schemas.Schematron.Tests.Unit.Loading.Inclusion;

using System.Threading;
using NetEvolve.Schemas.Schematron.Loading.Inclusion;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.XPath.Xdm.Nodes;
using static NetEvolve.Schemas.Schematron.Tests.Unit.Loading.Inclusion.InclusionFixture;

public class ModuleLoaderTests
{
    private static readonly DocumentLocation At = new(RootId, 3, 7);

    private static ModuleReference Reference(string href, string? baseUri = RootId) =>
        new(ModuleKind.SchematronInclude, href, baseUri, At);

    [Test]
    public async Task TryLoad_FoundDocument_ReturnsTheDocumentNodeAndTheSystemId(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var files = new FakeResources().Add("file:///s/x.sch", Pattern("px"));
        var loader = new ModuleLoader(files, ValidationLimits.Default);

        var ok = loader.TryLoad(Reference("x.sch"), cancellationToken, out var module, out var problem);

        await Assert.That(ok).IsTrue();
        await Assert.That(module.SystemId).IsEqualTo("file:///s/x.sch");
        await Assert.That(module.Root.Kind).IsEqualTo(XdmNodeKind.Document);
        await Assert.That(problem.Message).IsNull();
    }

    [Test]
    public async Task TryLoad_CountsEveryCall_AlsoForTheSameReference(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var files = new FakeResources().Add("file:///s/x.sch", Pattern("px"));
        var loader = new ModuleLoader(files, ValidationLimits.Default.WithMaxExternalResources(2));

        var first = loader.TryLoad(Reference("x.sch"), cancellationToken, out _, out _);
        var second = loader.TryLoad(Reference("x.sch"), cancellationToken, out _, out _);
        var third = loader.TryLoad(Reference("x.sch"), cancellationToken, out _, out var problem);

        await Assert.That(first).IsTrue();
        await Assert.That(second).IsTrue();
        await Assert.That(third).IsFalse();
        await Assert.That(problem.Code).IsEqualTo(CoreDiagnosticCodes.ExternalResourceLimitExceeded);
        await Assert.That(problem.Location).IsEqualTo(At);
        await Assert.That(files.Requests.Count).IsEqualTo(2);
    }

    [Test]
    public async Task TryLoad_RefusedAndMissingResources_CountAgainstTheLimit(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var loader = new ModuleLoader(new FakeResources(), ValidationLimits.Default.WithMaxExternalResources(1));

        var missing = loader.TryLoad(Reference("x.sch"), cancellationToken, out _, out var first);
        var next = loader.TryLoad(Reference("y.sch"), cancellationToken, out _, out var second);

        await Assert.That(missing).IsFalse();
        await Assert.That(first.Code).IsEqualTo(CoreDiagnosticCodes.ResourceNotFound);
        await Assert.That(next).IsFalse();
        await Assert.That(second.Code).IsEqualTo(CoreDiagnosticCodes.ExternalResourceLimitExceeded);
    }

    [Test]
    public async Task TryLoad_NullHref_ThrowsBeforeTheRequestIsCounted(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var files = new FakeResources().Add("file:///s/x.sch", Pattern("px"));
        var loader = new ModuleLoader(files, ValidationLimits.Default.WithMaxExternalResources(1));

        await Assert
            .That(() => loader.TryLoad(Reference(null!), cancellationToken, out _, out _))
            .Throws<ArgumentNullException>();
        var ok = loader.TryLoad(Reference("x.sch"), cancellationToken, out _, out _);

        await Assert.That(ok).IsTrue();
    }

    [Test]
    public async Task TryLoad_CancelledToken_Throws()
    {
        var loader = new ModuleLoader(new FakeResources(), ValidationLimits.Default);
        using var source = new CancellationTokenSource();
        await source.CancelAsync();

        await Assert
            .That(() => loader.TryLoad(Reference("x.sch"), source.Token, out _, out _))
            .Throws<OperationCanceledException>();
    }

    [Test]
    public async Task Constructor_NullArguments_Throw()
    {
        await Assert.That(() => new ModuleLoader(null!, ValidationLimits.Default)).Throws<ArgumentNullException>();
        await Assert.That(() => new ModuleLoader(new FakeResources(), null!)).Throws<ArgumentNullException>();
    }
}
