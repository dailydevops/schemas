namespace NetEvolve.Schemas.Xml.Tests.Unit.Dtd.External.Loader;

using System;
using System.Threading;
using NetEvolve.Schemas;
using NetEvolve.Schemas.Resources;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Dtd.Contracts;
using NetEvolve.Schemas.Xml.Dtd.External.Loader;
using static NetEvolve.Schemas.Xml.Tests.Unit.Dtd.External.Loader.LoaderHarness;

/// <summary>The resolver-backed loader of external entities (XML-61): deny by default, base URI, recursion, and limits.</summary>
public class ExternalEntityLoaderTests
{
    [Test]
    [Arguments(ResourceKind.ExternalSubset, null)]
    [Arguments(ResourceKind.ExternalSubset, "-//Example//DTD Example//EN")]
    [Arguments(ResourceKind.ExternalEntity, null)]
    [Arguments(ResourceKind.ExternalEntity, "-//Example//ENTITIES Example//EN")]
    [Arguments(ResourceKind.ExternalParameterEntity, null)]
    [Arguments(ResourceKind.ExternalParameterEntity, "-//Example//ENTITIES Example//EN")]
    public async Task TryLoad_DefaultOptions_RefusesEverySystemAndPublicIdentifier_ResolverReceivesNoCall(
        ResourceKind kind,
        string? publicId,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var resolver = new RecordingResolver().Add("http://example.org/doc/ext.ent", Utf8("x"));
        var loader = Loader(resolver, options: DtdValidationOptions.Default.WithResolver(resolver));

        var (frame, failure) = Load(loader, "e", "ext.ent", DocumentUri, cancellationToken, kind, publicId);

        await Assert.That(frame).IsNull();
        await Assert.That(failure.Code).IsSameReferenceAs(XmlErrorCodes.XML2002);
        await Assert.That(resolver.Requests).IsEmpty();
    }

    [Test]
    [Arguments(ResourceKind.ExternalSubset)]
    [Arguments(ResourceKind.ExternalEntity)]
    [Arguments(ResourceKind.ExternalParameterEntity)]
    public async Task TryLoad_FlagOnWithoutResolver_IsDeniedByTheDefaultResolver_Xml2002WithKindUriAndBase(
        ResourceKind kind,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var options = DtdValidationOptions
            .Default.WithAllowExternalSubset(true)
            .WithAllowExternalGeneralEntities(true)
            .WithAllowExternalParameterEntities(true);
        var loader = new ExternalEntityLoader(options, XmlReaderLimits.Default);

        var (frame, failure) = Load(loader, "e", "dir/ext.ent", DocumentUri, cancellationToken, kind);

        await Assert.That(frame).IsNull();
        await Assert.That(failure.Code).IsSameReferenceAs(XmlErrorCodes.XML2002);
        await Assert.That(failure.Message).Contains("XML2002");
        await Assert.That(failure.Message).Contains(kind.ToString());
        await Assert.That(failure.Message).Contains("http://example.org/doc/dir/ext.ent");
        await Assert.That(failure.Message).Contains(DocumentUri);
    }

    [Test]
    public async Task TryLoad_ResolverReportsNotFound_IsXml2002(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var resolver = new RecordingResolver();
        var loader = Loader(resolver);

        var (frame, failure) = Load(loader, "e", "missing.ent", DocumentUri, cancellationToken);

        await Assert.That(frame).IsNull();
        await Assert.That(failure.Code).IsSameReferenceAs(XmlErrorCodes.XML2002);
        await Assert.That(resolver.Requests.Count).IsEqualTo(1);
    }

