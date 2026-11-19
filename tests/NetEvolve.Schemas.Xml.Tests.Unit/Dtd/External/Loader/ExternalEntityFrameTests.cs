namespace NetEvolve.Schemas.Xml.Tests.Unit.Dtd.External.Loader;

using System;
using System.IO;
using System.Text;
using System.Threading;
using NetEvolve.Schemas.Resources;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Dtd.External.Loader;
using static NetEvolve.Schemas.Xml.Tests.Unit.Dtd.External.Loader.LoaderHarness;

/// <summary>The frame of a loaded external entity: per-entity encoding, text declaration, line ends, chunking, and disposal.</summary>
public class ExternalEntityFrameTests
{
    private const string EntityUri = "http://example.org/doc/e.ent";

    private static (RecordingResolver Resolver, ExternalEntityLoader Loader) Setup(
        byte[] content,
        int maxRead = int.MaxValue,
        bool failOnRead = false,
        bool asMemory = false
    )
    {
        var resolver = new RecordingResolver().Add(EntityUri, content, null, maxRead, asMemory, failOnRead);

        return (resolver, Loader(resolver));
    }

    [Test]
    public async Task Frame_Utf16WithTextDeclaration_LoadsIntoAUtf8Document(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // The document is UTF-8 (another frame, another decoder); this entity is UTF-16 with a byte order mark.
        var resolver = new RecordingResolver()
            .Add("http://example.org/doc/u8.ent", Utf8("<?xml version=\"1.0\" encoding=\"UTF-8\"?>xé"))
            .Add(EntityUri, Utf16Le("<?xml encoding=\"UTF-16\"?>aé<b/>€\U0001F600"));
        var loader = Loader(resolver);

        var (utf8, _) = Load(loader, "u8", "u8.ent", DocumentUri, cancellationToken);
        var (utf16, _) = Load(loader, "e", "e.ent", DocumentUri, cancellationToken);
        var utf16Read = Drain(utf16!);
        var utf8Read = Drain(utf8!);

        await Assert.That(utf16Read.Text).IsEqualTo("aé<b/>€\U0001F600");
        await Assert.That(utf16Read.Error).IsNull();
        await Assert.That(utf8Read.Text).IsEqualTo("xé");
        await Assert.That(utf8Read.Error).IsNull();
    }

    [Test]
    public async Task Frame_DeclaredEncodingOfTheEntity_SelectsItsDecoder(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var declaration = Encoding.ASCII.GetBytes("<?xml encoding=\"ISO-8859-1\"?>");
        var content = new byte[declaration.Length + 1];
        declaration.CopyTo(content, 0);
        content[^1] = 0xE9;
        var (_, loader) = Setup(content);

        var (frame, failure) = Load(loader, "e", "e.ent", DocumentUri, cancellationToken);

        await Assert.That(failure.Code).IsNull();
        await Assert.That(Drain(frame!).Text).IsEqualTo("é");
    }

    [Test]
    public async Task Frame_TextDeclaration_IsStripped_AndTheFollowingWhiteSpaceIsKept(
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var (_, loader) = Setup(Utf8("<?xml version='1.0' encoding='UTF-8'?>\n<b/>"));

        var (frame, _) = Load(loader, "e", "e.ent", DocumentUri, cancellationToken);

        await Assert.That(Drain(frame!).Text).IsEqualTo("\n<b/>");
    }

    [Test]
    public async Task Frame_WithoutTextDeclaration_DefaultsToUtf8AndSkipsTheByteOrderMark(
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var (_, loader) = Setup([0xEF, 0xBB, 0xBF, .. Utf8("café")]);

        var (frame, _) = Load(loader, "e", "e.ent", DocumentUri, cancellationToken);

        await Assert.That(Drain(frame!).Text).IsEqualTo("café");
    }

    [Test]
    [Arguments("<?xml version=\"1.0\"?>x", "XML0003")]
    [Arguments("<?xml encoding=\"UTF-8\" standalone=\"yes\"?>x", "XML0003")]
    [Arguments("<?xml encoding=\"9bad\"?>x", "XML0017")]
    [Arguments("<?xml encoding=\"UTF-8\"", "XML0001")]
    [Arguments("<?xml encoding=\"KLINGON-1\"?>x", "XML4001")]
    [Arguments(" <?xml encoding=\"UTF-8\"?>x", "XML0007")]
    public async Task TryLoad_InvalidTextDeclaration_IsAFatalFailure_AndTheStreamIsDisposed(
        string content,
        string alias,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var (resolver, loader) = Setup(Utf8(content));

        var (frame, failure) = Load(loader, "e", "e.ent", DocumentUri, cancellationToken);

        await Assert.That(frame).IsNull();
        await Assert.That(failure.Code!.Alias).IsEqualTo(alias);
        await Assert.That(resolver.Streams[0].IsDisposed).IsTrue();
    }

