namespace NetEvolve.Schemas.Tests.Unit.Formats;

using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NetEvolve.Schemas.Formats;
using NetEvolve.Schemas.Resources;

/// <summary>The resource loader (#529): resolve against a base, ask the resolver, cap the size, and map every outcome to a code.</summary>
public class ResourceLoaderTests
{
    private const string Base = "file:///dir/main.xsd";
    private static readonly byte[] Content = [1, 2, 3, 4, 5, 6, 7, 8];

    [Test]
    public async Task Load_FoundMemory_IsLoadedWithTheResolvedUri(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var resolver = new StubResolver(_ => ResourceResult.Found(Content));
        var loader = new ResourceLoader(resolver, ValidationLimits.Default);

        var result = loader.Load(ResourceKind.SchemaDocument, "other.xsd", Base, cancellationToken, "pub", "urn:ns");

        await Assert.That(result.Status).IsEqualTo(ResourceLoadStatus.Loaded);
        await Assert.That(result.Content.ToArray().SequenceEqual(Content)).IsTrue();
        await Assert.That(result.SystemId).IsEqualTo("file:///dir/other.xsd");
        await Assert.That(result.Message).IsEmpty();
        await Assert.That(result.Code).IsEqualTo(default(DiagnosticCode));
        await Assert.That(loader.RequestCount).IsEqualTo(1);
        var request = resolver.Requests.Single();
        await Assert.That(request.Kind).IsEqualTo(ResourceKind.SchemaDocument);
        await Assert.That(request.Reference).IsEqualTo("other.xsd");
        await Assert.That(request.BaseUri).IsEqualTo(Base);
        await Assert.That(request.ResolvedUri).IsEqualTo("file:///dir/other.xsd");
        await Assert.That(request.PublicId).IsEqualTo("pub");
        await Assert.That(request.NamespaceUri).IsEqualTo("urn:ns");
        await Assert.That(resolver.LastToken).IsEqualTo(cancellationToken);
    }

    [Test]
    public async Task Load_FoundStream_ReadsAndDisposesTheStreamAndKeepsTheRedirect(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var stream = new MemoryStream(Content);
        var loader = new ResourceLoader(
            new StubResolver(_ => ResourceResult.Found(stream, "file:///redirected.xsd")),
            ValidationLimits.Default
        );

        var result = loader.Load(ResourceKind.SchemaDocument, "other.xsd", Base, cancellationToken);

        await Assert.That(result.Status).IsEqualTo(ResourceLoadStatus.Loaded);
        await Assert.That(result.Content.ToArray().SequenceEqual(Content)).IsTrue();
        await Assert.That(result.SystemId).IsEqualTo("file:///redirected.xsd");
        await Assert.That(stream.CanRead).IsFalse();
    }

    [Test]
    public async Task Load_Denied_FailsWithNes0100AndTheResolverMessage(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var loader = new ResourceLoader(
            new StubResolver(_ => ResourceResult.Denied("no way")),
            ValidationLimits.Default
        );

        var result = loader.Load(ResourceKind.SchemaDocument, "other.xsd", Base, cancellationToken);

        await Assert.That(result.Status).IsEqualTo(ResourceLoadStatus.Failed);
        await Assert.That(result.Code).IsEqualTo(CoreDiagnosticCodes.ResourceDenied);
        await Assert.That(result.Message).IsEqualTo("no way");
        await Assert.That(result.Content.Length).IsEqualTo(0);
        await Assert.That(result.SystemId).IsNull();
    }

    [Test]
    public async Task Load_DeniedWithoutMessage_UsesAStandardText(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var loader = new ResourceLoader(new StubResolver(_ => ResourceResult.Denied()), ValidationLimits.Default);

        var result = loader.Load(ResourceKind.SchemaDocument, "other.xsd", Base, cancellationToken);

        await Assert.That(result.Message).IsEqualTo("The resource 'file:///dir/other.xsd' was denied.");
    }

    [Test]
    public async Task Load_NotFound_FailsWithNes0102(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var loader = new ResourceLoader(new StubResolver(_ => ResourceResult.NotFound()), ValidationLimits.Default);

        var result = loader.Load(ResourceKind.SchemaDocument, "other.xsd", Base, cancellationToken);

        await Assert.That(result.Status).IsEqualTo(ResourceLoadStatus.Failed);
        await Assert.That(result.Code).IsEqualTo(CoreDiagnosticCodes.ResourceNotFound);
        await Assert.That(result.Message).IsEqualTo("The resource 'file:///dir/other.xsd' was not found.");
    }

