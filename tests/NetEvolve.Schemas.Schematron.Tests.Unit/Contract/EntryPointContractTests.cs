namespace NetEvolve.Schemas.Schematron.Tests.Unit.Contract;

using System;
using System.Buffers;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using NetEvolve.Schemas.Formats;
using NetEvolve.Schemas.Schematron.Model;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Schematron.Tests.Unit.Fakes;

public class EntryPointContractTests
{
    private const string SchemaText = "<schema xmlns=\"http://purl.oclc.org/dsdl/schematron\"/>";

    [Test]
    public async Task ForSchematron_FiveOverloads_ReachOneLoaderMethod()
    {
        var pipeline = new FakePipeline();
        var bytes = Encoding.UTF8.GetBytes(SchemaText);
        using var stream = new MemoryStream(bytes);

        SchemaValidator.ForSchematron(stream).UsePipeline(pipeline).BuildNow();
        await Assert.That(pipeline.LoadCount).IsEqualTo(1);
        await Assert.That(pipeline.LastSource!.Kind).IsEqualTo(DocumentSourceKind.Bytes);

        SchemaValidator.ForSchematron(bytes.AsSpan()).UsePipeline(pipeline).BuildNow();
        await Assert.That(pipeline.LoadCount).IsEqualTo(2);
        await Assert.That(pipeline.LastSource!.Kind).IsEqualTo(DocumentSourceKind.Bytes);

        SchemaValidator.ForSchematron(SchemaText.AsSpan()).UsePipeline(pipeline).BuildNow();
        await Assert.That(pipeline.LoadCount).IsEqualTo(3);
        await Assert.That(pipeline.LastSource!.Kind).IsEqualTo(DocumentSourceKind.Chars);

        SchemaValidator.ForSchematron(new ReadOnlySequence<byte>(bytes)).UsePipeline(pipeline).BuildNow();
        await Assert.That(pipeline.LoadCount).IsEqualTo(4);
        await Assert.That(pipeline.LastSource!.Kind).IsEqualTo(DocumentSourceKind.Bytes);

        SchemaValidator
            .ForSchematron(new ReadOnlySequence<char>(SchemaText.ToCharArray()))
            .UsePipeline(pipeline)
            .BuildNow();
        await Assert.That(pipeline.LoadCount).IsEqualTo(5);
        await Assert.That(pipeline.LastSource!.Kind).IsEqualTo(DocumentSourceKind.Chars);
    }

    [Test]
    public async Task ForSchematron_SequenceSource_IsCopiedAcrossSegments(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var first = new Segment(Encoding.UTF8.GetBytes("<sch"));
        var last = first.Append(Encoding.UTF8.GetBytes("ema/>"));
        var sequence = new ReadOnlySequence<byte>(first, 0, last, last.Memory.Length);
        var pipeline = new FakePipeline();

        SchemaValidator.ForSchematron(sequence).UsePipeline(pipeline).BuildNow();

        await Assert
            .That(
                Encoding.UTF8.GetString(
                    pipeline.LastSource!.GetBytes(ValidationLimits.Default, cancellationToken).ToArray()
                )
            )
            .IsEqualTo("<schema/>");
    }

    [Test]
    public async Task ForSchematron_NullStream_ThrowsArgumentNullException() =>
        await Assert.That(() => SchemaValidator.ForSchematron((Stream)null!)).Throws<ArgumentNullException>();

    [Test]
    public async Task ForSchematron_NullArray_BindsToTheSpanOverload_AsAnEmptySource(
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        // Finding: a bare null literal is ambiguous between Stream and the span overloads (CS0121), contrary to the
        // statement of design #137 AP-5; a typed null array binds to the span overload and yields an empty source.
        var pipeline = new FakePipeline();

        SchemaValidator.ForSchematron((byte[])null!).UsePipeline(pipeline).BuildNow();

        await Assert.That(pipeline.LastSource!.Kind).IsEqualTo(DocumentSourceKind.Bytes);
        await Assert
            .That(pipeline.LastSource!.GetBytes(ValidationLimits.Default, cancellationToken).Length)
            .IsEqualTo(0);
    }