    [Test]
    public async Task TryLoad_Request_CarriesKindReferenceBaseResolvedUriAndPublicId(
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var resolver = new RecordingResolver().Add("http://example.org/doc/dir/ext.ent", Utf8("x"));
        var loader = Loader(resolver);

        var (frame, _) = Load(
            loader,
            "e",
            "dir/ext.ent",
            DocumentUri,
            cancellationToken,
            ResourceKind.ExternalParameterEntity,
            "-//Example//ENTITIES Example//EN"
        );
        frame?.Dispose();

        var request = resolver.Requests[0];
        await Assert.That(request.Kind).IsEqualTo(ResourceKind.ExternalParameterEntity);
        await Assert.That(request.Reference).IsEqualTo("dir/ext.ent");
        await Assert.That(request.BaseUri).IsEqualTo(DocumentUri);
        await Assert.That(request.ResolvedUri).IsEqualTo("http://example.org/doc/dir/ext.ent");
        await Assert.That(request.PublicId).IsEqualTo("-//Example//ENTITIES Example//EN");
    }

    [Test]
    public async Task TryLoad_RelativeSystemId_ResolvesAgainstTheDeclaringEntityNotTheDocument(
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var resolver = new RecordingResolver()
            .Add("http://example.org/doc/dtd/a.ent", Utf8("a"))
            .Add("http://example.org/doc/dtd/b.ent", Utf8("b"))
            .Add("http://example.org/doc/b.ent", Utf8("wrong"));
        var loader = Loader(resolver);

        var (outer, _) = Load(loader, "a", "dtd/a.ent", DocumentUri, cancellationToken);
        var (inner, _) = Load(loader, "b", "b.ent", outer!.BaseUri, cancellationToken);

        await Assert.That(outer.BaseUri).IsEqualTo("http://example.org/doc/dtd/a.ent");
        await Assert.That(inner!.ResolvedUri).IsEqualTo("http://example.org/doc/dtd/b.ent");
        await Assert.That(Drain(inner).Text).IsEqualTo("b");
        await Assert.That(resolver.Requests[1].BaseUri).IsEqualTo("http://example.org/doc/dtd/a.ent");

        inner.Dispose();
        outer.Dispose();
    }

    [Test]
    public async Task TryLoad_FinalUriOfTheResolver_BecomesTheBaseOfTheFrame(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var resolver = new RecordingResolver().Add(
            "http://example.org/doc/a.ent",
            Utf8("a"),
            finalUri: "http://mirror.example.org/store/a.ent"
        );
        var loader = Loader(resolver);

        var (frame, _) = Load(loader, "a", "a.ent", DocumentUri, cancellationToken);

        await Assert.That(frame!.ResolvedUri).IsEqualTo("http://example.org/doc/a.ent");
        await Assert.That(frame.BaseUri).IsEqualTo("http://mirror.example.org/store/a.ent");

        frame.Dispose();
    }

    [Test]
    public async Task TryLoad_RelativeSystemIdWithoutAbsoluteBase_ReachesTheResolverWithoutResolvedUri(
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var resolver = new RecordingResolver().Add("a.ent", Utf8("a"));
        var loader = Loader(resolver);

        var (frame, _) = Load(loader, "a", "a.ent", baseUri: null, cancellationToken);

        await Assert.That(frame).IsNotNull();
        await Assert.That(resolver.Requests[0].ResolvedUri).IsNull();
        await Assert.That(frame!.ResolvedUri).IsNull();
        await Assert.That(frame.BaseUri).IsNull();

        frame.Dispose();
    }

    [Test]
    public async Task TryLoad_EntityThatIncludesItselfThroughASecondEntity_RaisesXml0020(
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var resolver = new RecordingResolver()
            .Add("http://example.org/doc/a.ent", Utf8("a"))
            .Add("http://example.org/doc/b.ent", Utf8("b"));
        var loader = Loader(resolver);

        var (a, _) = Load(loader, "a", "a.ent", DocumentUri, cancellationToken);
        var (b, _) = Load(loader, "b", "b.ent", a!.BaseUri, cancellationToken);
        var (again, failure) = Load(loader, "a", "a.ent", b!.BaseUri, cancellationToken);

        await Assert.That(again).IsNull();
        await Assert.That(failure.Code).IsSameReferenceAs(XmlErrorCodes.XML0020);
        await Assert.That(resolver.Requests.Count).IsEqualTo(2);

        b.Dispose();
        a.Dispose();
    }