    [Test]
    public async Task TryLoad_DeclaredEncodingContradictsTheByteOrderMark_RaisesXml4002(
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var (resolver, loader) = Setup(Utf16Le("<?xml encoding=\"UTF-8\"?>x"));

        var (frame, failure) = Load(loader, "e", "e.ent", DocumentUri, cancellationToken);

        await Assert.That(frame).IsNull();
        await Assert.That(failure.Code).IsSameReferenceAs(XmlErrorCodes.XML4002);
        await Assert.That(resolver.Streams[0].IsDisposed).IsTrue();
    }

    [Test]
    public async Task Frame_InvalidBytes_AreXml4003AfterTheCharactersBeforeThem_AndTheStreamIsDisposed(
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var (resolver, loader) = Setup([0x61, 0x62, 0xFF, 0x63]);

        var (frame, _) = Load(loader, "e", "e.ent", DocumentUri, cancellationToken);
        var read = Drain(frame!);

        await Assert.That(read.Text).IsEqualTo("ab");
        await Assert.That(read.Error).IsSameReferenceAs(XmlErrorCodes.XML4003);
        await Assert.That(frame!.ErrorByteOffset).IsEqualTo(2);
        await Assert.That(resolver.Streams[0].IsDisposed).IsTrue();
    }

    // A character that is not an XML Char is valid in the encoding, so it is XML0002 and not a decoding error (#712); nothing
    // else checks the characters of an external entity, so the frame does.
    [Test]
    [Arguments(1)]
    [Arguments(3)]
    public async Task Frame_NonXmlCharacter_IsXml0002AfterTheCharactersBeforeIt_AndTheStreamIsDisposed(
        int maxRead,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var (resolver, loader) = Setup([0x61, 0x62, 0x01, 0x63], maxRead);

        var (frame, _) = Load(loader, "e", "e.ent", DocumentUri, cancellationToken);
        var read = Drain(frame!);

        await Assert.That(read.Text).IsEqualTo("ab");
        await Assert.That(read.Error).IsSameReferenceAs(XmlErrorCodes.XML0002);
        await Assert.That(frame!.ErrorByteOffset).IsEqualTo(2);
        await Assert.That(resolver.Streams[0].IsDisposed).IsTrue();
    }

    [Test]
    [Arguments(1)]
    [Arguments(3)]
    [Arguments(7)]
    public async Task Frame_Text_IsIdenticalForEveryReadSize(int maxRead, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        const string text = "<?xml encoding=\"UTF-16\"?>aé<b/>€\U0001F600 line\r\nend\r";
        var (_, whole) = Setup(Utf16Le(text));
        var (_, chunked) = Setup(Utf16Le(text), maxRead);

        var (wholeFrame, _) = Load(whole, "e", "e.ent", DocumentUri, cancellationToken);
        var (chunkedFrame, _) = Load(chunked, "e", "e.ent", DocumentUri, cancellationToken);
        var expected = Drain(wholeFrame!);
        var actual = Drain(chunkedFrame!);

        await Assert.That(expected.Text).IsEqualTo("aé<b/>€\U0001F600 line\nend\n");
        await Assert.That(actual.Text).IsEqualTo(expected.Text);
        await Assert.That(actual.Error).IsNull();
    }

    [Test]
    [Arguments(1)]
    [Arguments(2)]
    [Arguments(int.MaxValue)]
    public async Task Frame_LineEnds_AreNormalizedToLineFeed_AcrossChunks(
        int maxRead,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var (_, loader) = Setup(Utf8("a\r\nb\rc\r\n\r\nd\r"), maxRead);

        var (frame, _) = Load(loader, "e", "e.ent", DocumentUri, cancellationToken);

        await Assert.That(Drain(frame!).Text).IsEqualTo("a\nb\nc\n\nd\n");
    }

    [Test]
    public async Task Frame_ContentOfAMemoryResult_IsRead(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var (resolver, loader) = Setup(Utf8("<?xml encoding=\"UTF-8\"?>mem"), asMemory: true);

        var (frame, failure) = Load(loader, "e", "e.ent", DocumentUri, cancellationToken);

        await Assert.That(failure.Code).IsNull();
        await Assert.That(Drain(frame!).Text).IsEqualTo("mem");
        await Assert.That(resolver.Streams).IsEmpty();
    }

