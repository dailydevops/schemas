namespace NetEvolve.Schemas.Xml.Tests.Integration.XmlConf.Output;

using System.Buffers;
using System.Text;
using NetEvolve.Schemas.Conformance.Results;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Reader.Pipeline;
using NetEvolve.Schemas.Xml.Text.NameTable;
using NetEvolve.Schemas.Xml.Writer;

/// <summary>
/// Reads the input of every selected case that has an output file, writes it in canonical form, and compares the bytes
/// with the output file.
/// </summary>
/// <remarks>
/// Limitation: a case with a document type declaration is not compared. Its output depends on attribute defaults,
/// entity expansion, or attribute normalization from the declaration, which belong to XML-16, so it is recorded as
/// failed with the reason <see cref="NeedsDtdProcessing"/> and stays in the baseline.
/// </remarks>
internal static class XmlConfOutputRunner
{
    /// <summary>The reason of a case that is not compared because it has a document type declaration.</summary>
    public const string NeedsDtdProcessing = "needs DTD processing";

    /// <summary>Runs the comparison.</summary>
    /// <param name="selection">The selection; excluded cases are not part of the run.</param>
    /// <param name="outputs">The output file of each case by case id, see <see cref="XmlConfOutputReference"/>.</param>
    /// <param name="suiteRoot">The suite root directory.</param>
    /// <param name="cancellationToken">A token to cancel the run.</param>
    /// <returns>One result per selected case that has an output file, in catalog order.</returns>
    public static async Task<IReadOnlyList<CaseResult>> RunAsync(
        XmlConfSelection selection,
        IReadOnlyDictionary<string, string> outputs,
        string suiteRoot,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(selection);
        ArgumentNullException.ThrowIfNull(outputs);
        ArgumentNullException.ThrowIfNull(suiteRoot);
        var results = new List<CaseResult>();
        foreach (var xmlConfCase in selection.Selected)
        {
            if (!outputs.TryGetValue(xmlConfCase.Id, out var output))
            {
                continue;
            }

            var failure = await JudgeAsync(
                Path.Combine(suiteRoot, xmlConfCase.Uri),
                Path.Combine(suiteRoot, output),
                cancellationToken
            );
            results.Add(
                failure is null
                    ? new CaseResult(xmlConfCase.Id, xmlConfCase.TestSet, CaseOutcome.Pass)
                    : new CaseResult(xmlConfCase.Id, xmlConfCase.TestSet, CaseOutcome.Fail, failure)
            );
        }

        return results;
    }

    private static async Task<string?> JudgeAsync(
        string inputPath,
        string outputPath,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!File.Exists(inputPath))
        {
            return "The input document is missing from the pinned archive.";
        }

        if (!File.Exists(outputPath))
        {
            return "The output file is missing from the pinned archive.";
        }

        var input = await File.ReadAllBytesAsync(inputPath, cancellationToken);
        if (HasDoctype(input))
        {
            return NeedsDtdProcessing;
        }

        var (actual, failure) = Render(input);
        return failure
            ?? XmlConfOutputComparer.Compare(await File.ReadAllBytesAsync(outputPath, cancellationToken), actual);
    }

    private static (byte[] Bytes, string? Failure) Render(byte[] input)
    {
        var output = new ArrayBufferWriter<byte>();
        using var reader = XmlPipelineReader.Create(input.AsSpan());
        using var writer = new XmlWriter(
            output,
            new SpanInterningNameTable(XmlReaderLimits.Default.MaxDistinctNames),
            new XmlWriterOptions(canonical: true)
        );
        try
        {
            var sink = writer;
            return reader.Pump(ref sink) == XmlReadStatus.Completed
                ? (output.WrittenSpan.ToArray(), null)
                : ([], $"The reader reported a fatal error {reader.Diagnostic?.Code.Alias}.");
        }
        catch (XmlWriteException exception)
        {
            return ([], $"The writer rejected the events: {exception.Message}");
        }
    }

    // The same detection as the cases of the pipeline reader: UTF-16 by its byte order mark, otherwise one byte per character.
    private static bool HasDoctype(byte[] bytes) =>
        (
            bytes is [0xFF, 0xFE, ..] ? Encoding.Unicode.GetString(bytes)
            : bytes is [0xFE, 0xFF, ..] ? Encoding.BigEndianUnicode.GetString(bytes)
            : Encoding.Latin1.GetString(bytes)
        ).Contains("<!DOCTYPE", StringComparison.Ordinal);
}