    [Test]
    public async Task TryLoad_SameResolvedUriUnderAnotherName_RaisesXml0020(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var resolver = new RecordingResolver().Add("http://example.org/doc/a.ent", Utf8("a"));
        var loader = Loader(resolver);

        var (a, _) = Load(loader, "a", "a.ent", DocumentUri, cancellationToken);
        var (alias, failure) = Load(loader, "alias", "./a.ent", DocumentUri, cancellationToken);

        await Assert.That(alias).IsNull();
        await Assert.That(failure.Code).IsSameReferenceAs(XmlErrorCodes.XML0020);
        await Assert.That(resolver.Requests.Count).IsEqualTo(1);

        a!.Dispose();
    }

    [Test]
    public async Task TryLoad_SameNameOfAnotherKind_IsNotRecursion(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var resolver = new RecordingResolver()
            .Add("http://example.org/doc/a.ent", Utf8("a"))
            .Add("http://example.org/doc/p.ent", Utf8("p"));
        var loader = Loader(resolver);

        var (general, _) = Load(loader, "x", "a.ent", DocumentUri, cancellationToken);
        var (parameter, failure) = Load(
            loader,
            "x",
            "p.ent",
            DocumentUri,
            cancellationToken,
            ResourceKind.ExternalParameterEntity
        );

        await Assert.That(failure.Code).IsNull();
        await Assert.That(parameter).IsNotNull();

        parameter!.Dispose();
        general!.Dispose();
    }

    [Test]
    public async Task TryLoad_SameEntityAgainAfterTheFirstFrameClosed_IsNotRecursion(
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var resolver = new RecordingResolver().Add("http://example.org/doc/a.ent", Utf8("a"));
        var loader = Loader(resolver);

        var (first, _) = Load(loader, "a", "a.ent", DocumentUri, cancellationToken);
        first!.Dispose();
        var (second, failure) = Load(loader, "a", "a.ent", DocumentUri, cancellationToken);

        await Assert.That(failure.Code).IsNull();
        await Assert.That(second).IsNotNull();

        second!.Dispose();
    }

    [Test]
    public async Task TryLoad_NestingBeyondMaxEntityDepth_RaisesXml1110_AndAClosedFrameGivesTheDepthBack(
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var resolver = new RecordingResolver()
            .Add("http://example.org/doc/a.ent", Utf8("a"))
            .Add("http://example.org/doc/b.ent", Utf8("b"))
            .Add("http://example.org/doc/c.ent", Utf8("c"));
        var loader = Loader(resolver, XmlReaderLimits.Default.WithMaxEntityDepth(2));

        var (a, _) = Load(loader, "a", "a.ent", DocumentUri, cancellationToken);
        var (b, _) = Load(loader, "b", "b.ent", DocumentUri, cancellationToken);
        var (c, failure) = Load(loader, "c", "c.ent", DocumentUri, cancellationToken);

        await Assert.That(c).IsNull();
        await Assert.That(failure.Code).IsSameReferenceAs(XmlErrorCodes.XML1110);
        await Assert.That(resolver.Requests.Count).IsEqualTo(2);

        b!.Dispose();
        var (retry, retryFailure) = Load(loader, "c", "c.ent", DocumentUri, cancellationToken);

        await Assert.That(retryFailure.Code).IsNull();

        retry!.Dispose();
        a!.Dispose();
    }

