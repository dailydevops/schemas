namespace NetEvolve.Schemas.Xml.Tests.Unit.Dtd.Entry;

using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NetEvolve.Schemas.Xml.Tests.Unit.Input.Sequence;

/// <summary>
/// The documents and the helpers of the tests of the DTD entry point. Spans cannot cross an <see langword="await"/>, so every
/// span and sequence is built and consumed in a synchronous method.
/// </summary>
internal static class DtdEntryFixture
{
    internal const string SystemId = "http://example.org/doc/main.xml";

    // Two different validity errors on different lines; the comment holds a two-byte and a three-byte character, so a byte
    // column and a character column of <b/> differ.
    internal const string TwoErrors =
        "<!DOCTYPE r [<!ELEMENT r (a)><!ELEMENT a EMPTY><!ATTLIST a id ID #REQUIRED>]>\n<r>\n  <a/>\n  <!-- é€ --><b/>\n</r>";

    // What every input kind reports for TwoErrors: code, severity, system identifier, line, column, and constraint.
    internal static readonly string[] TwoErrorsIssues =
    [
        "NES1520|Error|http://example.org/doc/main.xml|3|3|Required Attribute",
        "NES1504|Error|http://example.org/doc/main.xml|4|14|Element Valid",
        "NES1504|Error|http://example.org/doc/main.xml|4|14|Element Valid",
    ];

    internal const string ValidNote = "<!DOCTYPE note [<!ELEMENT note (#PCDATA)>]><note>text</note>";

    internal const string ExternalSubsetDocument = "<!DOCTYPE r SYSTEM \"ext.dtd\">\n<r/>";

    internal static byte[] Utf8(string text) => Encoding.UTF8.GetBytes(text);

    internal static byte[] Utf16(string text)
    {
        var preamble = Encoding.Unicode.GetPreamble();
        var body = Encoding.Unicode.GetBytes(text);
        var bytes = new byte[preamble.Length + body.Length];
        preamble.CopyTo(bytes, 0);
        body.CopyTo(bytes, preamble.Length);

        return bytes;
    }

    /// <summary>Builds a validator from a builder that <paramref name="configure"/> changes.</summary>
    internal static IDtdValidator Build(Func<DtdValidatorBuilder, DtdValidatorBuilder>? configure = null)
    {
        var builder = SchemaValidator.ForDtd();

        return (configure is null ? builder : configure(builder)).Build();
    }

    /// <summary>Validates characters with a validator that <paramref name="configure"/> changes.</summary>
    internal static ValidationResult Run(
        string xml,
        Func<DtdValidatorBuilder, DtdValidatorBuilder>? configure = null,
        string? systemId = SystemId
    ) => Build(configure).Validate(xml.AsSpan(), systemId);

    /// <summary>Validates characters with a built validator.</summary>
    internal static ValidationResult Run(IDtdValidator validator, string xml, string? systemId = SystemId) =>
        validator.Validate(xml.AsSpan(), systemId);

    /// <summary>One line per issue: code, severity, system identifier, line, column, and constraint.</summary>
    internal static string[] Describe(ValidationResult result)
    {
        var lines = new List<string>();
        foreach (var issue in result.Issues)
        {
            lines.Add(
                $"{issue.Code}|{issue.Severity}|{issue.Location.Source}|{issue.Location.Line}|{issue.Location.Column}|{issue.Constraint}"
            );
        }

        return [.. lines];
    }

    /// <summary>Validates the document through every synchronous input kind.</summary>
    internal static List<(string Kind, ValidationResult Result)> SyncKinds(
        IDtdValidator validator,
        string xml,
        string? systemId = SystemId
    )
    {
        var chars = xml.ToCharArray();
        var bytes = Utf8(xml);
        var results = new List<(string, ValidationResult)>();
        using (var stream = new MemoryStream(bytes))
        {
            results.Add(("stream", validator.Validate(stream, systemId)));
        }

        results.Add(("chars", validator.Validate(xml.AsSpan(), systemId)));
        results.Add(("bytes", validator.Validate(bytes.AsSpan(), systemId)));
        results.Add(("utf-16 bytes", validator.Validate(Utf16(xml).AsSpan(), systemId)));
        results.Add(("one char segment", validator.Validate(SequenceBuilder.Whole(chars), systemId)));
        results.Add(("one byte segment", validator.Validate(SequenceBuilder.Whole(bytes), systemId)));
        results.Add(("cut char sequence", validator.Validate(SequenceBuilder.Chunks(chars, 13), systemId)));
        results.Add(("single chars", validator.Validate(SequenceBuilder.Chunks(chars, 1), systemId)));
        results.Add(
            ("empty segments", validator.Validate(SequenceBuilder.Chunks(chars, 7, withEmptySegments: true), systemId))
        );
        results.Add(("byte sequence", validator.Validate(SequenceBuilder.Chunks(bytes, 3), systemId)));
        results.Add(("single bytes", validator.Validate(SequenceBuilder.Chunks(bytes, 1), systemId)));

        return results;
    }

    /// <summary>Validates the document through every asynchronous input kind.</summary>
    internal static async Task<List<(string Kind, ValidationResult Result)>> AsyncKinds(
        IDtdValidator validator,
        string xml,
        CancellationToken cancellationToken,
        string? systemId = SystemId
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var chars = xml.ToCharArray();
        var bytes = Utf8(xml);
        var results = new List<(string, ValidationResult)>();
        using (var stream = new MemoryStream(bytes))
        {
            results.Add(("stream", await validator.ValidateAsync(stream, systemId, cancellationToken)));
        }

        results.Add(("chars", await validator.ValidateAsync(xml.AsMemory(), systemId, cancellationToken)));
        results.Add(
            ("bytes", await validator.ValidateAsync(new ReadOnlyMemory<byte>(bytes), systemId, cancellationToken))
        );
        results.Add(
            (
                "char sequence",
                await validator.ValidateAsync(SequenceBuilder.Chunks(chars, 9), systemId, cancellationToken)
            )
        );
        results.Add(
            (
                "byte sequence",
                await validator.ValidateAsync(SequenceBuilder.Chunks(bytes, 9), systemId, cancellationToken)
            )
        );

        return results;
    }
}
