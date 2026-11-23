namespace NetEvolve.Schemas.Xsd.Tests.Unit.Validation.Assembly;

using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using XsdVersion = NetEvolve.Schemas.Xml.Datatypes.Contracts.XsdVersion;

public class BuilderTests
{
    private static readonly Encoding Utf8 = new UTF8Encoding(false);

    [Test]
    public async Task AddSchemaAndWithDtdProcessing_ReturnTheSameBuilder()
    {
        var builder = SchemaValidator.ForXsd();
        using var stream = new MemoryStream();

        await Assert.That(builder.AddSchema("<a/>".AsMemory())).IsSameReferenceAs(builder);
        await Assert.That(builder.AddSchema(Utf8.GetBytes("<a/>").AsMemory())).IsSameReferenceAs(builder);
        await Assert.That(builder.AddSchema(stream, "id:1")).IsSameReferenceAs(builder);
        await Assert.That(builder.WithDtdProcessing(true)).IsSameReferenceAs(builder);
    }

    [Test]
    public async Task AddSchema_NullStream_Throws()
    {
        var builder = SchemaValidator.ForXsd();

        await Assert.That(() => builder.AddSchema((Stream)null!)).Throws<ArgumentNullException>();
    }

    [Test]
    public async Task Build_CharactersBytesAndStream_GiveEquivalentValidators()
    {
        var bytes = Utf8.GetBytes(AssemblyFixture.Schema);
        using var stream = new MemoryStream(bytes);

        var validators = BuildFromEverySource(bytes, stream);

        var expected = AssemblyFixture.Run(validators[0], AssemblyFixture.InvalidInstance);
        foreach (var validator in validators)
        {
            await Assert
                .That(AssemblyFixture.Same(AssemblyFixture.Run(validator, AssemblyFixture.InvalidInstance), expected))
                .IsTrue();
        }

        await Assert.That(expected.IsValid).IsFalse();
        await Assert.That(stream.CanRead).IsTrue().Because("the builder never disposes a stream");
    }

    [Test]
    public async Task BuildAsync_ReadsAStreamWithReadAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var stream = new AsyncOnlyStream(Utf8.GetBytes(AssemblyFixture.Schema));
        var builder = SchemaValidator.ForXsd().AddSchema(stream, AssemblyFixture.SchemaId);

        var validator = await builder.BuildAsync(cancellationToken);