    [Test]
    public async Task Load_NotFoundWithMessage_KeepsTheResolverMessage(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var loader = new ResourceLoader(
            new StubResolver(_ => ResourceResult.NotFound("gone")),
            ValidationLimits.Default
        );

        var result = loader.Load(ResourceKind.SchemaDocument, "other.xsd", Base, cancellationToken);

        await Assert.That(result.Message).IsEqualTo("gone");
    }

    [Test]
    public async Task Load_RelativeReferenceWithoutBase_PassesThroughWhenTheInternalSwitchIsSet(
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var resolver = new StubResolver(_ => ResourceResult.Found(Content));
        var loader = new ResourceLoader(resolver, ValidationLimits.Default) { PassMissingBaseToResolver = true };

        var result = loader.Load(ResourceKind.SchemaDocument, "other.xsd", null, cancellationToken);

        await Assert.That(result.Status).IsEqualTo(ResourceLoadStatus.Loaded);
        await Assert.That(result.SystemId).IsNull();
        await Assert.That(resolver.Requests.Single().ResolvedUri).IsNull();
        await Assert.That(loader.RequestCount).IsEqualTo(1);
    }

    [Test]
    public async Task Load_MissingBasePassThrough_MapsNotFoundAndOversizeWithTheReference(
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var missing = new ResourceLoader(new StubResolver(_ => ResourceResult.NotFound()), ValidationLimits.Default)
        {
            PassMissingBaseToResolver = true,
        };
        var large = new ResourceLoader(
            new StubResolver(_ => ResourceResult.Found(Content)),
            ValidationLimits.Default.WithMaxResourceSize(2)
        )
        {
            PassMissingBaseToResolver = true,
        };

        var notFound = missing.Load(ResourceKind.SchemaDocument, "other.xsd", null, cancellationToken);
        var tooLarge = large.Load(ResourceKind.SchemaDocument, "other.xsd", null, cancellationToken);

        await Assert.That(notFound.Message).IsEqualTo("The resource 'other.xsd' was not found.");
        await Assert.That(tooLarge.Code).IsEqualTo(CoreDiagnosticCodes.ResourceSizeLimitExceeded);
        await Assert.That(tooLarge.Message).IsEqualTo("The resource 'other.xsd' is larger than 2 bytes.");
    }

    [Test]
    public async Task Load_InvalidReference_FailsEvenWithTheInternalSwitch(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var resolver = new StubResolver(_ => ResourceResult.Found(Content));
        var loader = new ResourceLoader(resolver, ValidationLimits.Default) { PassMissingBaseToResolver = true };

        var result = loader.Load(ResourceKind.SchemaDocument, "http://[bad", Base, cancellationToken);

        await Assert.That(result.Code).IsEqualTo(CoreDiagnosticCodes.ResourceReferenceInvalid);
        await Assert.That(resolver.Requests).IsEmpty();
    }

    [Test]
    public async Task Load_RelativeReferenceWithoutBase_FailsWithNes0101WithoutAskingTheResolver(
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var resolver = new StubResolver(_ => ResourceResult.Found(Content));
        var loader = new ResourceLoader(resolver, ValidationLimits.Default);

        var result = loader.Load(ResourceKind.SchemaDocument, "other.xsd", null, cancellationToken);

        await Assert.That(result.Status).IsEqualTo(ResourceLoadStatus.Failed);
        await Assert.That(result.Code).IsEqualTo(CoreDiagnosticCodes.ResourceBaseUriMissing);
        await Assert
            .That(result.Message)
            .IsEqualTo("The reference 'other.xsd' is relative and the base URI is not absolute.");
        await Assert.That(resolver.Requests).IsEmpty();
        await Assert.That(loader.RequestCount).IsEqualTo(1);
    }

    [Test]
    public async Task Load_InvalidReference_FailsWithNes0103(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var resolver = new StubResolver(_ => ResourceResult.Found(Content));
        var loader = new ResourceLoader(resolver, ValidationLimits.Default);

        var result = loader.Load(ResourceKind.SchemaDocument, "ht tp://[bad", Base, cancellationToken);

        await Assert.That(result.Status).IsEqualTo(ResourceLoadStatus.Failed);
        await Assert.That(result.Code).IsEqualTo(CoreDiagnosticCodes.ResourceReferenceInvalid);
        await Assert.That(result.Message).IsEqualTo("The reference 'ht tp://[bad' is not a valid URI reference.");
        await Assert.That(resolver.Requests).IsEmpty();
    }

