namespace NetEvolve.Schemas.Schematron.Svrl;

using System;
using System.Buffers;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Schematron.Diagnostics;

/// <summary>Writes the events of a run as SVRL through the Xml package writer.</summary>
/// <remarks>Limitation: until work package SCH-15 lands, every callback throws a <see cref="SchemaCompilationException"/> with <see cref="SchematronDiagnosticCodes.NotImplemented"/>.</remarks>
public sealed class SvrlReportSink : ISchematronReportSink, IAsyncDisposable, IDisposable
{
    /// <summary>Initializes a new instance of the <see cref="SvrlReportSink"/> class that writes through a buffer writer; complete at <see cref="Finish"/>.</summary>
    /// <param name="output">The output.</param>
    /// <param name="options">The options, <see cref="SvrlWriterOptions.Default"/> when <see langword="null"/>.</param>
    public SvrlReportSink(IBufferWriter<byte> output, SvrlWriterOptions? options = null)
    {
        Argument.ThrowIfNull(output);

        Options = options ?? SvrlWriterOptions.Default;
    }

    /// <summary>Initializes a new instance of the <see cref="SvrlReportSink"/> class that buffers and writes to a stream at <see cref="CompleteAsync"/>.</summary>
    /// <param name="output">The output. The caller keeps ownership.</param>
    /// <param name="options">The options, <see cref="SvrlWriterOptions.Default"/> when <see langword="null"/>.</param>
    public SvrlReportSink(Stream output, SvrlWriterOptions? options = null)
    {
        Argument.ThrowIfNull(output);

        Options = options ?? SvrlWriterOptions.Default;
    }

    /// <summary>Gets a value indicating whether the output was fully written.</summary>
    public bool IsCompleted { get; private set; }

    internal SvrlWriterOptions Options { get; }

    /// <inheritdoc />
    public void Begin(in SchematronRunInfo info) => throw NotImplementedFailure.For("SVRL writer");

    /// <inheritdoc />
    public void ActivePattern(in SchematronPatternEvent pattern) => throw NotImplementedFailure.For("SVRL writer");

    /// <inheritdoc />
    public void FiredRule(in SchematronRuleEvent rule) => throw NotImplementedFailure.For("SVRL writer");

    /// <inheritdoc />
    public void Finding(in SchematronFinding finding) => throw NotImplementedFailure.For("SVRL writer");

    /// <inheritdoc />
    public void ErrorOccurred(in SchematronErrorEvent errorEvent) => throw NotImplementedFailure.For("SVRL writer");

    /// <inheritdoc />
    public void Finish(in SchematronRunSummary summary) => throw NotImplementedFailure.For("SVRL writer");

    /// <summary>Flushes the buffered output of a <see cref="Stream"/> writer asynchronously.</summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that completes when the output was written.</returns>
    public ValueTask CompleteAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return IsCompleted ? default : throw NotImplementedFailure.For("SVRL writer");
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync() => default;

    /// <inheritdoc />
    public void Dispose() { }
}