        await Assert.That(AssemblyFixture.Run(validator, AssemblyFixture.ValidInstance).IsValid).IsTrue();
    }

    [Test]
    public async Task BuildAsync_MemorySources_CompleteWithoutTheStreamPath(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var validator = await SchemaValidator
            .ForXsd()
            .AddSchema(AssemblyFixture.Schema.AsMemory(), AssemblyFixture.SchemaId)
            .BuildAsync(cancellationToken);

        await Assert.That(validator.Version).IsEqualTo(XsdVersion.V11);
    }

    [Test]
    public async Task BuildAsync_CanceledToken_IsCanceled()
    {
        using var source = new CancellationTokenSource();
#if NET10_0_OR_GREATER
        await source.CancelAsync();
#else
        source.Cancel();
#endif
        var builder = SchemaValidator.ForXsd().AddSchema(AssemblyFixture.Schema.AsMemory());

        await Assert.That(async () => await builder.BuildAsync(source.Token)).Throws<OperationCanceledException>();
    }

    [Test]
    public async Task BuildAsync_TokenCanceledWhileReadingAStream_IsCanceled()
    {
        using var source = new CancellationTokenSource();
        using var stream = new CancelingStream(Utf8.GetBytes(AssemblyFixture.Schema), source);
        var builder = SchemaValidator.ForXsd().AddSchema(stream, AssemblyFixture.SchemaId);

        await Assert.That(async () => await builder.BuildAsync(source.Token)).Throws<OperationCanceledException>();
    }

    [Test]
    public async Task Build_InvalidSchema_ThrowsWithTheLocatedIssues()
    {
        var schema = AssemblyFixture.WrapSchema("<xs:element name=\"e\" type=\"t:missing\"/>");

        var exception = AssemblyFixture.Failure(() => AssemblyFixture.Build(schema));

        await Assert.That(exception.Issues.Count).IsGreaterThanOrEqualTo(1);
        await Assert.That(exception.Issues[0].Location.Source).IsEqualTo(AssemblyFixture.SchemaId);
        await Assert.That(exception.Issues[0].Location.Line).IsGreaterThan(0);
    }

    [Test]
    public async Task Build_SchemaThatIsNotWellFormed_ThrowsWithAnIssue()
    {
        var exception = AssemblyFixture.Failure(() => AssemblyFixture.Build("<xs:schema"));

        await Assert.That(exception.Issues.Count).IsGreaterThanOrEqualTo(1);
    }

    [Test]
    public async Task Build_WithoutSchemas_Succeeds_AndEveryRootIsUndeclared()
    {
        var result = AssemblyFixture.Run(WithoutSchemas(), "<anything/>");

        await Assert.That(result.IsValid).IsFalse();
        await Assert.That(result.Issues.Any(i => i.Code == XsdRuleCodes.CvcAssessElt)).IsTrue();
    }

    [Test]
    public async Task Build_SeveralSchemas_CompileAsOneSchema()
    {
        var validator = BuildTwoSchemas();

        var first = AssemblyFixture.Run(validator, AssemblyFixture.ValidInstance);
        var second = AssemblyFixture.Run(validator, "<other xmlns=\"urn:o\">x</other>");

        await Assert.That(first.IsValid).IsTrue();
        await Assert.That(second.IsValid).IsTrue();
    }

    [Test]
    public async Task Build_SchemaLargerThanMaxInputSize_ThrowsWithTheInputLimitCode()
    {
        foreach (var build in OversizedBuilds())
        {
            var exception = AssemblyFixture.Failure(build);

            await Assert.That(exception.Issues[0].Code).IsEqualTo(CoreDiagnosticCodes.InputSizeLimitExceeded);
        }
    }

    [Test]
    public async Task BuildAsync_StreamLargerThanMaxInputSize_ThrowsWithTheInputLimitCode(
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var stream = new UnseekableStream(Utf8.GetBytes(AssemblyFixture.Schema));
        var builder = SchemaValidator
            .ForXsd()
            .WithLimits(ValidationLimits.Default.WithMaxInputSize(10))
            .AddSchema(stream);

        var exception = await Assert
            .That(async () => await builder.BuildAsync(cancellationToken))
            .Throws<SchemaCompilationException>();

        await Assert.That(exception!.Issues[0].Code).IsEqualTo(CoreDiagnosticCodes.InputSizeLimitExceeded);
    }

    [Test]
    public async Task Build_StreamIsReadFromItsCurrentPosition()
    {
        var bytes = Utf8.GetBytes("<!-- junk -->" + AssemblyFixture.Schema);
        using var stream = new MemoryStream(bytes) { Position = "<!-- junk -->".Length };

        var validator = BuildFromStream(stream);

        await Assert.That(AssemblyFixture.Run(validator, AssemblyFixture.ValidInstance).IsValid).IsTrue();
    }

    private static IXsdValidator BuildFromStream(Stream stream) =>
        SchemaValidator.ForXsd().AddSchema(stream, AssemblyFixture.SchemaId).Build();

    private static IXsdValidator WithoutSchemas() => SchemaValidator.ForXsd().Build();

    private static IXsdValidator[] BuildFromEverySource(byte[] bytes, Stream stream) =>
        [
            SchemaValidator.ForXsd().AddSchema(AssemblyFixture.Schema.AsMemory(), AssemblyFixture.SchemaId).Build(),
            SchemaValidator.ForXsd().AddSchema(bytes.AsMemory(), AssemblyFixture.SchemaId).Build(),
            BuildFromStream(stream),
        ];

    private static IXsdValidator BuildTwoSchemas()
    {
        var other = AssemblyFixture
            .WrapSchema("<xs:element name=\"other\" type=\"xs:string\"/>")
            .Replace("urn:t", "urn:o", StringComparison.Ordinal);

        return SchemaValidator
            .ForXsd()
            .AddSchema(AssemblyFixture.Schema.AsMemory(), AssemblyFixture.SchemaId)
            .AddSchema(Utf8.GetBytes(other).AsMemory(), "file:///o.xsd")
            .Build();
    }

    private static Func<IXsdValidator>[] OversizedBuilds()
    {
        var limits = ValidationLimits.Default.WithMaxInputSize(10);
        var bytes = Utf8.GetBytes(AssemblyFixture.Schema);

        return
        [
            () => SchemaValidator.ForXsd().WithLimits(limits).AddSchema(AssemblyFixture.Schema.AsMemory()).Build(),
            () => SchemaValidator.ForXsd().WithLimits(limits).AddSchema(bytes.AsMemory()).Build(),
            () =>
            {
                using var stream = new MemoryStream(bytes);
                return SchemaValidator.ForXsd().WithLimits(limits).AddSchema(stream).Build();
            },
            () =>
            {
                using var stream = new UnseekableStream(bytes);
                return SchemaValidator.ForXsd().WithLimits(limits).AddSchema(stream).Build();
            },
        ];
    }

    private sealed class AsyncOnlyStream(byte[] content) : Stream
    {
        private readonly MemoryStream _inner = new(content);

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return _inner.ReadAsync(buffer, offset, count, cancellationToken);
        }

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return _inner.ReadAsync(buffer, cancellationToken);
        }

        public override void Flush() { }

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _inner.Dispose();
            }

            base.Dispose(disposing);
        }
    }

    private sealed class UnseekableStream(byte[] content) : MemoryStream(content)
    {
        public override bool CanSeek => false;
    }

    private sealed class CancelingStream(byte[] content, CancellationTokenSource source) : MemoryStream(content)
    {
        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Fire();
            return base.ReadAsync(buffer, cancellationToken);
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Fire();
            return base.ReadAsync(buffer, offset, count, cancellationToken);
        }

        private void Fire() => source.Cancel();
    }
}