    [Test]
    public async Task TryLoad_DeniedAndNotFoundLoads_GiveTheDepthBack_SoALaterLoadStillWorksAtDepthOne(
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var resolver = new RecordingResolver()
            .Add("http://example.org/doc/a.ent", Utf8("a"))
            .Add("http://example.org/doc/bad.ent", Utf8("<?xml encoding=\"KLINGON-1\"?>x"));
        var options = Open(resolver).WithAllowExternalGeneralEntities(false);
        var loader = Loader(resolver, XmlReaderLimits.Default.WithMaxEntityDepth(1), options);
        const ResourceKind Kind = ResourceKind.ExternalParameterEntity;

        var (_, denied) = Load(loader, "d", "a.ent", DocumentUri, cancellationToken, ResourceKind.ExternalEntity);
        var (_, missing) = Load(loader, "m", "missing.ent", DocumentUri, cancellationToken, Kind);
        var (_, invalid) = Load(loader, "i", "bad.ent", DocumentUri, cancellationToken, Kind);
        var (frame, failure) = Load(loader, "a", "a.ent", DocumentUri, cancellationToken, Kind);

        await Assert.That(denied.Code).IsSameReferenceAs(XmlErrorCodes.XML2002);
        await Assert.That(missing.Code).IsSameReferenceAs(XmlErrorCodes.XML2002);
        await Assert.That(invalid.Code).IsSameReferenceAs(XmlErrorCodes.XML4001);
        await Assert.That(failure.Code).IsNull();
        await Assert.That(frame).IsNotNull();

        frame!.Dispose();
    }

    [Test]
    public async Task TryLoad_RequestRefusedByTheOptions_StillConsumesOneRequestCharge(
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var resolver = new RecordingResolver().Add("http://example.org/doc/a.ent", Utf8("a"));
        var options = Open(resolver)
            .WithAllowExternalGeneralEntities(false)
            .WithDtdLimits(DtdValidationLimits.Default.WithMaxExternalEntityRequests(1));
        var loader = Loader(resolver, options: options);

        var (_, denied) = Load(loader, "a", "a.ent", DocumentUri, cancellationToken);
        var (_, exhausted) = Load(loader, "a", "a.ent", DocumentUri, cancellationToken);

        await Assert.That(denied.Code).IsSameReferenceAs(XmlErrorCodes.XML2002);
        await Assert.That(exhausted.Code).IsSameReferenceAs(DtdLimitCodes.XML1117);
        await Assert.That(resolver.Requests).IsEmpty();
    }

    [Test]
    public async Task TryLoad_MoreRequestsThanMaxExternalEntityRequests_RaisesXml1117(
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var resolver = new RecordingResolver().Add("http://example.org/doc/a.ent", Utf8("a"));
        var options = Open(resolver).WithDtdLimits(DtdValidationLimits.Default.WithMaxExternalEntityRequests(2));
        var loader = Loader(resolver, options: options);

        for (var i = 0; i < 2; i++)
        {
            var (frame, _) = Load(loader, "a", "a.ent", DocumentUri, cancellationToken);
            frame!.Dispose();
        }

        var (third, failure) = Load(loader, "a", "a.ent", DocumentUri, cancellationToken);

        await Assert.That(third).IsNull();
        await Assert.That(failure.Code).IsSameReferenceAs(DtdLimitCodes.XML1117);
        await Assert.That(resolver.Requests.Count).IsEqualTo(2);
    }

    [Test]
    public async Task TryLoad_ExpansionBudget_IsChargedAcrossFrames_Xml1109(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var resolver = new RecordingResolver()
            .Add("http://example.org/doc/a.ent", Utf8("aaaaaa"))
            .Add("http://example.org/doc/b.ent", Utf8("bbbbbb"));
        var loader = Loader(resolver, XmlReaderLimits.Default.WithMaxEntityExpansionCharacters(10));

        var (a, _) = Load(loader, "a", "a.ent", DocumentUri, cancellationToken);
        var first = Drain(a!);
        a!.Dispose();
        var (b, _) = Load(loader, "b", "b.ent", DocumentUri, cancellationToken);
        var second = Drain(b!);

        await Assert.That(first.Text).IsEqualTo("aaaaaa");
        await Assert.That(first.Error).IsNull();
        await Assert.That(second.Text).IsEmpty();
        await Assert.That(second.Error).IsSameReferenceAs(XmlErrorCodes.XML1109);
        await Assert.That(resolver.Streams[1].IsDisposed).IsTrue();
    }