    [Test]
    public async Task ForSchematron_SpanSource_IsCopied_MutationAfterCallDoesNotChangeBuild(
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var bytes = Encoding.UTF8.GetBytes(SchemaText);
        var chars = SchemaText.ToCharArray();
        var bytesPipeline = new FakePipeline();
        var charsPipeline = new FakePipeline();

        var bytesBuilder = SchemaValidator.ForSchematron(bytes.AsSpan()).UsePipeline(bytesPipeline);
        var charsBuilder = SchemaValidator.ForSchematron(chars.AsSpan()).UsePipeline(charsPipeline);
        Array.Clear(bytes, 0, bytes.Length);
        Array.Clear(chars, 0, chars.Length);
        bytesBuilder.BuildNow();
        charsBuilder.BuildNow();

        await Assert
            .That(
                Encoding.UTF8.GetString(
                    bytesPipeline.LastSource!.GetBytes(ValidationLimits.Default, cancellationToken).ToArray()
                )
            )
            .IsEqualTo(SchemaText);
        await Assert
            .That(charsPipeline.LastSource!.GetChars(ValidationLimits.Default).ToString())
            .IsEqualTo(SchemaText);
    }

    [Test]
    public async Task ForSchematron_NamedClassAndExtensionMember_ReturnSameBuilderType()
    {
        var overloads = typeof(SchematronSchemaValidatorExtensions)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(method => string.Equals(method.Name, "ForSchematron", StringComparison.Ordinal))
            .ToArray();

        await Assert.That(overloads.Length).IsEqualTo(5);
        foreach (var overload in overloads)
        {
            await Assert.That(overload.ReturnType).IsEqualTo(typeof(SchematronValidatorBuilder));
            var parameters = overload.GetParameters();
            await Assert.That(parameters.Length).IsEqualTo(2);
            await Assert.That(parameters[1].Name).IsEqualTo("systemId");
            await Assert.That(parameters[1].IsOptional).IsTrue();
        }

        var viaNamedClass = SchematronSchemaValidatorExtensions.ForSchematron(SchemaText.AsSpan());
        var viaExtension = SchemaValidator.ForSchematron(SchemaText.AsSpan());

        await Assert.That(viaNamedClass.GetType()).IsEqualTo(viaExtension.GetType());
        await Assert.That(viaNamedClass.Phase).IsEqualTo(viaExtension.Phase);
        await Assert.That(viaNamedClass.Limits).IsSameReferenceAs(viaExtension.Limits);
    }

    [Test]
    public async Task ForSchematron_NoReadOnlyMemoryOverload_KeepsLegacyCompilersUnambiguous()
    {
        var parameterTypes = typeof(SchematronSchemaValidatorExtensions)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(method => string.Equals(method.Name, "ForSchematron", StringComparison.Ordinal))
            .Select(method => method.GetParameters()[0].ParameterType)
            .ToArray();

        await Assert.That(parameterTypes.Contains(typeof(ReadOnlyMemory<byte>))).IsFalse();
        await Assert.That(parameterTypes.Contains(typeof(ReadOnlyMemory<char>))).IsFalse();
    }

    [Test]
    public async Task ForSchematron_Stream_IsNotReadBeforeBuild()
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(SchemaText));

        var builder = SchemaValidator.ForSchematron(stream, "main.sch");

        await Assert.That(stream.Position).IsEqualTo(0L);
        await Assert.That(builder.Phase).IsEqualTo("#DEFAULT");
    }

    [Test]
    public async Task Build_Stream_IsNotDisposed_AndHandedOverFromItsCurrentPosition(
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(SchemaText));
        stream.Position = 3;
        var pipeline = new FakePipeline();

        SchemaValidator.ForSchematron(stream, "main.sch").UsePipeline(pipeline).BuildNow();

        await Assert.That(stream.Position).IsEqualTo(3L);
        await Assert.That(stream.CanRead).IsTrue();
        await Assert
            .That(
                Encoding.UTF8.GetString(
                    pipeline.LastSource!.GetBytes(ValidationLimits.Default, cancellationToken).ToArray()
                )
            )
            .IsEqualTo(SchemaText.Substring(3));
        await Assert.That(pipeline.LastSource!.SystemId).IsEqualTo("main.sch");
    }

    [Test]
    public async Task Build_WithoutAPipelineOverride_UsesTheComposedPipeline()
    {
        var builder = SchemaValidator.ForSchematron(SchemaText.AsSpan());

        var validator = builder.BuildNow();

        await Assert.That(validator.Edition).IsEqualTo(SchematronEdition.Iso2016);
        await Assert.That(validator.EffectivePhase).IsEqualTo("#ALL");
    }

    internal static SchemaCompilationException Capture(Action action)
    {
        try
        {
            action();
        }
        catch (SchemaCompilationException exception)
        {
            return exception;
        }

        throw new InvalidOperationException("The action did not throw a SchemaCompilationException.");
    }

    private sealed class Segment : ReadOnlySequenceSegment<byte>
    {
        internal Segment(byte[] bytes) => Memory = bytes;

        internal Segment Append(byte[] bytes)
        {
            var next = new Segment(bytes) { RunningIndex = RunningIndex + Memory.Length };
            Next = next;
            return next;
        }
    }
}
