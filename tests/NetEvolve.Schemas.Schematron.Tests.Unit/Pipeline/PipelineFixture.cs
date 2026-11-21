namespace NetEvolve.Schemas.Schematron.Tests.Unit.Pipeline;

using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NetEvolve.Schemas.Schematron.Model;

/// <summary>
/// The schema and the instance of the pipeline tests, and the helpers that feed them through every input kind. Spans cannot cross an
/// <see langword="await"/>, so every span and sequence is built and consumed in a synchronous method.
/// </summary>
internal static class PipelineFixture
{
    internal const string SchemaSystemId = "file:///invoice.sch";
    internal const string InstanceSystemId = "file:///invoice.xml";

    /// <summary>An invoice needs a number (an error) and a large total is reported (a warning). The message holds a character of two UTF-8 bytes.</summary>
    internal const string Schema = """
        <schema xmlns="http://purl.oclc.org/dsdl/schematron">
          <ns prefix="i" uri="urn:invoice"/>
          <pattern id="p1">
            <rule id="r1" context="i:invoice">
              <assert id="a1" test="i:number">An invoice needs a number, Müller.</assert>
              <report id="rp1" test="i:total &gt; 1000">Large invoice: <value-of select="i:total"/> for <value-of select="i:buyer"/></report>
            </rule>
          </pattern>
        </schema>
        """;

    /// <summary>The instance has no number and a total of 2500 for a buyer with a character of two UTF-8 bytes, so the assert and the report fire.</summary>
    internal const string Instance =
        "<invoice xmlns=\"urn:invoice\">\n  <total>2500</total>\n  <buyer>Müller</buyer>\n</invoice>";

    /// <summary>Two defects in one stage: a syntax error and an undeclared variable.</summary>
    internal const string TwoDefects = """
        <schema xmlns="http://purl.oclc.org/dsdl/schematron">
          <ns prefix="i" uri="urn:invoice"/>
          <pattern>
            <rule context="i:invoice">
              <assert test="i:number[">First defect.</assert>
              <assert test="$undefined">Second defect.</assert>
            </rule>
          </pattern>
        </schema>
        """;

    internal static string Wrap(string body, string attributes = "") =>
        "<schema xmlns=\"http://purl.oclc.org/dsdl/schematron\"" + attributes + ">" + body + "</schema>";

    internal static SchematronValidatorBuilder Builder(string schema, string? systemId = SchemaSystemId) =>
        SchemaValidator.ForSchematron(schema.AsSpan(), systemId);

    internal static ISchematronValidator Build(string schema, string? systemId = SchemaSystemId) =>
        Builder(schema, systemId).Build();

    internal static SchemaCompilationException BuildFailure(string schema, string? systemId = SchemaSystemId) =>
        Capture(() => _ = Builder(schema, systemId).Build());

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

    /// <summary>A canonical text of the issues of a result: code, severity, message, location, and constraint of each.</summary>
    internal static string Describe(ValidationResult result)
    {
        var text = new StringBuilder();
        foreach (var issue in result.Issues)
        {
            _ = text.Append(issue.Code)
                .Append('|')
                .Append(issue.Severity)
                .Append('|')
                .Append(issue.Message)
                .Append('|')
                .Append(issue.Location)
                .Append('|')
                .Append(issue.Constraint)
                .Append('\n');
        }

        return text.Append(result.IsValid).Append(result.IsTruncated).ToString();
    }

    /// <summary>A canonical text of what a compiled schema is: the edition, the phases, and every pattern, rule, and assertion.</summary>
    internal static string Describe(ISchematronValidator validator)
    {
        var text = new StringBuilder();
        _ = text.Append(validator.Edition)
            .Append('|')
            .Append(validator.Phase)
            .Append('|')
            .Append(validator.EffectivePhase);
        foreach (var pattern in validator.Schema.Patterns)
        {
            _ = text.Append("\npattern ").Append(pattern.Id).Append(pattern.IsAbstract ? " abstract" : string.Empty);
            foreach (var rule in pattern.Rules)
            {
                _ = text.Append("\n rule ").Append(rule.Id).Append(' ').Append(rule.Context);
                foreach (var assertion in rule.Items)
                {
                    if (assertion is SchematronAssertion found)
                    {
                        _ = text.Append("\n  ")
                            .Append(found is SchematronReport ? "report " : "assert ")
                            .Append(found.Id)
                            .Append(' ')
                            .Append(found.Test);
                    }
                }
            }
        }

        return text.ToString();
    }

    internal static byte[] Utf8(string text) => Encoding.UTF8.GetBytes(text);