    [Test]
    public async Task TryLoad_ResourceSizeCap_IsNotAppliedHere_TheExpansionBudgetBoundsTheEntity(
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var resolver = new RecordingResolver().Add("http://example.org/doc/a.ent", Utf8(new string('a', 40)));
        var options = Open(resolver).WithLimits(ValidationLimits.Default.WithMaxResourceSize(8));
        var loader = Loader(resolver, options: options);

        var (frame, _) = Load(loader, "a", "a.ent", DocumentUri, cancellationToken);
        var read = Drain(frame!);

        await Assert.That(read.Text.Length).IsEqualTo(40);
        await Assert.That(read.Error).IsNull();
    }

    [Test]
    public async Task TryLoad_CanceledToken_Throws_AndTheResolverIsNotCalled()
    {
        var resolver = new RecordingResolver().Add("http://example.org/doc/a.ent", Utf8("a"));
        var loader = Loader(resolver);
        using var source = new CancellationTokenSource();
        await source.CancelAsync();

        await Assert
            .That(() =>
            {
                _ = loader.TryLoad(
                    ResourceKind.ExternalEntity,
                    "a",
                    null,
                    "a.ent",
                    DocumentUri,
                    source.Token,
                    out _,
                    out _
                );
            })
            .Throws<OperationCanceledException>();
        await Assert.That(resolver.Requests).IsEmpty();
    }

    [Test]
    public async Task TryLoad_TokenCanceledWhileTheResolverResolves_Throws_AndTheResolverStreamIsDisposed()
    {
        using var source = new CancellationTokenSource();
        var resolver = new CancelingResolver(source);
        var loader = Loader(resolver);

        await Assert
            .That(() =>
            {
                _ = loader.TryLoad(
                    ResourceKind.ExternalEntity,
                    "a",
                    null,
                    "a.ent",
                    DocumentUri,
                    source.Token,
                    out _,
                    out _
                );
            })
            .Throws<OperationCanceledException>();
        await Assert.That(resolver.Stream!.IsDisposed).IsTrue();
    }

    [Test]
    public async Task Guards_NullArguments_Throw(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var resolver = new RecordingResolver();
        var loader = Loader(resolver);

        await Assert
            .That(() => new ExternalEntityLoader(null!, XmlReaderLimits.Default))
            .Throws<ArgumentNullException>();
        await Assert
            .That(() => new ExternalEntityLoader(DtdValidationOptions.Default, null!))
            .Throws<ArgumentNullException>();
        await Assert
            .That(() => Load(loader, null!, "a.ent", DocumentUri, cancellationToken))
            .Throws<ArgumentNullException>();
        await Assert
            .That(() => Load(loader, "a", null!, DocumentUri, cancellationToken))
            .Throws<ArgumentNullException>();
        await Assert.That(() => loader.Leave(null!)).Throws<ArgumentNullException>();
    }

    [Test]
    [Arguments(-1)]
    [Arguments(int.MinValue)]
    public async Task TryChargeExpansion_NegativeCount_Throws(int count, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var loader = Loader(new RecordingResolver());

        await Assert.That(() => loader.TryChargeExpansion(count)).Throws<ArgumentOutOfRangeException>();
    }

    // Cancels the token while it resolves, then hands out a found stream, like a resolver that races a cancellation.
    private sealed class CancelingResolver(CancellationTokenSource source) : IResourceResolver
    {
        internal TrackingStream? Stream { get; private set; }

        public ResourceResult Resolve(in ResourceRequest request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            source.Cancel();
            Stream = new TrackingStream(Utf8("a"));

            return ResourceResult.Found(Stream);
        }
    }
}
