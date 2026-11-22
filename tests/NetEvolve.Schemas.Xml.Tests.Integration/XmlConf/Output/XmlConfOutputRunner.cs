namespace NetEvolve.Schemas.Xml.Tests.Integration.XmlConf.Output;

using System.Text;
using NetEvolve.Schemas.Conformance.Results;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Reader.Pipeline;

/// <summary>
/// Reads the input of every selected case that has an output file, writes it in the canonical form of the suite, and
/// compares the bytes with the output file.
/// </summary>
/// <remarks>
/// <para>
/// A document with a document type declaration is read with DTD processing on, so the attribute defaults, the entity
/// expansion, and the normalization by the declared types show in the output (XML-16). The output is the canonical form of
/// <c>xmltest/canonxml.html</c>, written by <see cref="CanonicalXmlConfSink"/>, and, for a document that declares notations,
/// the second form of the output files: a document type declaration that lists the notations by name.
/// </para>
/// <para>
/// The comparand is not the canonical mode of <see cref="NetEvolve.Schemas.Xml.Writer.XmlWriter"/>, which is a documented
/// subset (it keeps comments, writes a processing instruction without the space after its target, and escapes fewer
/// characters in text, DEF-65), so the output files of the suite cannot all match it byte for byte.
/// Limitations: a case that needs an external entity (the <c>ENTITIES</c> tag of the catalog is not <c>none</c>), a case that
/// needs namespace processing switched off, and a case whose output lists a processing instruction of the internal subset
/// are not compared; each is recorded as failed with its own reason (<see cref="NeedsExternalEntities"/>,
/// <see cref="NeedsNamespacesOff"/>, <see cref="NeedsSubsetProcessingInstruction"/>) and stays in the baseline.
/// </para>
/// </remarks>
internal static class XmlConfOutputRunner
{
    /// <summary>The reason of a case that is not compared because it needs an external entity.</summary>
    public const string NeedsExternalEntities = "needs external entities";

    /// <summary>The reason of a case that is not compared because it needs namespace processing switched off, which the reader has no option for.</summary>
    public const string NeedsNamespacesOff = "needs namespace processing off";

    /// <summary>The reason of a case that is not compared because its output lists a processing instruction of the internal subset, which the reader does not report as an event.</summary>
    public const string NeedsSubsetProcessingInstruction = "needs a processing instruction of the internal subset";

    private const string SubsetProcessingInstructionCase = "ibm/ibm-valid-P29-ibm29v01.xml";

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
                xmlConfCase,
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
        XmlConfCase xmlConfCase,
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
        var hasDoctype = HasDoctype(input);
        if (hasDoctype && xmlConfCase.Entities != XmlConfEntities.None)
        {
            return NeedsExternalEntities;
        }

        if (!xmlConfCase.Namespaces)
        {
            return NeedsNamespacesOff;
        }

        if (string.Equals(xmlConfCase.Id, SubsetProcessingInstructionCase, StringComparison.Ordinal))
        {
            return NeedsSubsetProcessingInstruction;
        }

        var (actual, failure) = Render(input, hasDoctype);
        return failure
            ?? XmlConfOutputComparer.Compare(await File.ReadAllBytesAsync(outputPath, cancellationToken), actual);
    }

    private static (byte[] Bytes, string? Failure) Render(byte[] input, bool hasDoctype)
    {
        using var reader = XmlPipelineReader.Create(
            input.AsSpan(),
            hasDoctype ? XmlReaderOptions.Default.WithDtdProcessing(true) : null
        );
        var sink = new CanonicalXmlConfSink();

        return reader.Pump(ref sink) == XmlReadStatus.Completed
            ? (Encoding.UTF8.GetBytes(NotationDeclarations(reader) + sink.Output), null)
            : ([], $"The reader reported a fatal error {reader.Diagnostic?.Code.Alias}.");
    }

    // The second canonical form: the notations by name in ascending order, each on its own line, inside a document type declaration.
    private static string NotationDeclarations(XmlPipelineReader reader)
    {
        if (reader.Declarations is not { Notations.Count: > 0 } declarations)
        {
            return string.Empty;
        }

        var builder = new StringBuilder("<!DOCTYPE ").Append(declarations.Name).Append(" [\n");
        foreach (var notation in declarations.Notations.Values.OrderBy(n => n.Name, StringComparer.Ordinal))
        {
            _ = builder.Append("<!NOTATION ").Append(notation.Name);
            _ = builder.Append(Identifiers(notation));
            _ = builder.Append(">\n");
        }

        return builder.Append("]>\n").ToString();
    }

    private static string Identifiers(XmlNotation notation) =>
        (notation.PublicId, notation.SystemId) switch
        {
            (not null, not null) => " PUBLIC '" + notation.PublicId + "' '" + notation.SystemId + "'",
            (not null, null) => " PUBLIC '" + notation.PublicId + "'",
            _ => " SYSTEM '" + notation.SystemId + "'",
        };

    // The same detection as the cases of the pipeline reader: UTF-16 by its byte order mark, otherwise one byte per character.
    private static bool HasDoctype(byte[] bytes) =>
        (
            bytes is [0xFF, 0xFE, ..] ? Encoding.Unicode.GetString(bytes)
            : bytes is [0xFE, 0xFF, ..] ? Encoding.BigEndianUnicode.GetString(bytes)
            : Encoding.Latin1.GetString(bytes)
        ).Contains("<!DOCTYPE", StringComparison.Ordinal);
}