    /// <summary>The text in UTF-16 with a byte order mark, so the reader has to detect the encoding.</summary>
    internal static byte[] Utf16(string text)
    {
        var body = Encoding.Unicode.GetBytes(text);
        var bytes = new byte[body.Length + 2];
        bytes[0] = 0xFF;
        bytes[1] = 0xFE;
        body.CopyTo(bytes, 2);
        return bytes;
    }

    /// <summary>Splits the bytes into three segments; the first cut falls inside the two bytes of a character when there is one.</summary>
    internal static ReadOnlySequence<byte> Segments(byte[] bytes)
    {
        var cut = Math.Max(1, Array.IndexOf(bytes, (byte)0xC3) + 1);
        var first = new Segment<byte>(bytes[..cut]);
        var second = first.Append(bytes[cut..(cut + ((bytes.Length - cut) / 2))]);
        var last = second.Append(bytes[(cut + ((bytes.Length - cut) / 2))..]);
        return new ReadOnlySequence<byte>(first, 0, last, last.Memory.Length);
    }

    internal static ReadOnlySequence<char> Segments(string text)
    {
        var characters = text.ToCharArray();
        var cut = Math.Max(1, text.IndexOf('ü', StringComparison.Ordinal));
        var first = new Segment<char>(characters[..cut]);
        var second = first.Append(characters[cut..(cut + ((characters.Length - cut) / 2))]);
        var last = second.Append(characters[(cut + ((characters.Length - cut) / 2))..]);
        return new ReadOnlySequence<char>(first, 0, last, last.Memory.Length);
    }

    /// <summary>Builds the same schema through every schema input kind, each with the same system identifier.</summary>
    internal static List<(string Kind, ISchematronValidator Validator)> BuildEveryKind(string schema)
    {
        var bytes = Utf8(schema);
        var result = new List<(string, ISchematronValidator)>();
        using (var stream = new MemoryStream(bytes))
        {
            result.Add(("stream", SchemaValidator.ForSchematron(stream, SchemaSystemId).Build()));
        }

        result.Add(("bytes", SchemaValidator.ForSchematron(bytes.AsSpan(), SchemaSystemId).Build()));
        var byteMemory = new ReadOnlyMemory<byte>(bytes);
        result.Add(("byte memory", SchemaValidator.ForSchematron(byteMemory.Span, SchemaSystemId).Build()));
        result.Add(("chars", SchemaValidator.ForSchematron(schema.AsSpan(), SchemaSystemId).Build()));
        var charMemory = schema.AsMemory();
        result.Add(("char memory", SchemaValidator.ForSchematron(charMemory.Span, SchemaSystemId).Build()));
        result.Add(("byte sequence", SchemaValidator.ForSchematron(Segments(bytes), SchemaSystemId).Build()));
        result.Add(("char sequence", SchemaValidator.ForSchematron(Segments(schema), SchemaSystemId).Build()));
        result.Add(("utf-16 bytes", SchemaValidator.ForSchematron(Utf16(schema).AsSpan(), SchemaSystemId).Build()));
        return result;
    }

    /// <summary>Validates the instance through every synchronous instance input kind.</summary>
    internal static List<(string Kind, ValidationResult Result)> ValidateSyncKinds(
        ISchematronValidator validator,
        string instance
    )
    {
        var bytes = Utf8(instance);
        var result = new List<(string, ValidationResult)>();
        using (var stream = new MemoryStream(bytes))
        {
            result.Add(("stream", validator.Validate(stream, InstanceSystemId)));
        }

        result.Add(("bytes", validator.Validate(bytes.AsSpan(), InstanceSystemId)));
        result.Add(("chars", validator.Validate(instance.AsSpan(), InstanceSystemId)));
        result.Add(("byte sequence", validator.Validate(Segments(bytes), InstanceSystemId)));
        result.Add(("char sequence", validator.Validate(Segments(instance), InstanceSystemId)));
        result.Add(("single segment", validator.Validate(new ReadOnlySequence<byte>(bytes), InstanceSystemId)));
        result.Add(("utf-16 bytes", validator.Validate(Utf16(instance).AsSpan(), InstanceSystemId)));
        return result;
    }

    internal static ValidationResult Validate(ISchematronValidator validator, string instance) =>
        validator.Validate(instance.AsSpan(), InstanceSystemId);

    private sealed class Segment<T> : ReadOnlySequenceSegment<T>
    {
        internal Segment(T[] items) => Memory = items;

        internal Segment<T> Append(T[] items)
        {
            var next = new Segment<T>(items) { RunningIndex = RunningIndex + Memory.Length };
            Next = next;
            return next;
        }
    }
}