    [Test]
    public async Task Frame_EmptyEntity_ReadsNothing(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var (resolver, loader) = Setup([]);

        var (frame, failure) = Load(loader, "e", "e.ent", DocumentUri, cancellationToken);
        var read = Drain(frame!);

        await Assert.That(failure.Code).IsNull();
        await Assert.That(read.Text).IsEmpty();
        await Assert.That(read.Error).IsNull();
        await Assert.That(resolver.Streams[0].IsDisposed).IsTrue();
    }

    [Test]
    public async Task Disposal_OnSuccess_TheStreamIsDisposedAtTheEndAndAfterTheFrameIsDisposed(
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var (resolver, loader) = Setup(Utf8("abc"));

        var (frame, _) = Load(loader, "e", "e.ent", DocumentUri, cancellationToken);
        var read = Drain(frame!);
        var disposedAtEnd = resolver.Streams[0].IsDisposed;
        frame!.Dispose();
        frame.Dispose();

        await Assert.That(read.Text).IsEqualTo("abc");
        await Assert.That(disposedAtEnd).IsTrue();
        await Assert.That(resolver.Streams[0].IsDisposed).IsTrue();
    }

    [Test]
    public async Task Disposal_BeforeTheEnd_DisposesTheStream(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var (resolver, loader) = Setup(Utf8(new string('a', 100)), maxRead: 10);

        var (frame, _) = Load(loader, "e", "e.ent", DocumentUri, cancellationToken);

        await Assert.That(resolver.Streams[0].IsDisposed).IsFalse();

        frame!.Dispose();

        await Assert.That(resolver.Streams[0].IsDisposed).IsTrue();
    }

    [Test]
    public async Task Disposal_OnADecodingErrorInTheHead_TheStreamIsDisposed(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var (resolver, loader) = Setup([0xC3, 0x28, 0x61]);

        var (frame, _) = Load(loader, "e", "e.ent", DocumentUri, cancellationToken);
        var read = Drain(frame!);

        await Assert.That(read.Error).IsSameReferenceAs(XmlErrorCodes.XML4003);
        await Assert.That(resolver.Streams[0].IsDisposed).IsTrue();
    }

    [Test]
    public async Task Disposal_WhenTheStreamThrowsWhileStarting_TheStreamIsDisposedAndTheExceptionPropagates(
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var (resolver, loader) = Setup(Utf8("abc"), failOnRead: true);

        await Assert.That(() => Load(loader, "e", "e.ent", DocumentUri, cancellationToken)).Throws<IOException>();
        await Assert.That(resolver.Streams[0].IsDisposed).IsTrue();
    }

    [Test]
    public async Task Disposal_WhenTheStreamThrowsWhileReading_TheStreamIsDisposedAndTheExceptionPropagates(
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var (resolver, loader) = Setup(Utf8("abcdef"), maxRead: 2);
        var (frame, _) = Load(loader, "e", "e.ent", DocumentUri, cancellationToken);
        resolver.Streams[0].FailOnRead = true;

        await Assert.That(() => Drain(frame!)).Throws<IOException>();
        await Assert.That(resolver.Streams[0].IsDisposed).IsTrue();
    }

    [Test]
    public async Task Disposal_OnACanceledRead_TheStreamIsDisposed()
    {
        var (resolver, loader) = Setup(Utf8("abcdef"), maxRead: 2);
        using var source = new CancellationTokenSource();
        var (frame, _) = Load(loader, "e", "e.ent", DocumentUri, source.Token);
        await source.CancelAsync();

        await Assert.That(() => Drain(frame!)).Throws<System.OperationCanceledException>();
        await Assert.That(resolver.Streams[0].IsDisposed).IsTrue();
    }

    [Test]
    public async Task Guards_InvalidArguments_Throw(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var loader = Loader(new RecordingResolver());
        var result = ResourceResult.Found(Utf8("x"));

        await Assert
            .That(() =>
                ExternalEntityFrame.TryCreate(
                    loader,
                    (ResourceKind)99,
                    "e",
                    EntityUri,
                    in result,
                    XmlReaderLimits.Default,
                    cancellationToken,
                    out _,
                    out _
                )
            )
            .Throws<ArgumentOutOfRangeException>();
        await Assert.That(() => new ExternalEntityFailure(null!, "message")).Throws<ArgumentNullException>();
        await Assert
            .That(() => new ExternalEntityFailure(XmlErrorCodes.XML2002, null!))
            .Throws<ArgumentNullException>();
    }
}