    [Test]
    public async Task Load_OversizeMemory_ExceedsTheLimitWithNes0007(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var loader = new ResourceLoader(
            new StubResolver(_ => ResourceResult.Found(Content, "file:///big.xsd")),
            ValidationLimits.Default.WithMaxResourceSize(4)
        );

        var result = loader.Load(ResourceKind.SchemaDocument, "big.xsd", Base, cancellationToken);

        await Assert.That(result.Status).IsEqualTo(ResourceLoadStatus.LimitExceeded);
        await Assert.That(result.Code).IsEqualTo(CoreDiagnosticCodes.ResourceSizeLimitExceeded);
        await Assert.That(result.SystemId).IsEqualTo("file:///big.xsd");
        await Assert.That(result.Message).IsEqualTo("The resource 'file:///big.xsd' is larger than 4 bytes.");
        await Assert.That(result.Content.Length).IsEqualTo(0);
    }

    [Test]
    public async Task Load_OversizeStream_ExceedsTheLimitAndDisposesTheStream(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var stream = new ProbeStream(Content, canSeek: false);
        var loader = new ResourceLoader(
            new StubResolver(_ => ResourceResult.Found(stream)),
            ValidationLimits.Default.WithMaxResourceSize(4)
        );

        var result = loader.Load(ResourceKind.SchemaDocument, "big.xsd", Base, cancellationToken);

        await Assert.That(result.Status).IsEqualTo(ResourceLoadStatus.LimitExceeded);
        await Assert.That(result.SystemId).IsEqualTo("file:///dir/big.xsd");
        await Assert.That(stream.CanRead).IsFalse();
    }

    [Test]
    public async Task Load_MoreRequestsThanTheLimit_ExceedsTheLimitWithNes0006(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var resolver = new StubResolver(_ => ResourceResult.NotFound());
        var loader = new ResourceLoader(resolver, ValidationLimits.Default.WithMaxExternalResources(2));
        _ = loader.Load(ResourceKind.SchemaDocument, "a.xsd", Base, cancellationToken);
        _ = loader.Load(ResourceKind.SchemaDocument, "a.xsd", Base, cancellationToken);

        var result = loader.Load(ResourceKind.SchemaDocument, "a.xsd", Base, cancellationToken);

        await Assert.That(result.Status).IsEqualTo(ResourceLoadStatus.LimitExceeded);
        await Assert.That(result.Code).IsEqualTo(CoreDiagnosticCodes.ExternalResourceLimitExceeded);
        await Assert.That(result.Message).IsEqualTo("More than 2 external resources are referenced.");
        await Assert.That(loader.RequestCount).IsEqualTo(2);
        await Assert.That(resolver.Requests.Count).IsEqualTo(2);
    }

    [Test]
    public async Task Load_Cancelled_CancelsBeforeCounting(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var resolver = new StubResolver(_ => ResourceResult.Found(Content));
        var loader = new ResourceLoader(resolver, ValidationLimits.Default);
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert
            .That(() => loader.Load(ResourceKind.SchemaDocument, "a.xsd", Base, cts.Token))
            .Throws<OperationCanceledException>();
        await Assert.That(loader.RequestCount).IsEqualTo(0);
    }

    [Test]
    public async Task Guards_NullArguments_Throw(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var loader = new ResourceLoader(new StubResolver(_ => ResourceResult.NotFound()), ValidationLimits.Default);

        await Assert.That(() => new ResourceLoader(null!, ValidationLimits.Default)).Throws<ArgumentNullException>();
        await Assert
            .That(() => new ResourceLoader(new StubResolver(_ => ResourceResult.NotFound()), null!))
            .Throws<ArgumentNullException>();
        await Assert
            .That(() => loader.Load(ResourceKind.SchemaDocument, null!, Base, cancellationToken))
            .Throws<ArgumentNullException>();
    }

    [Test]
    public async Task Result_Default_IsLoadedWithEmptyContent(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var result = default(ResourceLoadResult);

        await Assert.That(result.Status).IsEqualTo(ResourceLoadStatus.Loaded);
        await Assert.That(result.Message).IsEmpty();
        await Assert.That(result.Content.Length).IsEqualTo(0);
    }
}
