namespace NetEvolve.Schemas.Schematron.Tests.Unit.Contract;

using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
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

    private static SchemaSource Source() => SchemaSource.FromChars("x".AsSpan(), null);

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
                new ParsedModule(),
                new FakeModuleLoader(),
                new FakeProblemSink(),
                out _
            )
        );

    [Test]
    public async Task StubStage_Expander_Throws_DiagnosticWithNotImplementedCode() =>
        await AssertNotImplemented(() =>
            _ = new NotImplementedModelExpander().TryExpand(
                new ResolvedModel(),
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
    public async Task StubStage_SvrlSink_EveryCallback_ThrowsNotImplemented()
    {
        var failures = SvrlCallbackFailures(out var isCompleted);

        await Assert.That(failures.Count).IsEqualTo(7);
        foreach (var failure in failures)
        {
            await Assert.That(failure.Issues.Count).IsEqualTo(1);
            await Assert.That(failure.Issues[0].Code).IsEqualTo(SchematronDiagnosticCodes.NotImplemented);
        }

        await Assert.That(isCompleted).IsFalse();
    }

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

    private static void Observe(ValueTask pending) => GC.KeepAlive(pending);

    private static void Record(List<SchemaCompilationException> failures, Action action)
    {
        try
        {
            action();
        }
        catch (SchemaCompilationException exception)
        {
            failures.Add(exception);
        }
    }

    // The event structs are ref structs: they live in this synchronous helper, never in an async test method.
    private static List<SchemaCompilationException> SvrlCallbackFailures(out bool isCompleted)
    {
        using var sink = new SvrlReportSink(new ArrayBufferWriter<byte>());
        var failures = new List<SchemaCompilationException>();

        var info = new SchematronRunInfo(default, default, default, default, [], default);
        TryBegin(sink, in info, failures);
        var pattern = new SchematronPatternEvent(default, default, default, default, false);
        TryPattern(sink, in pattern, failures);
        var rule = new SchematronRuleEvent(default, default, default, default, default);
        TryRule(sink, in rule, failures);
        var finding = new SchematronFinding(
            false,
            default,
            default,
            default,
            default,
            default,
            default,
            default,
            default,
            default,
            default,
            default,
            null
        );
        TryFinding(sink, in finding, failures);
        var error = new SchematronErrorEvent(default, default, default, default, null);
        TryError(sink, in error, failures);
        var summary = new SchematronRunSummary(false, default);
        TryFinish(sink, in summary, failures);
        Record(failures, () => Observe(sink.CompleteAsync()));

        isCompleted = sink.IsCompleted;
        return failures;
    }

    private static void TryBegin(
        SvrlReportSink sink,
        in SchematronRunInfo value,
        List<SchemaCompilationException> failures
    )
    {
        try
        {
            sink.Begin(in value);
        }
        catch (SchemaCompilationException exception)
        {
            failures.Add(exception);
        }
    }

    private static void TryPattern(
        SvrlReportSink sink,
        in SchematronPatternEvent value,
        List<SchemaCompilationException> failures
    )
    {
        try
        {
            sink.ActivePattern(in value);
        }
        catch (SchemaCompilationException exception)
        {
            failures.Add(exception);
        }
    }

    private static void TryRule(
        SvrlReportSink sink,
        in SchematronRuleEvent value,
        List<SchemaCompilationException> failures
    )
    {
        try
        {
            sink.FiredRule(in value);
        }
        catch (SchemaCompilationException exception)
        {
            failures.Add(exception);
        }
    }

    private static void TryFinding(
        SvrlReportSink sink,
        in SchematronFinding value,
        List<SchemaCompilationException> failures
    )
    {
        try
        {
            sink.Finding(in value);
        }
        catch (SchemaCompilationException exception)
        {
            failures.Add(exception);
        }
    }

    private static void TryError(
        SvrlReportSink sink,
        in SchematronErrorEvent value,
        List<SchemaCompilationException> failures
    )
    {
        try
        {
            sink.ErrorOccurred(in value);
        }
        catch (SchemaCompilationException exception)
        {
            failures.Add(exception);
        }
    }

    private static void TryFinish(
        SvrlReportSink sink,
        in SchematronRunSummary value,
        List<SchemaCompilationException> failures
    )
    {
        try
        {
            sink.Finish(in value);
        }
        catch (SchemaCompilationException exception)
        {
            failures.Add(exception);
        }
    }
}
