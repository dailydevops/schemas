namespace NetEvolve.Schemas.Schematron.Tests.Unit.Contract;

using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NetEvolve.Schemas.Formats;
using NetEvolve.Schemas.Resources;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Schematron.Svrl;
using NetEvolve.Schemas.Schematron.Tests.Unit.Fakes;

public class StubStageTests
{
    private static async Task AssertNotImplemented(Action action)
    {
        var exception = EntryPointContractTests.Capture(action);

        await Assert.That(exception.Issues.Count).IsEqualTo(1);
        await Assert.That(exception.Issues[0].Code).IsEqualTo(SchematronDiagnosticCodes.NotImplemented);
        await Assert.That(exception.Issues[0].Severity).IsEqualTo(IssueSeverity.Error);
    }

    private static BuildSettings Settings() =>
        new(
            "#DEFAULT",
            new Dictionary<string, string>(StringComparer.Ordinal),
            ValidationLimits.Default,
            DenyAllResourceResolver.Instance
        );

    private static SchemaSourceHolder Source() => SchemaSourceHolder.FromChars("x".AsSpan(), null);

    [Test]
    public async Task StubStage_Pipeline_Throws_DiagnosticWithNotImplementedCode() =>
        await AssertNotImplemented(() =>
            NotImplementedPipeline.Instance.Load(Source(), Settings(), CancellationToken.None)
        );

    [Test]
    public async Task StubStage_Parser_Throws_DiagnosticWithNotImplementedCode() =>
        await AssertNotImplemented(() =>
            _ = new NotImplementedModelParser().TryParse(Source(), new FakeProblemSink(), out _)
        );

    [Test]
    public async Task StubStage_Resolver_Throws_DiagnosticWithNotImplementedCode() =>
        await AssertNotImplemented(() =>
            _ = new NotImplementedModelResolver().TryResolve(
                new ParsedModule(FakeModel.Schema()),
                new FakeModuleLoader(),
                new FakeProblemSink(),
                out _
            )
        );

    [Test]
    public async Task StubStage_Expander_Throws_DiagnosticWithNotImplementedCode() =>
        await AssertNotImplemented(() =>
            _ = new NotImplementedModelExpander().TryExpand(
                new ResolvedModel(FakeModel.Schema()),
                Settings(),
                new FakeProblemSink(),
                out _
            )
        );

    [Test]
    public async Task StubStage_Compiler_Throws_DiagnosticWithNotImplementedCode() =>
        await AssertNotImplemented(() =>
            _ = new NotImplementedSchemaCompiler().TryCompile(
                FakeModel.Schema(),
                Settings(),
                new FakeProblemSink(),
                out _
            )
        );

    [Test]
    public async Task SvrlSink_Dispose_NeverThrows_AndLeavesTheSinkIncomplete()
    {
        await using var sink = new SvrlReportSink(new ArrayBufferWriter<byte>());

        await Assert.That(sink.IsCompleted).IsFalse();
    }

    [Test]
    public async Task SvrlSink_StreamConstructor_KeepsOptions()
    {
        using var stream = new MemoryStream();
        var options = new SvrlWriterOptions(indent: true);
        await using var custom = new SvrlReportSink(stream, options);
        await using var defaulted = new SvrlReportSink(stream);

        await Assert.That(custom.Options).IsSameReferenceAs(options);
        await Assert.That(defaulted.Options).IsSameReferenceAs(SvrlWriterOptions.Default);
    }

    [Test]
    public async Task SvrlSink_NullOutput_Throws()
    {
        await Assert.That(() => NewSink((Stream)null!)).Throws<ArgumentNullException>();
        await Assert.That(() => NewSink((IBufferWriter<byte>)null!)).Throws<ArgumentNullException>();
    }

    private static SvrlReportSink NewSink(Stream output) => new(output);

    private static SvrlReportSink NewSink(IBufferWriter<byte> output) => new(output);
}
